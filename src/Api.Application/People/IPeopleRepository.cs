using Api.Application.People.Dtos;
using Api.Domain.People;

namespace Api.Application.People;

/// <summary>Storage of people and their change history. Implemented in Infrastructure.</summary>
public interface IPeopleRepository
{
    /// <summary>How many people match <paramref name="search"/>.</summary>
    Task<int> CountAsync(PersonSearch search, CancellationToken ct);

    /// <summary>One page of the people matching <paramref name="search"/>, in its order. Not tracked.</summary>
    Task<IReadOnlyList<Person>> SearchAsync(PersonSearch search, int skip, int take, CancellationToken ct);

    /// <summary>Totals for the whole directory, unsorted.</summary>
    Task<PeopleStats> GetStatsAsync(CancellationToken ct);

    /// <summary>A person to read, or null. Not tracked.</summary>
    Task<Person?> FindAsync(int id, CancellationToken ct);

    /// <summary>A person to change, or null. Tracked: save with <see cref="TrySaveChangesAsync"/>.</summary>
    Task<Person?> FindForUpdateAsync(int id, CancellationToken ct);

    void Add(Person person);

    /// <summary>Deletes a person straight away. False when there was nobody with that id.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken ct);

    /// <summary>The audit log, newest first.</summary>
    Task<IReadOnlyList<PersonAuditEntry>> ListAuditLogAsync(int take, CancellationToken ct);

    void AddAuditEntry(PersonAuditEntry entry);

    /// <summary>
    /// Saves the pending changes, or returns false (and drops them) when the email belongs to
    /// someone else.
    /// </summary>
    Task<bool> TrySaveChangesAsync(CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}

/// <summary>Totals for the whole directory, as stored (unsorted).</summary>
public sealed record PeopleStats(
    int People,
    double AverageAge,
    IReadOnlyList<CountryCount> Countries,
    IReadOnlyList<InterestCount> Interests);
