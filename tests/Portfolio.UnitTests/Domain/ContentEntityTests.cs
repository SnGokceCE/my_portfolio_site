using Portfolio.Domain.Common;
using Portfolio.Domain.Entities;

namespace Portfolio.UnitTests.Domain;

public class ProjectTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void NewProject_IsDraft_WithSlugFromTitle()
    {
        var project = new Project("Hava Durumu Uygulaması", "Özet");

        Assert.False(project.IsPublished);
        Assert.Null(project.PublishedAt);
        Assert.Equal("hava-durumu-uygulamasi", project.Slug);
    }

    [Fact]
    public void Publish_KeepsFirstPublishDate()
    {
        var project = new Project("Proje", "Özet");

        project.Publish(Now);
        project.Unpublish();
        project.Publish(Now.AddDays(3));

        Assert.True(project.IsPublished);
        Assert.Equal(Now, project.PublishedAt);
    }

    [Fact]
    public void UpdateTimeline_RejectsEndBeforeStart()
    {
        var project = new Project("Proje", "Özet");

        Assert.Throws<DomainException>(() =>
            project.UpdateTimeline(new DateOnly(2026, 5, 1), new DateOnly(2026, 4, 1)));
    }

    [Fact]
    public void UpdateLinks_RejectsUnsafeUrls()
    {
        var project = new Project("Proje", "Özet");

        Assert.Throws<DomainException>(() => project.UpdateLinks("javascript:alert(1)", null));
    }

    [Fact]
    public void SetSkills_ReplacesAndDeduplicates()
    {
        var project = new Project("Proje", "Özet");
        var csharp = new Skill("C#");
        var sql = new Skill("SQL");

        project.SetSkills([csharp, csharp, sql]);
        Assert.Equal(2, project.Skills.Count);

        project.SetSkills([sql]);
        Assert.Equal([sql], project.Skills);
    }
}

public class ExperienceTests
{
    [Fact]
    public void WithoutEndDate_IsCurrent()
    {
        var experience = new Experience(ExperienceType.Internship, "Stajyer", "Şirket", new DateOnly(2026, 7, 1));

        Assert.True(experience.IsCurrent);
    }

    [Fact]
    public void Update_RejectsEndBeforeStart()
    {
        var experience = new Experience(ExperienceType.Work, "Geliştirici", "Şirket", new DateOnly(2026, 1, 1));

        Assert.Throws<DomainException>(() => experience.Update(
            ExperienceType.Work, "Geliştirici", "Şirket", null,
            new DateOnly(2026, 1, 1), new DateOnly(2025, 12, 31), null, null, 0));
    }

    [Fact]
    public void Update_RejectsUnknownType()
    {
        var experience = new Experience(ExperienceType.Work, "Geliştirici", "Şirket", new DateOnly(2026, 1, 1));

        Assert.Throws<DomainException>(() => experience.Update(
            (ExperienceType)99, "Geliştirici", "Şirket", null, new DateOnly(2026, 1, 1), null, null, null, 0));
    }
}

public class SkillAndCategoryTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Skill_RejectsLevelOutOfRange(int level)
    {
        Assert.Throws<DomainException>(() => new Skill("C#", level: level));
    }

    [Fact]
    public void Skill_BlankGroupBecomesNull()
    {
        Assert.Null(new Skill("C#", group: "   ").Group);
    }

    [Fact]
    public void Category_GeneratesSlugFromName()
    {
        Assert.Equal("veri-bilimi", new Category("Veri Bilimi").Slug);
    }
}
