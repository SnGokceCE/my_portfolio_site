using Portfolio.Domain.Common;

namespace Portfolio.Domain.Entities;

/// <summary>İletişim formundan gelen mesaj. Spam olarak işaretlenenler bildirim göndermez.</summary>
public sealed class ContactMessage : BaseEntity
{
    public const int NameMaxLength = 100;
    public const int EmailMaxLength = 200;
    public const int SubjectMaxLength = 150;
    public const int MessageMinLength = 10;
    public const int MessageMaxLength = 5000;

    private ContactMessage()
    {
        Name = string.Empty;
        Email = string.Empty;
        Message = string.Empty;
    }

    public ContactMessage(string name, string email, string? subject, string message, bool isSpam) : this()
    {
        Name = Guard.Required(name, nameof(Name), NameMaxLength);
        Email = Guard.Required(email, nameof(Email), EmailMaxLength);
        Subject = Guard.Optional(subject, nameof(Subject), SubjectMaxLength);
        Message = Guard.Required(message, nameof(Message), MessageMaxLength);
        IsSpam = isSpam;
    }

    public string Name { get; private set; }

    public string Email { get; private set; }

    public string? Subject { get; private set; }

    public string Message { get; private set; }

    public bool IsSpam { get; private set; }

    public DateTimeOffset? ReadAt { get; private set; }

    public bool IsRead => ReadAt is not null;

    public void MarkRead(DateTimeOffset now) => ReadAt ??= now;

    public void MarkUnread() => ReadAt = null;

    public void SetSpam(bool isSpam) => IsSpam = isSpam;
}
