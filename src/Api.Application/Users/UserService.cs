using Api.Application.Common;
using Api.Application.Users.Dtos;
using Api.Domain.Common;
using Api.Domain.Users;

namespace Api.Application.Users;

/// <summary>The users use cases, on top of <see cref="IUserRepository"/>.</summary>
public sealed class UserService(IUserRepository users, TimeProvider clock) : IUserService
{
    public async Task<UserPage> ListAsync(UserFilters filters, CancellationToken ct)
    {
        var terms = Text.SearchTerms(filters.Q);
        var sort = filters.Sort is { } name && UserSearchRules.Sorts.TryGetValue(name, out var known)
            ? known
            : terms.Length > 0 ? UserSort.BestMatch : UserSort.NewestFirst;
        var search = new UserSearch(
            terms,
            string.IsNullOrEmpty(filters.Country) ? null : filters.Country.ToUpperInvariant(),
            AgeRanges.Find(filters.Age),
            filters.Interests,
            sort);

        // Out-of-range pages are clamped to the first or last one.
        var size = Math.Clamp(filters.Size, 1, UserSearchRules.MaxPageSize);
        var total = await users.CountAsync(search, ct);
        var pageCount = Math.Max(1, (int)Math.Ceiling(total / (double)size));
        var page = Math.Clamp(filters.Page, 1, pageCount);
        var offset = (page - 1) * size;
        var rows = await users.SearchAsync(search, offset, size, ct);

        return new UserPage(
            [.. rows.Select(UserDto.From)],
            total,
            page,
            pageCount,
            total > 0 ? offset + 1 : 0,
            offset + rows.Count,
            terms);
    }

    public async Task<DirectorySummary> GetSummaryAsync(CancellationToken ct)
    {
        var stats = await users.GetStatsAsync(ct);
        return new DirectorySummary(
            stats.Users,
            (int)Math.Round(stats.AverageAge, MidpointRounding.AwayFromZero),
            [.. stats.Countries.OrderBy(c => c.Name, StringComparer.InvariantCulture)],
            [.. stats.Interests.OrderByDescending(i => i.Users).ThenBy(i => i.Name, StringComparer.Ordinal)]);
    }

    public UserOptions GetOptions() => new(
        [.. Countries.Names
            .Select(c => new CountryOption(c.Key, c.Value))
            .OrderBy(c => c.Name, StringComparer.InvariantCulture)],
        Interests.All,
        Interests.Max,
        [.. AgeRanges.All.Select(r => new AgeRangeOption(r.Value, r.Min, r.Max))],
        UserSearchRules.SortNames);

    public async Task<IReadOnlyList<UserAuditEntryDto>> ListAuditLogAsync(int? limit, CancellationToken ct)
    {
        var entries = await users.ListAuditLogAsync(Math.Clamp(limit ?? 50, 1, 200), ct);
        return [.. entries.Select(UserAuditEntryDto.From)];
    }

    public async Task<Result<UserDto>> GetAsync(int id, CancellationToken ct) =>
        await users.FindAsync(id, ct) is { } user ? UserDto.From(user) : UserErrors.NotFound;

    public async Task<Result<UserDto>> CreateAsync(UserInput input, Editor editor, CancellationToken ct)
    {
        var (user, errors) = UserRules.Validate(input);
        if (user is null) return UserErrors.Invalid(errors);

        user.MarkCreated(clock.GetUtcNow());
        users.Add(user);
        // Saved first, so the audit entry can name the new user's id.
        if (!await users.TrySaveChangesAsync(ct)) return UserErrors.DuplicateEmail;

        users.AddAuditEntry(AuditEntry(user.Id, UserAuditEntry.Created, editor));
        await users.SaveChangesAsync(ct);
        return UserDto.From(user);
    }

    public async Task<Result<UserDto>> UpdateAsync(int id, UserInput input, Editor editor, CancellationToken ct)
    {
        var (details, errors) = UserRules.Validate(input);
        if (details is null) return UserErrors.Invalid(errors);

        var user = await users.FindForUpdateAsync(id, ct);
        if (user is null) return UserErrors.NotFound;

        user.UpdateDetails(details, clock.GetUtcNow());
        users.AddAuditEntry(AuditEntry(user.Id, UserAuditEntry.Updated, editor));
        if (!await users.TrySaveChangesAsync(ct)) return UserErrors.DuplicateEmail;

        return UserDto.From(user);
    }

    public async Task<Result> DeleteAsync(int id, Editor editor, CancellationToken ct)
    {
        if (!await users.DeleteAsync(id, ct)) return UserErrors.NotFound;

        users.AddAuditEntry(AuditEntry(id, UserAuditEntry.Deleted, editor));
        await users.SaveChangesAsync(ct);
        return Result.Success;
    }

    private UserAuditEntry AuditEntry(int userId, string action, Editor editor) => new()
    {
        UserId = userId,
        Action = action,
        At = clock.GetUtcNow(),
        BySubject = editor.Subject,
        ByName = editor.Name,
        Client = editor.Client,
    };
}
