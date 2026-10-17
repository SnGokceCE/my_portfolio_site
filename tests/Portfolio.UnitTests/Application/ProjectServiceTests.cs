using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Portfolio.Application;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Application.Common.Models;
using Portfolio.Application.Projects;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Persistence;

namespace Portfolio.UnitTests.Application;

/// <summary>
/// ProjectService'i gerçek DI kurulumu ve bellek içi (InMemory) veritabanıyla test eder.
/// </summary>
public sealed class ProjectServiceTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly FakeStorage _storage = new();

    public ProjectServiceTests()
    {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString();
        services.AddApplication();
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(databaseName));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddSingleton<IFileStorage>(_storage);
        services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(2026, 10, 17, 0, 0, 0, TimeSpan.Zero)));

        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
    }

    private IProjectService Service => _scope.ServiceProvider.GetRequiredService<IProjectService>();

    private ApplicationDbContext Db => _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    [Fact]
    public async Task Create_GeneratesSlug_AndAttachesSkills()
    {
        var skill = new Skill("C#");
        Db.Skills.Add(skill);
        await Db.SaveChangesAsync();

        var result = await Service.CreateAsync(
            new ProjectInput { Title = "Görev Yöneticisi", Summary = "Özet", SkillIds = [skill.Id] }, cover: null);

        Assert.True(result.Succeeded);
        var project = await Db.Projects.Include(p => p.Skills).SingleAsync();
        Assert.Equal("gorev-yoneticisi", project.Slug);
        Assert.Single(project.Skills);
    }

    [Fact]
    public async Task Create_RejectsDuplicateSlug_OnSlugField()
    {
        await Service.CreateAsync(new ProjectInput { Title = "Aynı Ad", Summary = "Özet" }, null);

        var result = await Service.CreateAsync(new ProjectInput { Title = "Aynı Ad", Summary = "Başka" }, null);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains(result.Errors, e => e.Field == nameof(ProjectInput.Slug));
    }

    [Fact]
    public async Task Create_ReturnsValidationErrors_WithoutSaving()
    {
        var result = await Service.CreateAsync(new ProjectInput { Title = "", Summary = "" }, null);

        Assert.False(result.Succeeded);
        Assert.Empty(Db.Projects);
    }

    [Fact]
    public async Task Update_ReplacingCover_DeletesPreviousFile()
    {
        var created = await Service.CreateAsync(new ProjectInput { Title = "Proje", Summary = "Özet" }, FakeStorage.Image());
        var firstCover = (await Db.Projects.SingleAsync()).CoverImageUrl;

        var result = await Service.UpdateAsync(
            created.Value, new ProjectInput { Title = "Proje", Summary = "Özet" }, FakeStorage.Image(), removeCover: false);

        Assert.True(result.Succeeded);
        Assert.Contains(firstCover, _storage.Deleted);
        Assert.NotEqual(firstCover, (await Db.Projects.SingleAsync()).CoverImageUrl);
    }

    [Fact]
    public async Task Update_UnknownId_ReturnsNotFound()
    {
        var result = await Service.UpdateAsync(Guid.NewGuid(), new ProjectInput(), null, false);

        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task List_FiltersBySearchAndStatus()
    {
        await Service.CreateAsync(new ProjectInput { Title = "Hava Durumu", Summary = "API ile", IsPublished = true }, null);
        await Service.CreateAsync(new ProjectInput { Title = "Not Defteri", Summary = "Taslak fikir" }, null);

        var published = await Service.ListAsync(new ProjectQuery(Status: ProjectStatusFilter.Published));
        var search = await Service.ListAsync(new ProjectQuery(Search: "defter"));

        Assert.Equal("Hava Durumu", Assert.Single(published.Items).Title);
        Assert.Equal("Not Defteri", Assert.Single(search.Items).Title);
    }

    [Fact]
    public async Task Delete_RemovesProjectAndCover()
    {
        var created = await Service.CreateAsync(new ProjectInput { Title = "Proje", Summary = "Özet" }, FakeStorage.Image());
        var cover = (await Db.Projects.SingleAsync()).CoverImageUrl;

        var result = await Service.DeleteAsync(created.Value);

        Assert.True(result.Succeeded);
        Assert.Empty(Db.Projects);
        Assert.Contains(cover, _storage.Deleted);
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    private sealed class FakeStorage : IFileStorage
    {
        private int _counter;

        public List<string?> Deleted { get; } = [];

        public static ImageUpload Image() => new(new MemoryStream([1, 2, 3]), "kapak.png", 3);

        public Task<Result<string>> SaveImageAsync(ImageUpload image, string folder, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success($"/uploads/{folder}/{++_counter}.png"));

        public Task DeleteAsync(string? url, CancellationToken cancellationToken = default)
        {
            Deleted.Add(url);
            return Task.CompletedTask;
        }
    }
}
