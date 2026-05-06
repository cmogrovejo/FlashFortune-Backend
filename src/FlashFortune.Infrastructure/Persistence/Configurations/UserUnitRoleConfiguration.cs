using FlashFortune.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlashFortune.Infrastructure.Persistence.Configurations;

public class UserUnitRoleConfiguration : IEntityTypeConfiguration<UserUnitRole>
{
    public void Configure(EntityTypeBuilder<UserUnitRole> builder)
    {
        builder.HasKey(r => new { r.UserId, r.BusinessUnitId, r.Role });

        builder.HasOne<User>()
               .WithMany(u => u.UnitRoles)
               .HasForeignKey(r => r.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<BusinessUnit>()
               .WithMany()
               .HasForeignKey(r => r.BusinessUnitId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
