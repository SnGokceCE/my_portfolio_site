using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
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
            .SelectMany(file => KeyRegex().Matches(File.ReadAllText(file)).Select(m => (File: Path.GetFileName(file), Key: m.Groups[1].Value)))
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

    [GeneratedRegex("""=> "([^"]+)",?""")]
    private static partial Regex StatusRegex();

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex PlaceholderRegex();
}
