using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The PC frame (ReStory-style): clicking the PC opens a flat, front-facing
/// monitor over the left of the office, with the room still visible on the
/// right. The frame (bezel, close X, power button, exits) is overlay UI; the
/// desktop itself shows inside its glass through the frame camera, which
/// renders only the desktop's layer into the glass's screen rectangle and is
/// the desktop canvas's event camera, so clicks, dragging and text fields
/// work as on any canvas. The frame camera draws right after the art office's
/// camera (DrawAfter, set by the office binder at load), whatever depth the
/// art gave its camera: a base camera drawn before the office's is covered
/// by the office's clear (the anime hall's camera draws at depth 100, the
/// room's at -1). OfficeViewController opens and closes it; Q and a click on
/// the bezel (or the room beside it) leave the PC, nothing else does
/// (BUG-50a). The picture powers on as a CRT does (BUG-32c, GAME_FEEL_BIBLE
/// F1; CrtPower): a bright line that grows to the full picture, with a
/// one-frame glow on the glass (through FlashPolicy, G3), and collapses to a
/// line and a dot on closing; with Reduced Motion the frame fades instead.
/// Its input follows the view at once: only the picture moves. The bezel
/// grows to the screen's height on screens taller than 16:9 (BUG-58b,
/// CODE-161: at 5:4 and 4:3 the fixed frame left the PC's text at about 12
/// px), never past <see cref="MaxWidthShare"/> of the width, so the room
/// stays in view; it never shrinks under its designed size.
/// </summary>
public sealed class PcFrame : MonoBehaviour
{
    /// <summary>The power-on glow's strength on the glass (alpha) as a flash, and as a Reduce flashing tint.</summary>
    private const float GlowFlash = 0.55f, GlowTint = 0.2f;

    /// <summary>The most of the screen's width the grown bezel takes (the room stays in view on its right).</summary>
    private const float MaxWidthShare = 0.8f;

    /// <summary>The bezel's margin above and below on the screen (canvas units).</summary>
    private const float HeightMargin = 20f;

    /// <summary>The frame's overlay objects (bezel, glass, close X, power button, exit catcher), shown while open.</summary>
    [SerializeField] private GameObject root;

    /// <summary>The glass rectangle on the overlay canvas: the frame camera draws the desktop exactly there (4:3, like the desktop); its parent is the bezel.</summary>
    [SerializeField] private RectTransform glass;

    /// <summary>The camera that draws the desktop's layer into the glass (enabled only while open).</summary>
    [SerializeField] private Camera frameCamera;

    /// <summary>The glass's corners in screen pixels (reused; an overlay canvas's world space is the screen).</summary>
    private readonly Vector3[] _corners = new Vector3[4];

    /// <summary>True while the screen is on (MonitorScreen tells it): the frame may open (F1's guard).</summary>
    private static bool _screenOn = true;

    /// <summary>The frame's fade (Reduced Motion), on its root.</summary>
    private CanvasGroup _fade;

    /// <summary>The power-on glow on the glass (made at the first open).</summary>
    private Image _glow;

    /// <summary>True while open (from the press frame; the picture may still be powering on).</summary>
    private bool _open;

    /// <summary>True while the picture powers on or collapses.</summary>
    private bool _moving;

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

    /// <summary>The frame starts closed.</summary>
    private void Awake()
    {
        if (root != null && !root.TryGetComponent(out _fade))
            _fade = root.AddComponent<CanvasGroup>();
        _open = false;
        Finish();
    }

    /// <summary>Orders the frame camera right after <paramref name="office"/> (the art office's camera), so the desktop draws over the office in the glass and nothing draws over it.</summary>
    public void DrawAfter(Camera office)
    {
        if (frameCamera != null && office != null)
            frameCamera.depth = office.depth + 1f;
    }

    /// <summary>Opens the frame (fitting the frame camera to the glass; the picture powers on) or closes it (the picture collapses, then the frame goes).</summary>
    public void SetOpen(bool open)
    {
        if (open == _open)
            return;
        _open = open;
        if (root != null)
            Sounds.Play(open ? SoundCues.PcOn : SoundCues.PcOff); // the CRT's thump or collapse
        _moving = true;
        _movedAt = Time.unscaledTime;
        if (open)
        {
            if (root != null)
                root.SetActive(true);
            if (frameCamera != null)
                frameCamera.enabled = true;
            Glow();
        }
        Step();
    }

    /// <summary>Only while shown: steps the power-on or the collapse and follows the glass when the screen's size changes (no allocation).</summary>
    private void LateUpdate()
    {
        if (root != null && root.activeSelf)
            Step();
    }

    /// <summary>The picture's power move now (CrtPower), the glow, and the end of a collapse.</summary>
    private void Step()
    {
        FitBezel();
        bool reduced = MotionPreference.Reduced;
        float elapsed = Time.unscaledTime - _movedAt;
        (float x, float y, float alpha) = _moving ? CrtPower.At(elapsed, _open, reduced) : (1f, 1f, 1f);
        if (reduced)
            (x, y) = (1f, 1f);
        else
            alpha = 1f;
        if (_fade != null)
            _fade.alpha = alpha;
        Fit(x, y);
        ShowGlow();
        if (_moving && elapsed >= CrtPower.Seconds(_open, reduced))
        {
            _moving = false;
            if (!_open)
                Finish();
            else
                Fit(1f, 1f);
        }
    }

    /// <summary>A closed frame's end state: its overlay hidden and its camera off.</summary>
    private void Finish()
    {
        if (root != null)
            root.SetActive(false);
        if (frameCamera != null)
            frameCamera.enabled = false;
        if (_fade != null)
            _fade.alpha = 1f;
    }

    /// <summary>The power-on's glow on the glass as FlashPolicy allows it: a one-frame flash, a held tint (Reduce flashing), or nothing.</summary>
    private void Glow()
    {
        if (glass == null)
            return;
        float now = Time.unscaledTime;
        switch (FlashPolicy.Request(now))
        {
            case FlashStyle.Flash:
                _glowLevel = GlowFlash;
                _glowUntil = now + Time.unscaledDeltaTime * 0.5f; // this frame only (F1: one frame, local to the glass)
                break;
            case FlashStyle.Tint:
                _glowLevel = GlowTint;
                _glowUntil = now + FlashBudget.TintSeconds;
                break;
            default:
                return;
        }
        if (_glow == null)
        {
            var go = new GameObject("PowerGlow", typeof(RectTransform), typeof(Image));
            go.layer = glass.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(glass, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _glow = go.GetComponent<Image>();
            _glow.raycastTarget = false;
        }
    }

    /// <summary>The glow while it lasts (white over the glass at its level), else hidden.</summary>
    private void ShowGlow()
    {
        if (_glow == null)
            return;
        bool on = Time.unscaledTime < _glowUntil;
        if (_glow.gameObject.activeSelf != on)
            _glow.gameObject.SetActive(on);
        if (on)
            _glow.color = new Color(1f, 1f, 1f, _glowLevel);
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

    /// <summary>Sets the frame camera's viewport to the glass's rectangle on the screen, scaled about its centre to <paramref name="sx"/> by <paramref name="sy"/> (the CRT's line).</summary>
    private void Fit(float sx, float sy)
    {
        if (glass == null || frameCamera == null)
            return;
        glass.GetWorldCorners(_corners);
        float w = _corners[2].x - _corners[0].x, h = _corners[2].y - _corners[0].y;
        float cx = _corners[0].x + w / 2f, cy = _corners[0].y + h / 2f;
        float sw = Mathf.Max(w * sx, 1f), sh = Mathf.Max(h * sy, 1f);
        (float x, float y, float vw, float vh) = ScreenMapping.Viewport(cx - sw / 2f, cy - sh / 2f, sw, sh, Screen.width, Screen.height);
        frameCamera.rect = new Rect(x, y, vw, vh);
    }
}
