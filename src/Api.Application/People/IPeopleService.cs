using Api.Application.Common;
using Api.Application.People.Dtos;

namespace Api.Application.People;

/// <summary>The people use cases: what the <c>/people</c> endpoints do.</summary>
public interface IPeopleService
{
    Task<PersonPage> ListAsync(PeopleFilters filters, CancellationToken ct);

    Task<DirectorySummary> GetSummaryAsync(CancellationToken ct);

    PeopleOptions GetOptions();

    /// <summary>The audit log, newest first; <paramref name="limit"/> is clamped to 1–200 (default 50).</summary>
    Task<IReadOnlyList<PersonAuditEntryDto>> ListAuditLogAsync(int? limit, CancellationToken ct);

    Task<Result<PersonDto>> GetAsync(int id, CancellationToken ct);

    Task<Result<PersonDto>> CreateAsync(PersonInput input, Editor editor, CancellationToken ct);

    Task<Result<PersonDto>> UpdateAsync(int id, PersonInput input, Editor editor, CancellationToken ct);

    Task<Result> DeleteAsync(int id, Editor editor, CancellationToken ct);
}
