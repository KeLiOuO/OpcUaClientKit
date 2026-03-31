using OpcUaClientKit;

namespace OpcUaClientKit.Demo;

internal sealed class DemoContext
{
    public DemoContext(DemoSettings settings, IOpcUaClientFactory factory)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        Factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public DemoSettings Settings { get; }

    public IOpcUaClientFactory Factory { get; }
}
