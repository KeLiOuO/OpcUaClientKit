using OpcUaClientKit;

namespace OpcUaClientKit.RegressionTests.Infrastructure;

public sealed class OpcUaIntegrationFixture
{
    private readonly IOpcUaClientFactory _factory = new OpcUaClientFactory();

    public OpcUaIntegrationFixture()
    {
        Settings = OpcUaRegressionTestSettings.Load();
    }

    internal OpcUaRegressionTestSettings Settings { get; }

    public IOpcUaClient CreateClient()
    {
        return _factory.Create(
            Settings.ServerUrl,
            Settings.ApplicationName,
            Settings.DeviceId,
            Settings.UserName,
            Settings.Password,
            Settings.AutoAcceptUntrustedServerCertificate,
            Settings.UseSecurity,
            Settings.SessionTimeout);
    }

    public async Task<IOpcUaClient> CreateConnectedClientAsync(CancellationToken ct = default)
    {
        var client = CreateClient();
        await client.ConnectAsync(ct).ConfigureAwait(false);
        return client;
    }

    public IOpcUaClient CreateReconnectEnabledClient(Action<OpcUaReconnectOptions>? configure = null)
    {
        var builder = _factory
            .CreateBuilder()
            .WithServerUrl(Settings.ServerUrl)
            .WithApplicationName(Settings.ApplicationName)
            .WithDeviceId(Settings.DeviceId)
            .WithUserNamePassword(Settings.UserName, Settings.Password)
            .WithAutoAcceptUntrustedServerCertificate(Settings.AutoAcceptUntrustedServerCertificate)
            .WithSecurity(Settings.UseSecurity)
            .WithSessionTimeout(Settings.SessionTimeout)
            .WithReconnect(options =>
            {
                options.Enabled = true;
                options.MaxAttempts = 1;
                options.InitialDelayMs = 100;
                options.MaxDelayMs = 100;
                options.BackoffMultiplier = 1.0d;
                configure?.Invoke(options);
            });

        return builder.Build();
    }

    public async Task<IOpcUaClient> CreateReconnectEnabledConnectedClientAsync(
        Action<OpcUaReconnectOptions>? configure = null,
        CancellationToken ct = default)
    {
        var client = CreateReconnectEnabledClient(configure);
        await client.ConnectAsync(ct).ConfigureAwait(false);
        return client;
    }
}
