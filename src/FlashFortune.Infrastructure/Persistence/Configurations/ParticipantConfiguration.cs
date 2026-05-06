using FlashFortune.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlashFortune.Infrastructure.Persistence.Configurations;

public class ParticipantConfiguration : IEntityTypeConfiguration<Participant>
{
    public void Configure(EntityTypeBuilder<Participant> builder)
    {
        builder.HasKey(p => p.Id);

        // PII fields — encrypted at rest via column-level encryption or EF value converters in production
        builder.Property(p => p.FullName).HasMaxLength(400).IsRequired();
        builder.Property(p => p.IdentityDoc).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Phone).HasMaxLength(100);
        builder.Property(p => p.AccountNumber).HasMaxLength(100).IsRequired();
        builder.HasIndex(p => new { p.RaffleId, p.AccountNumber }).IsUnique();

        builder.OwnsOne(p => p.CouponRange, range =>
        {
            range.Property(r => r.VirtualStart).HasColumnName("coupon_virtual_start").IsRequired();
            range.Property(r => r.Count).HasColumnName("coupon_count").IsRequired();
        });
    }
}
