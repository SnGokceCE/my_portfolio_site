using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Common.Interfaces;

namespace Portfolio.Application.SiteProfiles;

internal sealed class SiteProfileService(IApplicationDbContext db) : ISiteProfileService
{
    public async Task<SiteProfileDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var profile = await db.SiteProfiles
            .AsNoTracking()
            .OrderBy(p => p.CreatedAt)
            .Select(p => new SiteProfileDto(
                p.FullName,
                p.Headline,
                p.Bio,
                p.Location,
                p.Email,
                p.GitHubUrl,
                p.LinkedInUrl,
                p.IsOpenToWork))
            .FirstOrDefaultAsync(cancellationToken);

        return profile ?? SiteProfileDto.Empty;
    }
}
