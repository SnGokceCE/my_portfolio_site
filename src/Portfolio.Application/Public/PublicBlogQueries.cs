using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Application.Common.Models;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Public;

public sealed record PostCard(
    string Title,
    string Slug,
    string Excerpt,
    string? CoverImageUrl,
    IReadOnlyList<string> Tags,
    DateTimeOffset PublishedAt,
    int ReadingMinutes,
    string Language);

public sealed record BlogCatalogQuery(string? Tag = null, int Page = 1, int PageSize = 8);

public sealed record BlogCatalog(PagedList<PostCard> Posts, IReadOnlyList<FilterOption> Tags, BlogCatalogQuery Query);

public sealed record PostDetail(
    Guid Id,
    string Title,
    string Slug,
    string Excerpt,
    RenderedMarkdown Body,
    string? CoverImageUrl,
    IReadOnlyList<string> Tags,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    DateTimeOffset LastModified,
    ProjectLink? Previous,
    ProjectLink? Next,
    IReadOnlyList<PostCard> Related,
    string Language);

public interface IPublicBlogQueries
{
    Task<BlogCatalog> GetCatalogAsync(BlogCatalogQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PostCard>> GetRecentAsync(int count, CancellationToken cancellationToken = default);

    Task<PostDetail?> GetBySlugAsync(string slug, bool includeDrafts, CancellationToken cancellationToken = default);
}

internal sealed class PublicBlogQueries(IApplicationDbContext db, IMarkdownRenderer markdown) : IPublicBlogQueries
{
    private const int RelatedCount = 2;

    public async Task<BlogCatalog> GetCatalogAsync(BlogCatalogQuery query, CancellationToken cancellationToken = default)
    {
        var posts = Published();
        if (!string.IsNullOrWhiteSpace(query.Tag))
        {
            posts = posts.Where(p => p.Tags.Contains(query.Tag));
        }

        var page = await ToCards(posts.OrderByDescending(p => p.PublishedAt))
            .ToPagedListAsync(query.Page, query.PageSize, cancellationToken);

        // Etiket sayımı bellekte yapılır: kişisel blogda yazı sayısı küçüktür ve
        // dizi (text[]) sütunu üzerinde gruplama her sağlayıcıda aynı şekilde çevrilmez.
        var allTags = await Published().Select(p => p.Tags).ToListAsync(cancellationToken);
        var tags = allTags
            .SelectMany(t => t)
            .GroupBy(t => t)
            .Select(g => new FilterOption(g.Key, g.Key, g.Count()))
            .OrderByDescending(t => t.Count).ThenBy(t => t.Label, StringComparer.CurrentCulture)
            .ToList();

        return new BlogCatalog(page, tags, query);
    }

    public async Task<IReadOnlyList<PostCard>> GetRecentAsync(int count, CancellationToken cancellationToken = default) =>
        await ToCards(Published().OrderByDescending(p => p.PublishedAt)).Take(count).ToListAsync(cancellationToken);

    public async Task<PostDetail?> GetBySlugAsync(string slug, bool includeDrafts, CancellationToken cancellationToken = default)
    {
        var post = await db.BlogPosts
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == slug && (p.IsPublished || includeDrafts), cancellationToken);
        if (post is null)
        {
            return null;
        }

        ProjectLink? previous = null, next = null;
        if (post.IsPublished)
        {
            // Yeniden eskiye sıralı listede "önceki" daha eski yazıdır.
            var sequence = await Published()
                .OrderByDescending(p => p.PublishedAt)
                .Select(p => new { p.Id, p.Title, p.Slug })
                .ToListAsync(cancellationToken);
            var index = sequence.FindIndex(p => p.Id == post.Id);
            if (index >= 0 && index < sequence.Count - 1)
            {
                previous = new ProjectLink(sequence[index + 1].Title, sequence[index + 1].Slug);
            }

            if (index > 0)
            {
                next = new ProjectLink(sequence[index - 1].Title, sequence[index - 1].Slug);
            }
        }

        var candidates = await ToCards(Published().Where(p => p.Id != post.Id).OrderByDescending(p => p.PublishedAt))
            .Take(20)
            .ToListAsync(cancellationToken);
        var related = candidates
            .OrderByDescending(c => c.Tags.Intersect(post.Tags).Count())
            .ThenByDescending(c => c.PublishedAt)
            .Take(RelatedCount)
            .ToList();

        return new PostDetail(
            post.Id,
            post.Title,
            post.Slug,
            post.Excerpt,
            markdown.Render(post.Body),
            post.CoverImageUrl,
            post.Tags,
            post.IsPublished,
            post.PublishedAt,
            post.UpdatedAt ?? post.CreatedAt,
            previous,
            next,
            related,
            post.Language);
    }

    private IQueryable<BlogPost> Published() => db.BlogPosts.AsNoTracking().Where(p => p.IsPublished);

    private static IQueryable<PostCard> ToCards(IQueryable<BlogPost> query) =>
        query.Select(p => new PostCard(
            p.Title, p.Slug, p.Excerpt, p.CoverImageUrl, p.Tags, p.PublishedAt ?? p.CreatedAt, p.ReadingMinutes, p.Language));
}
