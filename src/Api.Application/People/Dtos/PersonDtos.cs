using Api.Domain.People;

namespace Api.Application.People.Dtos;

/// <summary>A person, as the API returns it.</summary>
public sealed record PersonDto(
    int Id,
    string FullName,
    string Email,
    string? Phone,
    int Age,
    string Country,
    string? City,
    string? JobTitle,
    string? Bio,
    IReadOnlyList<string> Interests,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static PersonDto From(Person p) => new(
        p.Id, p.FullName, p.Email, p.Phone, p.Age, p.Country, p.City, p.JobTitle, p.Bio,
        p.Interests, p.CreatedAt, p.UpdatedAt);
}

/// <summary>One page of the list.</summary>
/// <param name="Items">The people on this page.</param>
/// <param name="Total">How many people match, on every page.</param>
/// <param name="Page">This page's number, from 1 (out-of-range pages are clamped).</param>
/// <param name="PageCount">How many pages there are (at least 1).</param>
/// <param name="From">The position of the first item on this page, from 1 (0 when nothing matches).</param>
/// <param name="To">The position of the last item on this page.</param>
/// <param name="Terms">The normalised search words, for highlighting matches.</param>
public sealed record PersonPage(
    IReadOnlyList<PersonDto> Items,
    int Total,
    int Page,
    int PageCount,
    int From,
    int To,
    IReadOnlyList<string> Terms);

public sealed record CountryCount(string Code, string Name, int People);

public sealed record InterestCount(string Name, int People);

/// <summary>Totals for the whole directory.</summary>
/// <param name="People">How many people there are.</param>
/// <param name="AverageAge">Their average age, rounded.</param>
/// <param name="Countries">People per country, by country name.</param>
/// <param name="Interests">People per interest, most popular first.</param>
public sealed record DirectorySummary(
    int People,
    int AverageAge,
    IReadOnlyList<CountryCount> Countries,
    IReadOnlyList<InterestCount> Interests);

public sealed record CountryOption(string Code, string Name);

public sealed record AgeRangeOption(string Value, int Min, int? Max);

/// <summary>The allowed values of the fields and filters.</summary>
public sealed record PeopleOptions(
    IReadOnlyList<CountryOption> Countries,
    IReadOnlyList<string> Interests,
    int MaxInterests,
    IReadOnlyList<AgeRangeOption> AgeRanges,
    IReadOnlyList<string> Sorts);

/// <summary>One line of the audit log.</summary>
public sealed record PersonAuditEntryDto(
    long Id,
    int PersonId,
    string Action,
    DateTimeOffset At,
    string ByName,
    string Client)
{
    public static PersonAuditEntryDto From(PersonAuditEntry e) => new(e.Id, e.PersonId, e.Action, e.At, e.ByName, e.Client);
}
