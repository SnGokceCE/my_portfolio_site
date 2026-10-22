using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Portfolio.Web.Localization;

namespace Portfolio.UnitTests.Web;

public partial class LocalizationTests
{
    /// <summary>
    /// Kalite kapısı: ziyaretçi görünümlerinde L["..."] ile kullanılan her metnin en.json'da
    /// karşılığı olmalı. Yeni bir metin eklenip çevirisi unutulursa bu test kırılır.
    /// </summary>
    [Fact]
    public void EveryLocalizedStringHasAnEnglishTranslation()
    {
        var web = Path.Combine(RepositoryRoot(), "src", "Portfolio.Web");
        var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(
            File.ReadAllText(Path.Combine(web, "Localization", "Translations", "en.json")))!;

        var sources = Directory.EnumerateFiles(Path.Combine(web, "Views"), "*.cshtml", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(web, "Controllers"), "*.cs"));

        var keys = sources
            .SelectMany(file =>
            {
                var text = File.ReadAllText(file);
                var name = Path.GetFileName(file);
                // L.Plural(...) metninin hem çoğul hem de "#one" ekli tekil karşılığı bulunmalı.
                return KeyRegex().Matches(text).Select(m => (File: name, Key: m.Groups[1].Value))
                    .Concat(PluralKeyRegex().Matches(text).SelectMany(m => new[]
                    {
                        (File: name, Key: m.Groups[1].Value),
                        (File: name, Key: m.Groups[1].Value + PluralLocalizerExtensions.SingularSuffix)
                    }));
            })
            .ToList();

        var statusTexts = StatusRegex().Matches(File.ReadAllText(Path.Combine(web, "Models", "StatusViewModel.cs")))
            .Select(m => (File: "StatusViewModel.cs", Key: m.Groups[1].Value));

        var missing = keys.Concat(statusTexts)
            .Where(k => !translations.ContainsKey(k.Key))
            .Select(k => $"{k.File}: \"{k.Key}\"")
            .Distinct()
            .ToList();

        Assert.True(missing.Count == 0, "en.json'da eksik çeviriler:\n" + string.Join('\n', missing));
        Assert.True(keys.Count > 50, "Anahtar taraması beklenenden az sonuç buldu; desen bozulmuş olabilir.");
    }

    [Fact]
    public void EnglishTranslationsKeepFormatPlaceholders()
    {
        var web = Path.Combine(RepositoryRoot(), "src", "Portfolio.Web");
        var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(
            File.ReadAllText(Path.Combine(web, "Localization", "Translations", "en.json")))!;

        foreach (var (turkish, english) in translations)
        {
            var expected = PlaceholderRegex().Matches(turkish).Select(m => m.Value).Order().ToList();
            var actual = PlaceholderRegex().Matches(english).Select(m => m.Value).Order().ToList();
            Assert.True(expected.SequenceEqual(actual), $"Yer tutucular uyuşmuyor: \"{turkish}\" → \"{english}\"");
        }
    }

    [Theory]
    [InlineData(1, "1 project")]
    [InlineData(0, "0 projects")]
    [InlineData(3, "3 projects")]
    public void Plural_UsesSingularTranslationOnlyForOne(int count, string expected)
    {
        var english = new DictionaryLocalizer(new()
        {
            ["{0} proje"] = "{0} projects",
            ["{0} proje" + PluralLocalizerExtensions.SingularSuffix] = "{0} project"
        });

        Assert.Equal(expected, english.Plural(count, "{0} proje", count).Value);
    }

    [Fact]
    public void Plural_WithoutTranslation_FallsBackToTurkishText()
    {
        var turkish = new DictionaryLocalizer([]);

        Assert.Equal("1 proje", turkish.Plural(1, "{0} proje", 1).Value);
    }

    [Theory]
    [InlineData("", "/projeler", "/projeler", "/en/projeler")]
    [InlineData("", "/", "/", "/en")]
    [InlineData("/en", "/blog/yazi", "/blog/yazi", "/en/blog/yazi")]
    [InlineData("/en", "/", "/", "/en")]
    public void AlternateUrls_SwapLanguagePrefix(string pathBase, string path, string turkish, string english)
    {
        var context = new DefaultHttpContext();
        context.Request.PathBase = pathBase;
        context.Request.Path = path;

        var urls = SiteCultures.AlternateUrls(context.Request);

        Assert.Equal(turkish, urls.Turkish);
        Assert.Equal(english, urls.English);
    }

    [Fact]
    public void AlternateUrls_KeepQueryString()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/projeler";
        context.Request.QueryString = new QueryString("?kategori=web");

        Assert.Equal("/en/projeler?kategori=web", SiteCultures.AlternateUrls(context.Request).English);
    }

    internal static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Portfolio.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Depo kökü bulunamadı.");
    }

    [GeneratedRegex("""(?:\bL|localizer)\["((?:[^"\\]|\\.)+)"[,\]]""")]
    private static partial Regex KeyRegex();

    [GeneratedRegex("""\bL\.Plural\([^,]+,\s*"((?:[^"\\]|\\.)+)"[,)]""")]
    private static partial Regex PluralKeyRegex();

    [GeneratedRegex("""=> "([^"]+)",?""")]
    private static partial Regex StatusRegex();

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex PlaceholderRegex();

    /// <summary>JsonStringLocalizer gibi davranır: çevirisi olmayan anahtar kendisiyle döner.</summary>
    private sealed class DictionaryLocalizer(Dictionary<string, string> translations) : IStringLocalizer
    {
        public LocalizedString this[string name] =>
            translations.TryGetValue(name, out var value)
                ? new LocalizedString(name, value)
                : new LocalizedString(name, name, resourceNotFound: true);

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                var format = this[name];
                return new LocalizedString(name, string.Format(CultureInfo.InvariantCulture, format.Value, arguments), format.ResourceNotFound);
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }
}
