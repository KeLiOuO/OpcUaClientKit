namespace OpcUaClientKit;

/// <summary>
/// Represents a batch write operation where at least one node failed while others may have succeeded.
/// </summary>
public sealed class OpcUaBatchWriteException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpcUaBatchWriteException"/> class.
    /// </summary>
    public OpcUaBatchWriteException(
        IReadOnlyList<string> successfulNodeIds,
        IReadOnlyList<OpcUaBatchOperationFailure> failures)
        : base(CreateMessage(failures))
    {
        SuccessfulNodeIds = successfulNodeIds ?? throw new ArgumentNullException(nameof(successfulNodeIds));
        Failures = failures ?? throw new ArgumentNullException(nameof(failures));

        if (Failures.Count == 0)
        {
            throw new ArgumentException("At least one failure is required.", nameof(failures));
        }
    }

    /// <summary>
    /// Gets the nodes that were written successfully before the exception was raised.
    /// </summary>
    public IReadOnlyList<string> SuccessfulNodeIds { get; }

    /// <summary>
    /// Gets the complete list of failed node writes.
    /// </summary>
    public IReadOnlyList<OpcUaBatchOperationFailure> Failures { get; }

    private static string CreateMessage(IReadOnlyList<OpcUaBatchOperationFailure> failures)
    {
        if (failures == null || failures.Count == 0)
        {
            return "Batch write failed.";
        }

        return $"Batch write failed for {failures.Count} node(s).";
    }
}
