using OpcUaClientKit;

namespace OpcUaClientKit.Demo;

internal sealed class ReadWriteScenario : IDemoScenario
{
    public string Key => "readwrite";

    public string Title => "Read / Write";

    public string Description => "Read a value, write one node, write multiple nodes and read them back.";

    public async Task RunAsync(DemoContext context, CancellationToken ct)
    {
        DemoConsole.PrintSection("Read / Write");
        DemoConsole.PrintConnectionSummary("Simple factory", context.Settings, context.Settings.SimpleDeviceId);

        await using var client = await DemoClientFactory
            .CreateSimpleConnectedClientAsync(context.Factory, context.Settings, ct)
            .ConfigureAwait(false);

        var levelNode = context.Settings.LevelNode.ToOpcUaNode();
        var writableNode1 = context.Settings.WritableNode1.ToOpcUaNode();
        var writableNode2 = context.Settings.WritableNode2.ToOpcUaNode();

        var levelValue = await client.ReadNodeAsync(levelNode, ct).ConfigureAwait(false);
        Console.WriteLine($"Read {levelNode.DisplayName}: {DemoConsole.FormatValue(levelValue)}");

        await client.WriteNodeAsync(writableNode1, context.Settings.SingleWriteValue, ct).ConfigureAwait(false);
        Console.WriteLine(
            $"Wrote {writableNode1.DisplayName} = {context.Settings.SingleWriteValue} using single-node write.");

        await client.WriteNodesAsync(
                new Dictionary<OpcUaNode, object?>
                {
                    [writableNode1] = context.Settings.BatchWriteValue1,
                    [writableNode2] = context.Settings.BatchWriteValue2
                },
                ct)
            .ConfigureAwait(false);

        Console.WriteLine("Executed batch write.");

        var values = await client
            .ReadNodesAsync(new[] { writableNode1, writableNode2 }, ct)
            .ConfigureAwait(false);

        foreach (var pair in values)
        {
            Console.WriteLine($"Read-back {pair.Key} = {DemoConsole.FormatValue(pair.Value)}");
        }
    }
}
