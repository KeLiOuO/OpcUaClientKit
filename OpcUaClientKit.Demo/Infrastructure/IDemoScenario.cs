namespace OpcUaClientKit.Demo;

internal interface IDemoScenario
{
    string Key { get; }

    string Title { get; }

    string Description { get; }

    Task RunAsync(DemoContext context, CancellationToken ct);
}
