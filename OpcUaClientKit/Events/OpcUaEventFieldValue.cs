namespace OpcUaClientKit;

public sealed class OpcUaEventFieldValue
{
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

    public string Key { get; }

    public string DisplayName { get; }

    public object? Value { get; }
}
