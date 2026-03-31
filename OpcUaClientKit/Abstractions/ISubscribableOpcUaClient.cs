namespace OpcUaClientKit;

/// <summary>
/// Extends <see cref="IOpcUaClient"/> with data-change subscription capabilities.
/// </summary>
public interface ISubscribableOpcUaClient : IOpcUaClient
{
    /// <summary>
    /// Creates a subscription group, adds the specified node and starts listening for data changes.
    /// </summary>
    /// <param name="nodeId">Node to monitor.</param>
    /// <param name="onChanged">Callback invoked whenever the node value changes.</param>
    /// <param name="ct">Cancellation token used to cancel subscription creation.</param>
    Task<IOpcUaSubscription> SubscribeNodeAsync(
        string nodeId,
        Action<OpcUaValueChangeNotification> onChanged,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a subscription group, adds the specified node and starts listening for data changes.
    /// </summary>
    /// <param name="node">Node to monitor.</param>
    /// <param name="onChanged">Callback invoked whenever the node value changes.</param>
    /// <param name="ct">Cancellation token used to cancel subscription creation.</param>
    Task<IOpcUaSubscription> SubscribeNodeAsync(
        OpcUaNode node,
        Action<OpcUaValueChangeNotification> onChanged,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a builder used to configure a data subscription group before it is created on the server.
    /// </summary>
    OpcUaSubscriptionBuilder CreateSubscriptionBuilder();
}
