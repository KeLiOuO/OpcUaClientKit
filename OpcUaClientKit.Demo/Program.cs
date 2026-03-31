using OpcUaClientKit;
using OpcUaClientKit.Demo;

var settings = DemoSettings.Load();
var scenarios = new IDemoScenario[]
{
    new QuickStartScenario(),
    new ReadWriteScenario(),
    new MethodCallScenario(),
    new DataSubscriptionScenario(),
    new EventSubscriptionScenario(),
    new ReconnectScenario()
};

if (args.Length > 0 &&
    string.Equals(args[0].Trim(), "list", StringComparison.OrdinalIgnoreCase))
{
    PrintScenarioList(scenarios);
    return;
}

var selectedScenario = ResolveScenario(args, scenarios);
if (selectedScenario == null)
{
    selectedScenario = PromptForScenarioSelection(scenarios);
}

if (selectedScenario == null)
{
    Console.WriteLine("No scenario selected. Exiting.");
    return;
}

var context = new DemoContext(settings, new OpcUaClientFactory());

Console.WriteLine("OpcUaClientKit Console Demo");
Console.WriteLine($"Settings file: {Path.Combine(AppContext.BaseDirectory, "DemoSettings.json")}");
Console.WriteLine();

try
{
    await selectedScenario.RunAsync(context, CancellationToken.None);
}
catch (Exception ex)
{
    Console.WriteLine("The demo failed:");
    Console.WriteLine(ex.Message);
    Console.WriteLine(ex);
}

static IDemoScenario? ResolveScenario(string[] args, IEnumerable<IDemoScenario> scenarios)
{
    if (args.Length == 0)
    {
        return null;
    }

    var input = args[0].Trim();
    return scenarios.FirstOrDefault(scenario =>
        string.Equals(scenario.Key, input, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(scenario.Title, input, StringComparison.OrdinalIgnoreCase));
}

static IDemoScenario? PromptForScenarioSelection(IReadOnlyList<IDemoScenario> scenarios)
{
    PrintScenarioList(scenarios);
    Console.WriteLine();
    Console.Write("Select a scenario by number or key: ");
    var input = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(input))
    {
        return null;
    }

    if (int.TryParse(input, out var index) &&
        index >= 1 &&
        index <= scenarios.Count)
    {
        return scenarios[index - 1];
    }

    return scenarios.FirstOrDefault(scenario =>
        string.Equals(scenario.Key, input, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(scenario.Title, input, StringComparison.OrdinalIgnoreCase));
}

static void PrintScenarioList(IEnumerable<IDemoScenario> scenarios)
{
    Console.WriteLine("Available scenarios:");
    var index = 1;
    foreach (var scenario in scenarios)
    {
        Console.WriteLine($"{index}. {scenario.Key} - {scenario.Title}");
        Console.WriteLine($"   {scenario.Description}");
        index++;
    }

    Console.WriteLine();
    Console.WriteLine(
        "You can also run a scenario directly, for example:");
    Console.WriteLine(
        "dotnet run --project .\\OpcUaClientKit.Demo\\OpcUaClientKit.Demo.csproj -- quickstart");
}
