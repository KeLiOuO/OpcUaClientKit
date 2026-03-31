namespace OpcUaClientKit;

public interface IOpcUaClientFactory
{
    IOpcUaClient Create(
        string serverUrl,
        string applicationName,
        bool autoAcceptUntrustedServerCertificate = false,
        bool useSecurity = true,
        int sessionTimeout = 60000);

    IOpcUaClient Create(
        string serverUrl,
        string applicationName,
        string deviceId,
        bool autoAcceptUntrustedServerCertificate = false,
        bool useSecurity = true,
        int sessionTimeout = 60000);

    IOpcUaClient Create(
        string serverUrl,
        string applicationName,
        string userName,
        string password,
        bool autoAcceptUntrustedServerCertificate = false,
        bool useSecurity = true,
        int sessionTimeout = 60000);

    IOpcUaClient Create(OpcUaClientOptions options);

    Task<IOpcUaClient> CreateConnectedAsync(
        string serverUrl,
        string applicationName,
        bool autoAcceptUntrustedServerCertificate = false,
        bool useSecurity = true,
        int sessionTimeout = 60000,
        CancellationToken ct = default);

    Task<IOpcUaClient> CreateConnectedAsync(
        string serverUrl,
        string applicationName,
        string deviceId,
        bool autoAcceptUntrustedServerCertificate = false,
        bool useSecurity = true,
        int sessionTimeout = 60000,
        CancellationToken ct = default);

    Task<IOpcUaClient> CreateConnectedAsync(
        string serverUrl,
        string applicationName,
        string userName,
        string password,
        bool autoAcceptUntrustedServerCertificate = false,
        bool useSecurity = true,
        int sessionTimeout = 60000,
        CancellationToken ct = default);

    IOpcUaClient Create(
        string serverUrl,
        string applicationName,
        string deviceId,
        string userName,
        string password,
        bool autoAcceptUntrustedServerCertificate = false,
        bool useSecurity = true,
        int sessionTimeout = 60000);

    Task<IOpcUaClient> CreateConnectedAsync(
        string serverUrl,
        string applicationName,
        string deviceId,
        string userName,
        string password,
        bool autoAcceptUntrustedServerCertificate = false,
        bool useSecurity = true,
        int sessionTimeout = 60000,
        CancellationToken ct = default);

    Task<IOpcUaClient> CreateConnectedAsync(
        OpcUaClientOptions options,
        CancellationToken ct = default);

    OpcUaClientBuilder CreateBuilder();
}
