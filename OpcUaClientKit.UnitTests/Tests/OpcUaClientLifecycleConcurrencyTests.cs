using Moq;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using OpcUaClientKit.UnitTests.Infrastructure;

namespace OpcUaClientKit.UnitTests.Tests;

public sealed class OpcUaClientLifecycleConcurrencyTests
{
    [Fact]
    public async Task DisposeAsync_and_DisconnectAsync_concurrently_cleanup_subscriptions_once()
    {
        var session = CreateConnectedSessionMock();
        var connection = new OpcUaClientConnection(new ApplicationConfiguration(), session.Object, (_, _) => { });
        var client = CreateClient(connection);

        ReflectionTestHelpers.SetPrivateField(client, "_connection", connection);
        AddSeedSubscriptions(client);

        var disconnectTask = client.DisconnectAsync();
        var disposeTask = client.DisposeAsync().AsTask();

        await Task.WhenAll(disconnectTask, disposeTask);

        Assert.Null(ReflectionTestHelpers.GetPrivateField<OpcUaClientConnection?>(client, "_connection"));

        session.Verify(
            mock => mock.RemoveSubscriptionAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        session.Verify(mock => mock.Dispose(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_waits_for_inflight_connect_and_prevents_reuse()
    {
        var session = CreateConnectedSessionMock();
        var connection = new OpcUaClientConnection(new ApplicationConfiguration(), session.Object, (_, _) => { });

        var connectStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowConnectToFinish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var connectCallCount = 0;

        var client = new OpcUaClient(
            new OpcUaClientOptions
            {
                ServerUrl = "opc.tcp://127.0.0.1:4840",
                ApplicationName = "UnitTests"
            },
            async (_, _) =>
            {
                Interlocked.Increment(ref connectCallCount);
                connectStarted.SetResult();
                await allowConnectToFinish.Task;
                return connection;
            });

        var connectTask = client.ConnectAsync();
        await connectStarted.Task;

        var disposeTask = client.DisposeAsync().AsTask();
        allowConnectToFinish.SetResult();

        await Task.WhenAll(connectTask, disposeTask);

        Assert.Equal(1, Volatile.Read(ref connectCallCount));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.ConnectAsync());
        session.Verify(mock => mock.Dispose(), Times.Once);
    }

    private static OpcUaClient CreateClient(OpcUaClientConnection connection)
    {
        return new OpcUaClient(
            new OpcUaClientOptions
            {
                ServerUrl = "opc.tcp://127.0.0.1:4840",
                ApplicationName = "UnitTests"
            },
            (_, _) => Task.FromResult(connection));
    }

    private static Mock<ISession> CreateConnectedSessionMock()
    {
        var session = new Mock<ISession>(MockBehavior.Strict);

        session.SetupGet(mock => mock.Connected).Returns(true);
        session.Setup(mock => mock.RemoveSubscriptionAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        session.Setup(mock => mock.CloseAsync(It.IsAny<int>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StatusCodes.Good);
        session.Setup(mock => mock.Dispose());

        return session;
    }

    private static void AddSeedSubscriptions(OpcUaClient client)
    {
        var subscriptions = ReflectionTestHelpers.GetPrivateField<List<OpcUaSubscriptionHandle>>(client, "_subscriptions");
        subscriptions.Add(new OpcUaSubscriptionHandle(
            "data-subscription",
            new Subscription(DefaultTelemetry.Create(_ => { }), null),
            Array.Empty<OpcUaMonitoredItemRegistration>(),
            new OpcUaSubscriptionState(),
            (_, _, _) => Task.CompletedTask,
            (_, _, _) => Task.CompletedTask,
            (_, _) => Task.CompletedTask));

        var eventSubscriptions = ReflectionTestHelpers.GetPrivateField<List<OpcUaEventSubscriptionHandle>>(client, "_eventSubscriptions");
        eventSubscriptions.Add(new OpcUaEventSubscriptionHandle(
            "event-subscription",
            new Subscription(DefaultTelemetry.Create(_ => { }), null),
            Array.Empty<OpcUaEventMonitoredItemRegistration>(),
            new OpcUaSubscriptionState(),
            CreateFilterDefinition(),
            queueSize: 1000,
            discardOldest: true,
            conditionRefreshOnStart: true,
            onEvent: _ => { },
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
