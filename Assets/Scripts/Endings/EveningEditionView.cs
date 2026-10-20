using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The ending screen's evening edition (FLOW, BUG-56a; the bible's J6: "an
/// evening-edition epilogue with pictures and the pet's fate", "Title and
/// Quit are reachable"): when the title scene loads after a run ended in this
/// session (fired, bankrupt, the last day), the run's last paper is laid over
/// it first: the masthead (the morning paper's, "The Temporal Times"), the
/// edition of the day, the ending's name as the headline over its picture
/// and story, and the pet's fate in Home's words (EveningEdition.Compose).
/// MENU lays the paper down (a fade) onto the title's own ending page (New
/// Run, the world you leave behind); QUIT GAME quits. Its words are the
/// game's existing strings (briefing.masthead, book.edition, taskbar.start,
/// startmenu.quit), so no string table changes with it.
/// A title opened at boot over an old ending shows its own page only. Built
/// at runtime in the title's own faces (the fonts its texts use), so the
/// title scene needs no rebuild.
/// </summary>
public sealed class EveningEditionView : MonoBehaviour
{
    /// <summary>The page's paper and ink (the UI kit's light ink).</summary>
    private static readonly Color Paper = new Color(0.93f, 0.9f, 0.82f, 1f), Ink = new Color(0.169f, 0.11f, 0.141f, 1f), Backdrop = new Color(0f, 0f, 0f, 0.86f);

    /// <summary>The page's size in reference pixels (the canvas fits 1920x1080 by its height).</summary>
    private static readonly Vector2 PageSize = new Vector2(1000f, 1000f);

    /// <summary>The canvas's sorting order: over the title's panels, under the scene change's shutter (TransitShutter).</summary>
    private const int SortingOrder = 500;

    /// <summary>True once a scene other than the title loaded since the title last did: the run was played this session.</summary>
    private static bool _playedSinceTitle;

    /// <summary>The run config (Resources), loaded once.</summary>
    private static RunConfigSO _config;

    /// <summary>The page's fade.</summary>
    private CanvasGroup _group;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _playedSinceTitle = false;
        _config = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Hook()
    {
        SceneManager.sceneLoaded -= HandleLoaded;
        SceneManager.sceneLoaded += HandleLoaded;
    }

    /// <summary>A scene loaded: a played scene arms the edition; the title after one gets the edition (it shows only when the run ended).</summary>
    private static void HandleLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single)
            return;
        if (_config == null)
            _config = Resources.Load<RunConfigSO>(RunManager.ConfigResourcePath);
        string title = _config != null ? _config.titleSceneName : "TitleScene";
        if (scene.name != title)
        {
            _playedSinceTitle = true;
            return;
        }
        if (!_playedSinceTitle)
            return;
        _playedSinceTitle = false;
        var host = new GameObject("EveningEdition");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<EveningEditionView>();
    }

    /// <summary>After the title's own Start (its ending page shown), lays the edition over it when the run ended; else goes.</summary>
    private IEnumerator Start()
    {
        yield return null;
        RunManager run = RunManager.HasInstance ? RunManager.Instance : null;
        WorldState world = run != null ? run.World : null;
        if (world == null || !ShiftEnd.EndsTheRun(world.endingId))
        {
            Destroy(gameObject);
            yield break;
        }

        ContentLibrarySO library = run.Library;
        EndingSO ending = library != null ? library.GetEndingById(world.endingId) : null;
        GameConfigSO config = run.Config != null ? run.Config.gameConfig : null;
        EveningEditionPage page = EveningEdition.Compose(UiText.Get("briefing.masthead"), UiText.Format("book.edition", world.day),
                                                         ending != null ? ending.displayName : null, "The End",
                                                         ending != null ? ending.bodyText : null, ending != null ? ending.closingCard : null,
                                                         world.pet, library != null ? library.Home.pet : null, HomeEconomy.NeedMax(config));
        Build(page, ending != null ? ending.picture : null);
        Debug.Log($"[EveningEditionView] The evening edition for '{world.endingId}' (day {world.day}).");
        Transit.Fade(_group, 1f, Transit.Seconds);
    }

    /// <summary>Lays the page out: a dark backdrop, the paper and its columns, the two buttons.</summary>
    private void Build(EveningEditionPage page, Sprite picture)
    {
        (TMP_FontAsset display, TMP_FontAsset body) = Faces(gameObject.scene);

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;
        gameObject.AddComponent<GraphicRaycaster>();
        _group = gameObject.AddComponent<CanvasGroup>();
        _group.alpha = 0f;

        Image backdrop = Box(transform, "Backdrop", Backdrop);
        Stretch(backdrop.rectTransform);

        Image sheet = Box(transform, "Page", Paper);
        RectTransform pageRect = sheet.rectTransform;
        pageRect.anchorMin = pageRect.anchorMax = new Vector2(0.5f, 0.5f);
        pageRect.sizeDelta = PageSize;
        var column = sheet.gameObject.AddComponent<VerticalLayoutGroup>();
        column.padding = new RectOffset(56, 56, 36, 36);
        column.spacing = 10f;
        column.childAlignment = TextAnchor.UpperCenter;
        column.childControlWidth = true;
        column.childControlHeight = true;
        column.childForceExpandWidth = true;
        column.childForceExpandHeight = false;

        Line(pageRect, "Masthead", page.Masthead, display, 64f, 40f, FontStyles.Bold, TextAlignmentOptions.Center, 76f);
        TMP_Text edition = Line(pageRect, "Edition", page.Edition, body, 22f, 16f, FontStyles.Bold, TextAlignmentOptions.Center, 30f);
        edition.characterSpacing = 6f;
        Image rule = Box(pageRect, "Rule", Ink);
        Fix(rule.gameObject, 4f, 0f);
        Line(pageRect, "Headline", page.Headline, display, 46f, 28f, FontStyles.Bold, TextAlignmentOptions.Center, 110f);
        if (picture != null)
        {
            Image photo = Box(pageRect, "Picture", Color.white);
            photo.sprite = picture;
            photo.preserveAspect = true;
            Fix(photo.gameObject, 300f, 0f);
        }
        TMP_Text story = Line(pageRect, "Story", page.Body, body, 24f, 15f, FontStyles.Normal, TextAlignmentOptions.TopJustified, 200f);
        story.GetComponent<LayoutElement>().flexibleHeight = 1f;
        story.overflowMode = TextOverflowModes.Ellipsis;
        if (page.PetLine.Length > 0)
            Line(pageRect, "Pet", page.PetLine, body, 22f, 14f, FontStyles.Italic, TextAlignmentOptions.Center, 64f);

        var row = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(pageRect, false);
        var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 24f;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = true;
        Fix(row, 64f, 0f);
        MakeButton(row.transform, "TitleButton", UiText.Get("taskbar.start"), display, ToTitle);
        MakeButton(row.transform, "QuitButton", UiText.Get("startmenu.quit"), display, Quit);
    }

    /// <summary>MENU (the title): the paper is laid down (a fade) onto the title's own ending page.</summary>
    private void ToTitle()
    {
        if (_group != null)
            _group.interactable = false;
        Transit.Fade(_group, 0f, Transit.Seconds, () => Destroy(gameObject));
    }

    /// <summary>QUIT GAME: the game closes (play mode ends in the editor).</summary>
    private static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// The title's own faces: the face of its largest text (the display face)
    /// and the face most of its texts use (the body face), neither TMP's
    /// default when the title uses another; TMP's default when it has none.
    /// </summary>
    private static (TMP_FontAsset display, TMP_FontAsset body) Faces(Scene scene)
    {
        TMP_FontAsset fallback = TMP_Settings.defaultFontAsset;
        var uses = new Dictionary<TMP_FontAsset, int>();
        TMP_FontAsset largest = null;
        float largestSize = 0f;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.font == null || text.font == fallback)
                    continue;
                uses[text.font] = uses.TryGetValue(text.font, out int n) ? n + 1 : 1;
                if (text.fontSize > largestSize)
                {
                    largestSize = text.fontSize;
                    largest = text.font;
                }
            }
        TMP_FontAsset body = null;
        int most = 0;
        foreach (KeyValuePair<TMP_FontAsset, int> use in uses)
            if (use.Value > most)
            {
                most = use.Value;
                body = use.Key;
            }
        body = body != null ? body : fallback;
        return (largest != null ? largest : body, body);
    }

    /// <summary>A text line in the page's column: <paramref name="size"/> (down to <paramref name="min"/> to fit), its row <paramref name="height"/> high.</summary>
    private static TMP_Text Line(Transform parent, string name, string text, TMP_FontAsset font, float size, float min, FontStyles style, TextAlignmentOptions alignment, float height)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var label = go.GetComponent<TextMeshProUGUI>();
        if (font != null)
            label.font = font;
        label.text = text ?? string.Empty;
        label.color = Ink;
        label.fontStyle = style;
        label.alignment = alignment;
        label.enableAutoSizing = true;
        label.fontSizeMax = size;
        label.fontSizeMin = min;
        label.fontSize = size;
        label.raycastTarget = false;
        Fix(go, height, 0f);
        return label;
    }

    /// <summary>A plain coloured box (an Image) under <paramref name="parent"/>.</summary>
    private static Image Box(Transform parent, string name, Color colour)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = colour;
        image.raycastTarget = name == "Backdrop" || name == "Page";
        return image;
    }

    /// <summary>A button: an ink plate with its label in paper, <paramref name="onClick"/> on a click.</summary>
    private static void MakeButton(Transform parent, string name, string label, TMP_FontAsset font, UnityEngine.Events.UnityAction onClick)
    {
        Image plate = Box(parent, name, Ink);
        plate.raycastTarget = true;
        var button = plate.gameObject.AddComponent<Button>();
        button.targetGraphic = plate;
        button.onClick.AddListener(onClick);
        Fix(plate.gameObject, 64f, 260f);
        TMP_Text text = Line(plate.transform, "Label", label, font, 26f, 16f, FontStyles.Bold, TextAlignmentOptions.Center, 64f);
        text.color = Paper;
        Stretch(text.rectTransform);
    }

    /// <summary>Fixes <paramref name="go"/>'s row height (and width, when above 0) in its layout.</summary>
    private static void Fix(GameObject go, float height, float width)
    {
        if (!go.TryGetComponent(out LayoutElement element))
            element = go.AddComponent<LayoutElement>();
        element.preferredHeight = height;
        element.minHeight = Mathf.Min(height, 4f);
        if (width > 0f)
        {
            element.preferredWidth = width;
            element.minWidth = width;
        }
    }

    /// <summary>Stretches <paramref name="rect"/> over its parent.</summary>
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
