using System.Globalization;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Domain.Entities;

namespace Portfolio.Web.Models;

/// <summary>
/// Görünümlerde kullanılan etiketler ve tarih biçimleri. İsteğin kültürüne (tr-TR / en-US) göre üretilir;
/// yönetim paneli her zaman Türkçe çalışır.
/// </summary>
public static class DisplayText
{
    private static CultureInfo Culture => CultureInfo.CurrentCulture;

    private static bool English => Culture.TwoLetterISOLanguageName == "en";

    private static readonly (ExperienceType Value, string Turkish, string English)[] ExperienceTypeLabels =
    [
        (ExperienceType.Work, "İş", "Work"),
        (ExperienceType.Internship, "Staj", "Internship"),
        (ExperienceType.Education, "Eğitim", "Education"),
        (ExperienceType.Volunteer, "Gönüllülük", "Volunteering"),
        (ExperienceType.Certification, "Sertifika", "Certification"),
        (ExperienceType.Other, "Diğer", "Other")
    ];

    public static IReadOnlyList<(ExperienceType Value, string Label)> ExperienceTypes =>
        ExperienceTypeLabels.Select(t => (t.Value, English ? t.English : t.Turkish)).ToList();

    public static string Label(ExperienceType type)
    {
        foreach (var (value, turkish, english) in ExperienceTypeLabels)
        {
            if (value == type)
            {
                return English ? english : turkish;
            }
        }

        return type.ToString();
    }

    public static string MonthYear(DateOnly date) => date.ToString("MMMM yyyy", Culture);

    public static string Period(DateOnly start, DateOnly? end) =>
        $"{MonthYear(start)} – {(end is { } e ? MonthYear(e) : Lang.T("Devam ediyor", "Present"))}";

    /// <summary>"Mart 2026 – Haziran 2026"; bitiş yoksa "Mart 2026 – devam ediyor". Tarih yoksa null.</summary>
    public static string? ProjectPeriod(DateOnly? start, DateOnly? end) => (start, end) switch
    {
        ({ } s, { } e) when s.Year == e.Year && s.Month == e.Month => MonthYear(s),
        ({ } s, { } e) => $"{MonthYear(s)} – {MonthYear(e)}",
        ({ } s, null) => $"{MonthYear(s)} – {Lang.T("devam ediyor", "ongoing")}",
        (null, { } e) => MonthYear(e),
        _ => null
    };

    /// <summary>İki tarih arasındaki süre, ör. "1 yıl 3 ay" / "1 yr 3 mos". Bitiş boşsa bugüne kadar hesaplanır.</summary>
    public static string Duration(DateOnly start, DateOnly? end, DateOnly today)
    {
        var last = end ?? today;
        var months = Math.Max(1, ((last.Year - start.Year) * 12) + last.Month - start.Month + 1);
        var (years, rest) = (months / 12, months % 12);

        string Years(int n) => English ? $"{n} {(n == 1 ? "yr" : "yrs")}" : $"{n} yıl";
        string Months(int n) => English ? $"{n} {(n == 1 ? "mo" : "mos")}" : $"{n} ay";

        return (years, rest) switch
        {
            (0, _) => Months(rest),
            (_, 0) => Years(years),
            _ => $"{Years(years)} {Months(rest)}"
        };
    }

    public static string Date(DateTimeOffset value) =>
        value.ToLocalTime().ToString(English ? "MMM d, yyyy, HH:mm" : "d MMM yyyy, HH:mm", Culture);

    public static string LongDate(DateTimeOffset value) =>
        value.ToLocalTime().ToString(English ? "MMMM d, yyyy" : "d MMMM yyyy", Culture);

    public static string Level(int level) => level switch
    {
        1 => Lang.T("Başlangıç", "Beginner"),
        2 => Lang.T("Temel", "Elementary"),
        3 => Lang.T("Orta", "Intermediate"),
        4 => Lang.T("İleri", "Advanced"),
        5 => Lang.T("Uzman", "Expert"),
        _ => level.ToString(Culture)
    };

    /// <summary>Türkçede "%40", İngilizcede "40%".</summary>
    public static string Percent(double value)
    {
        var number = value.ToString("0.#", Culture);
        return English ? $"{number}%" : $"%{number}";
    }

    /// <summary>Blog yazısının dil rozeti ("TR" / "EN").</summary>
    public static string LanguageBadge(string language) => language.ToUpperInvariant();
}
