using System.Text.Json;
using OpcUaClientKit;

namespace OpcUaClientKit.RegressionTests.Infrastructure;

internal sealed class OpcUaRegressionTestSettings
{
    public string ServerUrl { get; set; } = "opc.tcp://127.0.0.1:4840";

    public string ApplicationName { get; set; } = "OpcUaClientKitDemo";

    public string DeviceId { get; set; } = "device-a";

    public string UserName { get; set; } = "OpcUaClient";

    public string Password { get; set; } = "123456";

    public bool AutoAcceptUntrustedServerCertificate { get; set; } = true;

    public bool UseSecurity { get; set; } = true;

    public int SessionTimeout { get; set; } = 60000;

    public int WaitTimeoutSeconds { get; set; } = 15;

    public OpcUaRegressionNode LevelNode { get; set; } = new("ns=6;s=MyLevel", "Level");

    public OpcUaRegressionNode WritableNode1 { get; set; } = new("ns=3;s=/Plc/DB66.DBW0", "WritableNode1");

    public OpcUaRegressionNode WritableNode2 { get; set; } = new("ns=3;s=/Plc/DB66.DBW2", "WritableNode2");

    public OpcUaRegressionNode MethodObjectNode { get; set; } = new("ns=6;s=MyDevice", "MyDevice");

    public OpcUaRegressionNode MethodNode { get; set; } = new("ns=6;s=MyMethod", "MyMethod");

    public OpcUaRegressionNode AlarmSourceNode { get; set; } = new("ns=6;s=MyObjectsFolder", "MyObjects");

    public short SingleWriteValue { get; set; } = 1;

    public short BatchWriteValue1 { get; set; } = 1;

    public short BatchWriteValue2 { get; set; } = 3;

    public short SubscriptionWriteValue { get; set; } = 5;

    public string MethodTextArgument { get; set; } = "sin";

    public double MethodNumericArgument { get; set; } = 90d;

    public double MethodExpectedFirstOutput { get; set; } = 1d;

    public double MethodExpectedTolerance { get; set; } = 0.000001d;

    public double EventClearValue { get; set; } = 10d;

    public double EventActiveValue { get; set; } = 120d;

    public string ExpectedEventMessageContains { get; set; } = "Level exceeded";

    public static OpcUaRegressionTestSettings Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "RegressionTestSettings.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                "RegressionTestSettings.json was not found. Make sure the file is copied to the test output directory.",
                path);
        }

        var json = File.ReadAllText(path);
        var settings = JsonSerializer.Deserialize<OpcUaRegressionTestSettings>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (settings == null)
        {
            throw new InvalidOperationException("Failed to deserialize RegressionTestSettings.json.");
        }

        settings.Validate();
        return settings;
    }

    public TimeSpan WaitTimeout => TimeSpan.FromSeconds(Math.Max(1, WaitTimeoutSeconds));

    public IEnumerable<object?> CreateMethodArguments()
    {
        yield return MethodTextArgument;
        yield return MethodNumericArgument;
    }

    private void Validate()
    {
        ValidateRequired(ServerUrl, nameof(ServerUrl));
        ValidateRequired(ApplicationName, nameof(ApplicationName));
        ValidateRequired(DeviceId, nameof(DeviceId));
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

        if (WaitTimeoutSeconds <= 0)
        {
            throw new InvalidOperationException($"{nameof(WaitTimeoutSeconds)} must be greater than 0.");
        }
    }

    private static void ValidateNode(OpcUaRegressionNode node, string name)
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

internal sealed class OpcUaRegressionNode
{
    public OpcUaRegressionNode()
    {
    }

    public OpcUaRegressionNode(string nodeId, string? displayName = null)
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
