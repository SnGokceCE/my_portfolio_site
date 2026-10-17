using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Domain.Common;
using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Persistence.Configurations;

internal sealed class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> builder)
    {
        builder.ToTable("blog_posts");
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Title).HasMaxLength(BlogPost.TitleMaxLength).IsRequired();
        builder.Property(p => p.Slug).HasMaxLength(Slug.MaxLength).IsRequired();
        builder.Property(p => p.Excerpt).HasMaxLength(BlogPost.ExcerptMaxLength).IsRequired();
        builder.Property(p => p.Body).HasMaxLength(BlogPost.BodyMaxLength).IsRequired();
        builder.Property(p => p.CoverImageUrl).HasMaxLength(Guard.UrlMaxLength);

        // PostgreSQL'de text[] dizisi olarak saklanır; GIN indeksi etikete göre filtrelemeyi hızlandırır.
        builder.Property(p => p.Tags).IsRequired();
        builder.HasIndex(p => p.Tags).HasMethod("gin");

        builder.HasIndex(p => p.Slug).IsUnique();
        builder.HasIndex(p => new { p.IsPublished, p.PublishedAt });
    }
}

internal sealed class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.ToTable("contact_messages");
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Name).HasMaxLength(ContactMessage.NameMaxLength).IsRequired();
        builder.Property(m => m.Email).HasMaxLength(ContactMessage.EmailMaxLength).IsRequired();
        builder.Property(m => m.Subject).HasMaxLength(ContactMessage.SubjectMaxLength);
        builder.Property(m => m.Message).HasMaxLength(ContactMessage.MessageMaxLength).IsRequired();
        builder.Ignore(m => m.IsRead);
        builder.HasIndex(m => new { m.IsSpam, m.CreatedAt });
    }
}
