using OpcUaClientKit;
using OpcUaClientKit.RegressionTests.Infrastructure;
using Xunit;

namespace OpcUaClientKit.RegressionTests.Tests;

[Collection(OpcUaIntegrationCollection.Name)]
public sealed class OpcUaEventSubscriptionRegressionTests
{
    private readonly OpcUaIntegrationFixture _fixture;

    public OpcUaEventSubscriptionRegressionTests(OpcUaIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Can_receive_alarm_event_after_trigger_write()
    {
        var settings = _fixture.Settings;
        var triggerNode = settings.LevelNode.ToOpcUaNode();
        var alarmSourceNode = settings.AlarmSourceNode.ToOpcUaNode();

        await using var client = await _fixture.CreateConnectedClientAsync();
        var originalValue = await client.ReadNodeAsync<double>(triggerNode);
        var eventSource = new TaskCompletionSource<OpcUaEventNotification>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            await client.WriteNodeAsync(triggerNode, settings.EventClearValue);
            await Task.Delay(750);

            await using var subscription = await client
                .AsEventSubscribable()
                .CreateEventSubscriptionBuilder()
                .WithName("regression-event-subscription")
                .WithPublishingInterval(250)
                .WithConditionRefreshOnStart(false)
                .BuildAsync(
                    notification =>
                    {
                        if (!string.Equals(notification.SourceNodeId, triggerNode.NodeId, StringComparison.Ordinal))
                        {
                            return;
                        }

                        if (notification.Active != true)
                        {
                            return;
                        }

                        if (string.IsNullOrWhiteSpace(notification.Message) ||
                            notification.Message.IndexOf(settings.ExpectedEventMessageContains, StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            return;
                        }

                        eventSource.TrySetResult(notification);
                    });

            await subscription.AddSourceAsync(alarmSourceNode);
            await Task.Delay(750);
            await client.WriteNodeAsync(triggerNode, settings.EventActiveValue);

            var notification = await RegressionTestHelpers.TryWaitAsync(
                eventSource.Task,
                TimeSpan.FromSeconds(Math.Min(5, settings.WaitTimeout.TotalSeconds)));

            if (notification == null)
            {
                await subscription.RefreshAsync();
                notification = await RegressionTestHelpers.WaitAsync(
                    eventSource.Task,
                    settings.WaitTimeout,
                    $"Timed out waiting for an alarm event from '{alarmSourceNode.NodeId}'.");
            }

            Assert.NotEmpty(notification.EventId);
            Assert.Equal(triggerNode.NodeId, notification.SourceNodeId);
            Assert.True(notification.Active);
            Assert.True(
                (notification.Message ?? string.Empty)
                .IndexOf(settings.ExpectedEventMessageContains, StringComparison.OrdinalIgnoreCase) >= 0,
                $"Expected event message to contain '{settings.ExpectedEventMessageContains}', actual '{notification.Message}'.");
        }
        finally
        {
            await client.WriteNodeAsync(triggerNode, originalValue);
        }
    }
}
