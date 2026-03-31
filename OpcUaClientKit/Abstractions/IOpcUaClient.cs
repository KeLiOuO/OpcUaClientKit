namespace OpcUaClientKit;

/// <summary>
/// Defines the core OPC UA client operations exposed by <c>OpcUaClientKit</c>.
/// </summary>
/// <remarks>
/// This interface intentionally keeps the surface area small and focuses on the
/// most common client capabilities: connect, disconnect, read, write and method
/// invocation. Subscription capabilities are exposed through specialized
/// interfaces obtained via extension methods.
/// </remarks>
public interface IOpcUaClient : IAsyncDisposable
{
    /// <summary>
    /// Gets a value indicating whether the underlying OPC UA session is currently connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Establishes a session with the configured OPC UA server.
    /// </summary>
    /// <param name="ct">Cancellation token used to cancel the connection attempt.</param>
    Task ConnectAsync(CancellationToken ct = default);

    /// <summary>
    /// Closes the current session and releases all active subscriptions created by this client.
    /// Any previously obtained subscription handles become inactive and must be recreated after a later reconnect.
    /// </summary>
    /// <param name="ct">Cancellation token used to cancel the disconnect operation.</param>
    Task DisconnectAsync(CancellationToken ct = default);

    /// <summary>
    /// Reads the value of a single node by its raw OPC UA <c>NodeId</c> string.
    /// </summary>
    /// <param name="nodeId">Node to read, for example <c>ns=3;s=/Plc/DB66.DBW0</c>.</param>
    /// <param name="ct">Cancellation token used to cancel the read operation.</param>
    Task<object?> ReadNodeAsync(string nodeId, CancellationToken ct = default);

    /// <summary>
    /// Reads the value of a single node represented by an <see cref="OpcUaNode"/>.
    /// </summary>
    /// <param name="node">Node to read.</param>
    /// <param name="ct">Cancellation token used to cancel the read operation.</param>
    Task<object?> ReadNodeAsync(OpcUaNode node, CancellationToken ct = default);

    /// <summary>
    /// Reads a single node and converts the result to the requested type.
    /// </summary>
    /// <typeparam name="T">Target type expected by the caller.</typeparam>
    /// <param name="nodeId">Node to read.</param>
    /// <param name="ct">Cancellation token used to cancel the read operation.</param>
    Task<T?> ReadNodeAsync<T>(string nodeId, CancellationToken ct = default);

    /// <summary>
    /// Reads a single node and converts the result to the requested type.
    /// </summary>
    /// <typeparam name="T">Target type expected by the caller.</typeparam>
    /// <param name="node">Node to read.</param>
    /// <param name="ct">Cancellation token used to cancel the read operation.</param>
    Task<T?> ReadNodeAsync<T>(OpcUaNode node, CancellationToken ct = default);

    /// <summary>
    /// Reads multiple nodes in a single round trip using raw OPC UA <c>NodeId</c> strings.
    /// If the server returns a partial failure, the method throws <see cref="OpcUaBatchReadException"/>
    /// and exposes the successful values through the exception.
    /// </summary>
    /// <param name="nodeIds">Nodes to read.</param>
    /// <param name="ct">Cancellation token used to cancel the read operation.</param>
    Task<IReadOnlyDictionary<string, object?>> ReadNodesAsync(
        IEnumerable<string> nodeIds,
        CancellationToken ct = default);

    /// <summary>
    /// Reads multiple nodes in a single round trip using <see cref="OpcUaNode"/> instances.
    /// If the server returns a partial failure, the method throws <see cref="OpcUaBatchReadException"/>
    /// and exposes the successful values through the exception.
    /// </summary>
    /// <param name="nodes">Nodes to read.</param>
    /// <param name="ct">Cancellation token used to cancel the read operation.</param>
    Task<IReadOnlyDictionary<string, object?>> ReadNodesAsync(
        IEnumerable<OpcUaNode> nodes,
        CancellationToken ct = default);

    /// <summary>
    /// Writes a value to a single node addressed by its raw OPC UA <c>NodeId</c> string.
    /// </summary>
    /// <param name="nodeId">Node to write.</param>
    /// <param name="value">Value to write to the node.</param>
    /// <param name="ct">Cancellation token used to cancel the write operation.</param>
    Task WriteNodeAsync(string nodeId, object? value, CancellationToken ct = default);

    /// <summary>
    /// Writes a value to a single node represented by an <see cref="OpcUaNode"/>.
    /// </summary>
    /// <param name="node">Node to write.</param>
    /// <param name="value">Value to write to the node.</param>
    /// <param name="ct">Cancellation token used to cancel the write operation.</param>
    Task WriteNodeAsync(OpcUaNode node, object? value, CancellationToken ct = default);

    /// <summary>
    /// Writes multiple values in a batch using raw OPC UA <c>NodeId</c> strings as keys.
    /// If the server returns a partial failure, the method throws <see cref="OpcUaBatchWriteException"/>
    /// and exposes both failed and successful nodes through the exception.
    /// </summary>
    /// <param name="nodeValues">Node/value pairs to write.</param>
    /// <param name="ct">Cancellation token used to cancel the write operation.</param>
    Task WriteNodesAsync(
        Dictionary<string, object?> nodeValues,
        CancellationToken ct = default);

    /// <summary>
    /// Writes multiple values in a batch using raw OPC UA <c>NodeId</c> strings as keys.
    /// If the server returns a partial failure, the method throws <see cref="OpcUaBatchWriteException"/>
    /// and exposes both failed and successful nodes through the exception.
    /// </summary>
    /// <param name="nodeValues">Node/value pairs to write.</param>
    /// <param name="ct">Cancellation token used to cancel the write operation.</param>
    Task WriteNodesAsync(
        IReadOnlyDictionary<string, object?> nodeValues,
        CancellationToken ct = default);

    /// <summary>
    /// Writes multiple values in a batch using <see cref="OpcUaNode"/> instances as keys.
    /// If the server returns a partial failure, the method throws <see cref="OpcUaBatchWriteException"/>
    /// and exposes both failed and successful nodes through the exception.
    /// </summary>
    /// <param name="nodeValues">Node/value pairs to write.</param>
    /// <param name="ct">Cancellation token used to cancel the write operation.</param>
    Task WriteNodesAsync(
        IReadOnlyDictionary<OpcUaNode, object?> nodeValues,
        CancellationToken ct = default);

    /// <summary>
    /// Calls an OPC UA method using raw object and method <c>NodeId</c> strings.
    /// </summary>
    /// <param name="objectNodeId">NodeId of the object that owns the method.</param>
    /// <param name="methodNodeId">NodeId of the method to call.</param>
    /// <param name="inputArguments">Optional input arguments passed to the method.</param>
    /// <param name="ct">Cancellation token used to cancel the method invocation.</param>
    Task<IReadOnlyList<object?>> CallMethodAsync(
        string objectNodeId,
        string methodNodeId,
        IEnumerable<object?>? inputArguments = null,
        CancellationToken ct = default);

    /// <summary>
    /// Calls an OPC UA method using <see cref="OpcUaNode"/> objects for the target object and method.
    /// </summary>
    /// <param name="objectNode">Node that owns the method.</param>
    /// <param name="methodNode">Method node to invoke.</param>
    /// <param name="inputArguments">Optional input arguments passed to the method.</param>
    /// <param name="ct">Cancellation token used to cancel the method invocation.</param>
    Task<IReadOnlyList<object?>> CallMethodAsync(
        OpcUaNode objectNode,
        OpcUaNode methodNode,
        IEnumerable<object?>? inputArguments = null,
        CancellationToken ct = default);
}
