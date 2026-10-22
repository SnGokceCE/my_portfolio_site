using Portfolio.Application.Categories;
using Portfolio.Application.Common.Models;
using Portfolio.Application.Projects;
using Portfolio.Application.Skills;

namespace Portfolio.Web.Areas.Admin.Models;

/// <summary>Ekleme ve düzenleme için ortak form modeli. <see cref="Id"/> boşsa yeni kayıt demektir.</summary>
public class FormViewModel<TInput> where TInput : new()
{
    public Guid? Id { get; set; }

    public TInput Input { get; set; } = new();

    public bool IsNew => Id is null;
}

public sealed class SkillFormViewModel : FormViewModel<SkillInput>
{
    public IReadOnlyList<string> Groups { get; set; } = [];

    public IReadOnlyList<string> EnglishGroups { get; set; } = [];
}

public sealed class ProjectFormViewModel : FormViewModel<ProjectInput>
{
    public string? CoverImageUrl { get; set; }

    public IFormFile? Cover { get; set; }

    public bool RemoveCover { get; set; }

    public IReadOnlyList<CategoryOption> Categories { get; set; } = [];

    public IReadOnlyList<SkillOption> Skills { get; set; } = [];
}

public sealed record ProjectIndexViewModel(
    PagedList<ProjectListItem> Projects,
    ProjectQuery Query,
    IReadOnlyList<CategoryOption> Categories);
