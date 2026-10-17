using Portfolio.Application.Experiences;
using Portfolio.Application.Public;
using Portfolio.Application.SiteProfiles;
using Portfolio.Application.Skills;

namespace Portfolio.Web.Models;

public sealed record HomeViewModel(
    SiteProfileDto Profile,
    IReadOnlyList<ProjectCard> FeaturedProjects,
    IReadOnlyList<ExperienceListItem> Experiences,
    IReadOnlyList<SkillListItem> Skills,
    IReadOnlyList<PostCard> RecentPosts);
