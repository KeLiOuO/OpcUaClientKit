namespace OpcUaClientKit;

/// <summary>
/// Describes a single failed node operation within a batch read or write call.
/// </summary>
public sealed class OpcUaBatchOperationFailure
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpcUaBatchOperationFailure"/> class.
    /// </summary>
    public OpcUaBatchOperationFailure(
        string nodeId,
        uint statusCode,
        string? symbolicId,
        string message)
    {
        NodeId = string.IsNullOrWhiteSpace(nodeId)
            ? throw new ArgumentException("A node id is required.", nameof(nodeId))
            : nodeId.Trim();
        StatusCode = statusCode;
        SymbolicId = string.IsNullOrWhiteSpace(symbolicId)
            ? null
            : symbolicId.Trim();
        Message = string.IsNullOrWhiteSpace(message)
            ? throw new ArgumentException("A failure message is required.", nameof(message))
            : message.Trim();
    }

    /// <summary>
    /// Gets the failed node identifier.
    /// </summary>
    public string NodeId { get; }

    /// <summary>
    /// Gets the OPC UA status code returned by the server.
    /// </summary>
    public uint StatusCode { get; }

    /// <summary>
    /// Gets the symbolic name associated with <see cref="StatusCode"/>, when available.
    /// </summary>
    public string? SymbolicId { get; }

    /// <summary>
    /// Gets the human-readable failure message.
    /// </summary>
    public string Message { get; }
}
