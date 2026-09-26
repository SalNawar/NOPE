using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The PC reacts to history (piece 6): one persistent receiver of the UI cue
/// channel (created by CultureThemeBootstrap). The present culture is the
/// timeline leader's culture:{id} cue (piece 5); its theme (neutral before any
/// leader) is applied to every ThemeTag in the loaded scene when the scene
/// loads, never during a shift: colours by role, the culture's font, its
/// labels (or English, by setting or when no installed font draws them), the
/// wallpaper, the compare colours; diegetic roles are skipped. It also holds
/// the string lookup UiText reads and the Future currency the wallet shows.
/// Every decision is a tested rule (CultureCue.Pick, CultureChoice, UiStrings,
/// ThemeRoles); this class only reads engine objects and sets them.
/// </summary>
public sealed class CultureThemeService : TimelineCueReceiver
{
    /// <summary>The one service (null before the bootstrap ran, or without generated themes).</summary>
    public static CultureThemeService Instance { get; private set; }

    /// <summary>The configured content library.</summary>
    public ContentLibrarySO Library { get; private set; }

    /// <summary>The theme in use (the neutral one when no culture leads); never null once configured.</summary>
    public ThemeSO ActiveTheme { get; private set; }

    /// <summary>The leading culture's id, or null (neutral).</summary>
    public string ActiveCultureId { get; private set; }

    /// <summary>The string lookup (reading language only unless the culture's labels apply).</summary>
    public UiStrings Strings { get; private set; }

    /// <summary>Why the labels are in their language (CultureChoice.Language).</summary>
    public LabelLanguage Language { get; private set; }

    /// <summary>The leading culture's Future currency: the present's (ContentLibrarySO.BuildPresent, the leader's Future place), or null.</summary>
    public string FutureCurrency { get; private set; }

    /// <summary>The resolved font, for the inspector ("default" for the project font).</summary>
    public string FontName { get; private set; } = "default";

    /// <summary>The session's runtime fonts, also used by translation for its script fonts (piece 9); null before Configure.</summary>
    public RuntimeFonts Fonts => _fonts;

    /// <summary>The themed texts' font; null = the project's default TMP font.</summary>
    private TMP_FontAsset _font;

    /// <summary>The runtime font cache.</summary>
    private RuntimeFonts _fonts;

    /// <summary>The scene sceneLoaded asked to theme (null = every loaded scene).</summary>
    private Scene? _target;

    /// <summary>Warnings already logged (each once per session).</summary>
    private readonly HashSet<string> _warned = new HashSet<string>();

    /// <summary>The characters every culture font must draw besides the labels (western digits and number punctuation, the no-break space).</summary>
    private const string NumberSample = "0123456789:%+-.()  ";

    /// <summary>Sets the library before the host first enables (CultureThemeBootstrap).</summary>
    public void Configure(ContentLibrarySO library)
    {
        Library = library;
        _fonts?.Dispose();
        _fonts = new RuntimeFonts(library.CultureUi.latinFallbackFont);
        ActiveTheme = library.NeutralTheme;
        Strings = new UiStrings(ReadingTable()?.entries, null, false, library.CultureUi.glossPercent);
    }

    /// <summary>Re-applies the present culture to every loaded scene (the bootstrap, and GameManager.Start once the run exists); null-safe.</summary>
    public static void RefreshActive()
    {
        if (Instance != null)
            Instance.Refresh();
    }

    /// <summary>Applies the current theme under one root (UI built at runtime, such as the investigation text fallback).</summary>
    public void ApplyTo(GameObject root)
    {
        if (root != null && ActiveTheme != null)
            Apply(root);
    }

    /// <summary>The culture cue lives on the UI channel.</summary>
    protected override EffectChannel ListenChannel => EffectChannel.UI;

    /// <summary>No refresh of its own at Start: the bootstrap, sceneLoaded and GameManager.Start refresh.</summary>
    protected override void Start()
    {
    }

    /// <summary>Picks the culture, resolves its font, labels and currency, and applies the theme.</summary>
    protected override void OnCuesChanged(IReadOnlyList<string> cues)
    {
        if (Library == null || Library.NeutralTheme == null)
            return;

        string id = CultureCue.Pick(cues, out int matches);
        if (matches > 1)
            WarnOnce("two", "[CultureThemeService] Two culture cues are active; the first wins. Check the effects that emit culture: cues.");

        ThemeSO theme = id != null ? Library.GetThemeByCultureId(id) : null;
        if (id != null && theme == null)
        {
            WarnOnce("theme:" + id, $"[CultureThemeService] No theme for culture '{id}'; the desk stays neutral. Run Tools > TimeDesk > Generate World.");
            id = null;
        }
        ActiveTheme = theme ?? Library.NeutralTheme;
        ActiveCultureId = id;

        CultureUiSettings ui = Library.CultureUi;
        UiStringTableSO reading = ReadingTable();
        UiStringTableSO cultureTable = ActiveTheme.language != ui.readingLanguage ? Library.GetStringTable(ActiveTheme.language) : null;

        RuntimeFonts.Result font = _fonts.Resolve(ActiveTheme, Sample(reading, cultureTable, ui.glossPercent));
        Language = CultureChoice.Language(ActiveTheme.language, ui.readingLanguage, cultureTable != null, UiLanguagePreference.AlwaysEnglish, font.Covers);
        if (Language == LabelLanguage.EnglishNoFont)
            WarnOnce("font:" + ActiveTheme.cultureId, $"[CultureThemeService] No installed font draws the '{ActiveTheme.cultureId}' labels (missing {font.Missing}; tried {font.Tried}); the desk shows English labels in its colours.");
        _font = Language == LabelLanguage.EnglishNoFont ? null : font.Asset;
        FontName = _font != null ? font.Name : "default";

        bool cultureLabels = Language == LabelLanguage.Culture;
        Strings = new UiStrings(reading?.entries, cultureLabels ? cultureTable.entries : null, cultureLabels && cultureTable.rightToLeft, ui.glossPercent);

        FutureCurrency = ResolveFutureCurrency(id);

        if (_target.HasValue)
        {
            ApplyScene(_target.Value);
            return;
        }
        for (int i = 0; i < SceneManager.sceneCount; i++)
            ApplyScene(SceneManager.GetSceneAt(i));
    }

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        _fonts?.Dispose();
        if (Instance == this)
            Instance = null;
    }

    /// <summary>A new scene: theme it before its first Start (the briefing never flashes the neutral look).</summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _target = scene;
        try
        {
            Refresh();
        }
        finally
        {
            _target = null;
        }
    }

    /// <summary>The reading language's table (null with a one-time warning).</summary>
    private UiStringTableSO ReadingTable()
    {
        UiStringTableSO table = Library.GetStringTable(Library.CultureUi.readingLanguage);
        if (table == null)
            WarnOnce("reading", $"[CultureThemeService] The content library has no '{Library.CultureUi.readingLanguage}' UI string table. Run Tools > TimeDesk > Generate World.");
        return table;
    }

    /// <summary>What the culture font must draw: the culture's labels (shaped when right to left), the English glosses and the number characters.</summary>
    private static string Sample(UiStringTableSO reading, UiStringTableSO culture, int glossPercent)
    {
        var sb = new StringBuilder(NumberSample);
        if (culture == null)
            return sb.ToString();

        var glossed = new UiStrings(reading?.entries, culture.entries, culture.rightToLeft, glossPercent);
        foreach (UiStringEntry e in culture.entries)
            if (e != null && !string.IsNullOrEmpty(e.key))
                sb.Append(glossed.Get(e.key));
        return sb.ToString();
    }

    /// <summary>
    /// The leading culture's Future currency: the present's Currency fact
    /// (traveller types H1: the leader's Future place, history applied), read
    /// without building the world's facts (audit R4-018); null when neutral,
    /// before a run exists, or when the present is not the culture's.
    /// </summary>
    private string ResolveFutureCurrency(string cultureId)
    {
        if (cultureId == null || !RunManager.HasInstance)
            return null;

        PresentPlace present = Library.BuildPresent(RunManager.Instance.World.history);
        string currency = present != null && present.NationId == cultureId ? present.Fact(ClueCategory.Currency) : null;
        if (string.IsNullOrWhiteSpace(currency))
            WarnOnce("currency:" + cultureId, $"[CultureThemeService] Culture '{cultureId}' has no Future Currency fact; the wallet keeps its neutral word.");
        return currency;
    }

    /// <summary>Applies the theme to every root of a loaded scene.</summary>
    private void ApplyScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;
        foreach (GameObject root in scene.GetRootGameObjects())
            Apply(root);
    }

    /// <summary>Applies the theme to every tag and compare controller under a root (inactive ones included).</summary>
    private void Apply(GameObject root)
    {
        foreach (ThemeTag tag in root.GetComponentsInChildren<ThemeTag>(true))
            ApplyTag(tag);

        foreach (CompareController compare in root.GetComponentsInChildren<CompareController>(true))
            compare.ApplyTheme(Ink(ThemeRoleId.CompareMatch), Ink(ThemeRoleId.CompareMismatch), Ink(ThemeRoleId.CompareNeutral), Fill(ThemeRoleId.SelectionHighlight));
    }

    /// <summary>
    /// One tag: an image's colour (the wallpaper for the Desktop role), or a
    /// text's ink, font, style, fit and label. A diegetic tag keeps its look
    /// and only takes the shrink-to-fit layout (a book row's "[revised]").
    /// </summary>
    private void ApplyTag(ThemeTag tag)
    {
        if (ThemeRoles.IsDiegetic(tag.Role))
        {
            if (tag.ShrinkToFit && tag.TryGetComponent(out TMP_Text evidence))
                Fit(tag, evidence);
            return;
        }

        PaletteEntry entry = Entry(tag.Role);
        if (entry == null)
            return;

        if (tag.TryGetComponent(out TMP_Text text))
        {
            ApplyText(tag, text, entry);
            return;
        }

        if (!tag.TryGetComponent(out Image image))
            return;

        if (tag.Role == ThemeRoleId.Desktop)
        {
            Sprite wallpaper = ActiveTheme.wallpaper;
            image.sprite = wallpaper;
            image.color = wallpaper != null ? Color.white : entry.fill;
            if (wallpaper != null && tag.TryGetComponent(out AspectRatioFitter fitter))
                fitter.aspectRatio = wallpaper.rect.width / wallpaper.rect.height;
            return;
        }

        bool ink = tag.Part == ThemePart.Ink;
        if (ink ? entry.hasInk : entry.hasFill)
            image.color = ink ? entry.ink : entry.fill;
    }

    /// <summary>A themed text: ink, font, style (italics stripped or additions), shrink-to-fit, and its label when keyed.</summary>
    private void ApplyText(ThemeTag tag, TMP_Text text, PaletteEntry entry)
    {
        if (entry.hasInk)
            text.color = entry.ink;

        TMP_FontAsset font = _font != null ? _font : TMP_Settings.defaultFontAsset;
        if (font != null)
            text.font = font;

        int add = tag.TextKind == ThemeTextKind.Heading ? (int)ActiveTheme.headingAdd : tag.TextKind == ThemeTextKind.Button ? (int)ActiveTheme.buttonAdd : 0;
        text.fontStyle = (FontStyles)CultureChoice.ComposeStyle((int)tag.BaseStyle, (int)FontStyles.Italic, ActiveTheme.stripItalic, add);

        if (tag.ShrinkToFit)
            Fit(tag, text);

        if (!string.IsNullOrEmpty(tag.LabelKey))
            text.text = UiText.Get(tag.LabelKey);
    }

    /// <summary>No wrapping; auto-size from labelMinScale of the text's first size to that size (R21).</summary>
    private void Fit(ThemeTag tag, TMP_Text text)
    {
        float size = tag.FitSize(text);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.enableAutoSizing = true;
        text.fontSizeMax = size;
        text.fontSizeMin = size * Library.CultureUi.labelMinScale;
    }

    /// <summary>A role's entry in the active theme, else the neutral one's (warned once when neither has it).</summary>
    private PaletteEntry Entry(ThemeRoleId role)
    {
        PaletteEntry entry = ActiveTheme.Get(role) ?? Library.NeutralTheme.Get(role);
        if (entry == null)
            WarnOnce("role:" + role, $"[CultureThemeService] Theme '{ActiveTheme.cultureId}' has no colour for role '{role}'. Run Tools > TimeDesk > Generate World.");
        return entry;
    }

    /// <summary>A role's ink (white when missing).</summary>
    private Color Ink(ThemeRoleId role)
    {
        PaletteEntry e = Entry(role);
        return e != null && e.hasInk ? e.ink : Color.white;
    }

    /// <summary>A role's fill (white when missing).</summary>
    private Color Fill(ThemeRoleId role)
    {
        PaletteEntry e = Entry(role);
        return e != null && e.hasFill ? e.fill : Color.white;
    }

    /// <summary>Logs a warning once per session.</summary>
    private void WarnOnce(string key, string message)
    {
        if (_warned.Add(key))
            Debug.LogWarning(message);
    }
}
