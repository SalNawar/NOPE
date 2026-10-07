using UnityEngine;

/// <summary>
/// The desk scanner: a paper released with the pointer inside its drop area
/// (a rectangle in the transform's horizontal plane) scans on its bed
/// (DeskController, DeskPapers.Drop), and the scanner pulses when a scan
/// finishes. The office binder puts it on the art's scanner (or shows its
/// placeholder machine where the contract's default pose is) and sizes the
/// drop area and the bed. The placeholder machine shows a feeder tray while
/// the Auto-Feed Scanner is owned and a lamp while the Analysis Scanner is
/// (the PC redesign SC6), until the art adds them.
/// </summary>
public sealed class DeskScanner : MonoBehaviour
{
    /// <summary>The click box's height over the scanner's foot (metres): the binder sizes the box and the default bed sits halfway up it (audit R5-015).</summary>
    public const float BoxHeight = 0.12f;

    /// <summary>The stand-in flatbed's height over the desk (metres, its hinge's top): what its body hides behind it (ScannerClearance.Shadow) where the art has no scanner.</summary>
    public const float PlaceholderHeight = 0.075f;

    /// <summary>The drop area, in local XZ, centred on the transform.</summary>
    [SerializeField] private Vector2 dropSize = new Vector2(0.4f, 0.32f);

    /// <summary>Where a scanning paper lies, in local units (on the scanner's glass).</summary>
    [SerializeField] private Vector3 bedCentre = new Vector3(0f, BoxHeight / 2f, 0f);

    /// <summary>The scanner's click reaction, played when a scan finishes (optional).</summary>
    [SerializeField] private DeskReaction reaction;

    /// <summary>The placeholder machine's feeder tray, shown while the Auto-Feed Scanner is owned (optional: the art's scanner brings its own).</summary>
    [SerializeField] private GameObject feederTray;

    /// <summary>The placeholder machine's analysis lamp, shown while the Analysis Scanner is owned (optional).</summary>
    [SerializeField] private GameObject analysisLamp;

    /// <summary>The glowing bar that crosses the glass while a scan runs (drop and go, the scanner app spec §1; optional: the art's scanner may bring its own).</summary>
    [SerializeField] private Transform sweepBar;

    /// <summary>Where the bar runs, in local Z: from the glass's front edge (x) to its back edge (y).</summary>
    [SerializeField] private Vector2 sweepZ = new Vector2(-0.115f, 0.135f);

    /// <summary>The drop area's size in local XZ.</summary>
    public Vector2 DropSize => dropSize;

    /// <summary>The sweep at <paramref name="progress"/> of a running scan (0 to 1: the bar crosses the glass), or hidden (below 0: no scan).</summary>
    public void Sweep(float progress)
    {
        if (sweepBar == null)
            return;
        bool on = progress >= 0f;
        if (sweepBar.gameObject.activeSelf != on)
            sweepBar.gameObject.SetActive(on);
        if (!on)
            return;
        Vector3 p = sweepBar.localPosition;
        sweepBar.localPosition = new Vector3(p.x, p.y, Mathf.Lerp(sweepZ.x, sweepZ.y, Mathf.Clamp01(progress)));
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

    /// <summary>Shows the placeholder parts of the day's upgrades: the feeder tray with the Auto-Feed, the lamp with the Analysis (SC6).</summary>
    public void ShowUpgrades(ScannerDay day)
    {
        gameObject.SetActive(!day.Hidden);
        if (feederTray != null)
            feederTray.SetActive(day.AutoFeed);
        if (analysisLamp != null)
            analysisLamp.SetActive(day.Analysis);
    }

    /// <summary>Plays the scanner's reaction (a finished scan).</summary>
    public void Pulse()
    {
        if (reaction != null)
            reaction.Play();
    }
}
