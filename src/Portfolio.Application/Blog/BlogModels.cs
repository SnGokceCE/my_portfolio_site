using FluentValidation;
using Portfolio.Application.Common.Validation;
using Portfolio.Application.Projects;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Blog;

public sealed record BlogPostQuery(
    string? Search = null,
    ProjectStatusFilter Status = ProjectStatusFilter.All,
    int Page = 1,
    int PageSize = 15);

public sealed record BlogPostListItem(
    Guid Id,
    string Title,
    string Slug,
    string? CoverImageUrl,
    IReadOnlyList<string> Tags,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    int ReadingMinutes,
    DateTimeOffset LastModified);

public sealed class BlogPostInput
{
    public string Title { get; set; } = string.Empty;

    public string? Slug { get; set; }

    public string Excerpt { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    /// <summary>Formda virgülle ayrılmış tek alan: "C#, ASP.NET, Staj".</summary>
    public string? Tags { get; set; }

    public bool IsPublished { get; set; }

    public string Language { get; set; } = BlogPost.Turkish;
}

public sealed record BlogPostEditModel(BlogPostInput Input, string? CoverImageUrl, DateTimeOffset? PublishedAt);

public sealed class BlogPostInputValidator : AbstractValidator<BlogPostInput>
{
    public BlogPostInputValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(BlogPost.TitleMaxLength).WithName("Başlık");
        RuleFor(x => x.Slug).OptionalSlug().WithName("Slug");
        RuleFor(x => x.Excerpt).NotEmpty().MaximumLength(BlogPost.ExcerptMaxLength).WithName("Giriş");
        RuleFor(x => x.Body).NotEmpty().MaximumLength(BlogPost.BodyMaxLength).WithName("İçerik");
        RuleFor(x => x.Language)
            .Must(l => l is BlogPost.Turkish or BlogPost.English)
            .WithMessage("Yazı dili Türkçe ya da İngilizce olmalı.");
        RuleFor(x => x.Tags)
            .Must(t => BlogPost.NormalizeTags([t ?? string.Empty]).Length <= BlogPost.MaxTags)
            .WithMessage($"En fazla {BlogPost.MaxTags} etiket eklenebilir.")
            .Must(t => BlogPost.NormalizeTags([t ?? string.Empty]).All(tag => tag.Length <= BlogPost.TagMaxLength))
            .WithMessage($"Her etiket en fazla {BlogPost.TagMaxLength} karakter olabilir.");
    }
}
