using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The machine close-up (Saleh's 1018a playtest: "you cant see what each
/// machiene does the camera should be better"; his call 2026-10-18: a
/// Settings toggle, on by default; the desk layout spec §4): when a machine is
/// used (the scanner reads a paper, the chip reader reads a chip, an
/// upgrade's object is clicked) the camera pushes in on it, holds while it
/// works and gives the view back (CloseUpTimeline: never during a drag, any
/// click or key gives the view back at once, the next machine takes it over).
/// A Cinemachine camera of its own, above the office and the reading view
/// while it holds: DeskLayoutSettings.closeUpDistance from the machine's
/// middle on the chair's side, closeUpHeight of that above it, looking at it
/// through closeUpFieldOfView; the push-in and the way back take
/// closeUpBlendSeconds, eased (a cut under Reduced Motion). Off with
/// MotionPreference.MachineCloseUps. Added and fed by the office binder.
/// </summary>
public sealed class MachineCloseUp : MonoBehaviour
{
    /// <summary>The close-up's ids: its bay's name in the desk's plan (an upgrade's object goes by its own name).</summary>
    private const string ScannerId = "scanner", ChipReaderId = "chip_reader";

    /// <summary>How long a machine still working keeps its close-up past this frame (seconds): it is checked every frame.</summary>
    private const float WorkingHold = 0.4f;

    /// <summary>How often the upgrades' objects on the desk are looked for again (seconds): one may arrive on any morning.</summary>
    private const float UpgradeLook = 1f;

    /// <summary>The close-up's place above the office camera's priority (the reading view's is one above it).</summary>
    private const int PriorityAboveOffice = 2;

    private readonly CloseUpTimeline _timeline = new CloseUpTimeline();
    private readonly Dictionary<string, Renderer[]> _machines = new Dictionary<string, Renderer[]>();
    private readonly HashSet<Clickable> _heard = new HashSet<Clickable>();
    private CinemachineCamera _camera, _office;
    private DeskLayoutSettings _layout;
    private DeskFrame _frame;
    private DeskController _desk;
    private DeskHardware _hardware;
    private CinemachineCore.GetBlendOverrideDelegate _blend, _previous;
    private bool _scanning, _reading, _chaining;
    private int _startedFrame = -1;
    private float _nextLook;

    /// <summary>
    /// Readies the close-up (the office binder, once the desk is laid): its
    /// camera beside <paramref name="office"/>, the scanner's parts
    /// (<paramref name="scannerParts"/>), the machines of
    /// <paramref name="hardware"/> and the upgrades' objects; the scanner's
    /// work read from <paramref name="desk"/>.
    /// </summary>
    public void Init(CinemachineCamera office, DeskLayoutSettings layout, DeskFrame frame, DeskController desk, DeskHardware hardware, Renderer[] scannerParts)
    {
        if (office == null || layout == null)
            return;
        _office = office;
        _layout = layout;
        _frame = frame;
        _desk = desk;
        _hardware = hardware;
        if (scannerParts != null && scannerParts.Length > 0)
            _machines[ScannerId] = scannerParts;
        if (hardware != null && hardware.Reader != null)
            _machines[ChipReaderId] = hardware.Reader.GetComponentsInChildren<Renderer>(true);

        var go = new GameObject("MachineCloseUpCamera");
        go.transform.SetParent(office.transform.parent, false);
        _camera = go.AddComponent<CinemachineCamera>();
        LensSettings lens = office.Lens;
        lens.FieldOfView = layout.closeUpFieldOfView;
        _camera.Lens = lens;
        _camera.Priority = 0;
        // The blend hook goes in once, now (before the reading view's first turn puts its own over it), chained to the one before.
        _blend = Blend;
        _previous = CinemachineCore.GetBlendOverride;
        CinemachineCore.GetBlendOverride = _blend;
    }

    private void OnDestroy()
    {
        if (CinemachineCore.GetBlendOverride == _blend)
            CinemachineCore.GetBlendOverride = _previous;
    }

    /// <summary>Each frame: a machine that starts working is shown close; one still working holds it; any click or key, or the hold running out, gives the view back.</summary>
    private void Update()
    {
        if (_camera == null)
            return;
        float now = Time.unscaledTime;
        if (now >= _nextLook)
        {
            _nextLook = now + UpgradeLook;
            HearUpgrades();
        }

        bool scanning = _desk != null && _desk.Scanning;
        bool reading = _hardware != null && _hardware.Reader != null && _hardware.Reader.Busy;
        if (scanning && !_scanning)
            Show(ScannerId);
        if (reading && !_reading)
            Show(ChipReaderId);
        if (scanning)
            _timeline.Extend(ScannerId, now, WorkingHold);
        if (reading)
            _timeline.Extend(ChipReaderId, now, WorkingHold);
        _scanning = scanning;
        _reading = reading;

        if (_timeline.Active && Time.frameCount > _startedFrame && AnyInput() && _timeline.Input())
            Hide();
        else if (_timeline.Tick(now))
            Hide();
    }

    /// <summary>The upgrades' objects on the desk now: a click on one shows it close (each heard once).</summary>
    private void HearUpgrades()
    {
        DeskUpgrades upgrades = _hardware != null ? _hardware.Upgrades : null;
        if (upgrades == null)
            return;
        foreach (Component shown in upgrades.Shown)
        {
            if (shown == null)
                continue;
            foreach (Clickable click in shown.GetComponentsInChildren<Clickable>(true))
            {
                if (!_heard.Add(click))
                    continue;
                string id = shown.name;
                _machines[id] = shown.GetComponentsInChildren<Renderer>(true);
                click.onClick.AddListener(() => Show(id, _layout.closeUpHoldSeconds));
            }
        }
    }

    /// <summary>Shows machine <paramref name="id"/> close for <paramref name="hold"/> seconds (a working machine's hold is renewed while it works).</summary>
    private void Show(string id, float hold = WorkingHold)
    {
        bool dragging = _desk != null && _desk.IsDragging;
        if (!_machines.TryGetValue(id, out Renderer[] parts) || !TryBounds(parts, out Bounds b) ||
            !_timeline.Start(id, MotionPreference.MachineCloseUps, dragging, Time.unscaledTime, hold))
            return;
        Vector3 at = b.center - _frame.Forward * _layout.closeUpDistance + Vector3.up * (_layout.closeUpDistance * _layout.closeUpHeight);
        _camera.transform.SetPositionAndRotation(at, Quaternion.LookRotation(b.center - at, Vector3.up));
        _camera.Priority = _office.Priority.Value + PriorityAboveOffice;
        _startedFrame = Time.frameCount;
    }

    /// <summary>The view goes back to where the player was.</summary>
    private void Hide() => _camera.Priority = 0;

    /// <summary>True when a mouse button or a key went down this frame.</summary>
    private static bool AnyInput()
    {
        Mouse mouse = Mouse.current;
        Keyboard keys = Keyboard.current;
        return (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame))
               || (keys != null && keys.anyKey.wasPressedThisFrame);
    }

    private static bool TryBounds(Renderer[] parts, out Bounds b)
    {
        b = default;
        bool any = false;
        foreach (Renderer r in parts)
        {
            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy)
                continue;
            if (!any)
                b = r.bounds;
            else
                b.Encapsulate(r.bounds);
            any = true;
        }
        return any;
    }

    /// <summary>The blend to or from the close-up: DeskLayoutSettings.closeUpBlendSeconds, eased (a cut under Reduced Motion); any other blend is left to the hook before ours, or the brain's own.</summary>
    private CinemachineBlendDefinition Blend(ICinemachineCamera from, ICinemachineCamera to, CinemachineBlendDefinition fallback, Object owner)
    {
        if (_camera == null || (!ReferenceEquals(from, _camera) && !ReferenceEquals(to, _camera)))
        {
            // A hook that re-chains itself over ours (the reading view's) may lead back here: the brain's own blend then.
            if (_previous == null || _chaining)
                return fallback;
            _chaining = true;
            try
            {
                return _previous(from, to, fallback, owner);
            }
            finally
            {
                _chaining = false;
            }
        }
        float seconds = MotionPreference.Reduced ? 0f : _layout.closeUpBlendSeconds;
        return seconds > 0f
            ? new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, seconds)
            : new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
    }
}
