namespace Portfolio.Application.Common.Interfaces;

public sealed record GitHubRepository(
    string Name,
    string FullName,
    string? Description,
    string Url,
    string? Language,
    int Stars,
    int Forks,
    DateTimeOffset PushedAt,
    IReadOnlyList<string> Topics);

/// <summary>Bir dilin, kullanıcının (çatallanmamış) depoları içindeki payı.</summary>
public sealed record LanguageShare(string Name, int RepositoryCount, double Percent);

public sealed record GitHubProfile(
    string Login,
    string Url,
    int PublicRepositories,
    int Followers,
    int TotalStars,
    IReadOnlyList<GitHubRepository> RecentRepositories,
    IReadOnlyList<LanguageShare> Languages);

/// <summary>
/// GitHub'dan herkese açık bilgileri okur. Hata durumunda istisna fırlatmaz, null döner;
/// çağıran sayfa ilgili bölümü gizler.
/// </summary>
public interface IGitHubService
{
    Task<GitHubProfile?> GetProfileAsync(string username, CancellationToken cancellationToken = default);

    Task<GitHubRepository?> GetRepositoryAsync(string owner, string name, CancellationToken cancellationToken = default);
}

/// <summary>"https://github.com/kullanici[/depo]" adreslerini ayrıştırır.</summary>
public static class GitHubUrl
{
    public static bool TryParseUser(string? url, out string user)
    {
        user = string.Empty;
        if (!TrySegments(url, out var segments) || segments.Length < 1)
        {
            return false;
        }

        user = segments[0];
        return true;
    }

    public static bool TryParseRepository(string? url, out string owner, out string name)
    {
        owner = name = string.Empty;
        if (!TrySegments(url, out var segments) || segments.Length < 2)
        {
            return false;
        }

        owner = segments[0];
        name = segments[1].EndsWith(".git", StringComparison.OrdinalIgnoreCase) ? segments[1][..^4] : segments[1];
        return name.Length > 0;
    }

    private static bool TrySegments(string? url, out string[] segments)
    {
        segments = [];
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (!uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) &&
             !uri.Host.Equals("www.github.com", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.All(s => s.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.'));
    }
}
