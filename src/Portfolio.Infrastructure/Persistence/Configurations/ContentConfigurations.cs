using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Domain.Common;
using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Name).HasMaxLength(Category.NameMaxLength).IsRequired();
        builder.Property(c => c.Slug).HasMaxLength(Slug.MaxLength).IsRequired();
        builder.Property(c => c.NameEn).HasMaxLength(Category.NameMaxLength);
        builder.HasIndex(c => c.Slug).IsUnique();
    }
}

internal sealed class SkillConfiguration : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> builder)
    {
        builder.ToTable("skills");
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Name).HasMaxLength(Skill.NameMaxLength).IsRequired();
        builder.Property(s => s.Group).HasMaxLength(Skill.GroupMaxLength);
        builder.HasIndex(s => s.Name).IsUnique();
        builder.ToTable(t => t.HasCheckConstraint("ck_skills_level", $"level BETWEEN {Skill.MinLevel} AND {Skill.MaxLevel}"));
    }
}

internal sealed class ExperienceConfiguration : IEntityTypeConfiguration<Experience>
{
    public void Configure(EntityTypeBuilder<Experience> builder)
    {
        builder.ToTable("experiences");
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.Title).HasMaxLength(Experience.TitleMaxLength).IsRequired();
        builder.Property(e => e.Organization).HasMaxLength(Experience.OrganizationMaxLength).IsRequired();
        builder.Property(e => e.Location).HasMaxLength(Experience.LocationMaxLength);
        builder.Property(e => e.Description).HasMaxLength(Experience.DescriptionMaxLength);
        builder.Property(e => e.Url).HasMaxLength(Guard.UrlMaxLength);
        builder.Property(e => e.TitleEn).HasMaxLength(Experience.TitleMaxLength);
        builder.Property(e => e.DescriptionEn).HasMaxLength(Experience.DescriptionMaxLength);
        builder.Ignore(e => e.IsCurrent);
        builder.ToTable(t => t.HasCheckConstraint("ck_experiences_dates", "end_date IS NULL OR end_date >= start_date"));
    }
}

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Title).HasMaxLength(Project.TitleMaxLength).IsRequired();
        builder.Property(p => p.Slug).HasMaxLength(Slug.MaxLength).IsRequired();
        builder.Property(p => p.Summary).HasMaxLength(Project.SummaryMaxLength).IsRequired();
        builder.Property(p => p.Body).HasMaxLength(Project.BodyMaxLength);
        builder.Property(p => p.CoverImageUrl).HasMaxLength(Guard.UrlMaxLength);
        builder.Property(p => p.RepositoryUrl).HasMaxLength(Guard.UrlMaxLength);
        builder.Property(p => p.LiveUrl).HasMaxLength(Guard.UrlMaxLength);
        builder.Property(p => p.TitleEn).HasMaxLength(Project.TitleMaxLength);
        builder.Property(p => p.SummaryEn).HasMaxLength(Project.SummaryMaxLength);
        builder.Property(p => p.BodyEn).HasMaxLength(Project.BodyMaxLength);

        builder.HasIndex(p => p.Slug).IsUnique();
        builder.HasIndex(p => new { p.IsPublished, p.SortOrder });

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Projects)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(p => p.Skills)
            .WithMany(s => s.Projects)
            .UsingEntity(join => join.ToTable("project_skills"));

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_projects_dates", "completed_on IS NULL OR started_on IS NULL OR completed_on >= started_on"));
    }
}
