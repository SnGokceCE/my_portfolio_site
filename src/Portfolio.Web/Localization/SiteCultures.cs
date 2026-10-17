using System.Globalization;
using Microsoft.AspNetCore.Localization;

namespace Portfolio.Web.Localization;

/// <summary>
/// Dil, URL'den belirlenir: "/en/..." İngilizce, diğer tüm adresler Türkçe.
/// Böylece her dilin kendi adresi olur; arama motorları iki sürümü ayrı ayrı dizine ekler (hreflang).
/// </summary>
public static class SiteCultures
{
    public const string EnglishPrefix = "/en";

    public static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
    public static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    /// <summary>
    /// "/en" ön ekini PathBase'e taşır. Sonraki tüm katmanlar (yönlendirme, statik dosyalar,
    /// bağlantı üretimi) ön eki görmeden çalışır; üretilen bağlantılar ise otomatik olarak "/en" ile başlar.
    /// </summary>
    public static IApplicationBuilder UseCulturePrefix(this IApplicationBuilder app) =>
        app.Use((context, next) =>
        {
            if (context.Request.Path.StartsWithSegments(EnglishPrefix, StringComparison.OrdinalIgnoreCase, out var remaining))
            {
                context.Request.PathBase = context.Request.PathBase.Add(EnglishPrefix);
                context.Request.Path = remaining.HasValue ? remaining : "/";
            }

            return next(context);
        });

    public static IApplicationBuilder UseSiteLocalization(this IApplicationBuilder app) =>
        app.UseRequestLocalization(new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture(Turkish),
            SupportedCultures = [Turkish, English],
            SupportedUICultures = [Turkish, English],
            // Tarayıcı dili, çerez ya da sorgu dizesi değil; yalnızca URL belirleyicidir.
            RequestCultureProviders =
            [
                new CustomRequestCultureProvider(context => Task.FromResult<ProviderCultureResult?>(
                    IsEnglish(context.Request) ? new ProviderCultureResult(English.Name) : null))
            ]
        });

    public static bool IsEnglish(HttpRequest request) =>
        request.PathBase.StartsWithSegments(EnglishPrefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>Aynı sayfanın Türkçe ve İngilizce adresleri (dil değiştirici ve hreflang için).</summary>
    public static (string Turkish, string English) AlternateUrls(HttpRequest request)
    {
        var path = request.Path.Value is { Length: > 1 } p ? p : string.Empty;
        var query = request.QueryString.Value ?? string.Empty;
        var pathBase = request.PathBase.Value ?? string.Empty;
        var root = IsEnglish(request) ? pathBase[..^EnglishPrefix.Length] : pathBase;

        var turkish = $"{root}{(path.Length == 0 ? "/" : path)}{query}";
        var english = $"{root}{EnglishPrefix}{path}{query}";
        return (turkish, english);
    }
}
