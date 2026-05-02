using VeterinaryBackend.Domain.Common;

namespace VeterinaryBackend.Business.Helpers;

public static class LanguageHelper
{
    public const string DefaultLanguageCode = "uz";

    public static Language Parse(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Language.Uz;

        var normalized = code.Trim().ToLowerInvariant();

        if (normalized.Length > 2)
            normalized = normalized[..2];

        return normalized switch
        {
            "uz" => Language.Uz,
            "ru" => Language.Ru,
            "en" => Language.En,
            _ => Language.Uz
        };
    }

    public static string ToCode(Language language) => language switch
    {
        Language.Ru => "ru",
        Language.En => "en",
        _ => "uz"
    };
}
