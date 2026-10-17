using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Portfolio.Application;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Application.Public;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Content;
using Portfolio.Infrastructure.Persistence;

namespace Portfolio.UnitTests.Application;

public sealed class PublicProjectQueriesTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 10, 17, 0, 0, 0, TimeSpan.Zero);

    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;

    public PublicProjectQueriesTests()
    {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString();
        services.AddApplication();
        services.AddMemoryCache();
        services.AddSingleton<IMarkdownRenderer, MarkdownRenderer>();
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(databaseName));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddSingleton<TimeProvider>(new FakeTimeProvider(Now));

        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
    }

    private IPublicProjectQueries Queries => _scope.ServiceProvider.GetRequiredService<IPublicProjectQueries>();

    private ApplicationDbContext Db => _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    public async Task InitializeAsync()
    {
        var web = new Category("Web");
        var mobile = new Category("Mobil");
        var csharp = new Skill("C#");
        var kotlin = new Skill("Kotlin");

        Project Make(string title, Category category, Skill skill, int order, bool published = true, bool featured = false)
        {
            var p = new Project(title, $"{title} özeti");
            p.UpdateDisplay(category.Id, featured, order);
            p.SetSkills([skill]);
            p.UpdateDetails(title, null, $"{title} özeti", "## Problem\n\nMetin\n\n## Çözüm\n\nMetin");
            if (published)
            {
                p.Publish(Now);
            }

            return p;
        }

        Db.AddRange(web, mobile, csharp, kotlin);
        Db.Projects.AddRange(
            Make("Blog Motoru", web, csharp, order: 1),
            Make("Görev Yöneticisi", web, csharp, order: 2, featured: true),
            Make("Hava Durumu", mobile, kotlin, order: 3),
            Make("Gizli Taslak", web, csharp, order: 0, published: false));
        await Db.SaveChangesAsync();
    }

    [Fact]
    public async Task Catalog_ListsOnlyPublished_InDisplayOrder()
    {
        var catalog = await Queries.GetCatalogAsync(new ProjectCatalogQuery());

        Assert.Equal(["Blog Motoru", "Görev Yöneticisi", "Hava Durumu"], catalog.Projects.Items.Select(p => p.Title));
    }

    [Fact]
    public async Task Catalog_FilterOptions_CountOnlyPublishedProjects()
    {
        var catalog = await Queries.GetCatalogAsync(new ProjectCatalogQuery());

        Assert.Equal(2, catalog.Categories.Single(c => c.Value == "web").Count);
        Assert.Equal(2, catalog.Technologies.Single(t => t.Value == "C#").Count);
    }

    [Theory]
    [InlineData("mobil", null, "Hava Durumu")]
    [InlineData(null, "Kotlin", "Hava Durumu")]
    [InlineData("web", "C#", "Blog Motoru")]
    public async Task Catalog_FiltersByCategoryAndTechnology(string? category, string? technology, string expectedFirst)
    {
        var catalog = await Queries.GetCatalogAsync(new ProjectCatalogQuery(category, technology));

        Assert.Equal(expectedFirst, catalog.Projects.Items[0].Title);
        Assert.DoesNotContain(catalog.Projects.Items, p => p.Title == "Gizli Taslak");
    }

    [Fact]
    public async Task Featured_PutsFeaturedProjectFirst()
    {
        var featured = await Queries.GetFeaturedAsync(2);

        Assert.Equal("Görev Yöneticisi", featured[0].Title);
        Assert.Equal(2, featured.Count);
    }

    [Fact]
    public async Task BySlug_HidesDraftsFromVisitors_ButShowsToAdmin()
    {
        Assert.Null(await Queries.GetBySlugAsync("gizli-taslak", includeDrafts: false));

        var preview = await Queries.GetBySlugAsync("gizli-taslak", includeDrafts: true);
        Assert.NotNull(preview);
        Assert.False(preview.IsPublished);
        Assert.Null(preview.Previous);
        Assert.Null(preview.Next);
    }

    [Fact]
    public async Task BySlug_ReturnsRenderedBody_PrevNext_AndRelated()
    {
        var detail = await Queries.GetBySlugAsync("gorev-yoneticisi", includeDrafts: false);

        Assert.NotNull(detail);
        Assert.Equal(2, detail.Body.Headings.Count);
        Assert.Equal("blog-motoru", detail.Previous?.Slug);
        Assert.Equal("hava-durumu", detail.Next?.Slug);
        // Aynı kategorideki proje ilgili projelerde önce gelir; taslak hiç görünmez.
        Assert.Equal("Blog Motoru", detail.Related[0].Title);
        Assert.DoesNotContain(detail.Related, p => p.Title == "Gizli Taslak");
    }

    public Task DisposeAsync()
    {
        _scope.Dispose();
        return _provider.DisposeAsync().AsTask();
    }
}
