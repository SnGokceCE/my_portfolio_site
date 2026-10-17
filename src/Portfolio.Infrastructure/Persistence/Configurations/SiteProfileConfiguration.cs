using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Persistence.Configurations;

internal sealed class SiteProfileConfiguration : IEntityTypeConfiguration<SiteProfile>
{
    public void Configure(EntityTypeBuilder<SiteProfile> builder)
    {
        builder.ToTable("site_profiles");

        builder.Property(p => p.FullName).HasMaxLength(SiteProfile.FullNameMaxLength).IsRequired();
        builder.Property(p => p.Headline).HasMaxLength(SiteProfile.HeadlineMaxLength).IsRequired();
        builder.Property(p => p.Bio).HasMaxLength(SiteProfile.BioMaxLength);
        builder.Property(p => p.Location).HasMaxLength(SiteProfile.FullNameMaxLength);
        builder.Property(p => p.Email).HasMaxLength(SiteProfile.UrlMaxLength);
        builder.Property(p => p.GitHubUrl).HasMaxLength(SiteProfile.UrlMaxLength);
        builder.Property(p => p.LinkedInUrl).HasMaxLength(SiteProfile.UrlMaxLength);
        builder.Property(p => p.HeadlineEn).HasMaxLength(SiteProfile.HeadlineMaxLength);
        builder.Property(p => p.BioEn).HasMaxLength(SiteProfile.BioMaxLength);
    }
}
