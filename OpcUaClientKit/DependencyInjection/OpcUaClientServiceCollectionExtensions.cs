using Microsoft.Extensions.DependencyInjection;

namespace OpcUaClientKit;

public static class OpcUaClientServiceCollectionExtensions
{
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

