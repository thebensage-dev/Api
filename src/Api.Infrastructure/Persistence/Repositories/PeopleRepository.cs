using Api.Application.People;
using Api.Application.People.Dtos;
using Api.Domain.People;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Api.Infrastructure.Persistence.Repositories;

internal sealed class PeopleRepository(AppDbContext db) : IPeopleRepository
{
    public Task<int> CountAsync(PersonSearch search, CancellationToken ct) =>
        db.People.AsNoTracking().Matching(search).CountAsync(ct);

    public async Task<IReadOnlyList<Person>> SearchAsync(PersonSearch search, int skip, int take, CancellationToken ct) =>
        await db.People.AsNoTracking().Matching(search).SortedBy(search).Skip(skip).Take(take).ToListAsync(ct);

    public async Task<PeopleStats> GetStatsAsync(CancellationToken ct)
    {
        var people = db.People.AsNoTracking();
        var count = await people.CountAsync(ct);
        var averageAge = count > 0 ? await people.AverageAsync(p => p.Age, ct) : 0;
        var countries = await people
            .GroupBy(p => new { p.Country, p.CountryName })
            .Select(g => new CountryCount(g.Key.Country, g.Key.CountryName, g.Count()))
            .ToListAsync(ct);
        var interests = await people
            .SelectMany(p => p.Interests)
            .GroupBy(i => i)
            .Select(g => new InterestCount(g.Key, g.Count()))
            .ToListAsync(ct);
        return new PeopleStats(count, averageAge, countries, interests);
    }

    public Task<Person?> FindAsync(int id, CancellationToken ct) =>
        db.People.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Person?> FindForUpdateAsync(int id, CancellationToken ct) =>
        db.People.FirstOrDefaultAsync(p => p.Id == id, ct);

    public void Add(Person person) => db.People.Add(person);

    public async Task<bool> DeleteAsync(int id, CancellationToken ct) =>
        await db.People.Where(p => p.Id == id).ExecuteDeleteAsync(ct) > 0;

    public async Task<IReadOnlyList<PersonAuditEntry>> ListAuditLogAsync(int take, CancellationToken ct) =>
        await db.PersonAuditLog.AsNoTracking().OrderByDescending(c => c.Id).Take(take).ToListAsync(ct);

    public void AddAuditEntry(PersonAuditEntry entry) => db.PersonAuditLog.Add(entry);

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
