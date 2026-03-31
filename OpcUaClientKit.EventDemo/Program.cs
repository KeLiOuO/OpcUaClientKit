using OpcUaClientKit;

const string serverUrl = "opc.tcp://127.0.0.1:4840";
const string applicationName = "OpcUaClientKitEventDemo";
const string userName = "OpcUaClient";
const string password = "123456";
var useFixedSelectClauseMode = false;

// Replace this with the actual object/view node that exposes alarm events in your server.
var alarmSourceNode = new OpcUaNode("ns=6;s=MyObjectsFolder", "MyObjects");

IOpcUaClientFactory factory = new OpcUaClientFactory();

await using var client = factory.Create(
    serverUrl,
    applicationName,
    "event-device",
    userName,
    password,
    autoAcceptUntrustedServerCertificate: true,
    useSecurity: true,
    sessionTimeout: 60000);

try
{
    await client.ConnectAsync();

    var eventClient = client.AsEventSubscribable();
    var eventBuilder = eventClient
        .CreateEventSubscriptionBuilder()
        .WithName("alarm-events")
        .WithPublishingInterval(500)
        .WithConditionRefreshOnStart(true);
    //.WithIgnoreSuppressedOrShelved(true);

    if (useFixedSelectClauseMode)
    {
        eventBuilder.WithSelectClauseMode(OpcUaEventSelectClauseMode.Fixed);
    }

    await using var subscription = await eventBuilder.BuildAsync(OnAlarmEvent);

    await subscription.AddSourceAsync(alarmSourceNode);

    Console.WriteLine($"Connected to {serverUrl}");
    Console.WriteLine($"Listening for alarm events from {alarmSourceNode.DisplayName} ({alarmSourceNode.NodeId})");
    Console.WriteLine(
        $"SelectClauses mode: {(useFixedSelectClauseMode ? OpcUaEventSelectClauseMode.Fixed : OpcUaEventSelectClauseMode.Dynamic)}");
    Console.WriteLine("Press R to refresh retained alarms, Enter to exit.");

    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter)
        {
            break;
        }

        if (key.Key == ConsoleKey.R)
        {
            await subscription.RefreshAsync();
            Console.WriteLine("ConditionRefreshAsync sent.");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine("Event demo failed:");
    Console.WriteLine(ex.Message);
}

static void OnAlarmEvent(OpcUaEventNotification notification)
{
    var source = notification.SourceName
        ?? notification.SourceDisplayName
        ?? notification.SourceNodeId
        ?? "UnknownSource";

    Console.WriteLine(
        $"[{notification.Time:HH:mm:ss}] {source} | Message={notification.Message ?? "(null)"} | " +
        $"Severity={notification.Severity} | Active={notification.Active?.ToString() ?? "?"} | " +
        $"Acked={notification.Acked?.ToString() ?? "?"} | Retain={notification.Retain?.ToString() ?? "?"}");

    foreach (var field in notification.SelectedFields)
    {
        Console.WriteLine($"  - {field.DisplayName}: {FormatValue(field.Value)}");
    }
}

static string FormatValue(object? value)
{
    return value switch
    {
        null => "(null)",
        byte[] bytes when bytes.Length == 0 => "(empty)",
        byte[] bytes => Convert.ToHexString(bytes),
        _ => value.ToString() ?? "(null)"
    };
}
