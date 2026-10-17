using Portfolio.Application.Experiences;
using Portfolio.Application.Projects;
using Portfolio.Application.SiteProfiles;

namespace Portfolio.UnitTests.Application;

public class ValidatorTests
{
    [Fact]
    public void ProjectInput_Valid()
    {
        var result = new ProjectInputValidator().Validate(new ProjectInput
        {
            Title = "Proje",
            Summary = "Özet",
            RepositoryUrl = "https://github.com/kullanici/proje"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ProjectInput_ReportsEachInvalidField()
    {
        var result = new ProjectInputValidator().Validate(new ProjectInput
        {
            Title = "",
            Summary = "",
            Slug = "Büyük Harf",
            LiveUrl = "ftp://ornek.com",
            StartedOn = new DateOnly(2026, 6, 1),
            CompletedOn = new DateOnly(2026, 5, 1)
        });

        var fields = result.Errors.Select(e => e.PropertyName).ToHashSet();
        Assert.Contains(nameof(ProjectInput.Title), fields);
        Assert.Contains(nameof(ProjectInput.Summary), fields);
        Assert.Contains(nameof(ProjectInput.Slug), fields);
        Assert.Contains(nameof(ProjectInput.LiveUrl), fields);
        Assert.Contains(nameof(ProjectInput.CompletedOn), fields);
    }

    [Fact]
    public void ExperienceInput_RequiresStartDate()
    {
        var result = new ExperienceInputValidator().Validate(new ExperienceInput
        {
            Title = "Stajyer",
            Organization = "Şirket"
        });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ExperienceInput.StartDate));
    }

    [Theory]
    [InlineData("gecersiz-eposta", false)]
    [InlineData("ben@ornek.com", true)]
    [InlineData("", true)]
    public void SiteProfileInput_ValidatesOptionalEmail(string email, bool expected)
    {
        var result = new SiteProfileInputValidator().Validate(new SiteProfileInput
        {
            FullName = "Ad Soyad",
            Headline = "Unvan",
            Email = email
        });

        Assert.Equal(expected, result.IsValid);
    }
}
