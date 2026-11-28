using Api.Application.Users.Dtos;
using Api.Domain.Users;

namespace Api.Application.Users;

/// <summary>Storage of users and their change history. Implemented in Infrastructure.</summary>
public interface IUserRepository
{
    /// <summary>How many users match <paramref name="search"/>.</summary>
    Task<int> CountAsync(UserSearch search, CancellationToken ct);

    /// <summary>One page of the users matching <paramref name="search"/>, in its order. Not tracked.</summary>
    Task<IReadOnlyList<User>> SearchAsync(UserSearch search, int skip, int take, CancellationToken ct);

    /// <summary>Totals for the whole directory, unsorted.</summary>
    Task<UserStats> GetStatsAsync(CancellationToken ct);

    /// <summary>A user to read, or null. Not tracked.</summary>
    Task<User?> FindAsync(int id, CancellationToken ct);

    /// <summary>A user to change, or null. Tracked: save with <see cref="TrySaveChangesAsync"/>.</summary>
    Task<User?> FindForUpdateAsync(int id, CancellationToken ct);

    void Add(User user);

    /// <summary>Deletes a user straight away. False when there was nobody with that id.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken ct);

    /// <summary>The audit log, newest first.</summary>
    Task<IReadOnlyList<UserAuditEntry>> ListAuditLogAsync(int take, CancellationToken ct);

    void AddAuditEntry(UserAuditEntry entry);

    /// <summary>
    /// Saves the pending changes, or returns false (and drops them) when the email belongs to
    /// someone else.
    /// </summary>
    Task<bool> TrySaveChangesAsync(CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}

/// <summary>Totals for the whole directory, as stored (unsorted).</summary>
public sealed record UserStats(
    int Users,
    double AverageAge,
    IReadOnlyList<CountryCount> Countries,
    IReadOnlyList<InterestCount> Interests);
