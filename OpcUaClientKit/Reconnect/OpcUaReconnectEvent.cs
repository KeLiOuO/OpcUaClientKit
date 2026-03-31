namespace OpcUaClientKit;

/// <summary>
/// Describes an automatic reconnect lifecycle notification emitted by the OPC UA client.
/// </summary>
public sealed class OpcUaReconnectEvent
{
    /// <summary>
    /// Creates a reconnect lifecycle event.
    /// </summary>
    public OpcUaReconnectEvent(
        OpcUaReconnectEventKind kind,
        int attemptNumber,
        Exception? exception = null,
        TimeSpan nextRetryDelay = default)
    {
        Kind = kind;
        AttemptNumber = attemptNumber;
        Exception = exception;
        NextRetryDelay = nextRetryDelay;
    }

    /// <summary>
    /// Gets the reconnect lifecycle stage represented by the event.
    /// </summary>
    public OpcUaReconnectEventKind Kind { get; }

    /// <summary>
    /// Gets the 1-based reconnect attempt number. Zero is used for disconnected, reconnected and gave-up notifications.
    /// </summary>
    public int AttemptNumber { get; }

    /// <summary>
    /// Gets the exception that caused the current attempt to fail, if any.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets the delay that will be waited before the next retry attempt.
    /// </summary>
    public TimeSpan NextRetryDelay { get; }
}
