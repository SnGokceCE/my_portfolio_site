namespace Portfolio.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>Boşsa e-postalar gönderilmez, yalnızca loglanır (SMTP'siz geliştirme için).</summary>
    public string? SmtpHost { get; set; }

    public int SmtpPort { get; set; } = 587;

    public string? Username { get; set; }

    public string? Password { get; set; }

    /// <summary>true: STARTTLS zorunlu (587). Mailpit gibi yerel test sunucularında false.</summary>
    public bool UseStartTls { get; set; } = true;

    public string FromAddress { get; set; } = "no-reply@portfolio.local";

    public string FromName { get; set; } = "Portfolyo";

    public int MaxAttempts { get; set; } = 3;

    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);
}
