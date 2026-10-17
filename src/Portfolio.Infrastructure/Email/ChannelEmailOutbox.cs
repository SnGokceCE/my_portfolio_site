using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Portfolio.Application.Common.Interfaces;

namespace Portfolio.Infrastructure.Email;

/// <summary>
/// Bellek içi, sınırlı kapasiteli e-posta kuyruğu. İstek iş parçacığı sadece mesajı kanala yazar;
/// <see cref="EmailDispatcher"/> arka planda okuyup gönderir.
/// </summary>
public sealed partial class ChannelEmailOutbox(ILogger<ChannelEmailOutbox> logger) : IEmailOutbox
{
    private const int Capacity = 100;

    private readonly Channel<EmailMessage> _channel = Channel.CreateBounded<EmailMessage>(
        new BoundedChannelOptions(Capacity) { FullMode = BoundedChannelFullMode.Wait, SingleReader = true });

    public ChannelReader<EmailMessage> Reader => _channel.Reader;

    public ValueTask<bool> EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        // Kuyruk doluysa isteği bekletmek yerine e-postayı düşür; mesaj zaten veritabanında.
        if (_channel.Writer.TryWrite(message))
        {
            return ValueTask.FromResult(true);
        }

        LogQueueFull(logger, message.Subject);
        return ValueTask.FromResult(false);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "E-posta kuyruğu dolu; \"{Subject}\" gönderilmeyecek.")]
    private static partial void LogQueueFull(ILogger logger, string subject);
}
