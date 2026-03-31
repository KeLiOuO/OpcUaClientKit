namespace OpcUaClientKit;

public sealed class OpcUaClientOptions
{
    public string ServerUrl { get; set; } = string.Empty;

    public string ApplicationName { get; set; } = string.Empty;

    public string? DeviceId { get; set; }

    public string? SessionName { get; set; }

    public bool UseSecurity { get; set; } = true;

    public int SessionTimeout { get; set; } = 60000;

    public int OperationTimeout { get; set; } = 30000;

    public bool CheckDomain { get; set; }

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public OpcUaCertificateOptions Certificate { get; set; } = new();

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
            Certificate = Certificate.Clone()
        };
    }
}

public sealed class OpcUaCertificateOptions
{
    public string? OrganizationName { get; set; }

    public string? PkiRootPath { get; set; }

    public bool AutoAcceptUntrustedServerCertificate { get; set; }

    public bool AddAppCertToTrustedStore { get; set; }

    public bool SendCertificateChain { get; set; } = true;

    public ushort MinimumKeySize { get; set; } = 2048;

    public bool RejectSHA1SignedCertificates { get; set; } = true;

    public bool RejectUnknownRevocationStatus { get; set; } = true;

    public int MaxRejectedCertificates { get; set; } = 5;

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
