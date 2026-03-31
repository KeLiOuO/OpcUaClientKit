namespace OpcUaClientKit;

/// <summary>
/// Creates <see cref="IOpcUaClient"/> instances using either simple overloads or a richer options object.
/// </summary>
/// <remarks>
/// The factory is the main entry point for most applications. It centralizes
/// option normalization, certificate handling, endpoint selection and session
/// creation so callers can focus on business logic.
/// </remarks>
public interface IOpcUaClientFactory
{
    /// <summary>
    /// Creates a disconnected client using anonymous authentication.
    /// </summary>
    IOpcUaClient Create(
        string serverUrl,
        string applicationName,
        bool autoAcceptUntrustedServerCertificate = false,
        bool useSecurity = true,
        int sessionTimeout = 60000);

    /// <summary>
    /// Creates a disconnected client using anonymous authentication and a device-specific certificate folder.
    /// </summary>
    IOpcUaClient Create(
        string serverUrl,
        string applicationName,
        string deviceId,
        bool autoAcceptUntrustedServerCertificate = false,
        bool useSecurity = true,
        int sessionTimeout = 60000);

    /// <summary>
    /// Creates a disconnected client using username/password authentication.
    /// </summary>
    IOpcUaClient Create(
        string serverUrl,
        string applicationName,
        string userName,
        string password,
        bool autoAcceptUntrustedServerCertificate = false,
        bool useSecurity = true,
        int sessionTimeout = 60000);

    /// <summary>
    /// Creates a disconnected client from a fully populated options object.
    /// </summary>
    IOpcUaClient Create(OpcUaClientOptions options);

    /// <summary>
    /// Creates a client using anonymous authentication and connects it immediately.
    /// </summary>
    Task<IOpcUaClient> CreateConnectedAsync(
        string serverUrl,
        string applicationName,
        bool autoAcceptUntrustedServerCertificate = false,
        bool useSecurity = true,
        int sessionTimeout = 60000,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a client using anonymous authentication and a device-specific certificate folder, then connects it immediately.
    /// </summary>
    Task<IOpcUaClient> CreateConnectedAsync(
        string serverUrl,
        string applicationName,
        string deviceId,
        bool autoAcceptUntrustedServerCertificate = false,
        bool useSecurity = true,
        int sessionTimeout = 60000,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a client using username/password authentication and connects it immediately.
    /// </summary>
    Task<IOpcUaClient> CreateConnectedAsync(
        string serverUrl,
        string applicationName,
        string userName,
        string password,
        bool autoAcceptUntrustedServerCertificate = false,
        bool useSecurity = true,
        int sessionTimeout = 60000,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a disconnected client using username/password authentication and a device-specific certificate folder.
    /// </summary>
    IOpcUaClient Create(
        string serverUrl,
        string applicationName,
        string deviceId,
        string userName,
        string password,
        bool autoAcceptUntrustedServerCertificate = false,
        bool useSecurity = true,
        int sessionTimeout = 60000);

    /// <summary>
    /// Creates a client using username/password authentication and a device-specific certificate folder, then connects it immediately.
    /// </summary>
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

    /// <summary>
    /// Creates a client from a fully populated options object and connects it immediately.
    /// </summary>
    Task<IOpcUaClient> CreateConnectedAsync(
        OpcUaClientOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a builder used to compose an <see cref="OpcUaClientOptions"/> instance fluently.
    /// </summary>
    OpcUaClientBuilder CreateBuilder();
}
