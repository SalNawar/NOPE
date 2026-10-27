using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The PC frame (ReStory-style): clicking the PC opens a flat, front-facing
/// monitor over the left of the office, with the room still visible on the
/// right. The frame (bezel, close X, power button, the room catcher) is
/// overlay UI; the desktop itself shows inside its glass through the frame
/// camera, which renders only the desktop's layer into the glass's screen
/// rectangle and is the desktop canvas's event camera, so clicks, dragging
/// and text fields work as on any canvas. The frame camera draws right
/// after the art office's camera (DrawAfter, set by the office binder at
/// load), whatever depth the art gave its camera: a base camera drawn before
/// the office's is covered by the office's clear (the anime hall's camera
/// draws at depth 100, the room's at -1). OfficeViewController opens and
/// closes it; Q and a click on the bezel leave the PC, nothing else does
/// (BUG-50a; Saleh's R3/R4, 2026-10-20: "To leave the PC, press Q or click
/// the bezel"); a click on the room beside the frame is taken and does
/// nothing.
/// The picture powers on as a CRT does (BUG-32c, GAME_FEEL_BIBLE F1;
/// CrtPower, with DesktopConfigSO's knobs): from the press frame the frame
/// and its dark glass fade in (never whole in one frame: it is 63-77 % of
/// the screen, F1's "no frame-0 UI pop"), the degauss hum (pc_on) begins 80
/// ms before the picture, then the picture grows from a bright full-width
/// line to full height, with a one-frame glow on the glass (through
/// FlashPolicy, G3); closing, it squeezes to a line and a dot, the glass is
/// dark as the picture goes, the dot glows on for two frames (the phosphor
/// afterglow), then the frame fades out and only then hides. The line is
/// drawn by squeezing the frame camera's projection inside the glass's full
/// viewport (the camera's black clear is the dark tube around it), never by
/// shrinking the viewport, which would only draw the whole desktop smaller.
/// With Reduced Motion nothing moves: the frame and its dark glass fade in
/// over the room, then the dark glass fades off the picture (and the reverse
/// on closing), so the picture, drawn by its own camera, never cuts in or
/// out. A move reversed while under way (Q or the bezel during the power-on,
/// a click on the PC during the collapse, the afterglow or the fade out)
/// picks up where the glass is (CrtPower.Resume), so it never pops. The
/// bezel, its X and its power button, and the keys (DesktopKeyboard reads
/// MonitorScreen.IsInteractive, not the raycaster), take input from the
/// press frame; the desktop takes clicks once the picture is whole
/// (<see cref="PictureMoving"/>: MonitorScreen holds the desktop's raycaster
/// off while the picture moves, so a click is never mapped through the
/// squeezed projection onto a part of the desktop the player cannot see).
/// The bezel grows to the screen's height on screens taller than 16:9 (BUG-58b,
/// CODE-161: at 5:4 and 4:3 the fixed frame left the PC's text at about 12
/// px), never past <see cref="MaxWidthShare"/> of the width, so the room
/// stays in view; it never shrinks under its designed size.
/// </summary>
public sealed class PcFrame : MonoBehaviour
{
    /// <summary>The power-on glow's strength on the glass (alpha) as a flash, and as a Reduce flashing tint.</summary>
    private const float GlowFlash = 0.55f, GlowTint = 0.2f;

    /// <summary>The afterglow dot's size as a share of the glass's height.</summary>
    private const float DotShare = 0.02f;

    /// <summary>The most of the screen's width the grown bezel takes (the room stays in view on its right).</summary>
    private const float MaxWidthShare = 0.8f;

    /// <summary>The bezel's margin above and below on the screen (canvas units).</summary>
    private const float HeightMargin = 20f;

    /// <summary>The frame's overlay objects (bezel, glass, close X, power button, room catcher), shown while open.</summary>
    [SerializeField] private GameObject root;

    /// <summary>The glass rectangle on the overlay canvas: the frame camera draws the desktop exactly there (4:3, like the desktop); its parent is the bezel.</summary>
    [SerializeField] private RectTransform glass;

    /// <summary>The camera that draws the desktop's layer into the glass (enabled only while the picture shows).</summary>
    [SerializeField] private Camera frameCamera;

    /// <summary>The power move's and the flash budget's knobs (a scene built before the field: the knobs' own defaults).</summary>
    [SerializeField] private DesktopConfigSO config;

    /// <summary>The glass's corners in screen pixels (reused; an overlay canvas's world space is the screen).</summary>
    private readonly Vector3[] _corners = new Vector3[4];

    /// <summary>True while the screen is on (MonitorScreen tells it): the frame may open (F1's guard).</summary>
    private static bool _screenOn = true;

    /// <summary>The power move (CrtPower, from the config).</summary>
    private CrtPower _power;

    /// <summary>The power move's frame last drawn: a move reversed under way picks up from it (CrtPower.Resume).</summary>
    private CrtFrame _drawn;

    /// <summary>The frame's alpha (its fades) and whether it takes clicks (not while it closes), on its root.</summary>
    private CanvasGroup _fade;

    /// <summary>The dark glass over the picture (the warm-up, the Reduced Motion fade), the power-on glow over it, and the afterglow dot on top.</summary>
    private Image _veil, _glow, _dot;

    /// <summary>True while open (from the press frame; the picture may still be powering on).</summary>
    private bool _open;

    /// <summary>True while the picture powers on or collapses.</summary>
    private bool _moving;

    /// <summary>True once this opening's glow was asked for.</summary>
    private bool _glowed;

    /// <summary>True once this opening's degauss hum was played.</summary>
    private bool _hummed;

    /// <summary>True while the frame fades out after the afterglow.</summary>
    private bool _leaving;

    /// <summary>The afterglow's frame after a collapse (-1: none).</summary>
    private int _afterglow = -1;

    /// <summary>When the power-on or the collapse began (unscaled seconds).</summary>
    private float _movedAt;

    /// <summary>The glow's level and when it ends (unscaled seconds).</summary>
    private float _glowLevel, _glowUntil;

    /// <summary>True while the frame is open.</summary>
    public bool IsOpen => _open;

    /// <summary>True when the PC frame may open now (UI-PC's seam; OfficeControls' F1 path checks it, BUG-58c): while the screen is on, so F1 never opens a dark frame. True with no screen (the seam's default).</summary>
    public static bool CanOpen => _screenOn;

    /// <summary>The screen's power (MonitorScreen, each time it changes): F1 opens the frame only while it is on.</summary>
    public static void ScreenPowered(bool on) => _screenOn = on;

    /// <summary>True from the press frame until the picture is whole (opening), and from the close until the frame is gone: the desktop takes no clicks meanwhile (MonitorScreen; DesktopWindowManager keeps the EventSystem's navigation off too).</summary>
    public static bool PictureMoving { get; private set; }

    /// <summary>Raised when <see cref="PictureMoving"/> changes.</summary>
    public static event Action PictureMovingChanged;

    /// <summary>Sets <see cref="PictureMoving"/> and tells its listeners when it changed.</summary>
    private static void SetMoving(bool moving)
    {
        if (PictureMoving == moving)
            return;
        PictureMoving = moving;
        PictureMovingChanged?.Invoke();
    }

    /// <summary>The frame starts closed; the knobs are read once.</summary>
    private void Awake()
    {
        DesktopConfigSO knobs = config != null ? config : ScriptableObject.CreateInstance<DesktopConfigSO>();
        _power = knobs.PowerMove();
        FlashPolicy.Configure(knobs.flashesPerSecond, knobs.flashTintSeconds);
        if (knobs != config)
            Destroy(knobs);
        if (root != null && !root.TryGetComponent(out _fade))
            _fade = root.AddComponent<CanvasGroup>();
        if (glass != null)
        {
            _veil = GlassPart("PowerVeil", Vector2.zero, Vector2.one, Color.black);
            _glow = GlassPart("PowerGlow", Vector2.zero, Vector2.one, Color.white);
            float half = DotShare / 2f;
            _dot = GlassPart("Afterglow", new Vector2(0.5f - half * 0.75f, 0.5f - half), new Vector2(0.5f + half * 0.75f, 0.5f + half), Color.white);
        }
        _open = false;
        Finish();
    }

    /// <summary>A frame destroyed mid-move (a scene unload) leaves the desktop's input to the next one.</summary>
    private void OnDestroy() => SetMoving(false);

    /// <summary>Orders the frame camera right after <paramref name="office"/> (the art office's camera), so the desktop draws over the office in the glass and nothing draws over it.</summary>
    public void DrawAfter(Camera office)
    {
        if (frameCamera != null && office != null)
            frameCamera.depth = office.depth + 1f;
    }

    /// <summary>
    /// Opens the frame (it fades in, the hum leads the picture, the picture
    /// powers on) or closes it (the picture collapses, glows out, then the
    /// frame fades out). Reversed while a move is under way, the new move
    /// picks up where the glass is (CrtPower.Resume: F1's "no frame-0 UI
    /// pop"); closing before any picture showed, the frame only fades out.
    /// </summary>
    public void SetOpen(bool open)
    {
        if (open == _open)
            return;
        bool reversing = _moving || _afterglow >= 0 || _leaving;
        _open = open;
        if (_fade != null)
            _fade.blocksRaycasts = open; // closing, the fading frame (its room catcher, X and power button) takes no click: the first click after Q reaches the room
        if (!open && root != null)
            Sounds.Play(SoundCues.PcOff); // the collapse; opening, the hum plays when it is due (Step)
        bool leaving = false;
        float from = reversing ? _power.Resume(_drawn, open, MotionPreference.Reduced, out leaving) : 0f;
        _moving = !leaving;
        _glowed = reversing && _drawn.Picture; // a picture still on the glass powers back on without a second glow
        _hummed = false;
        _leaving = leaving;
        _afterglow = -1;
        Show(_dot, 0f);
        _movedAt = Time.unscaledTime - from;
        SetMoving(true);
        if (open && root != null)
            root.SetActive(true);
        Step();
    }

    /// <summary>Only while shown: steps the power move and the afterglow and follows the glass when the screen's size changes (no allocation).</summary>
    private void LateUpdate()
    {
        if (root != null && root.activeSelf)
            Step();
    }

    /// <summary>The picture's power move now (CrtPower), the hum, the glow, the afterglow, the frame's fade out, and the end of a move.</summary>
    private void Step()
    {
        FitBezel();
        if (_afterglow >= 0)
        {
            Afterglow();
            return;
        }
        if (_leaving)
        {
            Leave();
            return;
        }
        bool reduced = MotionPreference.Reduced;
        float elapsed = Time.unscaledTime - _movedAt;
        if (_open && _moving && !_hummed && root != null && _power.HumStarted(elapsed, reduced))
        {
            _hummed = true;
            Sounds.Play(SoundCues.PcOn); // the CRT's degauss hum, 80 ms before the picture (F1)
        }
        CrtFrame frame = _moving ? _power.At(elapsed, _open, reduced) : new CrtFrame(1f, 1f, 1f, 0f, true);
        Draw(frame);
        if (_open && !_glowed && _power.PictureStarted(elapsed, true, reduced))
        {
            _glowed = true;
            Glow();
        }
        ShowGlow();
        if (!_moving || elapsed < _power.Seconds(_open, reduced))
            return;
        _moving = false;
        if (_open)
        {
            Fit(1f, 1f);
            SetMoving(false); // the picture is whole: the desktop takes clicks
        }
        else if (reduced)
            Finish();
        else if (_power.AfterglowFrames > 0)
            Afterglow();
        else
            StartLeaving();
    }

    /// <summary>The frame's alpha, the dark glass, the picture's camera and its squeeze, as <paramref name="frame"/> says.</summary>
    private void Draw(CrtFrame frame)
    {
        _drawn = frame;
        if (_fade != null)
            _fade.alpha = frame.Bezel;
        Show(_veil, frame.Veil);
        if (frameCamera != null && frameCamera.enabled != frame.Picture)
            frameCamera.enabled = frame.Picture;
        if (frame.Picture)
            Fit(frame.X, frame.Y);
    }

    /// <summary>One frame of the phosphor afterglow on the dark glass (the picture is gone; the dot glows out), then the frame's fade out.</summary>
    private void Afterglow()
    {
        if (_afterglow < 0)
            _afterglow = 0;
        float level = _power.Afterglow(_afterglow++);
        if (level <= 0f)
        {
            _afterglow = -1;
            Show(_dot, 0f);
            StartLeaving();
            return;
        }
        if (frameCamera != null && frameCamera.enabled)
            frameCamera.enabled = false;
        Show(_dot, level);
    }

    /// <summary>Starts the frame's fade out (full motion, after the afterglow).</summary>
    private void StartLeaving()
    {
        _leaving = true;
        _movedAt = Time.unscaledTime;
        Leave();
    }

    /// <summary>One frame of the frame's fade out over the room (CrtPower.Leaving), then its end.</summary>
    private void Leave()
    {
        float elapsed = Time.unscaledTime - _movedAt;
        if (elapsed >= _power.LeaveSeconds)
        {
            Finish();
            return;
        }
        Draw(_power.Leaving(elapsed));
    }

    /// <summary>A closed frame's end state: its overlay hidden, its camera off, the glass parts hidden; the desktop's input is the screen's again.</summary>
    private void Finish()
    {
        _afterglow = -1;
        _leaving = false;
        SetMoving(false);
        if (root != null)
            root.SetActive(false);
        if (frameCamera != null)
        {
            frameCamera.enabled = false;
            frameCamera.ResetProjectionMatrix();
        }
        if (_fade != null)
            _fade.alpha = 1f;
        Show(_veil, 0f);
        Show(_dot, 0f);
        _glowUntil = 0f;
        ShowGlow();
    }

    /// <summary>The power-on's glow on the glass as FlashPolicy allows it: a one-frame flash, a held tint (Reduce flashing), or nothing.</summary>
    private void Glow()
    {
        float now = Time.unscaledTime;
        switch (FlashPolicy.Request(now))
        {
            case FlashStyle.Flash:
                _glowLevel = GlowFlash;
                _glowUntil = now + Time.unscaledDeltaTime * 0.5f; // this frame only (F1: one frame, local to the glass)
                break;
            case FlashStyle.Tint:
                _glowLevel = GlowTint;
                _glowUntil = now + FlashPolicy.TintSeconds;
                break;
        }
    }

    /// <summary>The glow while it lasts (white over the glass at its level), else hidden.</summary>
    private void ShowGlow() => Show(_glow, Time.unscaledTime < _glowUntil ? _glowLevel : 0f);

    /// <summary>A glass part at <paramref name="alpha"/> (hidden at 0).</summary>
    private static void Show(Image part, float alpha)
    {
        if (part == null)
            return;
        bool on = alpha > 0f;
        if (part.gameObject.activeSelf != on)
            part.gameObject.SetActive(on);
        if (on && !Mathf.Approximately(part.color.a, alpha))
        {
            Color c = part.color;
            c.a = alpha;
            part.color = c;
        }
    }

    /// <summary>A plain image on the glass between <paramref name="min"/> and <paramref name="max"/> (anchors), on the glass's layer, taking no clicks, hidden.</summary>
    private Image GlassPart(string partName, Vector2 min, Vector2 max, Color color)
    {
        var go = new GameObject(partName, typeof(RectTransform), typeof(Image));
        go.layer = glass.gameObject.layer;
        var rect = (RectTransform)go.transform;
        rect.SetParent(glass, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var image = go.GetComponent<Image>();
        image.raycastTarget = false;
        image.color = color;
        go.SetActive(false);
        return image;
    }

    /// <summary>The bezel (the glass's parent) as large as the screen's height allows (canvas units), within the width share, never under its designed size.</summary>
    private void FitBezel()
    {
        if (glass == null || !(glass.parent is RectTransform bezel) || !(bezel.parent is RectTransform area))
            return;
        Vector2 room = area.rect.size, size = bezel.rect.size;
        if (size.x <= 0f || size.y <= 0f)
            return;
        float scale = Mathf.Max(1f, Mathf.Min((room.y - 2f * HeightMargin) / size.y, room.x * MaxWidthShare / size.x));
        if (!Mathf.Approximately(bezel.localScale.x, scale))
            bezel.localScale = new Vector3(scale, scale, 1f);
    }

    /// <summary>
    /// The frame camera draws into the glass's whole rectangle on the screen,
    /// its picture squeezed about the glass's centre to <paramref name="sx"/>
    /// by <paramref name="sy"/> of it (the CRT's line and dot: a scaled
    /// projection, so the picture is squashed into a line inside the dark
    /// tube); the full picture uses the camera's own projection.
    /// </summary>
    private void Fit(float sx, float sy)
    {
        if (glass == null || frameCamera == null)
            return;
        glass.GetWorldCorners(_corners);
        float w = Mathf.Max(_corners[2].x - _corners[0].x, 1f), h = Mathf.Max(_corners[2].y - _corners[0].y, 1f);
        (float x, float y, float vw, float vh) = ScreenMapping.Viewport(_corners[0].x, _corners[0].y, w, h, Screen.width, Screen.height);
        frameCamera.rect = new Rect(x, y, vw, vh);
        frameCamera.ResetProjectionMatrix();
        if (sx >= 1f && sy >= 1f)
            return;
        frameCamera.projectionMatrix = Matrix4x4.Scale(new Vector3(Mathf.Max(sx, 1e-4f), Mathf.Max(sy, 1e-4f), 1f)) * frameCamera.projectionMatrix;
    }
}
