using OpcUaClientKit;
using OpcUaClientKit.RegressionTests.Infrastructure;
using Xunit;

namespace OpcUaClientKit.RegressionTests.Tests;

[Collection(OpcUaIntegrationCollection.Name)]
public sealed class OpcUaDataSubscriptionRegressionTests
{
    private readonly OpcUaIntegrationFixture _fixture;

    public OpcUaDataSubscriptionRegressionTests(OpcUaIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Can_receive_data_change_notification_after_write()
    {
        var settings = _fixture.Settings;
        var observedNode = settings.WritableNode1.ToOpcUaNode();

        await using var client = await _fixture.CreateConnectedClientAsync();
        var originalValue = await client.ReadNodeAsync<short>(observedNode);
        var targetValue = originalValue == settings.SubscriptionWriteValue
            ? checked((short)(settings.SubscriptionWriteValue + 1))
            : settings.SubscriptionWriteValue;

        var notificationSource = new TaskCompletionSource<OpcUaValueChangeNotification>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        await using var subscription = await client
            .AsSubscribable()
            .CreateSubscriptionBuilder()
            .WithName("regression-data-subscription")
            .WithPublishingInterval(250)
            .BuildAsync();

        await subscription.AddNodeAsync(
            observedNode,
            notification =>
            {
                if (!string.Equals(notification.NodeId, observedNode.NodeId, StringComparison.Ordinal))
                {
                    return;
                }

                if (RegressionTestHelpers.ToInt16(notification.Value, observedNode.NodeId) != targetValue)
                {
                    return;
                }

                notificationSource.TrySetResult(notification);
            });

        try
        {
            await Task.Delay(300);
            await client.WriteNodeAsync(observedNode, targetValue);

            var notification = await RegressionTestHelpers.WaitAsync(
                notificationSource.Task,
                settings.WaitTimeout,
                $"Timed out waiting for a data change notification from '{observedNode.NodeId}'.");

            Assert.True(notification.IsGood);
            Assert.Equal(observedNode.NodeId, notification.NodeId);
        }
        finally
        {
            await client.WriteNodeAsync(observedNode, originalValue);
        }
    }

    [Fact]
    public async Task Can_restore_data_subscription_after_reconnect()
    {
        var settings = _fixture.Settings;
        var observedNode = settings.WritableNode1.ToOpcUaNode();

        await using var client = await _fixture.CreateReconnectEnabledConnectedClientAsync();
        var originalValue = await client.ReadNodeAsync<short>(observedNode);
        var targetValue = originalValue == settings.SubscriptionWriteValue
            ? checked((short)(settings.SubscriptionWriteValue + 1))
            : settings.SubscriptionWriteValue;

        var notificationSource = new TaskCompletionSource<OpcUaValueChangeNotification>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        await using var subscription = await client
            .AsSubscribable()
            .CreateSubscriptionBuilder()
            .WithName("regression-data-subscription-reconnect")
            .WithPublishingInterval(250)
            .BuildAsync();

        await subscription.AddNodeAsync(
            observedNode,
            notification =>
            {
                if (!string.Equals(notification.NodeId, observedNode.NodeId, StringComparison.Ordinal))
                {
                    return;
                }

                if (RegressionTestHelpers.ToInt16(notification.Value, observedNode.NodeId) != targetValue)
                {
                    return;
                }

                notificationSource.TrySetResult(notification);
            });

        try
        {
            var reconnectLoopCts = RegressionTestHelpers.GetPrivateField<CancellationTokenSource>(
                client,
                "_reconnectLoopCts");
            RegressionTestHelpers.SetPrivateField(client, "_reconnectPending", 1);

            await RegressionTestHelpers.InvokePrivateTaskAsync(
                client,
                "RunReconnectLoopAsync",
                reconnectLoopCts);

            await Task.Delay(300);
            await client.WriteNodeAsync(observedNode, targetValue);

            var notification = await RegressionTestHelpers.WaitAsync(
                notificationSource.Task,
                settings.WaitTimeout,
                $"Timed out waiting for a restored data subscription notification from '{observedNode.NodeId}'.");

            Assert.True(subscription.IsActive);
            Assert.True(notification.IsGood);
            Assert.Equal(observedNode.NodeId, notification.NodeId);
        }
        finally
        {
            await client.WriteNodeAsync(observedNode, originalValue);
        }
    }
}
