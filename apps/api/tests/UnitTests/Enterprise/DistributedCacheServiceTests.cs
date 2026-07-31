using EnterpriseFramework.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace EnterpriseFramework.UnitTests.Enterprise;

public sealed class DistributedCacheServiceTests
{
    private static DistributedCacheService Create(IDistributedCache? cache = null) =>
        new(
            cache
                ?? new MemoryDistributedCache(
                    Options.Create(new MemoryDistributedCacheOptions())
                ),
            NullLogger<DistributedCacheService>.Instance
        );

    /// <summary>A cache whose every operation fails, standing in for Redis being down.</summary>
    private sealed class BrokenCache : IDistributedCache
    {
        public byte[]? Get(string key) => throw new InvalidOperationException("cache down");

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
            throw new InvalidOperationException("cache down");

        public void Refresh(string key) => throw new InvalidOperationException("cache down");

        public Task RefreshAsync(string key, CancellationToken token = default) =>
            throw new InvalidOperationException("cache down");

        public void Remove(string key) => throw new InvalidOperationException("cache down");

        public Task RemoveAsync(string key, CancellationToken token = default) =>
            throw new InvalidOperationException("cache down");

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
            throw new InvalidOperationException("cache down");

        public Task SetAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token = default
        ) => throw new InvalidOperationException("cache down");
    }

    [Fact]
    public async Task ComputesOnMissAndServesFromCacheAfterwards()
    {
        var cache = Create();
        var calls = 0;

        var first = await cache.GetOrCreateAsync(
            "key",
            _ =>
            {
                calls++;
                return Task.FromResult("value");
            },
            TimeSpan.FromMinutes(5)
        );
        var second = await cache.GetOrCreateAsync(
            "key",
            _ =>
            {
                calls++;
                return Task.FromResult("other");
            },
            TimeSpan.FromMinutes(5)
        );

        first.ShouldBe("value");
        second.ShouldBe("value");
        calls.ShouldBe(1);
    }

    [Fact]
    public async Task RemoveForcesRecomputation()
    {
        var cache = Create();
        await cache.GetOrCreateAsync("key", _ => Task.FromResult(1), TimeSpan.FromMinutes(5));

        await cache.RemoveAsync("key");
        var recomputed = await cache.GetOrCreateAsync(
            "key",
            _ => Task.FromResult(2),
            TimeSpan.FromMinutes(5)
        );

        recomputed.ShouldBe(2);
    }

    [Fact]
    public async Task DifferentKeysDoNotShareValues()
    {
        var cache = Create();

        var a = await cache.GetOrCreateAsync("a", _ => Task.FromResult("A"), TimeSpan.FromMinutes(5));
        var b = await cache.GetOrCreateAsync("b", _ => Task.FromResult("B"), TimeSpan.FromMinutes(5));

        a.ShouldBe("A");
        b.ShouldBe("B");
    }

    [Fact]
    public async Task AnUnreachableCacheDegradesToRecomputing()
    {
        var cache = Create(new BrokenCache());

        var value = await cache.GetOrCreateAsync(
            "key",
            _ => Task.FromResult("computed"),
            TimeSpan.FromMinutes(5)
        );

        // A cache exists to make a working system faster; losing it must not
        // turn every request into an error.
        value.ShouldBe("computed");
    }

    [Fact]
    public async Task RemoveOnAnUnreachableCacheDoesNotThrow()
    {
        var cache = Create(new BrokenCache());

        await Should.NotThrowAsync(() => cache.RemoveAsync("key"));
    }

    [Fact]
    public async Task RoundTripsComplexValues()
    {
        var cache = Create();

        var value = await cache.GetOrCreateAsync(
            "key",
            _ => Task.FromResult(new { Name = "Ada", Count = 3 }),
            TimeSpan.FromMinutes(5)
        );
        var cached = await cache.GetOrCreateAsync(
            "key",
            _ => Task.FromResult(new { Name = "Other", Count = 0 }),
            TimeSpan.FromMinutes(5)
        );

        cached.Name.ShouldBe(value.Name);
        cached.Count.ShouldBe(3);
    }
}
