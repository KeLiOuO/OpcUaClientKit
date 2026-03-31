namespace OpcUaClientKit;

public sealed class OpcUaSubscriptionNodeDefinition
{
    public OpcUaSubscriptionNodeDefinition(
        string nodeId,
        Action<OpcUaValueChangeNotification> onChanged)
        : this(new OpcUaNode(nodeId), onChanged)
    {
    }

    public OpcUaSubscriptionNodeDefinition(
        OpcUaNode node,
        Action<OpcUaValueChangeNotification> onChanged)
    {
        Node = node ?? throw new ArgumentNullException(nameof(node));
        OnChanged = onChanged ?? throw new ArgumentNullException(nameof(onChanged));
    }

    public OpcUaNode Node { get; }

    public string NodeId => Node.NodeId;

    public Action<OpcUaValueChangeNotification> OnChanged { get; }

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
