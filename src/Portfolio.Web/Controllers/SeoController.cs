using System.Globalization;
using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.Public;
using Portfolio.Web.Localization;

namespace Portfolio.Web.Controllers;

/// <summary>Arama motorları için site haritası ve robots.txt.</summary>
public class SeoController(ISitemapQueries sitemap) : Controller
{
    private const string SitemapNs = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private const string XhtmlNs = "http://www.w3.org/1999/xhtml";

    /// <summary>
    /// Her sayfa iki dilde listelenir ve birbirine hreflang ile bağlanır; böylece Google
    /// Türkçe arama yapana Türkçe, İngilizce arama yapana İngilizce sürümü gösterir.
    /// </summary>
    [HttpGet("/sitemap.xml")]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> Sitemap(CancellationToken cancellationToken)
    {
        var origin = Origin();
        var entries = await sitemap.GetEntriesAsync(cancellationToken);

        var settings = new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true, Async = true };
        using var buffer = new MemoryStream();
        await using (var xml = XmlWriter.Create(buffer, settings))
        {
            await xml.WriteStartDocumentAsync();
            xml.WriteStartElement("urlset", SitemapNs);
            xml.WriteAttributeString("xmlns", "xhtml", null, XhtmlNs);

            foreach (var entry in entries)
            {
                var turkish = origin + entry.Path;
                var english = origin + SiteCultures.EnglishPrefix + (entry.Path == "/" ? string.Empty : entry.Path);

                foreach (var (location, priority) in new[] { (turkish, entry.Priority), (english, entry.Priority * 0.9) })
                {
                    xml.WriteStartElement("url", SitemapNs);
                    xml.WriteElementString("loc", SitemapNs, location);
                    if (entry.LastModified is { } modified)
                    {
                        xml.WriteElementString("lastmod", SitemapNs, modified.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                    }

                    xml.WriteElementString("priority", SitemapNs, priority.ToString("0.0", CultureInfo.InvariantCulture));
                    WriteAlternate(xml, "tr", turkish);
                    WriteAlternate(xml, "en", english);
                    WriteAlternate(xml, "x-default", turkish);
                    xml.WriteEndElement();
                }
            }

            xml.WriteEndElement();
            await xml.WriteEndDocumentAsync();
        }

        return File(buffer.ToArray(), "application/xml; charset=utf-8");
    }

    [HttpGet("/robots.txt")]
    [ResponseCache(Duration = 86400)]
    public IActionResult Robots()
    {
        var text = $"""
            User-agent: *
            Allow: /
            Disallow: /admin
            Disallow: /account
            Disallow: /en/admin
            Disallow: /en/account

            Sitemap: {Origin()}/sitemap.xml
            """;
        return Content(text, "text/plain; charset=utf-8");
    }

    private string Origin()
    {
        // Dil ön eki PathBase'de olabilir; site haritası her zaman kök adresten üretilir.
        var pathBase = Request.PathBase.Value ?? string.Empty;
        if (SiteCultures.IsEnglish(Request))
        {
            pathBase = pathBase[..^SiteCultures.EnglishPrefix.Length];
        }

        return $"{Request.Scheme}://{Request.Host}{pathBase}";
    }

    private static void WriteAlternate(XmlWriter xml, string language, string href)
    {
        xml.WriteStartElement("xhtml", "link", XhtmlNs);
        xml.WriteAttributeString("rel", "alternate");
        xml.WriteAttributeString("hreflang", language);
        xml.WriteAttributeString("href", href);
        xml.WriteEndElement();
    }
}
