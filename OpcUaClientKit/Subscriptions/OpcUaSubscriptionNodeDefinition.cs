namespace OpcUaClientKit;

/// <summary>
/// Describes a monitored node to be added to a data subscription.
/// </summary>
public sealed class OpcUaSubscriptionNodeDefinition
{
    /// <summary>
    /// Initializes a new node definition from a raw OPC UA <c>NodeId</c> string.
    /// </summary>
    public OpcUaSubscriptionNodeDefinition(
        string nodeId,
        Action<OpcUaValueChangeNotification> onChanged)
        : this(new OpcUaNode(nodeId), onChanged)
    {
    }

    /// <summary>
    /// Initializes a new node definition from an <see cref="OpcUaNode"/>.
    /// </summary>
    public OpcUaSubscriptionNodeDefinition(
        OpcUaNode node,
        Action<OpcUaValueChangeNotification> onChanged)
    {
        Node = node ?? throw new ArgumentNullException(nameof(node));
        OnChanged = onChanged ?? throw new ArgumentNullException(nameof(onChanged));
    }

    /// <summary>
    /// Gets the node that should be monitored.
    /// </summary>
    public OpcUaNode Node { get; }

    /// <summary>
    /// Gets the raw OPC UA <c>NodeId</c> string.
    /// </summary>
    public string NodeId => Node.NodeId;

    /// <summary>
    /// Gets the callback invoked whenever the monitored item receives a data change notification.
    /// </summary>
    public Action<OpcUaValueChangeNotification> OnChanged { get; }

    /// <summary>
    /// Gets the optional monitored-item settings applied when the node is added to the subscription.
    /// </summary>
    public OpcUaMonitoredItemOptions Options { get; init; } = new();
}

internal sealed class OpcUaSubscriptionItemDefinition
{
    public OpcUaSubscriptionItemDefinition(
        OpcUaNode node,
        Action<OpcUaValueChangeNotification> onChanged,
        OpcUaMonitoredItemOptions options)
    {
        Node = node ?? throw new ArgumentNullException(nameof(node));
        OnChanged = onChanged ?? throw new ArgumentNullException(nameof(onChanged));
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public OpcUaNode Node { get; }

    public string NodeId => Node.NodeId;

    public Action<OpcUaValueChangeNotification> OnChanged { get; }

    public OpcUaMonitoredItemOptions Options { get; }

    public static OpcUaSubscriptionItemDefinition FromPublicDefinition(
        OpcUaSubscriptionNodeDefinition node)
    {
        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        return new OpcUaSubscriptionItemDefinition(
            node.Node,
            node.OnChanged,
            node.Options?.Clone() ?? new OpcUaMonitoredItemOptions());
    }

    public OpcUaSubscriptionItemDefinition Clone()
    {
        return new OpcUaSubscriptionItemDefinition(
            new OpcUaNode(Node.NodeId, Node.DisplayName),
            OnChanged,
            Options.Clone());
    }
}
