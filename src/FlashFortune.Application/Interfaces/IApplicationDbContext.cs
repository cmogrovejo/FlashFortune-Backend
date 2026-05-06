using FlashFortune.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlashFortune.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<BusinessUnit> BusinessUnits { get; }
    DbSet<UserUnitRole> UserUnitRoles { get; }
    DbSet<Raffle> Raffles { get; }
    DbSet<Prize> Prizes { get; }
    DbSet<Participant> Participants { get; }
    DbSet<Winner> Winners { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
