using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Application.Common.Models;
using Portfolio.Application.Common.Validation;
using Portfolio.Application.Projects;
using Portfolio.Domain.Common;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Blog;

public interface IBlogPostService
{
    Task<PagedList<BlogPostListItem>> ListAsync(BlogPostQuery query, CancellationToken cancellationToken = default);

    Task<BlogPostEditModel?> GetForEditAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<Guid>> CreateAsync(BlogPostInput input, ImageUpload? cover, CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(
        Guid id, BlogPostInput input, ImageUpload? cover, bool removeCover, CancellationToken cancellationToken = default);

    Task<Result> SetPublishedAsync(Guid id, bool published, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

internal sealed class BlogPostService(
    IApplicationDbContext db,
    IValidator<BlogPostInput> validator,
    IFileStorage storage,
    TimeProvider clock) : IBlogPostService
{
    private const string CoverFolder = "blog";

    public async Task<PagedList<BlogPostListItem>> ListAsync(BlogPostQuery query, CancellationToken cancellationToken = default)
    {
        var posts = db.BlogPosts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // SQL'e lower(...) LIKE olarak çevrilir.
#pragma warning disable CA1304, CA1311, CA1862
            var term = query.Search.Trim().ToLower();
            posts = posts.Where(p => p.Title.ToLower().Contains(term) || p.Excerpt.ToLower().Contains(term));
#pragma warning restore CA1304, CA1311, CA1862
        }

        posts = query.Status switch
        {
            ProjectStatusFilter.Published => posts.Where(p => p.IsPublished),
            ProjectStatusFilter.Draft => posts.Where(p => !p.IsPublished),
            _ => posts
        };

        return await posts
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .Select(p => new BlogPostListItem(
                p.Id, p.Title, p.Slug, p.CoverImageUrl, p.Tags, p.IsPublished, p.PublishedAt, p.ReadingMinutes,
                p.UpdatedAt ?? p.CreatedAt))
            .ToPagedListAsync(query.Page, query.PageSize, cancellationToken);
    }

    public async Task<BlogPostEditModel?> GetForEditAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var post = await db.BlogPosts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (post is null)
        {
            return null;
        }

        var input = new BlogPostInput
        {
            Title = post.Title,
            Slug = post.Slug,
            Excerpt = post.Excerpt,
            Body = post.Body,
            Tags = string.Join(", ", post.Tags),
            IsPublished = post.IsPublished
        };

        return new BlogPostEditModel(input, post.CoverImageUrl, post.PublishedAt);
    }

    public async Task<Result<Guid>> CreateAsync(BlogPostInput input, ImageUpload? cover, CancellationToken cancellationToken = default)
    {
        var errors = await ValidateAsync(null, input, cancellationToken);
        BlogPost? post = null;
        errors ??= ValidationExtensions.TryApply(() =>
        {
            post = new BlogPost(input.Title, input.Excerpt, input.Body, input.Slug);
            Apply(post, input);
        });
        if (errors is not null)
        {
            return Result.Invalid<Guid>(errors);
        }

        if (cover is not null)
        {
            var saved = await storage.SaveImageAsync(cover, CoverFolder, cancellationToken);
            if (!saved.Succeeded)
            {
                return Result.Invalid<Guid>(CoverErrors(saved));
            }

            post!.SetCoverImage(saved.Value);
        }

        db.BlogPosts.Add(post!);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(post!.Id);
    }

    public async Task<Result> UpdateAsync(
        Guid id, BlogPostInput input, ImageUpload? cover, bool removeCover, CancellationToken cancellationToken = default)
    {
        var post = await db.BlogPosts.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (post is null)
        {
            return Result.NotFound();
        }

        var errors = await ValidateAsync(id, input, cancellationToken)
                     ?? ValidationExtensions.TryApply(() =>
                     {
                         post.Update(input.Title, input.Slug, input.Excerpt, input.Body);
                         Apply(post, input);
                     });
        if (errors is not null)
        {
            return Result.Invalid(errors);
        }

        var previousCover = post.CoverImageUrl;
        if (cover is not null)
        {
            var saved = await storage.SaveImageAsync(cover, CoverFolder, cancellationToken);
            if (!saved.Succeeded)
            {
                return Result.Invalid(CoverErrors(saved));
            }

            post.SetCoverImage(saved.Value);
        }
        else if (removeCover)
        {
            post.SetCoverImage(null);
        }

        await db.SaveChangesAsync(cancellationToken);

        if (previousCover is not null && previousCover != post.CoverImageUrl)
        {
            await storage.DeleteAsync(previousCover, cancellationToken);
        }

        return Result.Success();
    }

    public async Task<Result> SetPublishedAsync(Guid id, bool published, CancellationToken cancellationToken = default)
    {
        var post = await db.BlogPosts.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (post is null)
        {
            return Result.NotFound();
        }

        if (published)
        {
            post.Publish(clock.GetUtcNow());
        }
        else
        {
            post.Unpublish();
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var post = await db.BlogPosts.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (post is null)
        {
            return Result.NotFound();
        }

        var cover = post.CoverImageUrl;
        db.BlogPosts.Remove(post);
        await db.SaveChangesAsync(cancellationToken);
        await storage.DeleteAsync(cover, cancellationToken);
        return Result.Success();
    }

    private void Apply(BlogPost post, BlogPostInput input)
    {
        post.SetTags([input.Tags ?? string.Empty]);
        if (input.IsPublished)
        {
            post.Publish(clock.GetUtcNow());
        }
        else
        {
            post.Unpublish();
        }
    }

    private static List<Error> CoverErrors(Result result) =>
        result.Errors.Select(e => e with { Field = ProjectFormFields.Cover }).ToList();

    private async Task<IReadOnlyList<Error>?> ValidateAsync(Guid? id, BlogPostInput input, CancellationToken cancellationToken)
    {
        var errors = await validator.ValidateInputAsync(input, cancellationToken);
        if (errors is not null)
        {
            return errors;
        }

        var slug = Slug.Generate(string.IsNullOrWhiteSpace(input.Slug) ? input.Title : input.Slug);
        return await db.BlogPosts.AnyAsync(p => p.Slug == slug && p.Id != id, cancellationToken)
            ? [new Error($"'{slug}' adresi başka bir yazıda kullanılıyor.", nameof(BlogPostInput.Slug))]
            : null;
    }
}
