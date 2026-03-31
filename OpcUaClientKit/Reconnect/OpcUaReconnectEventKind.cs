namespace OpcUaClientKit;

/// <summary>
/// Identifies a stage in the automatic reconnect lifecycle.
/// </summary>
public enum OpcUaReconnectEventKind
{
    /// <summary>
    /// The current session became unavailable and automatic reconnect is about to begin.
    /// </summary>
    Disconnected,

    /// <summary>
    /// A reconnect attempt is starting.
    /// </summary>
    Reconnecting,

    /// <summary>
    /// A reconnect attempt failed and another retry will follow after a delay.
    /// </summary>
    AttemptFailed,

    /// <summary>
    /// The client re-established the session and restored all subscriptions successfully.
    /// </summary>
    Reconnected,

    /// <summary>
    /// Automatic reconnect exhausted the configured retry budget and stopped retrying.
    /// </summary>
    GaveUp
}
