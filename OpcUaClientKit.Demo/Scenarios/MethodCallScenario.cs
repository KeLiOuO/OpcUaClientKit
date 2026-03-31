using OpcUaClientKit;

namespace OpcUaClientKit.Demo;

internal sealed class MethodCallScenario : IDemoScenario
{
    public string Key => "method";

    public string Title => "Method Call";

    public string Description => "Connect with the advanced builder configuration and call an OPC UA method.";

    public async Task RunAsync(DemoContext context, CancellationToken ct)
    {
        DemoConsole.PrintSection("Method Call");
        DemoConsole.PrintConnectionSummary("Advanced builder", context.Settings, context.Settings.AdvancedDeviceId);

        await using var client = await DemoClientFactory
            .CreateAdvancedConnectedClientAsync(context.Factory, context.Settings, ct)
            .ConfigureAwait(false);

        var outputs = await client
            .CallMethodAsync(
                context.Settings.MethodObjectNode.ToOpcUaNode(),
                context.Settings.MethodNode.ToOpcUaNode(),
                context.Settings.CreateMethodArguments(),
                ct)
            .ConfigureAwait(false);

        Console.WriteLine(
            $"Called {context.Settings.MethodNode.DisplayName} on {context.Settings.MethodObjectNode.DisplayName}.");

        for (var index = 0; index < outputs.Count; index++)
        {
            Console.WriteLine($"Output[{index}] = {DemoConsole.FormatValue(outputs[index])}");
        }

        if (outputs.Count > 0 &&
            outputs[0] is double firstOutput)
        {
            var delta = Math.Abs(firstOutput - context.Settings.MethodExpectedFirstOutput);
            Console.WriteLine(
                $"First output delta to expected value: {delta} " +
                $"(expected {context.Settings.MethodExpectedFirstOutput}, tolerance {context.Settings.MethodExpectedTolerance})");
        }
    }
}
