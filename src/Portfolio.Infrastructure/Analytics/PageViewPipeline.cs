using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portfolio.Application.Analytics;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Persistence;

namespace Portfolio.Infrastructure.Analytics;

/// <summary>İstek iş parçacığından gelen görüntülemeleri sınırlı bir kanala yazar; doluysa en eskisini atar.</summary>
public sealed class ChannelPageViewRecorder : IPageViewRecorder
{
    private readonly Channel<PageViewHit> _channel = Channel.CreateBounded<PageViewHit>(
        new BoundedChannelOptions(5_000) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    public ChannelReader<PageViewHit> Reader => _channel.Reader;

    public void Record(PageViewHit hit) => _channel.Writer.TryWrite(hit);
}

/// <summary>
/// Kanaldaki görüntülemeleri anonimleştirip toplu halde veritabanına yazar
/// (100 kayıtta bir ya da en geç 5 sn'de bir). Günde bir kez saklama süresi dolan kayıtları siler.
/// </summary>
public sealed partial class PageViewWriter : BackgroundService
{
    private const int BatchSize = 100;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan PurgeInterval = TimeSpan.FromDays(1);

    private readonly ChannelPageViewRecorder _recorder;
    private readonly IServiceScopeFactory _scopes;
    private readonly TimeProvider _clock;
    private readonly ILogger<PageViewWriter> _logger;
    private readonly byte[] _secret;
    private readonly TimeZoneInfo _zone;
    private DateTimeOffset _lastPurge = DateTimeOffset.MinValue;

    public PageViewWriter(
        ChannelPageViewRecorder recorder,
        IServiceScopeFactory scopes,
        IOptions<AnalyticsOptions> options,
        TimeProvider clock,
        ILogger<PageViewWriter> logger)
    {
        _recorder = recorder;
        _scopes = scopes;
        _clock = clock;
        _logger = logger;
        _zone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);

        if (string.IsNullOrWhiteSpace(options.Value.Secret))
        {
            // Gizli anahtar yoksa her açılışta yenisi üretilir: tekil ziyaretçi sayısı yeniden başlatmada sıfırlanır.
            _secret = RandomNumberGenerator.GetBytes(32);
            LogEphemeralSecret(_logger);
        }
        else
        {
            _secret = SHA256.HashData(Encoding.UTF8.GetBytes(options.Value.Secret));
        }
    }

    internal PageView ToPageView(PageViewHit hit)
    {
        var day = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(hit.OccurredAt, _zone).DateTime);
        var path = hit.Path.Length > PageView.PathMaxLength ? hit.Path[..PageView.PathMaxLength] : hit.Path;
        var referrer = TrafficClassifier.ReferrerHost(hit.Referrer, hit.SiteHost);
        if (referrer is { Length: > PageView.ReferrerMaxLength })
        {
            referrer = referrer[..PageView.ReferrerMaxLength];
        }

        return new PageView(
            path,
            referrer,
            TrafficClassifier.VisitorHash(_secret, day, hit.RemoteIp, hit.UserAgent),
            TrafficClassifier.Device(hit.UserAgent),
            hit.Language,
            day,
            hit.OccurredAt);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batch = new List<PageView>(BatchSize);
        var reader = _recorder.Reader;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var flushTimer = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                flushTimer.CancelAfter(FlushInterval);

                while (batch.Count < BatchSize && await reader.WaitToReadAsync(flushTimer.Token))
                {
                    while (batch.Count < BatchSize && reader.TryRead(out var hit))
                    {
                        batch.Add(ToPageView(hit));
                    }
                }
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                // Zamanlayıcı doldu: elimizdekileri yaz.
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await FlushAsync(batch, stoppingToken);
            await PurgeIfDueAsync(stoppingToken);
        }

        await FlushAsync(batch, CancellationToken.None);
    }

    private async Task FlushAsync(List<PageView> batch, CancellationToken cancellationToken)
    {
        if (batch.Count == 0)
        {
            return;
        }

        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.PageViews.AddRange(batch);
            await db.SaveChangesAsync(cancellationToken);
        }
#pragma warning disable CA1031 // İstatistik yazımı hiçbir koşulda uygulamayı durdurmamalı.
        catch (Exception ex) when (ex is not OperationCanceledException)
#pragma warning restore CA1031
        {
            LogFlushFailed(_logger, ex, batch.Count);
        }
        finally
        {
            batch.Clear();
        }
    }

    private async Task PurgeIfDueAsync(CancellationToken cancellationToken)
    {
        if (_clock.GetUtcNow() - _lastPurge < PurgeInterval)
        {
            return;
        }

        _lastPurge = _clock.GetUtcNow();
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var removed = await scope.ServiceProvider.GetRequiredService<IAnalyticsService>().PurgeExpiredAsync(cancellationToken);
            if (removed > 0)
            {
                LogPurged(_logger, removed);
            }
        }
#pragma warning disable CA1031
        catch (Exception ex) when (ex is not OperationCanceledException)
#pragma warning restore CA1031
        {
            LogPurgeFailed(_logger, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Analytics:Secret tanımlı değil; geçici anahtar kullanılıyor (tekil ziyaretçi sayıları yeniden başlatmada sıfırlanır).")]
    private static partial void LogEphemeralSecret(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "{Count} sayfa görüntüleme kaydı yazılamadı.")]
    private static partial void LogFlushFailed(ILogger logger, Exception exception, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Saklama süresi dolan {Count} sayfa görüntüleme kaydı silindi.")]
    private static partial void LogPurged(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Eski istatistik kayıtları silinemedi.")]
    private static partial void LogPurgeFailed(ILogger logger, Exception exception);
}
