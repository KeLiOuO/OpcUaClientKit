using OpcUaClientKit;
using Opc.Ua;

namespace OpcUaClientKit.Demo;

internal static class DemoClientFactory
{
    public static Task<IOpcUaClient> CreateSimpleConnectedClientAsync(
        IOpcUaClientFactory factory,
        DemoSettings settings,
        CancellationToken ct = default)
    {
        return factory.CreateConnectedAsync(
            settings.ServerUrl,
            settings.ApplicationName,
            settings.SimpleDeviceId,
            settings.UserName,
            settings.Password,
            settings.AutoAcceptUntrustedServerCertificate,
            settings.UseSecurity,
            settings.SessionTimeout,
            ct);
    }

    public static Task<IOpcUaClient> CreateAdvancedConnectedClientAsync(
        IOpcUaClientFactory factory,
        DemoSettings settings,
        CancellationToken ct = default)
    {
        var builder = factory
            .CreateBuilder()
            .WithServerUrl(settings.ServerUrl)
            .WithApplicationName(settings.ApplicationName)
            .WithDeviceId(settings.AdvancedDeviceId)
            .WithUserNamePassword(settings.UserName, settings.Password)
            .WithSecurity(settings.UseSecurity)
            .WithSessionTimeout(settings.SessionTimeout)
            .WithOperationTimeout(settings.OperationTimeout)
            .WithCheckDomain(settings.CheckDomain)
            .WithAutoAcceptUntrustedServerCertificate(settings.AutoAcceptUntrustedServerCertificate)
            .WithCertificateOptions(options =>
            {
                options.AddAppCertToTrustedStore = false;
                options.SendCertificateChain = true;
                options.MinimumKeySize = 2048;
                options.RejectSHA1SignedCertificates = true;
                options.RejectUnknownRevocationStatus = true;
                options.MaxRejectedCertificates = 5;
            });

        ApplyPreferredSecurityProfile(builder, settings);
        return builder.BuildConnectedAsync(ct);
    }

    public static Task<IOpcUaClient> CreateReconnectConnectedClientAsync(
        IOpcUaClientFactory factory,
        DemoSettings settings,
        Action<OpcUaReconnectEvent>? reconnectHandler = null,
        CancellationToken ct = default)
    {
        var builder = factory
            .CreateBuilder()
            .WithServerUrl(settings.ServerUrl)
            .WithApplicationName(settings.ApplicationName)
            .WithDeviceId($"{settings.AdvancedDeviceId}-reconnect")
            .WithUserNamePassword(settings.UserName, settings.Password)
            .WithSecurity(settings.UseSecurity)
            .WithSessionTimeout(settings.SessionTimeout)
            .WithOperationTimeout(settings.OperationTimeout)
            .WithCheckDomain(settings.CheckDomain)
            .WithAutoAcceptUntrustedServerCertificate(settings.AutoAcceptUntrustedServerCertificate)
            .WithCertificateOptions(options =>
            {
                options.AddAppCertToTrustedStore = false;
                options.SendCertificateChain = true;
                options.MinimumKeySize = 2048;
                options.RejectSHA1SignedCertificates = true;
                options.RejectUnknownRevocationStatus = true;
                options.MaxRejectedCertificates = 5;
            })
            .WithReconnect(reconnect =>
            {
                reconnect.Enabled = true;
                reconnect.MaxAttempts = settings.ReconnectMaxAttempts;
                reconnect.ReconnectImmediatelyOnFirstFailure = settings.ReconnectImmediatelyOnFirstFailure;
                reconnect.InitialDelayMs = settings.ReconnectInitialDelayMs;
                reconnect.MaxDelayMs = settings.ReconnectMaxDelayMs;
                reconnect.BackoffMultiplier = settings.ReconnectBackoffMultiplier;
                reconnect.ReconnectHandler = reconnectHandler;
            });

        ApplyPreferredSecurityProfile(builder, settings);
        return builder.BuildConnectedAsync(ct);
    }

    private static void ApplyPreferredSecurityProfile(
        OpcUaClientBuilder builder,
        DemoSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.PreferredSecurityPolicyUri))
        {
            builder.WithSecurityPolicyUri(settings.PreferredSecurityPolicyUri);
        }

        if (settings.ParsedPreferredMessageSecurityMode.HasValue)
        {
            builder.WithMessageSecurityMode(settings.ParsedPreferredMessageSecurityMode.Value);
        }
    }
}
