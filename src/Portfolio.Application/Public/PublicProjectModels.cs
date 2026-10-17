using Portfolio.Application.Common.Interfaces;
using Portfolio.Application.Common.Models;

namespace Portfolio.Application.Public;

public sealed record ProjectCard(
    Guid Id,
    string Title,
    string Slug,
    string Summary,
    string? CoverImageUrl,
    string? CategoryName,
    string? CategorySlug,
    IReadOnlyList<string> Skills,
    bool IsFeatured,
    DateOnly? StartedOn,
    DateOnly? CompletedOn);

/// <summary>Filtre çubuğundaki bir seçenek: URL'deki değeri, görünen adı ve kaç projede geçtiği.</summary>
public sealed record FilterOption(string Value, string Label, int Count);

public sealed record ProjectCatalogQuery(string? Category = null, string? Technology = null, int Page = 1, int PageSize = 9)
{
    public bool IsFiltered => !string.IsNullOrWhiteSpace(Category) || !string.IsNullOrWhiteSpace(Technology);
}

public sealed record ProjectCatalog(
    PagedList<ProjectCard> Projects,
    IReadOnlyList<FilterOption> Categories,
    IReadOnlyList<FilterOption> Technologies,
    ProjectCatalogQuery Query);

public sealed record ProjectLink(string Title, string Slug);

public sealed record ProjectDetail(
    Guid Id,
    string Title,
    string Slug,
    string Summary,
    RenderedMarkdown Body,
    string? CoverImageUrl,
    string? RepositoryUrl,
    string? LiveUrl,
    DateOnly? StartedOn,
    DateOnly? CompletedOn,
    string? CategoryName,
    string? CategorySlug,
    IReadOnlyList<string> Skills,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    DateTimeOffset LastModified,
    ProjectLink? Previous,
    ProjectLink? Next,
    IReadOnlyList<ProjectCard> Related);
