using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Modules.Realtime.Hubs;
using EnterpriseFramework.Modules.Realtime.Services;
using Microsoft.AspNetCore.SignalR;
using Shouldly;
using Xunit;

namespace EnterpriseFramework.UnitTests.Realtime;

/// <summary>
/// The dispatcher decides WHO receives an event. Getting this wrong leaks one
/// user's data to another with no error anywhere, so the targeting rules are
/// asserted explicitly.
/// </summary>
public sealed class RealtimeDispatcherTests
{
    private sealed record TestEvent(RealtimeAudience Audience, string Channel = "test.event")
        : IRealtimeEvent
    {
        public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
    }

    /// <summary>Records which client set a send was routed to.</summary>
    private sealed class RecordingHubContext : IHubContext<RealtimeHub>
    {
        public RecordingClients Recorder { get; } = new();

        public IHubClients Clients => Recorder;

        public IGroupManager Groups => throw new NotSupportedException();
    }

    private sealed class RecordingClients : IHubClients
    {
        public string? TargetedGroup { get; private set; }
        public bool TargetedAll { get; private set; }
        public RecordingProxy LastProxy { get; } = new();

        public IClientProxy All
        {
            get
            {
                TargetedAll = true;
                return LastProxy;
            }
        }

        public IClientProxy Group(string groupName)
        {
            TargetedGroup = groupName;
            return LastProxy;
        }

        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => LastProxy;

        public IClientProxy Client(string connectionId) => LastProxy;

        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => LastProxy;

        public IClientProxy GroupExcept(
            string groupName,
            IReadOnlyList<string> excludedConnectionIds
        ) => LastProxy;

        public IClientProxy Groups(IReadOnlyList<string> groupNames) => LastProxy;

        public IClientProxy User(string userId) => LastProxy;

        public IClientProxy Users(IReadOnlyList<string> userIds) => LastProxy;
    }

    private sealed class RecordingProxy : IClientProxy
    {
        public string? Method { get; private set; }
        public object?[]? Arguments { get; private set; }

        public Task SendCoreAsync(
            string method,
            object?[] args,
            CancellationToken cancellationToken = default
        )
        {
            Method = method;
            Arguments = args;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Dispatch_ToAUser_SendsOnlyToThatUsersGroup()
    {
        var userId = Guid.NewGuid();
        var context = new RecordingHubContext();
        var dispatcher = new RealtimeDispatcher(context);

        await dispatcher.DispatchAsync(new TestEvent(RealtimeAudience.ForUser(userId)));

        context.Recorder.TargetedGroup.ShouldBe(RealtimeGroups.ForUser(userId));
        context.Recorder.TargetedAll.ShouldBeFalse();
    }

    [Fact]
    public async Task Dispatch_ToAGroup_SendsToThatGroup()
    {
        var context = new RecordingHubContext();
        var dispatcher = new RealtimeDispatcher(context);

        await dispatcher.DispatchAsync(new TestEvent(RealtimeAudience.ForGroup("role:Admin")));

        context.Recorder.TargetedGroup.ShouldBe("role:Admin");
    }

    [Fact]
    public async Task Dispatch_Broadcast_ReachesEveryone()
    {
        var context = new RecordingHubContext();
        var dispatcher = new RealtimeDispatcher(context);

        await dispatcher.DispatchAsync(new TestEvent(RealtimeAudience.Everyone));

        context.Recorder.TargetedAll.ShouldBeTrue();
        context.Recorder.TargetedGroup.ShouldBeNull();
    }

    [Fact]
    public async Task Dispatch_WithAnAudienceTargetingNobody_ReachesNobody()
    {
        var context = new RecordingHubContext();
        var dispatcher = new RealtimeDispatcher(context);

        // An empty audience must fail closed. Falling back to broadcast would
        // turn a misconfigured event into a data leak.
        await dispatcher.DispatchAsync(new TestEvent(new RealtimeAudience()));

        context.Recorder.TargetedAll.ShouldBeFalse();
        context.Recorder.TargetedGroup.ShouldBeNull();
    }

    [Fact]
    public async Task Dispatch_WrapsTheEventInAnEnvelopeCarryingTheChannel()
    {
        var context = new RecordingHubContext();
        var dispatcher = new RealtimeDispatcher(context);
        var domainEvent = new TestEvent(RealtimeAudience.Everyone, "orders.created");

        await dispatcher.DispatchAsync(domainEvent);

        context.Recorder.LastProxy.Method.ShouldBe(RealtimeDispatcher.ClientMethod);
        var envelope = context.Recorder.LastProxy.Arguments![0].ShouldBeOfType<RealtimeEnvelope>();
        envelope.Channel.ShouldBe("orders.created");
        envelope.Payload.ShouldBe(domainEvent);
    }
}
