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
}
