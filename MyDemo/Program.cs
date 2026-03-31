using OpcUaClientKit;

const string serverUrl = "opc.tcp://127.0.0.1:4840";
const string applicationName = "Mydemo";
const string userName = "OpcUaClient";
const string password = "123456";

var levelNode = new OpcUaNode("ns=6;s=MyLevel", "Level");
var dbw2Node = new OpcUaNode("ns=3;s=/Plc/DB66.DBW2", "DBW2");
var dbw0Node = new OpcUaNode("ns=3;s=/Plc/DB66.DBW0", "DBW0");

IOpcUaClientFactory factory = new OpcUaClientFactory();

try
{
    var client = factory.Create(
        serverUrl,
        applicationName,
        "device-a",
        userName,
        password,
        autoAcceptUntrustedServerCertificate: true,
        useSecurity: true,
        sessionTimeout: 60000);

    await client.ConnectAsync();
    await methodCall(client);
    double level = await client.ReadNodeAsync<double>(levelNode);
    Console.WriteLine($"{levelNode.DisplayName} : {level}");

    await client.WriteNodesAsync(new Dictionary<OpcUaNode, object?>
    {
        [dbw2Node] = (short)2,
        [dbw0Node] = (short)5
    });

    var values = await client.ReadNodesAsync(new[] { dbw2Node, dbw0Node });
    foreach (var value in values)
    {
        Console.WriteLine($"{value.Key} : {value.Value}");
    }

    var subscribable = client.AsSubscribable();
    await using var subscription = await subscribable
        .CreateSubscriptionBuilder()
        .WithName("Test")
        .WithPublishingInterval(250)
        .BuildAsync();

    var nodes = new List<OpcUaSubscriptionNodeDefinition>
    {
        new(dbw2Node, t => Console.WriteLine($"{t.DisplayName ?? t.NodeId} : {t.Value}, Good:{t.IsGood}")),
        new(dbw0Node, t => Console.WriteLine($"{t.DisplayName ?? t.NodeId} changed"))
    };

    await subscription.AddNodesAsync(nodes);

    Console.WriteLine("Subscription started. Press Enter to exit.");
    Console.ReadLine();
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
    Console.ReadLine();
}


async Task methodCall(IOpcUaClient client)
{
    var objectNode = new OpcUaNode("ns=6;s=MyDevice");
    var methodNode = new OpcUaNode("ns=6;s=MyMethod");
    var outputs = await client.CallMethodAsync(objectNode, methodNode, ["sin", (double)90]);
    Console.WriteLine($"sin 90 = {outputs[0]}");
}