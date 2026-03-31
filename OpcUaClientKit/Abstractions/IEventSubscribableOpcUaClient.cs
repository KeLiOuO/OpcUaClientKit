namespace OpcUaClientKit;

public interface IEventSubscribableOpcUaClient : IOpcUaClient
{
    Task<IOpcUaEventSubscription> SubscribeAlarmEventsAsync(
        string sourceNodeId,
        Action<OpcUaEventNotification> onEvent,
        CancellationToken ct = default);

    Task<IOpcUaEventSubscription> SubscribeAlarmEventsAsync(
        OpcUaNode sourceNode,
        Action<OpcUaEventNotification> onEvent,
        CancellationToken ct = default);

    OpcUaEventSubscriptionBuilder CreateEventSubscriptionBuilder();
}
