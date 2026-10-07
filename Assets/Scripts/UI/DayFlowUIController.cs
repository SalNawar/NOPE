using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Owns the two day-flow panels in the office:
/// - Morning briefing (day number, the day's bulletin naming its one new
///   paper or check first, lesson 4, then the TomorrowPackage lines) before the shift
/// - End-of-day results (the ShiftReport's money ledger at a glance, lesson 5) after the last case
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

    /// <summary>Results body (pay breakdown, citations, departures; stability is the panel's Helix River, never a number).</summary>
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
    /// Shows the morning briefing: the day's <paramref name="bulletin"/> first
    /// under its header when the day brings something new (Papers Please
    /// lesson 4, DayPlanSO.Bulletin), then the world's tomorrow package.
    /// Invokes onStartShift when the player clicks Start (or immediately if unwired).
    /// </summary>
    public void ShowBriefing(WorldState world, string bulletin, Action onStartShift)
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
            bool hasBulletin = !string.IsNullOrWhiteSpace(bulletin);
            if (hasBulletin)
            {
                sb.AppendLine(UiText.Get("briefing.bulletinHeader"));
                sb.AppendLine(UiText.Format("briefing.bulletin", bulletin.Trim()));
                sb.AppendLine();
            }

            if (world.tomorrow.briefingLines.Count == 0 && world.tomorrow.newsLines.Count == 0 && world.tomorrow.deskLines.Count == 0)
            {
                if (!hasBulletin)
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

                // The desk's own stories (days 7-15, Q9), after the news.
                if (world.tomorrow.deskLines.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine(UiText.Get("briefing.deskHeader"));

                    foreach (string line in world.tomorrow.deskLines)
                        sb.AppendLine(UiText.Format("list.bullet", line));
                }
            }

            briefingBodyText.text = sb.ToString();
        }

        briefingPanel.SetActive(true);
    }

    /// <summary>
    /// Shows the end-of-day report (Papers Please lesson 5): the day's desk
    /// <paramref name="hours"/> and the travellers processed out of the queue,
    /// then a money ledger read in one glance
    /// (its currency named once in its header, the amounts bare in one column)
    /// (the right calls times their pay, the free warnings, the wrong calls
    /// times their fine, the stranding fines, the Debt Relief instalment, any
    /// other money, the shift's net in bold, tonight's bills and, in bold, the
    /// wallet after them), then citations and the departures (the panel's
    /// Helix River shows the timeline: no stability number).
    /// Invokes onGoHome when the player clicks Go Home (or immediately if unwired).
    /// </summary>
    public void ShowResults(WorldState world, ShiftLedger ledger, ShiftReport report, ShiftHours hours, Action onGoHome)
    {
        if (!HasResultsPanel || world == null || ledger == null || report == null)
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
            string cr = UiText.Currency(UiText.WalletForm.Short);
            string Amount(int amount) => UiText.Format("results.amount", amount);
            void Row(string label, int amount) => sb.AppendLine(UiText.Format("results.row", label, Amount(amount)));

            sb.AppendLine(UiText.Format("results.hours", hours.Open, hours.Close));
            sb.AppendLine(report.Waiting > 0
                ? UiText.Format("results.processedOf", report.Processed, report.Queued, report.Waiting)
                : UiText.Format("results.processedAll", report.Processed));
            sb.AppendLine();
            sb.AppendLine(UiText.Format("results.moneyHeader", cr));
            Row(report.PayRate > 0 ? UiText.Format("results.row.pay", report.Right, report.PayRate) : UiText.Format("results.row.payTotal", report.Right), report.Pay);
            if (report.Warned > 0)
                Row(UiText.Format("results.row.warned", report.Warned), 0);
            if (report.Fined > 0)
                Row(report.PenaltyRate > 0 ? UiText.Format("results.row.fined", report.Fined, report.PenaltyRate) : UiText.Format("results.row.finedTotal", report.Fined), -report.Penalties);
            if (report.Stranded > 0)
                Row(UiText.Format(report.StrandingFines > 0 ? "results.row.strandedFined" : "results.row.stranded", report.Stranded), -report.StrandingFines);
            if (report.DebtOwed != Account.Unknown)
                Row(UiText.Format("results.row.instalment", report.DebtOwed), -report.Instalment);
            if (report.Other != 0)
                Row(UiText.Get("results.row.other"), report.Other);
            sb.AppendLine(UiText.Format("results.rowBold", UiText.Get("results.row.net"), Amount(report.Net)));
            Row(UiText.Get("results.row.bills"), -report.Bills);
            sb.AppendLine(UiText.Format("results.rowBold", UiText.Get("results.row.after"), UiText.Format("results.wallet", report.WalletAfterBills, report.WalletNow)));
            sb.AppendLine();

            if (world.citationsToday > 0)
                sb.AppendLine(UiText.Format("results.citations", world.citationsToday));

            if (ledger.UnprovenDenialCount > 0)
                sb.AppendLine(UiText.Format("results.unproven", ledger.UnprovenDenialCount));

            if (ledger.DetainedCount > 0)
                sb.AppendLine(UiText.Format("results.detained", ledger.DetainedCount));

            sb.AppendLine(UiText.Format("results.leisureDepartures", ledger.LeisureDepartures));
            sb.AppendLine(UiText.Format("results.debtReliefDepartures", ledger.DebtReliefDepartures, ledger.DebtPutToWork, cr));

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
