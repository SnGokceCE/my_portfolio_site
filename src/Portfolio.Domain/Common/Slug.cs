using System.Globalization;
using System.Text;

namespace Portfolio.Domain.Common;

/// <summary>
/// URL'de kullanılacak okunabilir kimlik ("Mobil Uygulamam" → "mobil-uygulamam") üretir.
/// Türkçe karakterleri ASCII karşılıklarına çevirir.
/// </summary>
public static class Slug
{
    public const int MaxLength = 120;

    public static string Generate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(text.Length);
        var lastWasDash = true; // baştaki tireyi engeller

        foreach (var ch in Fold(text))
        {
            if (char.IsAsciiLetterOrDigit(ch))
            {
                builder.Append(char.ToLowerInvariant(ch));
                lastWasDash = false;
            }
            else if (!lastWasDash)
            {
                builder.Append('-');
                lastWasDash = true;
            }

            if (builder.Length >= MaxLength)
            {
                break;
            }
        }

        return builder.ToString().Trim('-');
    }

    /// <summary>
    /// Kullanıcı slug girdiyse onu, girmediyse metni slug'a çevirir. Sonuç boşsa hata fırlatır.
    /// </summary>
    public static string FromInputOrText(string? slug, string text)
    {
        var result = Generate(string.IsNullOrWhiteSpace(slug) ? text : slug);
        return result.Length == 0
            ? throw new DomainException("Slug üretilemedi; metin en az bir harf veya rakam içermeli.")
            : result;
    }

    public static bool IsValid(string? slug) =>
        !string.IsNullOrEmpty(slug) && slug.Length <= MaxLength && slug == Generate(slug);

    // Türkçe harfleri ve aksanlı karakterleri sadeleştirir (ı→i, ş→s, é→e ...).
    private static string Fold(string text)
    {
        var mapped = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            mapped.Append(ch switch
            {
                'ı' or 'İ' => 'i',
                'ğ' or 'Ğ' => 'g',
                'ş' or 'Ş' => 's',
                _ => ch
            });
        }

        var decomposed = mapped.ToString().Normalize(NormalizationForm.FormD);
        var result = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                result.Append(ch);
            }
        }

        return result.ToString();
    }
}
