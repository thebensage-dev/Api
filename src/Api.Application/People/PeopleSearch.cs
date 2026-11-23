namespace Api.Application.People;

/// <summary>What the caller asked the list for, as sent (<c>GET /people</c>).</summary>
public sealed record PeopleFilters(
    string? Q,
    string? Country,
    string? Age,
    IReadOnlyList<string> Interests,
    string? Sort,
    int Page,
    int Size);

/// <summary>The sort orders of the list.</summary>
public enum PersonSort
{
    NewestFirst,
    OldestFirst,
    NameAscending,
    NameDescending,
    AgeAscending,
    AgeDescending,
    CountryAscending,
    CountryDescending,
    /// <summary>Best match first; only used while searching without a sort.</summary>
    BestMatch,
}

/// <summary>The list's search, cleaned up for the repository: normalised words and known values only.</summary>
/// <param name="Terms">Normalised search words; every one must match.</param>
/// <param name="Country">An upper-case country code, or null for any.</param>
/// <param name="Age">An age range, or null for any.</param>
/// <param name="Interests">Interests the person must all have.</param>
/// <param name="Sort">The order.</param>
public sealed record PersonSearch(
    IReadOnlyList<string> Terms,
    string? Country,
    AgeRanges.AgeRange? Age,
    IReadOnlyList<string> Interests,
    PersonSort Sort);

public static class PeopleSearch
{
    public const int DefaultPageSize = 12;
    public const int MaxPageSize = 100;

    /// <summary>The sort orders' names in the API. Leave sort out for best match while searching, otherwise newest first.</summary>
    public static readonly IReadOnlyDictionary<string, PersonSort> Sorts = new Dictionary<string, PersonSort>(StringComparer.Ordinal)
    {
        ["id-desc"] = PersonSort.NewestFirst,
        ["id-asc"] = PersonSort.OldestFirst,
        ["name-asc"] = PersonSort.NameAscending,
        ["name-desc"] = PersonSort.NameDescending,
        ["age-asc"] = PersonSort.AgeAscending,
        ["age-desc"] = PersonSort.AgeDescending,
        ["country-asc"] = PersonSort.CountryAscending,
        ["country-desc"] = PersonSort.CountryDescending,
    };

    /// <summary>The names of <see cref="Sorts"/>, in the order the options list shows them.</summary>
    public static readonly string[] SortNames =
        ["id-desc", "id-asc", "name-asc", "name-desc", "age-asc", "age-desc", "country-asc", "country-desc"];
}
