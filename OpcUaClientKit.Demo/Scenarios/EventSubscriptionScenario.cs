using OpcUaClientKit;

namespace OpcUaClientKit.Demo;

internal sealed class EventSubscriptionScenario : IDemoScenario
{
    public string Key => "event-sub";

    public string Title => "Alarm Event Subscription";

    public string Description => "Subscribe to alarm events, refresh retained conditions and print selected event fields.";

    public async Task RunAsync(DemoContext context, CancellationToken ct)
    {
        DemoConsole.PrintSection("Alarm Event Subscription");
        DemoConsole.PrintConnectionSummary("Advanced builder", context.Settings, context.Settings.AdvancedDeviceId);

        await using var client = await DemoClientFactory
            .CreateAdvancedConnectedClientAsync(context.Factory, context.Settings, ct)
            .ConfigureAwait(false);

        var eventClient = client.AsEventSubscribable();
        await using var subscription = await eventClient
            .CreateEventSubscriptionBuilder()
            .WithName("demo-alarm-events")
            .WithPublishingInterval(500)
            .WithConditionRefreshOnStart(context.Settings.ConditionRefreshOnStart)
            .WithIgnoreSuppressedOrShelved(context.Settings.IgnoreSuppressedOrShelved)
            .WithSelectClauseMode(context.Settings.ParsedEventSelectClauseMode)
            .BuildAsync(
                notification => DemoConsole.PrintEvent(notification, context.Settings.EventFieldPreviewCount),
                ct)
            .ConfigureAwait(false);

        await subscription.AddSourceAsync(context.Settings.AlarmSourceNode.ToOpcUaNode(), ct).ConfigureAwait(false);

        Console.WriteLine("Event subscription started.");
        Console.WriteLine($"Source node: {context.Settings.AlarmSourceNode.NodeId}");
        Console.WriteLine($"SelectClause mode: {context.Settings.ParsedEventSelectClauseMode}");
        Console.WriteLine($"IgnoreSuppressedOrShelved: {context.Settings.IgnoreSuppressedOrShelved}");
        Console.WriteLine("Press A to trigger an alarm by writing the active value.");
        Console.WriteLine("Press C to clear the alarm by writing the clear value.");
        Console.WriteLine("Press R to call ConditionRefresh.");
        Console.WriteLine("Press Enter to stop the scenario.");

        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                break;
            }

            if (key.Key == ConsoleKey.A)
            {
                await client
                    .WriteNodeAsync(context.Settings.LevelNode.ToOpcUaNode(), context.Settings.EventActiveValue, ct)
                    .ConfigureAwait(false);
                Console.WriteLine($"Wrote active value {context.Settings.EventActiveValue} to {context.Settings.LevelNode.NodeId}.");
                continue;
            }

            if (key.Key == ConsoleKey.C)
            {
                await client
                    .WriteNodeAsync(context.Settings.LevelNode.ToOpcUaNode(), context.Settings.EventClearValue, ct)
                    .ConfigureAwait(false);
                Console.WriteLine($"Wrote clear value {context.Settings.EventClearValue} to {context.Settings.LevelNode.NodeId}.");
                continue;
            }

            if (key.Key == ConsoleKey.R)
            {
                await subscription.RefreshAsync(ct).ConfigureAwait(false);
                Console.WriteLine("ConditionRefreshAsync sent.");
            }
        }
    }
}
