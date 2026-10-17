using FluentValidation;
using Portfolio.Application.Common.Models;
using Portfolio.Domain.Common;

namespace Portfolio.Application.Common.Validation;

internal static class ValidationExtensions
{
    /// <summary>Giriş geçerliyse null, değilse alan bazlı hataları içeren bir sonuç döndürür.</summary>
    public static async Task<IReadOnlyList<Error>?> ValidateInputAsync<T>(
        this IValidator<T> validator, T input, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(input, cancellationToken);
        return result.IsValid
            ? null
            : result.Errors.Select(e => new Error(e.ErrorMessage, e.PropertyName)).ToList();
    }

    /// <summary>
    /// Domain katmanındaki son savunma hattını (DomainException) kullanıcıya gösterilebilir hataya çevirir.
    /// </summary>
    public static IReadOnlyList<Error>? TryApply(Action apply)
    {
        try
        {
            apply();
            return null;
        }
        catch (DomainException ex)
        {
            return [new Error(ex.Message)];
        }
    }

    public static IRuleBuilderOptions<T, string?> HttpUrl<T>(this IRuleBuilder<T, string?> rule) =>
        rule
            .MaximumLength(Guard.UrlMaxLength)
            .Must(url => string.IsNullOrWhiteSpace(url) ||
                         (Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) &&
                          (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
            .WithMessage("'{PropertyName}' http:// veya https:// ile başlayan geçerli bir adres olmalı.");

    public static IRuleBuilderOptions<T, string?> OptionalSlug<T>(this IRuleBuilder<T, string?> rule) =>
        rule
            .Must(slug => string.IsNullOrWhiteSpace(slug) || Slug.IsValid(slug.Trim()))
            .WithMessage("'{PropertyName}' yalnızca küçük harf, rakam ve tire içerebilir (ör. benim-projem).");
}
