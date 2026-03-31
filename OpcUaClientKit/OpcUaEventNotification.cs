using System.Collections.ObjectModel;

namespace OpcUaClientKit;

public sealed class OpcUaEventNotification
{
    public OpcUaEventNotification(
        byte[]? eventId,
        string eventTypeNodeId,
        string? sourceNodeId,
        string? sourceDisplayName,
        string? sourceName,
        DateTime time,
        DateTime receiveTime,
        string? message,
        ushort severity,
        string? conditionId,
        string? conditionName,
        bool? retain,
        bool? enabled,
        bool? active,
        bool? acked,
        IReadOnlyList<OpcUaEventFieldValue> selectedFields,
        IReadOnlyDictionary<string, object?> fields)
    {
        EventId = eventId?.ToArray() ?? Array.Empty<byte>();
        EventTypeNodeId = string.IsNullOrWhiteSpace(eventTypeNodeId)
            ? throw new ArgumentException("EventTypeNodeId is required.", nameof(eventTypeNodeId))
            : eventTypeNodeId.Trim();
        SourceNodeId = string.IsNullOrWhiteSpace(sourceNodeId)
            ? null
            : sourceNodeId.Trim();
        SourceDisplayName = string.IsNullOrWhiteSpace(sourceDisplayName)
            ? null
            : sourceDisplayName.Trim();
        SourceName = string.IsNullOrWhiteSpace(sourceName)
            ? null
            : sourceName.Trim();
        Time = time;
        ReceiveTime = receiveTime;
        Message = string.IsNullOrWhiteSpace(message)
            ? null
            : message.Trim();
        Severity = severity;
        ConditionId = string.IsNullOrWhiteSpace(conditionId)
            ? null
            : conditionId.Trim();
        ConditionName = string.IsNullOrWhiteSpace(conditionName)
            ? null
            : conditionName.Trim();
        Retain = retain;
        Enabled = enabled;
        Active = active;
        Acked = acked;
        SelectedFields = selectedFields == null
            ? throw new ArgumentNullException(nameof(selectedFields))
            : new ReadOnlyCollection<OpcUaEventFieldValue>(selectedFields.ToList());
        Fields = fields == null
            ? throw new ArgumentNullException(nameof(fields))
            : new ReadOnlyDictionary<string, object?>(
                new Dictionary<string, object?>(fields, StringComparer.Ordinal));
    }

    public byte[] EventId { get; }

    public string EventTypeNodeId { get; }

    public string? SourceNodeId { get; }

    public string? SourceDisplayName { get; }

    public string? SourceName { get; }

    public DateTime Time { get; }

    public DateTime ReceiveTime { get; }

    public string? Message { get; }

    public ushort Severity { get; }

    public string? ConditionId { get; }

    public string? ConditionName { get; }

    public bool? Retain { get; }

    public bool? Enabled { get; }

    public bool? Active { get; }

    public bool? Acked { get; }

    public IReadOnlyList<OpcUaEventFieldValue> SelectedFields { get; }

    public IReadOnlyDictionary<string, object?> Fields { get; }
}
