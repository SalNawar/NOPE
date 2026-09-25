using UnityEngine;

/// <summary>
/// The desk scanner: a paper released with the pointer inside its drop area
/// (a rectangle in the transform's horizontal plane) scans on its bed
/// (DeskController, DeskPapers.Drop), and the scanner pulses when a scan
/// finishes. The office binder puts it on the art's scanner (or shows its
/// placeholder machine where the contract's default pose is) and sizes the
/// drop area and the bed.
/// </summary>
public sealed class DeskScanner : MonoBehaviour
{
    /// <summary>The drop area, in local XZ, centred on the transform.</summary>
    [SerializeField] private Vector2 dropSize = new Vector2(0.4f, 0.32f);

    /// <summary>Where a scanning paper lies, in local units (on the scanner's glass).</summary>
    [SerializeField] private Vector3 bedCentre = new Vector3(0f, 0.06f, 0f);

    /// <summary>The scanner's click reaction, played when a scan finishes (optional).</summary>
    [SerializeField] private DeskReaction reaction;

    /// <summary>The drop area's size in local XZ.</summary>
    public Vector2 DropSize => dropSize;

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

    /// <summary>The bed's centre in world space.</summary>
    public Vector3 BedPoint => transform.TransformPoint(bedCentre);

    /// <summary>Plays the scanner's reaction (a finished scan).</summary>
    public void Pulse()
    {
        if (reaction != null)
            reaction.Play();
    }
}
