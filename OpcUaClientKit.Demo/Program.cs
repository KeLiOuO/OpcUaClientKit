using OpcUaClientKit;

const string serverUrl = "opc.tcp://127.0.0.1:4840";
const string applicationName = "OpcUaClientKitDemo";
const string userName = "OpcUaClient";
const string password = "123456";
var levelNode = new OpcUaNode("ns=6;s=MyLevel", "Level");

IOpcUaClientFactory factory = new OpcUaClientFactory();
List<DemoClientRegistration> clients = new List<DemoClientRegistration>();

try
{
    var simpleClient = await factory.CreateConnectedAsync(
        serverUrl,
        applicationName,
        "device-a",
        userName,
        password,
        autoAcceptUntrustedServerCertificate: true,
        useSecurity: true,
        sessionTimeout: 60000);
    clients.Add(new DemoClientRegistration("Simple", "device-a", simpleClient));
    PrintConnected("Simple", serverUrl, applicationName, "device-a");

    var complexClient = await factory
        .CreateBuilder()
        .WithServerUrl(serverUrl)
        .WithApplicationName(applicationName)
        .WithDeviceId("device-b")
        .WithUserNamePassword(userName, password)
        .WithSecurity(true)
        .WithCheckDomain(false)
        .WithSessionTimeout(60000)
        .WithOperationTimeout(30000)
        .WithAutoAcceptUntrustedServerCertificate(true)
        .WithCertificateOptions(options =>
        {
            options.OrganizationName = string.Empty;
            options.AddAppCertToTrustedStore = false;
            options.SendCertificateChain = true;
            options.MinimumKeySize = 2048;
            options.RejectSHA1SignedCertificates = true;
            options.RejectUnknownRevocationStatus = true;
            options.MaxRejectedCertificates = 5;
        })
        .BuildConnectedAsync();
    clients.Add(new DemoClientRegistration("Complex", "device-b", complexClient));
    PrintConnected("Complex", serverUrl, applicationName, "device-b");

    Console.WriteLine($"Connected {clients.Count} OPC UA client(s). Press Enter to disconnect.");
    double value = Convert.ToDouble(await clients[0].Client.ReadNodeAsync(levelNode));
    Console.WriteLine($"device1 read {levelNode.DisplayName} ({levelNode.NodeId}) value is {value}");
    Console.ReadLine();
}
finally
{
    foreach (var registration in clients)
    {
        await registration.Client.DisposeAsync();
    }
}

static void PrintConnected(
    string mode,
    string serverUrl,
    string applicationName,
    string deviceId)
{
    var effectiveApplicationName = string.IsNullOrWhiteSpace(deviceId)
        ? applicationName
        : $"{applicationName}-{deviceId}";
    var pkiRootPath = BuildDefaultPkiRootPath(applicationName, deviceId);

    Console.WriteLine($"[{mode}] {effectiveApplicationName} connected to {serverUrl}");
    Console.WriteLine($"[{mode}] Certificate directory: {pkiRootPath}");
}

static string BuildDefaultPkiRootPath(string applicationName, string? deviceId)
{
    var segments = new List<string>
    {
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OPC Foundation",
        SanitizePathSegment(applicationName)
    };

    if (!string.IsNullOrWhiteSpace(deviceId))
    {
        segments.Add(SanitizePathSegment(deviceId));
    }

    segments.Add("pki");
    return Path.Combine(segments.ToArray());
}

static string SanitizePathSegment(string value)
{
    var invalidCharacters = Path.GetInvalidFileNameChars();
    var sanitized = new string(value.Select(ch => invalidCharacters.Contains(ch) ? '_' : ch).ToArray());
    return string.IsNullOrWhiteSpace(sanitized) ? "OpcUaClient" : sanitized;
}

internal sealed record DemoClientRegistration(
    string Mode,
    string DeviceId,
    IOpcUaClient Client);

