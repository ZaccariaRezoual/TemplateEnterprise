using EnterpriseFramework.Modules.Realtime.Abstractions;
using EnterpriseFramework.Modules.Realtime.Services;
using Shouldly;
using Xunit;

namespace EnterpriseFramework.UnitTests.Realtime;

public sealed class InMemoryConnectionRegistryTests
{
    private static RealtimeConnection Connection(Guid userId, string connectionId) =>
        new(connectionId, userId, ["user:x"], DateTime.UtcNow);

    [Fact]
    public async Task Add_MakesTheAccountOnline()
    {
        var registry = new InMemoryConnectionRegistry();
        var userId = Guid.NewGuid();

        await registry.AddAsync(Connection(userId, "c1"));

        (await registry.IsOnlineAsync(userId)).ShouldBeTrue();
        (await registry.GetOnlineUsersAsync()).ShouldBe([userId]);
    }

    [Fact]
    public async Task ClosingOneOfSeveralConnections_KeepsTheAccountOnline()
    {
        var registry = new InMemoryConnectionRegistry();
        var userId = Guid.NewGuid();
        await registry.AddAsync(Connection(userId, "tab-1"));
        await registry.AddAsync(Connection(userId, "tab-2"));

        await registry.RemoveAsync("tab-1");

        // Closing one tab must not report the user as offline.
        (await registry.IsOnlineAsync(userId)).ShouldBeTrue();
    }

    [Fact]
    public async Task ClosingTheLastConnection_TakesTheAccountOffline()
    {
        var registry = new InMemoryConnectionRegistry();
        var userId = Guid.NewGuid();
        await registry.AddAsync(Connection(userId, "only"));

        await registry.RemoveAsync("only");

        (await registry.IsOnlineAsync(userId)).ShouldBeFalse();
        (await registry.GetOnlineUsersAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task GetOnlineUsers_ReportsEachAccountOnce()
    {
        var registry = new InMemoryConnectionRegistry();
        var userId = Guid.NewGuid();
        await registry.AddAsync(Connection(userId, "tab-1"));
        await registry.AddAsync(Connection(userId, "tab-2"));

        (await registry.GetOnlineUsersAsync()).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Remove_IsSafeForAnUnknownConnection()
    {
        var registry = new InMemoryConnectionRegistry();

        await Should.NotThrowAsync(() => registry.RemoveAsync("never-existed"));
    }
}
