using Microsoft.Extensions.Caching.Hybrid;
using SnpDepotManagement.Application.Abstractions.Caching;
using SnpDepotManagement.Application.Abstractions.Tenancy;
using StackExchange.Redis;

namespace SnpDepotManagement.Infrastructure.Caching;

public sealed class HybridCacheService : ICacheService
{
    private readonly HybridCache _hybridCache;
    private readonly ITenantProvider _tenantProvider;
    private readonly IConnectionMultiplexer? _redis;

    public HybridCacheService(
        HybridCache hybridCache,
        ITenantProvider tenantProvider,
        IConnectionMultiplexer? redis = null)
    {
        _hybridCache = hybridCache ?? throw new ArgumentNullException(nameof(hybridCache));
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
        _redis = redis;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var resolvedKey = BuildKey(key);

        try
        {
            // HybridCache GetOrCreate with null default value
            return await _hybridCache.GetOrCreateAsync<T?>(
                resolvedKey,
                _ => ValueTask.FromResult<T?>(default),
                cancellationToken: cancellationToken);
        }
        catch
        {
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var resolvedKey = BuildKey(key);

        var options = expiration.HasValue
            ? new HybridCacheEntryOptions
            {
                Expiration = expiration.Value,
                LocalCacheExpiration = expiration.Value
            }
            : null;

        var tags = GetTags(key);

        await _hybridCache.SetAsync(resolvedKey, value, options, tags, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var resolvedKey = BuildKey(key);

        await _hybridCache.RemoveAsync(resolvedKey, cancellationToken);
    }

    public async Task RemoveByPrefixAsync(string prefixKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefixKey);
        var resolvedPrefix = BuildKey(prefixKey);

        // Invalidate via HybridCache tag
        await _hybridCache.RemoveByTagAsync(resolvedPrefix, cancellationToken);

        // Invalidate via Redis SCAN and delete if Redis connection is active
        if (_redis is not null && _redis.IsConnected)
        {
            try
            {
                var db = _redis.GetDatabase();
                foreach (var endpoint in _redis.GetEndPoints())
                {
                    var server = _redis.GetServer(endpoint);
                    if (server.IsConnected)
                    {
                        var keys = server.Keys(pattern: $"{resolvedPrefix}*").ToArray();
                        if (keys.Length > 0)
                        {
                            await db.KeyDeleteAsync(keys);
                        }
                    }
                }
            }
            catch
            {
                // Fallback gracefully if Redis command fails
            }
        }
    }

    private string BuildKey(string key)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var prefix = !string.IsNullOrWhiteSpace(tenantId) ? $"tenant:{tenantId}:" : "global:";
        return key.StartsWith("tenant:", StringComparison.OrdinalIgnoreCase) ||
               key.StartsWith("global:", StringComparison.OrdinalIgnoreCase)
            ? key
            : $"{prefix}{key}";
    }

    private IEnumerable<string> GetTags(string key)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var prefix = !string.IsNullOrWhiteSpace(tenantId) ? $"tenant:{tenantId}:" : "global:";

        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { $"{prefix}{key}" };

        var parts = key.Split(':', StringSplitOptions.RemoveEmptyEntries);
        var accumulated = string.Empty;
        for (int i = 0; i < parts.Length - 1; i++)
        {
            accumulated = i == 0 ? parts[i] : $"{accumulated}:{parts[i]}";
            tags.Add($"{prefix}{accumulated}");
        }

        return tags;
    }
}
