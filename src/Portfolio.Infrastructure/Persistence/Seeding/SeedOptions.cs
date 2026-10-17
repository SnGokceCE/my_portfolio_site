namespace Portfolio.Infrastructure.Persistence.Seeding;

/// <summary>
/// "Seed" yapılandırma bölümü. Admin bilgileri koda gömülmez; geliştirmede
/// appsettings.Development.json, canlıda ortam değişkenleri üzerinden gelir.
/// </summary>
public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public string? AdminEmail { get; init; }

    public string? AdminPassword { get; init; }
}
