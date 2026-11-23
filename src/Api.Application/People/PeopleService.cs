using Api.Application.Common;
using Api.Application.People.Dtos;
using Api.Domain.Common;
using Api.Domain.People;

namespace Api.Application.People;

/// <summary>The people use cases, on top of <see cref="IPeopleRepository"/>.</summary>
public sealed class PeopleService(IPeopleRepository people, TimeProvider clock) : IPeopleService
{
    public async Task<PersonPage> ListAsync(PeopleFilters filters, CancellationToken ct)
    {
        var terms = Text.SearchTerms(filters.Q);
        var sort = filters.Sort is { } name && PeopleSearch.Sorts.TryGetValue(name, out var known)
            ? known
            : terms.Length > 0 ? PersonSort.BestMatch : PersonSort.NewestFirst;
        var search = new PersonSearch(
            terms,
            string.IsNullOrEmpty(filters.Country) ? null : filters.Country.ToUpperInvariant(),
            AgeRanges.Find(filters.Age),
            filters.Interests,
            sort);

        // Out-of-range pages are clamped to the first or last one.
        var size = Math.Clamp(filters.Size, 1, PeopleSearch.MaxPageSize);
        var total = await people.CountAsync(search, ct);
        var pageCount = Math.Max(1, (int)Math.Ceiling(total / (double)size));
        var page = Math.Clamp(filters.Page, 1, pageCount);
        var offset = (page - 1) * size;
        var rows = await people.SearchAsync(search, offset, size, ct);

        return new PersonPage(
            [.. rows.Select(PersonDto.From)],
            total,
            page,
            pageCount,
            total > 0 ? offset + 1 : 0,
            offset + rows.Count,
            terms);
    }

    public async Task<DirectorySummary> GetSummaryAsync(CancellationToken ct)
    {
        var stats = await people.GetStatsAsync(ct);
        return new DirectorySummary(
            stats.People,
            (int)Math.Round(stats.AverageAge, MidpointRounding.AwayFromZero),
            [.. stats.Countries.OrderBy(c => c.Name, StringComparer.InvariantCulture)],
            [.. stats.Interests.OrderByDescending(i => i.People).ThenBy(i => i.Name, StringComparer.Ordinal)]);
    }

    public PeopleOptions GetOptions() => new(
        [.. Countries.Names
            .Select(c => new CountryOption(c.Key, c.Value))
            .OrderBy(c => c.Name, StringComparer.InvariantCulture)],
        Interests.All,
        Interests.Max,
        [.. AgeRanges.All.Select(r => new AgeRangeOption(r.Value, r.Min, r.Max))],
        PeopleSearch.SortNames);

    public async Task<IReadOnlyList<PersonAuditEntryDto>> ListAuditLogAsync(int? limit, CancellationToken ct)
    {
        var entries = await people.ListAuditLogAsync(Math.Clamp(limit ?? 50, 1, 200), ct);
        return [.. entries.Select(PersonAuditEntryDto.From)];
    }

    public async Task<Result<PersonDto>> GetAsync(int id, CancellationToken ct) =>
        await people.FindAsync(id, ct) is { } person ? PersonDto.From(person) : PersonErrors.NotFound;

    public async Task<Result<PersonDto>> CreateAsync(PersonInput input, Editor editor, CancellationToken ct)
    {
        var (person, errors) = PersonRules.Validate(input);
        if (person is null) return PersonErrors.Invalid(errors);

        person.MarkCreated(clock.GetUtcNow());
        people.Add(person);
        // Saved first, so the audit entry can name the new person's id.
        if (!await people.TrySaveChangesAsync(ct)) return PersonErrors.DuplicateEmail;

        people.AddAuditEntry(AuditEntry(person.Id, PersonAuditEntry.Created, editor));
        await people.SaveChangesAsync(ct);
        return PersonDto.From(person);
    }

    public async Task<Result<PersonDto>> UpdateAsync(int id, PersonInput input, Editor editor, CancellationToken ct)
    {
        var (details, errors) = PersonRules.Validate(input);
        if (details is null) return PersonErrors.Invalid(errors);

        var person = await people.FindForUpdateAsync(id, ct);
        if (person is null) return PersonErrors.NotFound;

        person.UpdateDetails(details, clock.GetUtcNow());
        people.AddAuditEntry(AuditEntry(person.Id, PersonAuditEntry.Updated, editor));
        if (!await people.TrySaveChangesAsync(ct)) return PersonErrors.DuplicateEmail;

        return PersonDto.From(person);
    }

    public async Task<Result> DeleteAsync(int id, Editor editor, CancellationToken ct)
    {
        if (!await people.DeleteAsync(id, ct)) return PersonErrors.NotFound;

        people.AddAuditEntry(AuditEntry(id, PersonAuditEntry.Deleted, editor));
        await people.SaveChangesAsync(ct);
        return Result.Success;
    }

    private PersonAuditEntry AuditEntry(int personId, string action, Editor editor) => new()
    {
        PersonId = personId,
        Action = action,
        At = clock.GetUtcNow(),
        BySubject = editor.Subject,
        ByName = editor.Name,
        Client = editor.Client,
    };
}
