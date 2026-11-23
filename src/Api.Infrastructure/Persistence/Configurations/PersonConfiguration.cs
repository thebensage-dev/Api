using Api.Domain.People;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Persistence.Configurations;

internal sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> person)
    {
        person.ToTable("people");
        person.Property(p => p.FullName).HasMaxLength(PersonLimits.NameMax);
        person.Property(p => p.Email).HasMaxLength(PersonLimits.EmailMax);
        person.HasIndex(p => p.Email).IsUnique();
        person.Property(p => p.Phone).HasMaxLength(PersonLimits.PhoneMax);
        person.Property(p => p.Country).HasMaxLength(2).IsFixedLength();
        person.Property(p => p.City).HasMaxLength(PersonLimits.CityMax);
        person.Property(p => p.JobTitle).HasMaxLength(PersonLimits.JobTitleMax);
        person.Property(p => p.Bio).HasMaxLength(PersonLimits.BioMax);
        person.HasIndex(p => p.Country);
        person.HasIndex(p => p.Age);
    }
}
