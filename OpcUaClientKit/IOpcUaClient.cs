namespace OpcUaClientKit;

public interface IOpcUaClient : IAsyncDisposable
{
    bool IsConnected { get; }

    Task ConnectAsync(CancellationToken ct = default);

    Task DisconnectAsync(CancellationToken ct = default);

    Task<object?> ReadNodeAsync(string nodeId, CancellationToken ct = default);

    Task<object?> ReadNodeAsync(OpcUaNode node, CancellationToken ct = default);

    Task<T?> ReadNodeAsync<T>(string nodeId, CancellationToken ct = default);

    Task<T?> ReadNodeAsync<T>(OpcUaNode node, CancellationToken ct = default);

    Task<IReadOnlyDictionary<string, object?>> ReadNodesAsync(
        IEnumerable<string> nodeIds,
        CancellationToken ct = default);

    Task<IReadOnlyDictionary<string, object?>> ReadNodesAsync(
        IEnumerable<OpcUaNode> nodes,
        CancellationToken ct = default);

    Task WriteNodeAsync(string nodeId, object? value, CancellationToken ct = default);

    Task WriteNodeAsync(OpcUaNode node, object? value, CancellationToken ct = default);

    Task WriteNodesAsync(
        Dictionary<string, object?> nodeValues,
        CancellationToken ct = default);

    Task WriteNodesAsync(
        IReadOnlyDictionary<string, object?> nodeValues,
        CancellationToken ct = default);

    Task WriteNodesAsync(
        IReadOnlyDictionary<OpcUaNode, object?> nodeValues,
        CancellationToken ct = default);

    Task<IReadOnlyList<object?>> CallMethodAsync(
        string objectNodeId,
        string methodNodeId,
        IEnumerable<object?>? inputArguments = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<object?>> CallMethodAsync(
        OpcUaNode objectNode,
        OpcUaNode methodNode,
        IEnumerable<object?>? inputArguments = null,
        CancellationToken ct = default);
}
