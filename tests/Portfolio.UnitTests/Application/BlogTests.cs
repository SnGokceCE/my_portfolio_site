using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Blog;
using Portfolio.Application.Common.Models;
using Portfolio.Application.Public;

namespace Portfolio.UnitTests.Application;

public sealed class BlogTests : IAsyncLifetime
{
    // Yazılar farklı günlerde yayımlanmış gibi görünsün diye saat 10 gün önceden başlar.
    private readonly TestServices _services = new(start: TestServices.Now.AddDays(-10));

    private IBlogPostService Admin => _services.Get<IBlogPostService>();

    private IPublicBlogQueries Public => _services.Get<IPublicBlogQueries>();

    public async Task InitializeAsync()
    {
        async Task Create(string title, string tags, bool published, int daysAgo)
        {
            _services.Clock.SetUtcNow(TestServices.Now.AddDays(-daysAgo));
            var result = await Admin.CreateAsync(
                new BlogPostInput
                {
                    Title = title,
                    Excerpt = $"{title} girişi",
                    Body = "## Bölüm 1\n\nMetin\n\n## Bölüm 2\n\nMetin",
                    Tags = tags,
                    IsPublished = published
                },
                cover: null);
            Assert.True(result.Succeeded);
        }

        await Create("Stajın İlk Haftası", "Staj, ASP.NET", published: true, daysAgo: 10);
        await Create("EF Core İpuçları", "ASP.NET, EF Core", published: true, daysAgo: 5);
        await Create("Docker ile Başlamak", "Docker", published: true, daysAgo: 1);
        await Create("Yarım Kalmış Taslak", "Staj", published: false, daysAgo: 0);
        _services.Clock.SetUtcNow(TestServices.Now);
    }

    [Fact]
    public async Task Create_RejectsDuplicateSlug()
    {
        var result = await Admin.CreateAsync(
            new BlogPostInput { Title = "Docker ile Başlamak", Excerpt = "x", Body = "y" }, cover: null);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains(result.Errors, e => e.Field == nameof(BlogPostInput.Slug));
    }

    [Fact]
    public async Task Catalog_ShowsPublishedNewestFirst_WithTagCounts()
    {
        var catalog = await Public.GetCatalogAsync(new BlogCatalogQuery());

        Assert.Equal(["Docker ile Başlamak", "EF Core İpuçları", "Stajın İlk Haftası"], catalog.Posts.Items.Select(p => p.Title));
        Assert.Equal(2, catalog.Tags.Single(t => t.Value == "ASP.NET").Count);
        Assert.Equal(1, catalog.Tags.Single(t => t.Value == "Staj").Count); // taslak sayılmaz
    }

    [Fact]
    public async Task Catalog_FiltersByTag()
    {
        var catalog = await Public.GetCatalogAsync(new BlogCatalogQuery(Tag: "ASP.NET"));

        Assert.Equal(2, catalog.Posts.TotalCount);
    }

    [Fact]
    public async Task Detail_HasPrevNextInDateOrder_AndRelatedByTags()
    {
        var detail = await Public.GetBySlugAsync("ef-core-ipuclari", includeDrafts: false);

        Assert.NotNull(detail);
        Assert.Equal("stajin-ilk-haftasi", detail.Previous?.Slug); // daha eski
        Assert.Equal("docker-ile-baslamak", detail.Next?.Slug);    // daha yeni
        Assert.Equal("Stajın İlk Haftası", detail.Related[0].Title); // ortak etiket: ASP.NET
        Assert.Equal(2, detail.Body.Headings.Count);
    }

    [Fact]
    public async Task Drafts_AreHiddenFromVisitors()
    {
        Assert.Null(await Public.GetBySlugAsync("yarim-kalmis-taslak", includeDrafts: false));
        Assert.NotNull(await Public.GetBySlugAsync("yarim-kalmis-taslak", includeDrafts: true));
        Assert.DoesNotContain(await Public.GetRecentAsync(10), p => p.Title == "Yarım Kalmış Taslak");
    }

    [Fact]
    public async Task Update_ReplacesTags()
    {
        var post = await _services.Db.BlogPosts.AsNoTracking().FirstAsync(p => p.Slug == "docker-ile-baslamak");

        await Admin.UpdateAsync(
            post.Id,
            new BlogPostInput { Title = post.Title, Excerpt = post.Excerpt, Body = post.Body, Tags = "Docker, DevOps", IsPublished = true },
            cover: null,
            removeCover: false);

        var updated = await _services.Db.BlogPosts.AsNoTracking().FirstAsync(p => p.Id == post.Id);
        Assert.Equal(["Docker", "DevOps"], updated.Tags);
    }

    public async Task DisposeAsync() => await _services.DisposeAsync();
}
