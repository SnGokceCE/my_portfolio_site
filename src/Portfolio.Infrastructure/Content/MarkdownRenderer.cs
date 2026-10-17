using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Ganss.Xss;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.Extensions.Caching.Memory;
using Portfolio.Application.Common.Interfaces;

namespace Portfolio.Infrastructure.Content;

/// <summary>
/// Markdown'ı HTML'e çevirir. İki katmanlı güvenlik: (1) Markdown içindeki ham HTML devre dışı,
/// (2) üretilen HTML ayrıca HtmlSanitizer'dan geçer; javascript: gibi tehlikeli bağlantılar ve
/// olay öznitelikleri (onclick vb.) temizlenir. Sonuç içeriğin özetine (hash) göre önbelleğe alınır.
/// </summary>
public sealed partial class MarkdownRenderer : Application.Common.Interfaces.IMarkdownRenderer
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    private readonly IMemoryCache _cache;
    private readonly MarkdownPipeline _pipeline;
    private readonly HtmlSanitizer _sanitizer;

    public MarkdownRenderer(IMemoryCache cache)
    {
        _cache = cache;
        _pipeline = new MarkdownPipelineBuilder()
            .DisableHtml()
            .UseAutoIdentifiers(AutoIdentifierOptions.Default)
            .UsePipeTables()
            .UseTaskLists()
            .UseEmphasisExtras()
            .UseListExtras()
            .UseAutoLinks()
            .Build();

        _sanitizer = new HtmlSanitizer();
        _sanitizer.AllowedSchemes.Add("mailto");
        _sanitizer.AllowedAttributes.Add("id");
        _sanitizer.AllowedAttributes.Add("class");
        _sanitizer.AllowedAttributes.Add("target");
        _sanitizer.AllowedAttributes.Add("rel");
        _sanitizer.AllowedAttributes.Add("loading");
        _sanitizer.PostProcessNode += (_, e) => Decorate(e.Node);
    }

    public RenderedMarkdown Render(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return RenderedMarkdown.Empty;
        }

        var key = "md:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(markdown)));
        return _cache.GetOrCreate(key, entry =>
        {
            entry.SlidingExpiration = CacheDuration;
            return RenderCore(markdown);
        })!;
    }

    private RenderedMarkdown RenderCore(string markdown)
    {
        var document = Markdown.Parse(markdown, _pipeline);
        var headings = new List<MarkdownHeading>();

        foreach (var heading in document.Descendants<HeadingBlock>())
        {
            // Sayfa başlığı zaten <h1>; içerikteki "# Başlık" bir alt seviyeye indirilir.
            if (heading.Level == 1)
            {
                heading.Level = 2;
            }

            var id = heading.GetAttributes().Id;
            if (heading.Level <= 3 && !string.IsNullOrEmpty(id))
            {
                headings.Add(new MarkdownHeading(heading.Level, PlainText(heading.Inline), id));
            }
        }

        using var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer);
        _pipeline.Setup(renderer);
        renderer.Render(document);
        writer.Flush();

        var html = _sanitizer.Sanitize(writer.ToString());
        return new RenderedMarkdown(html, headings, WordRegex().Count(markdown));
    }

    private static string PlainText(ContainerInline? inline)
    {
        if (inline is null)
        {
            return string.Empty;
        }

        var text = new StringBuilder();
        foreach (var node in inline.Descendants())
        {
            switch (node)
            {
                case LiteralInline literal:
                    text.Append(literal.Content.ToString());
                    break;
                case CodeInline code:
                    text.Append(code.Content);
                    break;
            }
        }

        return text.ToString().Trim();
    }

    // Dış bağlantılar yeni sekmede ve "noopener" ile açılır; görseller tembel yüklenir.
    private static void Decorate(INode node)
    {
        if (node is not IElement element)
        {
            return;
        }

        if (element.LocalName == "a" &&
            element.GetAttribute("href") is { } href &&
            Uri.TryCreate(href, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            element.SetAttribute("target", "_blank");
            element.SetAttribute("rel", "noopener noreferrer");
        }
        else if (element.LocalName == "img")
        {
            element.SetAttribute("loading", "lazy");
        }
    }

    [GeneratedRegex(@"\w+", RegexOptions.CultureInvariant)]
    private static partial Regex WordRegex();
}
