using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Settings window's per-player choices: the UI language (piece 6 U12),
/// "Follow history" or "Always English" (UiLanguagePreference; labels change
/// at the next scene load, colours, fonts and the wallpaper follow history
/// either way), and motion (piece 9 R17), "Full" or "Reduced"
/// (MotionPreference; reduced shows translations at once, from the next
/// traveller); and the desktop's icons (the PC redesign DK5, DK6): open
/// with a "Double click" (the default) or a "Single click"
/// (DesktopPreferences), and "Reset icon positions" (DesktopIcons.Arrange).
/// In each pair the chosen button shows the theme's accent colours (the
/// SearchButton role), the other the default button colours. The
/// Investigation section's Text size (100, 125, 150 %: the zoom levels) is
/// the app's default zoom (InvestigationApp.SetZoomDefault, saved in
/// DesktopPreferences; Ctrl+0 goes back to it). The Keyboard section's "Show
/// shortcuts" opens the shortcut card (the F1 card: the one shortcut table).
/// </summary>
public sealed class SettingsWindowController : MonoBehaviour
{
    /// <summary>Chooses "Follow history".</summary>
    [SerializeField] private Button followHistoryButton;

    /// <summary>Chooses "Always English".</summary>
    [SerializeField] private Button alwaysEnglishButton;

    /// <summary>Chooses Full motion (translations flip letter by letter).</summary>
    [SerializeField] private Button fullMotionButton;

    /// <summary>Chooses Reduced motion (translations show at once).</summary>
    [SerializeField] private Button reducedMotionButton;

    /// <summary>Desktop icons open with a double click.</summary>
    [SerializeField] private Button iconDoubleClickButton;

    /// <summary>Desktop icons open with a single click.</summary>
    [SerializeField] private Button iconSingleClickButton;

    /// <summary>Lays the desktop's icons out in the default arrangement again.</summary>
    [SerializeField] private Button resetIconsButton;

    /// <summary>The desktop's icons (Reset icon positions).</summary>
    [SerializeField] private DesktopIcons icons;

    /// <summary>The Investigation section's Text size buttons, one per zoom level (DesktopConfigSO.zoomLevels, in order).</summary>
    [SerializeField] private Button[] textSizeButtons = new Button[0];

    /// <summary>The zoom levels.</summary>
    [SerializeField] private DesktopConfigSO config;

    /// <summary>The Investigation app (its default zoom).</summary>
    [SerializeField] private InvestigationApp app;

    /// <summary>The Keyboard section's "Show shortcuts".</summary>
    [SerializeField] private Button showShortcutsButton;

    /// <summary>The shortcut card it opens.</summary>
    [SerializeField] private DesktopWindow shortcutsWindow;

    private void Awake()
    {
        if (followHistoryButton != null)
            followHistoryButton.onClick.AddListener(() => Choose(false));
        if (alwaysEnglishButton != null)
            alwaysEnglishButton.onClick.AddListener(() => Choose(true));
        if (fullMotionButton != null)
            fullMotionButton.onClick.AddListener(() => ChooseMotion(false));
        if (reducedMotionButton != null)
            reducedMotionButton.onClick.AddListener(() => ChooseMotion(true));
        if (iconDoubleClickButton != null)
            iconDoubleClickButton.onClick.AddListener(() => ChooseIconOpen(false));
        if (iconSingleClickButton != null)
            iconSingleClickButton.onClick.AddListener(() => ChooseIconOpen(true));
        if (resetIconsButton != null)
            resetIconsButton.onClick.AddListener(ResetIcons);
        for (int i = 0; i < textSizeButtons.Length; i++)
        {
            int level = Level(i);
            if (textSizeButtons[i] != null)
                textSizeButtons[i].onClick.AddListener(() => ChooseTextSize(level));
        }
        if (showShortcutsButton != null && shortcutsWindow != null)
            showShortcutsButton.onClick.AddListener(shortcutsWindow.Open);
    }

    private void OnEnable() => ShowSelection();

    /// <summary>Stores the choice and shows it.</summary>
    private void Choose(bool alwaysEnglish)
    {
        UiLanguagePreference.AlwaysEnglish = alwaysEnglish;
        ShowSelection();
    }

    /// <summary>Stores the motion choice and shows it.</summary>
    private void ChooseMotion(bool reduced)
    {
        MotionPreference.Reduced = reduced;
        ShowSelection();
    }

    /// <summary>Stores how desktop icons open and shows it.</summary>
    private void ChooseIconOpen(bool singleClick)
    {
        DesktopPreferences.OpenIconsWithSingleClick = singleClick;
        ShowSelection();
    }

    /// <summary>Stores the app's default zoom, shows it now, and shows the choice.</summary>
    private void ChooseTextSize(int level)
    {
        if (app != null)
            app.SetZoomDefault(level);
        ShowSelection();
    }

    /// <summary>The zoom level of Text size button <paramref name="index"/> (100 % without the knobs).</summary>
    private int Level(int index) =>
        config != null && config.zoomLevels != null && index < config.zoomLevels.Length ? config.zoomLevels[index] : AppZoom.Normal;

    /// <summary>Lays the desktop's icons out in the default arrangement (and saves it).</summary>
    private void ResetIcons()
    {
        if (icons != null)
            icons.Arrange();
    }

    /// <summary>Colours each pair's chosen button with the accent, the other as a default button.</summary>
    private void ShowSelection()
    {
        CultureThemeService service = CultureThemeService.Instance;
        ThemeSO theme = service != null ? service.ActiveTheme : null;
        bool english = UiLanguagePreference.AlwaysEnglish;
        Paint(followHistoryButton, !english, theme);
        Paint(alwaysEnglishButton, english, theme);
        bool reduced = MotionPreference.Reduced;
        Paint(fullMotionButton, !reduced, theme);
        Paint(reducedMotionButton, reduced, theme);
        bool single = DesktopPreferences.OpenIconsWithSingleClick;
        Paint(iconDoubleClickButton, !single, theme);
        Paint(iconSingleClickButton, single, theme);
        int zoom = AppZoom.Parse(DesktopPreferences.DefaultZoom, config != null ? config.zoomLevels : null);
        for (int i = 0; i < textSizeButtons.Length; i++)
            Paint(textSizeButtons[i], Level(i) == zoom, theme);
    }

    /// <summary>One button's colours from the theme.</summary>
    private static void Paint(Button button, bool selected, ThemeSO theme)
    {
        if (button == null || theme == null)
            return;

        PaletteEntry entry = theme.Get(selected ? ThemeRoleId.SearchButton : ThemeRoleId.Button);
        if (entry == null)
            return;
        if (button.targetGraphic != null && entry.hasFill)
            button.targetGraphic.color = entry.fill;
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null && entry.hasInk)
            label.color = entry.ink;
    }
}
