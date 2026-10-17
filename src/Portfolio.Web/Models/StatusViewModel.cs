namespace Portfolio.Web.Models;

public sealed record StatusViewModel(int Code)
{
    public string Title => Code switch
    {
        404 => "Sayfa bulunamadı",
        403 => "Erişim engellendi",
        429 => "Biraz yavaşla",
        _ => "Bir şeyler ters gitti"
    };

    public string Message => Code switch
    {
        404 => "Aradığın sayfa taşınmış ya da hiç var olmamış olabilir.",
        403 => "Bu sayfayı görüntüleme yetkin yok.",
        429 => "Kısa sürede çok fazla istek gönderildi. Birkaç dakika sonra tekrar dene.",
        _ => "İstek işlenirken bir sorun oluştu. Biraz sonra tekrar dene."
    };
}
