namespace Api.Application.Users;

/// <summary>The age filters of the list.</summary>
public static class AgeRanges
{
    public sealed record AgeRange(string Value, int Min, int? Max);

    public static readonly AgeRange[] All =
    [
        new("18-24", 18, 24),
        new("25-34", 25, 34),
        new("35-44", 35, 44),
        new("45-54", 45, 54),
        new("55+", 55, null),
    ];

    public static AgeRange? Find(string? value) => All.FirstOrDefault(r => r.Value == value);
}
