using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Application.Common.Models;
using Portfolio.Application.Common.Validation;
using Portfolio.Domain.Common;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Categories;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryListItem>> ListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryOption>> ListOptionsAsync(CancellationToken cancellationToken = default);

    Task<CategoryInput?> GetForEditAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<Guid>> CreateAsync(CategoryInput input, CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(Guid id, CategoryInput input, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

internal sealed class CategoryService(IApplicationDbContext db, IValidator<CategoryInput> validator) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryListItem>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.Categories
            .AsNoTracking()
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new CategoryListItem(c.Id, c.Name, c.Slug, c.SortOrder, c.Projects.Count))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CategoryOption>> ListOptionsAsync(CancellationToken cancellationToken = default) =>
        await db.Categories
            .AsNoTracking()
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new CategoryOption(c.Id, c.Name))
            .ToListAsync(cancellationToken);

    public async Task<CategoryInput?> GetForEditAsync(Guid id, CancellationToken cancellationToken = default) =>
        await db.Categories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryInput { Name = c.Name, Slug = c.Slug, SortOrder = c.SortOrder })
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<Result<Guid>> CreateAsync(CategoryInput input, CancellationToken cancellationToken = default)
    {
        var errors = await ValidateAsync(null, input, cancellationToken);
        if (errors is not null)
        {
            return Result.Invalid<Guid>(errors);
        }

        Category? category = null;
        errors = ValidationExtensions.TryApply(() => category = new Category(input.Name, input.Slug, input.SortOrder));
        if (errors is not null)
        {
            return Result.Invalid<Guid>(errors);
        }

        db.Categories.Add(category!);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(category!.Id);
    }

    public async Task<Result> UpdateAsync(Guid id, CategoryInput input, CancellationToken cancellationToken = default)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category is null)
        {
            return Result.NotFound();
        }

        var errors = await ValidateAsync(id, input, cancellationToken)
                     ?? ValidationExtensions.TryApply(() => category.Update(input.Name, input.Slug, input.SortOrder));
        if (errors is not null)
        {
            return Result.Invalid(errors);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category is null)
        {
            return Result.NotFound();
        }

        // Projeler silinmez; veritabanı ilişkisi (ON DELETE SET NULL) kategorisiz bırakır.
        db.Categories.Remove(category);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<IReadOnlyList<Error>?> ValidateAsync(Guid? id, CategoryInput input, CancellationToken cancellationToken)
    {
        var errors = await validator.ValidateInputAsync(input, cancellationToken);
        if (errors is not null)
        {
            return errors;
        }

        var slug = Slug.Generate(string.IsNullOrWhiteSpace(input.Slug) ? input.Name : input.Slug);
        var taken = await db.Categories.AnyAsync(c => c.Slug == slug && c.Id != id, cancellationToken);
        return taken
            ? [new Error($"'{slug}' adresi başka bir kategoride kullanılıyor.", nameof(CategoryInput.Slug))]
            : null;
    }
}
