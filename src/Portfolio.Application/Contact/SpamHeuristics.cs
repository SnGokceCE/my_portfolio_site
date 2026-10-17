using System.Text.RegularExpressions;

namespace Portfolio.Application.Contact;

/// <summary>
/// Bot mesajlarında sık görülen kalıplara bakan basit kurallar. Kesin karar vermez;
/// şüpheli mesaj yine kaydedilir ama "Spam" klasörüne düşer ve bildirim gönderilmez.
/// </summary>
public static partial class SpamHeuristics
{
    private static readonly string[] SuspiciousPhrases =
    [
        "casino", "crypto", "bitcoin", "viagra", "seo service", "backlink", "loan offer", "bahis", "kumar"
    ];

    public static bool LooksLikeSpam(ContactInput input, int maxLinks)
    {
        var text = $"{input.Subject} {input.Message}";

        if (UrlRegex().Count(text) > maxLinks)
        {
            return true;
        }

        // İnsanlar adlarına bağlantı yazmaz; botlar sık yapar.
        if (UrlRegex().IsMatch(input.Name))
        {
            return true;
        }

        return SuspiciousPhrases.Any(p => text.Contains(p, StringComparison.OrdinalIgnoreCase));
    }

    [GeneratedRegex(@"(https?://|www\.)\S+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UrlRegex();
}
