using Portfolio.Application.SiteProfiles;
using Portfolio.Domain.Common;
using Portfolio.Domain.Entities;

namespace Portfolio.UnitTests.Domain;

public class SiteProfileTests
{
    [Fact]
    public void Constructor_TrimsValues()
    {
        var profile = new SiteProfile("  Ada Lovelace ", " Matematikçi ", " Bio ");

        Assert.Equal("Ada Lovelace", profile.FullName);
        Assert.Equal("Matematikçi", profile.Headline);
        Assert.Equal("Bio", profile.Bio);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsEmptyFullName(string fullName)
    {
        var exception = Assert.Throws<DomainException>(() => new SiteProfile(fullName, "Unvan", ""));

        Assert.Contains(nameof(SiteProfile.FullName), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_RejectsTooLongFullName()
    {
        var tooLong = new string('a', SiteProfile.FullNameMaxLength + 1);

        Assert.Throws<DomainException>(() => new SiteProfile(tooLong, "Unvan", ""));
    }

    [Fact]
    public void UpdateContact_TurnsBlankValuesIntoNull()
    {
        var profile = new SiteProfile("Ada", "Unvan", "");

        profile.UpdateContact(" ", "", null, "   ");

        Assert.Null(profile.Location);
        Assert.Null(profile.Email);
        Assert.Null(profile.GitHubUrl);
        Assert.Null(profile.LinkedInUrl);
    }

    [Theory]
    [InlineData("github.com/ada")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://example.com")]
    public void UpdateContact_RejectsNonHttpUrls(string url)
    {
        var profile = new SiteProfile("Ada", "Unvan", "");

        Assert.Throws<DomainException>(() => profile.UpdateContact(null, null, url, null));
    }

    [Fact]
    public void UpdateContact_AcceptsHttpsUrls()
    {
        var profile = new SiteProfile("Ada", "Unvan", "");

        profile.UpdateContact("İstanbul", "ada@example.com", "https://github.com/ada", "https://linkedin.com/in/ada");

        Assert.Equal("https://github.com/ada", profile.GitHubUrl);
        Assert.Equal("https://linkedin.com/in/ada", profile.LinkedInUrl);
    }

    [Theory]
    [InlineData("Ada Lovelace", "AL")]
    [InlineData("ada", "A")]
    [InlineData("Ada Byron Lovelace", "AB")]
    public void Dto_Initials_UsesFirstTwoWords(string fullName, string expected)
    {
        var dto = SiteProfileDto.Empty with { FullName = fullName };

        Assert.Equal(expected, dto.Initials);
    }
}
