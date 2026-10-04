using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Investigation app's Calendar (the PC workbench spec IA6; lesson D10:
/// the date is a document too): the agency calendar's page of the day (its
/// weekday, the day's number, the month and year, the shift day) over the
/// week's strip with today marked, and today's date as a value: a click
/// holds it on the workbench (MatchBoard.PickToday) to be matched with a
/// departure date or a Valid Until. Today is the agency block's first date
/// plus the shift day (AgencyCalendar), written as the papers write dates. A
/// day source: it works between travellers. Each pane has one; DayReference
/// writes them all.
/// </summary>
public sealed class CalendarView : AppView
{
    /// <summary>The weekday ("Wednesday").</summary>
    [SerializeField] private TMP_Text weekdayText;

    /// <summary>The day's number, large ("30").</summary>
    [SerializeField] private TMP_Text dayText;

    /// <summary>The month and year ("September 2150").</summary>
    [SerializeField] private TMP_Text monthText;

    /// <summary>The shift day ("Shift day 3").</summary>
    [SerializeField] private TMP_Text shiftText;

    /// <summary>Today's date as a value (the button holding it on the workbench).</summary>
    [SerializeField] private Button todayButton;

    /// <summary>Today's date as the papers print it ("30 Sep 2150").</summary>
    [SerializeField] private TMP_Text todayText;

    /// <summary>The week's seven days, Monday first, each its weekday over its number.</summary>
    [SerializeField] private TMP_Text[] weekTexts = new TMP_Text[0];

    /// <summary>The plate marking today in the week's strip (moved under today's day).</summary>
    [SerializeField] private RectTransform todayMark;

    /// <summary>The workbench today is held on.</summary>
    [SerializeField] private MatchBoard board;

    private string _today;

    /// <inheritdoc />
    public override AppTab Tab => AppTab.Calendar;

    /// <summary>Today's date as the papers print it (null when the agency block has no readable first date).</summary>
    public string Today => _today;

    private void Awake()
    {
        if (todayButton != null)
            todayButton.onClick.AddListener(() =>
            {
                if (board != null && _today != null)
                    board.PickToday(_today);
            });
    }

    /// <summary>Draws shift day <paramref name="day"/> of <paramref name="agency"/>'s calendar (no readable first date: today reads as unknown and cannot be held).</summary>
    public void SetDay(AgencyContent agency, int day)
    {
        DateTime date = default;
        bool known = agency != null && AgencyCalendar.TryToday(agency.firstDate, day, out date);
        _today = known ? AgencyCalendar.Write(date) : null;
        CultureInfo en = CultureInfo.InvariantCulture;
        Write(weekdayText, known ? date.ToString("dddd", en) : string.Empty);
        Write(dayText, known ? date.Day.ToString(en) : "?");
        Write(monthText, known ? date.ToString("MMMM yyyy", en) : UiText.Get("calendar.unknown"));
        Write(shiftText, UiText.Format("calendar.shiftDay", day));
        Write(todayText, _today ?? UiText.Get("calendar.unknown"));
        if (todayButton != null)
        {
            todayButton.interactable = known;
            AppRow.Mark(todayButton.gameObject, AppTab.Calendar, EntryKeys.CalendarToday, UiText.Get("calendar.todayTitle"), UiText.Get("calendar.today"),
                        _today ?? string.Empty, todayButton);
        }

        int monday = known ? ((int)date.DayOfWeek + 6) % 7 : -1;
        for (int i = 0; i < weekTexts.Length; i++)
        {
            if (weekTexts[i] == null)
                continue;
            weekTexts[i].text = known ? date.AddDays(i - monday).ToString("ddd", en) + "\n" + date.AddDays(i - monday).Day.ToString(en) : string.Empty;
            weekTexts[i].fontStyle = i == monday ? FontStyles.Bold : FontStyles.Normal;
        }
        if (todayMark != null)
        {
            todayMark.gameObject.SetActive(known && monday >= 0 && monday < weekTexts.Length && weekTexts[monday] != null);
            if (todayMark.gameObject.activeSelf)
            {
                var cell = (RectTransform)weekTexts[monday].transform;
                todayMark.anchorMin = cell.anchorMin;
                todayMark.anchorMax = cell.anchorMax;
                todayMark.offsetMin = cell.offsetMin;
                todayMark.offsetMax = cell.offsetMax;
            }
        }
    }

    /// <summary>A text, when wired.</summary>
    private static void Write(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }
}
