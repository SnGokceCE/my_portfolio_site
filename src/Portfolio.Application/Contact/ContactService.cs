using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Application.Common.Models;
using Portfolio.Application.Common.Validation;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Contact;

public interface IContactService
{
    /// <summary>Mesajı kaydeder; spam değilse site sahibine bildirim e-postası sıraya alınır.</summary>
    Task<Result> SubmitAsync(ContactInput input, CancellationToken cancellationToken = default);

    Task<PagedList<MessageListItem>> ListAsync(MessageFilter filter, int page, CancellationToken cancellationToken = default);

    /// <summary>Mesajı getirir ve okundu olarak işaretler.</summary>
    Task<MessageDetail?> OpenAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result> MarkUnreadAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result> SetSpamAsync(Guid id, bool isSpam, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<int> CountUnreadAsync(CancellationToken cancellationToken = default);
}

internal sealed class ContactService(
    IApplicationDbContext db,
    IValidator<ContactInput> validator,
    IEmailOutbox emails,
    IOptions<ContactOptions> options,
    TimeProvider clock) : IContactService
{
    private const int PageSize = 15;
    private const int PreviewLength = 140;

    public async Task<Result> SubmitAsync(ContactInput input, CancellationToken cancellationToken = default)
    {
        var errors = await validator.ValidateInputAsync(input, cancellationToken);
        if (errors is not null)
        {
            return Result.Invalid(errors);
        }

        var isSpam = SpamHeuristics.LooksLikeSpam(input, options.Value.MaxLinks);
        ContactMessage? message = null;
        errors = ValidationExtensions.TryApply(() =>
            message = new ContactMessage(input.Name, input.Email.Trim(), input.Subject, input.Message, isSpam));
        if (errors is not null)
        {
            return Result.Invalid(errors);
        }

        db.ContactMessages.Add(message!);
        await db.SaveChangesAsync(cancellationToken);

        if (!isSpam && await ResolveRecipientAsync(cancellationToken) is { } recipient)
        {
            await emails.EnqueueAsync(BuildNotification(message!, recipient), cancellationToken);
        }

        return Result.Success();
    }

    public async Task<PagedList<MessageListItem>> ListAsync(MessageFilter filter, int page, CancellationToken cancellationToken = default)
    {
        var query = db.ContactMessages.AsNoTracking();
        query = filter switch
        {
            MessageFilter.Unread => query.Where(m => !m.IsSpam && m.ReadAt == null),
            MessageFilter.Spam => query.Where(m => m.IsSpam),
            _ => query.Where(m => !m.IsSpam)
        };

        // Substring burada SQL'deki substr(...) fonksiyonuna çevrilir; span önerisi geçersiz.
#pragma warning disable CA1845
        return await query
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new MessageListItem(
                m.Id,
                m.Name,
                m.Email,
                m.Subject,
                m.Message.Length > PreviewLength ? m.Message.Substring(0, PreviewLength) + "…" : m.Message,
                m.ReadAt != null,
                m.IsSpam,
                m.CreatedAt))
            .ToPagedListAsync(page, PageSize, cancellationToken);
#pragma warning restore CA1845
    }

    public async Task<MessageDetail?> OpenAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var message = await db.ContactMessages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (message is null)
        {
            return null;
        }

        if (!message.IsRead)
        {
            message.MarkRead(clock.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
        }

        return new MessageDetail(
            message.Id, message.Name, message.Email, message.Subject, message.Message,
            message.IsSpam, message.CreatedAt, message.ReadAt);
    }

    public Task<Result> MarkUnreadAsync(Guid id, CancellationToken cancellationToken = default) =>
        UpdateAsync(id, m => m.MarkUnread(), cancellationToken);

    public Task<Result> SetSpamAsync(Guid id, bool isSpam, CancellationToken cancellationToken = default) =>
        UpdateAsync(id, m => m.SetSpam(isSpam), cancellationToken);

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var message = await db.ContactMessages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (message is null)
        {
            return Result.NotFound();
        }

        db.ContactMessages.Remove(message);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public Task<int> CountUnreadAsync(CancellationToken cancellationToken = default) =>
        db.ContactMessages.CountAsync(m => !m.IsSpam && m.ReadAt == null, cancellationToken);

    private async Task<Result> UpdateAsync(Guid id, Action<ContactMessage> change, CancellationToken cancellationToken)
    {
        var message = await db.ContactMessages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (message is null)
        {
            return Result.NotFound();
        }

        change(message);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<string?> ResolveRecipientAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(options.Value.NotifyEmail))
        {
            return options.Value.NotifyEmail;
        }

        return await db.SiteProfiles
            .AsNoTracking()
            .OrderBy(p => p.CreatedAt)
            .Select(p => p.Email)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static EmailMessage BuildNotification(ContactMessage message, string recipient)
    {
        var subject = string.IsNullOrWhiteSpace(message.Subject)
            ? $"Portfolyo: {message.Name} sana yazdı"
            : $"Portfolyo: {message.Subject}";

        var body = $"""
            Sitedeki iletişim formundan yeni bir mesaj var.

            Gönderen: {message.Name} <{message.Email}>
            Konu: {message.Subject ?? "(yok)"}

            {message.Message}

            —
            Bu e-postayı yanıtlarsan cevabın doğrudan gönderene gider.
            """;

        return new EmailMessage(recipient, subject, body, ReplyTo: message.Email);
    }
}
