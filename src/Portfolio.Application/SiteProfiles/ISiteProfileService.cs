namespace Portfolio.Application.SiteProfiles;

public interface ISiteProfileService
{
    Task<SiteProfileDto> GetAsync(CancellationToken cancellationToken = default);
}
