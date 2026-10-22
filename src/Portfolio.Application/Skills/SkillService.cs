using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Application.Common.Models;
using Portfolio.Application.Common.Validation;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Skills;

public interface ISkillService
{
    Task<IReadOnlyList<SkillListItem>> ListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SkillOption>> ListOptionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ListGroupsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ListEnglishGroupsAsync(CancellationToken cancellationToken = default);

    Task<SkillInput?> GetForEditAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<Guid>> CreateAsync(SkillInput input, CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(Guid id, SkillInput input, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

internal sealed class SkillService(
    IApplicationDbContext db,
    IValidator<SkillInput> validator,
    ICurrentLanguage language) : ISkillService
{
    // İngilizce sayfada çevirisi olan ad/grup İngilizce, olmayan Türkçe aslıyla gelir (yönetim paneli hep Türkçe).
    public async Task<IReadOnlyList<SkillListItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        var english = language.IsEnglish;
        return await Ordered(db.Skills.AsNoTracking())
            .Select(s => new SkillListItem(
                s.Id,
                english && s.NameEn != null ? s.NameEn : s.Name,
                english && s.GroupEn != null ? s.GroupEn : s.Group,
                s.Level,
                s.SortOrder,
                s.Projects.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SkillOption>> ListOptionsAsync(CancellationToken cancellationToken = default) =>
        await Ordered(db.Skills.AsNoTracking())
            .Select(s => new SkillOption(s.Id, s.Name, s.Group))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> ListGroupsAsync(CancellationToken cancellationToken = default) =>
        await db.Skills
            .AsNoTracking()
            .Where(s => s.Group != null)
            .Select(s => s.Group!)
            .Distinct()
            .OrderBy(g => g)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> ListEnglishGroupsAsync(CancellationToken cancellationToken = default) =>
        await db.Skills
            .AsNoTracking()
            .Where(s => s.GroupEn != null)
            .Select(s => s.GroupEn!)
            .Distinct()
            .OrderBy(g => g)
            .ToListAsync(cancellationToken);

    public async Task<SkillInput?> GetForEditAsync(Guid id, CancellationToken cancellationToken = default) =>
        await db.Skills
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new SkillInput
            {
                Name = s.Name,
                Group = s.Group,
                Level = s.Level,
                SortOrder = s.SortOrder,
                NameEn = s.NameEn,
                GroupEn = s.GroupEn
            })
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<Result<Guid>> CreateAsync(SkillInput input, CancellationToken cancellationToken = default)
    {
        var errors = await ValidateAsync(null, input, cancellationToken);
        Skill? skill = null;
        errors ??= ValidationExtensions.TryApply(() =>
        {
            skill = new Skill(input.Name, input.Group, input.Level, input.SortOrder);
            skill.UpdateEnglish(input.NameEn, input.GroupEn);
        });
        if (errors is not null)
        {
            return Result.Invalid<Guid>(errors);
        }

        db.Skills.Add(skill!);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(skill!.Id);
    }

    public async Task<Result> UpdateAsync(Guid id, SkillInput input, CancellationToken cancellationToken = default)
    {
        var skill = await db.Skills.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (skill is null)
        {
            return Result.NotFound();
        }

        var errors = await ValidateAsync(id, input, cancellationToken)
                     ?? ValidationExtensions.TryApply(() =>
                     {
                         skill.Update(input.Name, input.Group, input.Level, input.SortOrder);
                         skill.UpdateEnglish(input.NameEn, input.GroupEn);
                     });
        if (errors is not null)
        {
            return Result.Invalid(errors);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var skill = await db.Skills.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (skill is null)
        {
            return Result.NotFound();
        }

        db.Skills.Remove(skill);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static IQueryable<Skill> Ordered(IQueryable<Skill> query) =>
        query.OrderBy(s => s.Group == null).ThenBy(s => s.Group).ThenBy(s => s.SortOrder).ThenBy(s => s.Name);

    private async Task<IReadOnlyList<Error>?> ValidateAsync(Guid? id, SkillInput input, CancellationToken cancellationToken)
    {
        var errors = await validator.ValidateInputAsync(input, cancellationToken);
        if (errors is not null)
        {
            return errors;
        }

        // Bu ifade SQL'e (lower(name) = ...) çevrilir; kültür/StringComparison uyarıları burada geçersiz.
#pragma warning disable CA1304, CA1311, CA1862
        var name = input.Name.Trim().ToLower();
        var taken = await db.Skills.AnyAsync(s => s.Name.ToLower() == name && s.Id != id, cancellationToken);
#pragma warning restore CA1304, CA1311, CA1862
        return taken ? [new Error("Bu adda bir yetenek zaten var.", nameof(SkillInput.Name))] : null;
    }
}
