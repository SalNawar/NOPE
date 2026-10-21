using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The desk's slide-in (desk v3, Saleh 2026-10-20: "yes but they need to
/// slide"; D2: "the machine will slide in a little bit to help the player";
/// UNITY_PLAN section 5, DESK-HOMES): each frame it finds every present
/// slider (MachineSlideIn) under the cursor on the office camera (the
/// front-most silhouette among the Interactable click boxes, the cursor's
/// point on the desk inside a home's footprint, the px to each silhouette at
/// 1080p), lets ProximityAssist decide (nearest wins, one at a time, the
/// hand-off, the springs, the linger and the way home, busy machines left as
/// they are, Reduced Motion as a cut), gives each slider its pose and sounds
/// the cues: the unlatch tick, the felt shush, the knock at the peak, the
/// reversed shush, the seat's clack (DeskSlides' sound ids; a machine without
/// its clip yet plays the paper slide and the drawer's close as stand-ins;
/// Cued tells every listener, FEEDBACK's lamp and DESK-ART's shadow among
/// them). A machine a paper was let go on stays out while the paper lands
/// (DeskSlides.dropHoldSeconds). While the PC is open nothing is near.
/// The office binder adds and binds it once the desk is laid
/// (OfficeSceneBinder.BindSlideIn) and attaches today's sliders; owners attach
/// theirs (MachineSlideIn.Attach). Unbound or disabled, every slider is home.
/// </summary>
public sealed class DeskSlideIn : MonoBehaviour
{
    private const int MaxHits = 16;

    private static DeskSlideIn _current;

    private readonly List<MachineSlideIn> _sliders = new List<MachineSlideIn>();
    private readonly List<SlideCandidate> _candidates = new List<SlideCandidate>();
    private readonly HashSet<string> _busy = new HashSet<string>();
    private readonly HashSet<string> _taken = new HashSet<string>();
    private readonly Dictionary<string, float> _droppedAt = new Dictionary<string, float>();
    private readonly RaycastHit[] _hits = new RaycastHit[MaxHits];
    private ProximityAssist _assist;
    private DeskSlides _slides;
    private Camera _camera;
    private DeskController _papers;
    private Func<bool> _paused;
    private string _under;
    private bool _wasDragging;
    private int _mask;

    /// <summary>The desk being played (null when none is bound): MachineSlideIn.Attach binds to it.</summary>
    public static DeskSlideIn Current => _current;

    /// <summary>Raised for every cue of the slide-in as it happens (its slider and sync point; render/slides.json cues): the sound is played here, the lamp's pulse and the shadow's press follow it.</summary>
    public static event Action<SlideCue> Cued;

    /// <summary>The slide-in's data (the bound plan's DeskLayoutSettings.slides).</summary>
    public DeskSlides Slides => _slides;

    /// <summary>
    /// Binds the slide-in to the office <paramref name="camera"/> with the
    /// plan's <paramref name="slides"/>, the desk's <paramref name="papers"/>
    /// (a paper let go on a machine keeps it out while it lands) and when
    /// nothing is near (<paramref name="paused"/>: the PC open); every slider
    /// attached before is let go (the binder attaches today's again).
    /// </summary>
    public void Bind(Camera camera, DeskSlides slides, DeskController papers, Func<bool> paused)
    {
        foreach (MachineSlideIn slider in _sliders)
            if (slider != null)
                slider.Pose = default;
        _sliders.Clear();
        _droppedAt.Clear();
        _camera = camera;
        _slides = slides;
        _papers = papers;
        _paused = paused;
        _assist = slides != null ? new ProximityAssist(slides) : null;
        _mask = LayerMask.GetMask(OfficeLayers.Interactable);
        _current = this;
    }

    /// <summary>Adds an attached slider (MachineSlideIn.Attach), at home.</summary>
    public void Add(MachineSlideIn slider)
    {
        if (slider == null || _assist == null)
            return;
        if (!_sliders.Contains(slider))
            _sliders.Add(slider);
        _assist.Home(slider.Id);
    }

    /// <summary>The gain on the home <paramref name="homeId"/>'s own lamp now (FEEDBACK draws the lamp): DeskSlides.lampPulse for 100 ms after its slider's peak when DeskLayout.Lamps lights it, else 1.</summary>
    public float LampGain(string homeId)
    {
        if (_assist == null || string.IsNullOrEmpty(homeId))
            return 1f;
        return _assist.Pose(homeId, DeskLayout.Lamps.Lit == homeId).Lamp;
    }

    /// <summary>Every slider home at once (a forced retract: a load, a new day); a docked paper rides home with its machine (MachineSlideIn.Rider).</summary>
    public void ForceHome()
    {
        _assist?.Home(null);
        foreach (MachineSlideIn slider in _sliders)
            if (slider != null)
                slider.Pose = default;
    }

    private void OnEnable()
    {
        if (_assist != null)
            _current = this;
    }

    private void OnDisable()
    {
        ForceHome();
        if (_current == this)
            _current = null;
    }

    /// <summary>Finds the sliders under the cursor, steps the assist, sounds its cues and poses every slider (each puts its pose on in its LateUpdate).</summary>
    private void Update()
    {
        if (_assist == null || _camera == null || !DeskLayout.Bound)
            return;
        float now = UiMotion.Now;
        Find(now);
        _assist.Step(now, _candidates, _busy, UiMotion.Amount.Still);
        foreach (SlideCue cue in _assist.Cues)
        {
            Sound(cue);
            Cued?.Invoke(cue);
        }
        string lit = DeskLayout.Lamps.Lit;
        foreach (MachineSlideIn slider in _sliders)
            if (slider != null)
                slider.Pose = _assist.Pose(slider.Id, lit != null && lit == slider.Slider.Home);
    }

    /// <summary>
    /// This frame's candidates: every present slider not in the player's hand,
    /// with its silhouette's rank under the cursor (the office camera's ray
    /// through the Interactable click boxes, nearest first), whether the
    /// cursor's point on the desk lies in its home's footprint and its px to
    /// its silhouette at the reference height; the busy ones; a slider that
    /// left the desk goes home, one taken in hand gives way.
    /// </summary>
    private void Find(float now)
    {
        _candidates.Clear();
        _busy.Clear();
        bool dragging = _papers != null && _papers.IsDragging;
        if (_wasDragging && !dragging && _under != null)
            _droppedAt[_under] = now;
        _wasDragging = dragging;
        _under = null;

        Pointer pointer = Pointer.current;
        bool near = pointer != null && (_paused == null || !_paused());
        Vector2 at = near ? pointer.position.ReadValue() : Vector2.zero;
        Ray ray = _camera.ScreenPointToRay(at);
        int hits = near ? Physics.RaycastNonAlloc(ray, _hits, 50f, _mask, QueryTriggerInteraction.Collide) : 0;
        SortByDistance(hits);
        DeskFrame frame = DeskLayout.Frame;
        float enter = 0f;
        bool onDesk = near && new Plane(Vector3.up, frame.Origin).Raycast(ray, out enter);
        Vector2 deskPoint = onDesk ? frame.Local(ray.GetPoint(enter)) : Vector2.zero;
        float toReference = _slides.referenceHeight / Mathf.Max(1f, _camera.pixelHeight);

        foreach (MachineSlideIn slider in _sliders)
        {
            if (slider == null)
                continue;
            string id = slider.Id;
            if (!slider.Present)
            {
                if (_assist.Phase(id) != SlidePhase.Rest)
                    _assist.Home(id);
                continue;
            }
            if (slider.Taken)
            {
                if (_taken.Add(id))
                    _assist.Release(id);
                continue;
            }
            _taken.Remove(id);
            if (slider.Busy || (_droppedAt.TryGetValue(id, out float dropped) && now - dropped < _slides.dropHoldSeconds))
                _busy.Add(id);
            if (!near)
                continue;
            int rank = Rank(slider, hits);
            DeskHome home = DeskLayout.HomeOf(slider.Slider.Home);
            bool onFootprint = onDesk && home != null && home.Footprint.Contains(deskPoint.x, deskPoint.y);
            _candidates.Add(new SlideCandidate(id, rank, onFootprint, slider.ScreenDistance(_camera, at) * toReference));
            if (rank >= 0 && (_under == null || rank == 0))
                _under = id;
        }
    }

    /// <summary>The rank of <paramref name="slider"/>'s silhouette under the cursor among the sliders hit (the front-most 0), or -1.</summary>
    private int Rank(MachineSlideIn slider, int hits)
    {
        int rank = 0;
        string last = null;
        for (int i = 0; i < hits; i++)
        {
            MachineSlideIn owner = OwnerOf(_hits[i].collider);
            if (owner == null || owner.Id == last)
                continue;
            if (owner == slider)
                return rank;
            last = owner.Id;
            rank++;
        }
        return -1;
    }

    /// <summary>The slider a click box belongs to, or null.</summary>
    private MachineSlideIn OwnerOf(Collider collider)
    {
        foreach (MachineSlideIn slider in _sliders)
            if (slider != null && slider.Owns(collider))
                return slider;
        return null;
    }

    /// <summary>Sorts the first <paramref name="count"/> hits nearest first (an insertion sort: a handful of boxes).</summary>
    private void SortByDistance(int count)
    {
        for (int i = 1; i < count; i++)
        {
            RaycastHit hit = _hits[i];
            int j = i - 1;
            while (j >= 0 && _hits[j].distance > hit.distance)
            {
                _hits[j + 1] = _hits[j];
                j--;
            }
            _hits[j + 1] = hit;
        }
    }

    /// <summary>Plays <paramref name="cue"/>'s sound (DeskSlides' ids); a machine (a track) without its clip yet plays the stand-in (DESK-HOMES: the paper slide for the shushes, the drawer's close for the seat), a lift only its own.</summary>
    private void Sound(SlideCue cue)
    {
        string id;
        string standIn = null;
        switch (cue.Kind)
        {
            case SlideCueKind.Unlatch:
                id = _slides.unlatchCue;
                break;
            case SlideCueKind.Slide:
                id = _slides.slideCue;
                standIn = SoundCues.PaperSlide;
                break;
            case SlideCueKind.Peak:
                id = _slides.stopCue;
                break;
            case SlideCueKind.Seat:
                id = _slides.seatCue;
                standIn = SoundCues.DrawerClose;
                break;
            default:
                id = _slides.backCue;
                standIn = SoundCues.PaperSlide;
                break;
        }
        if (Sounds.Play(id) || standIn == null)
            return;
        DeskSlider slider = _slides.Find(cue.Id);
        if (slider != null && slider.kind == SlideKind.Track)
            Sounds.Play(standIn);
    }

    /// <summary>Forgets the bound desk when play starts (the editor without a domain reload keeps statics).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _current = null;
        Cued = null;
    }
}
