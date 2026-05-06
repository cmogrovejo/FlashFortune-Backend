using FlashFortune.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlashFortune.Infrastructure.Persistence.Configurations;

public class BusinessUnitConfiguration : IEntityTypeConfiguration<BusinessUnit>
{
    public void Configure(EntityTypeBuilder<BusinessUnit> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name)
               .HasMaxLength(200)
               .IsRequired();

        builder.Property(b => b.CorporateId)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(b => b.InstitutionType)
               .HasMaxLength(100);

        builder.Property(b => b.Address)
               .HasMaxLength(500);

        builder.HasQueryFilter(b => !b.IsDeleted);
    }
}
