namespace OpcUaClientKit;

public sealed class OpcUaValueChangeNotification
{
    public OpcUaValueChangeNotification(
        string nodeId,
        string? displayName,
        object? value,
        uint statusCode,
        bool isGood,
        DateTime sourceTimestamp,
        DateTime serverTimestamp)
    {
        NodeId = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
        DisplayName = displayName;
        Value = value;
        StatusCode = statusCode;
        IsGood = isGood;
        SourceTimestamp = sourceTimestamp;
        ServerTimestamp = serverTimestamp;
    }

    public string NodeId { get; }

    public string? DisplayName { get; }

    public object? Value { get; }

    public uint StatusCode { get; }

    public bool IsGood { get; }

    public DateTime SourceTimestamp { get; }

    public DateTime ServerTimestamp { get; }
}
