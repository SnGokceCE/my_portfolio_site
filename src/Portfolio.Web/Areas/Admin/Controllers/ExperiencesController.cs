using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.Experiences;
using Portfolio.Web.Areas.Admin.Models;

namespace Portfolio.Web.Areas.Admin.Controllers;

public class ExperiencesController(IExperienceService experiences) : AdminController
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await experiences.ListAsync(cancellationToken));

    public IActionResult Create() => View("Edit", new FormViewModel<ExperienceInput>());

    [HttpPost]
    public async Task<IActionResult> Create(FormViewModel<ExperienceInput> form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("Edit", form);
        }

        var result = await experiences.CreateAsync(form.Input, cancellationToken);
        if (!result.Succeeded)
        {
            AddErrors(result);
            return View("Edit", form);
        }

        Flash("Deneyim eklendi.");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var input = await experiences.GetForEditAsync(id, cancellationToken);
        return input is null ? NotFound() : View(new FormViewModel<ExperienceInput> { Id = id, Input = input });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, FormViewModel<ExperienceInput> form, CancellationToken cancellationToken)
    {
        form.Id = id;
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var result = await experiences.UpdateAsync(id, form.Input, cancellationToken);
        if (result.IsNotFound)
        {
            return NotFound();
        }

        if (!result.Succeeded)
        {
            AddErrors(result);
            return View(form);
        }

        Flash("Deneyim güncellendi.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        AfterAction(await experiences.DeleteAsync(id, cancellationToken), "Deneyim silindi.");
}
