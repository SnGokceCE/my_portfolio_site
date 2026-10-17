using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.SiteProfiles;

namespace Portfolio.Web.ViewComponents;

/// <summary>Alt bilgide profil adını ve sosyal bağlantıları gösterir.</summary>
public class SiteFooterViewComponent(ISiteProfileService profiles) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync() =>
        View(await profiles.GetAsync(HttpContext.RequestAborted));
}
