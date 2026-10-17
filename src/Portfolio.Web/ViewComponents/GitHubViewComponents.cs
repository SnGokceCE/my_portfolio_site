using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Application.SiteProfiles;

namespace Portfolio.Web.ViewComponents;

/// <summary>Ana sayfadaki "GitHub'da" bölümü. Profilde GitHub adresi yoksa ya da veri alınamazsa hiçbir şey çizmez.</summary>
public class GitHubActivityViewComponent(ISiteProfileService profiles, IGitHubService github) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var profile = await profiles.GetAsync(HttpContext.RequestAborted);
        if (!GitHubUrl.TryParseUser(profile.GitHubUrl, out var username))
        {
            return Content(string.Empty);
        }

        var activity = await github.GetProfileAsync(username, HttpContext.RequestAborted);
        return activity is null || activity.RecentRepositories.Count == 0 ? Content(string.Empty) : View(activity);
    }
}

/// <summary>Proje detayında, kaynak kod adresi bir GitHub deposuysa yıldız/çatal/son güncelleme kartı.</summary>
public class RepositoryStatsViewComponent(IGitHubService github) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(string? repositoryUrl)
    {
        if (!GitHubUrl.TryParseRepository(repositoryUrl, out var owner, out var name))
        {
            return Content(string.Empty);
        }

        var repository = await github.GetRepositoryAsync(owner, name, HttpContext.RequestAborted);
        return repository is null ? Content(string.Empty) : View(repository);
    }
}

/// <summary>GitHub'ın dil renkleri (linguist); bilinmeyen diller için nötr bir renk.</summary>
public static class LanguageColors
{
    private static readonly Dictionary<string, string> Colors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["C#"] = "#178600",
        ["TypeScript"] = "#3178c6",
        ["JavaScript"] = "#f1e05a",
        ["Python"] = "#3572A5",
        ["Java"] = "#b07219",
        ["Kotlin"] = "#A97BFF",
        ["Swift"] = "#F05138",
        ["Go"] = "#00ADD8",
        ["Rust"] = "#dea584",
        ["C++"] = "#f34b7d",
        ["C"] = "#555555",
        ["HTML"] = "#e34c26",
        ["CSS"] = "#563d7c",
        ["SCSS"] = "#c6538c",
        ["Vue"] = "#41b883",
        ["Dart"] = "#00B4AB",
        ["PHP"] = "#4F5D95",
        ["Ruby"] = "#701516",
        ["Shell"] = "#89e051",
        ["Jupyter Notebook"] = "#DA5B0B",
        ["Dockerfile"] = "#384d54"
    };

    public static string For(string? language) =>
        language is not null && Colors.TryGetValue(language, out var color) ? color : "#8b8b9e";
}
