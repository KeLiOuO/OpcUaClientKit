using System.Text.Json;
using Opc.Ua;
using OpcUaClientKit;

namespace OpcUaClientKit.Demo;

internal sealed class DemoSettings
{
    public string ServerUrl { get; set; } = "opc.tcp://127.0.0.1:4840";

    public string ApplicationName { get; set; } = "OpcUaClientKitDemo";

    public string SimpleDeviceId { get; set; } = "demo-simple";

    public string AdvancedDeviceId { get; set; } = "demo-advanced";

    public string UserName { get; set; } = "OpcUaClient";

    public string Password { get; set; } = "123456";

    public bool AutoAcceptUntrustedServerCertificate { get; set; } = true;

    public bool UseSecurity { get; set; } = true;

    public string? PreferredSecurityPolicyUri { get; set; }

    public string? PreferredMessageSecurityMode { get; set; }

    public int SessionTimeout { get; set; } = 60000;

    public int OperationTimeout { get; set; } = 30000;

    public bool CheckDomain { get; set; }

    public int WaitTimeoutSeconds { get; set; } = 15;

    public bool ConditionRefreshOnStart { get; set; } = true;

    public bool IgnoreSuppressedOrShelved { get; set; }

    public string EventSelectClauseMode { get; set; } = nameof(OpcUaEventSelectClauseMode.Dynamic);

    public int EventFieldPreviewCount { get; set; } = 12;

    public int ReconnectMaxAttempts { get; set; } = -1;

    public bool ReconnectImmediatelyOnFirstFailure { get; set; } = true;

    public int ReconnectInitialDelayMs { get; set; } = 1000;

    public int ReconnectMaxDelayMs { get; set; } = 10000;

    public double ReconnectBackoffMultiplier { get; set; } = 2.0d;

    public DemoNode LevelNode { get; set; } = new("ns=6;s=MyLevel", "Level");

    public DemoNode WritableNode1 { get; set; } = new("ns=3;s=/Plc/DB66.DBW0", "WritableNode1");

    public DemoNode WritableNode2 { get; set; } = new("ns=3;s=/Plc/DB66.DBW2", "WritableNode2");

    public DemoNode MethodObjectNode { get; set; } = new("ns=6;s=MyDevice", "MyDevice");

    public DemoNode MethodNode { get; set; } = new("ns=6;s=MyMethod", "MyMethod");

    public DemoNode AlarmSourceNode { get; set; } = new("ns=6;s=MyObjectsFolder", "MyObjects");

    public short SingleWriteValue { get; set; } = 1;

    public short BatchWriteValue1 { get; set; } = 1;

    public short BatchWriteValue2 { get; set; } = 3;

    public short SubscriptionWriteValue { get; set; } = 5;

    public double EventClearValue { get; set; } = 10d;

    public double EventActiveValue { get; set; } = 120d;

    public string MethodTextArgument { get; set; } = "sin";

    public double MethodNumericArgument { get; set; } = 90d;

    public double MethodExpectedFirstOutput { get; set; } = 1d;

    public double MethodExpectedTolerance { get; set; } = 0.000001d;

    public TimeSpan WaitTimeout => TimeSpan.FromSeconds(Math.Max(1, WaitTimeoutSeconds));

    public MessageSecurityMode? ParsedPreferredMessageSecurityMode
    {
        get
        {
            if (string.IsNullOrWhiteSpace(PreferredMessageSecurityMode))
            {
                return null;
            }

            return Enum.TryParse<MessageSecurityMode>(
                PreferredMessageSecurityMode,
                ignoreCase: true,
                out var mode)
                ? mode
                : throw new InvalidOperationException(
                    $"{nameof(PreferredMessageSecurityMode)} must be a valid {nameof(MessageSecurityMode)} value.");
        }
    }

    public OpcUaEventSelectClauseMode ParsedEventSelectClauseMode =>
        Enum.TryParse<OpcUaEventSelectClauseMode>(EventSelectClauseMode, ignoreCase: true, out var mode)
            ? mode
            : OpcUaEventSelectClauseMode.Dynamic;

    public IEnumerable<object?> CreateMethodArguments()
    {
        yield return MethodTextArgument;
        yield return MethodNumericArgument;
    }

    public static DemoSettings Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "DemoSettings.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                "DemoSettings.json was not found. Make sure the file is copied to the demo output directory.",
                path);
        }

        var json = File.ReadAllText(path);
        var settings = JsonSerializer.Deserialize<DemoSettings>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (settings == null)
        {
            throw new InvalidOperationException("Failed to deserialize DemoSettings.json.");
        }

        settings.Validate();
        return settings;
    }

    private void Validate()
    {
        ValidateRequired(ServerUrl, nameof(ServerUrl));
        ValidateRequired(ApplicationName, nameof(ApplicationName));
        ValidateRequired(SimpleDeviceId, nameof(SimpleDeviceId));
        ValidateRequired(AdvancedDeviceId, nameof(AdvancedDeviceId));
        ValidateRequired(UserName, nameof(UserName));
        ValidateRequired(Password, nameof(Password));
        ValidateNode(LevelNode, nameof(LevelNode));
        ValidateNode(WritableNode1, nameof(WritableNode1));
        ValidateNode(WritableNode2, nameof(WritableNode2));
        ValidateNode(MethodObjectNode, nameof(MethodObjectNode));
        ValidateNode(MethodNode, nameof(MethodNode));
        ValidateNode(AlarmSourceNode, nameof(AlarmSourceNode));

        if (SessionTimeout <= 0)
        {
            throw new InvalidOperationException($"{nameof(SessionTimeout)} must be greater than 0.");
        }

        if (OperationTimeout <= 0)
        {
            throw new InvalidOperationException($"{nameof(OperationTimeout)} must be greater than 0.");
        }

        _ = ParsedPreferredMessageSecurityMode;

        if (WaitTimeoutSeconds <= 0)
        {
            throw new InvalidOperationException($"{nameof(WaitTimeoutSeconds)} must be greater than 0.");
        }

        if (EventFieldPreviewCount < 1)
        {
            throw new InvalidOperationException($"{nameof(EventFieldPreviewCount)} must be greater than 0.");
        }

        if (ReconnectMaxAttempts < -1 || ReconnectMaxAttempts == 0)
        {
            throw new InvalidOperationException(
                $"{nameof(ReconnectMaxAttempts)} must be -1 or greater than 0.");
        }

        if (ReconnectInitialDelayMs <= 0)
        {
            throw new InvalidOperationException(
                $"{nameof(ReconnectInitialDelayMs)} must be greater than 0.");
        }

        if (ReconnectMaxDelayMs <= 0)
        {
            throw new InvalidOperationException(
                $"{nameof(ReconnectMaxDelayMs)} must be greater than 0.");
        }

        if (ReconnectBackoffMultiplier < 1d)
        {
            throw new InvalidOperationException(
                $"{nameof(ReconnectBackoffMultiplier)} must be greater than or equal to 1.");
        }
    }

    private static void ValidateNode(DemoNode node, string name)
    {
        if (node == null)
        {
            throw new InvalidOperationException($"{name} is required.");
        }

        ValidateRequired(node.NodeId, $"{name}.NodeId");
    }

    private static void ValidateRequired(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{name} is required.");
        }
    }
}

internal sealed class DemoNode
{
    public DemoNode()
    {
    }

    public DemoNode(string nodeId, string? displayName = null)
    {
        NodeId = nodeId;
        DisplayName = displayName;
    }

    public string NodeId { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public OpcUaNode ToOpcUaNode()
    {
        return new OpcUaNode(NodeId, DisplayName);
    }
}
