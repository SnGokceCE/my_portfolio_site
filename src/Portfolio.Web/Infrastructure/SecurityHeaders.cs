using System.Security.Cryptography;

namespace Portfolio.Web.Infrastructure;

/// <summary>
/// Tarayıcı güvenlik başlıkları. İçerik Güvenlik Politikası (CSP) yalnızca bu siteden ve izin verilen
/// font sağlayıcısından kaynak yüklenmesine izin verir; satır içi betikler yalnızca istek başına üretilen
/// nonce ile çalışır. Böylece içeriğe bir şekilde betik sızsa bile tarayıcı onu çalıştırmaz (XSS'e karşı ikinci hat).
/// </summary>
public static class SecurityHeaders
{
    private const string NonceKey = "csp-nonce";

    public static string CspNonce(this HttpContext context) =>
        context.Items[NonceKey] as string ?? string.Empty;

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use((context, next) =>
        {
            var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
            context.Items[NonceKey] = nonce;

            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers.ContentSecurityPolicy = string.Join("; ",
                    "default-src 'self'",
                    $"script-src 'self' 'nonce-{nonce}'",
                    // style="--level: 3" gibi öznitelikler ve view-transition-name için satır içi stile izin verilir.
                    "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com",
                    "font-src 'self' https://fonts.gstatic.com",
                    // Markdown içerikte başka sitelerden (https) görsel; admin önizlemesi için blob: adresleri.
                    "img-src 'self' data: blob: https:",
                    "connect-src 'self'",
                    "object-src 'none'",
                    "base-uri 'self'",
                    "form-action 'self'",
                    "frame-ancestors 'none'");
                headers.XContentTypeOptions = "nosniff";
                headers.XFrameOptions = "DENY";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), interest-cohort=()";
                headers["Cross-Origin-Opener-Policy"] = "same-origin";
                return Task.CompletedTask;
            });

            return next(context);
        });
}
