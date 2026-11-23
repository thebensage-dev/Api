using System.Globalization;
using System.Text;

namespace Api.Domain.Common;

/// <summary>Text helpers for search: the same rules as the User Directory's.</summary>
public static class Text
{
    /// <summary>
    /// Lower case, without accents: "Siobhán" becomes "siobhan". Needs ICU (the image is the
    /// "-extra" variant): with invariant globalization .NET doesn't decompose accents.
    /// </summary>
    public static string Normalise(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category is not (UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark))
            {
                builder.Append(c);
            }
        }
        return builder.ToString().ToLowerInvariant();
    }

    /// <summary>The words of a search, normalised. Every one of them must match.</summary>
    public static string[] SearchTerms(string? query) =>
        string.IsNullOrWhiteSpace(query)
            ? []
            : Normalise(query).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
}
