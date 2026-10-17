namespace Portfolio.Web.Models;

public sealed record StatusViewModel(int Code)
{
    public string Title => Code switch
    {
        404 => "Sayfa bulunamadı",
        403 => "Erişim engellendi",
        _ => "Bir şeyler ters gitti"
    };

    public string Message => Code switch
    {
        404 => "Aradığın sayfa taşınmış ya da hiç var olmamış olabilir.",
        403 => "Bu sayfayı görüntüleme yetkin yok.",
        _ => "İstek işlenirken bir sorun oluştu. Biraz sonra tekrar dene."
    };
}
