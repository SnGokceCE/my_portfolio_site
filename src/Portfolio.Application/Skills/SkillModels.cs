using FluentValidation;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Skills;

public sealed record SkillListItem(Guid Id, string Name, string? Group, int Level, int SortOrder, int ProjectCount);

public sealed record SkillOption(Guid Id, string Name, string? Group);

public sealed class SkillInput
{
    public string Name { get; set; } = string.Empty;

    public string? Group { get; set; }

    public int Level { get; set; } = 3;

    public int SortOrder { get; set; }

    public string? NameEn { get; set; }

    public string? GroupEn { get; set; }
}

public sealed class SkillInputValidator : AbstractValidator<SkillInput>
{
    public SkillInputValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Skill.NameMaxLength).WithName("Ad");
        RuleFor(x => x.Group).MaximumLength(Skill.GroupMaxLength).WithName("Grup");
        RuleFor(x => x.Level).InclusiveBetween(Skill.MinLevel, Skill.MaxLevel).WithName("Seviye");
        RuleFor(x => x.NameEn).MaximumLength(Skill.NameMaxLength).WithName("Ad (EN)");
        RuleFor(x => x.GroupEn).MaximumLength(Skill.GroupMaxLength).WithName("Grup (EN)");
    }
}
