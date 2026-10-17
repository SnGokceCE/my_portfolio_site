using Portfolio.Application.Common.Models;

namespace Portfolio.Application.SiteProfiles;

public interface ISiteProfileService
{
    Task<SiteProfileDto> GetAsync(CancellationToken cancellationToken = default);

    Task<SiteProfileInput> GetForEditAsync(CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(SiteProfileInput input, CancellationToken cancellationToken = default);
}
