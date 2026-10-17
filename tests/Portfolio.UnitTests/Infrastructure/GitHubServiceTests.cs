using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Infrastructure.GitHub;

namespace Portfolio.UnitTests.Infrastructure;

public sealed class GitHubServiceTests : IDisposable
{
    private const string User = """{ "login": "ada", "html_url": "https://github.com/ada", "public_repos": 4, "followers": 12 }""";

    private const string Repos = """
        [
          { "name": "portfolio", "full_name": "ada/portfolio", "description": "Site", "html_url": "https://github.com/ada/portfolio",
            "language": "C#", "stargazers_count": 5, "forks_count": 1, "pushed_at": "2026-10-10T10:00:00Z", "topics": ["dotnet"], "fork": false, "archived": false },
          { "name": "notes", "full_name": "ada/notes", "description": null, "html_url": "https://github.com/ada/notes",
            "language": "TypeScript", "stargazers_count": 2, "forks_count": 0, "pushed_at": "2026-10-15T10:00:00Z", "fork": false, "archived": false },
          { "name": "api", "full_name": "ada/api", "description": "API", "html_url": "https://github.com/ada/api",
            "language": "C#", "stargazers_count": 1, "forks_count": 0, "pushed_at": "2026-09-01T10:00:00Z", "fork": false, "archived": false },
          { "name": "forked", "full_name": "ada/forked", "description": "fork", "html_url": "https://github.com/ada/forked",
            "language": "Go", "stargazers_count": 999, "forks_count": 0, "pushed_at": "2026-10-16T10:00:00Z", "fork": true, "archived": false }
        ]
        """;

    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    [Fact]
    public async Task Profile_ExcludesForks_SortsByPush_AndComputesLanguageShares()
    {
        var handler = new StubHandler(_ => Ok(User), _ => Ok(Repos));

        var profile = await Create(handler).GetProfileAsync("ada");

        Assert.NotNull(profile);
        Assert.Equal(8, profile.TotalStars); // çatal (999) sayılmaz
        Assert.Equal(["notes", "portfolio", "api"], profile.RecentRepositories.Select(r => r.Name));
        var csharp = profile.Languages.Single(l => l.Name == "C#");
        Assert.Equal(2, csharp.RepositoryCount);
        Assert.Equal(66.7, csharp.Percent);
    }

    [Fact]
    public async Task Profile_IsCached()
    {
        var handler = new StubHandler(_ => Ok(User), _ => Ok(Repos));
        var service = Create(handler);

        await service.GetProfileAsync("ada");
        await service.GetProfileAsync("ADA");

        Assert.Equal(2, handler.Calls); // ilk istekte kullanıcı + depolar; ikincisi önbellekten
    }

    [Fact]
    public async Task Failure_ReturnsNull_InsteadOfThrowing()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        Assert.Null(await Create(handler).GetRepositoryAsync("ada", "portfolio"));
    }

    [Fact]
    public async Task Failure_FallsBackToLastGoodValue()
    {
        var options = new GitHubOptions { CacheDuration = TimeSpan.Zero };
        var healthy = new StubHandler(_ => Ok("""{ "name": "portfolio", "full_name": "ada/portfolio", "html_url": "https://github.com/ada/portfolio", "stargazers_count": 5, "forks_count": 1, "fork": false, "archived": false }"""));
        await Create(healthy, options).GetRepositoryAsync("ada", "portfolio");

        var broken = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var repo = await Create(broken, options).GetRepositoryAsync("ada", "portfolio");

        Assert.Equal(5, repo?.Stars);
    }

    [Theory]
    [InlineData("https://github.com/ada/portfolio", true, "ada", "portfolio")]
    [InlineData("https://github.com/ada/portfolio.git", true, "ada", "portfolio")]
    [InlineData("https://www.github.com/ada/site/tree/main", true, "ada", "site")]
    [InlineData("https://github.com/ada", false, "", "")]
    [InlineData("https://gitlab.com/ada/portfolio", false, "", "")]
    [InlineData("https://github.com/ada/<script>", false, "", "")]
    public void ParseRepositoryUrl(string url, bool ok, string owner, string name)
    {
        Assert.Equal(ok, GitHubUrl.TryParseRepository(url, out var o, out var n));
        Assert.Equal(owner, o);
        Assert.Equal(name, n);
    }

    public void Dispose() => _cache.Dispose();

    private GitHubService Create(StubHandler handler, GitHubOptions? options = null) => new(
        new HttpClient(handler) { BaseAddress = new Uri("https://api.github.com/") },
        _cache,
        Options.Create(options ?? new GitHubOptions()),
        NullLogger<GitHubService>.Instance);

    private static HttpResponseMessage Ok(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    /// <summary>Sırayla verilen yanıtları döndürür; son yanıt sonraki tüm istekler için tekrarlanır.</summary>
    private sealed class StubHandler(params Func<HttpRequestMessage, HttpResponseMessage>[] responses) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var respond = responses[Math.Min(Calls, responses.Length - 1)];
            Calls++;
            return Task.FromResult(respond(request));
        }
    }
}
