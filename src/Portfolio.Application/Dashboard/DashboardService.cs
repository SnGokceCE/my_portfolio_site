using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Common.Interfaces;

namespace Portfolio.Application.Dashboard;

public sealed record RecentProject(Guid Id, string Title, bool IsPublished, DateTimeOffset LastModified);

public sealed record DashboardStats(
    int ProjectCount,
    int PublishedProjectCount,
    int ExperienceCount,
    int SkillCount,
    int CategoryCount,
    int BlogPostCount,
    int UnreadMessageCount,
    IReadOnlyList<RecentProject> RecentProjects)
{
    public int DraftProjectCount => ProjectCount - PublishedProjectCount;
}

public interface IDashboardService
{
    Task<DashboardStats> GetStatsAsync(CancellationToken cancellationToken = default);
}

internal sealed class DashboardService(IApplicationDbContext db) : IDashboardService
{
    public async Task<DashboardStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        // DbContext aynı anda tek sorgu çalıştırabildiği için sorgular sırayla bekleniyor.
        var projectCount = await db.Projects.CountAsync(cancellationToken);
        var publishedCount = await db.Projects.CountAsync(p => p.IsPublished, cancellationToken);
        var experienceCount = await db.Experiences.CountAsync(cancellationToken);
        var skillCount = await db.Skills.CountAsync(cancellationToken);
        var categoryCount = await db.Categories.CountAsync(cancellationToken);
        var blogPostCount = await db.BlogPosts.CountAsync(cancellationToken);
        var unreadCount = await db.ContactMessages.CountAsync(m => !m.IsSpam && m.ReadAt == null, cancellationToken);

        var recent = await db.Projects
            .AsNoTracking()
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .Take(5)
            .Select(p => new RecentProject(p.Id, p.Title, p.IsPublished, p.UpdatedAt ?? p.CreatedAt))
            .ToListAsync(cancellationToken);

        return new DashboardStats(
            projectCount, publishedCount, experienceCount, skillCount, categoryCount, blogPostCount, unreadCount, recent);
    }
}
