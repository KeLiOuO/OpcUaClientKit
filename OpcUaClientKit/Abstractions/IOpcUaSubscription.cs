namespace OpcUaClientKit;

public interface IOpcUaSubscription : IAsyncDisposable
{
    string Name { get; }

    bool IsActive { get; }

    IReadOnlyList<string> NodeIds { get; }

    Task AddNodeAsync(
        string nodeId,
        Action<OpcUaValueChangeNotification> onChanged,
        CancellationToken ct = default);

    Task AddNodeAsync(
        OpcUaNode node,
        Action<OpcUaValueChangeNotification> onChanged,
        CancellationToken ct = default);

    Task AddNodeAsync(
        string nodeId,
        Action<OpcUaValueChangeNotification> onChanged,
        Action<OpcUaMonitoredItemOptions> configure,
        CancellationToken ct = default);

    Task AddNodeAsync(
        OpcUaNode node,
        Action<OpcUaValueChangeNotification> onChanged,
        Action<OpcUaMonitoredItemOptions> configure,
        CancellationToken ct = default);

    Task AddNodesAsync(
        IEnumerable<OpcUaSubscriptionNodeDefinition> nodes,
        CancellationToken ct = default);

    Task RemoveNodeAsync(string nodeId, CancellationToken ct = default);

    Task RemoveNodesAsync(IEnumerable<string> nodeIds, CancellationToken ct = default);

    Task UnsubscribeAsync(CancellationToken ct = default);
}
