namespace Api.Application.Users;

/// <summary>What the caller asked the list for, as sent (<c>GET /users</c>).</summary>
public sealed record UserFilters(
    string? Q,
    string? Country,
    string? Age,
    IReadOnlyList<string> Interests,
    string? Sort,
    int Page,
    int Size);

/// <summary>The sort orders of the list.</summary>
public enum UserSort
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
/// <param name="Interests">Interests the user must all have.</param>
/// <param name="Sort">The order.</param>
public sealed record UserSearch(
    IReadOnlyList<string> Terms,
    string? Country,
    AgeRanges.AgeRange? Age,
    IReadOnlyList<string> Interests,
    UserSort Sort);

public static class UserSearchRules
{
    public const int DefaultPageSize = 12;
    public const int MaxPageSize = 100;

    /// <summary>The sort orders' names in the API. Leave sort out for best match while searching, otherwise newest first.</summary>
    public static readonly IReadOnlyDictionary<string, UserSort> Sorts = new Dictionary<string, UserSort>(StringComparer.Ordinal)
    {
        ["id-desc"] = UserSort.NewestFirst,
        ["id-asc"] = UserSort.OldestFirst,
        ["name-asc"] = UserSort.NameAscending,
        ["name-desc"] = UserSort.NameDescending,
        ["age-asc"] = UserSort.AgeAscending,
        ["age-desc"] = UserSort.AgeDescending,
        ["country-asc"] = UserSort.CountryAscending,
        ["country-desc"] = UserSort.CountryDescending,
    };

    /// <summary>The names of <see cref="Sorts"/>, in the order the options list shows them.</summary>
    public static readonly string[] SortNames =
        ["id-desc", "id-asc", "name-asc", "name-desc", "age-asc", "age-desc", "country-asc", "country-desc"];
}
