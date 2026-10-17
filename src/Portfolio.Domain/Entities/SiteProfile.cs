using Portfolio.Domain.Common;

namespace Portfolio.Domain.Entities;

/// <summary>
/// Sitenin sahibine ait kimlik bilgileri: ad, unvan, kısa biyografi ve iletişim bağlantıları.
/// Sistemde tek bir kayıt olarak tutulur ve admin panelinden düzenlenir.
/// </summary>
public sealed class SiteProfile : BaseEntity
{
    public const int FullNameMaxLength = 100;
    public const int HeadlineMaxLength = 160;
    public const int BioMaxLength = 2000;
    public const int UrlMaxLength = Guard.UrlMaxLength;

    // EF Core için
    private SiteProfile()
    {
        FullName = string.Empty;
        Headline = string.Empty;
        Bio = string.Empty;
    }

    public SiteProfile(string fullName, string headline, string bio) : this()
    {
        UpdateIdentity(fullName, headline, bio);
    }

    public string FullName { get; private set; }

    public string Headline { get; private set; }

    public string Bio { get; private set; }

    public string? Location { get; private set; }

    public string? Email { get; private set; }

    public string? GitHubUrl { get; private set; }

    public string? LinkedInUrl { get; private set; }

    public bool IsOpenToWork { get; private set; }

    public void UpdateIdentity(string fullName, string headline, string? bio)
    {
        FullName = Guard.Required(fullName, nameof(FullName), FullNameMaxLength);
        Headline = Guard.Required(headline, nameof(Headline), HeadlineMaxLength);
        Bio = Guard.Optional(bio, nameof(Bio), BioMaxLength) ?? string.Empty;
    }

    public void UpdateContact(string? location, string? email, string? gitHubUrl, string? linkedInUrl)
    {
        Location = Guard.Optional(location, nameof(Location), FullNameMaxLength);
        Email = Guard.Optional(email, nameof(Email), UrlMaxLength);
        GitHubUrl = Guard.OptionalUrl(gitHubUrl, nameof(GitHubUrl));
        LinkedInUrl = Guard.OptionalUrl(linkedInUrl, nameof(LinkedInUrl));
    }

    public void SetOpenToWork(bool isOpen) => IsOpenToWork = isOpen;
}
