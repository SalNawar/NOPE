using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Settings window's per-player choices. Language (piece 6 U12; D7,
/// BUG-3a): "Follow history" or "Always English" for the papers only (the PC
/// and the rest of the game always read the player's language;
/// UiLanguagePreference), never locked and applied at once (the papers
/// already on the desk keep their print, and the line under the pair says
/// so); with no choice the papers read English until the culture day and the
/// world's language from then on (CultureChoice.EnglishBySetting). Motion
/// (piece 9 R17): "Full" or "Reduced" (MotionPreference; reduced shows
/// translations at once, from the next traveller, and cuts the game feel's
/// motion), with the Motion intensity slider under it (0-100 %,
/// MotionPreference.Intensity; off while Reduced is chosen), and Camera sway,
/// "Off" (the default) or "On" (MotionPreference.CameraSway; off and inert
/// while Reduced is chosen). The PC basics (BUG-62): the Master, Music and
/// Effects volumes (SoundSettings), the window mode, the resolution, vsync,
/// brightness and Reduce flashing (ScreenSettings), each applied at once;
/// their rows are built by OfficeSceneUIBuilder.BuildSettingsBasics. The
/// desktop's icons (the PC redesign DK5, DK6): open with a "Double click"
/// (the default) or a "Single click" (DesktopPreferences), and "Reset icon
/// positions" (DesktopIcons.Arrange). In each pair the chosen button shows
/// the kit's chosen plate (else the theme's accent colours, the Badge role).
/// The Investigation section's Text size (100, 125, 150 %: the zoom levels)
/// is the app's default zoom (InvestigationApp.SetZoomDefault, saved in
/// DesktopPreferences; Ctrl+0 goes back to it). The Keyboard section's "Show
/// shortcuts" opens the shortcut card (the F1 card: the one shortcut table).
/// </summary>
public sealed class SettingsWindowController : MonoBehaviour
{
    /// <summary>Chooses "Follow history" (the papers take the world's language).</summary>
    [SerializeField] private Button followHistoryButton;

    /// <summary>Chooses "Always English" (the papers stay English).</summary>
    [SerializeField] private Button alwaysEnglishButton;

    /// <summary>The line under the language pair (settings.languageDesk): the choice is the papers' only, never locked, from the next paper printed.</summary>
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

    /// <summary>The volume sliders (0 to 100), one per SoundChannel in its order (BUG-62).</summary>
    [SerializeField] private Slider[] volumeSliders = new Slider[0];

    /// <summary>Each volume's value beside its slider ("80 %"), in the sliders' order.</summary>
    [SerializeField] private TMP_Text[] volumeTexts = new TMP_Text[0];

    /// <summary>The window mode choices, one per WindowMode in its order.</summary>
    [SerializeField] private Button[] windowModeButtons = new Button[0];

    /// <summary>Steps the resolution down, and up.</summary>
    [SerializeField] private Button resolutionDownButton, resolutionUpButton;

    /// <summary>The resolution chosen ("1920 × 1080").</summary>
    [SerializeField] private TMP_Text resolutionText;

    /// <summary>Vsync off, and on.</summary>
    [SerializeField] private Button vsyncOffButton, vsyncOnButton;

    /// <summary>The brightness slider (0 to 100; 50 is the game as painted).</summary>
    [SerializeField] private Slider brightnessSlider;

    /// <summary>The brightness's value beside its slider ("50 %").</summary>
    [SerializeField] private TMP_Text brightnessText;

    /// <summary>Reduce flashing off (the default), and on.</summary>
    [SerializeField] private Button reduceFlashingOffButton, reduceFlashingOnButton;

    /// <summary>The scene's Settings window controller (null when no Settings window is loaded).</summary>
    private static SettingsWindowController _instance;

    /// <summary>
    /// Brings the Settings window up (its DesktopWindow opened, in front), for
    /// FLOW's Esc (D20: "if you press escape you should auto open the pc
    /// setting menu"; the PC itself is FLOW's to open). False when no Settings
    /// window is loaded.
    /// </summary>
    public static bool Open()
    {
        if (_instance == null || !_instance.TryGetComponent(out DesktopWindow window))
            return false;
        window.Open();
        return true;
    }

    private void Awake()
    {
        _instance = this;
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
        WireBasics();
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private void OnEnable() => ShowSelection();

    /// <summary>Wires the PC basics' controls (BUG-62), each applying its choice at once.</summary>
    private void WireBasics()
    {
        for (int i = 0; i < volumeSliders.Length; i++)
        {
            var channel = (SoundChannel)i;
            if (volumeSliders[i] != null)
                volumeSliders[i].onValueChanged.AddListener(v => ChooseVolume(channel, v));
        }
        for (int i = 0; i < windowModeButtons.Length; i++)
        {
            var mode = (WindowMode)i;
            if (windowModeButtons[i] != null)
                windowModeButtons[i].onClick.AddListener(() => ChooseWindowMode(mode));
        }
        if (resolutionDownButton != null)
            resolutionDownButton.onClick.AddListener(() => StepResolution(-1));
        if (resolutionUpButton != null)
            resolutionUpButton.onClick.AddListener(() => StepResolution(1));
        if (vsyncOffButton != null)
            vsyncOffButton.onClick.AddListener(() => ChooseVsync(false));
        if (vsyncOnButton != null)
            vsyncOnButton.onClick.AddListener(() => ChooseVsync(true));
        if (brightnessSlider != null)
            brightnessSlider.onValueChanged.AddListener(ChooseBrightness);
        if (reduceFlashingOffButton != null)
            reduceFlashingOffButton.onClick.AddListener(() => ChooseReduceFlashing(false));
        if (reduceFlashingOnButton != null)
            reduceFlashingOnButton.onClick.AddListener(() => ChooseReduceFlashing(true));
    }

    /// <summary>Stores the papers' language choice and applies it at once (never locked, BUG-3a): the theme service re-reads the papers' language, and every text printed from a UI string follows (CultureThemeService.LabelsChanged).</summary>
    private void Choose(bool alwaysEnglish)
    {
        UiLanguagePreference.Choose(alwaysEnglish);
        CultureThemeService.RefreshActive();
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

    /// <summary>Stores and applies a channel's volume (the slider's 0-100 as 0-1) and shows it.</summary>
    private void ChooseVolume(SoundChannel channel, float percent)
    {
        SoundSettings.SetVolume(channel, percent / 100f);
        ShowSelection();
    }

    /// <summary>Stores and applies a window mode and shows it.</summary>
    private void ChooseWindowMode(WindowMode mode)
    {
        ScreenSettings.SetMode(mode);
        ShowSelection();
    }

    /// <summary>Steps the resolution through the offered sizes (held at the ends), applies it and shows it.</summary>
    private void StepResolution(int step)
    {
        List<ScreenSize> sizes = ScreenSettings.Resolutions();
        if (sizes.Count == 0)
            return;
        ScreenSettings.SetResolution(sizes[SettingsRules.Step(SettingsRules.IndexOf(sizes, ScreenSettings.Resolution), step, sizes.Count)]);
        ShowSelection();
    }

    /// <summary>Stores and applies vsync and shows it.</summary>
    private void ChooseVsync(bool on)
    {
        ScreenSettings.SetVsync(on);
        ShowSelection();
    }

    /// <summary>Stores and applies the brightness (the slider's 0-100 as 0-1) and shows it.</summary>
    private void ChooseBrightness(float percent)
    {
        ScreenSettings.SetBrightness(percent / 100f);
        ShowSelection();
    }

    /// <summary>Stores Reduce flashing and shows it (the flashing effects read it as they flash).</summary>
    private void ChooseReduceFlashing(bool on)
    {
        ScreenSettings.ReduceFlashing = on;
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
        bool english = CultureChoice.EnglishBySetting(UiLanguagePreference.Stored, CultureThemeService.LanguageLocked);
        Paint(followHistoryButton, !english, theme);
        Paint(alwaysEnglishButton, english, theme);
        if (languageLockText != null)
        {
            // The choice is the papers' only, never locked (D7, BUG-3a), and the line under the pair says so.
            languageLockText.gameObject.SetActive(true);
            languageLockText.text = UiText.Pc.Get("settings.languageDesk");
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
            motionIntensityText.text = UiText.Pc.Format("settings.motionIntensityValue", percent);
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
        ShowBasics(theme);
    }

    /// <summary>Shows the PC basics' choices (BUG-62).</summary>
    private void ShowBasics(ThemeSO theme)
    {
        for (int i = 0; i < volumeSliders.Length; i++)
        {
            int volume = SettingsRules.Percent(SoundSettings.Volume((SoundChannel)i));
            if (volumeSliders[i] != null)
                volumeSliders[i].SetValueWithoutNotify(volume);
            if (i < volumeTexts.Length && volumeTexts[i] != null)
                volumeTexts[i].text = UiText.Pc.Format("settings.volumeValue", volume);
        }
        WindowMode mode = ScreenSettings.Mode;
        for (int i = 0; i < windowModeButtons.Length; i++)
            Paint(windowModeButtons[i], i == (int)mode, theme);
        if (resolutionText != null)
        {
            ScreenSize size = ScreenSettings.Resolution;
            resolutionText.text = UiText.Pc.Format("settings.resolutionValue", size.Width, size.Height);
        }
        bool vsync = ScreenSettings.Vsync;
        Paint(vsyncOffButton, !vsync, theme);
        Paint(vsyncOnButton, vsync, theme);
        int brightness = SettingsRules.Percent(ScreenSettings.Brightness);
        if (brightnessSlider != null)
            brightnessSlider.SetValueWithoutNotify(brightness);
        if (brightnessText != null)
            brightnessText.text = UiText.Pc.Format("settings.brightnessValue", brightness);
        bool calm = ScreenSettings.ReduceFlashing;
        Paint(reduceFlashingOffButton, !calm, theme);
        Paint(reduceFlashingOnButton, calm, theme);
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
