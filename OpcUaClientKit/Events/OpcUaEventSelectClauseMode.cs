namespace OpcUaClientKit;

/// <summary>
/// Controls how event select clauses are generated for alarm event subscriptions.
/// </summary>
public enum OpcUaEventSelectClauseMode
{
    /// <summary>
    /// Dynamically discovers fields from the event type model and its supertypes.
    /// </summary>
    Dynamic = 0,

    /// <summary>
    /// Uses the fixed built-in field set defined by the library.
    /// </summary>
    Fixed = 1
}
