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
/// detainCoverSeconds unused, or at a second click); a click on the button
/// while the cover is shut lifts it too (DetainCover.OnButton: on the
/// painted desk the shut guard hides the button's top from the office
/// camera, so the click lands on its housing). Click the button with
/// the cover up: a deep press, and while a traveller stands at the desk the
/// third verdict is committed (DeskStampTray.Commit: any time after the
/// arrival, stamped or not): the alarm chirp (detain), a red flash over the
/// hall, FeelDirector.Hit, guards take the traveller; with nobody there it
/// only clicks. Every motion on springs; Reduced Motion snaps them. Build
/// Office UI builds it in the office; the office binder lays it on the desk.
/// On the painted desk (DressCover) the painting's guard is the cover's look:
/// it swings on its own hinge with the cover's spring, shut over the button
/// at rest and up as painted when open, and the cover's and the button's
/// click boxes fit the painted guard and the painted DETAIN (Saleh's
/// playtest of 2026-10-28, point 2: "detain button case open and cant be
/// closed its collider is small too").
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

    /// <summary>The painted desk's cover look (DressCover): the guard, its rotation shut and open (in its parent), or null.</summary>
    private Transform _look;
    private Quaternion _lookShut, _lookOpen;
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

    /// <summary>The cover clicked: it flips up (or closes when up), a snap.</summary>
    public void ToggleCover()
    {
        _open = !_open;
        _closeAt = UiMotion.Now + UiMotion.Knobs.detainCoverSeconds;
        Aim(ref _coverAngle, _open ? UiMotion.Knobs.detainCoverAngle : 0f);
        if (_coverAngle.AtRest)
        {
            // Snapped (Reduced Motion): Update steps only a moving spring, so the hinge and the painted guard take the cut here.
            if (cover != null)
                cover.localRotation = _coverRest * Quaternion.Euler(-_coverAngle.Value, 0f, 0f);
            ShowLook();
        }
        CueSounds.Play(SoundCues.DetainCover, sound);
    }

    /// <summary>The button pressed: the cover shut, it lifts (DetainCover.OnButton); the cover up, a deep press, and while a traveller is at the desk, DETAINED is committed with the chirp, the flash and the hit.</summary>
    public void Press()
    {
        if (_pressed)
            return;
        if (DetainCover.OnButton(_open) == DetainClick.LiftCover)
        {
            ToggleCover();
            return;
        }
        MotionKnobs knobs = UiMotion.Knobs;
        _pressed = true;
        _releaseAt = UiMotion.Now + knobs.detainPressSeconds;
        _closeAt = UiMotion.Now + knobs.detainCoverSeconds;
        Aim(ref _press, knobs.detainPress);
        if (stamps == null || !stamps.TravellerHere || !stamps.Commit(DeskStamp.Detained))
        {
            Sounds.Play(SoundCues.UiError);
            return;
        }
        CueSounds.Play(SoundCues.Detain, sound);
        FeelDirector.Hit(knobs.detainHit);
        _flash.Snap(1f);
        _flash.Target = 0f;
        if (UiMotion.Amount.Still)
            _flash.Snap(0f);
        ShowFlash();
    }

    /// <summary>
    /// The painted desk's look of the cover (the office binder, once the
    /// painted DETAIN and its guard ride this button): <paramref name="look"/>
    /// (the painting's guard, hinged at its own origin, drawn open) swings
    /// between <paramref name="shut"/> (its world rotation over the button:
    /// the bake's own frame, as the painted DETAIN lies) and its painted
    /// rotation (open) as the cover's spring does (DetainCover.Share); the
    /// cover's click box rides it, fitted to its mesh (it was the old clear
    /// box, hidden, swinging on the old hinge: the guard took no click), and
    /// the button's fits <paramref name="body"/> (the painted DETAIN: a click
    /// on its housing lifts the shut cover, then presses). The outlines draw
    /// the guard and the body. Shown at once at the cover's state.
    /// </summary>
    public void DressCover(Transform look, Quaternion shut, Renderer[] body)
    {
        if (look == null || look.parent == null)
            return;
        _look = look;
        _lookOpen = look.localRotation;
        _lookShut = Quaternion.Inverse(look.parent.rotation) * shut;
        Renderer[] guard = look.GetComponentsInChildren<Renderer>(true);
        if (coverClick != null && coverClick.TryGetComponent(out BoxCollider lid) && look.TryGetComponent(out MeshFilter mesh) && mesh.sharedMesh != null)
        {
            coverClick.transform.SetParent(look, false);
            coverClick.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            coverClick.transform.localScale = Vector3.one;
            Bounds b = mesh.sharedMesh.bounds;
            Vector3 scale = look.lossyScale;
            lid.center = b.center;
            lid.size = b.size + new Vector3(ClickPad / Mathf.Max(1e-4f, Mathf.Abs(scale.x)), ClickPad / Mathf.Max(1e-4f, Mathf.Abs(scale.y)), ClickPad / Mathf.Max(1e-4f, Mathf.Abs(scale.z))) * 2f;
            coverClick.SetOutline(guard);
        }
        if (buttonClick != null && body != null && body.Length > 0 && buttonClick.TryGetComponent(out BoxCollider press))
        {
            Bounds b = body[0].bounds;
            foreach (Renderer r in body)
                b.Encapsulate(r.bounds);
            Transform t = buttonClick.transform;
            Vector3 size = t.InverseTransformVector(b.size);
            press.center = t.InverseTransformPoint(b.center);
            press.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
            buttonClick.SetOutline(body);
        }
        ShowLook();
    }

    /// <summary>How far a click box reaches past the painted guard on every side (metres; DeskHomeMap.ClickPad's centimetre).</summary>
    private const float ClickPad = 0.01f;

    /// <summary>The painted guard at the cover's swing (DetainCover.Share; nothing without DressCover).</summary>
    private void ShowLook()
    {
        if (_look != null)
            _look.localRotation = Quaternion.SlerpUnclamped(_lookShut, _lookOpen, DetainCover.Share(_coverAngle.Value, UiMotion.Knobs.detainCoverAngle));
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
            ShowLook();
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
