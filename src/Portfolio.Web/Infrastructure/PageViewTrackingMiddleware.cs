using Portfolio.Application.Analytics;
using Portfolio.Application.Common.Security;
using Portfolio.Web.Localization;

namespace Portfolio.Web.Infrastructure;

/// <summary>
/// Başarılı HTML sayfa yanıtlarını istatistik kuyruğuna yazar. Sayılmayanlar: yönetim ve giriş
/// sayfaları, admin'in kendi gezintisi, botlar, ön yüklemeler ve "beni izleme" (DNT / GPC) diyen tarayıcılar.
/// </summary>
public sealed class PageViewTrackingMiddleware(RequestDelegate next, IPageViewRecorder recorder, TimeProvider clock)
{
    private static readonly string[] ExcludedPrefixes = ["/admin", "/account", "/home/", "/health"];

    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        if (!ShouldTrack(context))
        {
            return;
        }

        var request = context.Request;

        // "/en/" ile "/en" aynı sayfa: raporda tek satır olsun diye sondaki "/" atılır (kök hariç).
        var path = (request.PathBase + request.Path).Value?.ToLowerInvariant() ?? "/";
        if (path.Length > 1)
        {
            path = path.TrimEnd('/');
        }

        recorder.Record(new PageViewHit(
            path,
            request.Headers.Referer.ToString(),
            context.Connection.RemoteIpAddress?.ToString(),
            request.Headers.UserAgent.ToString(),
            SiteCultures.IsEnglish(request) ? "en" : "tr",
            request.Host.Value ?? string.Empty,
            clock.GetUtcNow()));
    }

    private static bool ShouldTrack(HttpContext context)
    {
        var request = context.Request;
        if (!HttpMethods.IsGet(request.Method) ||
            context.Response.StatusCode != StatusCodes.Status200OK ||
            context.Response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) != true)
        {
            return false;
        }

        var path = request.Path.Value ?? "/";
        if (ExcludedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)) ||
            context.User.IsInRole(Roles.Admin))
        {
            return false;
        }

        var headers = request.Headers;
        if (headers["DNT"] == "1" || headers["Sec-GPC"] == "1" ||
            headers["Sec-Purpose"].ToString().Contains("prefetch", StringComparison.OrdinalIgnoreCase) ||
            headers["Purpose"] == "prefetch")
        {
            return false;
        }

        return !TrafficClassifier.IsBot(headers.UserAgent);
    }
}
