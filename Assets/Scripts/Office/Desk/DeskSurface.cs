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

    /// <summary>The nearest point inside the clamp area, on the plane.</summary>
    public Vector3 Clamp(Vector3 world)
    {
        Vector3 local = transform.InverseTransformPoint(world);
        (float x, float z) = new DeskRect(0f, 0f, size.x, size.y).Clamp(local.x, local.z);
        return transform.TransformPoint(new Vector3(x, 0f, z));
    }

    /// <summary>The point at (u, v) across the landing area (0..1 each; (0, 0) is its near left corner), on the plane.</summary>
    public Vector3 PointAt(Vector2 uv)
    {
        (float x, float z) = new DeskRect(spawnCentre.x, spawnCentre.y, spawnSize.x, spawnSize.y).PointAt(uv.x, uv.y);
        return transform.TransformPoint(new Vector3(x, 0f, z));
    }
}
