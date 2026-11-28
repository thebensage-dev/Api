using Api.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> user)
    {
        user.ToTable("users");
        user.Property(p => p.FullName).HasMaxLength(UserLimits.NameMax);
        user.Property(p => p.Email).HasMaxLength(UserLimits.EmailMax);
        user.HasIndex(p => p.Email).IsUnique();
        user.Property(p => p.Phone).HasMaxLength(UserLimits.PhoneMax);
        user.Property(p => p.Country).HasMaxLength(2).IsFixedLength();
        user.Property(p => p.City).HasMaxLength(UserLimits.CityMax);
        user.Property(p => p.JobTitle).HasMaxLength(UserLimits.JobTitleMax);
        user.Property(p => p.Bio).HasMaxLength(UserLimits.BioMax);
        user.HasIndex(p => p.Country);
        user.HasIndex(p => p.Age);
    }
}
