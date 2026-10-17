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

    public static string Date(DateTimeOffset value) => value.ToLocalTime().ToString("d MMM yyyy, HH:mm", Turkish);

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
