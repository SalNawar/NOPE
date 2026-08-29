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
    public void ShowBriefing(WorldState world, Action onStartShift) =>
        ShowBriefing(world, null, onStartShift);

    /// <summary>
    /// Shows the morning briefing: THE TEMPORAL TIMES headlines + today's travel
    /// directives (from the day plan), then the tomorrow-package lines. The plan
    /// block is skipped when null so older callers keep working.
    /// </summary>
    public void ShowBriefing(WorldState world, DayPlanSO plan, Action onStartShift)
    {
        if (!HasBriefingPanel || world == null)
        {
            onStartShift?.Invoke();
            return;
        }

        _onStartShift = onStartShift;

        if (briefingTitleText != null)
            briefingTitleText.text = $"Day {world.day} — Morning Briefing";

        if (briefingBodyText != null)
        {
            var sb = new System.Text.StringBuilder();
            bool hasTimes = plan != null && (plan.BriefingHeadlines.Count > 0 || plan.ActiveTravelRules.Count > 0);

            if (hasTimes)
            {
                sb.AppendLine("— THE TEMPORAL TIMES —");
                sb.AppendLine();

                foreach (string line in plan.BriefingHeadlines)
                    sb.AppendLine(line);

                if (plan.ActiveTravelRules.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("— DIRECTIVES IN EFFECT —");
                    sb.AppendLine();

                    foreach (TravelRuleSO rule in plan.ActiveTravelRules)
                    {
                        if (rule != null)
                            sb.AppendLine("• " + rule.Summary());
                    }
                }

                sb.AppendLine();
            }

            if (world.tomorrow.briefingLines.Count == 0 && world.tomorrow.newsLines.Count == 0 && !hasTimes)
            {
                sb.AppendLine("No directives. Process subjects accurately.");
            }
            else
            {
                foreach (string line in world.tomorrow.briefingLines)
                    sb.AppendLine("• " + line);

                if (world.tomorrow.newsLines.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("— TIMELINE NEWS —");

                    foreach (string line in world.tomorrow.newsLines)
                        sb.AppendLine("• " + line);
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
            resultsTitleText.text = $"Day {world.day} — Shift Report";

        if (resultsBodyText != null)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Subjects processed: {ledger.verdicts.Count}");
            sb.AppendLine($"Correct: {ledger.CorrectCount}   Wrong: {ledger.WrongCount}");
            sb.AppendLine();
            sb.AppendLine($"Pay earned: +{ledger.TotalPay}");

            if (ledger.TotalPenalties > 0)
                sb.AppendLine($"Citation penalties: -{ledger.TotalPenalties}");

            sb.AppendLine($"Net: {ledger.NetMoney:+0;-0;0} credits   (Balance: {world.money})");
            sb.AppendLine();
            sb.AppendLine($"Timeline stability: {world.timelineStability:0}% ({ledger.TotalStabilityDelta:+0.#;-0.#;0} today)");

            if (world.citationsToday > 0)
                sb.AppendLine($"Citations today: {world.citationsToday}");

            if (ledger.UnprovenDenialCount > 0)
                sb.AppendLine($"Undocumented denials: {ledger.UnprovenDenialCount} (scan the evidence before denying)");

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
