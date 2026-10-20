using System.Net;
using Microsoft.EntityFrameworkCore;

namespace Portfolio.IntegrationTests;

/// <summary>Yönetim panelinden içerik oluşturup ziyaretçi sitesinde görünmesini uçtan uca doğrular.</summary>
[Collection(WebTestGroup.Name)]
public class AdminContentTests(PortfolioFactory factory)
{
    [Fact]
    public async Task PublishedProject_AppearsOnSite_InSitemap_AndInEnglish()
    {
        var admin = await factory.LoginAsAdminAsync();
        var title = $"Entegrasyon Projesi {Guid.NewGuid():N}"[..30];

        var form = await admin.GetDocumentAsync("/admin/projects/create");
        using var created = await admin.PostAsync("/admin/projects/create", new MultipartFormDataContent
        {
            { new StringContent(form.AntiforgeryToken()), "__RequestVerificationToken" },
            { new StringContent(title), "Input.Title" },
            { new StringContent("Uçtan uca test ile oluşturuldu."), "Input.Summary" },
            { new StringContent("## Başlık\n\nİçerik **kalın**."), "Input.Body" },
            { new StringContent("Integration Project"), "Input.TitleEn" },
            { new StringContent("true"), "Input.IsPublished" },
            { new StringContent("0"), "Input.SortOrder" }
        });
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);

        var slug = await factory.WithDatabaseAsync(db => db.Projects.Where(p => p.Title == title).Select(p => p.Slug).SingleAsync());
        var visitor = factory.CreateBrowser();

        var list = await visitor.GetDocumentAsync("/projeler");
        Assert.Contains(list.QuerySelectorAll(".project-title"), e => e.TextContent.Trim() == title);

        var detail = await visitor.GetDocumentAsync($"/projeler/{slug}");
        Assert.Equal(title, detail.QuerySelector("h1")!.TextContent.Trim());
        Assert.NotNull(detail.QuerySelector(".prose strong"));
        Assert.Null(detail.QuerySelector(".preview-banner"));

        var english = await visitor.GetDocumentAsync($"/en/projeler/{slug}");
        Assert.Equal("Integration Project", english.QuerySelector("h1")!.TextContent.Trim());

        var sitemap = await visitor.GetStringAsync("/sitemap.xml");
        Assert.Contains($"/projeler/{slug}</loc>", sitemap, StringComparison.Ordinal);
        Assert.Contains($"/en/projeler/{slug}</loc>", sitemap, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DraftProject_IsHiddenFromVisitors_ButPreviewableByAdmin()
    {
        var admin = await factory.LoginAsAdminAsync();
        var title = $"Taslak {Guid.NewGuid():N}"[..20];

        var form = await admin.GetDocumentAsync("/admin/projects/create");
        await admin.PostAsync("/admin/projects/create", new MultipartFormDataContent
        {
            { new StringContent(form.AntiforgeryToken()), "__RequestVerificationToken" },
            { new StringContent(title), "Input.Title" },
            { new StringContent("Henüz hazır değil."), "Input.Summary" },
            { new StringContent("0"), "Input.SortOrder" }
        });
        var slug = await factory.WithDatabaseAsync(db => db.Projects.Where(p => p.Title == title).Select(p => p.Slug).SingleAsync());

        using var anonymous = await factory.CreateBrowser().GetAsync($"/projeler/{slug}");
        var preview = await admin.GetDocumentAsync($"/projeler/{slug}");

        Assert.Equal(HttpStatusCode.NotFound, anonymous.StatusCode);
        Assert.NotNull(preview.QuerySelector(".preview-banner"));
    }

    [Fact]
    public async Task InvalidProject_ShowsFieldErrors_AndSavesNothing()
    {
        var admin = await factory.LoginAsAdminAsync();
        var before = await factory.WithDatabaseAsync(db => db.Projects.CountAsync());

        var form = await admin.GetDocumentAsync("/admin/projects/create");
        using var response = await admin.PostAsync("/admin/projects/create", new MultipartFormDataContent
        {
            { new StringContent(form.AntiforgeryToken()), "__RequestVerificationToken" },
            { new StringContent(""), "Input.Title" },
            { new StringContent(""), "Input.Summary" },
            { new StringContent("javascript:alert(1)"), "Input.LiveUrl" },
            { new StringContent("0"), "Input.SortOrder" }
        });
        var page = await response.ReadDocumentAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEmpty(page.QuerySelector("[data-valmsg-for='Input.Title']")!.TextContent.Trim());
        Assert.NotEmpty(page.QuerySelector("[data-valmsg-for='Input.LiveUrl']")!.TextContent.Trim());
        Assert.Equal(before, await factory.WithDatabaseAsync(db => db.Projects.CountAsync()));
    }

    [Fact]
    public async Task UploadingNonImage_IsRejected()
    {
        var admin = await factory.LoginAsAdminAsync();
        var form = await admin.GetDocumentAsync("/admin/projects/create");

        var html = new ByteArrayContent("<html><script>alert(1)</script></html>"u8.ToArray());
        html.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        using var response = await admin.PostAsync("/admin/projects/create", new MultipartFormDataContent
        {
            { new StringContent(form.AntiforgeryToken()), "__RequestVerificationToken" },
            { new StringContent("Kötü Dosya"), "Input.Title" },
            { new StringContent("Özet"), "Input.Summary" },
            { new StringContent("0"), "Input.SortOrder" },
            { html, "Cover", "evil.png" }
        });
        var page = await response.ReadDocumentAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("JPG, PNG, WEBP", page.QuerySelector("[data-valmsg-for='Cover']")!.TextContent, StringComparison.Ordinal);
    }
}
