namespace OpcUaClientKit;

/// <summary>
/// Represents the complete configuration required to build an OPC UA client.
/// </summary>
/// <remarks>
/// This type is intended for callers that need more control than the simple
/// factory overloads provide. Most applications can either populate this model
/// directly or use <see cref="OpcUaClientBuilder"/> to create it fluently.
/// </remarks>
public sealed class OpcUaClientOptions
{
    /// <summary>
    /// Gets or sets the target OPC UA server URL.
    /// </summary>
    public string ServerUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the logical application name used for the OPC UA client identity.
    /// </summary>
    public string ApplicationName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional device identifier appended to the application identity
    /// and used to create device-specific certificate folders.
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// Gets or sets the optional session name sent to the server.
    /// </summary>
    public string? SessionName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether secure endpoints should be preferred.
    /// </summary>
    public bool UseSecurity { get; set; } = true;

    /// <summary>
    /// Gets or sets the requested OPC UA session timeout in milliseconds.
    /// </summary>
    public int SessionTimeout { get; set; } = 60000;

    /// <summary>
    /// Gets or sets the timeout used for endpoint discovery and service operations in milliseconds.
    /// </summary>
    public int OperationTimeout { get; set; } = 30000;

    /// <summary>
    /// Gets or sets a value indicating whether endpoint domain validation should be enforced.
    /// </summary>
    public bool CheckDomain { get; set; }

    /// <summary>
    /// Gets or sets the username used for authenticated sessions.
    /// Leave this property empty to use anonymous authentication.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets the password used together with <see cref="UserName"/>.
    /// This value is kept as a managed <see cref="string"/> and therefore remains in memory in plaintext.
    /// Avoid reusing highly sensitive credentials longer than necessary.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets an optional callback used to receive non-fatal client diagnostics such as
    /// swallowed subscription callback exceptions or event-filter fallback warnings.
    /// </summary>
    public Action<OpcUaClientDiagnosticEvent>? DiagnosticsHandler { get; set; }

    /// <summary>
    /// Gets or sets certificate and PKI related options.
    /// </summary>
    public OpcUaCertificateOptions Certificate { get; set; } = new();

    /// <summary>
    /// Gets or sets optional automatic reconnect behavior.
    /// Reconnect is disabled by default.
    /// </summary>
    public OpcUaReconnectOptions Reconnect { get; set; } = new();

    /// <summary>
    /// Creates a deep copy of the current options object.
    /// </summary>
    public OpcUaClientOptions Clone()
    {
        return new OpcUaClientOptions
        {
            ServerUrl = ServerUrl,
            ApplicationName = ApplicationName,
            DeviceId = DeviceId,
            SessionName = SessionName,
            UseSecurity = UseSecurity,
            SessionTimeout = SessionTimeout,
            OperationTimeout = OperationTimeout,
            CheckDomain = CheckDomain,
            UserName = UserName,
            Password = Password,
            DiagnosticsHandler = DiagnosticsHandler,
            Certificate = Certificate.Clone(),
            Reconnect = Reconnect.Clone()
        };
    }
}

/// <summary>
/// Represents certificate and PKI related options used by the OPC UA client.
/// </summary>
public sealed class OpcUaCertificateOptions
{
    /// <summary>
    /// Gets or sets the organization name placed into the generated application certificate subject.
    /// </summary>
    public string? OrganizationName { get; set; }

    /// <summary>
    /// Gets or sets the PKI root directory. If left empty, the factory creates a default folder structure.
    /// </summary>
    public string? PkiRootPath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether untrusted server certificates should be accepted automatically.
    /// </summary>
    public bool AutoAcceptUntrustedServerCertificate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the generated application certificate should be added to the trusted store automatically.
    /// </summary>
    public bool AddAppCertToTrustedStore { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the full client certificate chain should be sent to the server.
    /// </summary>
    public bool SendCertificateChain { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum key size required for generated certificates.
    /// </summary>
    public ushort MinimumKeySize { get; set; } = 2048;

    /// <summary>
    /// Gets or sets a value indicating whether SHA1 signed certificates should be rejected.
    /// </summary>
    public bool RejectSHA1SignedCertificates { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether certificates with unknown revocation status should be rejected.
    /// </summary>
    public bool RejectUnknownRevocationStatus { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of rejected server certificates to keep on disk.
    /// </summary>
    public int MaxRejectedCertificates { get; set; } = 5;

    /// <summary>
    /// Creates a deep copy of the current certificate options object.
    /// </summary>
    public OpcUaCertificateOptions Clone()
    {
        return new OpcUaCertificateOptions
        {
            OrganizationName = OrganizationName,
            PkiRootPath = PkiRootPath,
            AutoAcceptUntrustedServerCertificate = AutoAcceptUntrustedServerCertificate,
            AddAppCertToTrustedStore = AddAppCertToTrustedStore,
            SendCertificateChain = SendCertificateChain,
            MinimumKeySize = MinimumKeySize,
            RejectSHA1SignedCertificates = RejectSHA1SignedCertificates,
            RejectUnknownRevocationStatus = RejectUnknownRevocationStatus,
            MaxRejectedCertificates = MaxRejectedCertificates
        };
    }
}
