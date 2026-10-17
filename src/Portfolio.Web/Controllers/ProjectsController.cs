using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.Common.Security;
using Portfolio.Application.Public;

namespace Portfolio.Web.Controllers;

/// <summary>Ziyaretçiye açık proje sayfaları. URL'ler Türkçe: /projeler, /projeler/{slug}.</summary>
[Route("projeler")]
public class ProjectsController(IPublicProjectQueries queries) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(
        [FromQuery(Name = "kategori")] string? category,
        [FromQuery(Name = "teknoloji")] string? technology,
        [FromQuery(Name = "sayfa")] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var catalog = await queries.GetCatalogAsync(new ProjectCatalogQuery(category, technology, page), cancellationToken);
        return View(catalog);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Detail(string slug, CancellationToken cancellationToken)
    {
        // Admin, taslak projeleri yayımlamadan önce sitede nasıl görüneceğini önizleyebilir.
        var includeDrafts = User.IsInRole(Roles.Admin);
        var project = await queries.GetBySlugAsync(slug, includeDrafts, cancellationToken);
        return project is null ? NotFound() : View(project);
    }
}
