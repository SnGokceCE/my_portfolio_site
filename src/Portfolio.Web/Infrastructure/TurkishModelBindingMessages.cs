using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Portfolio.Web.Infrastructure;

/// <summary>Model bağlama (ör. sayı alanına harf girilmesi) hatalarının Türkçe metinleri.</summary>
internal static class TurkishModelBindingMessages
{
    public static void Apply(DefaultModelBindingMessageProvider provider)
    {
        provider.SetValueMustNotBeNullAccessor(_ => "Bu alan boş bırakılamaz.");
        provider.SetMissingBindRequiredValueAccessor(name => $"'{name}' alanı için değer girilmedi.");
        provider.SetMissingKeyOrValueAccessor(() => "Değer gerekli.");
        provider.SetMissingRequestBodyRequiredValueAccessor(() => "İstek gövdesi boş olamaz.");
        provider.SetAttemptedValueIsInvalidAccessor((value, _) => $"'{value}' geçerli bir değer değil.");
        provider.SetNonPropertyAttemptedValueIsInvalidAccessor(value => $"'{value}' geçerli bir değer değil.");
        provider.SetUnknownValueIsInvalidAccessor(_ => "Girilen değer geçerli değil.");
        provider.SetNonPropertyUnknownValueIsInvalidAccessor(() => "Girilen değer geçerli değil.");
        provider.SetValueIsInvalidAccessor(value => $"'{value}' geçerli değil.");
        provider.SetValueMustBeANumberAccessor(_ => "Bu alan bir sayı olmalı.");
        provider.SetNonPropertyValueMustBeANumberAccessor(() => "Bu alan bir sayı olmalı.");
    }
}
