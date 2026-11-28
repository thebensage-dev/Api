using Api.Application.Users;
using Api.Application.Users.Dtos;
using Api.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Api.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<int> CountAsync(UserSearch search, CancellationToken ct) =>
        db.Users.AsNoTracking().Matching(search).CountAsync(ct);

    public async Task<IReadOnlyList<User>> SearchAsync(UserSearch search, int skip, int take, CancellationToken ct) =>
        await db.Users.AsNoTracking().Matching(search).SortedBy(search).Skip(skip).Take(take).ToListAsync(ct);

    public async Task<UserStats> GetStatsAsync(CancellationToken ct)
    {
        var users = db.Users.AsNoTracking();
        var count = await users.CountAsync(ct);
        var averageAge = count > 0 ? await users.AverageAsync(p => p.Age, ct) : 0;
        var countries = await users
            .GroupBy(p => new { p.Country, p.CountryName })
            .Select(g => new CountryCount(g.Key.Country, g.Key.CountryName, g.Count()))
            .ToListAsync(ct);
        var interests = await users
            .SelectMany(p => p.Interests)
            .GroupBy(i => i)
            .Select(g => new InterestCount(g.Key, g.Count()))
            .ToListAsync(ct);
        return new UserStats(count, averageAge, countries, interests);
    }

    public Task<User?> FindAsync(int id, CancellationToken ct) =>
        db.Users.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<User?> FindForUpdateAsync(int id, CancellationToken ct) =>
        db.Users.FirstOrDefaultAsync(p => p.Id == id, ct);

    public void Add(User user) => db.Users.Add(user);

    public async Task<bool> DeleteAsync(int id, CancellationToken ct) =>
        await db.Users.Where(p => p.Id == id).ExecuteDeleteAsync(ct) > 0;

    public async Task<IReadOnlyList<UserAuditEntry>> ListAuditLogAsync(int take, CancellationToken ct) =>
        await db.UserAuditLog.AsNoTracking().OrderByDescending(c => c.Id).Take(take).ToListAsync(ct);

    public void AddAuditEntry(UserAuditEntry entry) => db.UserAuditLog.Add(entry);

    public async Task<bool> TrySaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            db.ChangeTracker.Clear();
            return false;
        }
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
