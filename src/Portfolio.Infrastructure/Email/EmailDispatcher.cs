using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portfolio.Application.Common.Interfaces;

namespace Portfolio.Infrastructure.Email;

/// <summary>
/// Kuyruktaki e-postaları sırayla gönderen arka plan servisi. Geçici hatalarda
/// (SMTP kapalı, ağ kesintisi) artan bekleme süresiyle yeniden dener.
/// </summary>
public sealed partial class EmailDispatcher(
    ChannelEmailOutbox outbox,
    IEmailTransport transport,
    IOptions<EmailOptions> options,
    TimeProvider clock,
    ILogger<EmailDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var message in outbox.Reader.ReadAllAsync(stoppingToken))
            {
                await SendWithRetryAsync(message, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Uygulama kapanıyor.
        }
    }

    internal async Task<bool> SendWithRetryAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        for (var attempt = 1; attempt <= settings.MaxAttempts; attempt++)
        {
            try
            {
                await transport.SendAsync(message, cancellationToken);
                LogSent(logger, message.To, message.Subject);
                return true;
            }
#pragma warning disable CA1031 // Arka plan servisi her hatayı yakalamalı; yoksa tüm gönderim durur.
            catch (Exception ex) when (ex is not OperationCanceledException)
#pragma warning restore CA1031
            {
                LogAttemptFailed(logger, ex, attempt, settings.MaxAttempts, message.Subject);
                if (attempt < settings.MaxAttempts)
                {
                    // 2 sn, 4 sn, 8 sn ... (üstel geri çekilme)
                    var delay = settings.RetryDelay * Math.Pow(2, attempt - 1);
                    await Task.Delay(delay, clock, cancellationToken);
                }
            }
        }

        return false;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "E-posta gönderildi: {To} — {Subject}")]
    private static partial void LogSent(ILogger logger, string to, string subject);

    [LoggerMessage(Level = LogLevel.Warning, Message = "E-posta gönderimi başarısız ({Attempt}/{MaxAttempts}): {Subject}")]
    private static partial void LogAttemptFailed(ILogger logger, Exception exception, int attempt, int maxAttempts, string subject);
}
