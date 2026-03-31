using System.Threading.Tasks;
using OpcUaClientKit;

namespace OpcUaClientKit.Demo;

internal sealed class DataSubscriptionScenario : IDemoScenario
{
    public string Key => "data-sub";

    public string Title => "Data Subscription";

    public string Description => "Create a subscription group, add nodes dynamically and print value changes.";

    public async Task RunAsync(DemoContext context, CancellationToken ct)
    {
        DemoConsole.PrintSection("Data Subscription");
        DemoConsole.PrintConnectionSummary("Simple factory", context.Settings, context.Settings.SimpleDeviceId);

        await using var client = await DemoClientFactory
            .CreateSimpleConnectedClientAsync(context.Factory, context.Settings, ct)
            .ConfigureAwait(false);

        var firstNotification = new TaskCompletionSource<OpcUaValueChangeNotification>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var levelNode = context.Settings.LevelNode.ToOpcUaNode();
        var writableNode1 = context.Settings.WritableNode1.ToOpcUaNode();
        var writableNode2 = context.Settings.WritableNode2.ToOpcUaNode();

        var subscribable = client.AsSubscribable();
        await using var subscription = await subscribable
            .CreateSubscriptionBuilder()
            .WithName("demo-data-subscription")
            .WithPublishingInterval(250)
            .BuildAsync(ct)
            .ConfigureAwait(false);

        await subscription.AddNodesAsync(
                new[]
                {
                    new OpcUaSubscriptionNodeDefinition(
                        levelNode,
                        notification => OnDataChanged(firstNotification, notification)),
                    new OpcUaSubscriptionNodeDefinition(
                        writableNode1,
                        notification => OnDataChanged(firstNotification, notification)),
                    new OpcUaSubscriptionNodeDefinition(
                        writableNode2,
                        notification => OnDataChanged(firstNotification, notification))
                },
                ct)
            .ConfigureAwait(false);

        Console.WriteLine("Subscription created. Writing one node to trigger a notification...");

        await client
            .WriteNodeAsync(writableNode1, context.Settings.SubscriptionWriteValue, ct)
            .ConfigureAwait(false);

        await WaitForNotificationAsync(firstNotification.Task, context.Settings.WaitTimeout).ConfigureAwait(false);

        Console.WriteLine("You can now modify subscribed nodes in the server. Press Enter to stop the scenario.");
        Console.ReadLine();
    }

    private static async Task WaitForNotificationAsync(
        Task<OpcUaValueChangeNotification> task,
        TimeSpan timeout)
    {
        var completed = await Task.WhenAny(task, Task.Delay(timeout)).ConfigureAwait(false);
        if (completed != task)
        {
            Console.WriteLine("No notification was received within the configured wait timeout.");
            return;
        }

        var notification = await task.ConfigureAwait(false);
        Console.WriteLine(
            $"First notification: {notification.DisplayName ?? notification.NodeId} = " +
            $"{DemoConsole.FormatValue(notification.Value)}");
    }

    private static void OnDataChanged(
        TaskCompletionSource<OpcUaValueChangeNotification> firstNotification,
        OpcUaValueChangeNotification notification)
    {
        Console.WriteLine(
            $"[{DateTime.Now:HH:mm:ss}] {notification.DisplayName ?? notification.NodeId} = " +
            $"{DemoConsole.FormatValue(notification.Value)} | Good={notification.IsGood}");

        firstNotification.TrySetResult(notification);
    }
}
