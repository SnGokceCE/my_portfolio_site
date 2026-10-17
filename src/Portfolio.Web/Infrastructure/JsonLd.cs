using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Html;
using Portfolio.Application.Public;
using Portfolio.Application.SiteProfiles;

namespace Portfolio.Web.Infrastructure;

/// <summary>
/// schema.org yapılandırılmış verisi (JSON-LD). Google bu verilerle arama sonuçlarında
/// yazar, tarih, kapak görseli gibi zengin bilgiler gösterebilir.
/// </summary>
public static class JsonLd
{
    // Varsayılan kodlayıcı "<" karakterini < olarak kaçırır; böylece içerikteki "</script>"
    // betik bloğunu kapatamaz.
    private static readonly JsonSerializerOptions Options = new()
    {
        Encoder = JavaScriptEncoder.Default,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public static IHtmlContent Script(params object[] graph) =>
        new HtmlString($"<script type=\"application/ld+json\">{JsonSerializer.Serialize(
            new Dictionary<string, object> { ["@context"] = "https://schema.org", ["@graph"] = graph }, Options)}</script>");

    public static Dictionary<string, object?> Person(SiteProfileDto profile, string origin) => new()
    {
        ["@type"] = "Person",
        ["@id"] = $"{origin}/#person",
        ["name"] = profile.FullName,
        ["jobTitle"] = profile.Headline,
        ["description"] = string.IsNullOrWhiteSpace(profile.Bio) ? null : profile.Bio,
        ["url"] = origin + "/",
        ["email"] = profile.Email is null ? null : $"mailto:{profile.Email}",
        ["address"] = profile.Location is null ? null : new Dictionary<string, object> { ["@type"] = "PostalAddress", ["addressLocality"] = profile.Location },
        ["sameAs"] = new[] { profile.GitHubUrl, profile.LinkedInUrl }.OfType<string>().ToArray()
    };

    public static Dictionary<string, object?> WebSite(SiteProfileDto profile, string origin, string language) => new()
    {
        ["@type"] = "WebSite",
        ["@id"] = $"{origin}/#website",
        ["name"] = profile.FullName,
        ["url"] = origin + "/",
        ["inLanguage"] = language,
        ["author"] = new Dictionary<string, string> { ["@id"] = $"{origin}/#person" }
    };

    public static Dictionary<string, object?> Project(ProjectDetail project, string url, string origin, string language) => new()
    {
        ["@type"] = project.RepositoryUrl is null ? "CreativeWork" : "SoftwareSourceCode",
        ["name"] = project.Title,
        ["description"] = project.Summary,
        ["url"] = url,
        ["image"] = project.CoverImageUrl is null ? null : origin + project.CoverImageUrl,
        ["codeRepository"] = project.RepositoryUrl,
        ["keywords"] = project.Skills.Count == 0 ? null : string.Join(", ", project.Skills),
        ["dateCreated"] = project.StartedOn?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        ["datePublished"] = project.PublishedAt?.ToString("O", CultureInfo.InvariantCulture),
        ["dateModified"] = project.LastModified.ToString("O", CultureInfo.InvariantCulture),
        ["inLanguage"] = language,
        ["author"] = new Dictionary<string, string> { ["@id"] = $"{origin}/#person" }
    };

    public static Dictionary<string, object?> BlogPosting(PostDetail post, string url, string origin) => new()
    {
        ["@type"] = "BlogPosting",
        ["headline"] = post.Title,
        ["description"] = post.Excerpt,
        ["url"] = url,
        ["mainEntityOfPage"] = url,
        ["image"] = post.CoverImageUrl is null ? null : origin + post.CoverImageUrl,
        ["keywords"] = post.Tags.Count == 0 ? null : string.Join(", ", post.Tags),
        ["datePublished"] = post.PublishedAt?.ToString("O", CultureInfo.InvariantCulture),
        ["dateModified"] = post.LastModified.ToString("O", CultureInfo.InvariantCulture),
        ["wordCount"] = post.Body.WordCount,
        ["inLanguage"] = post.Language,
        ["author"] = new Dictionary<string, string> { ["@id"] = $"{origin}/#person" }
    };

    public static Dictionary<string, object?> Breadcrumbs(params (string Name, string Url)[] items) => new()
    {
        ["@type"] = "BreadcrumbList",
        ["itemListElement"] = items.Select((item, index) => new Dictionary<string, object>
        {
            ["@type"] = "ListItem",
            ["position"] = index + 1,
            ["name"] = item.Name,
            ["item"] = item.Url
        }).ToArray()
    };
}
