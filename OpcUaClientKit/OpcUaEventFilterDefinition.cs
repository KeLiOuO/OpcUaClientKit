using Opc.Ua;

namespace OpcUaClientKit;

internal static class OpcUaEventFieldKeys
{
    public const string NodeId = "NodeId";
    public const string EventId = "EventId";
    public const string EventType = "EventType";
    public const string SourceNode = "SourceNode";
    public const string SourceName = "SourceName";
    public const string Time = "Time";
    public const string ReceiveTime = "ReceiveTime";
    public const string Message = "Message";
    public const string Severity = "Severity";
    public const string ConditionId = "ConditionId";
    public const string ConditionName = "ConditionName";
    public const string Retain = "Retain";
    public const string EnabledStateId = "EnabledState.Id";
    public const string ActiveStateId = "ActiveState.Id";
    public const string AckedStateId = "AckedState.Id";
    public const string SuppressedOrShelved = "SuppressedOrShelved";
}

internal sealed class OpcUaEventFilterDefinition
{
    public OpcUaEventFilterDefinition(
        OpcUaEventSelectClauseMode selectClauseMode,
        NodeId eventTypeNodeId,
        IReadOnlyList<OpcUaEventSelectClauseDescriptor> selectClauses,
        ushort? severityAtLeast,
        bool ignoreSuppressedOrShelved)
    {
        SelectClauseMode = selectClauseMode;
        EventTypeNodeId = eventTypeNodeId ?? throw new ArgumentNullException(nameof(eventTypeNodeId));
        SelectClauses = selectClauses ?? throw new ArgumentNullException(nameof(selectClauses));
        SeverityAtLeast = severityAtLeast;
        IgnoreSuppressedOrShelved = ignoreSuppressedOrShelved;
    }

    public OpcUaEventSelectClauseMode SelectClauseMode { get; }

    public NodeId EventTypeNodeId { get; }

    public IReadOnlyList<OpcUaEventSelectClauseDescriptor> SelectClauses { get; }

    public ushort? SeverityAtLeast { get; }

    public bool IgnoreSuppressedOrShelved { get; }

    public bool CanFallbackToClientSideSuppressedOrShelvedFilter => IgnoreSuppressedOrShelved;
}

internal sealed class OpcUaEventSelectClauseDescriptor
{
    public OpcUaEventSelectClauseDescriptor(
        string key,
        string displayName,
        SimpleAttributeOperand operand)
    {
        Key = string.IsNullOrWhiteSpace(key)
            ? throw new ArgumentException("Key is required.", nameof(key))
            : key.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName)
            ? throw new ArgumentException("DisplayName is required.", nameof(displayName))
            : displayName.Trim();
        Operand = operand ?? throw new ArgumentNullException(nameof(operand));
    }

    public string Key { get; }

    public string DisplayName { get; }

    public SimpleAttributeOperand Operand { get; }
}
