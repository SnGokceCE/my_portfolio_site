using FluentValidation;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Contact;

public sealed class ContactOptions
{
    public const string SectionName = "Contact";

    /// <summary>Yeni mesaj bildiriminin gideceği adres. Boşsa profildeki e-posta kullanılır.</summary>
    public string? NotifyEmail { get; set; }

    /// <summary>Bir mesajda bundan fazla bağlantı varsa spam sayılır.</summary>
    public int MaxLinks { get; set; } = 2;
}

public sealed class ContactInput
{
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Subject { get; set; }

    public string Message { get; set; } = string.Empty;
}

/// <summary>Ziyaretçiye açık form: mesajlar sayfanın diline (tr/en) göre üretilir.</summary>
public sealed class ContactInputValidator : AbstractValidator<ContactInput>
{
    public ContactInputValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().MaximumLength(ContactMessage.NameMaxLength)
            .WithName(_ => Lang.T("Adın", "Your name"));
        RuleFor(x => x.Email)
            .NotEmpty().WithName(_ => Lang.T("E-posta", "Email"))
            .EmailAddress().WithMessage(_ => Lang.T("Geçerli bir e-posta adresi gir.", "Please enter a valid email address."))
            .MaximumLength(ContactMessage.EmailMaxLength);
        RuleFor(x => x.Subject)
            .MaximumLength(ContactMessage.SubjectMaxLength)
            .WithName(_ => Lang.T("Konu", "Subject"));
        RuleFor(x => x.Message)
            .NotEmpty().WithName(_ => Lang.T("Mesaj", "Message"))
            .MinimumLength(ContactMessage.MessageMinLength)
            .WithMessage(_ => Lang.T(
                $"Mesaj en az {ContactMessage.MessageMinLength} karakter olmalı.",
                $"Message must be at least {ContactMessage.MessageMinLength} characters."))
            .MaximumLength(ContactMessage.MessageMaxLength);
    }
}

public enum MessageFilter
{
    Inbox,
    Unread,
    Spam
}

public sealed record MessageListItem(
    Guid Id,
    string Name,
    string Email,
    string? Subject,
    string Preview,
    bool IsRead,
    bool IsSpam,
    DateTimeOffset ReceivedAt);

public sealed record MessageDetail(
    Guid Id,
    string Name,
    string Email,
    string? Subject,
    string Message,
    bool IsSpam,
    DateTimeOffset ReceivedAt,
    DateTimeOffset? ReadAt);
