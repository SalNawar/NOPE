using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Chrono Converter desktop app: translates era-native birth dates to the
/// modern calendar and back, and checks the year against the selected era's
/// plausible span. Informational layer only — it sharpens the clerk's eye but
/// formal proof still runs through the compare bar (mismatch / origin /
/// record). Era list is injected by InvestigationUIController once per case.
/// </summary>
public sealed class CalendarConverterWindowController : MonoBehaviour
{
    [Header("Input")]
    /// <summary>Date text to convert ("21 Elaphebolion 437" or a bare year "1899").</summary>
    [SerializeField] private TMP_InputField dateInput;

    /// <summary>Cycles through the library's eras; its label shows the selected era.</summary>
    [SerializeField] private Button eraButton;

    [Header("Action")]
    [SerializeField] private Button convertButton;

    [Header("Output")]
    [SerializeField] private TMP_Text outputText;

    /// <summary>Eras with calendar data, injected per day.</summary>
    private IReadOnlyList<EraSO> _eras;

    /// <summary>Selected era index into <see cref="_eras"/>.</summary>
    private int _eraIndex;

    private void Awake()
    {
        if (eraButton != null)
            eraButton.onClick.AddListener(CycleEra);

        if (convertButton != null)
            convertButton.onClick.AddListener(Convert);

        if (dateInput != null)
            dateInput.onSubmit.AddListener(_ => Convert());

        ShowIdle();
    }

    /// <summary>Sets the convertible eras (called once per case by the investigation UI).</summary>
    public void SetEras(IReadOnlyList<EraSO> eras)
    {
        _eras = eras;
        _eraIndex = 0;
        RefreshEraLabel();
    }

    /// <summary>
    /// Receives a date the player clicked on a paper (passport field, citizen
    /// record). Fills the input and, when the era is known, selects it — the
    /// player then just presses CONVERT.
    /// </summary>
    public void OfferDate(string dateText, EraSO era)
    {
        if (string.IsNullOrWhiteSpace(dateText))
            return;

        if (dateInput != null)
            dateInput.text = dateText.Trim();

        if (era != null && _eras != null)
        {
            for (int i = 0; i < _eras.Count; i++)
            {
                if (_eras[i] == era)
                {
                    _eraIndex = i;
                    break;
                }
            }
        }

        RefreshEraLabel();

        if (outputText != null)
            outputText.text = "Date loaded — press CONVERT.";
    }

    private void CycleEra()
    {
        if (_eras == null || _eras.Count == 0)
            return;

        _eraIndex = (_eraIndex + 1) % _eras.Count;
        RefreshEraLabel();
    }

    private void RefreshEraLabel()
    {
        if (eraButton == null)
            return;

        TMP_Text label = eraButton.GetComponentInChildren<TMP_Text>(true);
        EraSO era = CurrentEra;

        if (label != null)
            label.text = era != null ? $"ERA: {era.displayName.ToUpperInvariant()}" : "ERA: —";
    }

    private EraSO CurrentEra => _eras != null && _eraIndex >= 0 && _eraIndex < _eras.Count ? _eras[_eraIndex] : null;

    /// <summary>Converts the input and renders the reckoning.</summary>
    public void Convert()
    {
        if (outputText == null)
            return;

        string input = dateInput != null ? dateInput.text : null;

        if (string.IsNullOrWhiteSpace(input))
        {
            ShowIdle();
            return;
        }

        EraSO era = CurrentEra;

        if (era == null)
        {
            outputText.text = "No era selected.";
            return;
        }

        input = input.Trim();

        // Vice-versa mode: a bare number is a modern year.
        if (CalendarConverter.IsBareYear(input))
        {
            int modernYear = int.Parse(input);
            outputText.text =
                $"MODERN: {CalendarConverter.FormatModern(modernYear)}\n" +
                $"{era.displayName}: {CalendarConverter.FormatEra(modernYear, era.CalendarYearsAreBCE, era.CalendarMonths)}\n" +
                SpanLine(era, modernYear);
            return;
        }

        if (!CalendarConverter.TryParse(input, era.CalendarMonths, out CalendarConverter.ParsedDate parsed))
        {
            outputText.text = "UNREADABLE — enter the date as printed (\"day month year\"), or a bare year.";
            return;
        }

        int modern = CalendarConverter.ToModernYear(parsed.year, era.CalendarYearsAreBCE);

        string monthNote = parsed.nativeMonth
            ? $"native {era.displayName} month"
            : "Gregorian month";

        outputText.text =
            $"MODERN RECKONING: {CalendarConverter.FormatModern(modern)}\n" +
            $"ERA RECKONING: {CalendarConverter.FormatEra(modern, era.CalendarYearsAreBCE, era.CalendarMonths, parsed.day, parsed.monthIndex)}  ({monthNote})\n" +
            SpanLine(era, modern);
    }

    /// <summary>The investigative nudge: does this year even belong to the selected era?</summary>
    private static string SpanLine(EraSO era, int modernYear)
    {
        bool within = CalendarConverter.WithinSpan(modernYear, era.CalendarSpanStart, era.CalendarSpanEnd);

        return within
            ? $"Span check: WITHIN {era.displayName} ({CalendarConverter.FormatModern(era.CalendarSpanStart)} – {CalendarConverter.FormatModern(era.CalendarSpanEnd)})."
            : $"Span check: OUTSIDE {era.displayName} ({CalendarConverter.FormatModern(era.CalendarSpanStart)} – {CalendarConverter.FormatModern(era.CalendarSpanEnd)}) — this date does not belong to the era.";
    }

    private void ShowIdle()
    {
        if (outputText != null)
            outputText.text = "Click a date on any paper,\nor type one — then CONVERT.";

        if (dateInput != null)
            dateInput.text = string.Empty;
    }
}
