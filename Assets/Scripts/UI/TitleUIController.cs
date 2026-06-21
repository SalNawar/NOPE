using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI for the title scene added in Alpha Phase 5: a title panel (Continue /
/// New Run) and an ending panel (shown instead, when WorldState.endingId is
/// set, displaying the reached EndingSO and offering New Run). Both panels
/// are optional; if unwired, TitleSceneController degrades to loading the
/// office scene directly so the run stays playable.
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

    /// <summary>Reached ending's flavor text.</summary>
    [SerializeField] private TMP_Text endingBodyText;

    /// <summary>Clears the ended run and starts a new one.</summary>
    [SerializeField] private Button endingNewRunButton;

    /// <summary>True if the title panel is wired.</summary>
    public bool HasTitlePanel => titlePanel != null;

    /// <summary>True if the ending panel is wired.</summary>
    public bool HasEndingPanel => endingPanel != null;

    /// <summary>Hides both panels until a Show* call activates one.</summary>
    private void Awake()
    {
        titlePanel?.SetActive(false);
        endingPanel?.SetActive(false);
    }

    /// <summary>
    /// Shows the title panel. The Continue button is hidden entirely if no
    /// save exists.
    /// </summary>
    public void ShowTitle(bool hasSave, Action onContinue, Action onNewRun)
    {
        if (titlePanel == null)
            return;

        endingPanel?.SetActive(false);
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

        titlePanel?.SetActive(false);
        endingPanel.SetActive(true);

        if (endingTitleText != null)
            endingTitleText.text = ending != null && !string.IsNullOrEmpty(ending.displayName)
                ? ending.displayName
                : "The End";

        if (endingBodyText != null)
            endingBodyText.text = ending != null ? ending.bodyText : string.Empty;

        if (endingNewRunButton != null)
        {
            endingNewRunButton.onClick.RemoveAllListeners();
            endingNewRunButton.onClick.AddListener(() => onNewRun?.Invoke());
        }
    }
}
