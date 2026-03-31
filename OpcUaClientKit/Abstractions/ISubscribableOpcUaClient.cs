namespace OpcUaClientKit;

public interface ISubscribableOpcUaClient : IOpcUaClient
{
    Task<IOpcUaSubscription> SubscribeNodeAsync(
        string nodeId,
        Action<OpcUaValueChangeNotification> onChanged,
        CancellationToken ct = default);

    Task<IOpcUaSubscription> SubscribeNodeAsync(
        OpcUaNode node,
        Action<OpcUaValueChangeNotification> onChanged,
        CancellationToken ct = default);

    OpcUaSubscriptionBuilder CreateSubscriptionBuilder();
}
