using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minimal UI controller that displays a CaseInstance and lets the player pick an Era.
/// This is intentionally simple and prototype-friendly.
/// </summary>
public sealed class OfficeUIController : MonoBehaviour
{
    [Header("Text")]
    /// <summary>Shows the visitor name and intro.</summary>
    [SerializeField] private TMP_Text visitorText;

    /// <summary>Shows the first document text.</summary>
    [SerializeField] private TMP_Text doc1Text;

    /// <summary>Shows the second document text.</summary>
    [SerializeField] private TMP_Text doc2Text;

    /// <summary>Shows the result after the player chooses.</summary>
    [SerializeField] private TMP_Text resultText;

    [Header("Era Buttons")]
    /// <summary>Parent transform where era buttons will be spawned.</summary>
    [SerializeField] private Transform eraButtonsRoot;

    /// <summary>Disabled template button used to clone era choice buttons.</summary>
    [SerializeField] private Button eraButtonTemplate;

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

    /// <summary>All spawned buttons so we can clear/refresh cleanly.</summary>
    private readonly List<Button> _spawnedButtons = new();

    /// <summary>Callback invoked when the player chooses an era.</summary>
    private Action<EraSO> _onEraChosen;

    /// <summary>True while waiting for the player’s choice.</summary>
    private bool _isAwaitingChoice;

    /// <summary>
    /// Displays the case and builds era choice buttons.
    /// </summary>
    public void ShowCase(CaseInstance inst, IReadOnlyList<EraSO> eras, Action<EraSO> onEraChosen)
    {
        if (inst == null)
        {
            Debug.LogError("OfficeUIController.ShowCase called with null CaseInstance.");
            return;
        }

        if (eras == null || eras.Count == 0)
        {
            Debug.LogError("OfficeUIController.ShowCase called with no eras.");
            return;
        }

        if (eraButtonsRoot == null || eraButtonTemplate == null)
        {
            Debug.LogError("OfficeUIController is missing eraButtonsRoot / eraButtonTemplate references.");
            return;
        }

        _onEraChosen = onEraChosen;
        _isAwaitingChoice = true;

        // Close any citation slip left over from the previous case.
        if (citationPanel != null)
            citationPanel.SetActive(false);

        // Populate text.
        if (visitorText != null)
            visitorText.text = $"{inst.visitorDisplayName}\n{inst.introLine}";

        if (resultText != null)
            resultText.text = string.Empty;

        // Documents (support 0, 1, or 2+ docs safely).
        if (doc1Text != null)
            doc1Text.text = inst.documents.Count >= 1 ? inst.documents[0].renderedText : "(No document)";

        if (doc2Text != null)
            doc2Text.text = inst.documents.Count >= 2 ? inst.documents[1].renderedText : "(No document)";

        // Rebuild buttons.
        ClearEraButtons();
        BuildEraButtons(eras);
        SetEraButtonsInteractable(true);
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Disables the UI and clears callbacks.
    /// </summary>
    public void Hide()
    {
        _isAwaitingChoice = false;
        _onEraChosen = null;
        ClearEraButtons();

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Spawns one button per era using the template.
    /// </summary>
    private void BuildEraButtons(IReadOnlyList<EraSO> eras)
    {
        for (int i = 0; i < eras.Count; i++)
        {
            EraSO era = eras[i];
            if (era == null)
                continue;

            Button btn = Instantiate(eraButtonTemplate, eraButtonsRoot);
            btn.gameObject.SetActive(true);
            _spawnedButtons.Add(btn);

            // Set label (supports TMP and legacy Text).
            TMP_Text tmp = btn.GetComponentInChildren<TMP_Text>();
            if (tmp != null)
                tmp.text = era.displayName;

            Text legacy = btn.GetComponentInChildren<Text>();
            if (legacy != null)
                legacy.text = era.displayName;

            // Capture local for closure safety.
            EraSO capturedEra = era;
            btn.onClick.AddListener(() => HandleEraClicked(capturedEra));
        }
    }

    /// <summary>
    /// Handles a player clicking an era choice button.
    /// </summary>
    private void HandleEraClicked(EraSO chosenEra)
    {
        if (!_isAwaitingChoice)
            return;

        _isAwaitingChoice = false;
        SetEraButtonsInteractable(false);

        _onEraChosen?.Invoke(chosenEra);
    }

    /// <summary>
    /// Enables/disables all spawned buttons.
    /// </summary>
    private void SetEraButtonsInteractable(bool interactable)
    {
        for (int i = 0; i < _spawnedButtons.Count; i++)
        {
            if (_spawnedButtons[i] != null)
                _spawnedButtons[i].interactable = interactable;
        }
    }

    /// <summary>
    /// Destroys any previously spawned era buttons (keeps the template).
    /// </summary>
    private void ClearEraButtons()
    {
        for (int i = 0; i < _spawnedButtons.Count; i++)
        {
            if (_spawnedButtons[i] != null)
                Destroy(_spawnedButtons[i].gameObject);
        }

        _spawnedButtons.Clear();
    }

    /// <summary>
    /// Updates the result label (call from GameManager after validation).
    /// </summary>
    public void SetResultText(string text)
    {
        if (resultText != null)
            resultText.text = text;
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
            moneyText.text = $"Credits: {world.money}";

        if (stabilityText != null)
            stabilityText.text = $"Stability: {world.timelineStability:0}%";

        if (dayText != null)
            dayText.text = $"Day {world.day}";
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

        if (resultText != null)
        {
            resultText.text = verdict.correct
                ? $"CORRECT  (+{verdict.payAwarded} credits)"
                : $"WRONG  ({verdict.stabilityDelta:+0.#;-0.#} stability{(verdict.moneyPenalty > 0 ? $", -{verdict.moneyPenalty} credits" : string.Empty)})";
        }

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

        Action cb = _onCitationDismissed;
        _onCitationDismissed = null;
        cb?.Invoke();
    }
}
