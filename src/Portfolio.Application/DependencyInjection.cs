using System.Globalization;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Application.Blog;
using Portfolio.Application.Categories;
using Portfolio.Application.Contact;
using Portfolio.Application.Dashboard;
using Portfolio.Application.Experiences;
using Portfolio.Application.Projects;
using Portfolio.Application.Public;
using Portfolio.Application.SiteProfiles;
using Portfolio.Application.Skills;

namespace Portfolio.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddOptions<ContactOptions>();

        // FluentValidation'ın hazır hata mesajları Türkçe olsun.
        ValidatorOptions.Global.LanguageManager.Culture = CultureInfo.GetCultureInfo("tr");
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddScoped<ISiteProfileService, SiteProfileService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ISkillService, SkillService>();
        services.AddScoped<IExperienceService, ExperienceService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IPublicProjectQueries, PublicProjectQueries>();
        services.AddScoped<IBlogPostService, BlogPostService>();
        services.AddScoped<IPublicBlogQueries, PublicBlogQueries>();
        services.AddScoped<IContactService, ContactService>();

        return services;
    }
}
