namespace OpcUaClientKit;

public interface IOpcUaEventSubscription : IAsyncDisposable
{
    string Name { get; }

    bool IsActive { get; }

    IReadOnlyList<string> SourceNodeIds { get; }

    Task AddSourceAsync(string sourceNodeId, CancellationToken ct = default);

    Task AddSourceAsync(OpcUaNode sourceNode, CancellationToken ct = default);

    Task AddSourcesAsync(IEnumerable<string> sourceNodeIds, CancellationToken ct = default);

    Task AddSourcesAsync(IEnumerable<OpcUaNode> sourceNodes, CancellationToken ct = default);

    Task RemoveSourceAsync(string sourceNodeId, CancellationToken ct = default);

    Task RemoveSourcesAsync(IEnumerable<string> sourceNodeIds, CancellationToken ct = default);

    Task RefreshAsync(CancellationToken ct = default);

    Task UnsubscribeAsync(CancellationToken ct = default);
}
