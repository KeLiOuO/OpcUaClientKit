using Opc.Ua.Client;

namespace OpcUaClientKit;

internal sealed class OpcUaEventSubscriptionHandle : IOpcUaEventSubscription
{
    private readonly object _registrationsLock = new();
    private readonly Func<OpcUaEventSubscriptionHandle, IReadOnlyList<OpcUaNode>, CancellationToken, Task> _addSourcesAsync;
    private readonly Func<OpcUaEventSubscriptionHandle, IReadOnlyList<string>, CancellationToken, Task> _removeSourcesAsync;
    private readonly Func<OpcUaEventSubscriptionHandle, CancellationToken, Task> _refreshAsync;
    private readonly Func<OpcUaEventSubscriptionHandle, CancellationToken, Task> _unsubscribeAsync;
    private readonly OpcUaSubscriptionState _state;
    private readonly Dictionary<string, OpcUaEventMonitoredItemRegistration> _registrations;
    private Subscription _subscription;
    private OpcUaEventFilterDefinition _filterDefinition;

    public OpcUaEventSubscriptionHandle(
        string name,
        Subscription subscription,
        OpcUaEventSubscriptionBuildRequest buildRequest,
        IReadOnlyList<OpcUaEventMonitoredItemRegistration> monitoredItems,
        OpcUaSubscriptionState state,
        OpcUaEventFilterDefinition filterDefinition,
        Func<OpcUaEventSubscriptionHandle, IReadOnlyList<OpcUaNode>, CancellationToken, Task> addSourcesAsync,
        Func<OpcUaEventSubscriptionHandle, IReadOnlyList<string>, CancellationToken, Task> removeSourcesAsync,
        Func<OpcUaEventSubscriptionHandle, CancellationToken, Task> refreshAsync,
        Func<OpcUaEventSubscriptionHandle, CancellationToken, Task> unsubscribeAsync)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
        BuildRequest = buildRequest ?? throw new ArgumentNullException(nameof(buildRequest));
        if (monitoredItems == null)
        {
            throw new ArgumentNullException(nameof(monitoredItems));
        }

        _registrations = monitoredItems.ToDictionary(static item => item.SourceNodeId, StringComparer.Ordinal);
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _filterDefinition = filterDefinition ?? throw new ArgumentNullException(nameof(filterDefinition));
        _addSourcesAsync = addSourcesAsync ?? throw new ArgumentNullException(nameof(addSourcesAsync));
        _removeSourcesAsync = removeSourcesAsync ?? throw new ArgumentNullException(nameof(removeSourcesAsync));
        _refreshAsync = refreshAsync ?? throw new ArgumentNullException(nameof(refreshAsync));
        _unsubscribeAsync = unsubscribeAsync ?? throw new ArgumentNullException(nameof(unsubscribeAsync));
    }

    public string Name { get; }

    public bool IsActive => _state.IsActive;

    public IReadOnlyList<string> SourceNodeIds => GetSourceNodeIdsSnapshot();

    internal Subscription Subscription => _subscription;

    internal OpcUaEventSubscriptionBuildRequest BuildRequest { get; }

    internal OpcUaSubscriptionState State => _state;

    internal OpcUaEventFilterDefinition FilterDefinition => _filterDefinition;

    internal uint QueueSize => BuildRequest.QueueSize;

    internal bool DiscardOldest => BuildRequest.DiscardOldest;

    internal bool ConditionRefreshOnStart => BuildRequest.ConditionRefreshOnStart;

    internal Action<OpcUaEventNotification> OnEvent => BuildRequest.OnEvent;

    public Task AddSourceAsync(string sourceNodeId, CancellationToken ct = default)
    {
        return AddSourceAsync(new OpcUaNode(sourceNodeId), ct);
    }

    public Task AddSourceAsync(OpcUaNode sourceNode, CancellationToken ct = default)
    {
        if (sourceNode == null)
        {
            throw new ArgumentNullException(nameof(sourceNode));
        }

        return _addSourcesAsync(this, new[] { sourceNode }, ct);
    }

    public Task AddSourcesAsync(IEnumerable<string> sourceNodeIds, CancellationToken ct = default)
    {
        if (sourceNodeIds == null)
        {
            throw new ArgumentNullException(nameof(sourceNodeIds));
        }

        return AddSourcesAsync(sourceNodeIds.Select(static sourceNodeId => new OpcUaNode(sourceNodeId)), ct);
    }

    public Task AddSourcesAsync(IEnumerable<OpcUaNode> sourceNodes, CancellationToken ct = default)
    {
        if (sourceNodes == null)
        {
            throw new ArgumentNullException(nameof(sourceNodes));
        }

        return _addSourcesAsync(this, sourceNodes.ToList(), ct);
    }

    public Task RemoveSourceAsync(string sourceNodeId, CancellationToken ct = default)
    {
        return RemoveSourcesAsync(new[] { sourceNodeId }, ct);
    }

    public Task RemoveSourcesAsync(IEnumerable<string> sourceNodeIds, CancellationToken ct = default)
    {
        if (sourceNodeIds == null)
        {
            throw new ArgumentNullException(nameof(sourceNodeIds));
        }

        return _removeSourcesAsync(this, sourceNodeIds.ToList(), ct);
    }

    public Task RefreshAsync(CancellationToken ct = default)
    {
        return _refreshAsync(this, ct);
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
            throw new InvalidOperationException("The OPC UA event subscription is no longer active.");
        }
    }

    internal bool ContainsSource(string sourceNodeId)
    {
        lock (_registrationsLock)
        {
            return _registrations.ContainsKey(sourceNodeId);
        }
    }

    internal IReadOnlyList<OpcUaEventMonitoredItemRegistration> GetRegistrationsOrThrow(
        IReadOnlyList<string> sourceNodeIds)
    {
        var registrations = new List<OpcUaEventMonitoredItemRegistration>(sourceNodeIds.Count);

        lock (_registrationsLock)
        {
            foreach (var sourceNodeId in sourceNodeIds)
            {
                if (!_registrations.TryGetValue(sourceNodeId, out var registration))
                {
                    throw new InvalidOperationException(
                        $"Source node '{sourceNodeId}' is not part of event subscription '{Name}'.");
                }

                registrations.Add(registration);
            }
        }

        return registrations;
    }

    internal void AddRegistrations(IReadOnlyList<OpcUaEventMonitoredItemRegistration> registrations)
    {
        lock (_registrationsLock)
        {
            foreach (var registration in registrations)
            {
                _registrations.Add(registration.SourceNodeId, registration);
            }
        }
    }

    internal void RemoveRegistrations(IReadOnlyList<string> sourceNodeIds)
    {
        lock (_registrationsLock)
        {
            foreach (var sourceNodeId in sourceNodeIds)
            {
                _registrations.Remove(sourceNodeId);
            }
        }
    }

    internal void DetachHandlers()
    {
        DetachHandlers(GetRegistrationsSnapshot());
    }

    internal IReadOnlyList<OpcUaEventMonitoredItemRegistration> GetRegistrationsSnapshot()
    {
        lock (_registrationsLock)
        {
            return _registrations.Values.ToList();
        }
    }

    internal IReadOnlyList<OpcUaNode> GetSourceNodes()
    {
        lock (_registrationsLock)
        {
            return _registrations.Values
                .Select(static registration => new OpcUaNode(
                    registration.SourceNodeId,
                    registration.SourceDisplayName))
                .ToList();
        }
    }

    internal static void DetachHandlers(IReadOnlyList<OpcUaEventMonitoredItemRegistration> registrations)
    {
        foreach (var registration in registrations)
        {
            registration.MonitoredItem.Notification -= registration.Handler;
        }
    }

    internal void ReplaceAfterReconnect(
        Subscription subscription,
        OpcUaEventFilterDefinition filterDefinition,
        IReadOnlyList<OpcUaEventMonitoredItemRegistration> registrations)
    {
        if (subscription == null)
        {
            throw new ArgumentNullException(nameof(subscription));
        }

        if (filterDefinition == null)
        {
            throw new ArgumentNullException(nameof(filterDefinition));
        }

        if (registrations == null)
        {
            throw new ArgumentNullException(nameof(registrations));
        }

        lock (_registrationsLock)
        {
            _subscription = subscription;
            _filterDefinition = filterDefinition;
            _registrations.Clear();
            foreach (var registration in registrations)
            {
                _registrations[registration.SourceNodeId] = registration;
            }
        }
    }

    private IReadOnlyList<string> GetSourceNodeIdsSnapshot()
    {
        lock (_registrationsLock)
        {
            return _registrations.Keys.ToList().AsReadOnly();
        }
    }
}

internal sealed class OpcUaEventMonitoredItemRegistration
{
    public OpcUaEventMonitoredItemRegistration(
        string sourceNodeId,
        string? sourceDisplayName,
        OpcUaEventFilterDefinition filterDefinition,
        MonitoredItem monitoredItem,
        MonitoredItemNotificationEventHandler handler)
    {
        SourceNodeId = sourceNodeId ?? throw new ArgumentNullException(nameof(sourceNodeId));
        SourceDisplayName = string.IsNullOrWhiteSpace(sourceDisplayName)
            ? null
            : sourceDisplayName.Trim();
        FilterDefinition = filterDefinition ?? throw new ArgumentNullException(nameof(filterDefinition));
        MonitoredItem = monitoredItem ?? throw new ArgumentNullException(nameof(monitoredItem));
        Handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public string SourceNodeId { get; }

    public string? SourceDisplayName { get; }

    public OpcUaEventFilterDefinition FilterDefinition { get; }

    public MonitoredItem MonitoredItem { get; }

    public MonitoredItemNotificationEventHandler Handler { get; }
}
