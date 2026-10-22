using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Application.Experiences;
using Portfolio.Application.Public;
using Portfolio.Application.SiteProfiles;
using Portfolio.Domain.Entities;

namespace Portfolio.UnitTests.Application;

/// <summary>İngilizce sayfada çevrilmiş alanlar İngilizce, çevrilmemişler Türkçe aslıyla gelmeli.</summary>
public sealed class LanguageAwareQueriesTests : IAsyncLifetime
{
    private readonly TestServices _english = new(s => s.AddSingleton<ICurrentLanguage>(new FixedLanguage(english: true)));

    public async Task InitializeAsync()
    {
        var category = new Category("Web Geliştirme");
        category.SetEnglishName("Web Development");

        var translated = new Project("Görev Yöneticisi", "Türkçe özet");
        translated.UpdateDetails("Görev Yöneticisi", null, "Türkçe özet", "## Türkçe");
        translated.UpdateEnglish("Task Manager", "English summary", "## English");
        translated.UpdateDisplay(category.Id, false, 0);
        translated.Publish(TestServices.Now);

        var untranslated = new Project("Hava Durumu", "Sadece Türkçe");
        untranslated.UpdateDetails("Hava Durumu", null, "Sadece Türkçe", "## Türkçe içerik");
        untranslated.UpdateDisplay(null, false, 1);
        untranslated.Publish(TestServices.Now);

        var profile = new SiteProfile("Ada", "Yazılım geliştirici", "Türkçe biyografi");
        profile.UpdateEnglish("Software developer", null);

        var experience = new Experience(ExperienceType.Internship, "Yazılım Stajyeri", "Şirket", new DateOnly(2026, 7, 1));
        experience.UpdateEnglish("Software Intern", null);

        _english.Db.AddRange(category, translated, untranslated, profile, experience);
        await _english.Db.SaveChangesAsync();
    }

    [Fact]
    public async Task ProjectCards_PreferEnglish_FallBackToTurkish()
    {
        var catalog = await _english.Get<IPublicProjectQueries>().GetCatalogAsync(new ProjectCatalogQuery());

        Assert.Equal(["Task Manager", "Hava Durumu"], catalog.Projects.Items.Select(p => p.Title));
        Assert.Equal("Web Development", catalog.Projects.Items[0].CategoryName);
        Assert.Equal("Web Development", Assert.Single(catalog.Categories).Label);
        Assert.Equal("web-gelistirme", catalog.Categories[0].Value); // adres değişmez
    }

    [Fact]
    public async Task ProjectDetail_FlagsMissingTranslation()
    {
        var queries = _english.Get<IPublicProjectQueries>();

        var translated = await queries.GetBySlugAsync("gorev-yoneticisi", includeDrafts: false);
        var untranslated = await queries.GetBySlugAsync("hava-durumu", includeDrafts: false);

        Assert.False(translated!.IsTranslationMissing);
        Assert.Contains("English", translated.Body.Html, StringComparison.Ordinal);
        Assert.True(untranslated!.IsTranslationMissing);
        Assert.Contains("Türkçe içerik", untranslated.Body.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProfileAndExperience_UseEnglishWhereAvailable()
    {
        var profile = await _english.Get<ISiteProfileService>().GetAsync();
        var experience = Assert.Single(await _english.Get<IExperienceService>().ListAsync());

        Assert.Equal("Software developer", profile.Headline);
        Assert.Equal("Türkçe biyografi", profile.Bio); // çevrilmemiş → Türkçe
        Assert.Equal("Software Intern", experience.Title);
    }

    [Fact]
    public async Task EditForm_AlwaysReturnsRawValuesForBothLanguages()
    {
        var input = await _english.Get<ISiteProfileService>().GetForEditAsync();

        Assert.Equal("Yazılım geliştirici", input.Headline);
        Assert.Equal("Software developer", input.HeadlineEn);
    }

    [Fact]
    public async Task Skills_ShowEnglishNameAndGroup_AndFilterAcceptsEnglishName()
    {
        var skill = new Skill("Makine öğrenmesi", "Yapay Zeka ve Veri", level: 3);
        skill.UpdateEnglish("Machine learning", "Artificial Intelligence & Data");
        var project = await _english.Db.Projects.FirstAsync(p => p.Slug == "gorev-yoneticisi");
        project.SetSkills([skill]);
        _english.Db.Skills.Add(skill);
        await _english.Db.SaveChangesAsync();

        var listed = (await _english.Get<Portfolio.Application.Skills.ISkillService>().ListAsync()).Single(s => s.Id == skill.Id);
        var catalog = await _english.Get<IPublicProjectQueries>().GetCatalogAsync(new ProjectCatalogQuery(Technology: "Machine learning"));

        Assert.Equal("Machine learning", listed.Name);
        Assert.Equal("Artificial Intelligence & Data", listed.Group);
        Assert.Equal("Task Manager", Assert.Single(catalog.Projects.Items).Title);
        Assert.Equal(["Machine learning"], catalog.Projects.Items[0].Skills);
        Assert.Contains(catalog.Technologies, t => t.Value == "Machine learning");
    }

    public Task DisposeAsync() => _english.DisposeAsync().AsTask();

    private sealed class FixedLanguage(bool english) : ICurrentLanguage
    {
        public bool IsEnglish => english;
    }
}
