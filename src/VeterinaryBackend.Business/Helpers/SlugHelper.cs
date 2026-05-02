using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace VeterinaryBackend.Business.Helpers;

public static class SlugHelper
{
    private static readonly Regex NonSlugChar = new("[^a-z0-9]+", RegexOptions.Compiled);

    private static readonly Dictionary<char, string> CyrillicMap = new()
    {
        ['а']="a",['б']="b",['в']="v",['г']="g",['д']="d",['е']="e",['ё']="yo",
        ['ж']="zh",['з']="z",['и']="i",['й']="y",['к']="k",['л']="l",['м']="m",
        ['н']="n",['о']="o",['п']="p",['р']="r",['с']="s",['т']="t",['у']="u",
        ['ф']="f",['х']="kh",['ц']="ts",['ч']="ch",['ш']="sh",['щ']="sch",
        ['ъ']="",['ы']="y",['ь']="",['э']="e",['ю']="yu",['я']="ya",
        ['ў']="o",['қ']="q",['ғ']="g",['ҳ']="h"
    };

    public static string Generate(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return string.Empty;

        var sb = new StringBuilder(source.Length);
        foreach (var ch in source.ToLowerInvariant().Trim())
        {
            if (CyrillicMap.TryGetValue(ch, out var mapped)) sb.Append(mapped);
            else sb.Append(ch);
        }

        var normalized = sb.ToString().Normalize(NormalizationForm.FormD);
        var stripped = new StringBuilder();
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                stripped.Append(ch);
        }

        var slug = NonSlugChar.Replace(stripped.ToString(), "-").Trim('-');
        if (slug.Length > 100) slug = slug[..100];
        return slug;
    }
}
