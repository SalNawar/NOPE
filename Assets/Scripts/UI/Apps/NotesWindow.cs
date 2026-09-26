using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Notes app (the PC spec's NT1, §2.14): a list of days on the left
/// (today first) and the chosen day's page on the right: CLIPPINGS, pasted
/// clips as cards (the text, where it came from, X to remove) grouped under
/// each traveller's name (Notes.Groups), and NOTES, one multi-line field of
/// at most DesktopConfigSO.notesMaxChars characters with its counter. An
/// empty page shows a hint. Pages live in WorldState.notes (Notes.Page, at
/// most notesDaysKept days) and are saved with the run. "Paste clipping"
/// pastes the system clipboard's text as a clip; phase 20's clipboard calls
/// <see cref="Clip"/> with the clip's source (and Ctrl+V with no field focused).
/// </summary>
public sealed class NotesWindow : MonoBehaviour
{
    /// <summary>The Notes limits (days kept, characters, clippings).</summary>
    [SerializeField] private DesktopConfigSO config;

    /// <summary>The day list's content (day buttons are cloned into it).</summary>
    [SerializeField] private RectTransform dayListRoot;

    /// <summary>A day button template (inactive).</summary>
    [SerializeField] private Button dayTemplate;

    /// <summary>The page's heading ("NOTES · DAY 3").</summary>
    [SerializeField] private TMP_Text pageTitle;

    /// <summary>Where the clippings' group headings and cards are cloned (a vertical layout).</summary>
    [SerializeField] private RectTransform clipRoot;

    /// <summary>A group heading template (inactive): the traveller's name.</summary>
    [SerializeField] private TMP_Text groupTemplate;

    /// <summary>A card template (inactive): "Text", "Label" and a "RemoveButton".</summary>
    [SerializeField] private RectTransform clipTemplate;

    /// <summary>The empty page's hint, and the "page is full" line.</summary>
    [SerializeField] private TMP_Text hintText;

    /// <summary>Pastes the system clipboard's text as a clipping.</summary>
    [SerializeField] private Button pasteButton;

    /// <summary>The typed notes.</summary>
    [SerializeField] private TMP_InputField notesField;

    /// <summary>The characters used of the limit ("312 / 2000").</summary>
    [SerializeField] private TMP_Text counterText;

    private readonly List<Button> _days = new List<Button>();
    private readonly List<int> _dayNumbers = new List<int>();
    private readonly List<GameObject> _drawn = new List<GameObject>();
    private int _day;
    private bool _full;

    private int MaxDays => config != null ? config.notesDaysKept : 0;
    private int MaxChars => config != null ? config.notesMaxChars : 0;
    private int MaxClippings => config != null ? config.notesMaxClippings : 0;
    private static WorldState World => RunManager.HasInstance ? RunManager.Instance.World : null;

    private void Awake()
    {
        if (dayTemplate != null)
            dayTemplate.gameObject.SetActive(false);
        if (groupTemplate != null)
            groupTemplate.gameObject.SetActive(false);
        if (clipTemplate != null)
            clipTemplate.gameObject.SetActive(false);
        if (pasteButton != null)
            pasteButton.onClick.AddListener(PasteClipboard);
        if (notesField != null)
        {
            notesField.lineType = TMP_InputField.LineType.MultiLineNewline;
            notesField.characterLimit = MaxChars;
            notesField.onValueChanged.AddListener(HandleTyped);
        }
    }

    /// <summary>Opens on today's page.</summary>
    private void OnEnable()
    {
        WorldState world = World;
        ShowDay(world != null ? world.day : 1);
    }

    /// <summary>Shows a day's page (created when missing).</summary>
    private void ShowDay(int day)
    {
        _day = day;
        _full = false;
        DrawDays();
        DrawPage();
    }

    /// <summary>
    /// Adds a clipping to the shown day's page (the entry point phase 20's
    /// clipboard calls with the clip's source); false when the page is full
    /// or the clip is blank.
    /// </summary>
    public bool Clip(Clipping clipping)
    {
        NotePage page = CurrentPage();
        bool added = Notes.Clip(page, clipping, MaxClippings);
        _full = !added && page != null && clipping != null && !string.IsNullOrWhiteSpace(clipping.text);
        DrawDays();
        DrawPage();
        return added;
    }

    /// <summary>"Paste clipping": the system clipboard's text as a clip from no case.</summary>
    private void PasteClipboard() =>
        Clip(new Clipping { text = GUIUtility.systemCopyBuffer ?? string.Empty, label = UiText.Get("notes.pasted") });

    /// <summary>The shown day's page (null outside a run).</summary>
    private NotePage CurrentPage()
    {
        WorldState world = World;
        return world != null ? Notes.Page(world.notes, _day, MaxDays) : null;
    }

    /// <summary>The typed notes are kept on the page as they are typed (cut at the limit).</summary>
    private void HandleTyped(string text)
    {
        NotePage page = CurrentPage();
        if (page == null)
            return;
        page.text = Notes.Trim(text, MaxChars);
        ShowCounter(page);
        ShowHint(page);
    }

    /// <summary>The day list: today and every day with a page, newest first.</summary>
    private void DrawDays()
    {
        WorldState world = World;
        _dayNumbers.Clear();
        if (world != null)
        {
            _dayNumbers.Add(world.day);
            foreach (NotePage p in world.notes)
                if (p != null && !_dayNumbers.Contains(p.day))
                    _dayNumbers.Add(p.day);
        }
        if (!_dayNumbers.Contains(_day) && _day >= 1)
            _dayNumbers.Add(_day);
        _dayNumbers.Sort((a, b) => b.CompareTo(a));

        if (dayListRoot == null || dayTemplate == null)
            return;
        while (_days.Count < _dayNumbers.Count)
        {
            Button b = Instantiate(dayTemplate, dayListRoot);
            b.gameObject.name = "DayButton";
            int index = _days.Count;
            b.onClick.AddListener(() => ShowDay(_dayNumbers[index]));
            _days.Add(b);
        }
        for (int i = 0; i < _days.Count; i++)
        {
            bool used = i < _dayNumbers.Count;
            _days[i].gameObject.SetActive(used);
            if (!used)
                continue;
            int day = _dayNumbers[i];
            TMP_Text label = _days[i].GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = world != null && day == world.day ? UiText.Format("notes.today", day) : UiText.Format("notes.day", day);
            AppRows.MarkSelected(_days[i], day == _day);
        }
    }

    /// <summary>The page: its heading, the clippings grouped by traveller, the typed notes and the counter.</summary>
    private void DrawPage()
    {
        foreach (GameObject go in _drawn)
            if (go != null)
                Destroy(go);
        _drawn.Clear();

        NotePage page = CurrentPage();
        if (pageTitle != null)
            pageTitle.text = UiText.Format("notes.pageTitle", _day);

        if (page != null && clipRoot != null && clipTemplate != null)
            foreach (ClippingGroup group in Notes.Groups(page))
            {
                if (groupTemplate != null)
                {
                    TMP_Text heading = Instantiate(groupTemplate, clipRoot);
                    heading.gameObject.SetActive(true);
                    heading.text = group.Traveller.Length > 0 ? group.Traveller : UiText.Get("notes.loose");
                    _drawn.Add(heading.gameObject);
                }
                foreach (int index in group.Indexes)
                    _drawn.Add(Card(page.clippings[index], index));
            }

        if (notesField != null)
            notesField.SetTextWithoutNotify(page != null ? page.text : string.Empty);
        ShowCounter(page);
        ShowHint(page);
    }

    /// <summary>One clipping's card: its text, where it came from, and X (removes it).</summary>
    private GameObject Card(Clipping clip, int index)
    {
        RectTransform card = Instantiate(clipTemplate, clipRoot);
        card.gameObject.SetActive(true);
        SetChild(card, "Text", clip.text);
        SetChild(card, "Label", clip.label);
        Transform remove = card.Find("RemoveButton");
        if (remove != null && remove.TryGetComponent(out Button button))
            button.onClick.AddListener(() =>
            {
                Notes.Unclip(CurrentPage(), index);
                _full = false;
                DrawPage();
            });
        return card.gameObject;
    }

    private void ShowCounter(NotePage page)
    {
        if (counterText != null)
            counterText.text = UiText.Format("notes.counter", page != null ? page.text.Length : 0, MaxChars);
    }

    /// <summary>The hint on an empty page, or the "page is full" line after a refused paste.</summary>
    private void ShowHint(NotePage page)
    {
        if (hintText == null)
            return;
        bool show = _full || Notes.IsEmpty(page);
        hintText.gameObject.SetActive(show);
        if (show)
            hintText.text = _full ? UiText.Format("notes.full", MaxClippings) : UiText.Get("notes.hint");
    }

    private static void SetChild(Transform parent, string name, string text)
    {
        Transform child = parent.Find(name);
        if (child != null && child.TryGetComponent(out TMP_Text t))
            t.text = text;
    }
}
