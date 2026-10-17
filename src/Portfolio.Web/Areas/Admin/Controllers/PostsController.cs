using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.Blog;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Application.Projects;
using Portfolio.Web.Areas.Admin.Models;

namespace Portfolio.Web.Areas.Admin.Controllers;

public class PostsController(IBlogPostService posts) : AdminController
{
    private const long MaxRequestBytes = 6 * 1024 * 1024;

    public async Task<IActionResult> Index(
        string? search,
        ProjectStatusFilter status = ProjectStatusFilter.All,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var query = new BlogPostQuery(search, status, page);
        return View(new PostIndexViewModel(await posts.ListAsync(query, cancellationToken), query));
    }

    public IActionResult Create() => View("Edit", new PostFormViewModel());

    [HttpPost, RequestSizeLimit(MaxRequestBytes)]
    public async Task<IActionResult> Create(PostFormViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("Edit", form);
        }

        await using var stream = form.Cover?.OpenReadStream();
        var result = await posts.CreateAsync(form.Input, ToUpload(form.Cover, stream), cancellationToken);
        if (!result.Succeeded)
        {
            AddErrors(result, ProjectFormFields.Cover);
            return View("Edit", form);
        }

        Flash($"“{form.Input.Title}” yazısı oluşturuldu.");
        return RedirectToAction(nameof(Edit), new { id = result.Value });
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var model = await posts.GetForEditAsync(id, cancellationToken);
        return model is null
            ? NotFound()
            : View(new PostFormViewModel { Id = id, Input = model.Input, CoverImageUrl = model.CoverImageUrl });
    }

    [HttpPost, RequestSizeLimit(MaxRequestBytes)]
    public async Task<IActionResult> Edit(Guid id, PostFormViewModel form, CancellationToken cancellationToken)
    {
        form.Id = id;
        if (ModelState.IsValid)
        {
            await using var stream = form.Cover?.OpenReadStream();
            var result = await posts.UpdateAsync(id, form.Input, ToUpload(form.Cover, stream), form.RemoveCover, cancellationToken);
            if (result.IsNotFound)
            {
                return NotFound();
            }

            if (result.Succeeded)
            {
                Flash("Yazı kaydedildi.");
                return RedirectToAction(nameof(Edit), new { id });
            }

            AddErrors(result, ProjectFormFields.Cover);
        }

        form.CoverImageUrl = (await posts.GetForEditAsync(id, cancellationToken))?.CoverImageUrl;
        return View(form);
    }

    [HttpPost]
    public async Task<IActionResult> Publish(Guid id, bool published, string? returnUrl, CancellationToken cancellationToken)
    {
        var result = await posts.SetPublishedAsync(id, published, cancellationToken);
        if (result.IsNotFound)
        {
            return NotFound();
        }

        Flash(published ? "Yazı yayımlandı." : "Yazı taslağa alındı.");
        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        AfterAction(await posts.DeleteAsync(id, cancellationToken), "Yazı silindi.");

    private static ImageUpload? ToUpload(IFormFile? file, Stream? stream) =>
        file is { Length: > 0 } && stream is not null ? new ImageUpload(stream, file.FileName, file.Length) : null;
}
