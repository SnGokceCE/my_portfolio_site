using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portfolio.Application.Common.Security;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Identity;

namespace Portfolio.Infrastructure.Persistence.Seeding;

/// <summary>
/// Uygulama açılırken bekleyen migration'ları uygular ve ilk verileri
/// (Admin rolü, admin kullanıcı, varsayılan profil) yoksa oluşturur.
/// </summary>
public sealed partial class DatabaseInitializer(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IOptions<SeedOptions> seedOptions,
    ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await db.Database.MigrateAsync(cancellationToken);

        await SeedAdminAsync();
        await SeedProfileAsync(cancellationToken);
    }

    private async Task SeedAdminAsync()
    {
        if (!await roleManager.RoleExistsAsync(Roles.Admin))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(Roles.Admin));
        }

        var options = seedOptions.Value;
        if (string.IsNullOrWhiteSpace(options.AdminEmail) || string.IsNullOrWhiteSpace(options.AdminPassword))
        {
            LogAdminSeedSkipped(logger);
            return;
        }

        if (await userManager.FindByEmailAsync(options.AdminEmail) is not null)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = options.AdminEmail,
            Email = options.AdminEmail,
            EmailConfirmed = true,
            DisplayName = "Admin"
        };

        var result = await userManager.CreateAsync(admin, options.AdminPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Admin kullanıcı oluşturulamadı: {errors}");
        }

        await userManager.AddToRoleAsync(admin, Roles.Admin);
        LogAdminCreated(logger, options.AdminEmail);
    }

    private async Task SeedProfileAsync(CancellationToken cancellationToken)
    {
        if (await db.SiteProfiles.AnyAsync(cancellationToken))
        {
            return;
        }

        var profile = new SiteProfile(
            "Ad Soyad",
            "Yazılım geliştirici · Tasarım meraklısı · Sürekli öğrenen",
            "Merhaba! Burası benim dijital vitrinim. Üzerinde çalıştığım projeleri, " +
            "deneyimlerimi ve öğrendiklerimi burada paylaşıyorum. Bu metni admin panelinden değiştirebilirsin.");
        profile.UpdateContact("Türkiye", null, "https://github.com", null);
        profile.SetOpenToWork(true);

        db.SiteProfiles.Add(profile);
        await db.SaveChangesAsync(cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Seed:AdminEmail / Seed:AdminPassword tanımlı değil; admin kullanıcı oluşturulmadı.")]
    private static partial void LogAdminSeedSkipped(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Admin kullanıcı oluşturuldu: {Email}")]
    private static partial void LogAdminCreated(ILogger logger, string email);
}
