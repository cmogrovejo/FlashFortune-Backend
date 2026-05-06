using FlashFortune.Domain.Entities;
using FlashFortune.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlashFortune.Infrastructure.Persistence.Configurations;

public class RaffleConfiguration : IEntityTypeConfiguration<Raffle>
{
    public void Configure(EntityTypeBuilder<Raffle> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Status).IsRequired();
        builder.Property(r => r.ConversionFactor).HasPrecision(18, 4);

        builder.OwnsOne(r => r.Seed, seed =>
        {
            seed.Property(s => s.RandomComponent).HasColumnName("seed_random").HasMaxLength(128);
            seed.Property(s => s.FileHash).HasColumnName("seed_file_hash").HasMaxLength(128);
        });

        builder.HasMany(r => r.Prizes).WithOne().HasForeignKey(p => p.RaffleId);
        builder.HasMany(r => r.Participants).WithOne().HasForeignKey(p => p.RaffleId);
        builder.HasMany(r => r.Winners).WithOne().HasForeignKey(w => w.RaffleId);

        builder.Navigation(r => r.Prizes).AutoInclude();
    }
}
