using Opc.Ua.Client;

namespace OpcUaClientKit;

internal sealed class OpcUaSubscriptionHandle : IOpcUaSubscription
{
    private readonly object _registrationsLock = new();
    private readonly Func<OpcUaSubscriptionHandle, IReadOnlyList<OpcUaSubscriptionItemDefinition>, CancellationToken, Task> _addNodesAsync;
    private readonly Func<OpcUaSubscriptionHandle, IReadOnlyList<string>, CancellationToken, Task> _removeNodesAsync;
    private readonly Func<OpcUaSubscriptionHandle, CancellationToken, Task> _unsubscribeAsync;
    private readonly OpcUaSubscriptionState _state;
    private readonly Dictionary<string, OpcUaMonitoredItemRegistration> _registrations;

    public OpcUaSubscriptionHandle(
        string name,
        Subscription subscription,
        IReadOnlyList<OpcUaMonitoredItemRegistration> monitoredItems,
        OpcUaSubscriptionState state,
        Func<OpcUaSubscriptionHandle, IReadOnlyList<OpcUaSubscriptionItemDefinition>, CancellationToken, Task> addNodesAsync,
        Func<OpcUaSubscriptionHandle, IReadOnlyList<string>, CancellationToken, Task> removeNodesAsync,
        Func<OpcUaSubscriptionHandle, CancellationToken, Task> unsubscribeAsync)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
        if (monitoredItems == null)
        {
            throw new ArgumentNullException(nameof(monitoredItems));
        }

        _registrations = monitoredItems.ToDictionary(static item => item.NodeId, StringComparer.Ordinal);
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _addNodesAsync = addNodesAsync ?? throw new ArgumentNullException(nameof(addNodesAsync));
        _removeNodesAsync = removeNodesAsync ?? throw new ArgumentNullException(nameof(removeNodesAsync));
        _unsubscribeAsync = unsubscribeAsync ?? throw new ArgumentNullException(nameof(unsubscribeAsync));
    }

    public string Name { get; }

    public bool IsActive => _state.IsActive;

    public IReadOnlyList<string> NodeIds => GetNodeIdsSnapshot();

    internal Subscription Subscription { get; }

    internal OpcUaSubscriptionState State => _state;

    public Task AddNodeAsync(
        string nodeId,
        Action<OpcUaValueChangeNotification> onChanged,
        CancellationToken ct = default)
    {
        return AddNodeAsync(new OpcUaNode(nodeId), onChanged, ct);
    }

    public Task AddNodeAsync(
        OpcUaNode node,
        Action<OpcUaValueChangeNotification> onChanged,
        CancellationToken ct = default)
    {
        return AddNodeAsync(node, onChanged, static _ => { }, ct);
    }

    public Task AddNodeAsync(
        string nodeId,
        Action<OpcUaValueChangeNotification> onChanged,
        Action<OpcUaMonitoredItemOptions> configure,
        CancellationToken ct = default)
    {
        return AddNodeAsync(new OpcUaNode(nodeId), onChanged, configure, ct);
    }

    public Task AddNodeAsync(
        OpcUaNode node,
        Action<OpcUaValueChangeNotification> onChanged,
        Action<OpcUaMonitoredItemOptions> configure,
        CancellationToken ct = default)
    {
        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (onChanged == null)
        {
            throw new ArgumentNullException(nameof(onChanged));
        }

        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var options = new OpcUaMonitoredItemOptions();
        configure(options);

        return _addNodesAsync(
            this,
            new[]
            {
                new OpcUaSubscriptionItemDefinition(node, onChanged, options)
            },
            ct);
    }

    public Task AddNodesAsync(
        IEnumerable<OpcUaSubscriptionNodeDefinition> nodes,
        CancellationToken ct = default)
    {
        if (nodes == null)
        {
            throw new ArgumentNullException(nameof(nodes));
        }

        var definitions = nodes.Select(OpcUaSubscriptionItemDefinition.FromPublicDefinition).ToList();
        return _addNodesAsync(this, definitions, ct);
    }

    public Task RemoveNodeAsync(string nodeId, CancellationToken ct = default)
    {
        return RemoveNodesAsync(new[] { nodeId }, ct);
    }

    public Task RemoveNodesAsync(IEnumerable<string> nodeIds, CancellationToken ct = default)
    {
        if (nodeIds == null)
        {
            throw new ArgumentNullException(nameof(nodeIds));
        }

        return _removeNodesAsync(this, nodeIds.ToList(), ct);
    }

    public Task UnsubscribeAsync(CancellationToken ct = default)
    {
        return _unsubscribeAsync(this, ct);
    }

    public ValueTask DisposeAsync()
    {
        return new ValueTask(UnsubscribeAsync());
    }

    internal bool TryDeactivate()
    {
        return _state.TryDeactivate();
    }

    internal void EnsureActive()
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("The OPC UA subscription is no longer active.");
        }
    }

    internal bool ContainsNode(string nodeId)
    {
        lock (_registrationsLock)
        {
            return _registrations.ContainsKey(nodeId);
        }
    }

    internal IReadOnlyList<OpcUaMonitoredItemRegistration> GetRegistrationsSnapshot()
    {
        lock (_registrationsLock)
        {
            return _registrations.Values.ToList();
        }
    }

    internal IReadOnlyList<OpcUaMonitoredItemRegistration> GetRegistrationsOrThrow(
        IReadOnlyList<string> nodeIds)
    {
        var registrations = new List<OpcUaMonitoredItemRegistration>(nodeIds.Count);

        lock (_registrationsLock)
        {
            foreach (var nodeId in nodeIds)
            {
                if (!_registrations.TryGetValue(nodeId, out var registration))
                {
                    throw new InvalidOperationException(
                        $"Node '{nodeId}' is not part of subscription '{Name}'.");
                }

                registrations.Add(registration);
            }
        }

        return registrations;
    }

    internal void AddRegistrations(IReadOnlyList<OpcUaMonitoredItemRegistration> registrations)
    {
        lock (_registrationsLock)
        {
            foreach (var registration in registrations)
            {
                _registrations.Add(registration.NodeId, registration);
            }
        }
    }

    internal void RemoveRegistrations(IReadOnlyList<string> nodeIds)
    {
        lock (_registrationsLock)
        {
            foreach (var nodeId in nodeIds)
            {
                _registrations.Remove(nodeId);
            }
        }
    }

    internal void DetachHandlers()
    {
        DetachHandlers(GetRegistrationsSnapshot());
    }

    internal static void DetachHandlers(IReadOnlyList<OpcUaMonitoredItemRegistration> registrations)
    {
        foreach (var registration in registrations)
        {
            registration.MonitoredItem.Notification -= registration.Handler;
        }
    }

    private IReadOnlyList<string> GetNodeIdsSnapshot()
    {
        lock (_registrationsLock)
        {
            return _registrations.Keys.ToList().AsReadOnly();
        }
    }
}

internal sealed class OpcUaMonitoredItemRegistration
{
    public OpcUaMonitoredItemRegistration(
        string nodeId,
        string? displayName,
        Action<OpcUaValueChangeNotification> onChanged,
        OpcUaMonitoredItemOptions options,
        MonitoredItem monitoredItem,
        MonitoredItemNotificationEventHandler handler)
    {
        NodeId = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
        DisplayName = string.IsNullOrWhiteSpace(displayName)
            ? null
            : displayName.Trim();
        OnChanged = onChanged ?? throw new ArgumentNullException(nameof(onChanged));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        MonitoredItem = monitoredItem ?? throw new ArgumentNullException(nameof(monitoredItem));
        Handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public string NodeId { get; }

    public string? DisplayName { get; }

    public Action<OpcUaValueChangeNotification> OnChanged { get; }

    public OpcUaMonitoredItemOptions Options { get; }

    public MonitoredItem MonitoredItem { get; }

    public MonitoredItemNotificationEventHandler Handler { get; }
}

internal sealed class OpcUaSubscriptionState
{
    private int _isActive = 1;

    public bool IsActive => Volatile.Read(ref _isActive) == 1;

    public bool TryDeactivate()
    {
        return Interlocked.Exchange(ref _isActive, 0) == 1;
    }
}
