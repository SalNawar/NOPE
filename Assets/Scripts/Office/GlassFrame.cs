using UnityEngine;

/// <summary>
/// A PC glass as seen from the office camera: which two of the glass mesh's
/// local axes run to the viewer's right (U) and up (V), its size along them,
/// and the same in world space. The desktop's clone projects across U and V
/// (PcScreenClone), and the power knob's default spot is measured from it.
/// </summary>
public readonly struct GlassFrame
{
    /// <summary>Where the art study's CRT has its power knob, in half-widths right and half-heights up from the glass's centre (Anchor_PCPower overrides it).</summary>
    private static readonly Vector2 KnobFromGlass = new Vector2(1.44f, -0.8f);

    /// <summary>The local axis running to the viewer's right.</summary>
    public readonly Vector3 AxisU;

    /// <summary>The local axis running up.</summary>
    public readonly Vector3 AxisV;

    /// <summary>The glass's local centre.</summary>
    public readonly Vector3 LocalCentre;

    /// <summary>The glass's size along U and V, in local units.</summary>
    public readonly Vector2 Size;

    /// <summary>The glass's centre in world space.</summary>
    public readonly Vector3 Centre;

    /// <summary>Half the glass's width, as a world vector pointing to the viewer's right.</summary>
    public readonly Vector3 Right;

    /// <summary>Half the glass's height, as a world vector pointing up.</summary>
    public readonly Vector3 Up;

    private GlassFrame(Vector3 axisU, Vector3 axisV, Vector3 localCentre, Vector2 size, Transform glass)
    {
        AxisU = axisU;
        AxisV = axisV;
        LocalCentre = localCentre;
        Size = size;
        Centre = glass.TransformPoint(localCentre);
        Right = glass.TransformVector(axisU * (size.x / 2f));
        Up = glass.TransformVector(axisV * (size.y / 2f));
    }

    /// <summary>Where the power knob is by default (the art study's CRT).</summary>
    public Vector3 PowerKnob => Centre + Right * KnobFromGlass.x + Up * KnobFromGlass.y;

    /// <summary>
    /// Measures a glass from its local bounds: the thinnest local axis is its
    /// normal (turned toward <paramref name="viewer"/>); of the other two, the
    /// one nearer the world's up is V, the other U, each turned to read upright
    /// from the viewer.
    /// </summary>
    public static GlassFrame Measure(Transform glass, Bounds local, Vector3 viewer)
    {
        Vector3 size = local.size;
        int normalAxis = size.x <= size.y && size.x <= size.z ? 0 : size.y <= size.z ? 1 : 2;
        Vector3 a = Axis((normalAxis + 1) % 3);
        Vector3 b = Axis((normalAxis + 2) % 3);
        Vector3 normal = Axis(normalAxis);
        if (Vector3.Dot(glass.TransformDirection(normal), viewer - glass.TransformPoint(local.center)) < 0f)
            normal = -normal;

        bool aIsUp = Mathf.Abs(Vector3.Dot(glass.TransformDirection(a).normalized, Vector3.up)) >= Mathf.Abs(Vector3.Dot(glass.TransformDirection(b).normalized, Vector3.up));
        Vector3 u = aIsUp ? b : a;
        Vector3 v = aIsUp ? a : b;
        Vector3 viewerRight = Vector3.Cross(Vector3.up, -glass.TransformDirection(normal));
        if (Vector3.Dot(glass.TransformDirection(u), viewerRight) < 0f)
            u = -u;
        if (Vector3.Dot(glass.TransformDirection(v), Vector3.up) < 0f)
            v = -v;

        return new GlassFrame(u, v, local.center, new Vector2(Mathf.Abs(Vector3.Dot(size, u)), Mathf.Abs(Vector3.Dot(size, v))), glass);
    }

    private static Vector3 Axis(int i) => i == 0 ? Vector3.right : i == 1 ? Vector3.up : Vector3.forward;
}
