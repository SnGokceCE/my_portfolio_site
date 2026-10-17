using System.Security.Cryptography;
using System.Text;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Analytics;

public sealed class AnalyticsOptions
{
    public const string SectionName = "Analytics";

    /// <summary>Ziyaretçi özetinin gizli anahtarı. Boşsa uygulama her başladığında rastgele üretilir.</summary>
    public string? Secret { get; set; }

    public string TimeZone { get; set; } = "Europe/Istanbul";

    /// <summary>Bu günden eski kayıtlar otomatik silinir (veri minimizasyonu).</summary>
    public int RetentionDays { get; set; } = 400;
}

/// <summary>Middleware'in yakaladığı ham istek bilgisi; kaydedilmeden önce anonimleştirilir.</summary>
public sealed record PageViewHit(
    string Path,
    string? Referrer,
    string? RemoteIp,
    string? UserAgent,
    string Language,
    string SiteHost,
    DateTimeOffset OccurredAt);

/// <summary>Sayfa görüntülemelerini isteği bekletmeden kaydeder (arka planda toplu yazım).</summary>
public interface IPageViewRecorder
{
    void Record(PageViewHit hit);
}

public sealed record DailyTraffic(DateOnly Day, int Views, int Visitors);

public sealed record RankedItem(string Label, int Views, int Visitors);

public sealed record ShareItem(string Label, int Count, double Percent);

public sealed record AnalyticsOverview(
    int Days,
    int Views,
    int Visitors,
    double? ViewsChange,
    double? VisitorsChange,
    int TodayViews,
    IReadOnlyList<DailyTraffic> Daily,
    IReadOnlyList<RankedItem> TopPages,
    IReadOnlyList<RankedItem> TopReferrers,
    IReadOnlyList<ShareItem> Devices,
    IReadOnlyList<ShareItem> Languages);

/// <summary>User-Agent ve IP'den anonim kayıt üreten saf (yan etkisiz) yardımcılar; birim testi kolaydır.</summary>
public static class TrafficClassifier
{
    private static readonly string[] BotMarkers =
    [
        "bot", "crawl", "spider", "slurp", "preview", "headless", "lighthouse", "curl", "wget",
        "python-requests", "httpclient", "go-http", "monitor", "uptime", "facebookexternalhit"
    ];

    public static bool IsBot(string? userAgent) =>
        string.IsNullOrWhiteSpace(userAgent) ||
        BotMarkers.Any(marker => userAgent.Contains(marker, StringComparison.OrdinalIgnoreCase));

    public static DeviceType Device(string? userAgent)
    {
        if (userAgent is null)
        {
            return DeviceType.Desktop;
        }

        if (userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase) ||
            (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase) &&
             !userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase)))
        {
            return DeviceType.Tablet;
        }

        return userAgent.Contains("Mobi", StringComparison.OrdinalIgnoreCase) ||
               userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase)
            ? DeviceType.Mobile
            : DeviceType.Desktop;
    }

    /// <summary>Sadece dış sitelerden gelen yönlendirmeler; "www." öneki atılır, iç gezinme sayılmaz.</summary>
    public static string? ReferrerHost(string? referrer, string siteHost)
    {
        if (!Uri.TryCreate(referrer, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return null;
        }

        var host = uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? uri.Host[4..] : uri.Host;
        var site = siteHost.Split(':')[0];
        site = site.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? site[4..] : site;
        return host.Equals(site, StringComparison.OrdinalIgnoreCase) ? null : host.ToLowerInvariant();
    }

    /// <summary>
    /// HMAC-SHA256(gizli anahtar, gün | IP | User-Agent) → ilk 16 onaltılık karakter.
    /// Gün anahtarın bir parçası olduğu için aynı kişi ertesi gün farklı görünür; IP geri elde edilemez.
    /// </summary>
    public static string VisitorHash(byte[] secret, DateOnly day, string? ip, string? userAgent)
    {
        var data = Encoding.UTF8.GetBytes($"{day:yyyy-MM-dd}|{ip}|{userAgent}");
        var hash = HMACSHA256.HashData(secret, data);
        return Convert.ToHexStringLower(hash)[..PageView.VisitorHashLength];
    }
}
