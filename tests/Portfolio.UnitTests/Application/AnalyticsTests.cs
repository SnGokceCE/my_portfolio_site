using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Portfolio.Application.Analytics;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Analytics;

namespace Portfolio.UnitTests.Application;

public class TrafficClassifierTests
{
    private const string ChromeDesktop = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/130.0 Safari/537.36";
    private const string IPhone = "Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) Mobile/15E148 Safari/604.1";

    [Theory]
    [InlineData("Googlebot/2.1 (+http://www.google.com/bot.html)", true)]
    [InlineData("curl/8.4.0", true)]
    [InlineData("", true)]
    [InlineData(null, true)]
    [InlineData(ChromeDesktop, false)]
    public void IsBot(string? userAgent, bool expected) => Assert.Equal(expected, TrafficClassifier.IsBot(userAgent));

    [Theory]
    [InlineData(ChromeDesktop, DeviceType.Desktop)]
    [InlineData(IPhone, DeviceType.Mobile)]
    [InlineData("Mozilla/5.0 (iPad; CPU OS 17_0 like Mac OS X)", DeviceType.Tablet)]
    [InlineData("Mozilla/5.0 (Linux; Android 14; SM-X710) Safari/537.36", DeviceType.Tablet)]
    [InlineData("Mozilla/5.0 (Linux; Android 14; Pixel 8) Mobile Safari/537.36", DeviceType.Mobile)]
    public void Device(string userAgent, DeviceType expected) => Assert.Equal(expected, TrafficClassifier.Device(userAgent));

    [Theory]
    [InlineData("https://www.linkedin.com/in/ada", "portfolyo.dev", "linkedin.com")]
    [InlineData("https://portfolyo.dev/projeler", "portfolyo.dev", null)]
    [InlineData("https://www.portfolyo.dev/blog", "portfolyo.dev:443", null)]
    [InlineData("android-app://com.slack", "portfolyo.dev", null)]
    [InlineData(null, "portfolyo.dev", null)]
    public void ReferrerHost_KeepsOnlyExternalDomain(string? referrer, string site, string? expected) =>
        Assert.Equal(expected, TrafficClassifier.ReferrerHost(referrer, site));

    [Fact]
    public void VisitorHash_IsStableWithinADay_ButChangesNextDay()
    {
        var secret = Encoding.UTF8.GetBytes("gizli");
        var day = new DateOnly(2026, 10, 17);

        var morning = TrafficClassifier.VisitorHash(secret, day, "203.0.113.5", ChromeDesktop);
        var evening = TrafficClassifier.VisitorHash(secret, day, "203.0.113.5", ChromeDesktop);
        var tomorrow = TrafficClassifier.VisitorHash(secret, day.AddDays(1), "203.0.113.5", ChromeDesktop);

        Assert.Equal(morning, evening);
        Assert.NotEqual(morning, tomorrow);
        Assert.Equal(PageView.VisitorHashLength, morning.Length);
        Assert.DoesNotContain("203", morning, StringComparison.Ordinal);
    }
}

public sealed class AnalyticsServiceTests : IAsyncLifetime
{
    private readonly TestServices _services = new(s =>
        s.Configure<AnalyticsOptions>(o => { o.TimeZone = "Europe/Istanbul"; o.RetentionDays = 30; }));

    private static readonly DateOnly Today = new(2026, 10, 17); // TestServices.Now = 17 Ekim 09:00 UTC (İstanbul 12:00)

    public async Task InitializeAsync()
    {
        PageView View(string path, string visitor, int daysAgo, string? referrer = null, DeviceType device = DeviceType.Desktop) =>
            new(path, referrer, visitor.PadRight(16, '0'), device, "tr", Today.AddDays(-daysAgo), TestServices.Now.AddDays(-daysAgo));

        _services.Db.PageViews.AddRange(
            View("/", "a", 0), View("/projeler", "a", 0), View("/", "b", 0, "linkedin.com", DeviceType.Mobile),
            View("/", "c", 1), View("/blog", "c", 1),
            // Önceki 7 günlük dönem: 2 görüntüleme
            View("/", "d", 8), View("/", "e", 9),
            // Saklama süresi (30 gün) dolmuş kayıt
            View("/", "f", 45));
        await _services.Db.SaveChangesAsync();
    }

    [Fact]
    public async Task Overview_CountsViewsAndDailyUniqueVisitors()
    {
        var overview = await _services.Get<IAnalyticsService>().GetOverviewAsync(7);

        Assert.Equal(5, overview.Views);
        Assert.Equal(3, overview.Visitors); // bugün a+b, dün c
        Assert.Equal(3, overview.TodayViews);
        Assert.Equal(7, overview.Daily.Count);
        Assert.Equal(Today, overview.Daily[^1].Day);
        Assert.Equal(150, overview.ViewsChange); // 2 → 5 = %150 artış
    }

    [Fact]
    public async Task Overview_RanksPagesReferrersAndDevices()
    {
        var overview = await _services.Get<IAnalyticsService>().GetOverviewAsync(7);

        Assert.Equal("/", overview.TopPages[0].Label);
        Assert.Equal(3, overview.TopPages[0].Views);
        Assert.Equal("linkedin.com", Assert.Single(overview.TopReferrers).Label);
        Assert.Equal(80, overview.Devices.Single(d => d.Label == "Desktop").Percent);
    }

    [Fact]
    public async Task Purge_RemovesOnlyExpiredViews()
    {
        var removed = await _services.Get<IAnalyticsService>().PurgeExpiredAsync();

        Assert.Equal(1, removed);
        Assert.Equal(7, _services.Db.PageViews.Count());
    }

    [Fact]
    public void Writer_AssignsLocalDay_AndNeverStoresRawIp()
    {
        var writer = new PageViewWriter(
            new ChannelPageViewRecorder(),
            _services.Get<IServiceScopeFactory>(),
            Options.Create(new AnalyticsOptions { Secret = "test", TimeZone = "Europe/Istanbul" }),
            TimeProvider.System,
            NullLogger<PageViewWriter>.Instance);

        // 17 Ekim 22:30 UTC = 18 Ekim 01:30 İstanbul
        var view = writer.ToPageView(new PageViewHit(
            "/projeler", "https://github.com/ada", "198.51.100.7", "Mozilla/5.0 (iPhone) Mobile", "en", "site.dev",
            new DateTimeOffset(2026, 10, 17, 22, 30, 0, TimeSpan.Zero)));

        Assert.Equal(new DateOnly(2026, 10, 18), view.Day);
        Assert.Equal("github.com", view.ReferrerHost);
        Assert.Equal(DeviceType.Mobile, view.Device);
        Assert.DoesNotContain("198.51", view.VisitorHash, StringComparison.Ordinal);
    }

    public Task DisposeAsync() => _services.DisposeAsync().AsTask();
}
