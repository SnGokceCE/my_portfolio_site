using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Analytics;

public interface IAnalyticsService
{
    Task<AnalyticsOverview> GetOverviewAsync(int days, CancellationToken cancellationToken = default);

    Task<int> PurgeExpiredAsync(CancellationToken cancellationToken = default);
}

internal sealed class AnalyticsService(
    IApplicationDbContext db,
    IOptions<AnalyticsOptions> options,
    TimeProvider clock) : IAnalyticsService
{
    private const int TopCount = 10;

    public async Task<AnalyticsOverview> GetOverviewAsync(int days, CancellationToken cancellationToken = default)
    {
        days = Math.Clamp(days, 1, 365);
        var today = Today();
        var from = today.AddDays(-(days - 1));
        var previousFrom = from.AddDays(-days);

        var views = db.PageViews.AsNoTracking();
        var period = views.Where(v => v.Day >= from && v.Day <= today);

        // Günlük tekil ziyaretçi = o günün farklı özet sayısı (özet her gün değiştiği için doğal olarak günlüktür).
        var daily = (await period
                .GroupBy(v => v.Day)
                .Select(g => new { Day = g.Key, Views = g.Count(), Visitors = g.Select(v => v.VisitorHash).Distinct().Count() })
                .ToListAsync(cancellationToken))
            .Select(d => new DailyTraffic(d.Day, d.Views, d.Visitors))
            .ToList();
        var byDay = daily.ToDictionary(d => d.Day);
        var series = Enumerable.Range(0, days)
            .Select(i => from.AddDays(i))
            .Select(d => byDay.TryGetValue(d, out var t) ? t : new DailyTraffic(d, 0, 0))
            .ToList();

        var previous = await views
            .Where(v => v.Day >= previousFrom && v.Day < from)
            .GroupBy(v => v.Day)
            .Select(g => new { Views = g.Count(), Visitors = g.Select(v => v.VisitorHash).Distinct().Count() })
            .ToListAsync(cancellationToken);

        var totalViews = series.Sum(d => d.Views);
        var totalVisitors = series.Sum(d => d.Visitors);

        // Sıralama anonim tip üzerinde yapılır; kayıt (record) en sonda oluşturulur, aksi halde SQL'e çevrilemez.
        var topPages = (await period
                .GroupBy(v => v.Path)
                .Select(g => new { Label = g.Key, Views = g.Count(), Visitors = g.Select(v => v.VisitorHash).Distinct().Count() })
                .OrderByDescending(r => r.Views)
                .Take(TopCount)
                .ToListAsync(cancellationToken))
            .Select(r => new RankedItem(r.Label, r.Views, r.Visitors))
            .ToList();

        var topReferrers = (await period
                .Where(v => v.ReferrerHost != null)
                .GroupBy(v => v.ReferrerHost!)
                .Select(g => new { Label = g.Key, Views = g.Count(), Visitors = g.Select(v => v.VisitorHash).Distinct().Count() })
                .OrderByDescending(r => r.Views)
                .Take(TopCount)
                .ToListAsync(cancellationToken))
            .Select(r => new RankedItem(r.Label, r.Views, r.Visitors))
            .ToList();

        var devices = await period.GroupBy(v => v.Device).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(cancellationToken);
        var languages = await period.GroupBy(v => v.Language).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(cancellationToken);

        return new AnalyticsOverview(
            days,
            totalViews,
            totalVisitors,
            Change(totalViews, previous.Sum(p => p.Views)),
            Change(totalVisitors, previous.Sum(p => p.Visitors)),
            byDay.TryGetValue(today, out var todayTraffic) ? todayTraffic.Views : 0,
            series,
            topPages,
            topReferrers,
            Shares(devices.Select(d => (d.Key.ToString(), d.Count))),
            Shares(languages.Select(l => (l.Key, l.Count))));
    }

    public async Task<int> PurgeExpiredAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = Today().AddDays(-options.Value.RetentionDays);
        var expired = await db.PageViews.Where(v => v.Day < cutoff).ToListAsync(cancellationToken);
        db.PageViews.RemoveRange(expired);
        await db.SaveChangesAsync(cancellationToken);
        return expired.Count;
    }

    private DateOnly Today()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), zone).DateTime);
    }

    /// <summary>Önceki döneme göre yüzde değişim; önceki dönem boşsa karşılaştırma anlamsızdır (null).</summary>
    private static double? Change(int current, int previous) =>
        previous == 0 ? null : Math.Round(100.0 * (current - previous) / previous, 1);

    private static List<ShareItem> Shares(IEnumerable<(string Label, int Count)> items)
    {
        var list = items.OrderByDescending(i => i.Count).ToList();
        var total = list.Sum(i => i.Count);
        return list.Select(i => new ShareItem(i.Label, i.Count, total == 0 ? 0 : Math.Round(100.0 * i.Count / total, 1))).ToList();
    }
}
