using System.Globalization;
using Opc.Ua;
using Opc.Ua.Client;

namespace OpcUaClientKit;

internal sealed class OpcUaClient : ISubscribableOpcUaClient, IEventSubscribableOpcUaClient
{
    private static readonly ITelemetryContext s_telemetry = DefaultTelemetry.Create(_ => { });
    private static readonly HashSet<uint> s_retryableSuppressedOrShelvedFilterStatusCodes = new()
    {
        StatusCodes.BadEventFilterInvalid,
        StatusCodes.BadFilterNotAllowed,
        StatusCodes.BadContentFilterInvalid,
        StatusCodes.BadFilterOperandInvalid,
        StatusCodes.BadFilterOperatorInvalid,
        StatusCodes.BadFilterOperatorUnsupported,
        StatusCodes.BadFilterOperandCountMismatch,
        StatusCodes.BadFilterElementInvalid,
        StatusCodes.BadFilterLiteralInvalid,
        StatusCodes.BadMonitoredItemFilterInvalid,
        StatusCodes.BadMonitoredItemFilterUnsupported
    };

    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private readonly object _subscriptionsLock = new();
    private readonly List<OpcUaSubscriptionHandle> _subscriptions = new();
    private readonly List<OpcUaEventSubscriptionHandle> _eventSubscriptions = new();
    private readonly Func<OpcUaClientOptions, CancellationToken, Task<OpcUaClientConnection>> _connectAsync;
    private readonly OpcUaClientOptions _options;
    private OpcUaClientConnection? _connection;
    private bool _disposed;

    public OpcUaClient(
        OpcUaClientOptions options,
        Func<OpcUaClientOptions, CancellationToken, Task<OpcUaClientConnection>> connectAsync)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _connectAsync = connectAsync ?? throw new ArgumentNullException(nameof(connectAsync));
    }

    public bool IsConnected => _connection?.Session.Connected == true;

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        await _syncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();

            if (IsConnected)
            {
                return;
            }

            if (_connection != null)
            {
                await DisconnectCoreUnsafeAsync(ct).ConfigureAwait(false);
            }

            _connection = await _connectAsync(_options, ct).ConfigureAwait(false);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        await _syncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await DisconnectCoreUnsafeAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task<object?> ReadNodeAsync(string nodeId, CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var session = GetRequiredSession();
        var normalizedNodeId = NormalizeNodeId(nodeId, nameof(nodeId));
        var dataValue = await session.ReadValueAsync(ParseNodeId(normalizedNodeId), ct).ConfigureAwait(false);
        EnsureReadSucceeded(dataValue, normalizedNodeId);
        return dataValue.Value;
    }

    public Task<object?> ReadNodeAsync(OpcUaNode node, CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var normalizedNode = NormalizeNode(node, nameof(node));
        return ReadNodeAsync(normalizedNode.NodeId, ct);
    }

    public async Task<T?> ReadNodeAsync<T>(string nodeId, CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var session = GetRequiredSession();
        var normalizedNodeId = NormalizeNodeId(nodeId, nameof(nodeId));
        var dataValue = await session.ReadValueAsync(ParseNodeId(normalizedNodeId), ct).ConfigureAwait(false);
        EnsureReadSucceeded(dataValue, normalizedNodeId);
        return ConvertReadValue<T>(dataValue.Value, normalizedNodeId);
    }

    public Task<T?> ReadNodeAsync<T>(OpcUaNode node, CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var normalizedNode = NormalizeNode(node, nameof(node));
        return ReadNodeAsync<T>(normalizedNode.NodeId, ct);
    }

    public async Task<IReadOnlyDictionary<string, object?>> ReadNodesAsync(
        IEnumerable<string> nodeIds,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var session = GetRequiredSession();
        var normalizedNodeIds = NormalizeNodeIds(nodeIds);
        return await ReadNodesCoreAsync(session, normalizedNodeIds, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<string, object?>> ReadNodesAsync(
        IEnumerable<OpcUaNode> nodes,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var session = GetRequiredSession();
        var normalizedNodes = NormalizeNodes(nodes, nameof(nodes));
        var normalizedNodeIds = normalizedNodes.Select(static node => node.NodeId).ToList();
        return await ReadNodesCoreAsync(session, normalizedNodeIds, ct).ConfigureAwait(false);
    }

    public async Task WriteNodeAsync(string nodeId, object? value, CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var session = GetRequiredSession();
        var normalizedNodeId = NormalizeNodeId(nodeId, nameof(nodeId));
        var valuesToWrite = new WriteValueCollection
        {
            new WriteValue
            {
                NodeId = ParseNodeId(normalizedNodeId),
                AttributeId = Attributes.Value,
                Value = new DataValue(new Variant(value))
            }
        };

        var response = await session.WriteAsync(null, valuesToWrite, ct).ConfigureAwait(false);
        var statusCode = response.Results?.Count > 0
            ? response.Results[0]
            : StatusCodes.BadUnexpectedError;

        if (StatusCode.IsBad(statusCode))
        {
            throw new ServiceResultException(statusCode, $"Failed to write node '{normalizedNodeId}'.");
        }
    }

    public Task WriteNodeAsync(OpcUaNode node, object? value, CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var normalizedNode = NormalizeNode(node, nameof(node));
        return WriteNodeAsync(normalizedNode.NodeId, value, ct);
    }

    public Task WriteNodesAsync(
        Dictionary<string, object?> nodeValues,
        CancellationToken ct = default)
    {
        return WriteNodesAsync((IReadOnlyDictionary<string, object?>)nodeValues, ct);
    }

    public async Task WriteNodesAsync(
        IReadOnlyDictionary<string, object?> nodeValues,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var session = GetRequiredSession();
        var normalizedNodeValues = NormalizeNodeValues(nodeValues);
        await WriteNodesCoreAsync(session, normalizedNodeValues, ct).ConfigureAwait(false);
    }

    public async Task WriteNodesAsync(
        IReadOnlyDictionary<OpcUaNode, object?> nodeValues,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var session = GetRequiredSession();
        var normalizedNodeValues = NormalizeNodeValues(nodeValues);
        await WriteNodesCoreAsync(session, normalizedNodeValues, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<object?>> CallMethodAsync(
        string objectNodeId,
        string methodNodeId,
        IEnumerable<object?>? inputArguments = null,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var session = GetRequiredSession();
        var normalizedObjectNodeId = NormalizeNodeId(objectNodeId, nameof(objectNodeId));
        var normalizedMethodNodeId = NormalizeNodeId(methodNodeId, nameof(methodNodeId));

        return await CallMethodCoreAsync(
                session,
                normalizedObjectNodeId,
                normalizedMethodNodeId,
                inputArguments,
                ct)
            .ConfigureAwait(false);
    }

    public Task<IReadOnlyList<object?>> CallMethodAsync(
        OpcUaNode objectNode,
        OpcUaNode methodNode,
        IEnumerable<object?>? inputArguments = null,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var normalizedObjectNode = NormalizeNode(objectNode, nameof(objectNode));
        var normalizedMethodNode = NormalizeNode(methodNode, nameof(methodNode));

        return CallMethodAsync(
            normalizedObjectNode.NodeId,
            normalizedMethodNode.NodeId,
            inputArguments,
            ct);
    }

    public Task<IOpcUaSubscription> SubscribeNodeAsync(
        string nodeId,
        Action<OpcUaValueChangeNotification> onChanged,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return SubscribeNodeAsync(new OpcUaNode(nodeId), onChanged, ct);
    }

    public async Task<IOpcUaSubscription> SubscribeNodeAsync(
        OpcUaNode node,
        Action<OpcUaValueChangeNotification> onChanged,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var normalizedNode = NormalizeNode(node, nameof(node));
        if (onChanged == null)
        {
            throw new ArgumentNullException(nameof(onChanged));
        }

        var request = CreateDefaultSubscriptionBuildRequest();
        var normalizedNodes = NormalizeSubscriptionNodes(
            new[]
            {
                new OpcUaSubscriptionItemDefinition(normalizedNode, onChanged, new OpcUaMonitoredItemOptions())
            },
            nameof(node));

        await _syncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();

            var session = GetRequiredSession();
            var subscription = (OpcUaSubscriptionHandle)await BuildSubscriptionUnsafeAsync(session, request, ct)
                .ConfigureAwait(false);

            try
            {
                await AddSubscriptionNodesUnsafeAsync(subscription, normalizedNodes, ct).ConfigureAwait(false);
                return subscription;
            }
            catch
            {
                await UnsubscribeSubscriptionUnsafeAsync(subscription, ct).ConfigureAwait(false);
                throw;
            }
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public OpcUaSubscriptionBuilder CreateSubscriptionBuilder()
    {
        ThrowIfDisposed();
        return new OpcUaSubscriptionBuilder(this);
    }

    public Task<IOpcUaEventSubscription> SubscribeAlarmEventsAsync(
        string sourceNodeId,
        Action<OpcUaEventNotification> onEvent,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return SubscribeAlarmEventsAsync(new OpcUaNode(sourceNodeId), onEvent, ct);
    }

    public async Task<IOpcUaEventSubscription> SubscribeAlarmEventsAsync(
        OpcUaNode sourceNode,
        Action<OpcUaEventNotification> onEvent,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var normalizedSourceNode = NormalizeNode(sourceNode, nameof(sourceNode));
        if (onEvent == null)
        {
            throw new ArgumentNullException(nameof(onEvent));
        }

        var request = CreateDefaultEventSubscriptionBuildRequest(onEvent);
        var normalizedSourceNodes = NormalizeNodes(new[] { normalizedSourceNode }, nameof(sourceNode));

        await _syncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();

            var session = GetRequiredSession();
            var subscription = (OpcUaEventSubscriptionHandle)await BuildEventSubscriptionUnsafeAsync(
                    session,
                    request,
                    ct)
                .ConfigureAwait(false);

            try
            {
                await AddEventSourcesUnsafeAsync(
                        subscription,
                        normalizedSourceNodes,
                        ct)
                    .ConfigureAwait(false);
                return subscription;
            }
            catch
            {
                await UnsubscribeEventSubscriptionUnsafeAsync(subscription, ct).ConfigureAwait(false);
                throw;
            }
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public OpcUaEventSubscriptionBuilder CreateEventSubscriptionBuilder()
    {
        ThrowIfDisposed();
        return new OpcUaEventSubscriptionBuilder(this);
    }

    internal async Task<IOpcUaSubscription> BuildSubscriptionAsync(
        OpcUaSubscriptionBuildRequest request,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var normalizedRequest = NormalizeSubscriptionBuildRequest(request);

        await _syncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            var session = GetRequiredSession();
            return await BuildSubscriptionUnsafeAsync(session, normalizedRequest, ct).ConfigureAwait(false);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    internal async Task<IOpcUaEventSubscription> BuildEventSubscriptionAsync(
        OpcUaEventSubscriptionBuildRequest request,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var normalizedRequest = NormalizeEventSubscriptionBuildRequest(request);

        await _syncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            var session = GetRequiredSession();
            return await BuildEventSubscriptionUnsafeAsync(session, normalizedRequest, ct).ConfigureAwait(false);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _syncLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            await DisconnectCoreUnsafeAsync(CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            _syncLock.Release();
            GC.SuppressFinalize(this);
        }
    }

    private async Task<IReadOnlyDictionary<string, object?>> ReadNodesCoreAsync(
        ISession session,
        IReadOnlyList<string> normalizedNodeIds,
        CancellationToken ct)
    {
        var nodesToRead = normalizedNodeIds.Select(ParseNodeId).ToList();
        var (dataValues, _) = await session.ReadValuesAsync(nodesToRead, ct).ConfigureAwait(false);

        if (dataValues == null || dataValues.Count != normalizedNodeIds.Count)
        {
            throw new InvalidOperationException("The OPC UA server returned an unexpected number of read results.");
        }

        return BuildReadResults(normalizedNodeIds, dataValues);
    }

    private static void EnsureReadSucceeded(DataValue dataValue, string nodeId)
    {
        if (dataValue == null)
        {
            throw new InvalidOperationException($"The OPC UA server returned no value for node '{nodeId}'.");
        }

        if (StatusCode.IsBad(dataValue.StatusCode))
        {
            throw new ServiceResultException(dataValue.StatusCode, $"Failed to read node '{nodeId}'.");
        }
    }

    private static T? ConvertReadValue<T>(object? value, string nodeId)
    {
        if (value == null)
        {
            return default;
        }

        if (value is T typedValue)
        {
            return typedValue;
        }

        var targetType = typeof(T);
        var effectiveTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        try
        {
            if (effectiveTargetType.IsEnum)
            {
                if (value is string enumText)
                {
                    return (T)Enum.Parse(effectiveTargetType, enumText, ignoreCase: true);
                }

                var enumUnderlyingType = Enum.GetUnderlyingType(effectiveTargetType);
                var enumValue = Convert.ChangeType(value, enumUnderlyingType, CultureInfo.InvariantCulture);
                return (T)Enum.ToObject(effectiveTargetType, enumValue!);
            }

            if (effectiveTargetType.IsInstanceOfType(value))
            {
                return (T)value;
            }

            if (value is IConvertible &&
                typeof(IConvertible).IsAssignableFrom(effectiveTargetType))
            {
                return (T)Convert.ChangeType(value, effectiveTargetType, CultureInfo.InvariantCulture);
            }
        }
        catch (Exception exception) when (
            exception is InvalidCastException ||
            exception is FormatException ||
            exception is OverflowException ||
            exception is ArgumentException)
        {
            throw new InvalidCastException(
                $"Failed to convert node '{nodeId}' value from '{value.GetType().FullName}' to '{targetType.FullName}'.",
                exception);
        }

        throw new InvalidCastException(
            $"Failed to convert node '{nodeId}' value from '{value.GetType().FullName}' to '{targetType.FullName}'.");
    }

    private async Task WriteNodesCoreAsync(
        ISession session,
        IReadOnlyList<KeyValuePair<string, object?>> normalizedNodeValues,
        CancellationToken ct)
    {
        var valuesToWrite = new WriteValueCollection();
        foreach (var nodeValue in normalizedNodeValues)
        {
            valuesToWrite.Add(new WriteValue
            {
                NodeId = ParseNodeId(nodeValue.Key),
                AttributeId = Attributes.Value,
                Value = new DataValue(new Variant(nodeValue.Value))
            });
        }

        var response = await session.WriteAsync(null, valuesToWrite, ct).ConfigureAwait(false);
        var results = response.Results;
        if (results == null || results.Count != normalizedNodeValues.Count)
        {
            throw new InvalidOperationException("The OPC UA server returned an unexpected number of write results.");
        }

        EnsureWriteResultsSucceeded(normalizedNodeValues, results);
    }

    private static async Task<IReadOnlyList<object?>> CallMethodCoreAsync(
        ISession session,
        string objectNodeId,
        string methodNodeId,
        IEnumerable<object?>? inputArguments,
        CancellationToken ct)
    {
        var methodsToCall = new CallMethodRequestCollection
        {
            new CallMethodRequest
            {
                ObjectId = ParseNodeId(objectNodeId),
                MethodId = ParseNodeId(methodNodeId),
                InputArguments = NormalizeInputArguments(inputArguments)
            }
        };

        var response = await session.CallAsync(null, methodsToCall, ct).ConfigureAwait(false);
        var results = response.Results;
        if (results == null || results.Count != 1)
        {
            throw new InvalidOperationException("The OPC UA server returned an unexpected number of method call results.");
        }

        var result = results[0];
        if (StatusCode.IsBad(result.StatusCode))
        {
            throw new ServiceResultException(
                result.StatusCode,
                $"Failed to call method '{methodNodeId}' on object '{objectNodeId}'.");
        }

        if (result.InputArgumentResults != null)
        {
            for (var i = 0; i < result.InputArgumentResults.Count; i++)
            {
                var inputStatusCode = result.InputArgumentResults[i];
                if (StatusCode.IsBad(inputStatusCode))
                {
                    throw new ServiceResultException(
                        inputStatusCode,
                        $"Input argument {i} is invalid when calling method '{methodNodeId}' on object '{objectNodeId}'.");
                }
            }
        }

        if (result.OutputArguments == null || result.OutputArguments.Count == 0)
        {
            return Array.Empty<object?>();
        }

        return result.OutputArguments
            .Select(static argument => argument.Value)
            .ToList();
    }

    private async Task<IOpcUaSubscription> BuildSubscriptionUnsafeAsync(
        ISession session,
        OpcUaSubscriptionBuildRequest request,
        CancellationToken ct)
    {
        var subscription = new Subscription(s_telemetry, null)
        {
            DisplayName = request.Name ?? string.Empty,
            PublishingEnabled = request.PublishingEnabled,
            PublishingInterval = request.PublishingInterval,
            KeepAliveCount = request.KeepAliveCount,
            LifetimeCount = request.LifetimeCount,
            MaxNotificationsPerPublish = request.MaxNotificationsPerPublish,
            Priority = request.Priority
        };

        var state = new OpcUaSubscriptionState();
        session.AddSubscription(subscription);

        try
        {
            await subscription.CreateAsync(ct).ConfigureAwait(false);

            var handle = new OpcUaSubscriptionHandle(
                request.Name ?? subscription.DisplayName ?? $"subscription-{Guid.NewGuid():N}",
                subscription,
                Array.Empty<OpcUaMonitoredItemRegistration>(),
                state,
                AddSubscriptionNodesAsync,
                RemoveSubscriptionNodesAsync,
                UnsubscribeSubscriptionAsync);

            RegisterSubscription(handle);
            return handle;
        }
        catch
        {
            state.TryDeactivate();
            await RemoveSubscriptionFromSessionAsync(session, subscription, ct).ConfigureAwait(false);
            throw;
        }
    }

    private async Task<IOpcUaEventSubscription> BuildEventSubscriptionUnsafeAsync(
        ISession session,
        OpcUaEventSubscriptionBuildRequest request,
        CancellationToken ct)
    {
        var eventTypeNodeId = await NormalizeAndValidateEventTypeNodeIdAsync(session, request.EventTypeNode.NodeId, ct)
            .ConfigureAwait(false);
        var eventFilterDefinition = await CreateEventFilterDefinitionAsync(
                session,
                eventTypeNodeId,
                request.SelectClauseMode,
                request.SeverityAtLeast,
                request.IgnoreSuppressedOrShelved,
                ct)
            .ConfigureAwait(false);

        var subscription = new Subscription(s_telemetry, null)
        {
            DisplayName = request.Name ?? string.Empty,
            PublishingEnabled = request.PublishingEnabled,
            PublishingInterval = request.PublishingInterval,
            KeepAliveCount = request.KeepAliveCount,
            LifetimeCount = request.LifetimeCount,
            MaxNotificationsPerPublish = request.MaxNotificationsPerPublish,
            Priority = request.Priority
        };

        var state = new OpcUaSubscriptionState();
        session.AddSubscription(subscription);

        try
        {
            await subscription.CreateAsync(ct).ConfigureAwait(false);

            var handle = new OpcUaEventSubscriptionHandle(
                request.Name ?? subscription.DisplayName ?? $"event-subscription-{Guid.NewGuid():N}",
                subscription,
                Array.Empty<OpcUaEventMonitoredItemRegistration>(),
                state,
                eventFilterDefinition,
                request.QueueSize,
                request.DiscardOldest,
                request.ConditionRefreshOnStart,
                request.OnEvent,
                AddEventSourcesAsync,
                RemoveEventSourcesAsync,
                RefreshEventSubscriptionAsync,
                UnsubscribeEventSubscriptionAsync);

            RegisterEventSubscription(handle);
            return handle;
        }
        catch
        {
            state.TryDeactivate();
            await RemoveSubscriptionFromSessionAsync(session, subscription, ct).ConfigureAwait(false);
            throw;
        }
    }

    private async Task AddSubscriptionNodesUnsafeAsync(
        OpcUaSubscriptionHandle subscription,
        IReadOnlyList<OpcUaSubscriptionItemDefinition> normalizedNodes,
        CancellationToken ct)
    {
        subscription.EnsureActive();
        _ = GetRequiredSession();

        foreach (var node in normalizedNodes)
        {
            if (subscription.ContainsNode(node.NodeId))
            {
                throw new InvalidOperationException(
                    $"Node '{node.NodeId}' is already part of subscription '{subscription.Name}'.");
            }
        }

        var monitoredItems = CreateMonitoredItemRegistrations(normalizedNodes, subscription.State);
        subscription.Subscription.AddItems(monitoredItems.Select(static item => item.MonitoredItem));

        try
        {
            await subscription.Subscription.ApplyChangesAsync(ct).ConfigureAwait(false);
            subscription.AddRegistrations(monitoredItems);
        }
        catch
        {
            subscription.Subscription.RemoveItems(monitoredItems.Select(static item => item.MonitoredItem));
            OpcUaSubscriptionHandle.DetachHandlers(monitoredItems);

            try
            {
                await subscription.Subscription.ApplyChangesAsync(ct).ConfigureAwait(false);
            }
            catch when (!ct.IsCancellationRequested)
            {
                // Best effort rollback.
            }

            throw;
        }
    }

    private async Task AddSubscriptionNodesAsync(
        OpcUaSubscriptionHandle subscription,
        IReadOnlyList<OpcUaSubscriptionItemDefinition> nodes,
        CancellationToken ct)
    {
        ThrowIfDisposed();

        var normalizedNodes = NormalizeSubscriptionNodes(nodes, nameof(nodes));

        await _syncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            await AddSubscriptionNodesUnsafeAsync(subscription, normalizedNodes, ct).ConfigureAwait(false);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private async Task RemoveSubscriptionNodesAsync(
        OpcUaSubscriptionHandle subscription,
        IReadOnlyList<string> nodeIds,
        CancellationToken ct)
    {
        ThrowIfDisposed();

        var normalizedNodeIds = NormalizeNodeIds(nodeIds, nameof(nodeIds));

        await _syncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            subscription.EnsureActive();
            _ = GetRequiredSession();

            var registrations = subscription.GetRegistrationsOrThrow(normalizedNodeIds);
            subscription.Subscription.RemoveItems(registrations.Select(static item => item.MonitoredItem));

            try
            {
                await subscription.Subscription.ApplyChangesAsync(ct).ConfigureAwait(false);
                OpcUaSubscriptionHandle.DetachHandlers(registrations);
                subscription.RemoveRegistrations(normalizedNodeIds);
            }
            catch
            {
                subscription.Subscription.AddItems(registrations.Select(static item => item.MonitoredItem));

                try
                {
                    await subscription.Subscription.ApplyChangesAsync(ct).ConfigureAwait(false);
                }
                catch when (!ct.IsCancellationRequested)
                {
                    // Best effort rollback.
                }

                throw;
            }
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private async Task AddEventSourcesUnsafeAsync(
        OpcUaEventSubscriptionHandle subscription,
        IReadOnlyList<OpcUaNode> normalizedSourceNodes,
        CancellationToken ct)
    {
        subscription.EnsureActive();
        var session = GetRequiredSession();

        foreach (var sourceNode in normalizedSourceNodes)
        {
            if (subscription.ContainsSource(sourceNode.NodeId))
            {
                throw new InvalidOperationException(
                    $"Source node '{sourceNode.NodeId}' is already part of event subscription '{subscription.Name}'.");
            }

            await EnsureSourceNodeSupportsEventsAsync(session, sourceNode.NodeId, ct).ConfigureAwait(false);
        }

        var filterDefinition = subscription.FilterDefinition;
        var useServerSideSuppressedOrShelvedFilter = filterDefinition.IgnoreSuppressedOrShelved;

        while (true)
        {
            var monitoredItems = CreateEventMonitoredItemRegistrations(
                normalizedSourceNodes,
                filterDefinition,
                subscription.QueueSize,
                subscription.DiscardOldest,
                subscription.OnEvent,
                subscription.State,
                useServerSideSuppressedOrShelvedFilter);

            subscription.Subscription.AddItems(monitoredItems.Select(static item => item.MonitoredItem));

            try
            {
                await subscription.Subscription.ApplyChangesAsync(ct).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                await RollbackEventMonitoredItemsAsync(subscription, monitoredItems, ct).ConfigureAwait(false);

                if (useServerSideSuppressedOrShelvedFilter &&
                    filterDefinition.CanFallbackToClientSideSuppressedOrShelvedFilter &&
                    CanRetryWithoutSuppressedOrShelvedServerFilter(exception))
                {
                    useServerSideSuppressedOrShelvedFilter = false;
                    ReportDiagnostic(
                        OpcUaClientDiagnosticKind.EventFilterFallbackWarning,
                        $"The server rejected the SuppressedOrShelved filter for event subscription '{subscription.Name}'. Falling back to client-side filtering.",
                        exception,
                        subscription.Name,
                        string.Join(", ", normalizedSourceNodes.Select(static node => node.NodeId)));
                    continue;
                }

                throw;
            }

            try
            {
                if (subscription.ConditionRefreshOnStart)
                {
                    await subscription.Subscription.ConditionRefreshAsync(ct).ConfigureAwait(false);
                }

                subscription.AddRegistrations(monitoredItems);
                break;
            }
            catch
            {
                await RollbackEventMonitoredItemsAsync(subscription, monitoredItems, ct).ConfigureAwait(false);
                throw;
            }
        }
    }

    private async Task AddEventSourcesAsync(
        OpcUaEventSubscriptionHandle subscription,
        IReadOnlyList<OpcUaNode> sourceNodes,
        CancellationToken ct)
    {
        ThrowIfDisposed();

        var normalizedSourceNodes = NormalizeNodes(sourceNodes, nameof(sourceNodes));

        await _syncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            await AddEventSourcesUnsafeAsync(
                    subscription,
                    normalizedSourceNodes,
                    ct)
                .ConfigureAwait(false);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private async Task RemoveEventSourcesAsync(
        OpcUaEventSubscriptionHandle subscription,
        IReadOnlyList<string> sourceNodeIds,
        CancellationToken ct)
    {
        ThrowIfDisposed();

        var normalizedSourceNodeIds = NormalizeNodeIds(sourceNodeIds, nameof(sourceNodeIds));

        await _syncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            subscription.EnsureActive();
            _ = GetRequiredSession();

            var registrations = subscription.GetRegistrationsOrThrow(normalizedSourceNodeIds);
            subscription.Subscription.RemoveItems(registrations.Select(static item => item.MonitoredItem));

            try
            {
                await subscription.Subscription.ApplyChangesAsync(ct).ConfigureAwait(false);
                OpcUaEventSubscriptionHandle.DetachHandlers(registrations);
                subscription.RemoveRegistrations(normalizedSourceNodeIds);
            }
            catch
            {
                subscription.Subscription.AddItems(registrations.Select(static item => item.MonitoredItem));

                try
                {
                    await subscription.Subscription.ApplyChangesAsync(ct).ConfigureAwait(false);
                }
                catch when (!ct.IsCancellationRequested)
                {
                    // Best effort rollback.
                }

                throw;
            }
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private async Task RefreshEventSubscriptionAsync(
        OpcUaEventSubscriptionHandle subscription,
        CancellationToken ct)
    {
        ThrowIfDisposed();

        await _syncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            subscription.EnsureActive();
            _ = GetRequiredSession();

            if (subscription.SourceNodeIds.Count == 0)
            {
                return;
            }

            await subscription.Subscription.ConditionRefreshAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private async Task UnsubscribeSubscriptionAsync(
        OpcUaSubscriptionHandle subscription,
        CancellationToken ct)
    {
        if (subscription == null)
        {
            throw new ArgumentNullException(nameof(subscription));
        }

        await _syncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await UnsubscribeSubscriptionUnsafeAsync(subscription, ct).ConfigureAwait(false);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private async Task UnsubscribeEventSubscriptionAsync(
        OpcUaEventSubscriptionHandle subscription,
        CancellationToken ct)
    {
        if (subscription == null)
        {
            throw new ArgumentNullException(nameof(subscription));
        }

        await _syncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await UnsubscribeEventSubscriptionUnsafeAsync(subscription, ct).ConfigureAwait(false);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private async Task UnsubscribeAllUnsafeAsync(CancellationToken ct)
    {
        foreach (var subscription in GetSubscriptionsSnapshot())
        {
            await UnsubscribeSubscriptionUnsafeAsync(subscription, ct).ConfigureAwait(false);
        }
    }

    private async Task UnsubscribeAllEventSubscriptionsUnsafeAsync(CancellationToken ct)
    {
        foreach (var subscription in GetEventSubscriptionsSnapshot())
        {
            await UnsubscribeEventSubscriptionUnsafeAsync(subscription, ct).ConfigureAwait(false);
        }
    }

    private async Task UnsubscribeSubscriptionUnsafeAsync(
        OpcUaSubscriptionHandle subscription,
        CancellationToken ct)
    {
        if (!subscription.TryDeactivate())
        {
            return;
        }

        subscription.DetachHandlers();
        UnregisterSubscription(subscription);

        if (_connection?.Session is not { Connected: true } session)
        {
            return;
        }

        await RemoveSubscriptionFromSessionAsync(session, subscription.Subscription, ct).ConfigureAwait(false);
    }

    private async Task UnsubscribeEventSubscriptionUnsafeAsync(
        OpcUaEventSubscriptionHandle subscription,
        CancellationToken ct)
    {
        if (!subscription.TryDeactivate())
        {
            return;
        }

        subscription.DetachHandlers();
        UnregisterEventSubscription(subscription);

        if (_connection?.Session is not { Connected: true } session)
        {
            return;
        }

        await RemoveSubscriptionFromSessionAsync(session, subscription.Subscription, ct).ConfigureAwait(false);
    }

    private static async Task RemoveSubscriptionFromSessionAsync(
        ISession session,
        Subscription subscription,
        CancellationToken ct)
    {
        try
        {
            await session.RemoveSubscriptionAsync(subscription, ct).ConfigureAwait(false);
        }
        catch when (!ct.IsCancellationRequested)
        {
            // Best effort cleanup. The session may already be closing or unavailable.
        }
    }

    private IReadOnlyList<OpcUaMonitoredItemRegistration> CreateMonitoredItemRegistrations(
        IReadOnlyList<OpcUaSubscriptionItemDefinition> nodes,
        OpcUaSubscriptionState state)
    {
        var monitoredItems = new List<OpcUaMonitoredItemRegistration>(nodes.Count);

        foreach (var node in nodes)
        {
            var monitoredItem = new MonitoredItem(s_telemetry, null)
            {
                StartNodeId = ParseNodeId(node.NodeId),
                AttributeId = Attributes.Value,
                DisplayName = node.Options.DisplayName ?? string.Empty,
                MonitoringMode = MonitoringMode.Reporting,
                SamplingInterval = ConvertSamplingInterval(node.Options.SamplingInterval),
                QueueSize = node.Options.QueueSize,
                DiscardOldest = node.Options.DiscardOldest
            };

            OpcUaMonitoredItemRegistration? registration = null;
            MonitoredItemNotificationEventHandler handler = (item, eventArgs) =>
                OnMonitoredItemNotification(state, registration!, item, eventArgs);

            registration = new OpcUaMonitoredItemRegistration(
                node.NodeId,
                node.Node.DisplayName,
                node.OnChanged,
                node.Options.Clone(),
                monitoredItem,
                handler);

            monitoredItem.Notification += handler;
            monitoredItems.Add(registration);
        }

        return monitoredItems;
    }

    private IReadOnlyList<OpcUaEventMonitoredItemRegistration> CreateEventMonitoredItemRegistrations(
        IReadOnlyList<OpcUaNode> sourceNodes,
        OpcUaEventFilterDefinition filterDefinition,
        uint queueSize,
        bool discardOldest,
        Action<OpcUaEventNotification> onEvent,
        OpcUaSubscriptionState state,
        bool useServerSideSuppressedOrShelvedFilter)
    {
        var monitoredItems = new List<OpcUaEventMonitoredItemRegistration>(sourceNodes.Count);

        foreach (var sourceNode in sourceNodes)
        {
            var monitoredItem = new MonitoredItem(s_telemetry, null)
            {
                StartNodeId = ParseNodeId(sourceNode.NodeId),
                AttributeId = Attributes.EventNotifier,
                DisplayName = sourceNode.DisplayName ?? string.Empty,
                MonitoringMode = MonitoringMode.Reporting,
                QueueSize = queueSize,
                DiscardOldest = discardOldest,
                Filter = CreateAlarmEventFilter(filterDefinition, useServerSideSuppressedOrShelvedFilter)
            };

            OpcUaEventMonitoredItemRegistration? registration = null;
            MonitoredItemNotificationEventHandler handler = (item, eventArgs) =>
                OnEventMonitoredItemNotification(state, registration!, onEvent, item, eventArgs);

            registration = new OpcUaEventMonitoredItemRegistration(
                sourceNode.NodeId,
                sourceNode.DisplayName,
                filterDefinition,
                monitoredItem,
                handler);

            monitoredItem.Notification += handler;
            monitoredItems.Add(registration);
        }

        return monitoredItems;
    }

    private void OnMonitoredItemNotification(
        OpcUaSubscriptionState state,
        OpcUaMonitoredItemRegistration registration,
        MonitoredItem monitoredItem,
        MonitoredItemNotificationEventArgs eventArgs)
    {
        if (!state.IsActive)
        {
            return;
        }

        var dataValue = TryGetDataValue(monitoredItem, eventArgs);
        if (dataValue == null)
        {
            return;
        }

        var displayName = registration.DisplayName;
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = string.IsNullOrWhiteSpace(monitoredItem.DisplayName)
                ? null
                : monitoredItem.DisplayName;
        }

        var notification = new OpcUaValueChangeNotification(
            registration.NodeId,
            displayName,
            dataValue.Value,
            dataValue.StatusCode.Code,
            !StatusCode.IsBad(dataValue.StatusCode),
            dataValue.SourceTimestamp,
            dataValue.ServerTimestamp);

        try
        {
            registration.OnChanged(notification);
        }
        catch (Exception exception)
        {
            ReportDiagnostic(
                OpcUaClientDiagnosticKind.SubscriptionCallbackException,
                $"A data subscription callback threw while handling node '{registration.NodeId}'.",
                exception,
                null,
                registration.NodeId);
        }
    }

    private void OnEventMonitoredItemNotification(
        OpcUaSubscriptionState state,
        OpcUaEventMonitoredItemRegistration registration,
        Action<OpcUaEventNotification> onEvent,
        MonitoredItem monitoredItem,
        MonitoredItemNotificationEventArgs eventArgs)
    {
        if (!state.IsActive)
        {
            return;
        }

        var notifications = TryGetEventFieldLists(monitoredItem, eventArgs);
        if (notifications.Count == 0)
        {
            return;
        }

        foreach (var eventFields in notifications)
        {
            var notification = CreateEventNotification(registration, monitoredItem, eventFields);
            if (notification == null)
            {
                continue;
            }

            try
            {
                onEvent(notification);
            }
            catch (Exception exception)
            {
                ReportDiagnostic(
                    OpcUaClientDiagnosticKind.EventSubscriptionCallbackException,
                    $"An event subscription callback threw while handling source '{registration.SourceNodeId}'.",
                    exception,
                    null,
                    registration.SourceNodeId);
            }
        }
    }

    private static DataValue? TryGetDataValue(
        MonitoredItem monitoredItem,
        MonitoredItemNotificationEventArgs eventArgs)
    {
        if (eventArgs.NotificationValue is MonitoredItemNotification notification)
        {
            return notification.Value;
        }

        return monitoredItem.LastValue as DataValue;
    }

    private static IReadOnlyList<EventFieldList> TryGetEventFieldLists(
        MonitoredItem monitoredItem,
        MonitoredItemNotificationEventArgs eventArgs)
    {
        var notifications = monitoredItem.DequeueEvents();
        if (notifications != null && notifications.Count > 0)
        {
            return notifications.ToList();
        }

        if (eventArgs.NotificationValue is EventFieldList eventFieldList)
        {
            return new[] { eventFieldList };
        }

        if (monitoredItem.LastValue is EventFieldList lastEventFieldList)
        {
            return new[] { lastEventFieldList };
        }

        return Array.Empty<EventFieldList>();
    }

    private static OpcUaEventNotification? CreateEventNotification(
        OpcUaEventMonitoredItemRegistration registration,
        MonitoredItem monitoredItem,
        EventFieldList eventFieldList)
    {
        var fields = new Dictionary<string, object?>(StringComparer.Ordinal);
        var selectedFields = new List<OpcUaEventFieldValue>(registration.FilterDefinition.SelectClauses.Count);
        var fieldValues = eventFieldList.EventFields ?? new VariantCollection();

        // The server returns event fields in the exact SelectClause order, so we rebuild both a
        // stable dictionary view and an ordered field list from the stored descriptors.
        for (var i = 0; i < registration.FilterDefinition.SelectClauses.Count; i++)
        {
            var descriptor = registration.FilterDefinition.SelectClauses[i];
            var value = NormalizeEventFieldValue(GetFieldValue(fieldValues, i));
            fields[descriptor.Key] = value;
            selectedFields.Add(new OpcUaEventFieldValue(descriptor.Key, descriptor.DisplayName, value));
        }

        if (registration.FilterDefinition.IgnoreSuppressedOrShelved &&
            ShouldIgnoreSuppressedOrShelvedEvent(fields))
        {
            return null;
        }

        var eventId = GetByteStringField(fields, OpcUaEventFieldKeys.EventId);
        var eventTypeNodeId = GetNodeIdField(fields, OpcUaEventFieldKeys.EventType)
            ?? ObjectTypeIds.BaseEventType.ToString();
        if (IsRefreshBoundaryEvent(eventTypeNodeId))
        {
            return null;
        }

        var sourceNodeId = GetNodeIdField(fields, OpcUaEventFieldKeys.SourceNode);
        var sourceName = GetStringField(fields, OpcUaEventFieldKeys.SourceName);
        var sourceDisplayName = string.Equals(sourceNodeId, registration.SourceNodeId, StringComparison.Ordinal)
            ? registration.SourceDisplayName
            : null;

        if (string.IsNullOrWhiteSpace(sourceDisplayName) &&
            string.Equals(sourceNodeId, registration.SourceNodeId, StringComparison.Ordinal))
        {
            sourceDisplayName = string.IsNullOrWhiteSpace(monitoredItem.DisplayName)
                ? null
                : monitoredItem.DisplayName;
        }

        var time = GetDateTimeField(fields, OpcUaEventFieldKeys.Time);
        var receiveTime = GetDateTimeField(fields, OpcUaEventFieldKeys.ReceiveTime);
        var message = GetStringField(fields, OpcUaEventFieldKeys.Message);
        var severity = GetUInt16Field(fields, OpcUaEventFieldKeys.Severity);
        var conditionId = GetNodeIdField(fields, OpcUaEventFieldKeys.ConditionId)
            ?? GetNodeIdField(fields, OpcUaEventFieldKeys.NodeId);
        var conditionName = GetStringField(fields, OpcUaEventFieldKeys.ConditionName);
        var retain = GetBooleanField(fields, OpcUaEventFieldKeys.Retain);
        var enabled = GetBooleanField(fields, OpcUaEventFieldKeys.EnabledStateId);
        var active = GetBooleanField(fields, OpcUaEventFieldKeys.ActiveStateId);
        var acked = GetBooleanField(fields, OpcUaEventFieldKeys.AckedStateId);

        return new OpcUaEventNotification(
            eventId,
            eventTypeNodeId,
            sourceNodeId,
            sourceDisplayName,
            sourceName,
            time,
            receiveTime,
            message,
            severity,
            conditionId,
            conditionName,
            retain,
            enabled,
            active,
            acked,
            selectedFields,
            fields);
    }

    private static EventFilter CreateAlarmEventFilter(
        OpcUaEventFilterDefinition filterDefinition,
        bool includeServerSideSuppressedOrShelvedFilter)
    {
        var filter = new EventFilter
        {
            SelectClauses = new SimpleAttributeOperandCollection(
                filterDefinition.SelectClauses
                    .Select(static descriptor => CloneSimpleAttributeOperand(descriptor.Operand))
                    .ToList()),
            WhereClause = CreateEventWhereClause(
                filterDefinition.EventTypeNodeId,
                filterDefinition.SeverityAtLeast,
                filterDefinition.IgnoreSuppressedOrShelved && includeServerSideSuppressedOrShelvedFilter)
        };

        return filter;
    }

    private static async Task<OpcUaEventFilterDefinition> CreateEventFilterDefinitionAsync(
        ISession session,
        NodeId eventTypeNodeId,
        OpcUaEventSelectClauseMode selectClauseMode,
        ushort? severityAtLeast,
        bool ignoreSuppressedOrShelved,
        CancellationToken ct)
    {
        if (ignoreSuppressedOrShelved)
        {
            var isAlarmConditionType = await session.NodeCache
                .IsTypeOfAsync(eventTypeNodeId, ObjectTypeIds.AlarmConditionType, ct)
                .ConfigureAwait(false);

            if (!isAlarmConditionType)
            {
                throw new InvalidOperationException(
                    "IgnoreSuppressedOrShelved is only supported for AlarmConditionType events or their subtypes.");
            }
        }

        var descriptors = selectClauseMode switch
        {
            OpcUaEventSelectClauseMode.Fixed => CreateFixedSelectClauseDescriptors(ignoreSuppressedOrShelved),
            _ => await ConstructDynamicSelectClausesAsync(session, eventTypeNodeId, ignoreSuppressedOrShelved, ct)
                .ConfigureAwait(false)
        };

        return new OpcUaEventFilterDefinition(
            selectClauseMode,
            eventTypeNodeId,
            descriptors,
            severityAtLeast,
            ignoreSuppressedOrShelved);
    }

    private static IReadOnlyList<OpcUaEventSelectClauseDescriptor> CreateFixedSelectClauseDescriptors(
        bool includeSuppressedOrShelved)
    {
        var descriptors = new List<OpcUaEventSelectClauseDescriptor>();
        var knownKeys = new HashSet<string>(StringComparer.Ordinal);

        AddSelectClauseDescriptor(
            descriptors,
            knownKeys,
            OpcUaEventFieldKeys.EventId,
            OpcUaEventFieldKeys.EventId,
            CreateEventOperand(ObjectTypeIds.BaseEventType, Attributes.Value, BrowseNames.EventId));
        AddSelectClauseDescriptor(
            descriptors,
            knownKeys,
            OpcUaEventFieldKeys.EventType,
            OpcUaEventFieldKeys.EventType,
            CreateEventOperand(ObjectTypeIds.BaseEventType, Attributes.Value, BrowseNames.EventType));
        AddSelectClauseDescriptor(
            descriptors,
            knownKeys,
            OpcUaEventFieldKeys.SourceNode,
            OpcUaEventFieldKeys.SourceNode,
            CreateEventOperand(ObjectTypeIds.BaseEventType, Attributes.Value, BrowseNames.SourceNode));
        AddSelectClauseDescriptor(
            descriptors,
            knownKeys,
            OpcUaEventFieldKeys.SourceName,
            OpcUaEventFieldKeys.SourceName,
            CreateEventOperand(ObjectTypeIds.BaseEventType, Attributes.Value, BrowseNames.SourceName));
        AddSelectClauseDescriptor(
            descriptors,
            knownKeys,
            OpcUaEventFieldKeys.Time,
            OpcUaEventFieldKeys.Time,
            CreateEventOperand(ObjectTypeIds.BaseEventType, Attributes.Value, BrowseNames.Time));
        AddSelectClauseDescriptor(
            descriptors,
            knownKeys,
            OpcUaEventFieldKeys.ReceiveTime,
            OpcUaEventFieldKeys.ReceiveTime,
            CreateEventOperand(ObjectTypeIds.BaseEventType, Attributes.Value, BrowseNames.ReceiveTime));
        AddSelectClauseDescriptor(
            descriptors,
            knownKeys,
            OpcUaEventFieldKeys.Message,
            OpcUaEventFieldKeys.Message,
            CreateEventOperand(ObjectTypeIds.BaseEventType, Attributes.Value, BrowseNames.Message));
        AddSelectClauseDescriptor(
            descriptors,
            knownKeys,
            OpcUaEventFieldKeys.Severity,
            OpcUaEventFieldKeys.Severity,
            CreateEventOperand(ObjectTypeIds.BaseEventType, Attributes.Value, BrowseNames.Severity));
        AddSelectClauseDescriptor(
            descriptors,
            knownKeys,
            OpcUaEventFieldKeys.ConditionId,
            OpcUaEventFieldKeys.ConditionId,
            CreateEventOperand(ObjectTypeIds.ConditionType, Attributes.Value, "ConditionId"));
        AddSelectClauseDescriptor(
            descriptors,
            knownKeys,
            OpcUaEventFieldKeys.ConditionName,
            OpcUaEventFieldKeys.ConditionName,
            CreateEventOperand(ObjectTypeIds.ConditionType, Attributes.Value, BrowseNames.ConditionName));
        AddSelectClauseDescriptor(
            descriptors,
            knownKeys,
            OpcUaEventFieldKeys.Retain,
            OpcUaEventFieldKeys.Retain,
            CreateEventOperand(ObjectTypeIds.ConditionType, Attributes.Value, BrowseNames.Retain));
        AddSelectClauseDescriptor(
            descriptors,
            knownKeys,
            OpcUaEventFieldKeys.EnabledStateId,
            OpcUaEventFieldKeys.EnabledStateId,
            CreateEventOperand(ObjectTypeIds.ConditionType, Attributes.Value, BrowseNames.EnabledState, BrowseNames.Id));
        AddSelectClauseDescriptor(
            descriptors,
            knownKeys,
            OpcUaEventFieldKeys.ActiveStateId,
            OpcUaEventFieldKeys.ActiveStateId,
            CreateEventOperand(ObjectTypeIds.AlarmConditionType, Attributes.Value, BrowseNames.ActiveState, BrowseNames.Id));
        AddSelectClauseDescriptor(
            descriptors,
            knownKeys,
            OpcUaEventFieldKeys.AckedStateId,
            OpcUaEventFieldKeys.AckedStateId,
            CreateEventOperand(
                ObjectTypeIds.AcknowledgeableConditionType,
                Attributes.Value,
                BrowseNames.AckedState,
                BrowseNames.Id));

        if (includeSuppressedOrShelved)
        {
            AddSelectClauseDescriptor(
                descriptors,
                knownKeys,
                OpcUaEventFieldKeys.SuppressedOrShelved,
                OpcUaEventFieldKeys.SuppressedOrShelved,
                CreateEventOperand(ObjectTypeIds.AlarmConditionType, Attributes.Value, BrowseNames.SuppressedOrShelved));
            AddSelectClauseDescriptor(
                descriptors,
                knownKeys,
                OpcUaEventFieldKeys.SuppressedStateId,
                OpcUaEventFieldKeys.SuppressedStateId,
                CreateEventOperand(ObjectTypeIds.AlarmConditionType, Attributes.Value, BrowseNames.SuppressedState, BrowseNames.Id));
            AddSelectClauseDescriptor(
                descriptors,
                knownKeys,
                OpcUaEventFieldKeys.ShelvingStateCurrentState,
                OpcUaEventFieldKeys.ShelvingStateCurrentState,
                CreateEventOperand(ObjectTypeIds.AlarmConditionType, Attributes.Value, BrowseNames.ShelvingState, BrowseNames.CurrentState));
        }

        return descriptors;
    }

    private static async Task<IReadOnlyList<OpcUaEventSelectClauseDescriptor>> ConstructDynamicSelectClausesAsync(
        ISession session,
        NodeId eventTypeNodeId,
        bool includeSuppressedOrShelved,
        CancellationToken ct)
    {
        var descriptors = new List<OpcUaEventSelectClauseDescriptor>();
        var knownKeys = new HashSet<string>(StringComparer.Ordinal);

        AddSelectClauseDescriptor(
            descriptors,
            knownKeys,
            OpcUaEventFieldKeys.NodeId,
            OpcUaEventFieldKeys.NodeId,
            CreateNodeIdEventOperand(ObjectTypeIds.BaseEventType));

        var superTypes = await BrowseSuperTypesAsync(session, eventTypeNodeId, ct).ConfigureAwait(false);
        var visitedNodes = new HashSet<string>(StringComparer.Ordinal);
        var parentPath = new QualifiedNameCollection();

        // Walk the inheritance tree from the top-most supertype down so base event fields appear
        // first and derived-type fields are appended deterministically.
        for (var i = superTypes.Count - 1; i >= 0; i--)
        {
            await CollectEventFieldsAsync(
                    session,
                    superTypes[i],
                    parentPath,
                    descriptors,
                    knownKeys,
                    visitedNodes,
                    ct)
                .ConfigureAwait(false);
        }

        await CollectEventFieldsAsync(
                session,
                eventTypeNodeId,
                parentPath,
                descriptors,
                knownKeys,
                visitedNodes,
                ct)
            .ConfigureAwait(false);

        if (includeSuppressedOrShelved)
        {
            AddSelectClauseDescriptor(
                descriptors,
                knownKeys,
                OpcUaEventFieldKeys.SuppressedOrShelved,
                OpcUaEventFieldKeys.SuppressedOrShelved,
                CreateEventOperand(ObjectTypeIds.AlarmConditionType, Attributes.Value, BrowseNames.SuppressedOrShelved));
            AddSelectClauseDescriptor(
                descriptors,
                knownKeys,
                OpcUaEventFieldKeys.SuppressedStateId,
                OpcUaEventFieldKeys.SuppressedStateId,
                CreateEventOperand(ObjectTypeIds.AlarmConditionType, Attributes.Value, BrowseNames.SuppressedState, BrowseNames.Id));
            AddSelectClauseDescriptor(
                descriptors,
                knownKeys,
                OpcUaEventFieldKeys.ShelvingStateCurrentState,
                OpcUaEventFieldKeys.ShelvingStateCurrentState,
                CreateEventOperand(ObjectTypeIds.AlarmConditionType, Attributes.Value, BrowseNames.ShelvingState, BrowseNames.CurrentState));
        }

        return descriptors;
    }

    private static async Task CollectEventFieldsAsync(
        ISession session,
        NodeId nodeId,
        QualifiedNameCollection parentPath,
        ICollection<OpcUaEventSelectClauseDescriptor> descriptors,
        ISet<string> knownKeys,
        ISet<string> visitedNodes,
        CancellationToken ct)
    {
        var children = await BrowseAsync(
                session,
                nodeId,
                BrowseDirection.Forward,
                ReferenceTypeIds.Aggregates,
                true,
                (uint)(NodeClass.Object | NodeClass.Variable),
                ct)
            .ConfigureAwait(false);

        if (children == null || children.Count == 0)
        {
            return;
        }

        foreach (var child in children)
        {
            if (child.NodeId.IsAbsolute)
            {
                continue;
            }

            var browsePath = new QualifiedNameCollection(parentPath)
            {
                child.BrowseName
            };

            var key = GetSelectClauseKey(browsePath);
            var displayName = GetSelectClauseDisplayName(browsePath);
            var attributeId = child.NodeClass == NodeClass.Variable
                ? Attributes.Value
                : Attributes.NodeId;

            AddSelectClauseDescriptor(
                descriptors,
                knownKeys,
                key,
                displayName,
                new SimpleAttributeOperand
                {
                    TypeDefinitionId = ObjectTypeIds.BaseEventType,
                    BrowsePath = browsePath,
                    AttributeId = attributeId
                });

            var targetId = ExpandedNodeId.ToNodeId(child.NodeId, session.NamespaceUris);
            if (targetId == null)
            {
                continue;
            }

            var targetKey = targetId.ToString();
            if (string.IsNullOrWhiteSpace(targetKey) || !visitedNodes.Add(targetKey))
            {
                continue;
            }

            await CollectEventFieldsAsync(
                    session,
                    targetId,
                    browsePath,
                    descriptors,
                    knownKeys,
                    visitedNodes,
                    ct)
                .ConfigureAwait(false);
        }
    }

    private static async Task<IReadOnlyList<NodeId>> BrowseSuperTypesAsync(
        ISession session,
        NodeId eventTypeNodeId,
        CancellationToken ct)
    {
        var superTypes = new List<NodeId>();
        var visitedNodes = new HashSet<string>(StringComparer.Ordinal);
        var currentNodeId = eventTypeNodeId;

        while (true)
        {
            var references = await BrowseAsync(
                    session,
                    currentNodeId,
                    BrowseDirection.Inverse,
                    ReferenceTypeIds.HasSubtype,
                    false,
                    (uint)NodeClass.ObjectType,
                    ct)
                .ConfigureAwait(false);

            var superTypeId = references
                .Select(reference => ExpandedNodeId.ToNodeId(reference.NodeId, session.NamespaceUris))
                .FirstOrDefault(static nodeId => nodeId != null);

            if (superTypeId == null)
            {
                break;
            }

            var key = superTypeId.ToString();
            if (string.IsNullOrWhiteSpace(key) || !visitedNodes.Add(key))
            {
                break;
            }

            superTypes.Add(superTypeId);
            currentNodeId = superTypeId;
        }

        return superTypes;
    }

    private static async Task<ReferenceDescriptionCollection> BrowseAsync(
        ISession session,
        NodeId nodeId,
        BrowseDirection browseDirection,
        NodeId referenceTypeId,
        bool includeSubtypes,
        uint nodeClassMask,
        CancellationToken ct)
    {
        var browser = new Browser(session)
        {
            BrowseDirection = browseDirection,
            ReferenceTypeId = referenceTypeId,
            IncludeSubtypes = includeSubtypes,
            NodeClassMask = nodeClassMask,
            ResultMask = (uint)BrowseResultMask.All,
            ContinueUntilDone = true
        };

        return await browser.BrowseAsync(nodeId, ct).ConfigureAwait(false);
    }

    private static void AddSelectClauseDescriptor(
        ICollection<OpcUaEventSelectClauseDescriptor> descriptors,
        ISet<string> knownKeys,
        string key,
        string displayName,
        SimpleAttributeOperand operand)
    {
        if (!knownKeys.Add(key))
        {
            return;
        }

        descriptors.Add(new OpcUaEventSelectClauseDescriptor(key, displayName, operand));
    }

    private static ContentFilter CreateEventWhereClause(
        NodeId eventTypeNodeId,
        ushort? severityAtLeast,
        bool ignoreSuppressedOrShelved)
    {
        if (!severityAtLeast.HasValue && !ignoreSuppressedOrShelved)
        {
            return new ContentFilter
            {
                Elements = new ContentFilterElementCollection
                {
                    CreateOfTypeFilterElement(eventTypeNodeId)
                }
            };
        }

        if (severityAtLeast.HasValue && !ignoreSuppressedOrShelved)
        {
            return new ContentFilter
            {
                Elements = new ContentFilterElementCollection
                {
                    CreateContentFilterElement(
                        FilterOperator.And,
                        new ElementOperand { Index = 1 },
                        new ElementOperand { Index = 2 }),
                    CreateOfTypeFilterElement(eventTypeNodeId),
                    CreateSeverityFilterElement(severityAtLeast.Value)
                }
            };
        }

        if (!severityAtLeast.HasValue)
        {
            return new ContentFilter
            {
                Elements = new ContentFilterElementCollection
                {
                    CreateContentFilterElement(
                        FilterOperator.And,
                        new ElementOperand { Index = 1 },
                        new ElementOperand { Index = 2 }),
                    CreateOfTypeFilterElement(eventTypeNodeId),
                    CreateSuppressedOrShelvedFilterElement(false)
                }
            };
        }

        return new ContentFilter
        {
            Elements = new ContentFilterElementCollection
            {
                CreateContentFilterElement(
                    FilterOperator.And,
                    new ElementOperand { Index = 1 },
                    new ElementOperand { Index = 2 }),
                CreateOfTypeFilterElement(eventTypeNodeId),
                CreateContentFilterElement(
                    FilterOperator.And,
                    new ElementOperand { Index = 3 },
                    new ElementOperand { Index = 4 }),
                CreateSeverityFilterElement(severityAtLeast.Value),
                CreateSuppressedOrShelvedFilterElement(false)
            }
        };
    }

    private static ContentFilterElement CreateOfTypeFilterElement(NodeId eventTypeNodeId)
    {
        return CreateContentFilterElement(
            FilterOperator.OfType,
            new LiteralOperand
            {
                Value = new Variant(eventTypeNodeId)
            });
    }

    private static ContentFilterElement CreateSeverityFilterElement(ushort severityAtLeast)
    {
        return CreateContentFilterElement(
            FilterOperator.GreaterThanOrEqual,
            CreateEventOperand(ObjectTypeIds.BaseEventType, Attributes.Value, BrowseNames.Severity),
            new LiteralOperand
            {
                Value = new Variant(severityAtLeast)
            });
    }

    private static ContentFilterElement CreateSuppressedOrShelvedFilterElement(bool isSuppressedOrShelved)
    {
        return CreateContentFilterElement(
            FilterOperator.Equals,
            CreateEventOperand(ObjectTypeIds.AlarmConditionType, Attributes.Value, BrowseNames.SuppressedOrShelved),
            new LiteralOperand
            {
                Value = new Variant(isSuppressedOrShelved)
            });
    }

    private static SimpleAttributeOperand CreateNodeIdEventOperand(NodeId typeDefinitionId)
    {
        return new SimpleAttributeOperand
        {
            TypeDefinitionId = typeDefinitionId,
            BrowsePath = new QualifiedNameCollection(),
            AttributeId = Attributes.NodeId
        };
    }

    private static SimpleAttributeOperand CreateEventOperand(
        NodeId typeDefinitionId,
        uint attributeId,
        params string[] browseNames)
    {
        var browsePath = new QualifiedNameCollection();
        foreach (var browseName in browseNames)
        {
            browsePath.Add(new QualifiedName(browseName));
        }

        return new SimpleAttributeOperand
        {
            TypeDefinitionId = typeDefinitionId,
            BrowsePath = browsePath,
            AttributeId = attributeId
        };
    }

    private static SimpleAttributeOperand CloneSimpleAttributeOperand(SimpleAttributeOperand operand)
    {
        return new SimpleAttributeOperand
        {
            TypeDefinitionId = operand.TypeDefinitionId,
            BrowsePath = operand.BrowsePath == null
                ? new QualifiedNameCollection()
                : new QualifiedNameCollection(operand.BrowsePath),
            AttributeId = operand.AttributeId
        };
    }

    private static string GetSelectClauseKey(QualifiedNameCollection browsePath)
    {
        var knownKey = TryGetKnownEventFieldKey(browsePath);
        if (!string.IsNullOrWhiteSpace(knownKey))
        {
            return knownKey;
        }

        return GetBrowsePathKey(browsePath);
    }

    private static string GetSelectClauseDisplayName(QualifiedNameCollection browsePath)
    {
        var knownKey = TryGetKnownEventFieldKey(browsePath);
        if (!string.IsNullOrWhiteSpace(knownKey))
        {
            return knownKey;
        }

        return GetBrowsePathDisplayName(browsePath);
    }

    private static string GetBrowsePathKey(QualifiedNameCollection browsePath)
    {
        if (browsePath == null || browsePath.Count == 0)
        {
            return OpcUaEventFieldKeys.NodeId;
        }

        return string.Join(
            ".",
            browsePath
                .Where(static item => !QualifiedName.IsNull(item))
                .Select(GetQualifiedNameKey));
    }

    private static string GetBrowsePathDisplayName(QualifiedNameCollection browsePath)
    {
        if (browsePath == null || browsePath.Count == 0)
        {
            return OpcUaEventFieldKeys.NodeId;
        }

        return string.Join(
            ".",
            browsePath
                .Where(static item => !QualifiedName.IsNull(item))
                .Select(GetQualifiedNameDisplayName));
    }

    private static string? TryGetKnownEventFieldKey(QualifiedNameCollection browsePath)
    {
        if (browsePath == null || browsePath.Count == 0)
        {
            return OpcUaEventFieldKeys.NodeId;
        }

        var normalizedSegments = new List<string>(browsePath.Count);

        foreach (var segment in browsePath)
        {
            if (QualifiedName.IsNull(segment) || segment.NamespaceIndex != 0)
            {
                return null;
            }

            normalizedSegments.Add(segment.Name);
        }

        return string.Join(".", normalizedSegments) switch
        {
            OpcUaEventFieldKeys.EventId => OpcUaEventFieldKeys.EventId,
            OpcUaEventFieldKeys.EventType => OpcUaEventFieldKeys.EventType,
            OpcUaEventFieldKeys.SourceNode => OpcUaEventFieldKeys.SourceNode,
            OpcUaEventFieldKeys.SourceName => OpcUaEventFieldKeys.SourceName,
            OpcUaEventFieldKeys.Time => OpcUaEventFieldKeys.Time,
            OpcUaEventFieldKeys.ReceiveTime => OpcUaEventFieldKeys.ReceiveTime,
            OpcUaEventFieldKeys.Message => OpcUaEventFieldKeys.Message,
            OpcUaEventFieldKeys.Severity => OpcUaEventFieldKeys.Severity,
            OpcUaEventFieldKeys.ConditionId => OpcUaEventFieldKeys.ConditionId,
            OpcUaEventFieldKeys.ConditionName => OpcUaEventFieldKeys.ConditionName,
            OpcUaEventFieldKeys.Retain => OpcUaEventFieldKeys.Retain,
            OpcUaEventFieldKeys.EnabledStateId => OpcUaEventFieldKeys.EnabledStateId,
            OpcUaEventFieldKeys.ActiveStateId => OpcUaEventFieldKeys.ActiveStateId,
            OpcUaEventFieldKeys.AckedStateId => OpcUaEventFieldKeys.AckedStateId,
            OpcUaEventFieldKeys.SuppressedOrShelved => OpcUaEventFieldKeys.SuppressedOrShelved,
            OpcUaEventFieldKeys.SuppressedStateId => OpcUaEventFieldKeys.SuppressedStateId,
            OpcUaEventFieldKeys.ShelvingStateCurrentState => OpcUaEventFieldKeys.ShelvingStateCurrentState,
            _ => null
        };
    }

    private static string GetQualifiedNameKey(QualifiedName qualifiedName)
    {
        return qualifiedName.NamespaceIndex == 0
            ? qualifiedName.Name
            : string.Format(
                CultureInfo.InvariantCulture,
                "ns={0}:{1}",
                qualifiedName.NamespaceIndex,
                qualifiedName.Name);
    }

    private static string GetQualifiedNameDisplayName(QualifiedName qualifiedName)
    {
        return GetQualifiedNameKey(qualifiedName);
    }

    private static bool IsRefreshBoundaryEvent(string eventTypeNodeId)
    {
        return string.Equals(eventTypeNodeId, ObjectTypeIds.RefreshStartEventType.ToString(), StringComparison.Ordinal) ||
               string.Equals(eventTypeNodeId, ObjectTypeIds.RefreshEndEventType.ToString(), StringComparison.Ordinal);
    }

    private static ContentFilterElement CreateContentFilterElement(
        FilterOperator filterOperator,
        params FilterOperand[] operands)
    {
        var element = new ContentFilterElement
        {
            FilterOperator = filterOperator
        };
        element.SetOperands(operands);
        return element;
    }

    private static byte[] GetByteStringField(
        IReadOnlyDictionary<string, object?> fields,
        string key)
    {
        return GetFieldValue(fields, key) as byte[] ?? Array.Empty<byte>();
    }

    private static string? GetNodeIdField(
        IReadOnlyDictionary<string, object?> fields,
        string key)
    {
        return GetNodeIdField(GetFieldValue(fields, key));
    }

    private static string? GetNodeIdField(object? value)
    {
        return value switch
        {
            NodeId nodeId => nodeId.ToString(),
            ExpandedNodeId expandedNodeId => expandedNodeId.ToString(),
            string text when !string.IsNullOrWhiteSpace(text) => text.Trim(),
            _ => null
        };
    }

    private static string? GetStringField(
        IReadOnlyDictionary<string, object?> fields,
        string key)
    {
        return GetStringField(GetFieldValue(fields, key));
    }

    private static string? GetStringField(object? value)
    {
        return value switch
        {
            null => null,
            string text when !string.IsNullOrWhiteSpace(text) => text.Trim(),
            LocalizedText localizedText when !LocalizedText.IsNullOrEmpty(localizedText) => localizedText.Text,
            QualifiedName qualifiedName when !QualifiedName.IsNull(qualifiedName) => qualifiedName.Name,
            _ => value.ToString()
        };
    }

    private static DateTime GetDateTimeField(
        IReadOnlyDictionary<string, object?> fields,
        string key)
    {
        return GetDateTimeField(GetFieldValue(fields, key));
    }

    private static DateTime GetDateTimeField(object? value)
    {
        return value switch
        {
            DateTime dateTime => dateTime,
            _ => DateTime.MinValue
        };
    }

    private static ushort GetUInt16Field(
        IReadOnlyDictionary<string, object?> fields,
        string key)
    {
        return GetUInt16Field(GetFieldValue(fields, key));
    }

    private static ushort GetUInt16Field(object? value)
    {
        return value switch
        {
            ushort number => number,
            byte number => number,
            short number when number >= 0 => checked((ushort)number),
            int number when number >= 0 && number <= ushort.MaxValue => checked((ushort)number),
            long number when number >= 0 && number <= ushort.MaxValue => checked((ushort)number),
            _ => 0
        };
    }

    private static bool? GetBooleanField(
        IReadOnlyDictionary<string, object?> fields,
        string key)
    {
        return GetBooleanField(GetFieldValue(fields, key));
    }

    private static bool? GetBooleanField(object? value)
    {
        return value switch
        {
            bool flag => flag,
            _ => null
        };
    }

    private static object? NormalizeEventFieldValue(object? value)
    {
        return value switch
        {
            null => null,
            byte[] bytes => bytes.ToArray(),
            LocalizedText localizedText when !LocalizedText.IsNullOrEmpty(localizedText) => localizedText.Text,
            QualifiedName qualifiedName when !QualifiedName.IsNull(qualifiedName) => qualifiedName.Name,
            NodeId nodeId => nodeId.ToString(),
            ExpandedNodeId expandedNodeId => expandedNodeId.ToString(),
            _ => value
        };
    }

    private static object? GetFieldValue(
        IReadOnlyDictionary<string, object?> fields,
        string key)
    {
        if (fields == null)
        {
            return null;
        }

        return fields.TryGetValue(key, out var value)
            ? value
            : null;
    }

    private static object? GetFieldValue(IList<Variant> fields, int index)
    {
        if (fields == null || index < 0 || index >= fields.Count)
        {
            return null;
        }

        return fields[index].Value;
    }

    private static async Task RollbackEventMonitoredItemsAsync(
        OpcUaEventSubscriptionHandle subscription,
        IReadOnlyList<OpcUaEventMonitoredItemRegistration> monitoredItems,
        CancellationToken ct)
    {
        subscription.Subscription.RemoveItems(monitoredItems.Select(static item => item.MonitoredItem));
        OpcUaEventSubscriptionHandle.DetachHandlers(monitoredItems);

        try
        {
            await subscription.Subscription.ApplyChangesAsync(ct).ConfigureAwait(false);
        }
        catch when (!ct.IsCancellationRequested)
        {
            // Best effort rollback.
        }
    }

    private static IReadOnlyDictionary<string, object?> BuildReadResults(
        IReadOnlyList<string> normalizedNodeIds,
        IList<DataValue> dataValues)
    {
        var successfulValues = new Dictionary<string, object?>(normalizedNodeIds.Count, StringComparer.Ordinal);
        var failures = new List<OpcUaBatchOperationFailure>();

        for (var i = 0; i < normalizedNodeIds.Count; i++)
        {
            var dataValue = dataValues[i];
            var statusCode = dataValue?.StatusCode ?? StatusCodes.BadUnexpectedError;

            if (StatusCode.IsBad(statusCode))
            {
                failures.Add(CreateBatchOperationFailure(
                    normalizedNodeIds[i],
                    statusCode,
                    "read"));
                continue;
            }

            successfulValues[normalizedNodeIds[i]] = dataValue?.Value;
        }

        if (failures.Count > 0)
        {
            throw new OpcUaBatchReadException(successfulValues, failures);
        }

        return successfulValues;
    }

    private static void EnsureWriteResultsSucceeded(
        IReadOnlyList<KeyValuePair<string, object?>> normalizedNodeValues,
        IList<StatusCode> results)
    {
        var successfulNodeIds = new List<string>(normalizedNodeValues.Count);
        var failures = new List<OpcUaBatchOperationFailure>();

        for (var i = 0; i < normalizedNodeValues.Count; i++)
        {
            var statusCode = results[i];
            if (StatusCode.IsBad(statusCode))
            {
                failures.Add(CreateBatchOperationFailure(
                    normalizedNodeValues[i].Key,
                    statusCode,
                    "write"));
                continue;
            }

            successfulNodeIds.Add(normalizedNodeValues[i].Key);
        }

        if (failures.Count > 0)
        {
            throw new OpcUaBatchWriteException(successfulNodeIds, failures);
        }
    }

    private static OpcUaBatchOperationFailure CreateBatchOperationFailure(
        string nodeId,
        StatusCode statusCode,
        string operation)
    {
        var code = statusCode.Code;
        var symbolicId = StatusCodes.LookupSymbolicId(code);
        var message = string.IsNullOrWhiteSpace(symbolicId)
            ? $"Failed to {operation} node '{nodeId}'."
            : $"Failed to {operation} node '{nodeId}' ({symbolicId}).";

        return new OpcUaBatchOperationFailure(nodeId, code, symbolicId, message);
    }

    private static bool CanRetryWithoutSuppressedOrShelvedServerFilter(Exception exception)
    {
        if (exception is AggregateException aggregateException)
        {
            return aggregateException.Flatten().InnerExceptions.Any(CanRetryWithoutSuppressedOrShelvedServerFilter);
        }

        if (exception is ServiceResultException serviceResultException)
        {
            return s_retryableSuppressedOrShelvedFilterStatusCodes.Contains(serviceResultException.StatusCode);
        }

        return false;
    }

    private static bool ShouldIgnoreSuppressedOrShelvedEvent(
        IReadOnlyDictionary<string, object?> fields)
    {
        if (GetBooleanField(fields, OpcUaEventFieldKeys.SuppressedOrShelved) == true)
        {
            return true;
        }

        if (GetBooleanField(fields, OpcUaEventFieldKeys.SuppressedStateId) == true)
        {
            return true;
        }

        var shelvingState = GetStringField(fields, OpcUaEventFieldKeys.ShelvingStateCurrentState);
        return !string.IsNullOrWhiteSpace(shelvingState) &&
               !string.Equals(shelvingState, "Unshelved", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task EnsureSourceNodeSupportsEventsAsync(
        ISession session,
        string sourceNodeId,
        CancellationToken ct)
    {
        var nodeToRead = ParseNodeId(sourceNodeId);
        var nodesToRead = new ReadValueIdCollection
        {
            new()
            {
                NodeId = nodeToRead,
                AttributeId = Attributes.NodeClass
            },
            new()
            {
                NodeId = nodeToRead,
                AttributeId = Attributes.EventNotifier
            }
        };

        var response = await session.ReadAsync(
                null,
                0,
                TimestampsToReturn.Neither,
                nodesToRead,
                ct)
            .ConfigureAwait(false);

        var results = response.Results;
        if (results == null || results.Count != nodesToRead.Count)
        {
            throw new InvalidOperationException("The OPC UA server returned an unexpected number of event source read results.");
        }

        if (StatusCode.IsBad(results[0].StatusCode))
        {
            throw new ServiceResultException(
                results[0].StatusCode,
                $"Failed to validate event source '{sourceNodeId}'.");
        }

        var nodeClass = results[0].Value switch
        {
            NodeClass value => value,
            _ => (NodeClass)Convert.ToInt32(results[0].Value, CultureInfo.InvariantCulture)
        };

        if (nodeClass != NodeClass.Object && nodeClass != NodeClass.View)
        {
            throw new InvalidOperationException(
                $"Node '{sourceNodeId}' is not an event source. Event subscriptions require an Object or View node.");
        }

        if (StatusCode.IsBad(results[1].StatusCode))
        {
            throw new InvalidOperationException(
                $"Node '{sourceNodeId}' does not expose the EventNotifier attribute required for alarm event subscriptions.");
        }

        var eventNotifier = results[1].Value switch
        {
            byte value => value,
            _ => Convert.ToByte(results[1].Value ?? 0, CultureInfo.InvariantCulture)
        };

        if ((eventNotifier & EventNotifiers.SubscribeToEvents) == 0)
        {
            throw new InvalidOperationException(
                $"Node '{sourceNodeId}' does not support event subscriptions.");
        }
    }

    private static async Task<NodeId> NormalizeAndValidateEventTypeNodeIdAsync(
        ISession session,
        string eventTypeNodeId,
        CancellationToken ct)
    {
        var normalizedEventTypeNodeId = NormalizeNodeId(eventTypeNodeId, nameof(eventTypeNodeId));
        var parsedEventTypeNodeId = ParseNodeId(normalizedEventTypeNodeId);

        await session.FetchTypeTreeAsync(new ExpandedNodeId(parsedEventTypeNodeId), ct).ConfigureAwait(false);

        var isConditionType = await session.NodeCache
            .IsTypeOfAsync(parsedEventTypeNodeId, ObjectTypeIds.ConditionType, ct)
            .ConfigureAwait(false);

        if (!isConditionType)
        {
            throw new InvalidOperationException(
                $"Event type '{normalizedEventTypeNodeId}' is not a subtype of ConditionType. Only condition/alarm events are supported.");
        }

        return parsedEventTypeNodeId;
    }

    private static async Task CloseConnectionAsync(
        OpcUaClientConnection connection,
        CancellationToken ct)
    {
        try
        {
            if (connection.Configuration.CertificateValidator != null)
            {
                connection.Configuration.CertificateValidator.CertificateValidation -=
                    connection.CertificateValidationHandler;
            }

            await connection.Session.CloseAsync(false, ct).ConfigureAwait(false);
        }
        catch
        {
            // If the server is already unavailable, we still want to dispose the local session.
        }
        finally
        {
            connection.Session.Dispose();
        }
    }

    private async Task DisconnectCoreUnsafeAsync(CancellationToken ct)
    {
        if (_connection == null)
        {
            return;
        }

        await UnsubscribeAllUnsafeAsync(ct).ConfigureAwait(false);
        await UnsubscribeAllEventSubscriptionsUnsafeAsync(ct).ConfigureAwait(false);
        await CloseConnectionAsync(_connection, ct).ConfigureAwait(false);
        _connection = null;
    }

    private ISession GetRequiredSession()
    {
        if (_connection?.Session is { Connected: true } session)
        {
            return session;
        }

        throw new InvalidOperationException("The OPC UA client is not connected.");
    }

    private static string NormalizeNodeId(string nodeId, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            throw new ArgumentException("NodeId is required.", parameterName);
        }

        return nodeId.Trim();
    }

    private static OpcUaNode NormalizeNode(OpcUaNode node, string parameterName)
    {
        if (node == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        return new OpcUaNode(node.NodeId, node.DisplayName);
    }

    private static List<string> NormalizeNodeIds(IEnumerable<string> nodeIds)
    {
        return NormalizeNodeIds(nodeIds, nameof(nodeIds));
    }

    private static List<string> NormalizeNodeIds(
        IEnumerable<string> nodeIds,
        string parameterName)
    {
        if (nodeIds == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        var normalizedNodeIds = new List<string>();
        var uniqueNodeIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var nodeId in nodeIds)
        {
            var normalizedNodeId = NormalizeNodeId(nodeId, parameterName);
            if (!uniqueNodeIds.Add(normalizedNodeId))
            {
                throw new ArgumentException(
                    $"Duplicate nodeId '{normalizedNodeId}' is not supported in batch operations.",
                    parameterName);
            }

            normalizedNodeIds.Add(normalizedNodeId);
        }

        if (normalizedNodeIds.Count == 0)
        {
            throw new ArgumentException("At least one NodeId is required.", parameterName);
        }

        return normalizedNodeIds;
    }

    private static List<OpcUaNode> NormalizeNodes(
        IEnumerable<OpcUaNode> nodes,
        string parameterName)
    {
        if (nodes == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        var normalizedNodes = new List<OpcUaNode>();
        var uniqueNodeIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var node in nodes)
        {
            var normalizedNode = NormalizeNode(node, parameterName);
            if (!uniqueNodeIds.Add(normalizedNode.NodeId))
            {
                throw new ArgumentException(
                    $"Duplicate nodeId '{normalizedNode.NodeId}' is not supported in batch operations.",
                    parameterName);
            }

            normalizedNodes.Add(normalizedNode);
        }

        if (normalizedNodes.Count == 0)
        {
            throw new ArgumentException("At least one node is required.", parameterName);
        }

        return normalizedNodes;
    }

    private static List<KeyValuePair<string, object?>> NormalizeNodeValues(
        IReadOnlyDictionary<string, object?> nodeValues)
    {
        if (nodeValues == null)
        {
            throw new ArgumentNullException(nameof(nodeValues));
        }

        if (nodeValues.Count == 0)
        {
            throw new ArgumentException("At least one node value is required.", nameof(nodeValues));
        }

        var normalizedNodeValues = new List<KeyValuePair<string, object?>>(nodeValues.Count);
        var uniqueNodeIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var nodeValue in nodeValues)
        {
            var normalizedNodeId = NormalizeNodeId(nodeValue.Key, nameof(nodeValues));
            if (!uniqueNodeIds.Add(normalizedNodeId))
            {
                throw new ArgumentException(
                    $"Duplicate nodeId '{normalizedNodeId}' is not supported in batch operations.",
                    nameof(nodeValues));
            }

            normalizedNodeValues.Add(new KeyValuePair<string, object?>(normalizedNodeId, nodeValue.Value));
        }

        return normalizedNodeValues;
    }

    private static List<KeyValuePair<string, object?>> NormalizeNodeValues(
        IReadOnlyDictionary<OpcUaNode, object?> nodeValues)
    {
        if (nodeValues == null)
        {
            throw new ArgumentNullException(nameof(nodeValues));
        }

        if (nodeValues.Count == 0)
        {
            throw new ArgumentException("At least one node value is required.", nameof(nodeValues));
        }

        var normalizedNodeValues = new List<KeyValuePair<string, object?>>(nodeValues.Count);
        var uniqueNodeIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var nodeValue in nodeValues)
        {
            var normalizedNode = NormalizeNode(nodeValue.Key, nameof(nodeValues));
            if (!uniqueNodeIds.Add(normalizedNode.NodeId))
            {
                throw new ArgumentException(
                    $"Duplicate nodeId '{normalizedNode.NodeId}' is not supported in batch operations.",
                    nameof(nodeValues));
            }

            normalizedNodeValues.Add(new KeyValuePair<string, object?>(normalizedNode.NodeId, nodeValue.Value));
        }

        return normalizedNodeValues;
    }

    private static VariantCollection NormalizeInputArguments(IEnumerable<object?>? inputArguments)
    {
        var normalizedInputArguments = new VariantCollection();
        if (inputArguments == null)
        {
            return normalizedInputArguments;
        }

        foreach (var inputArgument in inputArguments)
        {
            normalizedInputArguments.Add(new Variant(inputArgument));
        }

        return normalizedInputArguments;
    }

    private static OpcUaSubscriptionBuildRequest CreateDefaultSubscriptionBuildRequest()
    {
        return NormalizeSubscriptionBuildRequest(
            new OpcUaSubscriptionBuildRequest(
                name: null,
                publishingInterval: 1000,
                keepAliveCount: 10,
                lifetimeCount: 60,
                maxNotificationsPerPublish: 0,
                priority: 0,
                publishingEnabled: true));
    }

    private static OpcUaEventSubscriptionBuildRequest CreateDefaultEventSubscriptionBuildRequest(
        Action<OpcUaEventNotification> onEvent)
    {
        return NormalizeEventSubscriptionBuildRequest(
            new OpcUaEventSubscriptionBuildRequest(
                name: null,
                publishingInterval: 1000,
                keepAliveCount: 10,
                lifetimeCount: 60,
                maxNotificationsPerPublish: 0,
                priority: 0,
                publishingEnabled: true,
                eventTypeNode: new OpcUaNode(ObjectTypeIds.AlarmConditionType.ToString()),
                severityAtLeast: null,
                queueSize: 1000,
                discardOldest: true,
                conditionRefreshOnStart: true,
                selectClauseMode: OpcUaEventSelectClauseMode.Dynamic,
                ignoreSuppressedOrShelved: false,
                onEvent: onEvent));
    }

    private static OpcUaSubscriptionBuildRequest NormalizeSubscriptionBuildRequest(
        OpcUaSubscriptionBuildRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.PublishingInterval <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "PublishingInterval must be greater than 0.");
        }

        if (request.KeepAliveCount == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "KeepAliveCount must be greater than 0.");
        }

        if (request.LifetimeCount == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "LifetimeCount must be greater than 0.");
        }

        var normalizedName = string.IsNullOrWhiteSpace(request.Name)
            ? $"subscription-{Guid.NewGuid():N}"
            : request.Name.Trim();

        return new OpcUaSubscriptionBuildRequest(
            normalizedName,
            request.PublishingInterval,
            request.KeepAliveCount,
            request.LifetimeCount,
            request.MaxNotificationsPerPublish,
            request.Priority,
            request.PublishingEnabled);
    }

    private static OpcUaEventSubscriptionBuildRequest NormalizeEventSubscriptionBuildRequest(
        OpcUaEventSubscriptionBuildRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.PublishingInterval <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "PublishingInterval must be greater than 0.");
        }

        if (request.KeepAliveCount == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "KeepAliveCount must be greater than 0.");
        }

        if (request.LifetimeCount == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "LifetimeCount must be greater than 0.");
        }

        var normalizedName = string.IsNullOrWhiteSpace(request.Name)
            ? $"event-subscription-{Guid.NewGuid():N}"
            : request.Name.Trim();

        var normalizedEventTypeNode = NormalizeNode(request.EventTypeNode, nameof(request));

        return new OpcUaEventSubscriptionBuildRequest(
            normalizedName,
            request.PublishingInterval,
            request.KeepAliveCount,
            request.LifetimeCount,
            request.MaxNotificationsPerPublish,
            request.Priority,
            request.PublishingEnabled,
            normalizedEventTypeNode,
            request.SeverityAtLeast,
            request.QueueSize,
            request.DiscardOldest,
            request.ConditionRefreshOnStart,
            request.SelectClauseMode,
            request.IgnoreSuppressedOrShelved,
            request.OnEvent);
    }

    private static List<OpcUaSubscriptionItemDefinition> NormalizeSubscriptionNodes(
        IEnumerable<OpcUaSubscriptionItemDefinition> nodes,
        string parameterName)
    {
        if (nodes == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        var nodeList = nodes.ToList();
        if (nodeList.Count == 0)
        {
            throw new ArgumentException("At least one monitored node is required.", parameterName);
        }

        var normalizedNodes = new List<OpcUaSubscriptionItemDefinition>(nodeList.Count);
        var uniqueNodeIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var node in nodeList)
        {
            if (node == null)
            {
                throw new ArgumentException("Monitored node definition is required.", parameterName);
            }

            if (node.OnChanged == null)
            {
                throw new ArgumentException("A node change callback is required.", parameterName);
            }

            var normalizedNode = NormalizeNode(node.Node, parameterName);
            if (!uniqueNodeIds.Add(normalizedNode.NodeId))
            {
                throw new ArgumentException(
                    $"Duplicate nodeId '{normalizedNode.NodeId}' is not supported in subscriptions.",
                    parameterName);
            }

            var options = node.Options?.Clone() ?? new OpcUaMonitoredItemOptions();
            options.DisplayName = string.IsNullOrWhiteSpace(options.DisplayName)
                ? null
                : options.DisplayName.Trim();

            if (options.SamplingInterval < -1 || options.SamplingInterval > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    "SamplingInterval must be between -1 and Int32.MaxValue.");
            }

            normalizedNodes.Add(new OpcUaSubscriptionItemDefinition(normalizedNode, node.OnChanged, options));
        }

        return normalizedNodes;
    }

    private static int ConvertSamplingInterval(double samplingInterval)
    {
        if (samplingInterval == -1)
        {
            return -1;
        }

        return checked((int)Math.Round(samplingInterval, MidpointRounding.AwayFromZero));
    }

    private static NodeId ParseNodeId(string nodeId)
    {
        return NodeId.Parse(NormalizeNodeId(nodeId, nameof(nodeId)));
    }

    private List<OpcUaSubscriptionHandle> GetSubscriptionsSnapshot()
    {
        lock (_subscriptionsLock)
        {
            return _subscriptions.ToList();
        }
    }

    private List<OpcUaEventSubscriptionHandle> GetEventSubscriptionsSnapshot()
    {
        lock (_subscriptionsLock)
        {
            return _eventSubscriptions.ToList();
        }
    }

    private void RegisterSubscription(OpcUaSubscriptionHandle subscription)
    {
        lock (_subscriptionsLock)
        {
            _subscriptions.Add(subscription);
        }
    }

    private void RegisterEventSubscription(OpcUaEventSubscriptionHandle subscription)
    {
        lock (_subscriptionsLock)
        {
            _eventSubscriptions.Add(subscription);
        }
    }

    private void UnregisterSubscription(OpcUaSubscriptionHandle subscription)
    {
        lock (_subscriptionsLock)
        {
            _subscriptions.Remove(subscription);
        }
    }

    private void UnregisterEventSubscription(OpcUaEventSubscriptionHandle subscription)
    {
        lock (_subscriptionsLock)
        {
            _eventSubscriptions.Remove(subscription);
        }
    }

    private void ReportDiagnostic(
        OpcUaClientDiagnosticKind kind,
        string message,
        Exception? exception = null,
        string? subscriptionName = null,
        string? itemId = null)
    {
        var diagnosticsHandler = _options.DiagnosticsHandler;
        if (diagnosticsHandler == null)
        {
            return;
        }

        try
        {
            diagnosticsHandler(new OpcUaClientDiagnosticEvent(
                kind,
                message,
                exception,
                subscriptionName,
                itemId));
        }
        catch
        {
            // Diagnostics are best effort and must never interfere with the client pipeline.
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
    }
}
