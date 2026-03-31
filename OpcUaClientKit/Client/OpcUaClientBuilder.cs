namespace OpcUaClientKit;

public sealed class OpcUaClientBuilder
{
    private readonly IOpcUaClientFactory _factory;
    private readonly OpcUaClientOptions _options = new();

    internal OpcUaClientBuilder(IOpcUaClientFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public OpcUaClientBuilder WithServerUrl(string serverUrl)
    {
        _options.ServerUrl = serverUrl;
        return this;
    }

    public OpcUaClientBuilder WithApplicationName(string applicationName)
    {
        _options.ApplicationName = applicationName;
        return this;
    }

    public OpcUaClientBuilder WithDeviceId(string deviceId)
    {
        _options.DeviceId = deviceId;
        return this;
    }

    public OpcUaClientBuilder WithSecurity(bool useSecurity)
    {
        _options.UseSecurity = useSecurity;
        return this;
    }

    public OpcUaClientBuilder WithSessionTimeout(int sessionTimeout)
    {
        _options.SessionTimeout = sessionTimeout;
        return this;
    }

    public OpcUaClientBuilder WithOperationTimeout(int operationTimeout)
    {
        _options.OperationTimeout = operationTimeout;
        return this;
    }

    public OpcUaClientBuilder WithCheckDomain(bool checkDomain)
    {
        _options.CheckDomain = checkDomain;
        return this;
    }

    public OpcUaClientBuilder WithAnonymousIdentity()
    {
        _options.UserName = null;
        _options.Password = null;
        return this;
    }

    public OpcUaClientBuilder WithUserNamePassword(string userName, string password)
    {
        _options.UserName = userName;
        _options.Password = password;
        return this;
    }

    public OpcUaClientBuilder WithPkiRootPath(string pkiRootPath)
    {
        _options.Certificate.PkiRootPath = pkiRootPath;
        return this;
    }

    public OpcUaClientBuilder WithAutoAcceptUntrustedServerCertificate(
        bool autoAcceptUntrustedServerCertificate = true)
    {
        _options.Certificate.AutoAcceptUntrustedServerCertificate = autoAcceptUntrustedServerCertificate;
        return this;
    }

    public OpcUaClientBuilder WithCertificateOptions(Action<OpcUaCertificateOptions> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        configure(_options.Certificate);
        return this;
    }

    public IOpcUaClient Build()
    {
        return _factory.Create(_options.Clone());
    }

    public Task<IOpcUaClient> BuildConnectedAsync(CancellationToken ct = default)
    {
        return _factory.CreateConnectedAsync(_options.Clone(), ct);
    }
}

