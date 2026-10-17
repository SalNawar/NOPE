using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Lays a desk object where the office view shows a viewport point (the DETAIN
/// button, the desk machines and the upgrades' objects; the office binder,
/// once the desk is placed): on the desk's plane where the office camera,
/// drawn at the design aspect, sees that point, facing the chair along the
/// view's level forward (turned by a yaw), so it stands on the same spot of
/// the desk at every aspect.
/// </summary>
public static class DeskLay
{
    /// <summary>
    /// Lays <paramref name="target"/> on the desk's plane (<paramref name="deskTop"/>)
    /// where <paramref name="office"/>, drawn at <paramref name="aspect"/>,
    /// shows <paramref name="viewport"/>, facing the chair turned by
    /// <paramref name="yaw"/> degrees. False (it stays where it is) without
    /// a camera or when that point misses the plane.
    /// </summary>
    public static bool Lay(Transform target, CinemachineCamera office, float aspect, float deskTop, Vector2 viewport, float yaw = 0f)
    {
        if (target == null || office == null)
            return false;
        Transform view = office.transform;
        float tan = Mathf.Tan(office.Lens.FieldOfView * 0.5f * Mathf.Deg2Rad);
        Vector3 dir = view.rotation * new Vector3((viewport.x * 2f - 1f) * tan * aspect, (viewport.y * 2f - 1f) * tan, 1f);
        if (Mathf.Abs(dir.y) < 1e-5f)
            return false;
        float t = (deskTop - view.position.y) / dir.y;
        if (t <= 0f)
            return false;
        Vector3 level = Vector3.ProjectOnPlane(view.forward, Vector3.up);
        if (level.sqrMagnitude < 1e-6f)
            level = Vector3.forward;
        target.SetPositionAndRotation(view.position + dir * t, Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.LookRotation(level.normalized, Vector3.up));
        return true;
    }
}
