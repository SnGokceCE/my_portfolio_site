namespace Portfolio.Domain.Common;

/// <summary>
/// Varlıkların ortak doğrulama yardımcıları. Kural ihlalinde <see cref="DomainException"/> fırlatır.
/// </summary>
public static class Guard
{
    public const int UrlMaxLength = 300;

    public static string Required(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{field} boş olamaz.");
        }

        return MaxLength(value.Trim(), field, maxLength);
    }

    public static string? Optional(string? value, string field, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null : MaxLength(value.Trim(), field, maxLength);

    public static string? OptionalUrl(string? value, string field)
    {
        var url = Optional(value, field, UrlMaxLength);
        if (url is null)
        {
            return null;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            throw new DomainException($"{field} geçerli bir http(s) adresi olmalı.");
        }

        return url;
    }

    public static string MaxLength(string value, string field, int maxLength) =>
        value.Length > maxLength
            ? throw new DomainException($"{field} en fazla {maxLength} karakter olabilir.")
            : value;

    public static int Range(int value, string field, int min, int max) =>
        value < min || value > max
            ? throw new DomainException($"{field} {min} ile {max} arasında olmalı.")
            : value;

    public static void DateRange(DateOnly? start, DateOnly? end, string field)
    {
        if (start is not null && end is not null && end < start)
        {
            throw new DomainException($"{field} başlangıç tarihinden önce olamaz.");
        }
    }
}
