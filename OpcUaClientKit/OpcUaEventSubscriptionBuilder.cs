using Opc.Ua;

namespace OpcUaClientKit;

public sealed class OpcUaEventSubscriptionBuilder
{
    private readonly OpcUaClient _client;
    private string? _name;
    private int _publishingInterval = 1000;
    private uint _keepAliveCount = 10;
    private uint _lifetimeCount = 60;
    private uint _maxNotificationsPerPublish;
    private byte _priority;
    private bool _publishingEnabled = true;
    private OpcUaNode? _eventTypeNode;
    private ushort? _severityAtLeast;
    private uint _queueSize = 1000;
    private bool _discardOldest = true;
    private bool _conditionRefreshOnStart = true;
    private OpcUaEventSelectClauseMode _selectClauseMode = OpcUaEventSelectClauseMode.Dynamic;
    private bool _ignoreSuppressedOrShelved;

    internal OpcUaEventSubscriptionBuilder(OpcUaClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public OpcUaEventSubscriptionBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public OpcUaEventSubscriptionBuilder WithPublishingInterval(int milliseconds)
    {
        _publishingInterval = milliseconds;
        return this;
    }

    public OpcUaEventSubscriptionBuilder WithKeepAliveCount(uint keepAliveCount)
    {
        _keepAliveCount = keepAliveCount;
        return this;
    }

    public OpcUaEventSubscriptionBuilder WithLifetimeCount(uint lifetimeCount)
    {
        _lifetimeCount = lifetimeCount;
        return this;
    }

    public OpcUaEventSubscriptionBuilder WithMaxNotificationsPerPublish(uint maxNotificationsPerPublish)
    {
        _maxNotificationsPerPublish = maxNotificationsPerPublish;
        return this;
    }

    public OpcUaEventSubscriptionBuilder WithPriority(byte priority)
    {
        _priority = priority;
        return this;
    }

    public OpcUaEventSubscriptionBuilder WithPublishingEnabled(bool publishingEnabled)
    {
        _publishingEnabled = publishingEnabled;
        return this;
    }

    public OpcUaEventSubscriptionBuilder WithEventType(string eventTypeNodeId)
    {
        _eventTypeNode = new OpcUaNode(eventTypeNodeId);
        return this;
    }

    public OpcUaEventSubscriptionBuilder WithEventType(OpcUaNode eventTypeNode)
    {
        _eventTypeNode = eventTypeNode ?? throw new ArgumentNullException(nameof(eventTypeNode));
        return this;
    }

    public OpcUaEventSubscriptionBuilder WithSeverityAtLeast(ushort severity)
    {
        _severityAtLeast = severity;
        return this;
    }

    public OpcUaEventSubscriptionBuilder WithQueueSize(uint queueSize)
    {
        _queueSize = queueSize;
        return this;
    }

    public OpcUaEventSubscriptionBuilder WithDiscardOldest(bool discardOldest)
    {
        _discardOldest = discardOldest;
        return this;
    }

    public OpcUaEventSubscriptionBuilder WithConditionRefreshOnStart(bool enabled = true)
    {
        _conditionRefreshOnStart = enabled;
        return this;
    }

    public OpcUaEventSubscriptionBuilder WithSelectClauseMode(OpcUaEventSelectClauseMode mode)
    {
        _selectClauseMode = mode;
        return this;
    }

    public OpcUaEventSubscriptionBuilder WithIgnoreSuppressedOrShelved(bool enabled = true)
    {
        _ignoreSuppressedOrShelved = enabled;
        return this;
    }

    public Task<IOpcUaEventSubscription> BuildAsync(
        Action<OpcUaEventNotification> onEvent,
        CancellationToken ct = default)
    {
        if (onEvent == null)
        {
            throw new ArgumentNullException(nameof(onEvent));
        }

        return _client.BuildEventSubscriptionAsync(CreateRequest(onEvent), ct);
    }

    private OpcUaEventSubscriptionBuildRequest CreateRequest(Action<OpcUaEventNotification> onEvent)
    {
        return new OpcUaEventSubscriptionBuildRequest(
            _name,
            _publishingInterval,
            _keepAliveCount,
            _lifetimeCount,
            _maxNotificationsPerPublish,
            _priority,
            _publishingEnabled,
            _eventTypeNode ?? new OpcUaNode(ObjectTypeIds.AlarmConditionType.ToString()),
            _severityAtLeast,
            _queueSize,
            _discardOldest,
            _conditionRefreshOnStart,
            _selectClauseMode,
            _ignoreSuppressedOrShelved,
            onEvent);
    }
}

internal sealed class OpcUaEventSubscriptionBuildRequest
{
    public OpcUaEventSubscriptionBuildRequest(
        string? name,
        int publishingInterval,
        uint keepAliveCount,
        uint lifetimeCount,
        uint maxNotificationsPerPublish,
        byte priority,
        bool publishingEnabled,
        OpcUaNode eventTypeNode,
        ushort? severityAtLeast,
        uint queueSize,
        bool discardOldest,
        bool conditionRefreshOnStart,
        OpcUaEventSelectClauseMode selectClauseMode,
        bool ignoreSuppressedOrShelved,
        Action<OpcUaEventNotification> onEvent)
    {
        Name = name;
        PublishingInterval = publishingInterval;
        KeepAliveCount = keepAliveCount;
        LifetimeCount = lifetimeCount;
        MaxNotificationsPerPublish = maxNotificationsPerPublish;
        Priority = priority;
        PublishingEnabled = publishingEnabled;
        EventTypeNode = eventTypeNode ?? throw new ArgumentNullException(nameof(eventTypeNode));
        SeverityAtLeast = severityAtLeast;
        QueueSize = queueSize;
        DiscardOldest = discardOldest;
        ConditionRefreshOnStart = conditionRefreshOnStart;
        SelectClauseMode = selectClauseMode;
        IgnoreSuppressedOrShelved = ignoreSuppressedOrShelved;
        OnEvent = onEvent ?? throw new ArgumentNullException(nameof(onEvent));
    }

    public string? Name { get; }

    public int PublishingInterval { get; }

    public uint KeepAliveCount { get; }

    public uint LifetimeCount { get; }

    public uint MaxNotificationsPerPublish { get; }

    public byte Priority { get; }

    public bool PublishingEnabled { get; }

    public OpcUaNode EventTypeNode { get; }

    public ushort? SeverityAtLeast { get; }

    public uint QueueSize { get; }

    public bool DiscardOldest { get; }

    public bool ConditionRefreshOnStart { get; }

    public OpcUaEventSelectClauseMode SelectClauseMode { get; }

    public bool IgnoreSuppressedOrShelved { get; }

    public Action<OpcUaEventNotification> OnEvent { get; }
}
