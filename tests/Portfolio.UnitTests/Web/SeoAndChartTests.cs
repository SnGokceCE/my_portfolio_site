using Portfolio.Application.Common.Interfaces;
using Portfolio.Application.Public;
using Portfolio.Web.Infrastructure;

namespace Portfolio.UnitTests.Web;

public class SvgChartTests
{
    [Theory]
    [InlineData(0, 4)]
    [InlineData(3, 4)]
    [InlineData(7, 8)]
    [InlineData(23, 24)]
    [InlineData(140, 160)]
    [InlineData(1000, 1200)]
    public void NiceCeiling_IsAtLeastValue_AndDividesIntoFourIntegerSteps(int value, int expected)
    {
        var result = SvgChart.NiceCeiling(value);

        Assert.Equal(expected, result);
        Assert.True(result >= value);
        Assert.Equal(0, result % 4);
    }

    [Fact]
    public void Line_EncodesLabels()
    {
        var html = SvgChart.Line(["<a>"], [new SvgChart.Series("x", [1], "s", Area: false)]).ToString();

        Assert.DoesNotContain("<a>", html, StringComparison.Ordinal);
        Assert.Contains("&lt;a&gt;", html, StringComparison.Ordinal);
    }
}

public class JsonLdTests
{
    [Fact]
    public void Script_CannotBeBrokenOutOfByContent()
    {
        var post = new PostDetail(
            Guid.NewGuid(), "</script><script>alert(1)</script>", "slug", "özet", RenderedMarkdown.Empty,
            null, [], true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null, [], "tr");

        var html = JsonLd.Script(JsonLd.BlogPosting(post, "https://site/blog/slug", "https://site")).ToString()!;

        // Yalnızca bizim kapanış etiketimiz kalmalı.
        Assert.Equal(1, html.Split("</script>").Length - 1);
        Assert.Contains("\"@type\":\"BlogPosting\"", html, StringComparison.Ordinal);
    }
}
