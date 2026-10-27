using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The painted PC's CRT (desk v3, round 12; the concept-vs-game review, item
/// 1, and Saleh's crops of the monitor: "the hall shows through" between the
/// casing and the bezel, and "the screen is a flat floating panel ... sized
/// like a tablet"). Desk_PC.fbx's casing is an open box whose front is a
/// 46 x 40 cm hole with the 44 x 37 cm bezel slab standing in it, so the
/// office camera saw through the 1 to 2 cm gap into the hollow casing and on
/// to the hall (real see-through, not a baked colour: the casing's insides are
/// back faces, culled); and the screen was a flat quad 2 mm in front of the
/// bezel, its picture fitted at 4:3 and 92% (a thick black border). Fit, at
/// bind and before the desktop's clone binds (OfficeSceneBinder.BindPaintedPc):
/// the screen's quad becomes a curved CRT face (CrtFace.Dome) set into the
/// bezel (its rim under the bezel's face), whose bounds are sized so the
/// clone's 4:3 picture fills the visible glass with a CRT's overscan
/// (CrtFace.PictureBounds; PcScreenClone is unchanged); a recess plate in the
/// casing's colour closes the casing's opening behind the bezel; and the glass
/// (NOPE/Desk CRT Glass, Resources/DeskCrtGlass) lays the phosphor's tint,
/// the scanlines, the tube's edge falloff and a soft sheen over the picture.
/// DESK-ART's look on DESK-HOMES' bind; the bake (DeskPaintedImporter) is
/// unchanged, so a re-import keeps working.
/// </summary>
public static class PaintedCrt
{
    /// <summary>A CRT's overscan: the picture runs this much past the visible glass on its tighter side.</summary>
    public const float Overscan = 0.02f;

    /// <summary>How far the glass layer stands in front of the picture (metres): no depth fight, no visible gap.</summary>
    private const float GlassLift = 0.0012f;

    /// <summary>The recess plate's colour: the painting's casing in the bezel's shadow (sampled around the painted bezel).</summary>
    private static readonly Color RecessColour = new Color(0.58f, 0.47f, 0.34f, 1f);

    /// <summary>The CRT glass's material (Resources: always in a build).</summary>
    private const string GlassMaterial = "DeskCrtGlass";

    /// <summary>
    /// Makes <paramref name="screen"/> (DeskPainted.Screen: the quad
    /// DeskPaintedImporter.LayScreen laid on the bezel) the CRT face, for a
    /// clone that fills <paramref name="fill"/> of its fitted picture
    /// (DeskConfigSO.cloneFill). Idempotent (a second call finds its parts).
    /// Returns the visible glass's share of the desktop's width and height
    /// (what a desk-view picture must keep its content inside).
    /// </summary>
    public static Vector2 Fit(Renderer screen, float fill)
    {
        CrtFace.Shape shape = CrtFace.Painted;
        (float shareX, float shareY) = CrtFace.VisibleShare(shape, fill, Overscan);
        if (screen == null || !screen.TryGetComponent(out MeshFilter filter))
            return new Vector2(shareX, shareY);
        Transform t = screen.transform;
        if (t.Find("CrtGlass") != null)
            return new Vector2(shareX, shareY);

        // The screen's own frame in metres (the quad was scaled to the blockout's 36 x 29 cm).
        Vector3 parent = t.parent != null ? t.parent.lossyScale : Vector3.one;
        t.localScale = new Vector3(1f / parent.x, 1f / parent.y, 1f / parent.z);

        (CrtFace.Vertex[] vertices, int[] triangles) = CrtFace.Dome(shape);
        (float w, float h) = CrtFace.PictureBounds(shape, fill, Overscan);
        filter.sharedMesh = Build("CRT face", vertices, triangles, 0f, new Vector2(w, h));

        var glass = new GameObject("CrtGlass", typeof(MeshFilter), typeof(MeshRenderer));
        glass.layer = screen.gameObject.layer;
        glass.transform.SetParent(t, false);
        glass.transform.localPosition = Vector3.zero;
        glass.GetComponent<MeshFilter>().sharedMesh = Build("CRT glass", vertices, triangles, -GlassLift, Vector2.zero);
        var glassRenderer = glass.GetComponent<MeshRenderer>();
        Material glassMaterial = Resources.Load<Material>(GlassMaterial);
        if (glassMaterial != null)
            glassRenderer.sharedMaterial = glassMaterial;
        else
        {
            glassRenderer.enabled = false;
            Debug.LogWarning($"[PaintedCrt] Resources/{GlassMaterial} is missing: the CRT shows no scanlines or sheen.");
        }
        Quiet(glassRenderer);

        var recess = new GameObject("CrtRecess", typeof(MeshFilter), typeof(MeshRenderer));
        recess.layer = screen.gameObject.layer;
        recess.transform.SetParent(t, false);
        recess.transform.localPosition = new Vector3(0f, (CrtFace.RecessTop + CrtFace.RecessBottom) / 2f, CrtFace.RecessDepth);
        recess.transform.localScale = new Vector3(2f * CrtFace.RecessHalfWidth, CrtFace.RecessTop - CrtFace.RecessBottom, 1f);
        recess.GetComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        var recessRenderer = recess.GetComponent<MeshRenderer>();
        // The screen's art material (Desk_PC_Screen: URP Unlit) in the casing's shadowed beige.
        var recessMaterial = new Material(screen.sharedMaterial) { name = "CRT recess (runtime)" };
        if (recessMaterial.HasProperty("_BaseColor"))
            recessMaterial.SetColor("_BaseColor", RecessColour);
        recessRenderer.sharedMaterial = recessMaterial;
        Quiet(recessRenderer);
        return new Vector2(shareX, shareY);
    }

    /// <summary>A renderer that casts and takes no shadow.</summary>
    private static void Quiet(Renderer renderer)
    {
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    /// <summary>
    /// The dome as a mesh, moved <paramref name="lift"/> along z (- toward the
    /// chair); with a <paramref name="picture"/> size, its submesh reports the
    /// picture's 4:3 bounds (PcScreenClone fits the clone to them), centred on
    /// the glass.
    /// </summary>
    private static Mesh Build(string name, CrtFace.Vertex[] vertices, int[] triangles, float lift, Vector2 picture)
    {
        var positions = new Vector3[vertices.Length];
        var uvs = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            positions[i] = new Vector3(vertices[i].X, vertices[i].Y, vertices[i].Z + lift);
            uvs[i] = new Vector2(vertices[i].U, vertices[i].V);
        }
        var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
        mesh.SetVertices(positions);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        if (picture.x > 0f && picture.y > 0f)
        {
            Bounds dome = mesh.bounds;
            var bounds = new Bounds(new Vector3(0f, 0f, dome.center.z), new Vector3(picture.x, picture.y, dome.size.z));
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, triangles.Length)
            {
                bounds = bounds,
                firstVertex = 0,
                vertexCount = vertices.Length,
            }, MeshUpdateFlags.DontRecalculateBounds);
            dome.Encapsulate(bounds);
            mesh.bounds = dome;
        }
        return mesh;
    }
}
