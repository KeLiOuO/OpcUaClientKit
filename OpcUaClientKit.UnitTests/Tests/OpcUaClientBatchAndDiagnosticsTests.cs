using Moq;
using Opc.Ua;
using Opc.Ua.Client;
using OpcUaClientKit.UnitTests.Infrastructure;

namespace OpcUaClientKit.UnitTests.Tests;

public sealed class OpcUaClientBatchAndDiagnosticsTests
{
    [Fact]
    public void BuildReadResults_throws_aggregate_exception_with_partial_successes()
    {
        var nodeIds = new[] { "ns=2;s=Node1", "ns=2;s=Node2", "ns=2;s=Node3" };
        var dataValues = new List<DataValue>
        {
            new(new Variant(1)) { StatusCode = StatusCodes.Good },
            new() { StatusCode = StatusCodes.BadNodeIdUnknown },
            new(new Variant("ok")) { StatusCode = StatusCodes.Good }
        };

        var exception = Assert.Throws<OpcUaBatchReadException>(() =>
            ReflectionTestHelpers.InvokePrivateStatic(typeof(OpcUaClient), "BuildReadResults", nodeIds, dataValues));

        Assert.Equal(2, exception.SuccessfulValues.Count);
        Assert.Equal(1, exception.SuccessfulValues["ns=2;s=Node1"]);
        Assert.Equal("ok", exception.SuccessfulValues["ns=2;s=Node3"]);
        Assert.Single(exception.Failures);
        Assert.Equal("ns=2;s=Node2", exception.Failures[0].NodeId);
        Assert.Equal(StatusCodes.BadNodeIdUnknown, exception.Failures[0].StatusCode);
    }

    [Fact]
    public void EnsureWriteResultsSucceeded_throws_aggregate_exception_with_all_failures()
    {
        var nodeValues = new List<KeyValuePair<string, object?>>
        {
            new("ns=2;s=Node1", 1),
            new("ns=2;s=Node2", 2),
            new("ns=2;s=Node3", 3)
        };
        var results = new List<StatusCode>
        {
            StatusCodes.Good,
            StatusCodes.BadUserAccessDenied,
            StatusCodes.BadNotWritable
        };

        var exception = Assert.Throws<OpcUaBatchWriteException>(() =>
            ReflectionTestHelpers.InvokePrivateStatic(typeof(OpcUaClient), "EnsureWriteResultsSucceeded", nodeValues, results));

        Assert.Single(exception.SuccessfulNodeIds);
        Assert.Equal("ns=2;s=Node1", exception.SuccessfulNodeIds[0]);
        Assert.Equal(2, exception.Failures.Count);
        Assert.Contains(exception.Failures, failure => failure.NodeId == "ns=2;s=Node2");
        Assert.Contains(exception.Failures, failure => failure.NodeId == "ns=2;s=Node3");
    }

    [Fact]
    public void CanRetryWithoutSuppressedOrShelvedServerFilter_uses_explicit_status_codes_only()
    {
        var retryable = (bool)ReflectionTestHelpers.InvokePrivateStatic(
            typeof(OpcUaClient),
            "CanRetryWithoutSuppressedOrShelvedServerFilter",
            new ServiceResultException(StatusCodes.BadEventFilterInvalid))!;

        var aggregateRetryable = (bool)ReflectionTestHelpers.InvokePrivateStatic(
            typeof(OpcUaClient),
            "CanRetryWithoutSuppressedOrShelvedServerFilter",
            new AggregateException(new ServiceResultException(StatusCodes.BadMonitoredItemFilterUnsupported)))!;

        var notRetryable = (bool)ReflectionTestHelpers.InvokePrivateStatic(
            typeof(OpcUaClient),
            "CanRetryWithoutSuppressedOrShelvedServerFilter",
            new ServiceResultException(StatusCodes.BadNotConnected))!;

        var messageOnly = (bool)ReflectionTestHelpers.InvokePrivateStatic(
            typeof(OpcUaClient),
            "CanRetryWithoutSuppressedOrShelvedServerFilter",
            new InvalidOperationException("filter was rejected"))!;

        Assert.True(retryable);
        Assert.True(aggregateRetryable);
        Assert.False(notRetryable);
        Assert.False(messageOnly);
    }

    [Fact]
    public void OnMonitoredItemNotification_reports_callback_exceptions_to_diagnostics_handler()
    {
        OpcUaClientDiagnosticEvent? diagnostic = null;
        var client = CreateClient(diagnosticEvent => diagnostic = diagnosticEvent);
        var state = new OpcUaSubscriptionState();
        var monitoredItem = new MonitoredItem(DefaultTelemetry.Create(_ => { }), null)
        {
            DisplayName = "Node1"
        };
        var registration = new OpcUaMonitoredItemRegistration(
            "ns=2;s=Node1",
            "Node1",
            _ => throw new InvalidOperationException("boom"),
            new OpcUaMonitoredItemOptions(),
            monitoredItem,
            (_, _) => { });
        var args = ReflectionTestHelpers.CreateNonPublic<MonitoredItemNotificationEventArgs>(
            new MonitoredItemNotification
            {
                Value = new DataValue(new Variant(42))
                {
                    StatusCode = StatusCodes.Good
                }
            });

        ReflectionTestHelpers.InvokePrivateInstance(
            client,
            "OnMonitoredItemNotification",
            state,
            registration,
            monitoredItem,
            args);

        Assert.NotNull(diagnostic);
        Assert.Equal(OpcUaClientDiagnosticKind.SubscriptionCallbackException, diagnostic!.Kind);
        Assert.Equal("ns=2;s=Node1", diagnostic.ItemId);
        Assert.IsType<InvalidOperationException>(diagnostic.Exception);
    }

    [Fact]
    public void OnEventMonitoredItemNotification_reports_callback_exceptions_to_diagnostics_handler()
    {
        OpcUaClientDiagnosticEvent? diagnostic = null;
        var client = CreateClient(diagnosticEvent => diagnostic = diagnosticEvent);
        var state = new OpcUaSubscriptionState();
        var monitoredItem = new MonitoredItem(DefaultTelemetry.Create(_ => { }), null)
        {
            DisplayName = "AlarmSource"
        };

        var filterDefinition = new OpcUaEventFilterDefinition(
            OpcUaEventSelectClauseMode.Fixed,
            ObjectTypeIds.AlarmConditionType,
            new[]
            {
                new OpcUaEventSelectClauseDescriptor(
                    OpcUaEventFieldKeys.EventType,
                    OpcUaEventFieldKeys.EventType,
                    new SimpleAttributeOperand
                    {
                        TypeDefinitionId = ObjectTypeIds.BaseEventType,
                        BrowsePath = new QualifiedNameCollection
                        {
                            new(BrowseNames.EventType)
                        },
                        AttributeId = Attributes.Value
                    })
            },
            null,
            false);

        var registration = new OpcUaEventMonitoredItemRegistration(
            "ns=2;s=AlarmSource",
            "AlarmSource",
            filterDefinition,
            monitoredItem,
            (_, _) => { });

        var args = ReflectionTestHelpers.CreateNonPublic<MonitoredItemNotificationEventArgs>(
            new EventFieldList
            {
                EventFields = new VariantCollection
                {
                    new(new NodeId(ObjectTypeIds.AlarmConditionType))
                }
            });

        ReflectionTestHelpers.InvokePrivateInstance(
            client,
            "OnEventMonitoredItemNotification",
            state,
            registration,
            new Action<OpcUaEventNotification>(_ => throw new InvalidOperationException("event boom")),
            monitoredItem,
            args);

        Assert.NotNull(diagnostic);
        Assert.Equal(OpcUaClientDiagnosticKind.EventSubscriptionCallbackException, diagnostic!.Kind);
        Assert.Equal("ns=2;s=AlarmSource", diagnostic.ItemId);
        Assert.IsType<InvalidOperationException>(diagnostic.Exception);
    }

    [Fact]
    public void ReportDiagnostic_swallow_diagnostics_handler_exceptions()
    {
        var client = CreateClient(_ => throw new InvalidOperationException("diagnostic boom"));

        var exception = Record.Exception(() =>
            ReflectionTestHelpers.InvokePrivateInstance(
                client,
                "ReportDiagnostic",
                OpcUaClientDiagnosticKind.EventFilterFallbackWarning,
                "warning",
                null,
                "subscription-a",
                "ns=2;s=Node1"));

        Assert.Null(exception);
    }

    private static OpcUaClient CreateClient(Action<OpcUaClientDiagnosticEvent>? diagnosticsHandler = null)
    {
        var options = new OpcUaClientOptions
        {
            ServerUrl = "opc.tcp://127.0.0.1:4840",
            ApplicationName = "UnitTests",
            DiagnosticsHandler = diagnosticsHandler
        };

        return new OpcUaClient(
            options,
            (_, _) => Task.FromException<OpcUaClientConnection>(new InvalidOperationException("Connect should not be used in this test.")));
    }
}
