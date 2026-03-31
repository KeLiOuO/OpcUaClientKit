namespace OpcUaClientKit;

/// <summary>
/// Represents a live data subscription group hosted on an OPC UA session.
/// </summary>
/// <remarks>
/// Subscription handles are bound to the session that created them. If the owning client disconnects
/// or reconnects, existing handles become inactive and must be recreated.
/// </remarks>
public interface IOpcUaSubscription : IAsyncDisposable
{
    /// <summary>
    /// Gets the friendly subscription name used for diagnostics and tracing.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a value indicating whether the subscription is still active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Gets the current snapshot of monitored node identifiers.
    /// </summary>
    IReadOnlyList<string> NodeIds { get; }

    /// <summary>
    /// Adds a single monitored item to an already created subscription group.
    /// </summary>
    Task AddNodeAsync(
        string nodeId,
        Action<OpcUaValueChangeNotification> onChanged,
        CancellationToken ct = default);

    /// <summary>
    /// Adds a single monitored item to an already created subscription group.
    /// </summary>
    Task AddNodeAsync(
        OpcUaNode node,
        Action<OpcUaValueChangeNotification> onChanged,
        CancellationToken ct = default);

    /// <summary>
    /// Adds a single monitored item and allows custom monitored-item options to be applied.
    /// </summary>
    Task AddNodeAsync(
        string nodeId,
        Action<OpcUaValueChangeNotification> onChanged,
        Action<OpcUaMonitoredItemOptions> configure,
        CancellationToken ct = default);

    /// <summary>
    /// Adds a single monitored item and allows custom monitored-item options to be applied.
    /// </summary>
    Task AddNodeAsync(
        OpcUaNode node,
        Action<OpcUaValueChangeNotification> onChanged,
        Action<OpcUaMonitoredItemOptions> configure,
        CancellationToken ct = default);

    /// <summary>
    /// Adds multiple monitored items to the current subscription group in a single operation.
    /// </summary>
    Task AddNodesAsync(
        IEnumerable<OpcUaSubscriptionNodeDefinition> nodes,
        CancellationToken ct = default);

    /// <summary>
    /// Removes a single monitored item from the current subscription group.
    /// </summary>
    Task RemoveNodeAsync(string nodeId, CancellationToken ct = default);

    /// <summary>
    /// Removes multiple monitored items from the current subscription group.
    /// </summary>
    Task RemoveNodesAsync(IEnumerable<string> nodeIds, CancellationToken ct = default);

    /// <summary>
    /// Deletes the subscription from the server and releases all monitored items.
    /// </summary>
    Task UnsubscribeAsync(CancellationToken ct = default);
}
