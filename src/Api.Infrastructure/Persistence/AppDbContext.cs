using Api.Domain.People;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Person> People => Set<Person>();
    public DbSet<PersonAuditEntry> PersonAuditLog => Set<PersonAuditEntry>();

    /// <summary>Every <c>IEntityTypeConfiguration</c> in <c>Configurations/</c>.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
