namespace OpcUaClientKit;

public static class OpcUaClientSubscriptionExtensions
{
    public static ISubscribableOpcUaClient AsSubscribable(this IOpcUaClient client)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        if (client is ISubscribableOpcUaClient subscribableClient)
        {
            return subscribableClient;
        }

        throw new InvalidOperationException(
            "The provided IOpcUaClient does not support subscriptions. Ensure it was created by OpcUaClientKit.");
    }
}
