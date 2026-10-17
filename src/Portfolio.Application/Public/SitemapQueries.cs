using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Common.Interfaces;

namespace Portfolio.Application.Public;

/// <summary>Site haritasındaki bir sayfa: dil ön eki olmayan yol ve son değişiklik zamanı.</summary>
public sealed record SitemapEntry(string Path, DateTimeOffset? LastModified, double Priority);

public interface ISitemapQueries
{
    Task<IReadOnlyList<SitemapEntry>> GetEntriesAsync(CancellationToken cancellationToken = default);
}

internal sealed class SitemapQueries(IApplicationDbContext db) : ISitemapQueries
{
    public async Task<IReadOnlyList<SitemapEntry>> GetEntriesAsync(CancellationToken cancellationToken = default)
    {
        var projects = await db.Projects
            .AsNoTracking()
            .Where(p => p.IsPublished)
            .Select(p => new { p.Slug, Modified = p.UpdatedAt ?? p.CreatedAt })
            .ToListAsync(cancellationToken);

        var posts = await db.BlogPosts
            .AsNoTracking()
            .Where(p => p.IsPublished)
            .Select(p => new { p.Slug, Modified = p.UpdatedAt ?? p.CreatedAt })
            .ToListAsync(cancellationToken);

        DateTimeOffset? Latest(IEnumerable<DateTimeOffset> dates) => dates.Any() ? dates.Max() : null;

        var latestProject = Latest(projects.Select(p => p.Modified));
        var latestPost = Latest(posts.Select(p => p.Modified));

        var entries = new List<SitemapEntry>
        {
            new("/", Latest(new[] { latestProject, latestPost }.OfType<DateTimeOffset>()), 1.0),
            new("/projeler", latestProject, 0.9),
            new("/blog", latestPost, 0.8),
            new("/iletisim", null, 0.5)
        };

        entries.AddRange(projects.Select(p => new SitemapEntry($"/projeler/{p.Slug}", p.Modified, 0.8)));
        entries.AddRange(posts.Select(p => new SitemapEntry($"/blog/{p.Slug}", p.Modified, 0.7)));
        return entries;
    }
}
