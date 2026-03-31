using OpcUaClientKit;

namespace OpcUaClientKit.Demo;

internal static class DemoConsole
{
    public static void PrintSection(string title)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 72));
        Console.WriteLine(title);
        Console.WriteLine(new string('=', 72));
    }

    public static void PrintConnectionSummary(
        string mode,
        DemoSettings settings,
        string deviceId)
    {
        Console.WriteLine($"Mode: {mode}");
        Console.WriteLine($"Server: {settings.ServerUrl}");
        Console.WriteLine($"Application: {settings.ApplicationName}");
        Console.WriteLine($"DeviceId: {deviceId}");
        Console.WriteLine($"Security: {(settings.UseSecurity ? "Enabled" : "Disabled")}");
        Console.WriteLine($"Auto-accept untrusted server certificate: {settings.AutoAcceptUntrustedServerCertificate}");
        Console.WriteLine($"Default PKI path: {BuildDefaultPkiRootPath(settings.ApplicationName, deviceId)}");
    }

    public static string FormatValue(object? value)
    {
        return value switch
        {
            null => "(null)",
            byte[] bytes when bytes.Length == 0 => "(empty)",
            byte[] bytes => Convert.ToHexString(bytes),
            _ => value.ToString() ?? "(null)"
        };
    }

    public static void PrintEvent(
        OpcUaEventNotification notification,
        int previewFieldCount)
    {
        var source = notification.SourceName
            ?? notification.SourceDisplayName
            ?? notification.SourceNodeId
            ?? "UnknownSource";

        Console.WriteLine(
            $"[{notification.Time:HH:mm:ss}] {source} | Message={notification.Message ?? "(null)"} | " +
            $"Severity={notification.Severity} | Active={notification.Active?.ToString() ?? "?"} | " +
            $"Acked={notification.Acked?.ToString() ?? "?"} | Retain={notification.Retain?.ToString() ?? "?"}");

        foreach (var field in notification.SelectedFields.Take(previewFieldCount))
        {
            Console.WriteLine($"  - {field.DisplayName}: {FormatValue(field.Value)}");
        }

        if (notification.SelectedFields.Count > previewFieldCount)
        {
            Console.WriteLine($"  ... {notification.SelectedFields.Count - previewFieldCount} more field(s)");
        }
    }

    private static string BuildDefaultPkiRootPath(string applicationName, string? deviceId)
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

    private static string SanitizePathSegment(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(ch => invalidCharacters.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "OpcUaClient" : sanitized;
    }
}
