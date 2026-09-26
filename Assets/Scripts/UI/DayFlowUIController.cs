using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Owns the two day-flow panels in the office:
/// - Morning briefing (day number + TomorrowPackage lines) before the shift
/// - End-of-day results (ledger breakdown) after the last case
/// All references are optional; unwired panels are skipped gracefully.
/// </summary>
public sealed class DayFlowUIController : MonoBehaviour
{
    [Header("Briefing Panel")]
    /// <summary>Root of the briefing panel.</summary>
    [SerializeField] private GameObject briefingPanel;

    /// <summary>Briefing title ("Day 3 — Morning Briefing").</summary>
    [SerializeField] private TMP_Text briefingTitleText;

    /// <summary>Briefing body (rules + news lines).</summary>
    [SerializeField] private TMP_Text briefingBodyText;

    /// <summary>Starts the shift.</summary>
    [SerializeField] private Button startShiftButton;

    [Header("Results Panel")]
    /// <summary>Root of the results panel.</summary>
    [SerializeField] private GameObject resultsPanel;

    /// <summary>Results title ("Day 3 — Shift Report").</summary>
    [SerializeField] private TMP_Text resultsTitleText;

    /// <summary>Results body (pay breakdown, citations, stability).</summary>
    [SerializeField] private TMP_Text resultsBodyText;

    /// <summary>Leaves the office for the home phase.</summary>
    [SerializeField] private Button goHomeButton;

    /// <summary>Pending callback for the briefing start button.</summary>
    private Action _onStartShift;

    /// <summary>Pending callback for the go-home button.</summary>
    private Action _onGoHome;

    /// <summary>True if the briefing panel is wired and can be shown.</summary>
    public bool HasBriefingPanel => briefingPanel != null && startShiftButton != null;

    /// <summary>True if the results panel is wired and can be shown.</summary>
    public bool HasResultsPanel => resultsPanel != null && goHomeButton != null;

    /// <summary>Hides both panels on scene start (they pop when told to).</summary>
    private void Awake()
    {
        if (briefingPanel != null) briefingPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);

        if (startShiftButton != null)
            startShiftButton.onClick.AddListener(HandleStartShiftClicked);

        if (goHomeButton != null)
            goHomeButton.onClick.AddListener(HandleGoHomeClicked);
    }

    /// <summary>
    /// Shows the morning briefing built from the world's tomorrow package.
    /// Invokes onStartShift when the player clicks Start (or immediately if unwired).
    /// </summary>
    public void ShowBriefing(WorldState world, Action onStartShift)
    {
        if (!HasBriefingPanel || world == null)
        {
            onStartShift?.Invoke();
            return;
        }

        _onStartShift = onStartShift;

        if (briefingTitleText != null)
            briefingTitleText.text = UiText.Format("briefing.title", world.day);

        if (briefingBodyText != null)
        {
            var sb = new System.Text.StringBuilder();

            if (world.tomorrow.briefingLines.Count == 0 && world.tomorrow.newsLines.Count == 0)
            {
                sb.AppendLine(UiText.Get("briefing.empty"));
            }
            else
            {
                foreach (string line in world.tomorrow.briefingLines)
                    sb.AppendLine(UiText.Format("list.bullet", line));

                if (world.tomorrow.newsLines.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine(UiText.Get("briefing.newsHeader"));

                    foreach (string line in world.tomorrow.newsLines)
                        sb.AppendLine(UiText.Format("list.bullet", line));
                }
            }

            briefingBodyText.text = sb.ToString();
        }

        briefingPanel.SetActive(true);
    }

    /// <summary>
    /// Shows the end-of-day report from the shift ledger.
    /// Invokes onGoHome when the player clicks Go Home (or immediately if unwired).
    /// </summary>
    public void ShowResults(WorldState world, ShiftLedger ledger, Action onGoHome)
    {
        if (!HasResultsPanel || world == null || ledger == null)
        {
            onGoHome?.Invoke();
            return;
        }

        _onGoHome = onGoHome;

        if (resultsTitleText != null)
            resultsTitleText.text = UiText.Format("results.title", world.day);

        if (resultsBodyText != null)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(UiText.Format("results.processed", ledger.verdicts.Count));
            sb.AppendLine(UiText.Format("results.correctWrong", ledger.CorrectCount, ledger.WrongCount));
            sb.AppendLine();
            sb.AppendLine(UiText.Format("results.pay", ledger.TotalPay));

            if (ledger.TotalPenalties > 0)
                sb.AppendLine(UiText.Format("results.penalties", ledger.TotalPenalties));

            if (ledger.debtOwed != Account.Unknown)
                sb.AppendLine(UiText.Format("results.debtRelief", ledger.debtInstalment, ledger.debtOwed, UiText.Currency(UiText.WalletForm.Short)));

            sb.AppendLine(UiText.Format("results.net", ledger.NetMoney, UiText.Currency(UiText.WalletForm.Inline), world.money));
            sb.AppendLine();
            sb.AppendLine(UiText.Format("results.stability", world.timelineStability, ledger.TotalStabilityDelta));

            if (world.citationsToday > 0)
                sb.AppendLine(UiText.Format("results.citations", world.citationsToday));

            if (ledger.UnprovenDenialCount > 0)
                sb.AppendLine(UiText.Format("results.unproven", ledger.UnprovenDenialCount));

            resultsBodyText.text = sb.ToString();
        }

        resultsPanel.SetActive(true);
    }

    /// <summary>Briefing start clicked: close and begin the shift.</summary>
    private void HandleStartShiftClicked()
    {
        if (briefingPanel != null)
            briefingPanel.SetActive(false);

        Action cb = _onStartShift;
        _onStartShift = null;
        cb?.Invoke();
    }

    /// <summary>Go home clicked: close and hand off to the home phase.</summary>
    private void HandleGoHomeClicked()
    {
        if (resultsPanel != null)
            resultsPanel.SetActive(false);

        Action cb = _onGoHome;
        _onGoHome = null;
        cb?.Invoke();
    }
}
