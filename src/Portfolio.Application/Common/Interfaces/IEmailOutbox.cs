namespace Portfolio.Application.Common.Interfaces;

public sealed record EmailMessage(string To, string Subject, string TextBody, string? ReplyTo = null);

/// <summary>
/// E-postaları arka planda gönderilmek üzere sıraya alır. İstek, SMTP sunucusunun yanıtını
/// beklemez; gönderim hatası kullanıcının işlemini (ör. form gönderimi) bozmaz.
/// </summary>
public interface IEmailOutbox
{
    ValueTask<bool> EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
