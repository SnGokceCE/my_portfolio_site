using System.Net;

namespace Portfolio.IntegrationTests;

[Collection(WebTestGroup.Name)]
public class PublicSiteTests(PortfolioFactory factory)
{
    [Fact]
    public async Task Home_RendersTurkishByDefault_AndEnglishUnderPrefix()
    {
        var client = factory.CreateBrowser();

        var turkish = await client.GetDocumentAsync("/");
        var english = await client.GetDocumentAsync("/en");

        Assert.Equal("tr", turkish.DocumentElement.GetAttribute("lang"));
        Assert.Contains("Merhaba, ben", turkish.QuerySelector(".hero-title")!.TextContent, StringComparison.Ordinal);
        Assert.Equal("en", english.DocumentElement.GetAttribute("lang"));
        Assert.Contains("Hi, I'm", english.QuerySelector(".hero-title")!.TextContent, StringComparison.Ordinal);
        Assert.Equal("/en/projeler", english.QuerySelector(".site-nav a[href$='projeler']")!.GetAttribute("href"));
    }

    [Fact]
    public async Task Pages_SendSecurityHeaders_WithMatchingScriptNonce()
    {
        var client = factory.CreateBrowser();

        using var response = await client.GetAsync("/");
        var document = await response.ReadDocumentAsync();

        var csp = response.Headers.GetValues("Content-Security-Policy").Single();
        var nonce = document.QuerySelector("head script[nonce]")!.GetAttribute("nonce");
        Assert.Contains($"'nonce-{nonce}'", csp, StringComparison.Ordinal);
        Assert.DoesNotContain("script-src 'self' 'unsafe-inline'", csp, StringComparison.Ordinal);
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
    }

    [Fact]
    public async Task Health_ChecksTheRealDatabase()
    {
        var client = factory.CreateBrowser();

        var body = await client.GetStringAsync("/health");

        Assert.Equal("Healthy", body);
    }

    [Fact]
    public async Task UnknownPage_ReturnsLocalized404()
    {
        var client = factory.CreateBrowser();

        using var turkish = await client.GetAsync("/boyle-bir-sayfa-yok");
        using var english = await client.GetAsync("/en/no-such-page");

        Assert.Equal(HttpStatusCode.NotFound, turkish.StatusCode);
        Assert.Equal("Sayfa bulunamadı", (await turkish.ReadDocumentAsync()).QuerySelector("h1")!.TextContent.Trim());
        Assert.Equal("Page not found", (await english.ReadDocumentAsync()).QuerySelector("h1")!.TextContent.Trim());
    }

    [Fact]
    public async Task Robots_DisallowsAdmin_AndPointsToSitemap()
    {
        var robots = await factory.CreateBrowser().GetStringAsync("/robots.txt");

        Assert.Contains("Disallow: /admin", robots, StringComparison.Ordinal);
        Assert.Contains("/sitemap.xml", robots, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Admin_RequiresLogin()
    {
        using var response = await factory.CreateBrowser().GetAsync("/admin");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/account/login", response.Headers.Location!.PathAndQuery, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShowsError()
    {
        var client = factory.CreateBrowser();
        var page = await client.GetDocumentAsync("/account/login");

        using var response = await client.PostFormAsync("/account/login", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = page.AntiforgeryToken(),
            ["Email"] = PortfolioFactory.AdminEmail,
            ["Password"] = "yanlis-sifre"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page2 = await response.ReadDocumentAsync();
        Assert.Contains("E-posta veya şifre hatalı", page2.QuerySelector(".alert-error")!.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Post_WithoutAntiforgeryToken_IsRejected()
    {
        using var response = await factory.CreateBrowser().PostFormAsync("/iletisim", new Dictionary<string, string>
        {
            ["Input.Name"] = "Bot",
            ["Input.Email"] = "bot@example.com",
            ["Input.Message"] = "CSRF jetonu olmadan gönderim"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
