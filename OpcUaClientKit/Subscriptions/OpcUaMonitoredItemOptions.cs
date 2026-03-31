namespace OpcUaClientKit;

/// <summary>
/// Represents optional per-node settings applied when a monitored item is created.
/// </summary>
public sealed class OpcUaMonitoredItemOptions
{
    /// <summary>
    /// Gets or sets an optional display name used in callbacks and diagnostics.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the requested sampling interval in milliseconds.
    /// Use <c>-1</c> to let the server choose the default sampling interval.
    /// </summary>
    public double SamplingInterval { get; set; } = -1;

    /// <summary>
    /// Gets or sets the queue size for the monitored item.
    /// </summary>
    public uint QueueSize { get; set; } = 1;

    /// <summary>
    /// Gets or sets a value indicating whether the oldest item should be discarded when the queue is full.
    /// </summary>
    public bool DiscardOldest { get; set; } = true;

    /// <summary>
    /// Creates a copy of the current monitored item options object.
    /// </summary>
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
