using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// The game's pause and its Esc key (FLOW, BUG-34; Saleh's D20: "you should
/// be able to close the booth with the available sign if you want to pause
/// ... if you press escape you should auto open the pc setting menu"; R3/R4:
/// Esc opens the same Settings everywhere). There is no pause card: pausing
/// is the AVAILABLE sign (BELL-SIGN, a Clock source of PauseState), and this
/// host, made once at startup and kept across scenes, does the rest.
/// <list type="bullet">
/// <item><b>Esc:</b> lets go of one thing first (EscapeChain: the desk's hold,
/// the PC's own chain, a drag the office's back-out cancels, a track's
/// step), and otherwise opens Settings: in the
/// office the PC with its Settings app in front (the PC opens by Q's rules, so
/// not under the briefing or the report); in a scene without the PC, its
/// Settings window, once that scene has one (HOME builds Title's and Home's).
/// Esc never leaves a mode (R3: Q or the bezel leave the PC; a right-click
/// backs out): EscapeWatch keeps the office's back-out off an Esc press that
/// would (EscapeChain.KeepsTheOfficeOut).
/// Nothing while a scene loads or the cheat overlay holds the input.</item>
/// <item><b>Settings open</b> holds the game (PauseState Settings, Full): the
/// shift clock stops and the looping beds (room tone, music) pause; one-shot
/// cues finish, so Settings' own clicks sound. It counts as open while it is
/// the PC's front window (or the only one) with the PC open, or while a
/// non-PC Settings window is shown.</item>
/// <item><b>The window's focus lost</b> (only when the game does not run in the
/// background: the player; the QA rig and the editor's job runner turn
/// runInBackground on) holds the game too (PauseState Focus, Full), with all
/// audio paused; the desk's hold is cancelled either way (HoldRegistry).</item>
/// </list>
/// </summary>
[DefaultExecutionOrder(1000)]
public sealed class PauseMenu : MonoBehaviour
{
    /// <summary>The one host (made at startup; null outside play mode).</summary>
    private static PauseMenu _host;

    /// <summary>True once the loaded scenes changed: the scene references are found again when next needed.</summary>
    private bool _stale = true;

    /// <summary>The loaded Settings window (the PC's app in the office), or null.</summary>
    private SettingsWindowController _settings;

    /// <summary>The PC window that holds it (null for a Settings window outside the PC).</summary>
    private DesktopWindow _settingsWindow;

    /// <summary>The office's view (the PC open or not), or null outside the office.</summary>
    private OfficeViewController _view;

    /// <summary>The office's controls (Q's rules open the PC), or null outside the office.</summary>
    private OfficeControls _controls;

    /// <summary>The PC's keyboard poller (its own Escape chain), or null outside the office.</summary>
    private DesktopKeyboard _keyboard;

    /// <summary>The looping sources paused while a Full source holds (unpaused when none does), or null.</summary>
    private List<AudioSource> _pausedBeds;

    /// <summary>The office's controls state as an Esc press began, before the office's pollers ran (EscapeWatch), or null.</summary>
    private ControlState? _officeBefore;

    /// <summary>True while EscapeWatch keeps the office's controls out of this frame (an Esc that would leave a mode): Update turns them back on.</summary>
    private bool _controlsKeptOut;

    /// <summary>True while Settings shows (PauseState's Settings source holds); false outside play mode.</summary>
    public static bool SettingsShowing => _host != null && PauseState.Shared.Holds(PauseState.Settings);

    /// <summary>
    /// Opens Settings: in the office the PC (by Q's rules, and only when
    /// PcFrame.CanOpen) with its Settings app in front; elsewhere the scene's
    /// Settings window. False when there is none here, or the PC may not open now.
    /// </summary>
    public static bool OpenSettings()
    {
        if (_host == null)
            return false;
        _host.FindSceneParts();
        if (_host._settings == null)
            return false;
        if (_host._settingsWindow == null)
        {
            _host._settings.gameObject.SetActive(true);
            return true;
        }
        if (_host._view != null && _host._view.Current != OfficeView.MonitorFocus)
        {
            if (!PcFrame.CanOpen)
                return false;
            if (_host._controls != null)
                _host._controls.Press(ControlKey.Pc);
            else
                _host._view.FocusMonitor();
            if (_host._view.Current != OfficeView.MonitorFocus)
                return false; // the PC may not open now (the briefing, the report)
        }
        _host._settingsWindow.Open();
        return true;
    }

    /// <summary>The game window gained (<paramref name="focused"/>) or lost the focus: a loss cancels the desk's hold and, when the game does not run in the background, holds the game (PauseState Focus) until the focus comes back.</summary>
    public static void FocusChanged(bool focused)
    {
        if (!focused)
            HoldRegistry.Desk.OnFocusLost();
        if (!focused && PauseState.PausesOnFocusLoss(Application.runInBackground))
            PauseState.Shared.Request(PauseState.Focus, PauseScope.Full);
        else
            PauseState.Shared.Release(PauseState.Focus);
    }

    /// <summary>Forgets the pause, the Esc steps and the host when play starts (the editor without a domain reload keeps statics).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _host = null;
        PauseState.Shared.Clear();
        EscapeChain.Shared.Clear();
        AudioListener.pause = false;
    }

    /// <summary>Makes the host once the first scene has loaded (every scene: the title, the office, Home).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (_host != null)
            return;
        var host = new GameObject("PauseMenu") { hideFlags = HideFlags.HideInHierarchy };
        DontDestroyOnLoad(host);
        _host = host.AddComponent<PauseMenu>();
    }

    private void Awake()
    {
        // The chain's own steps: the desk's hold is cancelled first; the PC's own chain (a menu, a field) let go of something this frame.
        EscapeChain.Shared.Register("hold", EscapeChain.HoldOrder, CancelHold);
        EscapeChain.Shared.Register("desktop", EscapeChain.DesktopOrder, () => _keyboard != null && _keyboard.EscapeTakenFrame == Time.frameCount);
        EscapeChain.Shared.Register("office", EscapeChain.OfficeOrder, OfficeLetGo);
        gameObject.AddComponent<EscapeWatch>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += MarkStale;
        SceneManager.sceneUnloaded += MarkStale;
        PauseState.Shared.Changed += FollowAudio;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= MarkStale;
        SceneManager.sceneUnloaded -= MarkStale;
        PauseState.Shared.Changed -= FollowAudio;
    }

    private void OnDestroy()
    {
        EscapeChain.Shared.Unregister("hold");
        EscapeChain.Shared.Unregister("desktop");
        EscapeChain.Shared.Unregister("office");
        if (_host == this)
            _host = null;
    }

    /// <summary>The window's focus (the player's alt-tab).</summary>
    private void OnApplicationFocus(bool focus) => FocusChanged(focus);

    /// <summary>Esc (after every other poller has had the frame), then whether Settings shows.</summary>
    private void Update()
    {
        FindSceneParts();
        Keyboard kb = Keyboard.current;
        try
        {
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
                Escape();
        }
        finally
        {
            _officeBefore = null;
            if (_controlsKeptOut)
            {
                _controlsKeptOut = false;
                if (_controls != null)
                    _controls.enabled = true;
            }
        }
        FollowSettings();
    }

    /// <summary>
    /// An Esc press begins (EscapeWatch: after the PC's poller, before the
    /// office's): the office's controls state now; and when the office's
    /// back-out would leave a mode on this press (EscapeChain.KeepsTheOfficeOut;
    /// not with <paramref name="rightClick"/>, nor while a load or the cheat
    /// overlay holds Esc), the office's controls sit this frame out, so the
    /// mode stays and Settings opens (Update turns them back on).
    /// </summary>
    internal void WatchEscape(bool rightClick)
    {
        FindSceneParts();
        if (_controls == null || !_controls.isActiveAndEnabled)
        {
            _officeBefore = null;
            return;
        }
        ControlState state = _controls.State();
        _officeBefore = state;
        bool desktopTookIt = _keyboard != null && _keyboard.EscapeTakenFrame == Time.frameCount;
        if (!SceneTransit.Loading && !InputGate.Blocked && EscapeChain.KeepsTheOfficeOut(state, desktopTookIt, rightClick))
        {
            _controls.enabled = false;
            _controlsKeptOut = true;
        }
    }

    /// <summary>The chain's office step: the office's own back-out (OfficeControls, run on a right-click or Esc) let go of something on this press (a drag cancelled; EscapeWatch keeps every other back-out off Esc), so Settings waits for the next press.</summary>
    private bool OfficeLetGo() => _officeBefore.HasValue && _controls != null && !_controlsKeptOut && !_officeBefore.Value.Equals(_controls.State());

    /// <summary>One Esc press: nothing while a scene loads or the cheat overlay holds the input; else one thing let go (EscapeChain), or Settings opens.</summary>
    private static void Escape()
    {
        if (SceneTransit.Loading || InputGate.Blocked)
            return;
        if (EscapeChain.Shared.LetGo() != null)
            return;
        OpenSettings();
    }

    /// <summary>The chain's hold step: cancels what the desk holds (true), or holds nothing (false).</summary>
    private static bool CancelHold()
    {
        if (!HoldRegistry.Desk.IsHolding)
            return false;
        HoldRegistry.Desk.Cancel();
        return true;
    }

    /// <summary>Holds the game (PauseState Settings, Full) while Settings shows, and lets it go when it does not.</summary>
    private void FollowSettings()
    {
        bool showing = SettingsShownNow();
        if (showing == PauseState.Shared.Holds(PauseState.Settings))
            return;
        if (showing)
            PauseState.Shared.Request(PauseState.Settings, PauseScope.Full);
        else
            PauseState.Shared.Release(PauseState.Settings);
    }

    /// <summary>True when the player sees Settings now: the PC open with Settings its front window (or the only one, nothing focused), or a Settings window outside the PC shown.</summary>
    private bool SettingsShownNow()
    {
        if (_settings == null)
            return false;
        if (_settingsWindow == null)
            return _settings.isActiveAndEnabled;
        if (_view != null && _view.Current != OfficeView.MonitorFocus)
            return false;
        if (!_settingsWindow.IsOpen || _settingsWindow.IsMinimised)
            return false;
        DesktopWindow front = _settingsWindow.Manager != null ? _settingsWindow.Manager.FocusedWindow : null;
        return front == null || front == _settingsWindow;
    }

    /// <summary>
    /// Follows the pause's audio: everything stops while the window's focus is
    /// lost (AudioListener.pause), and the looping beds pause while any Full
    /// source holds (Settings), one-shot cues finishing; they resume as it ends.
    /// </summary>
    private void FollowAudio()
    {
        AudioListener.pause = PauseState.Shared.Holds(PauseState.Focus);
        bool full = PauseState.Shared.FullHeld;
        if (full && _pausedBeds == null)
        {
            _pausedBeds = new List<AudioSource>();
            foreach (AudioSource source in FindObjectsByType<AudioSource>())
                if (source.loop && source.isPlaying)
                {
                    source.Pause();
                    _pausedBeds.Add(source);
                }
        }
        else if (!full && _pausedBeds != null)
        {
            foreach (AudioSource source in _pausedBeds)
                if (source != null)
                    source.UnPause();
            _pausedBeds = null;
        }
    }

    private void MarkStale(Scene scene, LoadSceneMode mode) => _stale = true;

    private void MarkStale(Scene scene) => _stale = true;

    /// <summary>Finds the loaded scenes' Settings window, office view, controls and PC keyboard once after the scenes changed (never the art office's switched-off leftover copies).</summary>
    private void FindSceneParts()
    {
        if (!_stale)
            return;
        _stale = false;
        _settings = Find<SettingsWindowController>();
        _settingsWindow = _settings != null ? _settings.GetComponent<DesktopWindow>() : null;
        _view = Find<OfficeViewController>();
        _controls = Find<OfficeControls>();
        _keyboard = Find<DesktopKeyboard>();
    }

    /// <summary>The first loaded <typeparamref name="T"/>, inactive ones included (a closed window is inactive), outside the art office's leftovers; null when none.</summary>
    private static T Find<T>() where T : Component
    {
        foreach (T found in FindObjectsByType<T>(FindObjectsInactive.Include))
            if (found != null && !OfficeScenes.IsArtOffice(found.gameObject.scene))
                return found;
        return null;
    }
}
