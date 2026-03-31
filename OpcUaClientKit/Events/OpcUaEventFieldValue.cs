namespace OpcUaClientKit;

/// <summary>
/// Represents one field extracted from an OPC UA event notification.
/// </summary>
public sealed class OpcUaEventFieldValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpcUaEventFieldValue"/> class.
    /// </summary>
    public OpcUaEventFieldValue(string key, string displayName, object? value)
    {
        Key = string.IsNullOrWhiteSpace(key)
            ? throw new ArgumentException("Key is required.", nameof(key))
            : key.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName)
            ? throw new ArgumentException("DisplayName is required.", nameof(displayName))
            : displayName.Trim();
        Value = value;
    }

    /// <summary>
    /// Gets the stable field key used inside <see cref="OpcUaEventNotification.Fields"/>.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the display name generated for this field.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the field value.
    /// </summary>
    public object? Value { get; }
}
