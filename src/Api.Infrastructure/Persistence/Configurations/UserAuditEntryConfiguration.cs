using Api.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Persistence.Configurations;

internal sealed class UserAuditEntryConfiguration : IEntityTypeConfiguration<UserAuditEntry>
{
    public void Configure(EntityTypeBuilder<UserAuditEntry> entry)
    {
        entry.ToTable("user_audit_log");
        entry.Property(e => e.Action).HasMaxLength(16);
        entry.Property(e => e.BySubject).HasMaxLength(255);
        entry.Property(e => e.ByName).HasMaxLength(255);
        entry.Property(e => e.Client).HasMaxLength(255);
        entry.HasIndex(e => e.UserId);
        entry.HasIndex(e => e.At);
    }
}
