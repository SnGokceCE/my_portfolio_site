using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Application.Contact;
using Portfolio.Infrastructure.Content;
using Portfolio.Infrastructure.Email;
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
