using FluentValidation;
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

public sealed class ContactInputValidator : AbstractValidator<ContactInput>
{
    public ContactInputValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(ContactMessage.NameMaxLength).WithName("Adın");
        RuleFor(x => x.Email)
            .NotEmpty().WithName("E-posta")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi gir.")
            .MaximumLength(ContactMessage.EmailMaxLength);
        RuleFor(x => x.Subject).MaximumLength(ContactMessage.SubjectMaxLength).WithName("Konu");
        RuleFor(x => x.Message)
            .NotEmpty().WithName("Mesaj")
            .MinimumLength(ContactMessage.MessageMinLength).WithMessage($"Mesaj en az {ContactMessage.MessageMinLength} karakter olmalı.")
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
