namespace OpcUaClientKit;

/// <summary>
/// Centralizes client naming and PKI path conventions used across the library.
/// </summary>
internal static class OpcUaClientPathHelper
{
    /// <summary>
    /// Combines the base application name with the optional device id to form the effective client identity.
    /// </summary>
    public static string ResolveEffectiveApplicationName(string applicationName, string? deviceId)
    {
        return string.IsNullOrWhiteSpace(deviceId)
            ? applicationName
            : $"{applicationName}-{deviceId}";
    }

    /// <summary>
    /// Resolves the default PKI root path for the supplied application and optional device id.
    /// </summary>
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

    /// <summary>
    /// Replaces invalid path characters so application and device names can be used safely on disk.
    /// </summary>
    public static string SanitizePathSegment(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(ch => invalidCharacters.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "OpcUaClient" : sanitized;
    }
}

