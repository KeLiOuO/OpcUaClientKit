namespace OpcUaClientKit;

/// <summary>
/// Provides helper methods for obtaining data-subscription capabilities from <see cref="IOpcUaClient"/>.
/// </summary>
public static class OpcUaClientSubscriptionExtensions
{
    /// <summary>
    /// Casts an <see cref="IOpcUaClient"/> to <see cref="ISubscribableOpcUaClient"/> when the client was created by this library.
    /// </summary>
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
