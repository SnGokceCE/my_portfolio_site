using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.Common.Interfaces;

namespace Portfolio.Web.Areas.Admin.Controllers;

/// <summary>Editördeki "Önizle" sekmesi için Markdown'ı sitedeki işleyiciyle HTML'e çevirir.</summary>
public class MarkdownController(IMarkdownRenderer renderer) : AdminController
{
    private const int MaxLength = 100_000;

    [HttpPost]
    public IActionResult Preview([FromForm] string? markdown)
    {
        if (markdown is { Length: > MaxLength })
        {
            return BadRequest();
        }

        var rendered = renderer.Render(markdown);
        return Content(
            rendered.IsEmpty ? "<p class=\"muted\">Önizlenecek içerik yok.</p>" : rendered.Html,
            "text/html; charset=utf-8");
    }
}
