using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.SiteProfiles;
using Portfolio.Web.Areas.Admin.Models;

namespace Portfolio.Web.Areas.Admin.Controllers;

public class ProfileController(ISiteProfileService profiles) : AdminController
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(new FormViewModel<SiteProfileInput> { Input = await profiles.GetForEditAsync(cancellationToken) });

    [HttpPost]
    public async Task<IActionResult> Index(FormViewModel<SiteProfileInput> form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var result = await profiles.UpdateAsync(form.Input, cancellationToken);
        if (!result.Succeeded)
        {
            AddErrors(result);
            return View(form);
        }

        Flash("Profil güncellendi.");
        return RedirectToAction(nameof(Index));
    }
}
