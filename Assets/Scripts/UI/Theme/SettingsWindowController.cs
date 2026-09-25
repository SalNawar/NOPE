using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Settings window's UI language choice (piece 6 U12): "Follow history"
/// or "Always English" (UiLanguagePreference). The chosen button shows the
/// theme's accent colours (the SearchButton role), the other the default
/// button colours. Labels change at the next scene load; colours, fonts and
/// the wallpaper follow history either way.
/// </summary>
public sealed class SettingsWindowController : MonoBehaviour
{
    /// <summary>Chooses "Follow history".</summary>
    [SerializeField] private Button followHistoryButton;

    /// <summary>Chooses "Always English".</summary>
    [SerializeField] private Button alwaysEnglishButton;

    private void Awake()
    {
        if (followHistoryButton != null)
            followHistoryButton.onClick.AddListener(() => Choose(false));
        if (alwaysEnglishButton != null)
            alwaysEnglishButton.onClick.AddListener(() => Choose(true));
    }

    private void OnEnable() => ShowSelection();

    /// <summary>Stores the choice and shows it.</summary>
    private void Choose(bool alwaysEnglish)
    {
        UiLanguagePreference.AlwaysEnglish = alwaysEnglish;
        ShowSelection();
    }

    /// <summary>Colours the chosen button with the accent, the other as a default button.</summary>
    private void ShowSelection()
    {
        CultureThemeService service = CultureThemeService.Instance;
        ThemeSO theme = service != null ? service.ActiveTheme : null;
        bool english = UiLanguagePreference.AlwaysEnglish;
        Paint(followHistoryButton, !english, theme);
        Paint(alwaysEnglishButton, english, theme);
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
