using Microsoft.Extensions.Caching.Memory;
using Portfolio.Infrastructure.Content;

namespace Portfolio.UnitTests.Infrastructure;

public sealed class MarkdownRendererTests : IDisposable
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly MarkdownRenderer _renderer;

    public MarkdownRendererTests()
    {
        _renderer = new MarkdownRenderer(_cache);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Render_EmptyInput_ReturnsEmpty(string? markdown)
    {
        Assert.True(_renderer.Render(markdown).IsEmpty);
    }

    [Fact]
    public void Render_EscapesRawHtml()
    {
        var html = _renderer.Render("Merhaba <script>alert('x')</script> <img src=x onerror=alert(1)>").Html;

        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<img", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Render_RemovesJavascriptLinks()
    {
        var html = _renderer.Render("[tıkla](javascript:alert(1))").Html;

        Assert.DoesNotContain("javascript:", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Render_ExternalLinksOpenSafelyInNewTab()
    {
        var html = _renderer.Render("[GitHub](https://github.com)").Html;

        Assert.Contains("target=\"_blank\"", html, StringComparison.Ordinal);
        Assert.Contains("rel=\"noopener noreferrer\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_ExtractsHeadings_AndDemotesH1()
    {
        var result = _renderer.Render("# Giriş\n\nmetin\n\n## Çözüm Yolu\n\n### Ayrıntı\n\n#### Çok derin");

        Assert.Contains("<h2 id=\"giris\">", result.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("<h1", result.Html, StringComparison.Ordinal);
        Assert.Equal(["Giriş", "Çözüm Yolu", "Ayrıntı"], result.Headings.Select(h => h.Text));
        Assert.Equal([2, 2, 3], result.Headings.Select(h => h.Level));
        Assert.All(result.Headings, h => Assert.False(string.IsNullOrEmpty(h.Id)));
    }

    [Fact]
    public void Render_SupportsTablesAndTaskLists()
    {
        var html = _renderer.Render("| A | B |\n|---|---|\n| 1 | 2 |\n\n- [x] bitti\n- [ ] yapılacak").Html;

        Assert.Contains("<table", html, StringComparison.Ordinal);
        Assert.Contains("task-list", html, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadingMinutes_IsAtLeastOne_AndScalesWithLength()
    {
        Assert.Equal(1, _renderer.Render("kısa metin").ReadingMinutes);
        Assert.Equal(3, _renderer.Render(string.Join(' ', Enumerable.Repeat("kelime", 450))).ReadingMinutes);
    }

    public void Dispose() => _cache.Dispose();
}
