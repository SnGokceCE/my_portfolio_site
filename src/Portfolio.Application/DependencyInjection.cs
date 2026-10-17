using Microsoft.Extensions.DependencyInjection;
using Portfolio.Application.SiteProfiles;

namespace Portfolio.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ISiteProfileService, SiteProfileService>();

        return services;
    }
}
