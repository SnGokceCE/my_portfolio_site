using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portfolio.Application.Common.Interfaces;

namespace Portfolio.Infrastructure.GitHub;

public sealed class GitHubOptions
{
    public const string SectionName = "GitHub";

    /// <summary>İsteğe bağlı kişisel erişim jetonu. Yoksa saatte 60 istek sınırı geçerlidir (önbellekle yeterli).</summary>
    public string? Token { get; set; }

    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>Hata sonrası GitHub'ı tekrar denemeden önce beklenen süre.</summary>
    public TimeSpan FailureBackoff { get; set; } = TimeSpan.FromMinutes(5);

    public int RecentRepositoryCount { get; set; } = 6;
}

/// <summary>
/// GitHub REST API istemcisi. Üç katmanlı dayanıklılık:
/// 1) HttpClient'a eklenen standart resilience (zaman aşımı, yeniden deneme, devre kesici),
/// 2) başarılı yanıtların önbelleklenmesi, 3) hata olursa son başarılı verinin ("bayat") gösterilmesi.
/// </summary>
public sealed partial class GitHubService(
    HttpClient http,
    IMemoryCache cache,
    IOptions<GitHubOptions> options,
    ILogger<GitHubService> logger) : IGitHubService
{
    private static readonly TimeSpan StaleDuration = TimeSpan.FromDays(7);

    public Task<GitHubProfile?> GetProfileAsync(string username, CancellationToken cancellationToken = default) =>
        GetCachedAsync($"github:profile:{username.ToUpperInvariant()}", ct => FetchProfileAsync(username, ct), cancellationToken);

    public Task<GitHubRepository?> GetRepositoryAsync(string owner, string name, CancellationToken cancellationToken = default) =>
        GetCachedAsync(
            $"github:repo:{owner.ToUpperInvariant()}/{name.ToUpperInvariant()}",
            async ct => Map(await http.GetFromJsonAsync<RepoDto>($"repos/{owner}/{name}", ct)),
            cancellationToken);

    private async Task<T?> GetCachedAsync<T>(string key, Func<CancellationToken, Task<T?>> fetch, CancellationToken cancellationToken)
        where T : class
    {
        if (cache.TryGetValue(key, out T? fresh))
        {
            return fresh;
        }

        var staleKey = key + ":stale";
        var failedKey = key + ":failed";
        if (cache.TryGetValue(failedKey, out _))
        {
            return cache.Get<T>(staleKey);
        }

        try
        {
            var value = await fetch(cancellationToken);
            if (value is not null)
            {
                if (options.Value.CacheDuration > TimeSpan.Zero)
                {
                    cache.Set(key, value, options.Value.CacheDuration);
                }

                cache.Set(staleKey, value, StaleDuration);
            }

            return value;
        }
#pragma warning disable CA1031 // GitHub kesintisi sayfayı bozmamalı; her hata loglanıp yutulur.
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
#pragma warning restore CA1031
        {
            LogFetchFailed(logger, ex, key);
            cache.Set(failedKey, true, options.Value.FailureBackoff);
            return cache.Get<T>(staleKey);
        }
    }

    private async Task<GitHubProfile?> FetchProfileAsync(string username, CancellationToken cancellationToken)
    {
        var user = await http.GetFromJsonAsync<UserDto>($"users/{username}", cancellationToken);
        if (user is null)
        {
            return null;
        }

        var repos = await http.GetFromJsonAsync<List<RepoDto>>(
            $"users/{username}/repos?type=owner&sort=pushed&per_page=100", cancellationToken) ?? [];

        var own = repos.Where(r => !r.Fork && !r.Archived).Select(Map).OfType<GitHubRepository>().ToList();
        var withLanguage = own.Where(r => r.Language is not null).ToList();
        var languages = withLanguage
            .GroupBy(r => r.Language!)
            .Select(g => new LanguageShare(g.Key, g.Count(), Math.Round(100.0 * g.Count() / withLanguage.Count, 1)))
            .OrderByDescending(l => l.RepositoryCount).ThenBy(l => l.Name, StringComparer.Ordinal)
            .Take(6)
            .ToList();

        return new GitHubProfile(
            user.Login,
            user.HtmlUrl,
            user.PublicRepos,
            user.Followers,
            own.Sum(r => r.Stars),
            own.OrderByDescending(r => r.PushedAt).Take(options.Value.RecentRepositoryCount).ToList(),
            languages);
    }

    private static GitHubRepository? Map(RepoDto? dto) => dto is null
        ? null
        : new GitHubRepository(
            dto.Name, dto.FullName, dto.Description, dto.HtmlUrl, dto.Language,
            dto.StargazersCount, dto.ForksCount, dto.PushedAt ?? DateTimeOffset.MinValue, dto.Topics ?? []);

    [LoggerMessage(Level = LogLevel.Warning, Message = "GitHub verisi alınamadı ({Key}); varsa önbellekteki eski veri gösterilecek.")]
    private static partial void LogFetchFailed(ILogger logger, Exception exception, string key);

    private sealed record UserDto(
        [property: JsonPropertyName("login")] string Login,
        [property: JsonPropertyName("html_url")] string HtmlUrl,
        [property: JsonPropertyName("public_repos")] int PublicRepos,
        [property: JsonPropertyName("followers")] int Followers);

    private sealed record RepoDto(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("full_name")] string FullName,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("html_url")] string HtmlUrl,
        [property: JsonPropertyName("language")] string? Language,
        [property: JsonPropertyName("stargazers_count")] int StargazersCount,
        [property: JsonPropertyName("forks_count")] int ForksCount,
        [property: JsonPropertyName("pushed_at")] DateTimeOffset? PushedAt,
        [property: JsonPropertyName("topics")] List<string>? Topics,
        [property: JsonPropertyName("fork")] bool Fork,
        [property: JsonPropertyName("archived")] bool Archived);
}
