using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.Skills;
using Portfolio.Web.Areas.Admin.Models;

namespace Portfolio.Web.Areas.Admin.Controllers;

public class SkillsController(ISkillService skills) : AdminController
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await skills.ListAsync(cancellationToken));

    public async Task<IActionResult> Create(CancellationToken cancellationToken) =>
        View("Edit", await WithGroupsAsync(new SkillFormViewModel(), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(SkillFormViewModel form, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await skills.CreateAsync(form.Input, cancellationToken);
            if (result.Succeeded)
            {
                Flash($"“{form.Input.Name}” eklendi.");
                return RedirectToAction(nameof(Index));
            }

            AddErrors(result);
        }

        return View("Edit", await WithGroupsAsync(form, cancellationToken));
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var input = await skills.GetForEditAsync(id, cancellationToken);
        return input is null
            ? NotFound()
            : View(await WithGroupsAsync(new SkillFormViewModel { Id = id, Input = input }, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, SkillFormViewModel form, CancellationToken cancellationToken)
    {
        form.Id = id;
        if (ModelState.IsValid)
        {
            var result = await skills.UpdateAsync(id, form.Input, cancellationToken);
            if (result.IsNotFound)
            {
                return NotFound();
            }

            if (result.Succeeded)
            {
                Flash("Yetenek güncellendi.");
                return RedirectToAction(nameof(Index));
            }

            AddErrors(result);
        }

        return View(await WithGroupsAsync(form, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        AfterAction(await skills.DeleteAsync(id, cancellationToken), "Yetenek silindi.");

    private async Task<SkillFormViewModel> WithGroupsAsync(SkillFormViewModel form, CancellationToken cancellationToken)
    {
        form.Groups = await skills.ListGroupsAsync(cancellationToken);
        form.EnglishGroups = await skills.ListEnglishGroupsAsync(cancellationToken);
        return form;
    }
}
