using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// What the painted PC's CRT shows from the chair (desk v3, round 12; Saleh's
/// crop of the desk PC's screen: the XP-style wallpaper, icons and a banner
/// too small to read at desk distance, the clock in a floating box, no CRT
/// feel). The desktop on the office PC is its live clone at about a sixth of
/// its size, so from the chair it reads as noise; this is the CRT's desk
/// view instead: the desktop's own wallpaper filling the glass, one status
/// line large enough to read from the chair (the desktop's own idle line,
/// "WAITING FOR THE NEXT TRAVELLER", or who is at the desk), and a readout
/// strip along the bottom like the taskbar's tray, with the shift clock and
/// today's date and day. It is a world-space canvas on the desktop's layer, in
/// front of the desktop, so the clone camera draws it onto the glass
/// (PaintedCrt's curved face, with its scanlines); it is off while the PC is
/// open (PcFrame: the full desktop, unchanged), so only the office's CRT shows
/// it. It reads the desktop (the wallpaper, the idle line, the
/// investigation's status line) and the game (the shift clock, the day), and
/// writes nothing of theirs: UI-PC's desktop is untouched (the hand-off is in
/// docs/pending/DESK-V3.md, round 12). Its content keeps inside the glass's
/// visible share of the picture (PaintedCrt.Fit: the bezel and the overscan
/// hide the rest). The desk CRT's old clock plate (CrtClock, retired by UI-PC)
/// is kept hidden under it. Allocation-free per frame: each text is written
/// only when its source changes.
/// </summary>
public sealed class PaintedCrtReadout : MonoBehaviour
{
    /// <summary>The desktop canvas's size (canvas units).</summary>
    private static readonly Vector2 DesktopSize = new Vector2(1440f, 1080f);

    /// <summary>The status line's band, as shares of the visible glass (bottom, top).</summary>
    private static readonly Vector2 StatusBand = new Vector2(0.44f, 0.74f);

    /// <summary>The readout strip along the bottom, as a share of the visible glass's height.</summary>
    private const float StripHeight = 0.26f;

    /// <summary>The phosphor's warm cream (the status line).</summary>
    private static readonly Color Cream = new Color(1f, 0.93f, 0.78f, 1f);

    /// <summary>The phosphor's green (the clock).</summary>
    private static readonly Color Phosphor = new Color(0.72f, 1f, 0.78f, 1f);

    /// <summary>The kit's lamp amber (the strip's rule and the day).</summary>
    private static readonly Color Amber = new Color(1f, 0.72f, 0.3f, 1f);

    /// <summary>The bands' dark glass.</summary>
    private static readonly Color Band = new Color(0.02f, 0.06f, 0.07f, 0.8f);

    private Canvas _canvas;
    private PcFrame _frame;
    private Image _wallpaper;
    private Image _wallpaperSource;
    private GameObject _idleSource;
    private TMP_Text _idleText;
    private TMP_Text _hintSource;
    private TMP_Text _status;
    private GameObject _statusBand;
    private TMP_Text _time;
    private TMP_Text _date;
    private TMP_Text _day;
    private ShiftClockDriver _clock;
    private GameObject _oldPlate;
    private string _shownStatus;
    private int _shownMinute = int.MinValue;
    private int _shownDay = int.MinValue;
    private Rect _dateRect;

    /// <summary>The date line's rectangle as shares of the desktop (0 to 1, from the bottom left): where the calendar's click goes on the glass (OfficeSceneBinder.BindPaintedCalendar).</summary>
    public Rect DateRect => _dateRect;

    /// <summary>The date's text (the calendar's click animates it).</summary>
    public Transform DateText => _date != null ? _date.transform : transform;

    /// <summary>
    /// Builds the readout beside <paramref name="desktop"/> (the desktop
    /// canvas, under PcDesktop), keeping its content inside
    /// <paramref name="visible"/> (the share of the desktop the CRT's glass
    /// shows, PaintedCrt.Fit), off while <paramref name="frame"/> is open.
    /// Idempotent.
    /// </summary>
    public static PaintedCrtReadout Create(Canvas desktop, PcFrame frame, Vector2 visible)
    {
        if (desktop == null)
            return null;
        Transform host = desktop.transform.parent;
        Transform found = host != null ? host.Find("PaintedCrtReadout") : null;
        if (found != null && found.TryGetComponent(out PaintedCrtReadout existing))
            return existing;

        var go = new GameObject("PaintedCrtReadout", typeof(RectTransform), typeof(Canvas));
        go.layer = desktop.gameObject.layer;
        var rect = (RectTransform)go.transform;
        rect.SetParent(host, false);
        var desk = (RectTransform)desktop.transform;
        rect.sizeDelta = DesktopSize;
        rect.localScale = desk.localScale;
        rect.localRotation = desk.localRotation;
        // In front of the desktop (the desktop's cameras look down +z from -10): it covers the desktop on the glass.
        rect.localPosition = desk.localPosition + new Vector3(0f, 0f, -1f);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = desktop.worldCamera;
        canvas.overrideSorting = true;
        canvas.sortingLayerID = desktop.sortingLayerID;
        canvas.sortingOrder = desktop.sortingOrder + 50;

        PaintedCrtReadout readout = go.AddComponent<PaintedCrtReadout>();
        readout.Build(desktop, frame, visible);
        return readout;
    }

    private void Build(Canvas desktop, PcFrame frame, Vector2 visible)
    {
        _canvas = GetComponent<Canvas>();
        _frame = frame;
        Transform desk = desktop.transform;
        Transform wall = desk.Find("Desktop");
        _wallpaperSource = wall != null ? wall.GetComponent<Image>() : null;
        Transform idle = desk.Find("IdleScreen");
        _idleSource = idle != null ? idle.gameObject : null;
        Transform idleText = idle != null ? idle.Find("IdleText") : null;
        _idleText = idleText != null ? idleText.GetComponent<TMP_Text>() : null;
        _hintSource = InvestigationHint(desk);
        _clock = FindAnyObjectByType<ShiftClockDriver>(FindObjectsInactive.Include);
        CrtClock plate = FindAnyObjectByType<CrtClock>(FindObjectsInactive.Include);
        _oldPlate = plate != null ? plate.gameObject : null;
        TMP_FontAsset font = _idleText != null ? _idleText.font : TMP_Settings.defaultFontAsset;

        // The glass's visible rectangle (shares of the desktop), a margin inside it for the tube's rounded corners.
        float sx = Mathf.Clamp01(visible.x) * 0.94f, sy = Mathf.Clamp01(visible.y) * 0.94f;
        var safe = new Rect((1f - sx) / 2f, (1f - sy) / 2f, sx, sy);

        _wallpaper = Panel(transform, "Wallpaper", new Rect(0f, 0f, 1f, 1f), Color.white);
        var fitter = _wallpaper.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        CopyWallpaper();

        Rect status = Inner(safe, 0f, StatusBand.x, 1f, StatusBand.y);
        Image band = Panel(transform, "StatusBand", status, Band);
        _statusBand = band.gameObject;
        Panel(band.transform, "RuleTop", new Rect(0f, 0.97f, 1f, 0.03f), Amber * new Color(1f, 1f, 1f, 0.7f));
        Panel(band.transform, "RuleBottom", new Rect(0f, 0f, 1f, 0.03f), Amber * new Color(1f, 1f, 1f, 0.7f));
        _status = Text(band.transform, "Status", new Rect(0.04f, 0.08f, 0.92f, 0.84f), font, 132f, Cream, TextAlignmentOptions.Center, FontStyles.Bold | FontStyles.UpperCase);
        _status.textWrappingMode = TextWrappingModes.Normal;

        Rect strip = Inner(safe, 0f, 0f, 1f, StripHeight);
        Image stripPanel = Panel(transform, "Readout", strip, Band);
        Panel(stripPanel.transform, "Rule", new Rect(0f, 0.97f, 1f, 0.03f), Amber);
        _time = Text(stripPanel.transform, "Time", new Rect(0.03f, 0.06f, 0.42f, 0.86f), font, 190f, Phosphor, TextAlignmentOptions.Left, FontStyles.Bold);
        _date = Text(stripPanel.transform, "Date", new Rect(0.46f, 0.44f, 0.51f, 0.48f), font, 92f, Cream, TextAlignmentOptions.Right, FontStyles.Bold);
        _day = Text(stripPanel.transform, "Day", new Rect(0.46f, 0.08f, 0.51f, 0.36f), font, 70f, Amber, TextAlignmentOptions.Right, FontStyles.UpperCase);
        _dateRect = new Rect(strip.x + strip.width * 0.46f, strip.y + strip.height * 0.08f, strip.width * 0.51f, strip.height * 0.84f);
        LateUpdate();
    }

    /// <summary>The investigation's status line (MatchBoard's idle plate: who is at the desk, or the waiting words), or null.</summary>
    private static TMP_Text InvestigationHint(Transform desktop)
    {
        MatchBoard board = desktop.GetComponentInChildren<MatchBoard>(true);
        FieldInfo field = typeof(MatchBoard).GetField("idleText", BindingFlags.NonPublic | BindingFlags.Instance);
        return board != null && field != null ? field.GetValue(board) as TMP_Text : null;
    }

    /// <summary>Shows the readout while the PC is closed; keeps each line in step with its source (written only on a change).</summary>
    private void LateUpdate()
    {
        bool shown = _frame == null || !_frame.IsOpen;
        if (_canvas != null && _canvas.enabled != shown)
            _canvas.enabled = shown;
        if (_oldPlate != null && _oldPlate.activeSelf)
            _oldPlate.SetActive(false);
        if (!shown)
            return;

        CopyWallpaper();

        // The desktop's idle line between travellers; with a traveller at the desk, who it is.
        string status = _idleSource != null && _idleSource.activeInHierarchy && _idleText != null ? _idleText.text
                      : _hintSource != null ? _hintSource.text : null;
        if (!ReferenceEquals(status, _shownStatus))
        {
            _shownStatus = status;
            bool any = !string.IsNullOrEmpty(status);
            if (_statusBand.activeSelf != any)
                _statusBand.SetActive(any);
            if (any)
                _status.text = status;
        }

        ShiftClock clock = _clock != null ? _clock.Clock : null;
        int minute = clock != null ? Mathf.FloorToInt(clock.CurrentMinute) : -1;
        if (minute != _shownMinute)
        {
            _shownMinute = minute;
            _time.text = clock != null ? ShiftClock.Format(clock.CurrentMinute) : string.Empty;
        }

        WorldState world = RunManager.HasInstance ? RunManager.Instance.World : null;
        int day = world != null ? world.day : 0;
        if (day != _shownDay)
        {
            _shownDay = day;
            ContentLibrarySO library = RunManager.HasInstance ? RunManager.Instance.Library : null;
            string today = library != null && day > 0 ? AgencyCalendar.Today(library.Agency.firstDate, day) : null;
            _date.text = today != null ? today.ToUpperInvariant() : string.Empty;
            _day.text = day > 0 ? UiText.Pc.Format("tray.day", day) : string.Empty;
        }
    }

    /// <summary>The desktop's wallpaper (the theme changes it when a culture leads or the world changes), at its own aspect.</summary>
    private void CopyWallpaper()
    {
        if (_wallpaperSource == null || _wallpaper == null || _wallpaper.sprite == _wallpaperSource.sprite)
            return;
        _wallpaper.sprite = _wallpaperSource.sprite;
        _wallpaper.color = _wallpaperSource.sprite != null ? Color.white : _wallpaperSource.color;
        Sprite s = _wallpaperSource.sprite;
        _wallpaper.GetComponent<AspectRatioFitter>().aspectRatio = s != null && s.rect.height > 0f ? s.rect.width / s.rect.height : DesktopSize.x / DesktopSize.y;
    }

    /// <summary>The rectangle (x0, y0) to (x1, y1) of <paramref name="r"/> (shares of it).</summary>
    private static Rect Inner(Rect r, float x0, float y0, float x1, float y1) =>
        new Rect(r.x + r.width * x0, r.y + r.height * y0, r.width * (x1 - x0), r.height * (y1 - y0));

    private static Image Panel(Transform parent, string name, Rect anchors, Color colour)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.layer = parent.gameObject.layer;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        Place(rect, anchors);
        var image = go.GetComponent<Image>();
        image.color = colour;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text Text(Transform parent, string name, Rect anchors, TMP_FontAsset font, float size, Color colour, TextAlignmentOptions alignment, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.layer = parent.gameObject.layer;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        Place(rect, anchors);
        var text = go.GetComponent<TextMeshProUGUI>();
        if (font != null)
            text.font = font;
        text.enableAutoSizing = true;
        text.fontSizeMax = size;
        text.fontSizeMin = size * 0.4f;
        text.color = colour;
        text.alignment = alignment;
        text.fontStyle = style;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        text.text = string.Empty;
        return text;
    }

    private static void Place(RectTransform rect, Rect anchors)
    {
        rect.anchorMin = anchors.min;
        rect.anchorMax = anchors.max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }
}
