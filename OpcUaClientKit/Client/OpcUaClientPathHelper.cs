namespace OpcUaClientKit;

internal static class OpcUaClientPathHelper
{
    public static string ResolveEffectiveApplicationName(string applicationName, string? deviceId)
    {
        return string.IsNullOrWhiteSpace(deviceId)
            ? applicationName
            : $"{applicationName}-{deviceId}";
    }

    public static string ResolveDefaultPkiRootPath(string applicationName, string? deviceId)
    {
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var segments = new List<string>
        {
            localApplicationData,
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

    public static string SanitizePathSegment(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(ch => invalidCharacters.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "OpcUaClient" : sanitized;
    }
}

