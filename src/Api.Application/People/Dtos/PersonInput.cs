namespace Api.Application.People.Dtos;

/// <summary>
/// What a person sent to <c>POST /people</c> or <c>PUT /people/{id}</c>. Optional text may be
/// left out, null or empty; it is stored as null.
/// </summary>
public sealed record PersonInput
{
    /// <example>Amara Okafor</example>
    public string? FullName { get; init; }
    /// <example>amara.okafor@example.com</example>
    public string? Email { get; init; }
    /// <example>+44 7700 900123</example>
    public string? Phone { get; init; }
    /// <example>29</example>
    public int? Age { get; init; }
    /// <summary>ISO 3166-1 alpha-2 code (see <c>GET /people/options</c>).</summary>
    /// <example>GB</example>
    public string? Country { get; init; }
    /// <example>Manchester</example>
    public string? City { get; init; }
    /// <example>Frontend developer</example>
    public string? JobTitle { get; init; }
    /// <example>Builds accessible design systems.</example>
    public string? Bio { get; init; }
    /// <summary>1 to 5 of the interests in <c>GET /people/options</c>.</summary>
    public IReadOnlyList<string>? Interests { get; init; }
}
