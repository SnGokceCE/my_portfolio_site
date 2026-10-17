using System.Globalization;

namespace Portfolio.Application.Common.Interfaces;

/// <summary>
/// İsteğin dili. İçerik sorguları İngilizce sürüm varsa onu, yoksa Türkçe aslını döndürür.
/// </summary>
public interface ICurrentLanguage
{
    bool IsEnglish { get; }
}

/// <summary>Arayüz kültürüne bakan varsayılan uygulama (web isteğinde RequestLocalization ayarlar).</summary>
public sealed class CultureCurrentLanguage : ICurrentLanguage
{
    public bool IsEnglish => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en";
}

/// <summary>Uygulama katmanındaki iki dilli kısa metinler (ör. doğrulama mesajları) için yardımcı.</summary>
public static class Lang
{
    public static string T(string turkish, string english) =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en" ? english : turkish;
}
