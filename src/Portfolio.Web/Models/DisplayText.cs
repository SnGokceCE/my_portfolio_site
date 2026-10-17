using System.Globalization;
using Portfolio.Domain.Entities;

namespace Portfolio.Web.Models;

/// <summary>Görünümlerde kullanılan Türkçe etiketler ve tarih biçimleri.</summary>
public static class DisplayText
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    public static IReadOnlyList<(ExperienceType Value, string Label)> ExperienceTypes { get; } =
    [
        (ExperienceType.Work, "İş"),
        (ExperienceType.Internship, "Staj"),
        (ExperienceType.Education, "Eğitim"),
        (ExperienceType.Volunteer, "Gönüllülük"),
        (ExperienceType.Certification, "Sertifika"),
        (ExperienceType.Other, "Diğer")
    ];

    public static string Label(ExperienceType type) =>
        ExperienceTypes.FirstOrDefault(t => t.Value == type).Label ?? type.ToString();

    public static string MonthYear(DateOnly date) => date.ToString("MMMM yyyy", Turkish);

    public static string Period(DateOnly start, DateOnly? end) =>
        $"{MonthYear(start)} – {(end is { } e ? MonthYear(e) : "Devam ediyor")}";

    /// <summary>"Mart 2026 – Haziran 2026"; bitiş yoksa "Mart 2026 – devam ediyor". Tarih yoksa null.</summary>
    public static string? ProjectPeriod(DateOnly? start, DateOnly? end) => (start, end) switch
    {
        ({ } s, { } e) when s.Year == e.Year && s.Month == e.Month => MonthYear(s),
        ({ } s, { } e) => $"{MonthYear(s)} – {MonthYear(e)}",
        ({ } s, null) => $"{MonthYear(s)} – devam ediyor",
        (null, { } e) => MonthYear(e),
        _ => null
    };

    /// <summary>İki tarih arasındaki süre, ör. "1 yıl 3 ay". Bitiş boşsa bugüne kadar hesaplanır.</summary>
    public static string Duration(DateOnly start, DateOnly? end, DateOnly today)
    {
        var last = end ?? today;
        var months = Math.Max(1, ((last.Year - start.Year) * 12) + last.Month - start.Month + 1);
        var (years, rest) = (months / 12, months % 12);
        return (years, rest) switch
        {
            (0, _) => $"{rest} ay",
            (_, 0) => $"{years} yıl",
            _ => $"{years} yıl {rest} ay"
        };
    }

    public static string Date(DateTimeOffset value) => value.ToLocalTime().ToString("d MMM yyyy, HH:mm", Turkish);

    public static string LongDate(DateTimeOffset value) => value.ToLocalTime().ToString("d MMMM yyyy", Turkish);

    public static string Level(int level) => level switch
    {
        1 => "Başlangıç",
        2 => "Temel",
        3 => "Orta",
        4 => "İleri",
        5 => "Uzman",
        _ => level.ToString(Turkish)
    };
}
