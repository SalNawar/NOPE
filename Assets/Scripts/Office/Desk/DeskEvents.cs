using System;

/// <summary>
/// The desk's events (P0-A seam; owner DOC-HANDLE): a paper landed on the
/// desk, the case ended, an upgrade arrived, and a paper's place in the
/// papers' stack changed (its rank: DOC-LOOK's contact shadow, BUG-40c). The
/// P0 stub declares them; nothing raises them yet (today's behaviour).
/// </summary>
public static class DeskEvents
{
    /// <summary>A paper landed on the desk (handed over or dropped).</summary>
    public static event Action<DeskDocument> DocumentLanded;

    /// <summary>The case at the desk ended (decided, or the traveller left).</summary>
    public static event Action CaseEnded;

    /// <summary>An upgrade arrived at the desk (its id).</summary>
    public static event Action<string> UpgradeArrived;

    /// <summary>A paper's place in the papers' stack changed: the paper and its rank (0 the bottom).</summary>
    public static event Action<DeskDocument, int> StackChanged;

    /// <summary>Raises <see cref="DocumentLanded"/>.</summary>
    public static void RaiseDocumentLanded(DeskDocument paper) => DocumentLanded?.Invoke(paper);

    /// <summary>Raises <see cref="CaseEnded"/>.</summary>
    public static void RaiseCaseEnded() => CaseEnded?.Invoke();

    /// <summary>Raises <see cref="UpgradeArrived"/>.</summary>
    public static void RaiseUpgradeArrived(string upgradeId) => UpgradeArrived?.Invoke(upgradeId);

    /// <summary>Raises <see cref="StackChanged"/>.</summary>
    public static void RaiseStackChanged(DeskDocument paper, int rank) => StackChanged?.Invoke(paper, rank);
}
