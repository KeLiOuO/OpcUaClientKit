using System.Collections.ObjectModel;

namespace OpcUaClientKit;

/// <summary>
/// Represents a normalized OPC UA alarm/condition event notification.
/// </summary>
public sealed class OpcUaEventNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpcUaEventNotification"/> class.
    /// </summary>
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
                fields.ToDictionary(
                    static field => field.Key,
                    static field => field.Value,
                    StringComparer.Ordinal));
    }

    /// <summary>
    /// Gets the event identifier emitted by the server.
    /// </summary>
    public byte[] EventId { get; }

    /// <summary>
    /// Gets the event type <c>NodeId</c>.
    /// </summary>
    public string EventTypeNodeId { get; }

    /// <summary>
    /// Gets the source object/view <c>NodeId</c> reported by the server.
    /// </summary>
    public string? SourceNodeId { get; }

    /// <summary>
    /// Gets the optional display name associated with the subscribed event source.
    /// </summary>
    public string? SourceDisplayName { get; }

    /// <summary>
    /// Gets the source name reported by the OPC UA server.
    /// </summary>
    public string? SourceName { get; }

    /// <summary>
    /// Gets the event timestamp.
    /// </summary>
    public DateTime Time { get; }

    /// <summary>
    /// Gets the receive timestamp reported by the server.
    /// </summary>
    public DateTime ReceiveTime { get; }

    /// <summary>
    /// Gets the localized message text carried by the event, if present.
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// Gets the event severity.
    /// </summary>
    public ushort Severity { get; }

    /// <summary>
    /// Gets the condition instance identifier, when available.
    /// </summary>
    public string? ConditionId { get; }

    /// <summary>
    /// Gets the condition name, when available.
    /// </summary>
    public string? ConditionName { get; }

    /// <summary>
    /// Gets the retained-state flag, when available.
    /// </summary>
    public bool? Retain { get; }

    /// <summary>
    /// Gets the enabled-state flag, when available.
    /// </summary>
    public bool? Enabled { get; }

    /// <summary>
    /// Gets the active-state flag, when available.
    /// </summary>
    public bool? Active { get; }

    /// <summary>
    /// Gets the acknowledged-state flag, when available.
    /// </summary>
    public bool? Acked { get; }

    /// <summary>
    /// Gets the selected event fields in the same order as the generated select clauses.
    /// </summary>
    public IReadOnlyList<OpcUaEventFieldValue> SelectedFields { get; }

    /// <summary>
    /// Gets the selected event fields as a dictionary keyed by the library's normalized field names.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Fields { get; }
}
