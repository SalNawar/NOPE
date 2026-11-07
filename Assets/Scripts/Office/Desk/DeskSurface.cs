using UnityEngine;

/// <summary>
/// The desk top papers lie on: a horizontal plane through this transform (its
/// up is the plane's normal), with two rectangles in its local XZ: the area
/// paper centres stay in (the desk anchor and the scanner's drop area
/// together, so a paper can be dragged onto the scanner) and the area papers
/// land in when handed over (the desk anchor). The pointer is projected onto
/// the plane by a camera ray, so a perspective office camera works. The office
/// binder sizes both from the art office's anchors.
/// </summary>
public sealed class DeskSurface : MonoBehaviour
{
    /// <summary>The rectangle paper centres stay in (local XZ), centred on the transform.</summary>
    [SerializeField] private Vector2 size = new Vector2(1.6f, 1f);

    /// <summary>The landing area's centre (local XZ).</summary>
    [SerializeField] private Vector2 spawnCentre;

    /// <summary>The landing area's size (local XZ).</summary>
    [SerializeField] private Vector2 spawnSize = new Vector2(1.6f, 1f);

    /// <summary>Sets both rectangles (local XZ; the clamp area is centred on the transform).</summary>
    public void Configure(Vector2 clampSize, Vector2 landingCentre, Vector2 landingSize)
    {
        size = clampSize;
        spawnCentre = landingCentre;
        spawnSize = landingSize;
    }

    /// <summary>Where the camera ray through a screen point meets the desk plane; false when the ray is parallel to it or the plane is behind the camera.</summary>
    public bool TryProject(Camera cam, Vector2 screenPoint, out Vector3 world)
    {
        world = Vector3.zero;
        if (cam == null)
            return false;

        Ray ray = cam.ScreenPointToRay(screenPoint);
        var plane = new Plane(transform.up, transform.position);
        if (!plane.Raycast(ray, out float enter))
            return false;

        world = ray.GetPoint(enter);
        return true;
    }

    /// <summary>The clamp area (local XZ, centred on the transform).</summary>
    public DeskRect Area => new DeskRect(0f, 0f, size.x, size.y);

    /// <summary>
    /// The clamp area grown just enough to hold <paramref name="world"/> too
    /// (local XZ; the point taken on the plane): a drag's area for the point
    /// it holds, grown to hold the point its press met. A paper may rest with
    /// a part past the clamp area (its rest area is the clamp area grown by
    /// its footprint), and a press on that part must not move it (FOLD-01
    /// round 4, the verifier's P1).
    /// </summary>
    public DeskRect AreaHolding(Vector3 world)
    {
        Vector3 local = transform.InverseTransformPoint(world);
        return DeskRect.Union(Area, new DeskRect(local.x, local.z, 0f, 0f));
    }

    /// <summary>The nearest point inside the clamp area, on the plane.</summary>
    public Vector3 Clamp(Vector3 world) => Clamp(world, Area);

    /// <summary>The nearest point inside <paramref name="area"/> (local XZ: the clamp area, or a drag's area from AreaHolding), on the plane.</summary>
    public Vector3 Clamp(Vector3 world, DeskRect area)
    {
        Vector3 local = transform.InverseTransformPoint(world);
        (float x, float z) = area.Clamp(local.x, local.z);
        return transform.TransformPoint(new Vector3(x, 0f, z));
    }

    /// <summary>The clamp area's four corners on the plane, in world space (the frame ScannerClearance keeps a moved paper inside).</summary>
    public Vector3[] Corners()
    {
        Vector2 half = size / 2f;
        return new[]
        {
            transform.TransformPoint(new Vector3(-half.x, 0f, -half.y)), transform.TransformPoint(new Vector3(half.x, 0f, -half.y)),
            transform.TransformPoint(new Vector3(-half.x, 0f, half.y)), transform.TransformPoint(new Vector3(half.x, 0f, half.y)),
        };
    }

    /// <summary>The point at (u, v) across the landing area (0..1 each; (0, 0) is its near left corner), on the plane.</summary>
    public Vector3 PointAt(Vector2 uv)
    {
        (float x, float z) = new DeskRect(spawnCentre.x, spawnCentre.y, spawnSize.x, spawnSize.y).PointAt(uv.x, uv.y);
        return transform.TransformPoint(new Vector3(x, 0f, z));
    }
}
