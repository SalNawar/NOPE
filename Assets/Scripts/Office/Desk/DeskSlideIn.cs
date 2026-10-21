using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The desk's slide-in (desk v3, Saleh 2026-10-20: "yes but they need to
/// slide"; D2: "the machine will slide in a little bit to help the player";
/// UNITY_PLAN section 5, DESK-HOMES): each frame it gathers every attached
/// slider (MachineSlideIn) as the office camera finds it (the front-most
/// silhouette among the Interactable click boxes, the cursor's point on the
/// desk inside a home's footprint, the px to each silhouette at 1080p,
/// whether it stands on the desk, is in the player's hand or busy) and
/// whether a paper is dragged; ProximityAssist decides everything (nearest
/// wins, one at a time, the hand-off, the springs, the linger and the way
/// home, busy machines left as they are, the drop hold, Reduced Motion as a
/// cut); it gives each slider its pose and sounds the cues: the unlatch tick,
/// the felt shush, the knock at the peak, the reversed shush, the seat's clack
/// (DeskSlides' sound ids; a machine without its clip yet plays the paper
/// slide and the drawer's close as stand-ins). At closing time
/// (<c>closed</c>) every slider is retracted at once, a busy one with its
/// docked paper riding it (the forced retract). While the PC is open nothing
/// is near. The office binder adds and binds it once the desk is laid
/// (OfficeSceneBinder.BindSlideIn) and attaches today's sliders; owners attach
/// theirs (MachineSlideIn.Attach). Unbound or disabled, every slider is home.
/// A frame allocates nothing.
/// </summary>
public sealed class DeskSlideIn : MonoBehaviour
{
    private const int MaxHits = 16;

    private static DeskSlideIn _current;

    private readonly List<MachineSlideIn> _sliders = new List<MachineSlideIn>();
    private readonly List<SlideCandidate> _candidates = new List<SlideCandidate>();
    private readonly RaycastHit[] _hits = new RaycastHit[MaxHits];
    private ProximityAssist _assist;
    private DeskSlides _slides;
    private IReadOnlyList<DeskHome> _homes;
    private Camera _camera;
    private DeskController _papers;
    private Func<bool> _paused, _closed;
    private bool _wasClosed;
    private int _mask;

    /// <summary>The desk being played (null when none is bound): MachineSlideIn.Attach binds to it.</summary>
    public static DeskSlideIn Current => _current;

    /// <summary>The slide-in's data (the bound plan's DeskLayoutSettings.slides).</summary>
    public DeskSlides Slides => _slides;

    /// <summary>The assist deciding the slides (FramingFocus follows its pick).</summary>
    public ProximityAssist Assist => _assist;

    /// <summary>The material of the sliders' contact shadows (the papers' Paper_Shadow; null: no shadows).</summary>
    public Material ShadowMaterial { get; private set; }

    /// <summary>
    /// Binds the slide-in to the office <paramref name="camera"/> with the
    /// plan's <paramref name="slides"/> on its <paramref name="homes"/>, the
    /// desk's <paramref name="papers"/> (a paper let go on a machine keeps it
    /// out while it lands), when nothing is near (<paramref name="paused"/>:
    /// the PC open), when the desk closes (<paramref name="closed"/>: the
    /// forced retract) and the contact shadows' <paramref name="shadow"/>
    /// material; every slider attached before is let go (the binder attaches
    /// today's again).
    /// </summary>
    public void Bind(Camera camera, DeskSlides slides, IReadOnlyList<DeskHome> homes, DeskController papers, Func<bool> paused, Func<bool> closed, Material shadow)
    {
        foreach (MachineSlideIn slider in _sliders)
            if (slider != null)
                slider.Pose = default;
        _sliders.Clear();
        _camera = camera;
        _slides = slides;
        _homes = homes;
        _papers = papers;
        _paused = paused;
        _closed = closed;
        _wasClosed = false;
        ShadowMaterial = shadow;
        _assist = slides != null ? new ProximityAssist(slides, homes) : null;
        _mask = LayerMask.GetMask(OfficeLayers.Interactable);
        _current = this;
    }

    /// <summary>Adds an attached slider (MachineSlideIn.Attach), at home.</summary>
    public void Add(MachineSlideIn slider)
    {
        if (slider != null && _assist != null && !_sliders.Contains(slider))
            _sliders.Add(slider);
    }

    /// <summary>The attached slider <paramref name="id"/>, or null (FramingFocus aims at it).</summary>
    public MachineSlideIn Find(string id)
    {
        for (int i = 0; i < _sliders.Count; i++)
            if (_sliders[i] != null && _sliders[i].Id == id)
                return _sliders[i];
        return null;
    }

    private void OnEnable()
    {
        if (_assist != null)
            _current = this;
    }

    /// <summary>Off: every slider home at once (a new assist, nothing out).</summary>
    private void OnDisable()
    {
        if (_slides != null)
            _assist = new ProximityAssist(_slides, _homes);
        foreach (MachineSlideIn slider in _sliders)
            if (slider != null)
                slider.Pose = default;
        if (_current == this)
            _current = null;
    }

    /// <summary>Gathers the sliders, steps the assist, sounds its cues and poses every slider (each puts its pose on in its LateUpdate); the forced retract when the desk closes.</summary>
    private void Update()
    {
        if (_assist == null || _camera == null || !DeskLayout.Bound)
            return;
        bool closed = _closed != null && _closed();
        if (closed && !_wasClosed)
            Retire();
        _wasClosed = closed;
        Gather();
        _assist.Step(UiMotion.Now, _candidates, _papers != null && _papers.IsDragging, UiMotion.Amount.Still);
        IReadOnlyList<SlideCue> cues = _assist.Cues;
        for (int i = 0; i < cues.Count; i++)
            Sound(cues[i]);
        for (int i = 0; i < _sliders.Count; i++)
            if (_sliders[i] != null)
                _sliders[i].Pose = _assist.Pose(_sliders[i].Id);
    }

    /// <summary>The forced retract (closing time; DESK-HOMES P2): every slider out goes home at once, a busy one only when the paper docked in it can ride it (MachineSlideIn.TakeRider: one mover).</summary>
    private void Retire()
    {
        for (int i = 0; i < _sliders.Count; i++)
        {
            MachineSlideIn slider = _sliders[i];
            if (slider != null)
                _assist.Retire(slider.Id, slider.Busy && slider.TakeRider());
        }
    }

    /// <summary>
    /// This frame's facts, one per attached slider: whether it stands on the
    /// desk, is in the player's hand or busy, and, while the cursor is on the
    /// desk, its silhouette's rank under the cursor (the office camera's ray
    /// through the Interactable click boxes, nearest first), whether the
    /// cursor's point on the desk lies in its home's footprint and its px to
    /// its silhouette at the reference height.
    /// </summary>
    private void Gather()
    {
        _candidates.Clear();
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

        for (int i = 0; i < _sliders.Count; i++)
        {
            MachineSlideIn slider = _sliders[i];
            if (slider == null)
                continue;
            string id = slider.Id;
            if (!slider.Present)
            {
                _candidates.Add(SlideCandidate.Away(id, present: false));
                continue;
            }
            bool busy = slider.Busy, taken = slider.Taken;
            if (!near || taken)
            {
                _candidates.Add(SlideCandidate.Away(id, busy, taken));
                continue;
            }
            _candidates.Add(new SlideCandidate(id, Rank(slider, hits), onDesk && slider.RestsOn(deskPoint), slider.ScreenDistance(_camera, at) * toReference, busy));
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
        for (int i = 0; i < _sliders.Count; i++)
            if (_sliders[i] != null && _sliders[i].Owns(collider))
                return _sliders[i];
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
    private static void ResetStatics() => _current = null;
}
