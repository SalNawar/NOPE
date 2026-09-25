using UnityEngine;

/// <summary>
/// The desk scanner: a paper released with the pointer inside its drop area
/// scans on its glass bed (DeskController, DeskPapers.Drop), and the scanner
/// pulses when a scan finishes.
/// </summary>
public sealed class DeskScanner : MonoBehaviour
{
    /// <summary>The drop area, in local units, centred on the transform.</summary>
    [SerializeField] private Vector2 dropSize;

    /// <summary>Where a scanning paper lies, in local units.</summary>
    [SerializeField] private Vector2 bedCentre;

    /// <summary>The scanner's click reaction, played when a scan finishes (optional).</summary>
    [SerializeField] private DeskReaction reaction;

    /// <summary>True when a world point lies inside the drop area.</summary>
    public bool Contains(Vector3 world)
    {
        Vector3 local = transform.InverseTransformPoint(world);
        return new DeskRect(0f, 0f, dropSize.x, dropSize.y).Contains(local.x, local.y);
    }

    /// <summary>The glass bed's centre in world space.</summary>
    public Vector3 BedPoint => transform.TransformPoint(bedCentre);

    /// <summary>Plays the scanner's reaction (a finished scan).</summary>
    public void Pulse()
    {
        if (reaction != null)
            reaction.Play();
    }
}
