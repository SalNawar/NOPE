using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The DETAIN button on the desk (the desk machine spec §2; Saleh
/// 2026-10-07: "detain only if the traveller breaks the law", and
/// "everything diegetic": no screen-space buttons for verdicts): a red
/// mushroom button under a hinged clear safety cover, built in-engine under
/// a prop contract (a root with Base, Cover and Button; the Cover turns
/// about its hinge at the back edge, the Button's Cap sinks; each of Cover
/// and Button carries a click box on the Interactable layer; the art may
/// replace the meshes). Click the cover: it flips up on a spring hinge
/// (MotionKnobs.detainCoverAngle; it closes again by itself after
/// detainCoverSeconds unused, or at a second click). Click the button with
/// the cover up: a deep press, and while a traveller stands at the desk the
/// third verdict is committed (DeskStampTray.Commit: any time after the
/// arrival, stamped or not): the alarm chirp (detain), a red flash over the
/// hall, FeelDirector.Hit, guards take the traveller; with nobody there it
/// only clicks. Every motion on springs; Reduced Motion snaps them. Build
/// Office UI builds it in the office; the office binder lays it on the desk.
/// </summary>
public sealed class DetainButton : MonoBehaviour
{
    /// <summary>The stamps: the case and the commit.</summary>
    [SerializeField] private DeskStampTray stamps;

    /// <summary>The cover's hinge (it turns about its local x by the open angle).</summary>
    [SerializeField] private Transform cover;

    /// <summary>The cover's click box (it flips the cover).</summary>
    [SerializeField] private Clickable coverClick;

    /// <summary>The button's cap (it sinks by MotionKnobs.detainPress).</summary>
    [SerializeField] private Transform cap;

    /// <summary>The button's click box (it presses the button; under the closed cover it cannot be reached).</summary>
    [SerializeField] private Clickable buttonClick;

    /// <summary>The red flash over the hall as guards take the traveller (an image over the office, no raycasts, clear at rest).</summary>
    [SerializeField] private Image flash;

    /// <summary>Plays the cover's snap, the press and the chirp (optional).</summary>
    [SerializeField] private AudioSource sound;

    /// <summary>The flash's strongest alpha.</summary>
    private const float FlashAlpha = 0.35f;

    private Quaternion _coverRest;
    private Vector3 _capRest;
    private Spring _coverAngle, _press, _flash;
    private bool _open, _pressed;
    private float _closeAt, _releaseAt;

    /// <summary>True while the cover is up (the button can be pressed).</summary>
    public bool CoverOpen => _open;

    /// <summary>True while the cover swings, the button moves or the flash fades (the probes wait for it).</summary>
    public bool Moving => !_coverAngle.AtRest || !_press.AtRest || !_flash.AtRest || _pressed;

    private void Awake()
    {
        if (cover != null)
            _coverRest = cover.localRotation;
        if (cap != null)
            _capRest = cap.localPosition;
        if (coverClick != null)
            coverClick.onClick.AddListener(ToggleCover);
        if (buttonClick != null)
            buttonClick.onClick.AddListener(Press);
    }

    /// <summary>
    /// Lays the button (the office binder, once the desk is placed): on the
    /// desk's plane (<paramref name="deskTop"/>) where the office view
    /// (<paramref name="office"/>, drawn at <paramref name="aspect"/>) shows
    /// <paramref name="viewport"/> (DeskConfigSO.detainView), facing the
    /// chair along the view's level forward. False (it stays where it is)
    /// without a camera or when that point misses the plane.
    /// </summary>
    public bool Lay(CinemachineCamera office, float aspect, float deskTop, Vector2 viewport)
    {
        if (office == null)
            return false;
        Transform view = office.transform;
        float tan = Mathf.Tan(office.Lens.FieldOfView * 0.5f * Mathf.Deg2Rad);
        Vector3 dir = view.rotation * new Vector3((viewport.x * 2f - 1f) * tan * aspect, (viewport.y * 2f - 1f) * tan, 1f);
        if (Mathf.Abs(dir.y) < 1e-5f)
            return false;
        float t = (deskTop - view.position.y) / dir.y;
        if (t <= 0f)
            return false;
        Vector3 level = Vector3.ProjectOnPlane(view.forward, Vector3.up);
        if (level.sqrMagnitude < 1e-6f)
            level = Vector3.forward;
        transform.SetPositionAndRotation(view.position + dir * t, Quaternion.LookRotation(level.normalized, Vector3.up));
        return true;
    }

    /// <summary>The cover clicked: it flips up (or closes when up), a snap.</summary>
    public void ToggleCover()
    {
        _open = !_open;
        _closeAt = UiMotion.Now + UiMotion.Knobs.detainCoverSeconds;
        Aim(ref _coverAngle, _open ? UiMotion.Knobs.detainCoverAngle : 0f);
        CueSounds.Play(CueSounds.CoverSnap, sound);
    }

    /// <summary>The button pressed (the cover up): a deep press; while a traveller is at the desk, DETAINED is committed with the chirp, the flash and the hit.</summary>
    public void Press()
    {
        if (!_open || _pressed)
            return;
        MotionKnobs knobs = UiMotion.Knobs;
        _pressed = true;
        _releaseAt = UiMotion.Now + knobs.detainPressSeconds;
        _closeAt = UiMotion.Now + knobs.detainCoverSeconds;
        Aim(ref _press, knobs.detainPress);
        if (stamps == null || !stamps.TravellerHere || !stamps.Commit(DeskStamp.Detained))
        {
            Sounds.Play(SoundCues.UiPress);
            return;
        }
        CueSounds.Play(CueSounds.Detain, sound);
        FeelDirector.Hit(knobs.detainHit);
        _flash.Snap(1f);
        _flash.Target = 0f;
        if (UiMotion.Amount.Still)
            _flash.Snap(0f);
        ShowFlash();
    }

    /// <summary>Sets a spring's target (snapped under Reduced Motion).</summary>
    private static void Aim(ref Spring spring, float target)
    {
        spring.Target = target;
        if (UiMotion.Amount.Still)
            spring.Snap(target);
    }

    /// <summary>Steps the cover, the button and the flash on their springs; the button comes back up after its press, the cover closes by itself when left open.</summary>
    private void Update()
    {
        float dt = UiMotion.Delta(Time.unscaledDeltaTime);
        MotionKnobs knobs = UiMotion.Knobs;
        if (_pressed && UiMotion.Now >= _releaseAt)
        {
            _pressed = false;
            Aim(ref _press, 0f);
        }
        if (_open && !_pressed && UiMotion.Now >= _closeAt)
            ToggleCover();

        if (!_coverAngle.AtRest)
        {
            _coverAngle.Step(dt, knobs.Get(knobs.detainCoverFeel), 0.01f, 0.1f);
            if (cover != null)
                cover.localRotation = _coverRest * Quaternion.Euler(-_coverAngle.Value, 0f, 0f);
        }
        if (!_press.AtRest)
        {
            _press.Step(dt, knobs.Get(_pressed ? knobs.detainPressFeel : knobs.detainReleaseFeel), 1e-5f, 1e-3f);
            if (cap != null)
                cap.localPosition = _capRest + Vector3.down * _press.Value;
        }
        if (!_flash.AtRest)
        {
            _flash.Step(dt, knobs.Get(knobs.paperFeel), 0.002f, 0.01f);
            ShowFlash();
        }
    }

    private void ShowFlash()
    {
        if (flash == null)
            return;
        Color c = flash.color;
        c.a = Mathf.Clamp01(_flash.Value) * FlashAlpha;
        flash.color = c;
    }
}
