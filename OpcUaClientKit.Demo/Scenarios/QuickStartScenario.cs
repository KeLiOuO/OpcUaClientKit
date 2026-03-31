using OpcUaClientKit;

namespace OpcUaClientKit.Demo;

internal sealed class QuickStartScenario : IDemoScenario
{
    public string Key => "quickstart";

    public string Title => "Quick Start";

    public string Description => "Connect with the simple factory overload and read a single node.";

    public async Task RunAsync(DemoContext context, CancellationToken ct)
    {
        DemoConsole.PrintSection("Quick Start");
        DemoConsole.PrintConnectionSummary("Simple factory", context.Settings, context.Settings.SimpleDeviceId);

        await using var client = await DemoClientFactory
            .CreateSimpleConnectedClientAsync(context.Factory, context.Settings, ct)
            .ConfigureAwait(false);

        var levelValue = await client
            .ReadNodeAsync<double>(context.Settings.LevelNode.ToOpcUaNode(), ct)
            .ConfigureAwait(false);

        Console.WriteLine(
            $"Read {context.Settings.LevelNode.DisplayName} ({context.Settings.LevelNode.NodeId}) = {levelValue}");
    }
}
