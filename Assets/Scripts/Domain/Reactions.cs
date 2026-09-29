/// <summary>
/// The stamp's verdict as a reaction reads it (the personalities spec's R2):
/// the traveller was accepted or denied. Serialized in the reactions' rows
/// (append only; SerializedEnumsTests pins the values).
/// </summary>
public enum ReactionVerdict
{
    /// <summary>The traveller was accepted.</summary>
    Accepted,

    /// <summary>The traveller was denied.</summary>
    Denied
}

/// <summary>
/// What the traveller meant (the personalities spec's R2): Lying for a place
/// lie (smuggling included) or a record lie; Honest otherwise, a directive
/// fault and a costume error included, since those travellers do not know.
/// Serialized in the reactions' rows (append only; pinned).
/// </summary>
public enum ReactionIntent
{
    /// <summary>No lie: the traveller believes their papers (a directive fault or a costume error included).</summary>
    Honest,

    /// <summary>A place lie, smuggling included, or a record lie.</summary>
    Lying
}

/// <summary>The reaction's intent of a traveller (the personalities spec's R2). Pure.</summary>
public static class ReactionIntents
{
    /// <summary>Lying when the traveller carries a place lie (<paramref name="isLiar"/>: CaseInstance.IsLiar, smuggling included) or a record lie (<paramref name="isForger"/>: CaseInstance.IsForger); Honest otherwise.</summary>
    public static ReactionIntent Of(bool isLiar, bool isForger) => isLiar || isForger ? ReactionIntent.Lying : ReactionIntent.Honest;
}
