namespace OpcUaClientKit;

/// <summary>
/// Provides a fluent API for composing <see cref="IOpcUaClient"/> options.
/// </summary>
public sealed class OpcUaClientBuilder
{
    private readonly IOpcUaClientFactory _factory;
    private readonly OpcUaClientOptions _options = new();

    internal OpcUaClientBuilder(IOpcUaClientFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <summary>
    /// Sets the OPC UA server endpoint URL.
    /// </summary>
    public OpcUaClientBuilder WithServerUrl(string serverUrl)
    {
        _options.ServerUrl = serverUrl;
        return this;
    }

    /// <summary>
    /// Sets the base application name used for the client identity and certificate subject.
    /// </summary>
    public OpcUaClientBuilder WithApplicationName(string applicationName)
    {
        _options.ApplicationName = applicationName;
        return this;
    }

    /// <summary>
    /// Sets the logical device identifier used to derive a device-specific certificate path.
    /// </summary>
    public OpcUaClientBuilder WithDeviceId(string deviceId)
    {
        _options.DeviceId = deviceId;
        return this;
    }

    /// <summary>
    /// Enables or disables secure endpoint selection.
    /// </summary>
    public OpcUaClientBuilder WithSecurity(bool useSecurity)
    {
        _options.UseSecurity = useSecurity;
        return this;
    }

    /// <summary>
    /// Sets the default session timeout in milliseconds.
    /// </summary>
    public OpcUaClientBuilder WithSessionTimeout(int sessionTimeout)
    {
        _options.SessionTimeout = sessionTimeout;
        return this;
    }

    /// <summary>
    /// Sets the per-operation timeout in milliseconds.
    /// </summary>
    public OpcUaClientBuilder WithOperationTimeout(int operationTimeout)
    {
        _options.OperationTimeout = operationTimeout;
        return this;
    }

    /// <summary>
    /// Controls whether the endpoint domain is checked when the session is created.
    /// </summary>
    public OpcUaClientBuilder WithCheckDomain(bool checkDomain)
    {
        _options.CheckDomain = checkDomain;
        return this;
    }

    /// <summary>
    /// Configures the client to connect anonymously.
    /// </summary>
    public OpcUaClientBuilder WithAnonymousIdentity()
    {
        _options.UserName = null;
        _options.Password = null;
        return this;
    }

    /// <summary>
    /// Configures the client to connect with username and password credentials.
    /// </summary>
    public OpcUaClientBuilder WithUserNamePassword(string userName, string password)
    {
        _options.UserName = userName;
        _options.Password = password;
        return this;
    }

    /// <summary>
    /// Overrides the default PKI root directory used by the SDK.
    /// </summary>
    public OpcUaClientBuilder WithPkiRootPath(string pkiRootPath)
    {
        _options.Certificate.PkiRootPath = pkiRootPath;
        return this;
    }

    /// <summary>
    /// Enables or disables automatic acceptance of untrusted server certificates.
    /// </summary>
    public OpcUaClientBuilder WithAutoAcceptUntrustedServerCertificate(
        bool autoAcceptUntrustedServerCertificate = true)
    {
        _options.Certificate.AutoAcceptUntrustedServerCertificate = autoAcceptUntrustedServerCertificate;
        return this;
    }

    /// <summary>
    /// Applies additional certificate settings to the pending options instance.
    /// </summary>
    public OpcUaClientBuilder WithCertificateOptions(Action<OpcUaCertificateOptions> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        configure(_options.Certificate);
        return this;
    }

    /// <summary>
    /// Creates a disconnected client from the current builder state.
    /// </summary>
    public IOpcUaClient Build()
    {
        return _factory.Create(_options.Clone());
    }

    /// <summary>
    /// Creates and connects a client from the current builder state.
    /// </summary>
    public Task<IOpcUaClient> BuildConnectedAsync(CancellationToken ct = default)
    {
        return _factory.CreateConnectedAsync(_options.Clone(), ct);
    }
}

