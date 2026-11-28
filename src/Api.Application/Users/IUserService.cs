using Api.Application.Common;
using Api.Application.Users.Dtos;

namespace Api.Application.Users;

/// <summary>The users use cases: what the <c>/users</c> endpoints do.</summary>
public interface IUserService
{
    Task<UserPage> ListAsync(UserFilters filters, CancellationToken ct);

    Task<DirectorySummary> GetSummaryAsync(CancellationToken ct);

    UserOptions GetOptions();

    /// <summary>The audit log, newest first; <paramref name="limit"/> is clamped to 1–200 (default 50).</summary>
    Task<IReadOnlyList<UserAuditEntryDto>> ListAuditLogAsync(int? limit, CancellationToken ct);

    Task<Result<UserDto>> GetAsync(int id, CancellationToken ct);

    Task<Result<UserDto>> CreateAsync(UserInput input, Editor editor, CancellationToken ct);

    Task<Result<UserDto>> UpdateAsync(int id, UserInput input, Editor editor, CancellationToken ct);

    Task<Result> DeleteAsync(int id, Editor editor, CancellationToken ct);
}
