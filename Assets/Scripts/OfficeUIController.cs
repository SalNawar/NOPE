using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office's HUD, verdict line and citation slip (the case itself is
/// shown by the investigation desk; the legacy era-pick screen is gone,
/// audit R3-011).
/// </summary>
public sealed class OfficeUIController : MonoBehaviour
{
    [Header("Text")]
    /// <summary>Shows the result after the player decides.</summary>
    [SerializeField] private TMP_Text resultText;

    /// <summary>The verdict line's strip; shown only while the line has text (piece 6 R18).</summary>
    [SerializeField] private GameObject resultBackdrop;

    [Header("HUD (optional — null-safe)")]
    /// <summary>Shows current money.</summary>
    [SerializeField] private TMP_Text moneyText;

    /// <summary>Shows timeline stability.</summary>
    [SerializeField] private TMP_Text stabilityText;

    /// <summary>Shows the current day number.</summary>
    [SerializeField] private TMP_Text dayText;

    [Header("Citation Slip (optional — null-safe)")]
    /// <summary>Panel shown when a citation is issued.</summary>
    [SerializeField] private GameObject citationPanel;

    /// <summary>Citation slip body text.</summary>
    [SerializeField] private TMP_Text citationText;

    /// <summary>Button that dismisses the citation and continues the day.</summary>
    [SerializeField] private Button citationContinueButton;

    /// <summary>Pending continue callback while a citation slip is open.</summary>
    private Action _onCitationDismissed;

    /// <summary>
    /// Updates the result label (call from GameManager after validation).
    /// </summary>
    public void SetResultText(string text) => SetResult(text);

    /// <summary>Writes the verdict line and shows its strip only while it has text.</summary>
    private void SetResult(string text)
    {
        if (resultText != null)
            resultText.text = text;
        if (resultBackdrop != null)
            resultBackdrop.SetActive(!string.IsNullOrEmpty(text));
    }

    /// <summary>
    /// Refreshes the money/stability/day HUD from world state.
    /// Safe to call with unwired HUD fields.
    /// </summary>
    public void UpdateHud(WorldState world)
    {
        if (world == null)
            return;

        if (moneyText != null)
            moneyText.text = UiText.Format("tray.money", UiText.Currency(UiText.WalletForm.Label), world.money);

        if (stabilityText != null)
            stabilityText.text = UiText.Format("tray.stability", StabilityRules.Format(world.timelineStability));

        if (dayText != null)
            dayText.text = UiText.Format("tray.day", world.day);
    }

    /// <summary>
    /// Shows a verdict: result line, plus a citation slip when issued.
    /// If the citation panel is wired, the day pauses until the player dismisses it;
    /// otherwise onContinue is invoked immediately.
    /// </summary>
    public void ShowVerdict(CaseVerdict verdict, Action onContinue)
    {
        if (verdict == null)
        {
            onContinue?.Invoke();
            return;
        }

        string credits = UiText.Currency(UiText.WalletForm.Inline);
        SetResult(verdict.correct
            ? UiText.Format("verdict.correct", verdict.payAwarded, credits)
            : verdict.moneyPenalty > 0
                ? UiText.Format("verdict.wrongPenalty", StabilityRules.FormatChange(verdict.stabilityDelta), verdict.moneyPenalty, credits)
                : UiText.Format("verdict.wrong", StabilityRules.FormatChange(verdict.stabilityDelta)));

        bool canShowSlip = verdict.citationIssued && citationPanel != null && citationText != null;

        if (!canShowSlip)
        {
            onContinue?.Invoke();
            return;
        }

        // Open the slip and hold the day until dismissed.
        _onCitationDismissed = onContinue;
        citationText.text = verdict.citationText;
        citationPanel.SetActive(true);

        if (citationContinueButton != null)
        {
            citationContinueButton.onClick.RemoveListener(HandleCitationDismissed);
            citationContinueButton.onClick.AddListener(HandleCitationDismissed);
        }
        else
        {
            // No button wired: leave the slip visible (next case hides it)
            // but don't block the day.
            _onCitationDismissed = null;
            onContinue?.Invoke();
        }
    }

    /// <summary>
    /// Closes the citation slip and resumes the day.
    /// </summary>
    private void HandleCitationDismissed()
    {
        if (citationPanel != null)
            citationPanel.SetActive(false);

        OneShot.Fire(ref _onCitationDismissed);
    }
}
