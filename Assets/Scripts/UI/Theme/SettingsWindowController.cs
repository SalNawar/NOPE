using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Settings window's per-player choices: the UI language (piece 6 U12),
/// "Follow history" or "Always English" (UiLanguagePreference; labels change
/// at the next scene load, colours, fonts and the wallpaper follow history
/// either way), and motion (piece 9 R17), "Full" or "Reduced"
/// (MotionPreference; reduced shows translations at once, from the next
/// traveller). In each pair the chosen button shows the theme's accent
/// colours (the SearchButton role), the other the default button colours.
/// The Keyboard section's "Show shortcuts" opens the shortcut card (redesign
/// phase 25; the desktop's keys today; phase 20's F1 card replaces it).
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
