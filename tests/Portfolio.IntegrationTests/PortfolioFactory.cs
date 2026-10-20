using System.Collections.Concurrent;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Infrastructure.Email;
using Portfolio.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Portfolio.IntegrationTests;

/// <summary>
/// Uygulamayı bellekte (TestServer) gerçek bir PostgreSQL'e karşı ayağa kaldırır. Veritabanı
/// Testcontainers ile her test çalıştırmasında sıfırdan açılan bir Docker konteyneridir; migration'lar
/// ve başlangıç verisi uygulamanın kendi açılış kodu tarafından uygulanır. Dış bağımlılıklar
/// (SMTP, GitHub, saat) kontrol edilebilir sahteleriyle değiştirilir.
/// </summary>
public sealed class PortfolioFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin@integration.test";
    public const string AdminPassword = "Integration-Test-2026!";

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine").Build();

    private readonly string _uploads = Path.Combine(Path.GetTempPath(), "portfolio-it", Guid.NewGuid().ToString("N"));

    public FakeTimeProvider Clock { get; } = new(DateTimeOffset.UtcNow);

    public RecordingEmailTransport Emails { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _database.GetConnectionString());
        builder.UseSetting("Seed:AdminEmail", AdminEmail);
        builder.UseSetting("Seed:AdminPassword", AdminPassword);
        builder.UseSetting("Analytics:Secret", "integration-tests");
        builder.UseSetting("Storage:UploadsPath", _uploads);
        builder.UseSetting("Contact:NotifyEmail", "owner@integration.test");
        builder.UseSetting("RateLimit:ContactPermitLimit", "1000");

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<TimeProvider>(Clock);
            services.AddSingleton<IEmailTransport>(Emails);
            services.AddSingleton<IGitHubService, OfflineGitHubService>();
        });
    }

    public Task InitializeAsync() => _database.StartAsync();

    // WebApplicationFactory.DisposeAsync ValueTask döndürür; xUnit Task bekler, bu yüzden açık arayüz uygulaması.
    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
        if (Directory.Exists(_uploads))
        {
            Directory.Delete(_uploads, recursive: true);
        }
    }

    /// <summary>Çerezleri saklayan, yönlendirmeleri takip etmeyen istemci (yanıtları tek tek inceleyebilmek için).</summary>
    public HttpClient CreateBrowser() => CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    public async Task<T> WithDatabaseAsync<T>(Func<ApplicationDbContext, Task<T>> action)
    {
        await using var scope = Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }

    public sealed class RecordingEmailTransport : IEmailTransport
    {
        public ConcurrentQueue<EmailMessage> Sent { get; } = new();

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            Sent.Enqueue(message);
            return Task.CompletedTask;
        }
    }

    /// <summary>Testler internete çıkmasın: GitHub her zaman "veri yok" döner.</summary>
    private sealed class OfflineGitHubService : IGitHubService
    {
        public Task<GitHubProfile?> GetProfileAsync(string username, CancellationToken cancellationToken = default) =>
            Task.FromResult<GitHubProfile?>(null);

        public Task<GitHubRepository?> GetRepositoryAsync(string owner, string name, CancellationToken cancellationToken = default) =>
            Task.FromResult<GitHubRepository?>(null);
    }
}

[CollectionDefinition(Name)]
public sealed class WebTestGroup : ICollectionFixture<PortfolioFactory>
{
    public const string Name = "web";
}

/// <summary>HTML yanıtlarını ayrıştırma ve formları gönderme yardımcıları.</summary>
public static class HttpExtensions
{
    private static readonly HtmlParser Parser = new();

    public static async Task<IHtmlDocument> GetDocumentAsync(this HttpClient client, string url)
    {
        using var response = await client.GetAsync(url);
        return await response.ReadDocumentAsync();
    }

    public static async Task<IHtmlDocument> ReadDocumentAsync(this HttpResponseMessage response) =>
        await Parser.ParseDocumentAsync(await response.Content.ReadAsStringAsync());

    public static string AntiforgeryToken(this IHtmlDocument document) =>
        document.QuerySelector("input[name=__RequestVerificationToken]")?.GetAttribute("value")
        ?? throw new InvalidOperationException("Sayfada CSRF jetonu yok.");

    public static Task<HttpResponseMessage> PostFormAsync(this HttpClient client, string url, IDictionary<string, string> fields) =>
        client.PostAsync(url, new FormUrlEncodedContent(fields));

    public static async Task<HttpClient> LoginAsAdminAsync(this PortfolioFactory factory)
    {
        var client = factory.CreateBrowser();
        var login = await client.GetDocumentAsync("/account/login");
        var response = await client.PostFormAsync("/account/login", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = login.AntiforgeryToken(),
            ["Email"] = PortfolioFactory.AdminEmail,
            ["Password"] = PortfolioFactory.AdminPassword
        });

        if (response.StatusCode != System.Net.HttpStatusCode.Redirect)
        {
            throw new InvalidOperationException($"Admin girişi başarısız: {response.StatusCode}");
        }

        return client;
    }
}
