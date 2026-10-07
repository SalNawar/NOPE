using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Settings window's per-player choices: the UI language (piece 6 U12),
/// "Follow history" or "Always English" (UiLanguagePreference; labels change
/// at the next scene load, colours, fonts and the wallpaper follow history
/// either way), a free choice until the Translation Lens's day (day 8): from
/// then on both buttons are disabled, "Follow history" shows as chosen and a
/// line under them says why (CultureThemeService.LanguageLocked; the stored
/// choice comes back with a new run), and motion (piece 9 R17), "Full" or "Reduced"
/// (MotionPreference; reduced shows translations at once, from the next
/// traveller, and cuts the game feel's motion), with the Motion intensity
/// slider under it (0-100 %, MotionPreference.Intensity: how far the game
/// feel's springs, shakes and the camera move; off while Reduced is chosen),
/// and Camera sway, "Off" (the default) or "On" (MotionPreference.CameraSway:
/// the cameras' idle breathing; off and inert while Reduced is chosen);
/// and the desktop's icons (the PC redesign DK5, DK6): open
/// with a "Double click" (the default) or a "Single click"
/// (DesktopPreferences), and "Reset icon positions" (DesktopIcons.Arrange)
/// (the step hints' pair is gone with the hints: the PC clean-up of
/// 2026-10-05). In each pair the chosen button shows the theme's accent colours (the
/// Badge role), the other the default button colours. The
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

    /// <summary>The line under the language pair, shown while the language is locked (settings.languageLocked).</summary>
    [SerializeField] private TMP_Text languageLockText;

    /// <summary>Chooses Full motion (translations flip letter by letter).</summary>
    [SerializeField] private Button fullMotionButton;

    /// <summary>Chooses Reduced motion (translations show at once).</summary>
    [SerializeField] private Button reducedMotionButton;

    /// <summary>Camera sway "Off" (the default) in the Motion group.</summary>
    [SerializeField] private Button cameraSwayOffButton;

    /// <summary>Camera sway "On": the cameras' idle breathing (MotionPreference.CameraSway; off while Reduced is chosen).</summary>
    [SerializeField] private Button cameraSwayOnButton;

    /// <summary>The Motion intensity slider (0 to 100).</summary>
    [SerializeField] private Slider motionIntensitySlider;

    /// <summary>The intensity's value beside the slider ("80 %").</summary>
    [SerializeField] private TMP_Text motionIntensityText;

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

    /// <summary>The UI kit (run 7): a chosen option is its oxblood plate, the others bone (sheet 02's segmented pairs); without it the theme's colours mark the choice.</summary>
    [SerializeField] private UiKitSO kit;

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
        if (cameraSwayOffButton != null)
            cameraSwayOffButton.onClick.AddListener(() => ChooseSway(false));
        if (cameraSwayOnButton != null)
            cameraSwayOnButton.onClick.AddListener(() => ChooseSway(true));
        if (motionIntensitySlider != null)
            motionIntensitySlider.onValueChanged.AddListener(ChooseIntensity);
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

    /// <summary>Stores the choice and shows it (never while the language is locked).</summary>
    private void Choose(bool alwaysEnglish)
    {
        if (CultureThemeService.LanguageLocked)
            return;
        UiLanguagePreference.AlwaysEnglish = alwaysEnglish;
        ShowSelection();
    }

    /// <summary>Stores the motion choice and shows it.</summary>
    private void ChooseMotion(bool reduced)
    {
        MotionPreference.Reduced = reduced;
        ShowSelection();
    }

    /// <summary>Stores the camera sway choice and shows it (never while Reduced is chosen: it forces the sway off).</summary>
    private void ChooseSway(bool on)
    {
        if (MotionPreference.Reduced)
            return;
        MotionPreference.CameraSway = on;
        ShowSelection();
    }

    /// <summary>Stores the Motion intensity (the slider's 0-100 as 0-1) and shows it.</summary>
    private void ChooseIntensity(float percent)
    {
        MotionPreference.Intensity = percent / 100f;
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
        bool locked = CultureThemeService.LanguageLocked;
        bool english = UiLanguagePreference.AlwaysEnglish && !locked;
        Paint(followHistoryButton, !english, theme);
        Paint(alwaysEnglishButton, english, theme);
        if (followHistoryButton != null)
            followHistoryButton.interactable = !locked;
        if (alwaysEnglishButton != null)
            alwaysEnglishButton.interactable = !locked;
        if (languageLockText != null)
        {
            languageLockText.gameObject.SetActive(locked);
            if (locked)
                languageLockText.text = UiText.Format("settings.languageLocked",
                    RunManager.HasInstance && RunManager.Instance.Library != null ? TranslationLens.LockDay(RunManager.Instance.Library.Introductions) : 0);
        }
        bool reduced = MotionPreference.Reduced;
        Paint(fullMotionButton, !reduced, theme);
        Paint(reducedMotionButton, reduced, theme);
        int percent = Mathf.RoundToInt(MotionPreference.Intensity * 100f);
        if (motionIntensitySlider != null)
        {
            motionIntensitySlider.SetValueWithoutNotify(percent);
            motionIntensitySlider.interactable = !reduced;
        }
        if (motionIntensityText != null)
            motionIntensityText.text = UiText.Format("settings.motionIntensityValue", percent);
        bool sway = MotionPreference.CameraSway && !reduced;
        Paint(cameraSwayOffButton, !sway, theme);
        Paint(cameraSwayOnButton, sway, theme);
        if (cameraSwayOffButton != null)
            cameraSwayOffButton.interactable = !reduced;
        if (cameraSwayOnButton != null)
            cameraSwayOnButton.interactable = !reduced;
        bool single = DesktopPreferences.OpenIconsWithSingleClick;
        Paint(iconDoubleClickButton, !single, theme);
        Paint(iconSingleClickButton, single, theme);
        int zoom = AppZoom.Parse(DesktopPreferences.DefaultZoom, config != null ? config.zoomLevels : null);
        for (int i = 0; i < textSizeButtons.Length; i++)
            Paint(textSizeButtons[i], Level(i) == zoom, theme);
    }

    /// <summary>The kit pieces of a chosen option and of the others.</summary>
    private const string ChosenPlate = "miniplate_ox", PlainPlate = "miniplate_bone";

    /// <summary>One option's look: the kit's chosen or plain plate and its ink, else its colours from the theme.</summary>
    private void Paint(Button button, bool selected, ThemeSO theme)
    {
        if (button == null)
            return;
        if (kit != null && button.targetGraphic is Image face)
        {
            string piece = selected ? ChosenPlate : PlainPlate;
            kit.Show(face, piece, button);
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
                text.color = kit.InkOn(piece);
            return;
        }
        if (theme == null)
            return;

        PaletteEntry entry = theme.Get(selected ? ThemeRoleId.Badge : ThemeRoleId.Button);
        if (entry == null)
            return;
        if (button.targetGraphic != null && entry.hasFill)
            button.targetGraphic.color = entry.fill;
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null && entry.hasInk)
            label.color = entry.ink;
    }
}
