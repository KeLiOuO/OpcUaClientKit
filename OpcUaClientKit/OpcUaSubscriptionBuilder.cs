namespace OpcUaClientKit;

public sealed class OpcUaSubscriptionBuilder
{
    private readonly OpcUaClient _client;
    private string? _name;
    private int _publishingInterval = 1000;
    private uint _keepAliveCount = 10;
    private uint _lifetimeCount = 60;
    private uint _maxNotificationsPerPublish;
    private byte _priority;
    private bool _publishingEnabled = true;

    internal OpcUaSubscriptionBuilder(OpcUaClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public OpcUaSubscriptionBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public OpcUaSubscriptionBuilder WithPublishingInterval(int milliseconds)
    {
        _publishingInterval = milliseconds;
        return this;
    }

    public OpcUaSubscriptionBuilder WithKeepAliveCount(uint keepAliveCount)
    {
        _keepAliveCount = keepAliveCount;
        return this;
    }

    public OpcUaSubscriptionBuilder WithLifetimeCount(uint lifetimeCount)
    {
        _lifetimeCount = lifetimeCount;
        return this;
    }

    public OpcUaSubscriptionBuilder WithMaxNotificationsPerPublish(uint maxNotificationsPerPublish)
    {
        _maxNotificationsPerPublish = maxNotificationsPerPublish;
        return this;
    }

    public OpcUaSubscriptionBuilder WithPriority(byte priority)
    {
        _priority = priority;
        return this;
    }

    public OpcUaSubscriptionBuilder WithPublishingEnabled(bool publishingEnabled)
    {
        _publishingEnabled = publishingEnabled;
        return this;
    }

    public Task<IOpcUaSubscription> BuildAsync(CancellationToken ct = default)
    {
        return _client.BuildSubscriptionAsync(CreateRequest(), ct);
    }

    private OpcUaSubscriptionBuildRequest CreateRequest()
    {
        return new OpcUaSubscriptionBuildRequest(
            _name,
            _publishingInterval,
            _keepAliveCount,
            _lifetimeCount,
            _maxNotificationsPerPublish,
            _priority,
            _publishingEnabled);
    }
}

internal sealed class OpcUaSubscriptionBuildRequest
{
    public OpcUaSubscriptionBuildRequest(
        string? name,
        int publishingInterval,
        uint keepAliveCount,
        uint lifetimeCount,
        uint maxNotificationsPerPublish,
        byte priority,
        bool publishingEnabled)
    {
        Name = name;
        PublishingInterval = publishingInterval;
        KeepAliveCount = keepAliveCount;
        LifetimeCount = lifetimeCount;
        MaxNotificationsPerPublish = maxNotificationsPerPublish;
        Priority = priority;
        PublishingEnabled = publishingEnabled;
    }

    public string? Name { get; }

    public int PublishingInterval { get; }

    public uint KeepAliveCount { get; }

    public uint LifetimeCount { get; }

    public uint MaxNotificationsPerPublish { get; }

    public byte Priority { get; }

    public bool PublishingEnabled { get; }
}
