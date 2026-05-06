using Amazon.S3;
using FlashFortune.Application.Interfaces;
using FlashFortune.Domain.Interfaces;
using FlashFortune.Infrastructure.BackgroundJobs;
using FlashFortune.Infrastructure.Cache;
using FlashFortune.Infrastructure.Engine;
using FlashFortune.Infrastructure.Identity;
using FlashFortune.Infrastructure.Persistence;
using FlashFortune.Infrastructure.Storage;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace FlashFortune.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("Postgres")));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // Redis
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(config.GetConnectionString("Redis")!));
        services.AddScoped<ICacheService, RedisCacheService>();

        // S3 / MinIO
        services.AddSingleton<IAmazonS3>(_ =>
        {
            var s3Config = new AmazonS3Config
            {
                ServiceURL = config["Storage:ServiceUrl"],
                ForcePathStyle = true   // Required for MinIO
            };
            return new AmazonS3Client(
                config["Storage:AccessKey"],
                config["Storage:SecretKey"],
                s3Config);
        });
        services.AddScoped<IFileStorageService, S3FileStorageService>();

        // Hangfire
        services.AddHangfire(cfg => cfg
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(config.GetConnectionString("Postgres"))));
        services.AddHangfireServer();
        services.AddScoped<IBackgroundJobService, HangfireJobService>();
        services.AddScoped<FileIngestionJob>();

        // Domain services
        services.AddSingleton<IPermutationAlgorithm, FeistelPermutation>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();

        return services;
    }
}
