namespace OpcUaClientKit;

/// <summary>
/// Represents a batch read operation where at least one node failed while others may have succeeded.
/// </summary>
public sealed class OpcUaBatchReadException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpcUaBatchReadException"/> class.
    /// </summary>
    public OpcUaBatchReadException(
        IReadOnlyDictionary<string, object?> successfulValues,
        IReadOnlyList<OpcUaBatchOperationFailure> failures)
        : base(CreateMessage(failures))
    {
        SuccessfulValues = successfulValues ?? throw new ArgumentNullException(nameof(successfulValues));
        Failures = failures ?? throw new ArgumentNullException(nameof(failures));

        if (Failures.Count == 0)
        {
            throw new ArgumentException("At least one failure is required.", nameof(failures));
        }
    }

    /// <summary>
    /// Gets the values that were read successfully before the exception was raised.
    /// </summary>
    public IReadOnlyDictionary<string, object?> SuccessfulValues { get; }

    /// <summary>
    /// Gets the complete list of failed node reads.
    /// </summary>
    public IReadOnlyList<OpcUaBatchOperationFailure> Failures { get; }

    private static string CreateMessage(IReadOnlyList<OpcUaBatchOperationFailure> failures)
    {
        if (failures == null || failures.Count == 0)
        {
            return "Batch read failed.";
        }

        return $"Batch read failed for {failures.Count} node(s).";
    }
}
