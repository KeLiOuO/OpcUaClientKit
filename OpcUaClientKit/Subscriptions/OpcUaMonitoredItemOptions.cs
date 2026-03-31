namespace OpcUaClientKit;

public sealed class OpcUaMonitoredItemOptions
{
    public string? DisplayName { get; set; }

    public double SamplingInterval { get; set; } = -1;

    public uint QueueSize { get; set; } = 1;

    public bool DiscardOldest { get; set; } = true;

    public OpcUaMonitoredItemOptions Clone()
    {
        return new OpcUaMonitoredItemOptions
        {
            DisplayName = DisplayName,
            SamplingInterval = SamplingInterval,
            QueueSize = QueueSize,
            DiscardOldest = DiscardOldest
        };
    }
}
