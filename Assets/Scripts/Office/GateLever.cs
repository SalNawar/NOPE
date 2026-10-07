using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// The gate lever (the desk machine spec §2; Saleh 2026-10-07: "three
/// actions", Approve = the gate lever): a heavy floor-mounted lever beside
/// the desk, built in-engine under a prop contract (a root with Base, Arm and
/// Knob; the art may replace the meshes; the Arm turns about its own pivot,
/// the Knob's click box on the Interactable layer takes the pointer). Drag
/// it down: the arm follows the pull against resistance (LeverTravel.Resisted
/// on the Lever spring, MotionKnobs), with a ratchet click every notch
/// (lever_ratchet); let go early and it springs back up. At the bottom it
/// thunks home (lever_home, a hit through FeelDirector), the papers' APPROVED
/// verdict is committed (DeskStampTray.Commit: the hall's portal flares as
/// the traveller leaves into it, portal_through) and after a moment it
/// springs back up. It only moves for an APPROVED passport handed back:
/// otherwise a press gives a "no" wobble, a thunk and a note. Reduced Motion
/// snaps the arm instead of springing it and drops the wobble; the Motion
/// intensity scales the wobble. Build Office UI builds it in the office; the
/// office binder lays it beside the desk.
/// </summary>
public sealed class GateLever : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    /// <summary>The stamps: the passport's verdict and the hand-back the lever needs, and the commit.</summary>
    [SerializeField] private DeskStampTray stamps;

    /// <summary>The arm (its pivot at the hinge on the base): it turns about its local x, down by the travel.</summary>
    [SerializeField] private Transform arm;

    /// <summary>Plays the ratchet, the thunk, the portal's whoosh and the refusal's thunk (optional).</summary>
    [SerializeField] private AudioSource sound;

    /// <summary>Which way the arm turns about its local x when pulled down (1 or -1, as the lever faces the chair).</summary>
    [SerializeField] private float downSign = 1f;

    private Quaternion _armRest;
    private Spring _angle, _wobble;
    private float _pull;
    private bool _pulling;
    private bool _home;
    private float _homeUntil;

    /// <summary>The arm's angle down from rest (degrees; the probes read it).</summary>
    public float Angle => _angle.Value;

    /// <summary>True while the arm rests home after a full pull.</summary>
    public bool IsHome => _home;

    /// <summary>True while the arm or its wobble moves (the probes wait for it).</summary>
    public bool Moving => _pulling || _home || !_angle.AtRest || !_wobble.AtRest;

    private void Awake()
    {
        if (arm != null)
            _armRest = arm.localRotation;
    }

    /// <summary>
    /// Lays the lever (the office binder, once the desk is placed): its hinge
    /// on the desk's plane (<paramref name="deskTop"/>) where the office
    /// view (<paramref name="office"/>, drawn at <paramref name="aspect"/>)
    /// shows <paramref name="viewport"/> (DeskConfigSO.leverView: beside the
    /// desk, low on the right), its base reaching the floor below, facing the
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

    /// <summary>True when the lever may move: an APPROVED passport handed back, its traveller still at the desk.</summary>
    private bool Allowed => stamps != null && stamps.HandedBack && stamps.Verdict == DeskStamp.Approved && stamps.TravellerHere;

    /// <summary>The left button went down on the knob: refused (the "no" wobble, a thunk, a note) unless the lever may move.</summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || _home)
            return;
        if (!Allowed)
            Refuse();
    }

    /// <summary>A pull starts (only when the lever may move).</summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || _home || !Allowed)
            return;
        _pulling = true;
        _pull = _angle.Value;
    }

    /// <summary>The pull: each pixel down pulls MotionKnobs.leverPullPerPixel degrees, the arm lagging against its resistance; a click each notch it passes going down; home at the full travel.</summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (!_pulling)
            return;
        MotionKnobs knobs = UiMotion.Knobs;
        _pull = Mathf.Max(0f, _pull - eventData.delta.y * knobs.leverPullPerPixel);
        _angle.Target = LeverTravel.Resisted(_pull, knobs.leverTravel, knobs.leverResistance);
        if (UiMotion.Amount.Still)
            Step(0f);
    }

    /// <summary>Let go: home already, or the arm springs back up.</summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_pulling)
            return;
        _pulling = false;
        if (!_home)
            _angle.Target = 0f;
    }

    /// <summary>The "no": the arm wobbles (a kick on its wobble spring), a thunk and the note why.</summary>
    private void Refuse()
    {
        MotionKnobs knobs = UiMotion.Knobs;
        MotionAmount amount = UiMotion.Amount;
        if (!amount.Still)
            _wobble.Kick(knobs.Get(knobs.leverRefuseFeel).KickFor(knobs.leverRefuse * amount.Share));
        CueSounds.Play(UiSoundCue.Error, sound);
        if (stamps != null)
            stamps.Note("hardware.refused.lever");
    }

    private void Update()
    {
        if (_pulling || _home || !_angle.AtRest || !_wobble.AtRest)
            Step(FeelDirector.StepDelta(Time.unscaledDeltaTime));
    }

    /// <summary>Steps the arm and the wobble on their springs (snaps under Reduced Motion), clicks the ratchet, thunks home and commits, and springs back after the rest home.</summary>
    private void Step(float dt)
    {
        MotionKnobs knobs = UiMotion.Knobs;
        float before = _angle.Value;
        if (UiMotion.Amount.Still)
            _angle.Snap(_angle.Target);
        else
            _angle.Step(dt, knobs.Get(knobs.leverFeel), 0.01f, 0.1f);
        _wobble.Step(dt, knobs.Get(knobs.leverRefuseFeel), 0.01f, 0.1f);

        if (_pulling && LeverTravel.Crossed(before, _angle.Value, knobs.leverNotch) > 0)
            CueSounds.Play(UiSoundCue.LeverRatchet, sound, 1f + 0.02f * LeverTravel.Notch(_angle.Value, knobs.leverNotch));

        if (_pulling && !_home && LeverTravel.Home(_angle.Value, knobs.leverTravel))
            Thunk(knobs);
        if (_home && !_pulling && FeelDirector.Now >= _homeUntil)
        {
            _home = false;
            _angle.Target = 0f;
        }

        if (arm != null)
            arm.localRotation = _armRest * Quaternion.Euler(downSign * (_angle.Value + _wobble.Value), 0f, 0f);
    }

    /// <summary>Home: the thunk and the hit, the APPROVED verdict committed (the traveller leaves into the portal), the arm resting there a moment.</summary>
    private void Thunk(MotionKnobs knobs)
    {
        _home = true;
        _homeUntil = FeelDirector.Now + knobs.leverHomeSeconds;
        _angle.Snap(knobs.leverTravel);
        CueSounds.Play(UiSoundCue.LeverHome, sound);
        FeelDirector.Hit(knobs.leverHit);
        if (stamps != null && stamps.Commit(DeskStamp.Approved))
            CueSounds.Play(UiSoundCue.PortalThrough, sound);
        _pulling = false;
    }
}
