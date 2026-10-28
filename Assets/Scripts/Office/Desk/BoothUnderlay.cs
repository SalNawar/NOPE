using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The painted booth's two closing surfaces (the night pops, 2026-10-28; the
/// director's review, "close the void behind the counter" and "the 5:4 frame
/// shows a grey grid floor"), laid by the office binder under the painted desk:
/// <list type="bullet">
/// <item>The counter's floor: a plate behind the counter that lies just under
/// the office eye's sight line over the bars' foot (the counter's back edge),
/// from the bars to the hall's painted canvas. The office view never sees it
/// (the counter hides all below that line); every pose above that eye (the
/// reading toggle's path and the reading view) sees it where it looked past the
/// canvas's lower edge into the hall's floor extension (the grey grid: the
/// AnimeHall's "Hall Foreground Floor", NOPE/Hall Foreground Floor) and the
/// office camera's clear colour (the black void).</item>
/// <item>The desk's front apron: a panel under the desk's front edge, down past
/// the frame's bottom, which a taller frame (16:10, 5:4) shows below the desk,
/// where it showed the same floor extension.</item>
/// <item>The desk's well floor (Saleh 2026-10-28: "things have these black
/// visual glitches as the camera move"): a flat plate under the whole desk
/// top. The shell's well is wider than the mat (gaps of 1 to 7 cm at the mat's
/// far edge and both sides), and the desk top has holes behind the PC and in
/// the deal tray's paper slot. A view from above looked down through them to
/// the clear colour: the black band above the slot and the strips beside the
/// mat in the reading view, thin lines along the mat in the office view. The
/// plate lies under the desk top's lowest surface, so no desk part is ever
/// behind it; it shows only through those gaps, in their shadow tone.</item>
/// </list>
/// All are painted flat in the booth's tones (NOPE/Desk Painted, so they take
/// the hour's light as the desk does), cast no shadow and take no clicks.
/// </summary>
public sealed class BoothUnderlay : MonoBehaviour
{
    /// <summary>The office eye the counter's floor hides from (the art office camera's position; HallForegroundFloor.referenceCameraPosition).</summary>
    public static readonly Vector3 OfficeEye = new Vector3(0f, 2.16f, -2.62f);

    /// <summary>How far under the eye's sight line over the bars' foot the counter's floor lies (m): the camera's breathing never shows it at rest.</summary>
    public const float Margin = 0.03f;

    /// <summary>Where the counter's floor ends (world z, m): just before the hall's painted canvas (the AnimeHall's registered layers, about z 17.0 at the sight line's height), so it never draws over the canvas past it.</summary>
    public const float FloorEnd = 16.9f;

    /// <summary>The desk's front apron (world, m): its plane just behind the desk's front edge face (z about -1.31), its top behind that face, its bottom under the lowest ray of a 5:4 office frame (y 0.49).</summary>
    public const float ApronZ = -1.27f, ApronTop = 1.02f, ApronBottom = -0.6f, ApronHalfWidth = 3f;

    /// <summary>The hall floor's tone and the counter's shade at its foot (sRGB): the up-facing plate shows about 1.5 times brighter under the hour's light, so the far end lands on the painted floor's (85, 71, 53) just above the counter (the office still, day 1).</summary>
    private static readonly Color32 FloorFar = new Color32(56, 48, 38, 255), FloorNear = new Color32(45, 37, 29, 255);

    /// <summary>The desk front's painted tone under its lip (sRGB; the 5:4 office still) and its shade at the floor.</summary>
    private static readonly Color32 ApronTopColour = new Color32(49, 28, 12, 255), ApronBottomColour = new Color32(20, 12, 6, 255);

    /// <summary>The gaps' shadow tone (sRGB): the desk's ink-line brown; the up-facing plate shows about 1.5 times brighter under the hour's light.</summary>
    private static readonly Color32 WellShadow = new Color32(24, 15, 9, 255);

    private const string PaintedShader = "NOPE/Desk Painted";

    /// <summary>Lays the surfaces under <paramref name="desk"/> (once; again replaces them). <paramref name="bars"/> is the booth's bars (their bounds give the bars' foot); without them the counter's floor is not laid.</summary>
    public static BoothUnderlay Install(Transform desk, Renderer bars)
    {
        if (desk == null)
            return null;
        Transform old = desk.Find("BoothUnderlay");
        if (old != null)
            Destroy(old.gameObject);
        var root = new GameObject("BoothUnderlay");
        root.transform.SetParent(desk, false);
        var underlay = root.AddComponent<BoothUnderlay>();
        Shader shader = Shader.Find(PaintedShader);
        if (shader == null)
            return underlay;
        if (bars != null)
        {
            Bounds b = bars.bounds;
            var foot = new Vector3(0f, b.min.y, b.min.z);
            float slope = (foot.y - OfficeEye.y) / (foot.z - OfficeEye.z);
            float nearY = foot.y - Margin;
            float farY = nearY + slope * (FloorEnd - foot.z);
            var floor = new[]
            {
                new Vector3(-8f, nearY, foot.z), new Vector3(8f, nearY, foot.z),
                new Vector3(-60f, farY, FloorEnd), new Vector3(60f, farY, FloorEnd),
            };
            Surface(root.transform, "Counter floor", floor, Gradient(FloorNear, FloorFar), shader);
        }
        var apron = new[]
        {
            new Vector3(-ApronHalfWidth, ApronBottom, ApronZ), new Vector3(ApronHalfWidth, ApronBottom, ApronZ),
            new Vector3(-ApronHalfWidth, ApronTop, ApronZ), new Vector3(ApronHalfWidth, ApronTop, ApronZ),
        };
        Surface(root.transform, "Desk front apron", apron, Gradient(ApronBottomColour, ApronTopColour), shader);
        // The desk's well floor: DeskPaintedMap's numbers (measured on the bake's meshes).
        float y = DeskPaintedMap.WellFloorY;
        for (int i = 0; i < DeskPaintedMap.WellFloor.Length; i++)
        {
            (float x0, float z0, float x1, float z1) = DeskPaintedMap.WellFloor[i];
            var plate = new[] { new Vector3(x0, y, z0), new Vector3(x1, y, z0), new Vector3(x0, y, z1), new Vector3(x1, y, z1) };
            Surface(root.transform, i == 0 ? "Desk well floor" : "Desk well floor (left back)", plate, Gradient(WellShadow, WellShadow), shader);
        }
        return underlay;
    }

    /// <summary>A quad at the world corners (near left, near right, far left, far right; v runs near to far), drawn both sides.</summary>
    private static void Surface(Transform parent, string name, Vector3[] world, Texture2D paint, Shader shader)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        go.transform.localScale = Vector3.one;
        Matrix4x4 toLocal = go.transform.worldToLocalMatrix;
        var mesh = new Mesh { name = name };
        var local = new Vector3[4];
        for (int i = 0; i < 4; i++)
            local[i] = toLocal.MultiplyPoint3x4(world[i]);
        mesh.vertices = local;
        mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
        mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        var material = new Material(shader) { name = name + " (runtime)" };
        material.SetTexture("_BaseMap", paint);
        material.SetFloat("_Cull", (float)CullMode.Off);
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    /// <summary>A 1x32 paint from <paramref name="from"/> (v 0) to <paramref name="to"/> (v 1), sRGB, clamped.</summary>
    private static Texture2D Gradient(Color32 from, Color32 to)
    {
        var texture = new Texture2D(1, 32, TextureFormat.RGBA32, false, false) { name = "BoothUnderlay paint", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var pixels = new Color32[32];
        for (int i = 0; i < 32; i++)
            pixels[i] = Color32.Lerp(from, to, i / 31f);
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }
}
