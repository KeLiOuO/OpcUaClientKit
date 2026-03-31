namespace OpcUaClientKit;

/// <summary>
/// Extends <see cref="IOpcUaClient"/> with condition/alarm event subscription capabilities.
/// </summary>
public interface IEventSubscribableOpcUaClient : IOpcUaClient
{
    /// <summary>
    /// Creates an alarm event subscription, adds the specified event source and starts listening immediately.
    /// </summary>
    /// <param name="sourceNodeId">Object or view node that exposes the <c>EventNotifier</c> attribute.</param>
    /// <param name="onEvent">Callback invoked whenever a matching alarm event is received.</param>
    /// <param name="ct">Cancellation token used to cancel subscription creation.</param>
    Task<IOpcUaEventSubscription> SubscribeAlarmEventsAsync(
        string sourceNodeId,
        Action<OpcUaEventNotification> onEvent,
        CancellationToken ct = default);

    /// <summary>
    /// Creates an alarm event subscription, adds the specified event source and starts listening immediately.
    /// </summary>
    /// <param name="sourceNode">Object or view node that exposes the <c>EventNotifier</c> attribute.</param>
    /// <param name="onEvent">Callback invoked whenever a matching alarm event is received.</param>
    /// <param name="ct">Cancellation token used to cancel subscription creation.</param>
    Task<IOpcUaEventSubscription> SubscribeAlarmEventsAsync(
        OpcUaNode sourceNode,
        Action<OpcUaEventNotification> onEvent,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a builder used to configure an event subscription group before it is created on the server.
    /// </summary>
    OpcUaEventSubscriptionBuilder CreateEventSubscriptionBuilder();
}
