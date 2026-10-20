using System;

/// <summary>
/// The desk's events (owner DOC-HANDLE): a paper handed over landed on the
/// counter (the hand-over's slap, BUG-49b: FEEDBACK plays its sound on that
/// frame), the case ended, an upgrade arrived, and a paper's place in the
/// papers' stack changed (its rank: DOC-LOOK's contact shadow, BUG-40c).
/// DeskController raises DocumentLanded, CaseEnded and StackChanged;
/// UpgradeArrived is raised by whoever lays an upgrade on the desk (the
/// supply crate's owner).
/// </summary>
public static class DeskEvents
{
    /// <summary>A paper the traveller handed over landed on the counter, on the frame it lands (the hand-over's slap; a drop plays the paper's own drop cue, not this).</summary>
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
