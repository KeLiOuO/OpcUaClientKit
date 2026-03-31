namespace OpcUaClientKit;

/// <summary>
/// Controls the automatic reconnect and subscription restore behavior of the OPC UA client.
/// </summary>
public sealed class OpcUaReconnectOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether automatic reconnect is enabled.
    /// Default is <c>false</c>.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of consecutive reconnect attempts before giving up.
    /// Set to <c>-1</c> for unlimited retries. Default is <c>10</c>.
    /// </summary>
    public int MaxAttempts { get; set; } = 10;

    /// <summary>
    /// Gets or sets the delay before the first reconnect attempt in milliseconds.
    /// Default is <c>1000</c>.
    /// </summary>
    public int InitialDelayMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the maximum reconnect delay in milliseconds.
    /// Default is <c>30000</c>.
    /// </summary>
    public int MaxDelayMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets the multiplier applied to the reconnect delay after each failed attempt.
    /// Default is <c>2.0</c>.
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0d;

    /// <summary>
    /// Gets or sets an optional callback invoked at each stage of the reconnect lifecycle.
    /// </summary>
    public Action<OpcUaReconnectEvent>? ReconnectHandler { get; set; }

    /// <summary>
    /// Creates a deep copy of the current reconnect options.
    /// </summary>
    public OpcUaReconnectOptions Clone()
    {
        return new OpcUaReconnectOptions
        {
            Enabled = Enabled,
            MaxAttempts = MaxAttempts,
            InitialDelayMs = InitialDelayMs,
            MaxDelayMs = MaxDelayMs,
            BackoffMultiplier = BackoffMultiplier,
            ReconnectHandler = ReconnectHandler
        };
    }
}
