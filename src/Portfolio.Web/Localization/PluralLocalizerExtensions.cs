using Microsoft.Extensions.Localization;

namespace Portfolio.Web.Localization;

public static class PluralLocalizerExtensions
{
    /// <summary>Tekil karşılığın en.json'daki anahtar eki: <c>"{0} proje#one"</c>.</summary>
    public const string SingularSuffix = "#one";

    /// <summary>
    /// Sayıya göre tekil/çoğul seçen çeviri. Türkçede sayıdan sonra isim hep tekildir ("1 proje", "3 proje");
    /// İngilizcede sayı 1 ise anahtarın <see cref="SingularSuffix"/> ekli karşılığı kullanılır, yoksa çoğul metin.
    /// </summary>
    public static LocalizedString Plural(this IStringLocalizer localizer, int count, string name, params object[] arguments)
    {
        if (count == 1 && localizer[name + SingularSuffix, arguments] is { ResourceNotFound: false } singular)
        {
            return new LocalizedString(name, singular.Value);
        }

        return localizer[name, arguments];
    }
}
