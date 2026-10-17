using FluentValidation;
using Portfolio.Application.Common.Validation;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Projects;

/// <summary>Girişte (ProjectInput) yer almayan form alanlarının adları.</summary>
public static class ProjectFormFields
{
    public const string Cover = "Cover";
}

public enum ProjectStatusFilter
{
    All,
    Published,
    Draft
}

public sealed record ProjectQuery(
    string? Search = null,
    Guid? CategoryId = null,
    ProjectStatusFilter Status = ProjectStatusFilter.All,
    int Page = 1,
    int PageSize = 10);

public sealed record ProjectListItem(
    Guid Id,
    string Title,
    string Slug,
    string? CategoryName,
    string? CoverImageUrl,
    bool IsPublished,
    bool IsFeatured,
    int SkillCount,
    DateTimeOffset LastModified);

public sealed class ProjectInput
{
    public string Title { get; set; } = string.Empty;

    public string? Slug { get; set; }

    public string Summary { get; set; } = string.Empty;

    public string? Body { get; set; }

    public Guid? CategoryId { get; set; }

    public IList<Guid> SkillIds { get; init; } = [];

    public string? RepositoryUrl { get; set; }

    public string? LiveUrl { get; set; }

    public DateOnly? StartedOn { get; set; }

    public DateOnly? CompletedOn { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsPublished { get; set; }

    public int SortOrder { get; set; }

    public string? TitleEn { get; set; }

    public string? SummaryEn { get; set; }

    public string? BodyEn { get; set; }
}

/// <summary>Düzenleme formunun ihtiyaç duyduğu, girişte olmayan ek bilgiler.</summary>
public sealed record ProjectEditModel(ProjectInput Input, string? CoverImageUrl, DateTimeOffset? PublishedAt);

public sealed class ProjectInputValidator : AbstractValidator<ProjectInput>
{
    public ProjectInputValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(Project.TitleMaxLength).WithName("Başlık");
        RuleFor(x => x.Slug).OptionalSlug().WithName("Slug");
        RuleFor(x => x.Summary).NotEmpty().MaximumLength(Project.SummaryMaxLength).WithName("Özet");
        RuleFor(x => x.Body).MaximumLength(Project.BodyMaxLength).WithName("İçerik");
        RuleFor(x => x.RepositoryUrl).HttpUrl().WithName("Kaynak kod adresi");
        RuleFor(x => x.LiveUrl).HttpUrl().WithName("Canlı adres");
        RuleFor(x => x.TitleEn).MaximumLength(Project.TitleMaxLength).WithName("Başlık (EN)");
        RuleFor(x => x.SummaryEn).MaximumLength(Project.SummaryMaxLength).WithName("Özet (EN)");
        RuleFor(x => x.BodyEn).MaximumLength(Project.BodyMaxLength).WithName("İçerik (EN)");
        RuleFor(x => x.CompletedOn)
            .GreaterThanOrEqualTo(x => x.StartedOn)
            .When(x => x.StartedOn is not null && x.CompletedOn is not null)
            .WithMessage("Bitiş tarihi başlangıç tarihinden önce olamaz.");
    }
}
