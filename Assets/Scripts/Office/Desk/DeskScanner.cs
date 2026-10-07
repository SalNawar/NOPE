using TMPro;
using UnityEngine;

/// <summary>
/// The desk scanner: a paper released with the pointer inside its drop area
/// (a rectangle in the transform's horizontal plane) scans on its bed
/// (DeskController, DeskPapers.Drop), and the scanner pulses when a scan
/// finishes. The office binder puts it on the art's scanner (or shows its
/// own machine where the contract's default pose is) and sizes the drop area
/// and the bed. Its machine is the cream 80s scanner (Track BR, Saleh
/// 2026-10-08: "it auto opens when you drag a document near and makes a
/// xerox sound as it quickly scans, then opens again"; Build Office UI
/// installs the art when it is complete, else the built stand-in with the
/// feeder tray and lamp the Auto-Feed and Analysis Scanners show, SC6): its
/// hinged lid swings open on a spring while a dragged paper comes near
/// (ScannerLid), shuts as the paper drops and the scan runs (the bright bar
/// sweeping over the paper under the smoked glass, a cyan glow leaking from
/// the lid's edges, the green readout counting, the xerox sound), opens
/// again as the scan ends and shuts by itself once idle.
/// </summary>
public sealed class DeskScanner : MonoBehaviour
{
    /// <summary>The click box's height over the scanner's foot (metres): the binder sizes the box and the default bed sits halfway up it (audit R5-015).</summary>
    public const float BoxHeight = 0.12f;

    /// <summary>The drop area, in local XZ, centred on the transform.</summary>
    [SerializeField] private Vector2 dropSize = new Vector2(0.4f, 0.32f);

    /// <summary>Where a scanning paper lies, in local units (on the scanner's glass).</summary>
    [SerializeField] private Vector3 bedCentre = new Vector3(0f, BoxHeight / 2f, 0f);

    /// <summary>The machine's height over the desk with its lid shut (metres): what its body hides behind it (ScannerClearance.Shadow) where the art office has no scanner.</summary>
    [SerializeField] private float machineHeight = 0.075f;

    /// <summary>The scanner's click reaction, played when a scan finishes (optional).</summary>
    [SerializeField] private DeskReaction reaction;

    /// <summary>The stand-in machine's feeder tray, shown while the Auto-Feed Scanner is owned (optional: the cream scanner has none yet).</summary>
    [SerializeField] private GameObject feederTray;

    /// <summary>The stand-in machine's analysis lamp, shown while the Analysis Scanner is owned (optional: the cream scanner has none yet).</summary>
    [SerializeField] private GameObject analysisLamp;

    /// <summary>The glowing bar that crosses the glass while a scan runs (drop and go, the scanner app spec §1), over the scanning paper; hidden while idle.</summary>
    [SerializeField] private Transform sweepBar;

    /// <summary>Where the bar runs, in local Z: from the glass's front edge (x) to its back edge (y).</summary>
    [SerializeField] private Vector2 sweepZ = new Vector2(-0.115f, 0.135f);

    /// <summary>The hinged lid (optional: the stand-in has none): it turns about its local x on the hinge, open at MotionKnobs.scannerLidAngle.</summary>
    [SerializeField] private Transform lid;

    /// <summary>The cyan light leaking from the shut lid's edges while a scan runs (optional), and its renderers (their colour's alpha flickers with the bar).</summary>
    [SerializeField] private GameObject spill;

    /// <summary>The green readout's count (optional): 000 at rest, counting to 100 as a scan runs.</summary>
    [SerializeField] private TMP_Text readout;

    /// <summary>Plays the lid's and the scan's placeholder sounds (optional).</summary>
    [SerializeField] private AudioSource sound;

    /// <summary>The placeholder sounds (the scanner art's make_scanner_sfx.py) until the sound bank has SoundCues.ScannerLidOpen, ScannerLidClose and ScannerScan: a hinge's swing up, the lid's clack, the xerox scan.</summary>
    [SerializeField] private AudioClip lidOpenSound, lidCloseSound, scanSound;

    private Spring _lid;
    private Quaternion _lidRest = Quaternion.identity;
    private bool _lidOpen, _near, _scanning, _rested;
    private float _sinceScan = float.PositiveInfinity;
    private int _count = -1;
    private Renderer[] _spill = System.Array.Empty<Renderer>();
    private MaterialPropertyBlock _block;

    /// <summary>The drop area's size in local XZ.</summary>
    public Vector2 DropSize => dropSize;

    /// <summary>The machine's height over the desk with its lid shut (metres).</summary>
    public float MachineHeight => machineHeight;

    private void Awake() => Rest();

    private void Rest()
    {
        if (_rested)
            return;
        _rested = true;
        if (lid != null)
            _lidRest = lid.localRotation;
        if (spill != null)
        {
            _spill = spill.GetComponentsInChildren<Renderer>(true);
            spill.SetActive(false);
        }
        ShowCount(0);
    }

    /// <summary>The sweep at <paramref name="progress"/> of a running scan (0 to 1: the bar crosses the glass), or hidden (below 0: no scan). A scan's start shuts the lid and plays the xerox scan; its end opens the lid again.</summary>
    public void Sweep(float progress)
    {
        Rest();
        bool on = progress >= 0f;
        if (on != _scanning)
        {
            _scanning = on;
            if (on)
                PlayCue(SoundCues.ScannerScan, scanSound);
            else
                _sinceScan = 0f;
            if (spill != null)
                spill.SetActive(on);
        }
        if (on)
            ShowCount(ScannerLid.Count(progress));
        if (sweepBar == null)
            return;
        if (sweepBar.gameObject.activeSelf != on)
            sweepBar.gameObject.SetActive(on);
        if (!on)
            return;
        Vector3 p = sweepBar.localPosition;
        sweepBar.localPosition = new Vector3(p.x, p.y, Mathf.Lerp(sweepZ.x, sweepZ.y, Mathf.Clamp01(progress)));
    }

    /// <summary>A dragged paper's pointer is near the scanner (<paramref name="near"/>: DeskController while a paper is dragged; false when it is let go): the lid opens for it.</summary>
    public void SetNear(bool near) => _near = near;

    /// <summary>True when a world point lies within the drop area grown by MotionKnobs.scannerLidNear (the lid opens for a paper dragged there).</summary>
    public bool Near(Vector3 world)
    {
        Vector3 local = transform.InverseTransformPoint(world);
        return ScannerLid.Near(local.x, local.z, dropSize.x, dropSize.y, UiMotion.Knobs.scannerLidNear);
    }

    /// <summary>Swings the lid toward open or shut on its spring (a snap under Reduced Motion; a sound as it turns), lets the readout fall back to 000 once the lid has shut after a scan, and flickers the spill while a scan runs.</summary>
    private void Update()
    {
        MotionKnobs knobs = UiMotion.Knobs;
        float dt = UiMotion.Delta(Time.unscaledDeltaTime);
        if (!float.IsPositiveInfinity(_sinceScan))
            _sinceScan += dt;
        bool open = ScannerLid.Open(_scanning, _near, _sinceScan, knobs.scannerLidIdleSeconds);
        if (open != _lidOpen)
        {
            _lidOpen = open;
            _lid.Target = open ? 1f : 0f;
            if (lid != null)
                PlayCue(open ? SoundCues.ScannerLidOpen : SoundCues.ScannerLidClose, open ? lidOpenSound : lidCloseSound);
        }
        if (lid != null && !_lid.AtRest)
        {
            if (UiMotion.Amount.Still)
                _lid.Snap(_lid.Target);
            else
                _lid.Step(dt, knobs.Get(knobs.scannerLidFeel), 1e-3f, 0.02f);
            lid.localRotation = Quaternion.AngleAxis(knobs.scannerLidAngle * _lid.Value, Vector3.right) * _lidRest;
        }
        if (!_scanning && !_lidOpen && _count != 0)
            ShowCount(0);
        if (_scanning && _spill.Length > 0)
        {
            _block ??= new MaterialPropertyBlock();
            float glow = 0.75f + 0.25f * Mathf.Sin(UiMotion.Now * 40f);
            foreach (Renderer r in _spill)
            {
                r.GetPropertyBlock(_block);
                Color c = r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor") ? r.sharedMaterial.GetColor("_BaseColor") : Color.cyan;
                c.a *= glow;
                _block.SetColor("_BaseColor", c);
                r.SetPropertyBlock(_block);
            }
        }
    }

    private void ShowCount(int count)
    {
        if (_count == count)
            return;
        _count = count;
        if (readout != null)
            readout.SetText("{0:000}", count);
    }

    private void PlayCue(string cue, AudioClip placeholder)
    {
        if (Sounds.Play(cue) || sound == null || placeholder == null)
            return;
        sound.PlayOneShot(placeholder);
    }

    /// <summary>Sets the drop area (local XZ) and the bed (local).</summary>
    public void Configure(Vector2 drop, Vector3 bed)
    {
        dropSize = drop;
        bedCentre = bed;
    }

    /// <summary>True when a world point lies over the drop area.</summary>
    public bool Contains(Vector3 world)
    {
        Vector3 local = transform.InverseTransformPoint(world);
        return new DeskRect(0f, 0f, dropSize.x, dropSize.y).Contains(local.x, local.z);
    }

    /// <summary>The drop area's four corners in world space (the scanner's footprint on the desk: ScannerClearance keeps papers out of it).</summary>
    public Vector3[] Corners()
    {
        Vector2 half = dropSize / 2f;
        return new[]
        {
            transform.TransformPoint(new Vector3(-half.x, 0f, -half.y)), transform.TransformPoint(new Vector3(half.x, 0f, -half.y)),
            transform.TransformPoint(new Vector3(-half.x, 0f, half.y)), transform.TransformPoint(new Vector3(half.x, 0f, half.y)),
        };
    }

    /// <summary>The bed's centre in world space.</summary>
    public Vector3 BedPoint => transform.TransformPoint(bedCentre);

    /// <summary>Shows the stand-in's parts of the day's upgrades: the feeder tray with the Auto-Feed, the lamp with the Analysis (SC6).</summary>
    public void ShowUpgrades(ScannerDay day)
    {
        gameObject.SetActive(!day.Hidden);
        if (feederTray != null)
            feederTray.SetActive(day.AutoFeed);
        if (analysisLamp != null)
            analysisLamp.SetActive(day.Analysis);
    }

    /// <summary>Plays the scanner's reaction and its done beep (a finished scan).</summary>
    public void Pulse()
    {
        Sounds.Play(SoundCues.ScannerDone);
        if (reaction != null)
            reaction.Play();
    }
}
