namespace OpcUaClientKit;

/// <summary>
/// Represents an OPC UA node together with an optional display name used by application code.
/// </summary>
public sealed class OpcUaNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpcUaNode"/> class.
    /// </summary>
    /// <param name="nodeId">Raw OPC UA <c>NodeId</c> string.</param>
    /// <param name="displayName">Optional application-friendly label for logging, UI and callbacks.</param>
    public OpcUaNode(string nodeId, string? displayName = null)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            throw new ArgumentException("NodeId is required.", nameof(nodeId));
        }

        NodeId = nodeId.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName)
            ? null
            : displayName.Trim();
    }

    /// <summary>
    /// Gets the raw OPC UA <c>NodeId</c> string.
    /// </summary>
    public string NodeId { get; }

    /// <summary>
    /// Gets the optional application-friendly display name.
    /// </summary>
    public string? DisplayName { get; }
}
