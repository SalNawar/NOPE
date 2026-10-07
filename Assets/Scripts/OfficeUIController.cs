using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office's HUD and verdict line (the case itself is shown by the
/// investigation desk; the legacy era-pick screen is gone, audit R3-011; a
/// citation is a paper on the desk since 2026-10-07: DeskController.Cite).
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

    /// <summary>Shows today's date and the day number in the taskbar ("14 MAR 2150 · Day 1"; the desk-first redesign: "the date should be on the PC").</summary>
    [SerializeField] private TMP_Text dayText;

    /// <summary>The date's button: a click holds today's date on the Investigation app's workbench, to compare with an expiry or a ticket's date.</summary>
    [SerializeField] private Button dateButton;

    /// <summary>The Investigation app (the date is held on its workbench).</summary>
    [SerializeField] private InvestigationApp app;

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

    /// <summary>Shows a verdict's result line (the pay, the penalty or the plain wrong).</summary>
    public void ShowVerdict(CaseVerdict verdict)
    {
        if (verdict == null)
            return;

        string credits = UiText.Currency(UiText.WalletForm.Inline);
        SetResult(verdict.correct
            ? UiText.Format("verdict.correct", verdict.payAwarded, credits)
            : verdict.moneyPenalty > 0
                ? UiText.Format("verdict.wrongPenalty", verdict.moneyPenalty, credits)
                : UiText.Get("verdict.wrong"));
    }
}
