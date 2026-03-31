namespace OpcUaClientKit;

/// <summary>
/// Identifies the kind of diagnostic event reported by <see cref="OpcUaClientOptions.DiagnosticsHandler"/>.
/// </summary>
public enum OpcUaClientDiagnosticKind
{
    /// <summary>
    /// A user data-subscription callback threw an exception.
    /// </summary>
    SubscriptionCallbackException,

    /// <summary>
    /// A user event-subscription callback threw an exception.
    /// </summary>
    EventSubscriptionCallbackException,

    /// <summary>
    /// The client had to fall back from a server-side event filter to client-side filtering.
    /// </summary>
    EventFilterFallbackWarning
}
