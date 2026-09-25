using UnityEngine;

/// <summary>
/// The desk top papers lie on: a rectangle of <see cref="size"/> centred on
/// this transform, in its plane (position and forward). The pointer is
/// projected onto the plane by a camera ray, so the orthographic booth and a
/// perspective 3D desk work alike, and paper centres are kept inside the rectangle.
/// </summary>
public sealed class DeskSurface : MonoBehaviour
{
    /// <summary>The rectangle paper centres stay in, in local XY, centred on the transform.</summary>
    [SerializeField] private Vector2 size;

    /// <summary>Where the camera ray through a screen point meets the desk plane; false when the ray is parallel to it or the plane is behind the camera.</summary>
    public bool TryProject(Camera cam, Vector2 screenPoint, out Vector3 world)
    {
        world = Vector3.zero;
        if (cam == null)
            return false;

        Ray ray = cam.ScreenPointToRay(screenPoint);
        var plane = new Plane(transform.forward, transform.position);
        if (!plane.Raycast(ray, out float enter))
            return false;

        world = ray.GetPoint(enter);
        return true;
    }

    /// <summary>The nearest point inside the rectangle, on the plane.</summary>
    public Vector3 Clamp(Vector3 world)
    {
        Vector3 local = transform.InverseTransformPoint(world);
        (float x, float y) = new DeskRect(0f, 0f, size.x, size.y).Clamp(local.x, local.y);
        return transform.TransformPoint(new Vector3(x, y, 0f));
    }

    /// <summary>The point at (u, v) across the rectangle (0..1 each; (0, 0) is the bottom left).</summary>
    public Vector3 PointAt(Vector2 uv)
    {
        (float x, float y) = new DeskRect(0f, 0f, size.x, size.y).PointAt(uv.x, uv.y);
        return transform.TransformPoint(new Vector3(x, y, 0f));
    }
}
