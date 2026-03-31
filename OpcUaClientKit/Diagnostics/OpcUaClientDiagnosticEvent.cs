namespace OpcUaClientKit;

/// <summary>
/// Represents a non-fatal diagnostic event emitted by the OPC UA client.
/// </summary>
public sealed class OpcUaClientDiagnosticEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpcUaClientDiagnosticEvent"/> class.
    /// </summary>
    public OpcUaClientDiagnosticEvent(
        OpcUaClientDiagnosticKind kind,
        string message,
        Exception? exception = null,
        string? subscriptionName = null,
        string? itemId = null)
    {
        Kind = kind;
        Message = string.IsNullOrWhiteSpace(message)
            ? throw new ArgumentException("A diagnostic message is required.", nameof(message))
            : message.Trim();
        Exception = exception;
        SubscriptionName = string.IsNullOrWhiteSpace(subscriptionName)
            ? null
            : subscriptionName.Trim();
        ItemId = string.IsNullOrWhiteSpace(itemId)
            ? null
            : itemId.Trim();
    }

    /// <summary>
    /// Gets the diagnostic category.
    /// </summary>
    public OpcUaClientDiagnosticKind Kind { get; }

    /// <summary>
    /// Gets a human-readable description of the diagnostic event.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the exception that triggered the diagnostic event, if any.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets the affected subscription name when the diagnostic relates to a subscription.
    /// </summary>
    public string? SubscriptionName { get; }

    /// <summary>
    /// Gets the affected node or source identifier when available.
    /// </summary>
    public string? ItemId { get; }
}
