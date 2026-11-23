using Api.Domain.People;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Persistence.Configurations;

internal sealed class PersonAuditEntryConfiguration : IEntityTypeConfiguration<PersonAuditEntry>
{
    public void Configure(EntityTypeBuilder<PersonAuditEntry> entry)
    {
        entry.ToTable("person_audit_log");
        entry.Property(e => e.Action).HasMaxLength(16);
        entry.Property(e => e.BySubject).HasMaxLength(255);
        entry.Property(e => e.ByName).HasMaxLength(255);
        entry.Property(e => e.Client).HasMaxLength(255);
        entry.HasIndex(e => e.PersonId);
        entry.HasIndex(e => e.At);
    }
}
