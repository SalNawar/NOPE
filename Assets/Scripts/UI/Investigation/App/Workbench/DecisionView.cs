using TMPro;
using UnityEngine;

/// <summary>
/// The decision step (the PC workbench spec IA10; W5: the decision comes
/// last and is argued from the findings): in place of the two panes, what
/// was logged ("You logged 3 findings: 1 difference to cite."), then Accept
/// (the traveller goes through) and Deny, which cites the first logged
/// difference or broken rule, or says to log one first. The façade
/// (InvestigationUIController) owns the two buttons and greys Deny while
/// nothing can be cited; the desk's stamp tray decides without this gate.
/// </summary>
public sealed class DecisionView : MonoBehaviour
{
    /// <summary>What was logged.</summary>
    [SerializeField] private TMP_Text summaryText;

    /// <summary>Accept's second line.</summary>
    [SerializeField] private TMP_Text acceptDetail;

    /// <summary>Deny's second line: what it cites, or why it waits.</summary>
    [SerializeField] private TMP_Text denyDetail;

    /// <summary>Draws the decision from <paramref name="log"/> for <paramref name="traveller"/> (null: nobody at the desk).</summary>
    public void Show(FindingLog log, string traveller)
    {
        int count = log != null ? log.Count : 0, differences = log != null ? log.Differences : 0;
        if (summaryText != null)
            summaryText.text = traveller == null ? UiText.Get("idle.waiting")
                : UiText.Format(differences > 0 ? "decision.summary" : "decision.summaryNone", count, differences);
        if (acceptDetail != null)
            acceptDetail.text = UiText.Format("decision.accept", traveller ?? string.Empty);
        if (denyDetail != null)
        {
            Finding cite = log != null ? log.FirstDifference : null;
            denyDetail.text = cite != null ? UiText.Format("decision.denyCites", FindingsView.Title(cite)) : UiText.Get("decision.denyWaits");
        }
    }
}
