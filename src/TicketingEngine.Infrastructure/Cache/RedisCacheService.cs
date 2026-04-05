using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using TicketingEngine.Application.Interfaces;

namespace TicketingEngine.Infrastructure.Cache;

public sealed class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private static readonly JsonSerializerOptions _opts = new()
        { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public RedisCacheService(IDistributedCache cache) => _cache = cache;

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var bytes = await _cache.GetAsync(key, ct);
        return bytes is null ? default : JsonSerializer.Deserialize<T>(bytes, _opts);
    }

    public async Task SetAsync<T>(string key, T value,
        TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, _opts);
        var opts  = new DistributedCacheEntryOptions();
        if (expiry.HasValue)
            opts.AbsoluteExpirationRelativeToNow = expiry;
        await _cache.SetAsync(key, bytes, opts, ct);
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
        => _cache.RemoveAsync(key, ct);
}
