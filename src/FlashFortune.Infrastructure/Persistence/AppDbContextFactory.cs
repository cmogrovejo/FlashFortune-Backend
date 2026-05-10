using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FlashFortune.Infrastructure.Persistence;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../FlashFortune.API"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(config.GetConnectionString("Postgres"))
            .Options;

        return new AppDbContext(options);
    }
}
