using OpcUaClientKit;

namespace OpcUaClientKit.Demo;

internal sealed class ReconnectScenario : IDemoScenario
{
    public string Key => "reconnect";

    public string Title => "Automatic Reconnect";

    public string Description =>
        "Enable automatic reconnect, observe lifecycle notifications and verify subscriptions recover.";

    public async Task RunAsync(DemoContext context, CancellationToken ct)
    {
        DemoConsole.PrintSection("Automatic Reconnect");
        DemoConsole.PrintConnectionSummary(
            "Advanced builder with reconnect",
            context.Settings,
            $"{context.Settings.AdvancedDeviceId}-reconnect");

        await using var client = await DemoClientFactory
            .CreateReconnectConnectedClientAsync(
                context.Factory,
                context.Settings,
                reconnectHandler: PrintReconnectEvent,
                ct: ct)
            .ConfigureAwait(false);

        var levelNode = context.Settings.LevelNode.ToOpcUaNode();
        var alarmSourceNode = context.Settings.AlarmSourceNode.ToOpcUaNode();

        var dataNotificationCount = 0;
        var eventNotificationCount = 0;

        var subscribable = client.AsSubscribable();
        await using var dataSubscription = await subscribable
            .CreateSubscriptionBuilder()
            .WithName("demo-reconnect-data")
            .WithPublishingInterval(250)
            .BuildAsync(ct)
            .ConfigureAwait(false);

        await dataSubscription
            .AddNodeAsync(
                levelNode,
                notification =>
                {
                    var current = Interlocked.Increment(ref dataNotificationCount);
                    Console.WriteLine(
                        $"[DATA {current}] {notification.DisplayName ?? notification.NodeId} = " +
                        $"{DemoConsole.FormatValue(notification.Value)} | Good={notification.IsGood}");
                },
                ct)
            .ConfigureAwait(false);

        var eventClient = client.AsEventSubscribable();
        await using var eventSubscription = await eventClient
            .CreateEventSubscriptionBuilder()
            .WithName("demo-reconnect-events")
            .WithPublishingInterval(500)
            .WithConditionRefreshOnStart(context.Settings.ConditionRefreshOnStart)
            .WithIgnoreSuppressedOrShelved(context.Settings.IgnoreSuppressedOrShelved)
            .WithSelectClauseMode(context.Settings.ParsedEventSelectClauseMode)
            .BuildAsync(
                notification =>
                {
                    var current = Interlocked.Increment(ref eventNotificationCount);
                    Console.WriteLine($"[EVENT {current}]");
                    DemoConsole.PrintEvent(notification, context.Settings.EventFieldPreviewCount);
                },
                ct)
            .ConfigureAwait(false);

        await eventSubscription.AddSourceAsync(alarmSourceNode, ct).ConfigureAwait(false);

        Console.WriteLine("Reconnect demo is running.");
        Console.WriteLine($"Data subscription node: {levelNode.NodeId}");
        Console.WriteLine($"Event source node: {alarmSourceNode.NodeId}");
        Console.WriteLine(
            $"Reconnect policy: MaxAttempts={context.Settings.ReconnectMaxAttempts}, " +
            $"ImmediateFirstAttempt={context.Settings.ReconnectImmediatelyOnFirstFailure}, " +
            $"InitialDelayMs={context.Settings.ReconnectInitialDelayMs}, " +
            $"MaxDelayMs={context.Settings.ReconnectMaxDelayMs}, " +
            $"BackoffMultiplier={context.Settings.ReconnectBackoffMultiplier}");
        Console.WriteLine();
        Console.WriteLine("Suggested flow:");
        Console.WriteLine("1. Stop or disconnect the OPC UA server unexpectedly.");
        Console.WriteLine("2. Watch for Disconnected / Reconnecting / AttemptFailed notifications.");
        Console.WriteLine("3. Start the server again and wait for Reconnected.");
        Console.WriteLine("4. After reconnection, write a new value or trigger an alarm to verify subscriptions resumed.");
        Console.WriteLine();
        Console.WriteLine($"Press W to write {context.Settings.SubscriptionWriteValue} to {levelNode.NodeId}.");
        Console.WriteLine($"Press A to write active alarm value {context.Settings.EventActiveValue}.");
        Console.WriteLine($"Press C to write clear alarm value {context.Settings.EventClearValue}.");
        Console.WriteLine("Press R to call ConditionRefresh on the event subscription.");
        Console.WriteLine("Press Enter to stop the scenario.");

        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                break;
            }

            if (key.Key == ConsoleKey.W)
            {
                await client
                    .WriteNodeAsync(levelNode, context.Settings.SubscriptionWriteValue, ct)
                    .ConfigureAwait(false);
                Console.WriteLine(
                    $"Manual write sent: {context.Settings.SubscriptionWriteValue} -> {levelNode.NodeId}");
                continue;
            }

            if (key.Key == ConsoleKey.A)
            {
                await client
                    .WriteNodeAsync(levelNode, context.Settings.EventActiveValue, ct)
                    .ConfigureAwait(false);
                Console.WriteLine(
                    $"Manual write sent: {context.Settings.EventActiveValue} -> {levelNode.NodeId}");
                continue;
            }

            if (key.Key == ConsoleKey.C)
            {
                await client
                    .WriteNodeAsync(levelNode, context.Settings.EventClearValue, ct)
                    .ConfigureAwait(false);
                Console.WriteLine(
                    $"Manual write sent: {context.Settings.EventClearValue} -> {levelNode.NodeId}");
                continue;
            }

            if (key.Key == ConsoleKey.R)
            {
                await eventSubscription.RefreshAsync(ct).ConfigureAwait(false);
                Console.WriteLine("ConditionRefreshAsync sent.");
            }
        }
    }

    private static void PrintReconnectEvent(OpcUaReconnectEvent reconnectEvent)
    {
        Console.WriteLine(
            $"[RECONNECT] Kind={reconnectEvent.Kind}, Attempt={reconnectEvent.AttemptNumber}, " +
            $"NextDelay={reconnectEvent.NextRetryDelay}");

        if (reconnectEvent.Exception != null)
        {
            Console.WriteLine($"  Reason: {reconnectEvent.Exception.Message}");
        }
    }
}
