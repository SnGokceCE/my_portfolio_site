using System.Globalization;
using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.Common.Security;
using Portfolio.Application.Public;
using Portfolio.Application.SiteProfiles;

namespace Portfolio.Web.Controllers;

[Route("blog")]
public class BlogController(IPublicBlogQueries queries, ISiteProfileService profiles) : Controller
{
    private const int FeedSize = 20;

    [HttpGet("")]
    public async Task<IActionResult> Index(
        [FromQuery(Name = "etiket")] string? tag,
        [FromQuery(Name = "sayfa")] int page = 1,
        CancellationToken cancellationToken = default) =>
        View(await queries.GetCatalogAsync(new BlogCatalogQuery(tag, page), cancellationToken));

    [HttpGet("{slug}")]
    public async Task<IActionResult> Detail(string slug, CancellationToken cancellationToken)
    {
        var post = await queries.GetBySlugAsync(slug, User.IsInRole(Roles.Admin), cancellationToken);
        return post is null ? NotFound() : View(post);
    }

    /// <summary>RSS 2.0 akışı: okuyucular (Feedly vb.) yeni yazılardan haberdar olur.</summary>
    [HttpGet("rss.xml")]
    [ResponseCache(Duration = 600)]
    public async Task<IActionResult> Rss(CancellationToken cancellationToken)
    {
        var profile = await profiles.GetAsync(cancellationToken);
        var catalog = await queries.GetCatalogAsync(new BlogCatalogQuery(PageSize: FeedSize), cancellationToken);
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var settings = new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true, Async = true };
        using var buffer = new MemoryStream();
        await using (var xml = XmlWriter.Create(buffer, settings))
        {
            await xml.WriteStartDocumentAsync();
            xml.WriteStartElement("rss");
            xml.WriteAttributeString("version", "2.0");
            xml.WriteStartElement("channel");
            xml.WriteElementString("title", $"{profile.FullName} · Blog");
            xml.WriteElementString("link", $"{baseUrl}/blog");
            xml.WriteElementString("description", profile.Headline);
            xml.WriteElementString("language", "tr-TR");

            foreach (var post in catalog.Posts.Items)
            {
                var url = $"{baseUrl}/blog/{post.Slug}";
                xml.WriteStartElement("item");
                xml.WriteElementString("title", post.Title);
                xml.WriteElementString("link", url);
                xml.WriteElementString("guid", url);
                xml.WriteElementString("description", post.Excerpt);
                xml.WriteElementString("pubDate", post.PublishedAt.ToUniversalTime().ToString("r", CultureInfo.InvariantCulture));
                foreach (var tag in post.Tags)
                {
                    xml.WriteElementString("category", tag);
                }

                xml.WriteEndElement();
            }

            xml.WriteEndElement();
            xml.WriteEndElement();
            await xml.WriteEndDocumentAsync();
        }

        return File(buffer.ToArray(), "application/rss+xml; charset=utf-8");
    }
}
