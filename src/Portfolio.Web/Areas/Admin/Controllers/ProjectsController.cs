using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.Categories;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Application.Projects;
using Portfolio.Application.Skills;
using Portfolio.Web.Areas.Admin.Models;

namespace Portfolio.Web.Areas.Admin.Controllers;

public class ProjectsController(
    IProjectService projects,
    ICategoryService categories,
    ISkillService skills) : AdminController
{
    // Kapak görseli (5 MB) + form alanları için güvenli üst sınır.
    private const long MaxRequestBytes = 6 * 1024 * 1024;

    public async Task<IActionResult> Index(
        string? search,
        Guid? categoryId,
        ProjectStatusFilter status = ProjectStatusFilter.All,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var query = new ProjectQuery(search, categoryId, status, page);
        var list = await projects.ListAsync(query, cancellationToken);
        var options = await categories.ListOptionsAsync(cancellationToken);
        return View(new ProjectIndexViewModel(list, query, options));
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken) =>
        View("Edit", await WithOptionsAsync(new ProjectFormViewModel(), cancellationToken));

    [HttpPost, RequestSizeLimit(MaxRequestBytes)]
    public async Task<IActionResult> Create(ProjectFormViewModel form, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            await using var cover = form.Cover?.OpenReadStream();
            var upload = ToUpload(form.Cover, cover);
            var result = await projects.CreateAsync(form.Input, upload, cancellationToken);
            if (result.Succeeded)
            {
                Flash($"“{form.Input.Title}” projesi oluşturuldu.");
                return RedirectToAction(nameof(Edit), new { id = result.Value });
            }

            AddErrors(result, ProjectFormFields.Cover);
        }

        return View("Edit", await WithOptionsAsync(form, cancellationToken));
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var model = await projects.GetForEditAsync(id, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        var form = new ProjectFormViewModel { Id = id, Input = model.Input, CoverImageUrl = model.CoverImageUrl };
        return View(await WithOptionsAsync(form, cancellationToken));
    }

    [HttpPost, RequestSizeLimit(MaxRequestBytes)]
    public async Task<IActionResult> Edit(Guid id, ProjectFormViewModel form, CancellationToken cancellationToken)
    {
        form.Id = id;
        if (ModelState.IsValid)
        {
            await using var cover = form.Cover?.OpenReadStream();
            var upload = ToUpload(form.Cover, cover);
            var result = await projects.UpdateAsync(id, form.Input, upload, form.RemoveCover, cancellationToken);
            if (result.IsNotFound)
            {
                return NotFound();
            }

            if (result.Succeeded)
            {
                Flash("Değişiklikler kaydedildi.");
                return RedirectToAction(nameof(Edit), new { id });
            }

            AddErrors(result, ProjectFormFields.Cover);
        }

        // Hata durumunda mevcut kapak görselini tekrar göstermek için veritabanından oku.
        form.CoverImageUrl = (await projects.GetForEditAsync(id, cancellationToken))?.CoverImageUrl;
        return View(await WithOptionsAsync(form, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Publish(Guid id, bool published, string? returnUrl, CancellationToken cancellationToken)
    {
        var result = await projects.SetPublishedAsync(id, published, cancellationToken);
        if (result.IsNotFound)
        {
            return NotFound();
        }

        Flash(published ? "Proje yayımlandı." : "Proje taslağa alındı.");
        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        AfterAction(await projects.DeleteAsync(id, cancellationToken), "Proje silindi.");

    private static ImageUpload? ToUpload(IFormFile? file, Stream? stream) =>
        file is { Length: > 0 } && stream is not null ? new ImageUpload(stream, file.FileName, file.Length) : null;

    private async Task<ProjectFormViewModel> WithOptionsAsync(ProjectFormViewModel form, CancellationToken cancellationToken)
    {
        form.Categories = await categories.ListOptionsAsync(cancellationToken);
        form.Skills = await skills.ListOptionsAsync(cancellationToken);
        return form;
    }
}
