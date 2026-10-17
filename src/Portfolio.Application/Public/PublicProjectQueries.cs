using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Application.Common.Models;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Public;

/// <summary>
/// Ziyaretçi sitesinin okuma sorguları. Yalnızca yayımlanmış projeleri döndürür;
/// taslaklar sadece admin önizlemesi için (<c>includeDrafts</c>) açılır.
/// </summary>
public interface IPublicProjectQueries
{
    Task<ProjectCatalog> GetCatalogAsync(ProjectCatalogQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectCard>> GetFeaturedAsync(int count, CancellationToken cancellationToken = default);

    Task<ProjectDetail?> GetBySlugAsync(string slug, bool includeDrafts, CancellationToken cancellationToken = default);
}

internal sealed class PublicProjectQueries(IApplicationDbContext db, IMarkdownRenderer markdown) : IPublicProjectQueries
{
    private const int RelatedCount = 3;

    public async Task<ProjectCatalog> GetCatalogAsync(ProjectCatalogQuery query, CancellationToken cancellationToken = default)
    {
        var projects = Published();

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            projects = projects.Where(p => p.Category != null && p.Category.Slug == query.Category);
        }

        if (!string.IsNullOrWhiteSpace(query.Technology))
        {
            projects = projects.Where(p => p.Skills.Any(s => s.Name == query.Technology));
        }

        var page = await ToCards(OrderByDisplay(projects)).ToPagedListAsync(query.Page, query.PageSize, cancellationToken);

        var categories = await db.Categories
            .AsNoTracking()
            .Select(c => new { c.Slug, c.Name, c.SortOrder, Count = c.Projects.Count(p => p.IsPublished) })
            .Where(c => c.Count > 0)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new FilterOption(c.Slug, c.Name, c.Count))
            .ToListAsync(cancellationToken);

        var technologies = await db.Skills
            .AsNoTracking()
            .Select(s => new { s.Name, Count = s.Projects.Count(p => p.IsPublished) })
            .Where(s => s.Count > 0)
            .OrderByDescending(s => s.Count).ThenBy(s => s.Name)
            .Select(s => new FilterOption(s.Name, s.Name, s.Count))
            .ToListAsync(cancellationToken);

        return new ProjectCatalog(page, categories, technologies, query);
    }

    public async Task<IReadOnlyList<ProjectCard>> GetFeaturedAsync(int count, CancellationToken cancellationToken = default) =>
        // Öne çıkanlar önce gelir; hiç öne çıkan yoksa en güncel projeler gösterilir.
        await ToCards(ThenByDisplay(Published().OrderByDescending(p => p.IsFeatured)))
            .Take(count)
            .ToListAsync(cancellationToken);

    public async Task<ProjectDetail?> GetBySlugAsync(string slug, bool includeDrafts, CancellationToken cancellationToken = default)
    {
        var project = await db.Projects
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Skills)
            .FirstOrDefaultAsync(p => p.Slug == slug && (p.IsPublished || includeDrafts), cancellationToken);

        if (project is null)
        {
            return null;
        }

        ProjectLink? previous = null, next = null;
        if (project.IsPublished)
        {
            var sequence = await OrderByDisplay(Published())
                .Select(p => new { p.Id, p.Title, p.Slug })
                .ToListAsync(cancellationToken);
            var index = sequence.FindIndex(p => p.Id == project.Id);
            if (index > 0)
            {
                previous = new ProjectLink(sequence[index - 1].Title, sequence[index - 1].Slug);
            }

            if (index >= 0 && index < sequence.Count - 1)
            {
                next = new ProjectLink(sequence[index + 1].Title, sequence[index + 1].Slug);
            }
        }

        // Aynı kategoridekiler önce, ardından diğerleri.
        var categoryId = project.CategoryId;
        var related = await ToCards(ThenByDisplay(Published()
                .Where(p => p.Id != project.Id)
                .OrderByDescending(p => categoryId != null && p.CategoryId == categoryId)))
            .Take(RelatedCount)
            .ToListAsync(cancellationToken);

        return new ProjectDetail(
            project.Id,
            project.Title,
            project.Slug,
            project.Summary,
            markdown.Render(project.Body),
            project.CoverImageUrl,
            project.RepositoryUrl,
            project.LiveUrl,
            project.StartedOn,
            project.CompletedOn,
            project.Category?.Name,
            project.Category?.Slug,
            project.Skills.OrderBy(s => s.SortOrder).ThenBy(s => s.Name).Select(s => s.Name).ToList(),
            project.IsPublished,
            project.PublishedAt,
            project.UpdatedAt ?? project.CreatedAt,
            previous,
            next,
            related);
    }

    private IQueryable<Project> Published() => db.Projects.AsNoTracking().Where(p => p.IsPublished);

    // Sitedeki standart sıra: admin'in verdiği sıra numarası, sonra en yeni yayın.
    private static IOrderedQueryable<Project> OrderByDisplay(IQueryable<Project> query) =>
        query.OrderBy(p => p.SortOrder).ThenByDescending(p => p.PublishedAt);

    private static IOrderedQueryable<Project> ThenByDisplay(IOrderedQueryable<Project> query) =>
        query.ThenBy(p => p.SortOrder).ThenByDescending(p => p.PublishedAt);

    private static IQueryable<ProjectCard> ToCards(IQueryable<Project> query) =>
        query.Select(p => new ProjectCard(
            p.Id,
            p.Title,
            p.Slug,
            p.Summary,
            p.CoverImageUrl,
            p.Category != null ? p.Category.Name : null,
            p.Category != null ? p.Category.Slug : null,
            p.Skills.OrderBy(s => s.SortOrder).ThenBy(s => s.Name).Select(s => s.Name).ToList(),
            p.IsFeatured,
            p.StartedOn,
            p.CompletedOn));
}
