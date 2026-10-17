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
    public const int UrlMaxLength = 300;

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

    public void UpdateIdentity(string fullName, string headline, string bio)
    {
        FullName = Require(fullName, nameof(FullName), FullNameMaxLength);
        Headline = Require(headline, nameof(Headline), HeadlineMaxLength);
        Bio = Limit(bio.Trim(), nameof(Bio), BioMaxLength);
    }

    public void UpdateContact(string? location, string? email, string? gitHubUrl, string? linkedInUrl)
    {
        Location = Optional(location, nameof(Location), FullNameMaxLength);
        Email = Optional(email, nameof(Email), UrlMaxLength);
        GitHubUrl = OptionalUrl(gitHubUrl, nameof(GitHubUrl));
        LinkedInUrl = OptionalUrl(linkedInUrl, nameof(LinkedInUrl));
    }

    public void SetOpenToWork(bool isOpen) => IsOpenToWork = isOpen;

    private static string Require(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{field} boş olamaz.");
        }

        return Limit(value.Trim(), field, maxLength);
    }

    private static string? Optional(string? value, string field, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null : Limit(value.Trim(), field, maxLength);

    private static string? OptionalUrl(string? value, string field)
    {
        var url = Optional(value, field, UrlMaxLength);
        if (url is null)
        {
            return null;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            throw new DomainException($"{field} geçerli bir http(s) adresi olmalı.");
        }

        return url;
    }

    private static string Limit(string value, string field, int maxLength) =>
        value.Length > maxLength
            ? throw new DomainException($"{field} en fazla {maxLength} karakter olabilir.")
            : value;
}
