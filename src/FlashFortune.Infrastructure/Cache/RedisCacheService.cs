using FlashFortune.Application.Interfaces;
using StackExchange.Redis;

namespace FlashFortune.Infrastructure.Cache;

public sealed class RedisCacheService(IConnectionMultiplexer redis) : ICacheService
{
    private static string ExclusionKey(Guid raffleId) => $"exclusion:{raffleId}";

    public async Task AddToExclusionSetAsync(Guid raffleId, string identityDoc, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        await db.SetAddAsync(ExclusionKey(raffleId), identityDoc);
    }

    public async Task<bool> IsExcludedAsync(Guid raffleId, string identityDoc, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        return await db.SetContainsAsync(ExclusionKey(raffleId), identityDoc);
    }

    public async Task RemoveFromExclusionSetAsync(Guid raffleId, string identityDoc, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        await db.SetRemoveAsync(ExclusionKey(raffleId), identityDoc);
    }

    public async Task ClearExclusionSetAsync(Guid raffleId, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        await db.KeyDeleteAsync(ExclusionKey(raffleId));
    }
}
