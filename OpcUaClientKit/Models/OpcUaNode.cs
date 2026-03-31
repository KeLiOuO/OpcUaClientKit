namespace OpcUaClientKit;

public sealed class OpcUaNode
{
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

    public string NodeId { get; }

    public string? DisplayName { get; }
}
