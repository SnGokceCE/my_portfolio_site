using Portfolio.Domain.Common;

namespace Portfolio.UnitTests.Domain;

public class SlugTests
{
    [Theory]
    [InlineData("Mobil Uygulamam", "mobil-uygulamam")]
    [InlineData("Çiçekçi Şükrü'nün Gölgeli Işığı", "cicekci-sukru-nun-golgeli-isigi")]
    [InlineData("İSTANBUL ĞÜŞİÖÇ", "istanbul-gusioc")]
    [InlineData("  --Merhaba,   Dünya!--  ", "merhaba-dunya")]
    [InlineData("C# & .NET 10", "c-net-10")]
    [InlineData("Café résumé", "cafe-resume")]
    public void Generate_ProducesReadableAsciiSlug(string input, string expected)
    {
        Assert.Equal(expected, Slug.Generate(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!!")]
    public void Generate_ReturnsEmptyForTextWithoutLettersOrDigits(string? input)
    {
        Assert.Equal(string.Empty, Slug.Generate(input));
    }

    [Fact]
    public void Generate_RespectsMaxLength_AndDoesNotEndWithDash()
    {
        var slug = Slug.Generate(string.Join(" ", Enumerable.Repeat("kelime", 60)));

        Assert.True(slug.Length <= Slug.MaxLength);
        Assert.False(slug.EndsWith('-'));
    }

    [Theory]
    [InlineData("benim-projem", true)]
    [InlineData("proje2", true)]
    [InlineData("Benim-Projem", false)]
    [InlineData("benim projem", false)]
    [InlineData("-baslik", false)]
    [InlineData("", false)]
    public void IsValid_AcceptsOnlyNormalizedSlugs(string slug, bool expected)
    {
        Assert.Equal(expected, Slug.IsValid(slug));
    }

    [Fact]
    public void FromInputOrText_PrefersExplicitSlug()
    {
        Assert.Equal("ozel-adres", Slug.FromInputOrText("Özel Adres", "Başlık"));
        Assert.Equal("baslik", Slug.FromInputOrText(null, "Başlık"));
    }

    [Fact]
    public void FromInputOrText_ThrowsWhenNothingUsable()
    {
        Assert.Throws<DomainException>(() => Slug.FromInputOrText("", "???"));
    }
}
