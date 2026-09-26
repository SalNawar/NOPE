using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI for the title scene added in Alpha Phase 5: a title panel (Continue /
/// New Run) and an ending panel (shown instead, when WorldState.endingId is
/// set, displaying the reached EndingSO and offering New Run; on the Debt
/// Relief ending, the clerk's own papers beside it, redesign phase 13). Both
/// panels are optional; if unwired, TitleSceneController degrades to loading
/// the office scene directly so the run stays playable.
/// </summary>
public sealed class TitleUIController : MonoBehaviour
{
    [Header("Title Panel")]
    /// <summary>Root panel shown when the run has not ended.</summary>
    [SerializeField] private GameObject titlePanel;

    /// <summary>Game title / day-counter text.</summary>
    [SerializeField] private TMP_Text titleText;

    /// <summary>Resumes the saved run (hidden if there is no save).</summary>
    [SerializeField] private Button continueButton;

    /// <summary>Starts a brand-new run.</summary>
    [SerializeField] private Button newRunButton;

    [Header("Ending Panel")]
    /// <summary>Root panel shown when WorldState.endingId is set.</summary>
    [SerializeField] private GameObject endingPanel;

    /// <summary>Reached ending's display name.</summary>
    [SerializeField] private TMP_Text endingTitleText;

    /// <summary>The reached ending's picture, full screen behind the ending panel (EndingSO.picture; hidden when it has none).</summary>
    [SerializeField] private Image endingPicture;

    /// <summary>Reached ending's flavor text.</summary>
    [SerializeField] private TMP_Text endingBodyText;

    /// <summary>Clears the ended run and starts a new one.</summary>
    [SerializeField] private Button endingNewRunButton;

    [Header("Ending Panel: the clerk's papers (redesign phase 13)")]
    /// <summary>The Debt Relief ending's papers, beside the ending panel: the clerk's own Labour Contract and account (hidden for every other ending).</summary>
    [SerializeField] private GameObject clerkPapers;

    /// <summary>The clerk's Labour Contract (TC-520): its title over its rows.</summary>
    [SerializeField] private TMP_Text clerkContractText;

    /// <summary>The clerk's Record Extract (TC-901), now Frozen: its title and holder line over its rows.</summary>
    [SerializeField] private TMP_Text clerkAccountText;

    /// <summary>True if the title panel is wired.</summary>
    public bool HasTitlePanel => titlePanel != null;

    /// <summary>True if the ending panel is wired.</summary>
    public bool HasEndingPanel => endingPanel != null;

    /// <summary>
    /// Hides both panels until a Show* call activates one. The panels are
    /// optional, so each is tested with Unity's == (audit R4-010): an
    /// unassigned serialized field is Unity's fake null in the Editor, which
    /// ?. does not see, and SetActive on it would throw.
    /// </summary>
    private void Awake()
    {
        if (titlePanel != null) titlePanel.SetActive(false);
        if (endingPanel != null) endingPanel.SetActive(false);
        if (endingPicture != null) endingPicture.gameObject.SetActive(false);
    }

    /// <summary>
    /// Shows the title panel. The Continue button is hidden entirely if no
    /// save exists.
    /// </summary>
    public void ShowTitle(bool hasSave, Action onContinue, Action onNewRun)
    {
        if (titlePanel == null)
            return;

        if (endingPanel != null) endingPanel.SetActive(false);
        titlePanel.SetActive(true);

        if (titleText != null)
            titleText.text = "Time Sorter";

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(hasSave);
            continueButton.onClick.RemoveAllListeners();

            if (hasSave)
                continueButton.onClick.AddListener(() => onContinue?.Invoke());
        }

        if (newRunButton != null)
        {
            newRunButton.onClick.RemoveAllListeners();
            newRunButton.onClick.AddListener(() => onNewRun?.Invoke());
        }
    }

    /// <summary>
    /// Shows the ending panel for the reached EndingSO (may be null if the id
    /// has no matching content yet — falls back to a generic message).
    /// </summary>
    public void ShowEnding(EndingSO ending, Action onNewRun)
    {
        if (endingPanel == null)
            return;

        if (titlePanel != null) titlePanel.SetActive(false);
        endingPanel.SetActive(true);

        if (endingTitleText != null)
            endingTitleText.text = ending != null && !string.IsNullOrEmpty(ending.displayName)
                ? ending.displayName
                : "The End";

        if (endingBodyText != null)
            endingBodyText.text = ending != null ? ending.bodyText : string.Empty;

        if (endingPicture != null)
        {
            Sprite picture = ending != null ? ending.picture : null;
            endingPicture.sprite = picture;
            endingPicture.gameObject.SetActive(picture != null);
        }

        if (endingNewRunButton != null)
        {
            endingNewRunButton.onClick.RemoveAllListeners();
            endingNewRunButton.onClick.AddListener(() => onNewRun?.Invoke());
        }
    }

    /// <summary>
    /// Shows the clerk's papers beside the ending panel (the Debt Relief ending,
    /// redesign phase 13: the Labour Contract on one side, the account, now
    /// Frozen, on the other), each a title over its rows under their group
    /// headings. Drawn with today's widgets, the one place the forms engine
    /// replaces with TC-520 and TC-901. A null contract or account hides them.
    /// </summary>
    public void ShowClerkPapers(string contractTitle, IReadOnlyList<AccountRow> contract, string accountTitle, IReadOnlyList<AccountRow> account)
    {
        if (clerkPapers == null)
            return;

        bool show = contract != null && account != null;
        clerkPapers.SetActive(show);
        if (!show)
            return;

        if (clerkContractText != null)
            clerkContractText.text = Paper(contractTitle, contract);
        if (clerkAccountText != null)
            clerkAccountText.text = Paper(accountTitle, account);
    }

    /// <summary>A paper's text: its title in bold, then each row's label and value (the value indented to its column, so a long one wraps there), a bold heading where the group changes.</summary>
    private static string Paper(string title, IReadOnlyList<AccountRow> rows)
    {
        var sb = new StringBuilder();
        sb.Append("<b>").Append(title).Append("</b>\n");
        string group = null;
        foreach (AccountRow row in rows)
        {
            if (row.Group != group && !string.IsNullOrEmpty(row.Group))
                sb.Append("\n<b>").Append(row.Group).Append("</b>\n");
            group = row.Group;
            sb.Append(row.Label).Append("<indent=42%>").Append(row.Value).Append("</indent>\n");
        }
        return sb.ToString();
    }
}
