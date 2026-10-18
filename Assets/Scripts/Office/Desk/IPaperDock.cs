using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A desk machine a paper can be fed to (Saleh's 1018a playtest: "when you
/// move a document to a machine it should move to the correct elevation and
/// orintation - if is the wrong document it shouldnt go through the
/// machience"): the scanner, the chip reader, any later machine. It says
/// which machine it is (MachineIntake decides what it takes), where a drop
/// counts as on it, its casing (a paper's way never passes through it:
/// DockPath), where and how a paper lies docked in it (its bed, its pad, its
/// slot at the slot's height and angle) and its "no" when it refuses one.
/// What the machine then does with the paper is its own (the scan, the read).
/// </summary>
public interface IPaperDock
{
    /// <summary>Which machine it is.</summary>
    DeskMachine Machine { get; }

    /// <summary>True while it is on the desk and can take a paper.</summary>
    bool OnDesk { get; }

    /// <summary>True when a paper released at <paramref name="world"/> drops on it.</summary>
    bool Contains(Vector3 world);

    /// <summary>Its casing as a box standing on the desk (world metres).</summary>
    MachineBox Casing { get; }

    /// <summary>Where a paper's bottom lies docked at <paramref name="dock"/>: the bed's or the pad's top centre, or the slot's mouth (world).</summary>
    Vector3 DockPoint(MachineDock dock);

    /// <summary>How a paper lies docked at <paramref name="dock"/> (a level rotation: its top along the way in, square to the bed, the pad or the slot).</summary>
    Quaternion DockFacing(MachineDock dock);

    /// <summary>The slot's way in (a level unit vector, world) and how deep it is (metres); a machine with no slot answers its forward and 0.</summary>
    (Vector3 way, float depth) Slot { get; }

    /// <summary>The machine's "no" as it refuses a paper (a buzz, a red lamp, dashes on its readout).</summary>
    void Refuse();
}

/// <summary>The casings of desk machines measured from their renderers (DeskScanner, ChipReader).</summary>
public static class MachineCasing
{
    /// <summary>
    /// The box around <paramref name="renderers"/> (every corner of each
    /// one's bounds) in <paramref name="frame"/>'s level axes (its width along
    /// its right, its depth along its forward), standing on the desk at the
    /// frame's height up to their highest point, at least
    /// <paramref name="leastTop"/> metres over the frame; an empty box at the
    /// frame without renderers.
    /// </summary>
    public static MachineBox Measure(Transform frame, IEnumerable<Renderer> renderers, float leastTop = 0f)
    {
        Vector3 right = Vector3.ProjectOnPlane(frame.right, Vector3.up);
        right = right.sqrMagnitude > 1e-8f ? right.normalized : Vector3.right;
        Vector3 forward = Vector3.Cross(right, Vector3.up);
        Vector3 origin = frame.position;
        float minA = float.MaxValue, maxA = float.MinValue, minB = float.MaxValue, maxB = float.MinValue, top = origin.y + leastTop;
        bool any = false;
        foreach (Renderer r in renderers)
        {
            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy)
                continue;
            Bounds b = r.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 c = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z) - origin;
                float a = Vector3.Dot(c, right), d = Vector3.Dot(c, forward);
                minA = Mathf.Min(minA, a);
                maxA = Mathf.Max(maxA, a);
                minB = Mathf.Min(minB, d);
                maxB = Mathf.Max(maxB, d);
                top = Mathf.Max(top, c.y + origin.y);
            }
            any = true;
        }
        if (!any)
            return new MachineBox(origin.x, origin.z, right.x, right.z, 0f, 0f, top);
        Vector3 centre = origin + right * ((minA + maxA) / 2f) + forward * ((minB + maxB) / 2f);
        return new MachineBox(centre.x, centre.z, right.x, right.z, (maxA - minA) / 2f, (maxB - minB) / 2f, top);
    }
}
