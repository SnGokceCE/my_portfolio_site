using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Application.Analytics;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Application.Contact;
using Portfolio.Infrastructure.Analytics;
using Portfolio.Infrastructure.Content;
using Portfolio.Infrastructure.Email;
using Portfolio.Infrastructure.GitHub;
using Portfolio.Infrastructure.Identity;
using Portfolio.Infrastructure.Persistence;
using Portfolio.Infrastructure.Persistence.Interceptors;
using Portfolio.Infrastructure.Persistence.Seeding;
using Portfolio.Infrastructure.Security;
using Portfolio.Infrastructure.Storage;

namespace Portfolio.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default tanımlı değil.");

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) => options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>()));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 10;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddMemoryCache();
        services.AddSingleton<IMarkdownRenderer, MarkdownRenderer>();

        // GitHub: tipli HttpClient + standart dayanıklılık (zaman aşımı, üstel yeniden deneme, devre kesici).
        services.Configure<GitHubOptions>(configuration.GetSection(GitHubOptions.SectionName));
        services.AddHttpClient<IGitHubService, GitHubService>(client =>
            {
                client.BaseAddress = new Uri("https://api.github.com/");
                client.DefaultRequestHeaders.UserAgent.ParseAdd("portfolio-site/1.0");
                client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
                client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
                if (configuration[$"{GitHubOptions.SectionName}:{nameof(GitHubOptions.Token)}"] is { Length: > 0 } token)
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }
            })
            .AddStandardResilienceHandler(resilience =>
            {
                resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(4);
                resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);
                resilience.Retry.MaxRetryAttempts = 2;
            });

        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.AddSingleton<LocalFileStorage>();
        services.AddSingleton<IFileStorage>(sp => sp.GetRequiredService<LocalFileStorage>());

        // E-posta: istek kuyruğa yazar, arka plan servisi gönderir.
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddSingleton<ChannelEmailOutbox>();
        services.AddSingleton<IEmailOutbox>(sp => sp.GetRequiredService<ChannelEmailOutbox>());
        if (string.IsNullOrWhiteSpace(configuration[$"{EmailOptions.SectionName}:{nameof(EmailOptions.SmtpHost)}"]))
        {
            services.AddSingleton<IEmailTransport, LogEmailTransport>();
        }
        else
        {
            services.AddSingleton<IEmailTransport, SmtpEmailTransport>();
        }

        services.AddHostedService<EmailDispatcher>();

        // Ziyaret istatistikleri: istek kanala yazar, arka plan servisi anonimleştirip toplu kaydeder.
        services.Configure<AnalyticsOptions>(configuration.GetSection(AnalyticsOptions.SectionName));
        services.AddSingleton<ChannelPageViewRecorder>();
        services.AddSingleton<IPageViewRecorder>(sp => sp.GetRequiredService<ChannelPageViewRecorder>());
        services.AddHostedService<PageViewWriter>();

        services.Configure<ContactOptions>(configuration.GetSection(ContactOptions.SectionName));
        // Anahtarlar kalıcı saklanmazsa her yeniden başlatmada form jetonları ve oturum çerezleri geçersiz olur.
        var dataProtection = services.AddDataProtection().SetApplicationName("Portfolio");
        if (configuration["DataProtection:KeysPath"] is { Length: > 0 } keysPath)
        {
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        }

        services.AddSingleton<IFormTimeTrap, DataProtectionFormTimeTrap>();

        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));
        services.AddScoped<DatabaseInitializer>();

        services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>("database");

        return services;
    }
}
