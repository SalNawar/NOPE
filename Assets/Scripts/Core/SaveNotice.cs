using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The save slot's notice (RUN-SAFE, BUG-35a: "Failed saves are visible";
/// "a read-only save folder shows a notice"): when SaveSystem reports a
/// problem (a save that could not be written, a save set aside as
/// unreadable or too old, a run recovered from the backup), a strip at the
/// top of the screen says so for a few seconds, wherever the player is. It
/// lives on the persistent RunManager object (RunManager.GetOrCreate adds
/// it), builds its small canvas the first time it is needed, takes no clicks
/// and runs no Update while nothing shows. Its lines are UI strings (keys
/// save.notice.*) with English fallbacks until the string tables carry them.
/// </summary>
public sealed class SaveNotice : MonoBehaviour
{
    /// <summary>How long a notice shows (unscaled seconds).</summary>
    private const float ShowSeconds = 9f;

    /// <summary>The canvas sorting order: above every screen of the game.</summary>
    private const int SortingOrder = 32000;

    /// <summary>The strip's root (null until the first notice).</summary>
    private GameObject _strip;

    /// <summary>The strip's line.</summary>
    private TMP_Text _line;

    /// <summary>When the showing notice goes (unscaled time).</summary>
    private float _until;

    /// <summary>Listens for save problems; one already met (the boot's load) shows at once.</summary>
    private void Awake()
    {
        SaveSystem.ProblemChanged += Show;
        enabled = false;
        if (SaveSystem.Problem != SaveProblem.None)
            Show();
    }

    /// <summary>Stops listening.</summary>
    private void OnDestroy() => SaveSystem.ProblemChanged -= Show;

    /// <summary>Shows the current problem's line (a problem cleared hides the strip).</summary>
    private void Show()
    {
        SaveProblem problem = SaveSystem.Problem;
        if (problem == SaveProblem.None)
        {
            Hide();
            return;
        }

        Build();
        _line.text = Line(problem);
        _strip.SetActive(true);
        _until = Time.unscaledTime + ShowSeconds;
        enabled = true;
        Debug.LogWarning($"[SaveNotice] {problem}: {_line.text} ({SaveSystem.ProblemDetail})");
    }

    /// <summary>Hides the strip once its time is up.</summary>
    private void Update()
    {
        if (Time.unscaledTime >= _until)
            Hide();
    }

    /// <summary>Hides the strip and stops updating.</summary>
    private void Hide()
    {
        if (_strip != null)
            _strip.SetActive(false);
        enabled = false;
    }

    /// <summary>The player's line for <paramref name="problem"/>: its UI string, else the English fallback.</summary>
    private static string Line(SaveProblem problem)
    {
        switch (problem)
        {
            case SaveProblem.WriteFailed: return Text("save.notice.writeFailed", "RECORDS: today's file could not be saved (is the disk full or read-only?). Your run goes on; the last good save stands.");
            case SaveProblem.Unreadable: return Text("save.notice.unreadable", "RECORDS: a damaged save was found. It is kept beside your saves, untouched; this run starts fresh.");
            case SaveProblem.TooOld: return Text("save.notice.tooOld", "RECORDS: a save from an older build was found. It is kept beside your saves; this build starts a new run.");
            default: return Text("save.notice.recovered", "RECORDS: your last save could not be read, so the run was restored from its backup.");
        }
    }

    /// <summary>The UI string <paramref name="key"/>, or <paramref name="english"/> while no table has it.</summary>
    private static string Text(string key, string english)
    {
        string text = UiText.Get(key);
        return string.IsNullOrEmpty(text) || text == key ? english : text;
    }

    /// <summary>Builds the strip once: a screen overlay canvas, a dark band at the top centre, the line on it; nothing takes clicks.</summary>
    private void Build()
    {
        if (_strip != null)
            return;

        _strip = new GameObject("SaveNotice", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        _strip.transform.SetParent(transform, false);
        var canvas = _strip.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        var scaler = _strip.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var band = new GameObject("Band", typeof(RectTransform), typeof(Image));
        band.transform.SetParent(_strip.transform, false);
        var rect = (RectTransform)band.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -24f);
        rect.sizeDelta = new Vector2(1180f, 84f);
        var image = band.GetComponent<Image>();
        image.color = new Color(0.08f, 0.07f, 0.06f, 0.9f);
        image.raycastTarget = false;

        var label = new GameObject("Line", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(band.transform, false);
        var labelRect = (RectTransform)label.transform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(24f, 8f);
        labelRect.offsetMax = new Vector2(-24f, -8f);
        var text = label.GetComponent<TextMeshProUGUI>();
        text.fontSize = 24f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.96f, 0.91f, 0.8f, 1f);
        text.raycastTarget = false;
        _line = text;
    }
}
