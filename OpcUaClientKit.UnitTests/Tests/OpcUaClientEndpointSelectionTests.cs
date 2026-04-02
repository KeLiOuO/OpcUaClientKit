using Opc.Ua;
using OpcUaClientKit.UnitTests.Infrastructure;

namespace OpcUaClientKit.UnitTests.Tests;

public sealed class OpcUaClientEndpointSelectionTests
{
    [Fact]
    public void SelectEndpointDescription_prefers_highest_security_endpoint_when_useSecurity_is_enabled()
    {
        var selected = InvokeSelectEndpointDescription(
            new OpcUaClientOptions
            {
                ServerUrl = "opc.tcp://127.0.0.1:4840",
                ApplicationName = "UnitTests",
                UseSecurity = true
            },
            CreateEndpoint(
                "opc.tcp://127.0.0.1:4840/none",
                SecurityPolicies.None,
                MessageSecurityMode.None,
                1,
                UserTokenType.Anonymous),
            CreateEndpoint(
                "opc.tcp://127.0.0.1:4840/basic128",
                SecurityPolicies.Basic128Rsa15,
                MessageSecurityMode.SignAndEncrypt,
                10,
                UserTokenType.Anonymous),
            CreateEndpoint(
                "opc.tcp://127.0.0.1:4840/basic256sha256",
                SecurityPolicies.Basic256Sha256,
                MessageSecurityMode.SignAndEncrypt,
                10,
                UserTokenType.Anonymous));

        Assert.Equal(SecurityPolicies.Basic256Sha256, selected.SecurityPolicyUri);
        Assert.Equal(MessageSecurityMode.SignAndEncrypt, selected.SecurityMode);
    }

    [Fact]
    public void SelectEndpointDescription_selects_none_endpoint_when_useSecurity_is_disabled()
    {
        var selected = InvokeSelectEndpointDescription(
            new OpcUaClientOptions
            {
                ServerUrl = "opc.tcp://127.0.0.1:4840",
                ApplicationName = "UnitTests",
                UseSecurity = false
            },
            CreateEndpoint(
                "opc.tcp://127.0.0.1:4840/basic256",
                SecurityPolicies.Basic256,
                MessageSecurityMode.Sign,
                20,
                UserTokenType.Anonymous),
            CreateEndpoint(
                "opc.tcp://127.0.0.1:4840/none",
                SecurityPolicies.None,
                MessageSecurityMode.None,
                1,
                UserTokenType.Anonymous));

        Assert.Equal(SecurityPolicies.None, selected.SecurityPolicyUri);
        Assert.Equal(MessageSecurityMode.None, selected.SecurityMode);
    }

    [Fact]
    public void SelectEndpointDescription_filters_by_explicit_security_policy()
    {
        var selected = InvokeSelectEndpointDescription(
            new OpcUaClientOptions
            {
                ServerUrl = "opc.tcp://127.0.0.1:4840",
                ApplicationName = "UnitTests",
                PreferredSecurityPolicyUri = SecurityPolicies.Basic128Rsa15
            },
            CreateEndpoint(
                "opc.tcp://127.0.0.1:4840/basic256sha256",
                SecurityPolicies.Basic256Sha256,
                MessageSecurityMode.SignAndEncrypt,
                10,
                UserTokenType.Anonymous),
            CreateEndpoint(
                "opc.tcp://127.0.0.1:4840/basic128",
                SecurityPolicies.Basic128Rsa15,
                MessageSecurityMode.SignAndEncrypt,
                2,
                UserTokenType.Anonymous));

        Assert.Equal(SecurityPolicies.Basic128Rsa15, selected.SecurityPolicyUri);
    }

    [Fact]
    public void SelectEndpointDescription_prefers_higher_security_level_before_policy_rank_when_modes_match()
    {
        var selected = InvokeSelectEndpointDescription(
            new OpcUaClientOptions
            {
                ServerUrl = "opc.tcp://127.0.0.1:4840",
                ApplicationName = "UnitTests",
                UseSecurity = true
            },
            CreateEndpoint(
                "opc.tcp://127.0.0.1:4840/basic256sha256-lowlevel",
                SecurityPolicies.Basic256Sha256,
                MessageSecurityMode.SignAndEncrypt,
                1,
                UserTokenType.Anonymous),
            CreateEndpoint(
                "opc.tcp://127.0.0.1:4840/basic128-highlevel",
                SecurityPolicies.Basic128Rsa15,
                MessageSecurityMode.SignAndEncrypt,
                10,
                UserTokenType.Anonymous));

        Assert.Equal(SecurityPolicies.Basic128Rsa15, selected.SecurityPolicyUri);
        Assert.Equal(10, selected.SecurityLevel);
    }

    [Fact]
    public void SelectEndpointDescription_filters_by_explicit_message_security_mode()
    {
        var selected = InvokeSelectEndpointDescription(
            new OpcUaClientOptions
            {
                ServerUrl = "opc.tcp://127.0.0.1:4840",
                ApplicationName = "UnitTests",
                PreferredMessageSecurityMode = MessageSecurityMode.Sign
            },
            CreateEndpoint(
                "opc.tcp://127.0.0.1:4840/sign-and-encrypt",
                SecurityPolicies.Basic256Sha256,
                MessageSecurityMode.SignAndEncrypt,
                10,
                UserTokenType.Anonymous),
            CreateEndpoint(
                "opc.tcp://127.0.0.1:4840/sign",
                SecurityPolicies.Basic256,
                MessageSecurityMode.Sign,
                5,
                UserTokenType.Anonymous));

        Assert.Equal(MessageSecurityMode.Sign, selected.SecurityMode);
        Assert.Equal(SecurityPolicies.Basic256, selected.SecurityPolicyUri);
    }

    [Fact]
    public void SelectEndpointDescription_requires_both_explicit_policy_and_mode_when_both_are_set()
    {
        var selected = InvokeSelectEndpointDescription(
            new OpcUaClientOptions
            {
                ServerUrl = "opc.tcp://127.0.0.1:4840",
                ApplicationName = "UnitTests",
                PreferredSecurityPolicyUri = SecurityPolicies.Basic128Rsa15,
                PreferredMessageSecurityMode = MessageSecurityMode.SignAndEncrypt
            },
            CreateEndpoint(
                "opc.tcp://127.0.0.1:4840/basic128-sign",
                SecurityPolicies.Basic128Rsa15,
                MessageSecurityMode.Sign,
                10,
                UserTokenType.Anonymous),
            CreateEndpoint(
                "opc.tcp://127.0.0.1:4840/basic128-signandencrypt",
                SecurityPolicies.Basic128Rsa15,
                MessageSecurityMode.SignAndEncrypt,
                1,
                UserTokenType.Anonymous));

        Assert.Equal(SecurityPolicies.Basic128Rsa15, selected.SecurityPolicyUri);
        Assert.Equal(MessageSecurityMode.SignAndEncrypt, selected.SecurityMode);
    }

    [Fact]
    public void SelectEndpointDescription_throws_clear_error_when_explicit_selection_has_no_match()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            InvokeSelectEndpointDescription(
                new OpcUaClientOptions
                {
                    ServerUrl = "opc.tcp://127.0.0.1:4840",
                    ApplicationName = "UnitTests",
                    PreferredSecurityPolicyUri = SecurityPolicies.Basic128Rsa15,
                    PreferredMessageSecurityMode = MessageSecurityMode.SignAndEncrypt
                },
                CreateEndpoint(
                    "opc.tcp://127.0.0.1:4840/basic256sha256",
                    SecurityPolicies.Basic256Sha256,
                    MessageSecurityMode.SignAndEncrypt,
                    10,
                    UserTokenType.Anonymous)));

        Assert.Contains(SecurityPolicies.Basic128Rsa15, exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(MessageSecurityMode.SignAndEncrypt), exception.Message, StringComparison.Ordinal);
        Assert.Contains("Available endpoints", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SelectEndpointDescription_keeps_basic128rsa15_in_automatic_candidates()
    {
        var selected = InvokeSelectEndpointDescription(
            new OpcUaClientOptions
            {
                ServerUrl = "opc.tcp://127.0.0.1:4840",
                ApplicationName = "UnitTests",
                UseSecurity = true
            },
            CreateEndpoint(
                "opc.tcp://127.0.0.1:4840/basic128",
                SecurityPolicies.Basic128Rsa15,
                MessageSecurityMode.SignAndEncrypt,
                5,
                UserTokenType.Anonymous),
            CreateEndpoint(
                "opc.tcp://127.0.0.1:4840/none",
                SecurityPolicies.None,
                MessageSecurityMode.None,
                1,
                UserTokenType.Anonymous));

        Assert.Equal(SecurityPolicies.Basic128Rsa15, selected.SecurityPolicyUri);
        Assert.Equal(MessageSecurityMode.SignAndEncrypt, selected.SecurityMode);
    }

    [Fact]
    public void SelectEndpointDescription_only_considers_endpoints_that_support_the_requested_identity()
    {
        var selected = InvokeSelectEndpointDescription(
            new OpcUaClientOptions
            {
                ServerUrl = "opc.tcp://127.0.0.1:4840",
                ApplicationName = "UnitTests",
                UserName = "opc-user",
                Password = "secret",
                UseSecurity = true
            },
            CreateEndpoint(
                "opc.tcp://127.0.0.1:4840/highest-anonymous",
                SecurityPolicies.Basic256Sha256,
                MessageSecurityMode.SignAndEncrypt,
                20,
                UserTokenType.Anonymous),
            CreateEndpoint(
                "opc.tcp://127.0.0.1:4840/username",
                SecurityPolicies.Basic128Rsa15,
                MessageSecurityMode.SignAndEncrypt,
                5,
                UserTokenType.UserName));

        Assert.Equal("opc.tcp://127.0.0.1:4840/username", selected.EndpointUrl);
        Assert.Equal(SecurityPolicies.Basic128Rsa15, selected.SecurityPolicyUri);
    }

    private static EndpointDescription InvokeSelectEndpointDescription(
        OpcUaClientOptions options,
        params EndpointDescription[] endpoints)
    {
        var endpointCollection = new EndpointDescriptionCollection(endpoints);
        return (EndpointDescription)ReflectionTestHelpers.InvokePrivateStatic(
            typeof(OpcUaClientFactory),
            "SelectEndpointDescription",
            endpointCollection,
            options)!;
    }

    private static EndpointDescription CreateEndpoint(
        string endpointUrl,
        string securityPolicyUri,
        MessageSecurityMode securityMode,
        byte securityLevel,
        params UserTokenType[] supportedTokenTypes)
    {
        var endpoint = new EndpointDescription
        {
            EndpointUrl = endpointUrl,
            SecurityPolicyUri = securityPolicyUri,
            SecurityMode = securityMode,
            SecurityLevel = securityLevel,
            UserIdentityTokens = new UserTokenPolicyCollection()
        };

        foreach (var supportedTokenType in supportedTokenTypes)
        {
            endpoint.UserIdentityTokens.Add(new UserTokenPolicy
            {
                TokenType = supportedTokenType
            });
        }

        return endpoint;
    }
}
