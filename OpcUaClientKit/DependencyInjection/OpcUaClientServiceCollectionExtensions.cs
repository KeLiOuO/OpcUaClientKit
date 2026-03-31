using Microsoft.Extensions.DependencyInjection;

namespace OpcUaClientKit;

/// <summary>
/// Adds <c>OpcUaClientKit</c> services to an <see cref="IServiceCollection"/>.
/// </summary>
public static class OpcUaClientServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IOpcUaClientFactory"/> as a singleton.
    /// </summary>
    public static IServiceCollection AddOpcUaClientFactory(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddSingleton<IOpcUaClientFactory, OpcUaClientFactory>();
        return services;
    }
}

