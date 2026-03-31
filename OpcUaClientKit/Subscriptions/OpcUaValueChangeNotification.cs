namespace OpcUaClientKit;

/// <summary>
/// Represents a normalized data-change notification returned by <c>OpcUaClientKit</c>.
/// </summary>
public sealed class OpcUaValueChangeNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpcUaValueChangeNotification"/> class.
    /// </summary>
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

    /// <summary>
    /// Gets the raw OPC UA <c>NodeId</c> string for the monitored item.
    /// </summary>
    public string NodeId { get; }

    /// <summary>
    /// Gets the optional application-friendly display name.
    /// </summary>
    public string? DisplayName { get; }

    /// <summary>
    /// Gets the value carried by the notification.
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Gets the OPC UA status code as an unsigned integer.
    /// </summary>
    public uint StatusCode { get; }

    /// <summary>
    /// Gets a value indicating whether the notification status code is good.
    /// </summary>
    public bool IsGood { get; }

    /// <summary>
    /// Gets the source timestamp reported by the server.
    /// </summary>
    public DateTime SourceTimestamp { get; }

    /// <summary>
    /// Gets the server timestamp reported by the server.
    /// </summary>
    public DateTime ServerTimestamp { get; }
}
