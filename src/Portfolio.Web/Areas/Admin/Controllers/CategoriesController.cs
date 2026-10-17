using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.Categories;
using Portfolio.Web.Areas.Admin.Models;

namespace Portfolio.Web.Areas.Admin.Controllers;

public class CategoriesController(ICategoryService categories) : AdminController
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await categories.ListAsync(cancellationToken));

    public IActionResult Create() => View("Edit", new FormViewModel<CategoryInput>());

    [HttpPost]
    public async Task<IActionResult> Create(FormViewModel<CategoryInput> form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("Edit", form);
        }

        var result = await categories.CreateAsync(form.Input, cancellationToken);
        if (!result.Succeeded)
        {
            AddErrors(result);
            return View("Edit", form);
        }

        Flash($"“{form.Input.Name}” kategorisi eklendi.");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var input = await categories.GetForEditAsync(id, cancellationToken);
        return input is null ? NotFound() : View(new FormViewModel<CategoryInput> { Id = id, Input = input });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, FormViewModel<CategoryInput> form, CancellationToken cancellationToken)
    {
        form.Id = id;
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var result = await categories.UpdateAsync(id, form.Input, cancellationToken);
        if (result.IsNotFound)
        {
            return NotFound();
        }

        if (!result.Succeeded)
        {
            AddErrors(result);
            return View(form);
        }

        Flash("Kategori güncellendi.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        AfterAction(await categories.DeleteAsync(id, cancellationToken), "Kategori silindi.");
}
