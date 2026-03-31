namespace OpcUaClientKit;

/// <summary>
/// Represents a live alarm/condition event subscription group hosted on an OPC UA session.
/// </summary>
public interface IOpcUaEventSubscription : IAsyncDisposable
{
    /// <summary>
    /// Gets the friendly subscription name used for diagnostics and tracing.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a value indicating whether the event subscription is still active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Gets the current snapshot of source nodes monitored by this event subscription.
    /// </summary>
    IReadOnlyList<string> SourceNodeIds { get; }

    /// <summary>
    /// Adds a single alarm event source to the subscription.
    /// </summary>
    Task AddSourceAsync(string sourceNodeId, CancellationToken ct = default);

    /// <summary>
    /// Adds a single alarm event source to the subscription.
    /// </summary>
    Task AddSourceAsync(OpcUaNode sourceNode, CancellationToken ct = default);

    /// <summary>
    /// Adds multiple alarm event sources to the subscription in a single operation.
    /// </summary>
    Task AddSourcesAsync(IEnumerable<string> sourceNodeIds, CancellationToken ct = default);

    /// <summary>
    /// Adds multiple alarm event sources to the subscription in a single operation.
    /// </summary>
    Task AddSourcesAsync(IEnumerable<OpcUaNode> sourceNodes, CancellationToken ct = default);

    /// <summary>
    /// Removes a single event source from the subscription.
    /// </summary>
    Task RemoveSourceAsync(string sourceNodeId, CancellationToken ct = default);

    /// <summary>
    /// Removes multiple event sources from the subscription.
    /// </summary>
    Task RemoveSourcesAsync(IEnumerable<string> sourceNodeIds, CancellationToken ct = default);

    /// <summary>
    /// Requests a <c>ConditionRefresh</c> for the current event subscription.
    /// </summary>
    Task RefreshAsync(CancellationToken ct = default);

    /// <summary>
    /// Deletes the event subscription from the server and releases all monitored event sources.
    /// </summary>
    Task UnsubscribeAsync(CancellationToken ct = default);
}
