using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.SiteProfiles;
using Portfolio.Web.Models;

namespace Portfolio.Web.Controllers;

public class HomeController(ISiteProfileService profiles) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var profile = await profiles.GetAsync(cancellationToken);
        return View(profile);
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
