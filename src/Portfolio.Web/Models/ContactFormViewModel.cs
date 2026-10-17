using Portfolio.Application.Contact;
using Portfolio.Application.SiteProfiles;

namespace Portfolio.Web.Models;

public sealed class ContactFormViewModel
{
    public ContactInput Input { get; set; } = new();

    /// <summary>
    /// Bal küpü (honeypot): insanlara gizli, botların doldurduğu alan. Adı bilerek
    /// sıradan bir alan gibi seçildi; dolu gelirse gönderim sessizce yok sayılır.
    /// </summary>
    public string? Website { get; set; }

    /// <summary>Formun ne zaman oluşturulduğunu taşıyan şifreli jeton (zaman tuzağı).</summary>
    public string? FormToken { get; set; }

    public bool Sent { get; set; }

    public SiteProfileDto Profile { get; set; } = SiteProfileDto.Empty;
}
