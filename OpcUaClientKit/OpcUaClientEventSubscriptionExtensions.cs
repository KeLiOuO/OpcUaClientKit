namespace OpcUaClientKit;

public static class OpcUaClientEventSubscriptionExtensions
{
    public static IEventSubscribableOpcUaClient AsEventSubscribable(this IOpcUaClient client)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        if (client is IEventSubscribableOpcUaClient eventSubscribableClient)
        {
            return eventSubscribableClient;
        }

        throw new InvalidOperationException(
            "The provided IOpcUaClient does not support event subscriptions. Ensure it was created by OpcUaClientKit.");
    }
}
