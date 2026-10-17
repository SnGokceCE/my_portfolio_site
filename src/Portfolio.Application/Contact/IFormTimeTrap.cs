namespace Portfolio.Application.Contact;

public enum TimeTrapResult
{
    Valid,

    /// <summary>Form, bir insanın dolduramayacağı kadar hızlı gönderildi (bot).</summary>
    TooFast,

    /// <summary>Form çok uzun süre açık kaldı; kullanıcıya tekrar göndermesi söylenir.</summary>
    Expired,

    /// <summary>Jeton eksik ya da kurcalanmış.</summary>
    Invalid
}

/// <summary>
/// Spam koruması için "zaman tuzağı": form oluşturulurken şifreli bir zaman damgası verilir,
/// gönderimde ne kadar süre geçtiği kontrol edilir.
/// </summary>
public interface IFormTimeTrap
{
    string Issue();

    TimeTrapResult Check(string? token);
}
