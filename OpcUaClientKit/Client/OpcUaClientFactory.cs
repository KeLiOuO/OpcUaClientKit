using System.Text;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

namespace OpcUaClientKit;

/// <summary>
/// Creates <see cref="IOpcUaClient"/> instances and owns the OPC UA connection bootstrap pipeline.
/// </summary>
public sealed class OpcUaClientFactory : IOpcUaClientFactory
{
    private const ushort DefaultCertificateLifetimeInMonths = 120;
    private const string Aes256Sha256RsaPssPolicy = "http://opcfoundation.org/UA/SecurityPolicy#Aes256_Sha256_RsaPss";
    private const string Aes128Sha256RsaOaepPolicy = "http://opcfoundation.org/UA/SecurityPolicy#Aes128_Sha256_RsaOaep";
    private static readonly IList<string> s_preferredLocales = new List<string>();
#if !NETSTANDARD2_0
    private static readonly ITelemetryContext s_telemetry = DefaultTelemetry.Create(_ => { });
#endif

    /// <summary>
    /// Creates a disconnected client with anonymous identity and simple options.
    /// </summary>
    public IOpcUaClient Create(
        string serverUrl,
        string applicationName,
        bool autoAcceptUntrustedServerCertificate = false,
        bool useSecurity = true,
        int sessionTimeout = 60000)
    {
        return Create(new OpcUaClientOptions
        {
            ServerUrl = serverUrl,
            ApplicationName = applicationName,
            Certificate =
            {
                AutoAcceptUntrustedServerCertificate = autoAcceptUntrustedServerCertificate
            },
            UseSecurity = useSecurity,
            SessionTimeout = sessionTimeout
        });
    }

    /// <summary>
    /// Creates a disconnected client with anonymous identity and device-specific certificate storage.
    /// </summary>
    public IOpcUaClient Create(
        string serverUrl,
        string applicationName,
        string deviceId,
        bool autoAcceptUntrustedServerCertificate = false,
        bool useSecurity = true,
        int sessionTimeout = 60000)
    {
        return Create(new OpcUaClientOptions
        {
            ServerUrl = serverUrl,
            ApplicationName = applicationName,
            DeviceId = deviceId,
            Certificate =
            {
                AutoAcceptUntrustedServerCertificate = autoAcceptUntrustedServerCertificate
            },
            UseSecurity = useSecurity,
            SessionTimeout = sessionTimeout
        });
    }

    /// <summary>
    /// Creates a disconnected client with username and password credentials.
    /// </summary>
    public IOpcUaClient Create(
        string serverUrl,
        string applicationName,
        string userName,
        string password,
        bool autoAcceptUntrustedServerCertificate = false,
        bool useSecurity = true,
        int sessionTimeout = 60000)
    {
        return Create(new OpcUaClientOptions
        {
            ServerUrl = serverUrl,
            ApplicationName = applicationName,
            UserName = userName,
            Password = password,
            Certificate =
            {
                AutoAcceptUntrustedServerCertificate = autoAcceptUntrustedServerCertificate
            },
            UseSecurity = useSecurity,
            SessionTimeout = sessionTimeout
        });
    }

    /// <summary>
    /// Creates a disconnected client with username and password credentials plus device-specific certificate storage.
    /// </summary>
    public IOpcUaClient Create(
        string serverUrl,
        string applicationName,
        string deviceId,
        string userName,
        string password,
        bool autoAcceptUntrustedServerCertificate = false,
        bool useSecurity = true,
        int sessionTimeout = 60000)
    {
        return Create(new OpcUaClientOptions
        {
            ServerUrl = serverUrl,
            ApplicationName = applicationName,
            DeviceId = deviceId,
            UserName = userName,
            Password = password,
            Certificate =
            {
                AutoAcceptUntrustedServerCertificate = autoAcceptUntrustedServerCertificate
            },
            UseSecurity = useSecurity,
            SessionTimeout = sessionTimeout
        });
    }

    /// <summary>
    /// Creates a disconnected client from a complete options object.
    /// </summary>
    public IOpcUaClient Create(OpcUaClientOptions options)
    {
        var normalizedOptions = NormalizeOptions(options);
        return CreateClientCore(normalizedOptions);
    }

    /// <summary>
    /// Creates and connects an anonymous client using the simple parameter set.
    /// </summary>
    public async Task<IOpcUaClient> CreateConnectedAsync(
        string serverUrl,
        string applicationName,
        bool autoAcceptUntrustedServerCertificate = false,
        bool useSecurity = true,
        int sessionTimeout = 60000,
        CancellationToken ct = default)
    {
        var client = Create(
            serverUrl,
            applicationName,
            autoAcceptUntrustedServerCertificate,
            useSecurity,
            sessionTimeout);
        await client.ConnectAsync(ct).ConfigureAwait(false);
        return client;
    }

    /// <summary>
    /// Creates and connects an anonymous client using device-specific certificate storage.
    /// </summary>
    public async Task<IOpcUaClient> CreateConnectedAsync(
        string serverUrl,
        string applicationName,
        string deviceId,
        bool autoAcceptUntrustedServerCertificate = false,
        bool useSecurity = true,
        int sessionTimeout = 60000,
        CancellationToken ct = default)
    {
        var client = Create(
            serverUrl,
            applicationName,
            deviceId,
            autoAcceptUntrustedServerCertificate,
            useSecurity,
            sessionTimeout);
        await client.ConnectAsync(ct).ConfigureAwait(false);
        return client;
    }

    /// <summary>
    /// Creates and connects a username/password client using the simple parameter set.
    /// </summary>
    public async Task<IOpcUaClient> CreateConnectedAsync(
        string serverUrl,
        string applicationName,
        string userName,
        string password,
        bool autoAcceptUntrustedServerCertificate = false,
        bool useSecurity = true,
        int sessionTimeout = 60000,
        CancellationToken ct = default)
    {
        var client = Create(
            serverUrl,
            applicationName,
            userName,
            password,
            autoAcceptUntrustedServerCertificate,
            useSecurity,
            sessionTimeout);
        await client.ConnectAsync(ct).ConfigureAwait(false);
        return client;
    }

    /// <summary>
    /// Creates and connects a username/password client using device-specific certificate storage.
    /// </summary>
    public async Task<IOpcUaClient> CreateConnectedAsync(
        string serverUrl,
        string applicationName,
        string deviceId,
        string userName,
        string password,
        bool autoAcceptUntrustedServerCertificate = false,
        bool useSecurity = true,
        int sessionTimeout = 60000,
        CancellationToken ct = default)
    {
        var client = Create(
            serverUrl,
            applicationName,
            deviceId,
            userName,
            password,
            autoAcceptUntrustedServerCertificate,
            useSecurity,
            sessionTimeout);
        await client.ConnectAsync(ct).ConfigureAwait(false);
        return client;
    }

    /// <summary>
    /// Creates and connects a client from a complete options object.
    /// </summary>
    public async Task<IOpcUaClient> CreateConnectedAsync(
        OpcUaClientOptions options,
        CancellationToken ct = default)
    {
        var client = Create(options);
        await client.ConnectAsync(ct).ConfigureAwait(false);
        return client;
    }

    /// <summary>
    /// Creates a fluent builder for advanced client configuration.
    /// </summary>
    public OpcUaClientBuilder CreateBuilder()
    {
        return new OpcUaClientBuilder(this);
    }

    private IOpcUaClient CreateClientCore(OpcUaClientOptions options)
    {
        return new OpcUaClient(options, ConnectCoreAsync);
    }

    private async Task<OpcUaClientConnection> ConnectCoreAsync(
        OpcUaClientOptions options,
        CancellationToken ct)
    {
        var effectiveApplicationName = OpcUaClientPathHelper.ResolveEffectiveApplicationName(
            options.ApplicationName,
            options.DeviceId);
        var sessionName = options.SessionName ?? effectiveApplicationName;

        // Keep the bootstrap order explicit so configuration, certificates, endpoint selection
        // and session creation always operate on the same normalized option set.
        var configuration = await BuildApplicationConfigurationAsync(
                options,
                effectiveApplicationName,
                ct)
            .ConfigureAwait(false);
        var certificateValidationHandler = await EnsureApplicationCertificateAsync(
                configuration,
                effectiveApplicationName,
                options,
                ct)
            .ConfigureAwait(false);

        var endpoint = await SelectEndpointAsync(configuration, options, ct).ConfigureAwait(false);
        var userIdentity = CreateUserIdentity(options);
        var session = await CreateSessionAsync(
                configuration,
                endpoint,
                userIdentity,
                sessionName,
                options,
                ct)
            .ConfigureAwait(false);

        return new OpcUaClientConnection(configuration, session, certificateValidationHandler);
    }

    private static OpcUaClientOptions NormalizeOptions(OpcUaClientOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var normalized = options.Clone();

        normalized.ServerUrl = normalized.ServerUrl?.Trim() ?? string.Empty;
        normalized.ApplicationName = normalized.ApplicationName?.Trim() ?? string.Empty;
        normalized.DeviceId = string.IsNullOrWhiteSpace(normalized.DeviceId)
            ? null
            : normalized.DeviceId.Trim();
        normalized.SessionName = string.IsNullOrWhiteSpace(normalized.SessionName)
            ? null
            : normalized.SessionName.Trim();
        normalized.PreferredSecurityPolicyUri = string.IsNullOrWhiteSpace(normalized.PreferredSecurityPolicyUri)
            ? null
            : normalized.PreferredSecurityPolicyUri.Trim();
        normalized.UserName = string.IsNullOrWhiteSpace(normalized.UserName)
            ? null
            : normalized.UserName.Trim();
        normalized.Password = normalized.UserName == null
            ? null
            : normalized.Password ?? string.Empty;
        normalized.Certificate.OrganizationName =
            string.IsNullOrWhiteSpace(normalized.Certificate.OrganizationName)
                ? null
                : normalized.Certificate.OrganizationName.Trim();
        normalized.Certificate.PkiRootPath =
            string.IsNullOrWhiteSpace(normalized.Certificate.PkiRootPath)
                ? null
                : normalized.Certificate.PkiRootPath.Trim();

        if (string.IsNullOrWhiteSpace(normalized.ServerUrl))
        {
            throw new ArgumentException("ServerUrl is required.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(normalized.ApplicationName))
        {
            throw new ArgumentException("ApplicationName is required.", nameof(options));
        }

        if (!string.IsNullOrWhiteSpace(normalized.DeviceId) &&
            normalized.DeviceId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("DeviceId contains invalid path characters.", nameof(options));
        }

        if (normalized.SessionTimeout <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "SessionTimeout must be greater than 0.");
        }

        if (normalized.OperationTimeout <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "OperationTimeout must be greater than 0.");
        }

        if (normalized.PreferredMessageSecurityMode == MessageSecurityMode.Invalid)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "PreferredMessageSecurityMode cannot be MessageSecurityMode.Invalid.");
        }

        if (normalized.Certificate.MinimumKeySize == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MinimumKeySize must be greater than 0.");
        }

        if (normalized.Certificate.MaxRejectedCertificates < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MaxRejectedCertificates must be greater than 0.");
        }

        return normalized;
    }

    private static async Task<ApplicationConfiguration> BuildApplicationConfigurationAsync(
        OpcUaClientOptions options,
        string effectiveApplicationName,
        CancellationToken ct)
    {
        // The SDK expects a complete PKI layout up front. Creating the directories here keeps
        // certificate generation and trust-list updates self-contained for callers.
        var pkiRoot = ResolvePkiRootPath(options);
        var ownStorePath = EnsureDirectory(Path.Combine(pkiRoot, "own"));
        var trustedStorePath = EnsureDirectory(Path.Combine(pkiRoot, "trusted"));
        var issuerStorePath = EnsureDirectory(Path.Combine(pkiRoot, "issuer"));
        var rejectedStorePath = EnsureDirectory(Path.Combine(pkiRoot, "rejected"));
        var trustedUserStorePath = EnsureDirectory(Path.Combine(pkiRoot, "trustedUser"));
        var userIssuerStorePath = EnsureDirectory(Path.Combine(pkiRoot, "userIssuer"));
        var certificateSubjectName = CreateCertificateSubjectName(
            effectiveApplicationName,
            options.Certificate.OrganizationName);
        var applicationCertificates = ApplicationConfigurationBuilder.CreateDefaultApplicationCertificates(
            certificateSubjectName,
            CertificateStoreType.Directory,
            ownStorePath);

        var applicationConfiguration = new ApplicationConfiguration
        {
            ApplicationName = effectiveApplicationName,
            ApplicationUri = CreateApplicationUri(effectiveApplicationName),
            ProductUri = CreateProductUri(effectiveApplicationName),
            ApplicationType = ApplicationType.Client,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificates = applicationCertificates,
                TrustedPeerCertificates = CreateTrustList(trustedStorePath),
                TrustedIssuerCertificates = CreateTrustList(issuerStorePath),
                RejectedCertificateStore = CreateTrustList(rejectedStorePath),
                TrustedUserCertificates = CreateTrustList(trustedUserStorePath),
                UserIssuerCertificates = CreateTrustList(userIssuerStorePath),
                AutoAcceptUntrustedCertificates = options.Certificate.AutoAcceptUntrustedServerCertificate,
                AddAppCertToTrustedStore = options.Certificate.AddAppCertToTrustedStore,
                SendCertificateChain = options.Certificate.SendCertificateChain,
                MinimumCertificateKeySize = options.Certificate.MinimumKeySize,
                RejectSHA1SignedCertificates = options.Certificate.RejectSHA1SignedCertificates,
                RejectUnknownRevocationStatus = options.Certificate.RejectUnknownRevocationStatus,
                MaxRejectedCertificates = options.Certificate.MaxRejectedCertificates
            },
            TransportConfigurations = new TransportConfigurationCollection(),
            TransportQuotas = new TransportQuotas
            {
                OperationTimeout = options.OperationTimeout
            },
            ClientConfiguration = new ClientConfiguration
            {
                DefaultSessionTimeout = options.SessionTimeout
            }
        };

#if NETSTANDARD2_0
        await applicationConfiguration.Validate(ApplicationType.Client).ConfigureAwait(false);
#else
        await applicationConfiguration.ValidateAsync(ApplicationType.Client, ct).ConfigureAwait(false);
#endif
        return applicationConfiguration;
    }

    private static async Task<CertificateValidationEventHandler> EnsureApplicationCertificateAsync(
        ApplicationConfiguration configuration,
        string effectiveApplicationName,
        OpcUaClientOptions options,
        CancellationToken ct)
    {
        var application =
#if NETSTANDARD2_0
            new ApplicationInstance
#else
            new ApplicationInstance(s_telemetry)
#endif
        {
            ApplicationName = effectiveApplicationName,
            ApplicationType = ApplicationType.Client,
            ApplicationConfiguration = configuration
        };

        // This call both loads an existing application certificate and creates one on first use.
        var hasApplicationCertificate = await application
#if NETSTANDARD2_0
            .CheckApplicationInstanceCertificates(true, DefaultCertificateLifetimeInMonths, ct)
#else
            .CheckApplicationInstanceCertificatesAsync(true, DefaultCertificateLifetimeInMonths, ct)
#endif
            .ConfigureAwait(false);

        if (!hasApplicationCertificate)
        {
            throw new InvalidOperationException("The OPC UA application certificate could not be created or loaded.");
        }

#if NETSTANDARD2_0
        await configuration.CertificateValidator.Update(configuration).ConfigureAwait(false);
#else
        await configuration.CertificateValidator.UpdateAsync(configuration, ct).ConfigureAwait(false);
#endif

        var certificateValidationHandler = CreateCertificateValidationHandler(options);
        configuration.CertificateValidator.CertificateValidation += certificateValidationHandler;
        return certificateValidationHandler;
    }

    private static CertificateValidationEventHandler CreateCertificateValidationHandler(
        OpcUaClientOptions options)
    {
        return (_, e) =>
        {
            if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted &&
                options.Certificate.AutoAcceptUntrustedServerCertificate)
            {
                e.Accept = true;
            }
        };
    }

    private static async Task<ConfiguredEndpoint> SelectEndpointAsync(
        ApplicationConfiguration configuration,
        OpcUaClientOptions options,
        CancellationToken ct)
    {
        // Discover the full endpoint set first so the library can apply a deterministic
        // ranking strategy and support explicit policy/mode selection.
        var endpointDescriptions = await DiscoverEndpointsAsync(configuration, options, ct).ConfigureAwait(false);
        var endpointDescription = SelectEndpointDescription(endpointDescriptions, options);

        return new ConfiguredEndpoint(
            null,
            endpointDescription,
            EndpointConfiguration.Create(configuration));
    }

    private static async Task<EndpointDescriptionCollection> DiscoverEndpointsAsync(
        ApplicationConfiguration configuration,
        OpcUaClientOptions options,
        CancellationToken ct)
    {
        var endpointConfiguration = EndpointConfiguration.Create(configuration);
        var discoveryUrl = CoreClientUtils.GetDiscoveryUrl(options.ServerUrl);

#if NETSTANDARD2_0
        using var discoveryClient = DiscoveryClient.Create(configuration, discoveryUrl, endpointConfiguration);
#else
        using var discoveryClient = await DiscoveryClient
            .CreateAsync(configuration, discoveryUrl, endpointConfiguration, DiagnosticsMasks.None, ct)
            .ConfigureAwait(false);
#endif
        var endpointDescriptions = await discoveryClient
            .GetEndpointsAsync(null, ct)
            .ConfigureAwait(false);

        PatchEndpointUrls(endpointDescriptions, discoveryUrl);

        if (endpointDescriptions == null || endpointDescriptions.Count == 0)
        {
            throw new InvalidOperationException(
                $"No OPC UA endpoints were discovered for server '{options.ServerUrl}'.");
        }

        return endpointDescriptions;
    }

    private static void PatchEndpointUrls(
        IEnumerable<EndpointDescription> endpointDescriptions,
        Uri discoveryUrl)
    {
        foreach (var endpointDescription in endpointDescriptions)
        {
            if (endpointDescription == null ||
                string.IsNullOrWhiteSpace(endpointDescription.EndpointUrl) ||
                !Uri.TryCreate(endpointDescription.EndpointUrl, UriKind.Absolute, out var endpointUrl) ||
                !RequiresHostPatch(endpointUrl))
            {
                continue;
            }

            var patchedEndpointUrl = new UriBuilder(endpointUrl)
            {
                Host = discoveryUrl.Host
            };

            endpointDescription.EndpointUrl = patchedEndpointUrl.Uri.AbsoluteUri;
        }
    }

    private static bool RequiresHostPatch(Uri endpointUrl)
    {
        return endpointUrl.IsLoopback ||
               string.Equals(endpointUrl.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(endpointUrl.Host, "0.0.0.0", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(endpointUrl.Host, "::", StringComparison.OrdinalIgnoreCase);
    }

    private static EndpointDescription SelectEndpointDescription(
        EndpointDescriptionCollection endpointDescriptions,
        OpcUaClientOptions options)
    {
        var candidates = endpointDescriptions
            .OfType<EndpointDescription>()
            .Where(static endpoint => endpoint != null)
            .ToList();

        if (candidates.Count == 0)
        {
            throw new InvalidOperationException(
                $"No OPC UA endpoints were discovered for server '{options.ServerUrl}'.");
        }

        candidates = candidates
            .Where(endpoint => SupportsRequestedIdentity(endpoint, options))
            .ToList();

        var hasExplicitSecuritySelection =
            !string.IsNullOrWhiteSpace(options.PreferredSecurityPolicyUri) ||
            options.PreferredMessageSecurityMode.HasValue;

        if (!string.IsNullOrWhiteSpace(options.PreferredSecurityPolicyUri))
        {
            candidates = candidates
                .Where(endpoint => string.Equals(
                    endpoint.SecurityPolicyUri,
                    options.PreferredSecurityPolicyUri,
                    StringComparison.Ordinal))
                .ToList();
        }

        if (options.PreferredMessageSecurityMode.HasValue)
        {
            candidates = candidates
                .Where(endpoint => endpoint.SecurityMode == options.PreferredMessageSecurityMode.Value)
                .ToList();
        }

        if (!hasExplicitSecuritySelection)
        {
            if (options.UseSecurity)
            {
                var secureCandidates = candidates
                    .Where(static endpoint => endpoint.SecurityMode != MessageSecurityMode.None)
                    .ToList();

                if (secureCandidates.Count > 0)
                {
                    candidates = secureCandidates;
                }
            }
            else
            {
                candidates = candidates
                    .Where(static endpoint => endpoint.SecurityMode == MessageSecurityMode.None)
                    .ToList();
            }
        }

        if (candidates.Count == 0)
        {
            throw CreateEndpointSelectionException(endpointDescriptions, options);
        }

        return candidates
            .OrderByDescending(endpoint => GetSecurityModeRank(endpoint.SecurityMode))
            .ThenByDescending(endpoint => endpoint.SecurityLevel)
            .ThenByDescending(endpoint => GetSecurityPolicyRank(endpoint.SecurityPolicyUri))
            .ThenBy(endpoint => endpoint.EndpointUrl, StringComparer.OrdinalIgnoreCase)
            .First();
    }

    private static bool SupportsRequestedIdentity(
        EndpointDescription endpoint,
        OpcUaClientOptions options)
    {
        if (endpoint.UserIdentityTokens == null || endpoint.UserIdentityTokens.Count == 0)
        {
            return true;
        }

        var requiredTokenType = string.IsNullOrWhiteSpace(options.UserName)
            ? UserTokenType.Anonymous
            : UserTokenType.UserName;

        return endpoint.UserIdentityTokens.Any(policy => policy.TokenType == requiredTokenType);
    }

    private static InvalidOperationException CreateEndpointSelectionException(
        IEnumerable<EndpointDescription> endpointDescriptions,
        OpcUaClientOptions options)
    {
        var requestedSelectionSummary = FormatRequestedEndpointSelection(options);
        var availableEndpointsSummary = string.Join(
            "; ",
            endpointDescriptions.Select(FormatEndpointSummary));

        return new InvalidOperationException(
            $"No OPC UA endpoint matched the requested selection ({requestedSelectionSummary}). " +
            $"Available endpoints: {availableEndpointsSummary}");
    }

    private static string FormatRequestedEndpointSelection(OpcUaClientOptions options)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(options.PreferredSecurityPolicyUri))
        {
            parts.Add($"SecurityPolicyUri='{options.PreferredSecurityPolicyUri}'");
        }

        if (options.PreferredMessageSecurityMode.HasValue)
        {
            parts.Add($"SecurityMode='{options.PreferredMessageSecurityMode.Value}'");
        }

        if (parts.Count == 0)
        {
            parts.Add(options.UseSecurity
                ? "automatic secure endpoint selection"
                : "automatic non-secure endpoint selection");
        }

        parts.Add(string.IsNullOrWhiteSpace(options.UserName)
            ? "Identity='Anonymous'"
            : "Identity='UserName'");

        return string.Join(", ", parts);
    }

    private static string FormatEndpointSummary(EndpointDescription endpoint)
    {
        return
            $"Mode={endpoint.SecurityMode}, " +
            $"Policy={endpoint.SecurityPolicyUri}, " +
            $"Level={endpoint.SecurityLevel}, " +
            $"Url={endpoint.EndpointUrl}";
    }

    private static int GetSecurityModeRank(MessageSecurityMode securityMode)
    {
        return securityMode switch
        {
            MessageSecurityMode.SignAndEncrypt => 3,
            MessageSecurityMode.Sign => 2,
            MessageSecurityMode.None => 1,
            _ => 0
        };
    }

    private static int GetSecurityPolicyRank(string? securityPolicyUri)
    {
        return securityPolicyUri switch
        {
            Aes256Sha256RsaPssPolicy => 600,
            Aes128Sha256RsaOaepPolicy => 500,
            SecurityPolicies.Basic256Sha256 => 400,
            SecurityPolicies.Basic256 => 300,
            SecurityPolicies.Basic128Rsa15 => 200,
            SecurityPolicies.None => 0,
            _ => 100
        };
    }

    private static IUserIdentity CreateUserIdentity(OpcUaClientOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.UserName))
        {
            return new UserIdentity(new AnonymousIdentityToken());
        }

        return new UserIdentity(
#if NETSTANDARD2_0
            options.UserName,
            options.Password ?? string.Empty);
#else
            options.UserName,
            Encoding.UTF8.GetBytes(options.Password ?? string.Empty));
#endif
    }

    private static async Task<ISession> CreateSessionAsync(
        ApplicationConfiguration configuration,
        ConfiguredEndpoint endpoint,
        IUserIdentity userIdentity,
        string sessionName,
        OpcUaClientOptions options,
        CancellationToken ct)
    {
        // Session creation is the last step after configuration, certificate handling and
        // endpoint discovery have completed successfully.
        var sessionFactory =
#if NETSTANDARD2_0
            DefaultSessionFactory.Instance;
#else
            new DefaultSessionFactory(s_telemetry);
#endif
        return await sessionFactory
            .CreateAsync(
                configuration,
                endpoint,
                true,
                options.CheckDomain,
                sessionName,
                (uint)options.SessionTimeout,
                userIdentity,
                s_preferredLocales,
                ct)
            .ConfigureAwait(false);
    }

    private static CertificateTrustList CreateTrustList(string storePath)
    {
        return new CertificateTrustList
        {
            StoreType = CertificateStoreType.Directory,
            StorePath = storePath
        };
    }

    private static string ResolvePkiRootPath(OpcUaClientOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Certificate.PkiRootPath))
        {
            return EnsureDirectory(
                Path.GetFullPath(Environment.ExpandEnvironmentVariables(options.Certificate.PkiRootPath)));
        }

        return EnsureDirectory(
            OpcUaClientPathHelper.ResolveDefaultPkiRootPath(options.ApplicationName, options.DeviceId));
    }

    private static string EnsureDirectory(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }

    private static string CreateCertificateSubjectName(string applicationName, string? organizationName)
    {
        if (string.IsNullOrWhiteSpace(organizationName))
        {
            return $"CN={applicationName}";
        }

        return $"CN={applicationName}, O={organizationName}";
    }

    private static string CreateApplicationUri(string applicationName)
    {
        return $"urn:{Environment.MachineName}:{applicationName}";
    }

    private static string CreateProductUri(string applicationName)
    {
        return $"uri:{applicationName}";
    }

}

internal sealed record OpcUaClientConnection(
    ApplicationConfiguration Configuration,
    ISession Session,
    CertificateValidationEventHandler CertificateValidationHandler);



