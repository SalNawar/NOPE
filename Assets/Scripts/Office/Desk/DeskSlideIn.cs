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
/// whether it stands on the desk, is in the player's hand or busy, and
/// whether its docked paper can ride it), whether a paper is dragged and how
/// fast the cursor moves (px a second at 1080p: the drive-by guard);
/// ProximityAssist decides everything (nearest wins, one at a time, the
/// hand-off, the springs, the linger and the way home, busy machines left as
/// they are, the drop hold, the forced retract, Reduced Motion as a cut); it
/// gives each slider its pose and sounds the cues: the unlatch tick, the felt
/// shush, the knock at the peak, the reversed shush, the seat's clack
/// (DeskSlides.Sound: the sound list's ids; until the list delivers them,
/// each plays its stand-in through the house fallback, CueSounds: the bank's
/// clip, else its placeholder). When the shift is over (the day
/// orchestrator's OnDayCompleted: the last traveller finished, or the booth
/// shut with nobody at the desk; never at closing time itself, while the last
/// traveller may still be served) every slider goes home and nothing slides
/// any more, a busy one with its docked paper riding it (the forced retract).
/// While the PC is open nothing is near. The office binder adds and binds it
/// once the desk is laid (OfficeSceneBinder.BindSlideIn) and attaches today's
/// sliders; owners attach theirs (MachineSlideIn.Attach). Unbound or
/// disabled, every slider is home. A frame allocates nothing.
/// </summary>
public sealed class DeskSlideIn : MonoBehaviour
{
    private const int MaxHits = 16;

    /// <summary>How loud the stand-ins' placeholders play (slides.json: the shush about -18 dB, the knock about -22 dB, the tick very low; the pitches lower the tick and the knock further).</summary>
    private const float StandInVolume = 0.3f;

    private static DeskSlideIn _current;

    private readonly List<MachineSlideIn> _sliders = new List<MachineSlideIn>();
    private readonly List<SlideCandidate> _candidates = new List<SlideCandidate>();
    private readonly RaycastHit[] _hits = new RaycastHit[MaxHits];
    private readonly string[] _owners = new string[MaxHits];
    private ProximityAssist _assist;
    private DeskSlides _slides;
    private IReadOnlyList<DeskHome> _homes;
    private Camera _camera;
    private DeskController _papers;
    private DayOrchestrator _day;
    private AudioSource _sound;
    private Func<bool> _paused;
    private bool _shiftOver;
    private int _mask;

    /// <summary>The cursor's screen point and time last frame (the drive-by guard's speed), and whether there was one.</summary>
    private Vector2 _lastPointer;
    private float _lastPointerAt;
    private bool _hadPointer;

    /// <summary>How fast the cursor moved this frame (px a second at DeskSlides.referenceHeight; 0 without a pointer): ProximityAssist's drive-by guard.</summary>
    private float _cursorSpeed;

    /// <summary>The desk being played (null when none is bound): MachineSlideIn.Attach binds to it.</summary>
    public static DeskSlideIn Current => _current;

    /// <summary>The slide-in's data (the bound plan's DeskLayoutSettings.slides).</summary>
    public DeskSlides Slides => _slides;

    /// <summary>The material of the sliders' contact shadows (the papers' Paper_Shadow; null: no shadows).</summary>
    public Material ShadowMaterial { get; private set; }

    /// <summary>
    /// Binds the slide-in to the office <paramref name="camera"/> with the
    /// plan's <paramref name="slides"/> on its <paramref name="homes"/>, the
    /// desk's <paramref name="papers"/> (a paper let go on a machine keeps it
    /// out while it lands), when nothing is near (<paramref name="paused"/>:
    /// the PC open), the <paramref name="day"/> whose end retracts every
    /// slider (the forced retract) and the contact shadows'
    /// <paramref name="shadow"/> material; every slider attached before is
    /// let go (the binder attaches today's again).
    /// </summary>
    public void Bind(Camera camera, DeskSlides slides, IReadOnlyList<DeskHome> homes, DeskController papers, Func<bool> paused, DayOrchestrator day, Material shadow)
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
        Hear(day);
        _shiftOver = false;
        ShadowMaterial = shadow;
        _assist = slides != null ? new ProximityAssist(slides, homes) : null;
        _mask = LayerMask.GetMask(OfficeLayers.Interactable);
        MakeSound();
        _current = this;
    }

    /// <summary>Adds an attached slider (MachineSlideIn.Attach), at home.</summary>
    public void Add(MachineSlideIn slider)
    {
        if (slider != null && _assist != null && !_sliders.Contains(slider))
            _sliders.Add(slider);
    }

    /// <summary>Listens to <paramref name="day"/>'s end (the forced retract), and no longer to the day bound before.</summary>
    private void Hear(DayOrchestrator day)
    {
        if (_day == day)
            return;
        if (_day != null)
            _day.OnDayCompleted -= HandleDayCompleted;
        _day = day;
        if (_day != null)
            _day.OnDayCompleted += HandleDayCompleted;
    }

    /// <summary>The shift is over: every slider goes home and nothing slides any more (ProximityAssist.Close, from the next frame on).</summary>
    private void HandleDayCompleted()
    {
        _shiftOver = true;
        _assist?.Close();
    }

    /// <summary>The stand-ins' voice (CueSounds plays a placeholder through it): a child of its own on the desk's sound bus, made once.</summary>
    private void MakeSound()
    {
        if (_sound != null)
            return;
        Transform voice = transform.Find("SlideInSound");
        if (voice == null)
        {
            voice = new GameObject("SlideInSound").transform;
            voice.SetParent(transform, false);
        }
        if (!voice.TryGetComponent(out _sound))
            _sound = voice.gameObject.AddComponent<AudioSource>();
        _sound.playOnAwake = false;
        _sound.spatialBlend = 0f;
        _sound.volume = StandInVolume;
        SoundBankSO bank = Sounds.Bank;
        if (bank != null)
            _sound.outputAudioMixerGroup = bank.Group(SoundBus.Desk);
    }

    private void OnEnable()
    {
        if (_assist != null)
            _current = this;
    }

    /// <summary>Off: every slider home at once (a new assist, nothing out; still closed once the shift is over).</summary>
    private void OnDisable()
    {
        if (_slides != null)
        {
            _assist = new ProximityAssist(_slides, _homes);
            if (_shiftOver)
                _assist.Close();
        }
        foreach (MachineSlideIn slider in _sliders)
            if (slider != null)
                slider.Pose = default;
        if (_current == this)
            _current = null;
    }

    private void OnDestroy() => Hear(null);

    /// <summary>Gathers the sliders, steps the assist, sounds its cues and poses every slider (each puts its pose on in its LateUpdate).</summary>
    private void Update()
    {
        if (_assist == null || _camera == null || !DeskLayout.Bound)
            return;
        Gather();
        _assist.Step(UiMotion.Now, _candidates, _papers != null && _papers.IsDragging, UiMotion.Amount.Still, _cursorSpeed);
        IReadOnlyList<SlideCue> cues = _assist.Cues;
        for (int i = 0; i < cues.Count; i++)
            Sound(cues[i]);
        for (int i = 0; i < _sliders.Count; i++)
            if (_sliders[i] != null)
                _sliders[i].Pose = _assist.Pose(_sliders[i].Id);
    }

    /// <summary>
    /// This frame's facts, one per attached slider (SlideCandidate.Found):
    /// whether it stands on the desk, is in the player's hand or busy, whether
    /// its docked paper rides it once the shift is over (MachineSlideIn.Rides),
    /// and, while the cursor is on the desk, its silhouette's rank under the
    /// cursor (the office camera's ray through the Interactable click boxes,
    /// nearest first: ProximityAssist.Rank), whether the cursor's point on the
    /// desk lies in its home's footprint and its px to its silhouette at the
    /// reference height (DeskSlides.ToReference).
    /// </summary>
    private void Gather()
    {
        _candidates.Clear();
        Pointer pointer = Pointer.current;
        bool near = pointer != null && (_paused == null || !_paused());
        Vector2 at = near ? pointer.position.ReadValue() : Vector2.zero;
        float now = UiMotion.Now, dt = now - _lastPointerAt;
        _cursorSpeed = near && _hadPointer && dt > 1e-4f ? _slides.ToReference((at - _lastPointer).magnitude, _camera.pixelHeight) / dt : 0f;
        _lastPointer = at;
        _lastPointerAt = now;
        _hadPointer = near;
        Ray ray = _camera.ScreenPointToRay(at);
        int hits = near ? Physics.RaycastNonAlloc(ray, _hits, 50f, _mask, QueryTriggerInteraction.Collide) : 0;
        SortByDistance(hits);
        for (int i = 0; i < hits; i++)
            _owners[i] = OwnerOf(_hits[i].collider);
        DeskFrame frame = DeskLayout.Frame;
        float enter = 0f;
        bool onDesk = near && new Plane(Vector3.up, frame.Origin).Raycast(ray, out enter);
        Vector2 deskPoint = onDesk ? frame.Local(ray.GetPoint(enter)) : Vector2.zero;

        for (int i = 0; i < _sliders.Count; i++)
        {
            MachineSlideIn slider = _sliders[i];
            if (slider == null)
                continue;
            bool present = slider.Present, busy = present && slider.Busy, taken = present && slider.Taken;
            bool rides = _shiftOver && busy && slider.Rides();
            bool looks = near && present && !taken;
            _candidates.Add(SlideCandidate.Found(slider.Id, present, busy, taken, rides, near,
                                                 looks ? ProximityAssist.Rank(_owners, hits, slider.Id) : -1,
                                                 looks && onDesk && slider.RestsOn(deskPoint),
                                                 looks ? _slides.ToReference(slider.ScreenDistance(_camera, at), _camera.pixelHeight) : float.PositiveInfinity));
        }
    }

    /// <summary>The id of the slider a click box belongs to, or null.</summary>
    private string OwnerOf(Collider collider)
    {
        for (int i = 0; i < _sliders.Count; i++)
            if (_sliders[i] != null && _sliders[i].Owns(collider))
                return _sliders[i].Id;
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

    /// <summary>Plays <paramref name="cue"/>'s sound (DeskSlides.Sound): its clip in the bank, else its stand-in through the house fallback (CueSounds: the stand-in's bank clip, else its placeholder), for every slider.</summary>
    private void Sound(SlideCue cue)
    {
        (string id, string standIn, float pitch) = _slides.Sound(cue.Kind);
        if (!Sounds.Play(id))
            CueSounds.Play(standIn, _sound, pitch);
    }

    /// <summary>Forgets the bound desk when play starts (the editor without a domain reload keeps statics).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _current = null;
}
