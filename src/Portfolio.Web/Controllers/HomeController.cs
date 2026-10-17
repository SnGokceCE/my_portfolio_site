using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.Experiences;
using Portfolio.Application.Public;
using Portfolio.Application.SiteProfiles;
using Portfolio.Application.Skills;
using Portfolio.Web.Models;

namespace Portfolio.Web.Controllers;

public class HomeController(
    ISiteProfileService profiles,
    IPublicProjectQueries projects,
    IExperienceService experiences,
    ISkillService skills) : Controller
{
    private const int FeaturedCount = 6;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = new HomeViewModel(
            await profiles.GetAsync(cancellationToken),
            await projects.GetFeaturedAsync(FeaturedCount, cancellationToken),
            await experiences.ListAsync(cancellationToken),
            await skills.ListAsync(cancellationToken));

        return View(model);
    }

    [Route("home/status/{code:int}")]
    public IActionResult Status(int code)
    {
        Response.StatusCode = code;
        return View(new StatusViewModel(code));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
