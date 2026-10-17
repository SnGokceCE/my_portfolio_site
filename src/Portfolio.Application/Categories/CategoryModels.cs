using FluentValidation;
using Portfolio.Application.Common.Validation;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Categories;

public sealed record CategoryListItem(Guid Id, string Name, string Slug, int SortOrder, int ProjectCount);

public sealed record CategoryOption(Guid Id, string Name);

public sealed class CategoryInput
{
    public string Name { get; set; } = string.Empty;

    public string? Slug { get; set; }

    public int SortOrder { get; set; }

    public string? NameEn { get; set; }
}

public sealed class CategoryInputValidator : AbstractValidator<CategoryInput>
{
    public CategoryInputValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Category.NameMaxLength).WithName("Ad");
        RuleFor(x => x.Slug).OptionalSlug().WithName("Slug");
        RuleFor(x => x.NameEn).MaximumLength(Category.NameMaxLength).WithName("Ad (EN)");
    }
}
