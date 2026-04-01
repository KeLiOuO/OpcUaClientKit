using Moq;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using OpcUaClientKit.UnitTests.Infrastructure;

namespace OpcUaClientKit.UnitTests.Tests;

public sealed class OpcUaReconnectTests
{
    [Fact]
    public async Task RunReconnectLoopAsync_successfully_reconnects_and_reports_events()
    {
        var reconnectEvents = new List<OpcUaReconnectEvent>();
        var originalSession = CreateSessionMock();
        var reconnectedSession = CreateSessionMock();
        var originalConnection = new OpcUaClientConnection(
            new ApplicationConfiguration(),
            originalSession.Object,
            (_, _) => { });
        var reconnectedConnection = new OpcUaClientConnection(
            new ApplicationConfiguration(),
            reconnectedSession.Object,
            (_, _) => { });

        var connectCallCount = 0;
        var client = new OpcUaClient(
            new OpcUaClientOptions
            {
                ServerUrl = "opc.tcp://127.0.0.1:4840",
                ApplicationName = "UnitTests",
                Reconnect = new OpcUaReconnectOptions
                {
                    Enabled = true,
                    MaxAttempts = 1,
                    InitialDelayMs = 1,
                    MaxDelayMs = 1,
                    BackoffMultiplier = 1.0d,
                    ReconnectHandler = reconnectEvents.Add
                }
            },
            (_, _) =>
            {
                Interlocked.Increment(ref connectCallCount);
                return Task.FromResult(reconnectedConnection);
            });

        ReflectionTestHelpers.SetPrivateField(client, "_connection", originalConnection);
        ReflectionTestHelpers.SetPrivateField(client, "_reconnectLoopCts", new CancellationTokenSource());
        ReflectionTestHelpers.SetPrivateField(client, "_reconnectPending", 1);

        var reconnectLoopCts = ReflectionTestHelpers.GetPrivateField<CancellationTokenSource>(client, "_reconnectLoopCts");
        await (Task)ReflectionTestHelpers.InvokePrivateInstance(client, "RunReconnectLoopAsync", reconnectLoopCts)!;

        Assert.Equal(1, Volatile.Read(ref connectCallCount));
        Assert.Same(reconnectedConnection, ReflectionTestHelpers.GetPrivateField<OpcUaClientConnection?>(client, "_connection"));
        Assert.Equal(0, ReflectionTestHelpers.GetPrivateField<int>(client, "_reconnectPending"));
        Assert.Contains(reconnectEvents, static evt => evt.Kind == OpcUaReconnectEventKind.Reconnecting);
        Assert.Contains(reconnectEvents, static evt => evt.Kind == OpcUaReconnectEventKind.Reconnected);
        Assert.DoesNotContain(reconnectEvents, static evt => evt.Kind == OpcUaReconnectEventKind.GaveUp);
        reconnectedSession.VerifyAdd(mock => mock.KeepAlive += It.IsAny<KeepAliveEventHandler>(), Times.Once);
    }

    [Fact]
    public void ComputeReconnectDelay_returns_zero_for_first_attempt_when_immediate_reconnect_is_enabled()
    {
        var delay = (TimeSpan)ReflectionTestHelpers.InvokePrivateStatic(
            typeof(OpcUaClient),
            "ComputeReconnectDelay",
            1,
            new OpcUaReconnectOptions
            {
                Enabled = true,
                ReconnectImmediatelyOnFirstFailure = true,
                InitialDelayMs = 1000,
                MaxDelayMs = 10000,
                BackoffMultiplier = 2.0d
            })!;

        Assert.Equal(TimeSpan.Zero, delay);
    }

    [Fact]
    public void ComputeReconnectDelay_returns_initial_delay_for_second_attempt_when_immediate_reconnect_is_enabled()
    {
        var delay = (TimeSpan)ReflectionTestHelpers.InvokePrivateStatic(
            typeof(OpcUaClient),
            "ComputeReconnectDelay",
            2,
            new OpcUaReconnectOptions
            {
                Enabled = true,
                ReconnectImmediatelyOnFirstFailure = true,
                InitialDelayMs = 1000,
                MaxDelayMs = 10000,
                BackoffMultiplier = 2.0d
            })!;

        Assert.Equal(TimeSpan.FromMilliseconds(1000), delay);
    }

    [Fact]
    public void ComputeReconnectDelay_returns_initial_delay_for_first_attempt_when_immediate_reconnect_is_disabled()
    {
        var delay = (TimeSpan)ReflectionTestHelpers.InvokePrivateStatic(
            typeof(OpcUaClient),
            "ComputeReconnectDelay",
            1,
            new OpcUaReconnectOptions
            {
                Enabled = true,
                ReconnectImmediatelyOnFirstFailure = false,
                InitialDelayMs = 1000,
                MaxDelayMs = 10000,
                BackoffMultiplier = 2.0d
            })!;

        Assert.Equal(TimeSpan.FromMilliseconds(1000), delay);
    }

    [Fact]
    public async Task RunReconnectLoopAsync_give_up_preserves_handles_and_reports_failure()
    {
        var reconnectEvents = new List<OpcUaReconnectEvent>();
        var originalSession = CreateSessionMock();
        var originalConnection = new OpcUaClientConnection(
            new ApplicationConfiguration(),
            originalSession.Object,
            (_, _) => { });

        var client = new OpcUaClient(
            new OpcUaClientOptions
            {
                ServerUrl = "opc.tcp://127.0.0.1:4840",
                ApplicationName = "UnitTests",
                Reconnect = new OpcUaReconnectOptions
                {
                    Enabled = true,
                    MaxAttempts = 1,
                    InitialDelayMs = 1,
                    MaxDelayMs = 1,
                    BackoffMultiplier = 1.0d,
                    ReconnectHandler = reconnectEvents.Add
                }
            },
            (_, _) => Task.FromException<OpcUaClientConnection>(new InvalidOperationException("connect failed")));

        ReflectionTestHelpers.SetPrivateField(client, "_connection", originalConnection);
        ReflectionTestHelpers.SetPrivateField(client, "_reconnectLoopCts", new CancellationTokenSource());
        ReflectionTestHelpers.SetPrivateField(client, "_reconnectPending", 1);
        AddSeedSubscriptions(client);

        var reconnectLoopCts = ReflectionTestHelpers.GetPrivateField<CancellationTokenSource>(client, "_reconnectLoopCts");
        await (Task)ReflectionTestHelpers.InvokePrivateInstance(client, "RunReconnectLoopAsync", reconnectLoopCts)!;

        Assert.Null(ReflectionTestHelpers.GetPrivateField<OpcUaClientConnection?>(client, "_connection"));
        Assert.Single(ReflectionTestHelpers.GetPrivateField<List<OpcUaSubscriptionHandle>>(client, "_subscriptions"));
        Assert.Single(ReflectionTestHelpers.GetPrivateField<List<OpcUaEventSubscriptionHandle>>(client, "_eventSubscriptions"));
        Assert.All(
            ReflectionTestHelpers.GetPrivateField<List<OpcUaSubscriptionHandle>>(client, "_subscriptions"),
            static subscription => Assert.True(subscription.IsActive));
        Assert.All(
            ReflectionTestHelpers.GetPrivateField<List<OpcUaEventSubscriptionHandle>>(client, "_eventSubscriptions"),
            static subscription => Assert.True(subscription.IsActive));
        Assert.Contains(reconnectEvents, static evt => evt.Kind == OpcUaReconnectEventKind.AttemptFailed);
        Assert.Contains(reconnectEvents, static evt => evt.Kind == OpcUaReconnectEventKind.GaveUp);
        Assert.DoesNotContain(reconnectEvents, static evt => evt.Kind == OpcUaReconnectEventKind.Reconnected);
    }

    private static Mock<ISession> CreateSessionMock()
    {
        var session = new Mock<ISession>(MockBehavior.Strict);

        session.SetupGet(mock => mock.Connected).Returns(true);
        session.SetupAdd(mock => mock.KeepAlive += It.IsAny<KeepAliveEventHandler>());
        session.SetupRemove(mock => mock.KeepAlive -= It.IsAny<KeepAliveEventHandler>());
        session.Setup(mock => mock.Dispose());

        return session;
    }

    private static void AddSeedSubscriptions(OpcUaClient client)
    {
        var subscriptions = ReflectionTestHelpers.GetPrivateField<List<OpcUaSubscriptionHandle>>(client, "_subscriptions");
        subscriptions.Add(new OpcUaSubscriptionHandle(
            "data-subscription",
            new Subscription(DefaultTelemetry.Create(_ => { }), null),
            new OpcUaSubscriptionBuildRequest("data-subscription", 1000, 10, 60, 0, 0, true),
            Array.Empty<OpcUaMonitoredItemRegistration>(),
            new OpcUaSubscriptionState(),
            (_, _, _) => Task.CompletedTask,
            (_, _, _) => Task.CompletedTask,
            (_, _) => Task.CompletedTask));

        var eventSubscriptions = ReflectionTestHelpers.GetPrivateField<List<OpcUaEventSubscriptionHandle>>(client, "_eventSubscriptions");
        eventSubscriptions.Add(new OpcUaEventSubscriptionHandle(
            "event-subscription",
            new Subscription(DefaultTelemetry.Create(_ => { }), null),
            new OpcUaEventSubscriptionBuildRequest(
                "event-subscription",
                1000,
                10,
                60,
                0,
                0,
                true,
                new OpcUaNode(ObjectTypeIds.AlarmConditionType.ToString()),
                null,
                1000,
                true,
                true,
                OpcUaEventSelectClauseMode.Fixed,
                false,
                _ => { }),
            Array.Empty<OpcUaEventMonitoredItemRegistration>(),
            new OpcUaSubscriptionState(),
            CreateFilterDefinition(),
            (_, _, _) => Task.CompletedTask,
            (_, _, _) => Task.CompletedTask,
            (_, _) => Task.CompletedTask,
            (_, _) => Task.CompletedTask));
    }

    private static OpcUaEventFilterDefinition CreateFilterDefinition()
    {
        return new OpcUaEventFilterDefinition(
            OpcUaEventSelectClauseMode.Fixed,
            ObjectTypeIds.AlarmConditionType,
            new[]
            {
                new OpcUaEventSelectClauseDescriptor(
                    OpcUaEventFieldKeys.EventType,
                    OpcUaEventFieldKeys.EventType,
                    new SimpleAttributeOperand
                    {
                        TypeDefinitionId = ObjectTypeIds.BaseEventType,
                        BrowsePath = new QualifiedNameCollection
                        {
                            new(BrowseNames.EventType)
                        },
                        AttributeId = Attributes.Value
                    })
            },
            null,
            false);
    }
}
