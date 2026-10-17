namespace Portfolio.Application.SiteProfiles;

public sealed record SiteProfileDto(
    string FullName,
    string Headline,
    string Bio,
    string? Location,
    string? Email,
    string? GitHubUrl,
    string? LinkedInUrl,
    bool IsOpenToWork)
{
    /// <summary>Veritabanında henüz profil yokken gösterilecek yer tutucu.</summary>
    public static SiteProfileDto Empty { get; } =
        new("Portfolyo", "Yazılım, tasarım ve daha fazlası", string.Empty, null, null, null, null, false);

    public string Initials =>
        string.Concat(FullName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(part => char.ToUpperInvariant(part[0])));
}
