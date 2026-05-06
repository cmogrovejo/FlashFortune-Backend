using FlashFortune.Application.Interfaces;
using FlashFortune.Domain.Entities;
using FlashFortune.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace FlashFortune.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<BusinessUnit> BusinessUnits => Set<BusinessUnit>();
    public DbSet<UserUnitRole> UserUnitRoles => Set<UserUnitRole>();
    public DbSet<Raffle> Raffles => Set<Raffle>();
    public DbSet<Prize> Prizes => Set<Prize>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Winner> Winners => Set<Winner>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
