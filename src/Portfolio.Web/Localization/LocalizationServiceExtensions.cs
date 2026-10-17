namespace Portfolio.Web.Localization;

public static class LocalizationServiceExtensions
{
    public static IServiceCollection AddSiteLocalization(this IServiceCollection services)
    {
        services.AddSingleton<Microsoft.Extensions.Localization.IStringLocalizerFactory, JsonStringLocalizerFactory>();
        services.AddTransient(typeof(Microsoft.Extensions.Localization.IStringLocalizer<>), typeof(Microsoft.Extensions.Localization.StringLocalizer<>));
        return services;
    }
}
