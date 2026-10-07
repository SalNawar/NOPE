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

    /// <summary>The verdict ribbon's face (the UI kit's: green for a right call, red for a wrong one, brass for a free warning or a notice); optional.</summary>
    [SerializeField] private Image resultRibbon;

    /// <summary>The UI kit the ribbon's faces come from; optional.</summary>
    [SerializeField] private UiKitSO kit;

    [Header("HUD (optional — null-safe)")]
    /// <summary>Shows current money.</summary>
    [SerializeField] private TMP_Text moneyText;

    /// <summary>Shows today's date and the day number in the taskbar ("14 MAR 2150 · Day 1"; the desk-first redesign: "the date should be on the PC").</summary>
    [SerializeField] private TMP_Text dayText;

    /// <summary>The date's button: a click holds today's date on the Investigation app's workbench, to compare with an expiry or a ticket's date.</summary>
    [SerializeField] private Button dateButton;

    /// <summary>The Investigation app (the date is held on its workbench).</summary>
    [SerializeField] private InvestigationApp app;

    [Header("Citation Slip (optional — null-safe)")]
    /// <summary>Panel shown when a citation is issued.</summary>
    [SerializeField] private GameObject citationPanel;

    /// <summary>The slip's reason line (CaseVerdict.citationReason).</summary>
    [SerializeField] private TMP_Text citationReasonText;

    /// <summary>The slip's rule and values (CaseVerdict.citationDetail).</summary>
    [SerializeField] private TMP_Text citationDetailText;

    /// <summary>The slip's warning or penalty (CaseVerdict.citationConsequence).</summary>
    [SerializeField] private TMP_Text citationConsequenceText;

    /// <summary>Button that dismisses the citation and continues the day.</summary>
    [SerializeField] private Button citationContinueButton;

    /// <summary>Pending continue callback while a citation slip is open.</summary>
    private Action _onCitationDismissed;

    /// <summary>
    /// Updates the result label (call from GameManager after validation), on the notice ribbon.
    /// </summary>
    public void SetResultText(string text) => SetResult(text, UiKitNames.VerdictRibbon(null, false));

    /// <summary>Writes the verdict line on <paramref name="ribbon"/> (a UI kit sprite) and shows its strip only while it has text.</summary>
    private void SetResult(string text, string ribbon)
    {
        if (resultText != null)
            resultText.text = text;
        if (kit != null)
        {
            kit.Show(resultRibbon, ribbon);
            if (resultText != null)
                resultText.color = kit.InkOn(ribbon);
        }
        if (resultBackdrop != null)
            resultBackdrop.SetActive(!string.IsNullOrEmpty(text));
    }

    /// <summary>
    /// Refreshes the money/day HUD from world state (stability shows as the Helix River, HelixRiverMonitor, never a number).
    /// Safe to call with unwired HUD fields.
    /// </summary>
    public void UpdateHud(WorldState world)
    {
        if (world == null)
            return;

        if (moneyText != null)
            moneyText.text = UiText.Format("tray.money", UiText.Currency(UiText.WalletForm.Label), world.money);

        if (dayText != null)
            dayText.text = TrayDate(world.day);
    }

    /// <summary>The taskbar's date for <paramref name="day"/>: today in the agency's calendar, in capitals, and the day ("14 MAR 2150 · Day 1"), or the plain day when the library has no readable first date.</summary>
    private static string TrayDate(int day)
    {
        ContentLibrarySO library = RunManager.HasInstance ? RunManager.Instance.Library : null;
        string today = library != null ? AgencyCalendar.Today(library.Agency.firstDate, day) : null;
        return today != null ? UiText.Format("tray.date", today.ToUpperInvariant(), day) : UiText.Format("tray.day", day);
    }

    /// <summary>The date's button holds today on the app's workbench.</summary>
    private void Awake()
    {
        if (dateButton != null && app != null)
            dateButton.onClick.AddListener(app.HoldToday);
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
                ? UiText.Format("verdict.wrongPenalty", verdict.moneyPenalty, credits)
                : UiText.Get("verdict.wrong"),
            UiKitNames.VerdictRibbon(verdict.correct, verdict.wasFreeWarning));

        bool canShowSlip = verdict.citationIssued && citationPanel != null;

        if (!canShowSlip)
        {
            onContinue?.Invoke();
            return;
        }

        // Open the slip and hold the day until dismissed.
        _onCitationDismissed = onContinue;
        if (citationReasonText != null)
            citationReasonText.text = verdict.citationReason;
        if (citationDetailText != null)
            citationDetailText.text = verdict.citationDetail;
        if (citationConsequenceText != null)
            citationConsequenceText.text = verdict.citationConsequence;
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
