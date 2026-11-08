using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The painted desk (desk v3, Saleh 2026-10-20 on mock_office.png: "yes but
/// they need to slide"; UNITY_PLAN 4, DESK-ART): the approved painting baked
/// onto real meshes (Assets/Art/Office/DeskPainted, built into its prefab by
/// Tools > TimeDesk > Import Painted Desk, DeskPaintedImporter), with every
/// movable object its own mesh on its own pivot. Build Office UI puts it into
/// the gameplay scene; the office binder installs it when the plan's
/// DeskArt.Painted toggle is on (DeskLayoutSettings.paintedDesk), else hides
/// it. It carries the homes the importer measured on its meshes (Homes,
/// FixedArt: the desk's frame, metres), so the meshes are the painted map's
/// one source. Installing: Align puts it on the bound desk's frame (the
/// painted mat's top where the papers lie); Reskin hands an object's look to
/// the gameplay object it stands for (the object's own meshes hide, the
/// painted one rides its slide); SliderOf gives a painted object that slides
/// on its own (home root, Slide child, mesh) to MachineSlideIn.Attach; Hide
/// keeps one off (DeskPaintedMap: PaintedRole). BuildStandIn puts a neutral,
/// painting-matched stand-in on a gameplay object the bake has no painted
/// model of (DeskPaintedMap.StandIns); WearBoxes dresses one that keeps its
/// own moving parts (the supply crate) in painting-matched boxes; Unshow and
/// Silence keep an old model's object or click boxes off the painted desk
/// (DeskPaintedMap.Unshown, DeadClicks); EnsureClickable gives a painted
/// slider no gameplay object stands for its hover outline; and Light lights
/// only the next-tool lamp's jewel (DeskLayout.Lamps).
/// </summary>
public sealed class DeskPainted : MonoBehaviour
{
    /// <summary>One object of the bake as the importer laid it.</summary>
    [Serializable]
    public sealed class Part
    {
        /// <summary>The object's name in the bake (DeskPaintedMap.Objects).</summary>
        public string name;

        /// <summary>Its home root: for a movable object the empty at its pivot whose Slide child holds the mesh; else the mesh object itself.</summary>
        public Transform root;

        /// <summary>The mesh object (the bake's object, with its children).</summary>
        public Transform mesh;
    }

    /// <summary>Every object of the bake, as the importer laid it.</summary>
    [SerializeField] private Part[] parts = Array.Empty<Part>();

    /// <summary>The painted homes, measured by the importer on the meshes (the desk's frame, metres; DeskPaintedMap.Home).</summary>
    [SerializeField] private DeskHome[] homes = Array.Empty<DeskHome>();

    /// <summary>The painted fixed art (the PC, its keyboard, the AVAILABLE sign), measured the same way.</summary>
    [SerializeField] private DeskHome[] fixedArt = Array.Empty<DeskHome>();

    /// <summary>The height of the painted mat's pad over the bake's floor (metres; measured by the importer on the mesh): the papers lie there (Align).</summary>
    [SerializeField] private float paperPlane = 1.069f;

    /// <summary>The PC's screen: a quad in the CRT's cut-out the desktop's clone draws on (PcScreenClone).</summary>
    [SerializeField] private Renderer screen;

    private readonly Dictionary<string, Part> _byName = new Dictionary<string, Part>();
    private readonly HashSet<Renderer> _painted = new HashSet<Renderer>();
    private readonly HashSet<Transform> _worn = new HashSet<Transform>();

    /// <summary>True when <paramref name="r"/> is part of the painted look: one of the bake's renderers, or one under a line the painted desk wears (WearReadout: the readout's text and any sub-mesh it makes later).</summary>
    private bool IsPainted(Renderer r)
    {
        if (_painted.Contains(r))
            return true;
        for (Transform t = r.transform; t != null; t = t.parent)
            if (_worn.Contains(t))
                return true;
        return false;
    }

    /// <summary>The painted homes (DeskLayoutSettings.UsePainted merges them over DESK-PLAN's map).</summary>
    public IReadOnlyList<DeskHome> Homes => homes;

    /// <summary>The painted fixed art the homes keep clear of.</summary>
    public IReadOnlyList<DeskHome> FixedArt => fixedArt;

    /// <summary>The PC's screen quad (the desktop's clone draws on it), or null.</summary>
    public Renderer Screen => screen;

    /// <summary>Sets what the importer laid and measured (DeskPaintedImporter, editor only).</summary>
    public void Configure(Part[] laid, DeskHome[] measuredHomes, DeskHome[] measuredFixedArt, float pad, Renderer pcScreen)
    {
        parts = laid ?? Array.Empty<Part>();
        homes = measuredHomes ?? Array.Empty<DeskHome>();
        fixedArt = measuredFixedArt ?? Array.Empty<DeskHome>();
        paperPlane = pad;
        screen = pcScreen;
        _byName.Clear();
    }

    /// <summary>The object <paramref name="name"/> of the bake, or null.</summary>
    private Part Find(string name)
    {
        if (_byName.Count == 0)
            foreach (Part p in parts)
                if (p != null && !string.IsNullOrEmpty(p.name))
                {
                    _byName[p.name] = p;
                    if (p.mesh != null)
                        _painted.UnionWith(p.mesh.GetComponentsInChildren<Renderer>(true));
                }
        return name != null && _byName.TryGetValue(name, out Part part) ? part : null;
    }

    /// <summary>The mesh object of the bake's <paramref name="name"/>, or null.</summary>
    public Transform Mesh(string name) => Find(name)?.mesh;

    /// <summary>
    /// Puts the desk on <paramref name="frame"/>: the bake's origin on the
    /// frame's origin across the desk (the bake's frame is the game's: the art
    /// mat's centre), square to its axes, and the painted mat's pad on the
    /// frame's height (the papers lie there; the office binder raises the
    /// frame onto the pad: DeskLayoutSettings.paintedLift).
    /// </summary>
    public void Align(DeskFrame frame)
    {
        transform.SetPositionAndRotation(frame.Point(Vector2.zero, -paperPlane), frame.Facing(0f));
        CastNoShadows();
        CloseDealTraySlot();
        CloseGapStrips();
    }

    /// <summary>The DESK-02 gap strips' material (Resources; DeskPaintedImporter builds it) and their reading paint's texture.</summary>
    public const string GapStripsMaterial = "DeskGapStrips", GapStripsTexture = "DeskGapStrips_Read";

    /// <summary>The gap strips' name under the desk (one object, two quads).</summary>
    public const string GapStripsName = "DeskGapStrips";

    /// <summary>
    /// The two gaps beside the mat (the prefab's frame, metres: x from, x to; bake v4's strips): between the mat's edge and the
    /// trays' rims on the left, the brass rail on the right, from under the mat's near edge (z -0.530) to its far edge (0.5194).
    /// </summary>
    public static readonly Vector2[] GapStripsX = { new Vector2(-0.7425f, -0.7295f), new Vector2(0.7295f, 0.7455f) };

    /// <summary>The gap strips' near and far ends (the prefab's z, metres) and their height (0.2 mm under the mat's lowest point, over the well floor).</summary>
    public const float GapStripsNear = -0.530f, GapStripsFar = 0.5194f, GapStripsY = 1.0688f;

    /// <summary>
    /// Closes the gaps beside the mat in the reading view (DESK-02, the audit of 2026-10-28: "a flat black slit at the
    /// mat's right edge"): the 1.2 to 1.5 cm gaps between the mat and the rail and trays meet no desk mesh, so the hall
    /// showed through them from above. Two level quads close them, painted from the overhead plates (the bake v6 reading
    /// set's DeskGapStrips_Read.png: the left strip's UVs u 0.02 to 0.48, the right one's 0.52 to 0.98, v from near to
    /// far). Their material draws only in the reading blend (NOPE/Desk Painted's _ReadingOnly): clipped in the office
    /// pose, so the office view stays v3's exactly, and faded in with the Tab tilt. Idempotent; none without the material.
    /// </summary>
    private void CloseGapStrips()
    {
        if (transform.Find(GapStripsName) != null)
            return;
        var material = Resources.Load<Material>(GapStripsMaterial);
        if (material == null)
            return;
        var strips = new GameObject(GapStripsName, typeof(MeshFilter), typeof(MeshRenderer));
        strips.layer = gameObject.layer;
        strips.transform.SetParent(transform, false);
        strips.GetComponent<MeshFilter>().sharedMesh = GapStripsMesh();
        var renderer = strips.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    /// <summary>The gap strips' mesh in the prefab's frame (two level quads facing up, UVs as the bake laid them).</summary>
    public static Mesh GapStripsMesh()
    {
        var vertices = new Vector3[8];
        var uvs = new Vector2[8];
        var triangles = new int[12];
        for (int i = 0; i < 2; i++)
        {
            float u0 = i * 0.5f + 0.02f, u1 = i * 0.5f + 0.48f;
            Vector2 x = GapStripsX[i];
            int v = 4 * i;
            vertices[v] = new Vector3(x.x, GapStripsY, GapStripsNear);
            vertices[v + 1] = new Vector3(x.x, GapStripsY, GapStripsFar);
            vertices[v + 2] = new Vector3(x.y, GapStripsY, GapStripsFar);
            vertices[v + 3] = new Vector3(x.y, GapStripsY, GapStripsNear);
            uvs[v] = new Vector2(u0, 0f);
            uvs[v + 1] = new Vector2(u0, 1f);
            uvs[v + 2] = new Vector2(u1, 1f);
            uvs[v + 3] = new Vector2(u1, 0f);
            // clockwise seen from above: Unity's front face
            triangles[6 * i] = v;
            triangles[6 * i + 1] = v + 1;
            triangles[6 * i + 2] = v + 2;
            triangles[6 * i + 3] = v;
            triangles[6 * i + 4] = v + 2;
            triangles[6 * i + 5] = v + 3;
        }
        var mesh = new Mesh { name = "Desk gap strips (runtime)", vertices = vertices, uv = uvs, triangles = triangles };
        mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up };
        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>The back of the deal tray's slot (the prefab's frame: centre and size, metres; a quad facing the chair).</summary>
    private static readonly Vector3 SlotBackCentre = new Vector3(0f, 1.12f, 1.21f), SlotBackSize = new Vector3(0.5f, 0.1f, 1f);

    /// <summary>The slot back's colour: the counter's dark band over it (sRGB 58, 34, 20).</summary>
    private static readonly Color SlotBackColour = new Color(58f / 255f, 34f / 255f, 20f / 255f, 1f);

    /// <summary>
    /// Closes the slot behind the deal tray (desk v3 round 12; the
    /// concept-vs-game review, item 7, "a light grey bar along the top edge of
    /// the deal tray"): the bake's tray runs under the counter's face and its
    /// floor ends 2 cm short of it, so four rows of screen (y 693 to 696 at
    /// 1080p) met no desk mesh and showed the hall's foreground floor, pale, as
    /// a bar. A quad in the counter's dark brown stands at the slot's back.
    /// (The painting shows the traveller's robe there; a traveller at the
    /// desk no longer shows through those 4 px.) Idempotent.
    /// </summary>
    private void CloseDealTraySlot()
    {
        if (transform.Find("DealTraySlotBack") != null || screen == null || screen.sharedMaterial == null)
            return;
        var back = new GameObject("DealTraySlotBack", typeof(MeshFilter), typeof(MeshRenderer));
        back.layer = gameObject.layer;
        back.transform.SetParent(transform, false);
        back.transform.localPosition = SlotBackCentre;
        back.transform.localRotation = Quaternion.identity;
        back.transform.localScale = SlotBackSize;
        back.GetComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        var renderer = back.GetComponent<MeshRenderer>();
        // The PC screen's art material (URP Unlit), in the counter's brown.
        var material = new Material(screen.sharedMaterial) { name = "Deal tray slot back (runtime)" };
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", SlotBackColour);
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    /// <summary>
    /// No painted object casts a real-time shadow (desk v3 round 12; the
    /// concept-vs-game review, items 6 and 11): the paint already holds each
    /// object's shadow, and the hall's daylight cast them the other way (light
    /// from the left, the painting's from the upper right) as pale blue
    /// wedges, the "ghost square" right of the chip reader among them. A
    /// moving object's contact shadow is SlideShadow's. The bake's prefab
    /// still marks the movers to cast (DeskPaintedImporter, which round 12
    /// changes too, for its next import); this puts every painted renderer
    /// off at install.
    /// </summary>
    public void CastNoShadows()
    {
        foreach (Part p in parts)
            if (p != null && p.mesh != null)
                foreach (Renderer r in p.mesh.GetComponentsInChildren<Renderer>(true))
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    /// <summary>The height of the painted mat's pad over the bake's floor (metres): the bake's floor is the world's, so the papers lie this high (the office binder's paintedLift).</summary>
    public float PaperPlane => paperPlane;

    // -----------------------------
    // The lamps (C's next-tool lamp; UNITY_PLAN 4.3)
    // -----------------------------

    /// <summary>The lamps' jewels by lamp name (anchors.json's lamps: Lamps/lamp_*/Jewel), found once.</summary>
    private readonly Dictionary<string, Renderer> _jewels = new Dictionary<string, Renderer>();

    private void OnEnable()
    {
        DeskLayout.Lamps.Changed += Light;
        Light(DeskLayout.Lamps.Lit);
    }

    private void OnDisable() => DeskLayout.Lamps.Changed -= Light;

    /// <summary>
    /// Lights the lamp beside the home <paramref name="home"/> (DeskLayout.Lamps:
    /// the next tool, NextToolLamp; null: none) and leaves every other lamp
    /// unlit (its jewel off, its painted bezel showing; slides.json: a lamp is
    /// guidance, never lit at rest for nothing). FEEDBACK's renderer may take
    /// over the jewels later (its pulse).
    /// </summary>
    public void Light(string home)
    {
        if (_jewels.Count == 0)
        {
            Transform lamps = transform.Find("Lamps");
            if (lamps != null)
                foreach (Transform lamp in lamps)
                {
                    Transform jewel = lamp.Find("Jewel");
                    if (jewel != null && jewel.TryGetComponent(out Renderer r))
                        _jewels[lamp.name] = r;
                }
        }
        foreach (KeyValuePair<string, Renderer> jewel in _jewels)
            if (jewel.Value != null)
                jewel.Value.enabled = DeskPaintedMap.LampLit(jewel.Key, home);
        // A bezel the painting does not draw shows only while its lamp is lit (DeskPaintedMap.BezelShown).
        Transform all = transform.Find("Lamps");
        if (all != null)
            foreach (Transform lamp in all)
            {
                Transform bezel = lamp.Find(DeskPaintedMap.LampBezel);
                if (bezel == null)
                    continue;
                bool shown = DeskPaintedMap.BezelShown(lamp.name, home);
                foreach (Renderer r in bezel.GetComponentsInChildren<Renderer>(true))
                    r.enabled = shown;
            }
    }

    // -----------------------------
    // Stand-ins (DeskPaintedMap.StandIns)
    // -----------------------------

    /// <summary>The stand-ins' materials by colour (the painted shader on the colour's painted cells, no mask), made once, and their textures.</summary>
    private readonly Dictionary<Color, Material> _standInMaterials = new Dictionary<Color, Material>();
    private readonly List<Texture2D> _standInTextures = new List<Texture2D>();

    /// <summary>The stand-ins' box: a unit cube whose six faces map into their own cells of a stand-in texture (DeskPaintedMap.StandInTexel), made once.</summary>
    private static Mesh _standInCube;

    /// <summary>
    /// Builds <paramref name="standIn"/> under <paramref name="parent"/> (the
    /// gameplay object it stands for, or its add-on's root: it rides that
    /// object's slide, shows and pops with it) on the desk's
    /// <paramref name="frame"/>, its anchor at the desk point
    /// <paramref name="at"/> (the frame's x, y) <paramref name="floor"/>
    /// metres over the frame: each box a cube in its painting-matched colour
    /// with the painting's light and ink painted on its faces (the painted
    /// shader adds no light of its own at the painting's hour: the bake's
    /// objects carry theirs in their paint), lit by NOPE/Desk Painted with no
    /// mask, casting a shadow as everything that moves, with no collider. The parent may wait
    /// at zero scale for the crate: it is built at <paramref name="restScale"/>
    /// (the scale it pops to, DeskUpgrades.RestScale). Replaced when built
    /// again. Returns its renderers (the click boxes' outline).
    /// </summary>
    public Renderer[] BuildStandIn(StandIn standIn, Transform parent, Vector3 restScale, DeskFrame frame, Vector2 at, float floor)
    {
        if (standIn == null || parent == null)
            return Array.Empty<Renderer>();
        string name = "StandIn_" + standIn.id;
        Transform old = parent.Find(name);
        if (old != null)
            Destroy(old.gameObject);
        // The stand-in's root at its anchor, square to the desk's frame and at the world's scale once its parent stands at rest: its boxes lie in the frame's axes (x right, y up, z away from the chair) under it.
        var root = new GameObject(name).transform;
        PlaceUnder(root, parent, restScale, frame.Point(at, floor), frame.Facing(0f), Vector3.one);
        var made = new List<Renderer>();
        foreach (StandInBox box in standIn.boxes)
        {
            var part = new GameObject(box.name, typeof(MeshFilter), typeof(MeshRenderer));
            part.GetComponent<MeshFilter>().sharedMesh = StandInCube();
            part.transform.SetParent(root, false);
            part.transform.localPosition = new Vector3(box.x, box.z + box.height / 2f, box.y);
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = new Vector3(box.width, box.height, box.depth);
            var renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = StandInMaterial(new Color(box.r, box.g, box.b, 1f));
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // round 12: the paint holds the shadows (CastNoShadows)
            made.Add(renderer);
        }
        return made.ToArray();
    }

    /// <summary>
    /// Puts <paramref name="child"/> under <paramref name="parent"/> at the world
    /// pose <paramref name="position"/>, <paramref name="rotation"/> and world size
    /// <paramref name="size"/>, as if <paramref name="parent"/> stood at
    /// <paramref name="restScale"/> (it may wait at zero scale for the crate).
    /// </summary>
    private static void PlaceUnder(Transform child, Transform parent, Vector3 restScale, Vector3 position, Quaternion rotation, Vector3 size)
    {
        Transform up = parent.parent;
        Matrix4x4 rest = (up != null ? up.localToWorldMatrix : Matrix4x4.identity) * Matrix4x4.TRS(parent.localPosition, parent.localRotation, restScale);
        Quaternion restRotation = (up != null ? up.rotation : Quaternion.identity) * parent.localRotation;
        Vector3 lossy = Vector3.Scale(up != null ? up.lossyScale : Vector3.one, restScale);
        child.SetParent(parent, false);
        child.localPosition = rest.inverse.MultiplyPoint3x4(position);
        child.localRotation = Quaternion.Inverse(restRotation) * rotation;
        child.localScale = new Vector3(size.x / Safe(lossy.x), size.y / Safe(lossy.y), size.z / Safe(lossy.z));
    }

    private static float Safe(float v) => Mathf.Abs(v) > 1e-6f ? v : 1f;

    /// <summary>The stand-ins' unit cube: each face (DeskPaintedMap.FaceShade's order: top, front, right, left, back, bottom) on its own cell of the texture, inset by half a texel, front-facing outward.</summary>
    private static Mesh StandInCube()
    {
        if (_standInCube != null)
            return _standInCube;
        var faces = new (Vector3 n, Vector3 a, Vector3 b)[]
        {
            (Vector3.up, Vector3.right, Vector3.forward), (Vector3.back, Vector3.right, Vector3.up), (Vector3.right, Vector3.forward, Vector3.up),
            (Vector3.left, Vector3.back, Vector3.up), (Vector3.forward, Vector3.left, Vector3.up), (Vector3.down, Vector3.right, Vector3.back),
        };
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();
        int cell = DeskPaintedMap.StandInCell;
        float insetU = 0.5f / (3f * cell), insetV = 0.5f / (2f * cell);
        for (int f = 0; f < faces.Length; f++)
        {
            (Vector3 n, Vector3 a, Vector3 b) = faces[f];
            float u0 = f % 3 / 3f + insetU, u1 = (f % 3 + 1) / 3f - insetU, v0 = f / 3 / 2f + insetV, v1 = (f / 3 + 1) / 2f - insetV;
            int at = vertices.Count;
            vertices.Add((n - a - b) * 0.5f);
            vertices.Add((n + a - b) * 0.5f);
            vertices.Add((n + a + b) * 0.5f);
            vertices.Add((n - a + b) * 0.5f);
            uvs.Add(new Vector2(u0, v0));
            uvs.Add(new Vector2(u1, v0));
            uvs.Add(new Vector2(u1, v1));
            uvs.Add(new Vector2(u0, v1));
            for (int i = 0; i < 4; i++)
                normals.Add(n);
            bool outward = Vector3.Dot(Vector3.Cross(vertices[at + 1] - vertices[at], vertices[at + 2] - vertices[at]), n) > 0f;
            triangles.AddRange(outward ? new[] { at, at + 1, at + 2, at, at + 2, at + 3 } : new[] { at, at + 2, at + 1, at, at + 3, at + 2 });
        }
        _standInCube = new Mesh { name = "DeskPainted_StandInCube" };
        _standInCube.SetVertices(vertices);
        _standInCube.SetNormals(normals);
        _standInCube.SetUVs(0, uvs);
        _standInCube.SetTriangles(triangles, 0);
        _standInCube.RecalculateBounds();
        return _standInCube;
    }

    /// <summary>The stand-ins' painted cells of <paramref name="colour"/> (sRGB, mipmapped): each face its share of the painting's light, its edges inked (DeskPaintedMap.StandInTexel).</summary>
    private Texture2D StandInTexture(Color colour)
    {
        int cell = DeskPaintedMap.StandInCell;
        var texture = new Texture2D(3 * cell, 2 * cell, TextureFormat.RGBA32, true, false)
        {
            name = $"DeskPainted_StandIn_{ColorUtility.ToHtmlStringRGB(colour)}",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };
        var pixels = new Color32[3 * cell * 2 * cell];
        for (int f = 0; f < 6; f++)
            for (int y = 0; y < cell; y++)
                for (int x = 0; x < cell; x++)
                {
                    (float r, float g, float b) t = DeskPaintedMap.StandInTexel((colour.r, colour.g, colour.b), f, x, y);
                    pixels[(f / 3 * cell + y) * 3 * cell + f % 3 * cell + x] = new Color(t.r, t.g, t.b, 1f);
                }
        texture.SetPixels32(pixels);
        texture.Apply(true, true);
        _standInTextures.Add(texture);
        return texture;
    }

    /// <summary>The stand-ins' material of <paramref name="colour"/>: a copy of a painted material (NOPE/Desk Painted, so it takes the desk's light and shadow) on the colour's painted cells (StandInTexture), no mask.</summary>
    private Material StandInMaterial(Color colour)
    {
        if (_standInMaterials.TryGetValue(colour, out Material made))
            return made;
        Material template = null;
        foreach (Part p in parts)
            if (p != null && p.mesh != null && p.mesh.TryGetComponent(out Renderer r) && r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor") && r.sharedMaterial.HasProperty("_Locked"))
            {
                template = r.sharedMaterial;
                break;
            }
        made = template != null ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
        made.name = $"DeskPainted_StandIn_{ColorUtility.ToHtmlStringRGB(colour)}";
        if (made.HasProperty("_BaseMap"))
        {
            made.SetTexture("_BaseMap", StandInTexture(colour));
            made.SetTextureScale("_BaseMap", Vector2.one);
            made.SetTextureOffset("_BaseMap", Vector2.zero);
            made.SetColor("_BaseColor", Color.white);
        }
        else
            made.SetColor("_BaseColor", colour);
        made.DisableKeyword("_MASKMAP");
        if (made.HasProperty("_UseMask"))
            made.SetFloat("_UseMask", 0f);
        if (made.HasProperty("_Locked"))
            made.SetFloat("_Locked", 0f);
        _standInMaterials[colour] = made;
        return made;
    }

    private void OnDestroy()
    {
        foreach (Material m in _standInMaterials.Values)
            if (m != null)
                Destroy(m);
        foreach (Texture2D t in _standInTextures)
            if (t != null)
                Destroy(t);
    }

    /// <summary>The name of a box WearBoxes builds over a part.</summary>
    public const string PartBoxName = "PaintedBox";

    /// <summary>A box's least thickness (world metres): a part with no depth (a label's quad) still makes a box.</summary>
    private const float MinBox = 0.001f;

    /// <summary>
    /// The gameplay object <paramref name="gameplay"/> on
    /// <paramref name="home"/> (DeskPaintedMap.Boxed: the supply crate)
    /// wears each of its own parts as a painting-matched box
    /// (DeskPaintedMap.LookOf): a box over the part's own mesh bounds in the
    /// part's colour, with the painting's light and ink on its faces (the
    /// stand-ins' look, NOPE/Desk Painted), riding the part (a flap still
    /// opens, the crate still pops in and shrinks away), casting a shadow as
    /// everything that moves, with no collider; a part LookOf hides is
    /// hidden, a kept part (DeskPaintedMap.Kept: the crate's tag, the
    /// delivery's live label) drawn as it is. Its click boxes stay (the boxes
    /// fill the old model's bounds they were fitted to) and outline the
    /// boxes. A second call builds nothing more. Returns the boxes' renderers.
    /// </summary>
    public Renderer[] WearBoxes(Transform gameplay, string home)
    {
        if (gameplay == null)
            return Array.Empty<Renderer>();
        string[] keep = DeskPaintedMap.Kept(home);
        var made = new List<Renderer>();
        foreach (Renderer r in gameplay.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null || Kept(r.transform, gameplay, keep))
                continue;
            if (r.name == PartBoxName)
            {
                made.Add(r); // built by an earlier call
                continue;
            }
            PartLook look = DeskPaintedMap.LookOf(home, r.name, out var colour, out float topInset);
            if (look == PartLook.Keep)
                continue;
            r.enabled = false;
            if (look == PartLook.Box && r.transform.Find(PartBoxName) == null && r.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null)
                made.Add(BoxOver(r.transform, filter.sharedMesh.bounds, new Color(colour.r, colour.g, colour.b, 1f), topInset));
        }
        Renderer[] boxes = made.ToArray();
        foreach (Clickable click in gameplay.GetComponentsInChildren<Clickable>(true))
            click.SetOutline(boxes);
        return boxes;
    }

    /// <summary>A painting-matched box (the stand-ins' cube and material) under <paramref name="part"/> over its mesh's local <paramref name="bounds"/>, at least MinBox thick, its top <paramref name="topInset"/> world metres lower (along the part's axis nearest the world's up).</summary>
    private Renderer BoxOver(Transform part, Bounds bounds, Color colour, float topInset)
    {
        var box = new GameObject(PartBoxName, typeof(MeshFilter), typeof(MeshRenderer));
        box.layer = part.gameObject.layer;
        box.GetComponent<MeshFilter>().sharedMesh = StandInCube();
        Vector3 lossy = part.lossyScale, size = bounds.size, centre = bounds.center;
        int up = 0;
        float most = -1f;
        for (int i = 0; i < 3; i++)
        {
            size[i] = Mathf.Max(size[i], MinBox / Mathf.Max(1e-6f, Mathf.Abs(lossy[i])));
            float alongUp = Mathf.Abs(part.TransformDirection(Unit(i)).y);
            if (alongUp > most)
            {
                most = alongUp;
                up = i;
            }
        }
        if (topInset > 0f && Mathf.Abs(lossy[up]) > 1e-6f)
        {
            float local = Mathf.Min(topInset / Mathf.Abs(lossy[up]), size[up] / 2f);
            size[up] -= local;
            centre[up] -= Mathf.Sign(part.TransformDirection(Unit(up)).y) * local / 2f;
        }
        Transform t = box.transform;
        t.SetParent(part, false);
        t.localPosition = centre;
        t.localRotation = Quaternion.identity;
        t.localScale = size;
        var renderer = box.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = StandInMaterial(colour);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // round 12: the paint holds the shadows (CastNoShadows)
        return renderer;
    }

    private static Vector3 Unit(int axis) => axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;

    /// <summary>
    /// What of the gameplay object <paramref name="gameplay"/> on
    /// <paramref name="home"/> the painted desk still draws or clicks of its
    /// old model (the binder's audit at bind: a warning names each, so a
    /// smoke run's console shows it): every renderer still on that is neither
    /// painted (the bake's own, or under a line the desk wears: WearReadout),
    /// a stand-in's (under a "StandIn_" root), a part's box
    /// (PartBoxName) nor under a part the home keeps (DeskPaintedMap.Kept),
    /// and every click box still on under a part the painted look has no
    /// part for (DeskPaintedMap.DeadClicks), or anywhere on an unshown
    /// object (DeskPaintedMap.Unshown). Each as its path under the object;
    /// empty on a clean desk (Saleh: every object matches the painting).
    /// </summary>
    public List<string> OldParts(Transform gameplay, string home)
    {
        var old = new List<string>();
        if (gameplay == null)
            return old;
        Find(null); // the painted renderers, gathered once
        string[] keep = DeskPaintedMap.Kept(home), dead = DeskPaintedMap.DeadClicks(home);
        bool unshown = Array.IndexOf(DeskPaintedMap.Unshown, home) >= 0;
        foreach (Renderer r in gameplay.GetComponentsInChildren<Renderer>(true))
            if (r.enabled && !IsPainted(r) && r.name != PartBoxName && !UnderStandIn(r.transform, gameplay) && (unshown || !Kept(r.transform, gameplay, keep)))
                old.Add(PathOf(r.transform, gameplay));
        foreach (Collider box in gameplay.GetComponentsInChildren<Collider>(true))
            if (box.enabled && (unshown || Kept(box.transform, gameplay, dead)))
                old.Add(PathOf(box.transform, gameplay) + " (click box)");
        return old;
    }

    /// <summary>True when <paramref name="part"/> lies under a stand-in's root (BuildStandIn's "StandIn_" object) below <paramref name="top"/>.</summary>
    private static bool UnderStandIn(Transform part, Transform top)
    {
        for (Transform t = part; t != null && t != top; t = t.parent)
            if (t.name.StartsWith("StandIn_", StringComparison.Ordinal))
                return true;
        return false;
    }

    /// <summary><paramref name="part"/>'s path under <paramref name="top"/> ("." for the object itself).</summary>
    private static string PathOf(Transform part, Transform top)
    {
        string path = null;
        for (Transform t = part; t != null && t != top; t = t.parent)
            path = path == null ? t.name : t.name + "/" + path;
        return path ?? ".";
    }

    /// <summary>
    /// Keeps the gameplay object <paramref name="gameplay"/> (DeskPaintedMap.
    /// Unshown: the RING SERVICE panel) off the painted desk: none of its
    /// renderers draws (its live texts included) and none of its click boxes
    /// takes a click or a hover, whatever its owner shows or pops later (a
    /// renderer's and a collider's enabled flags outlive SetActive); its logic
    /// runs on unseen.
    /// </summary>
    public static void Unshow(Transform gameplay)
    {
        if (gameplay == null)
            return;
        foreach (Renderer r in gameplay.GetComponentsInChildren<Renderer>(true))
            r.enabled = false;
        Silence(gameplay, null);
    }

    /// <summary>
    /// Switches off the click boxes under <paramref name="gameplay"/> on a
    /// part named in <paramref name="parts"/> or under one (every one with
    /// null): their colliders take no ray (no click, no hover) and their
    /// Clickables neither click nor outline (DeskPaintedMap.DeadClicks: an
    /// old model's box the painted look has no part for).
    /// </summary>
    public static void Silence(Transform gameplay, string[] parts)
    {
        if (gameplay == null)
            return;
        foreach (Collider box in gameplay.GetComponentsInChildren<Collider>(true))
            if (parts == null || Kept(box.transform, gameplay, parts))
                box.enabled = false;
        foreach (Clickable click in gameplay.GetComponentsInChildren<Clickable>(true))
            if (parts == null || Kept(click.transform, gameplay, parts))
            {
                click.Interactable = false;
                click.enabled = false;
            }
    }

    /// <summary>
    /// The hover target of the bake's <paramref name="name"/> (a painted
    /// slider no gameplay object stands for: the glasses, the folded book;
    /// the review of the Unity capture: one rim rule for every slider,
    /// glasses included): its click box (EnsureClickBox) with a Clickable
    /// that outlines the painted mesh and answers a click with
    /// <paramref name="onClick"/> (added once). Null without the object.
    /// </summary>
    public Clickable EnsureClickable(string name, UnityEngine.Events.UnityAction onClick)
    {
        EnsureClickBox(name);
        Transform mesh = Mesh(name);
        Collider box = mesh != null ? mesh.GetComponent<Collider>() : null;
        if (box == null)
            return null;
        if (!box.TryGetComponent(out Clickable click))
        {
            click = box.gameObject.AddComponent<Clickable>();
            if (onClick != null)
                click.onClick.AddListener(onClick);
        }
        click.SetOutline(RenderersOf(name));
        return click;
    }

    /// <summary>A click box on the Interactable layer around the bake's <paramref name="name"/> (a painted slider no gameplay object stands for: the slide-in ranks the silhouettes under the cursor by these boxes), riding its slide; the importer makes them for its sliders, and this makes one the prefab lacks. A box the mesh already carries is put on the layer too: off it, a box takes no office click (the camera's raycaster reads the Interactable layer).</summary>
    public void EnsureClickBox(string name)
    {
        Transform mesh = Mesh(name);
        if (mesh == null)
            return;
        int layer = LayerMask.NameToLayer(OfficeLayers.Interactable);
        if (mesh.GetComponent<Collider>() != null)
        {
            if (layer >= 0)
                mesh.gameObject.layer = layer;
            return;
        }
        if (!mesh.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null)
            return;
        if (layer >= 0)
            mesh.gameObject.layer = layer;
        var box = mesh.gameObject.AddComponent<BoxCollider>();
        Bounds b = filter.sharedMesh.bounds;
        box.center = b.center;
        box.size = b.size + Vector3.one * (2f * DeskHomeMap.ClickPad);
    }

    /// <summary>Shows the bake's <paramref name="name"/> (a part the prefab was built with hidden, or hidden before).</summary>
    public void Show(string name)
    {
        Transform mesh = Mesh(name);
        if (mesh != null)
            mesh.gameObject.SetActive(true);
    }

    /// <summary>The world bounds of the bake's <paramref name="name"/> (its mesh and children: every renderer, shown or not); false without it.</summary>
    public bool TryBounds(string name, out Bounds bounds)
    {
        bounds = default;
        Transform mesh = Mesh(name);
        return mesh != null && DeskLayout.TryMeshBounds(mesh, out bounds);
    }

    /// <summary>
    /// The world height the home <paramref name="home"/>'s object stands on:
    /// the base of its painted movable objects (the scanner's, the chip
    /// reader's: the gameplay object goes there), or the top of its locked
    /// home's insert (the owned object stands in it); false when it has
    /// neither.
    /// </summary>
    public bool TryFloor(string home, out float floor)
    {
        floor = 0f;
        bool insert = Array.IndexOf(DeskPaintedMap.Inserts, home) >= 0, any = false;
        foreach (PaintedObject o in DeskPaintedMap.Objects)
        {
            if (o.home != home || (!insert && o.role == PaintedRole.Fixed) || !TryBounds(o.name, out Bounds b))
                continue;
            float y = insert ? b.max.y : b.min.y;
            floor = any ? (insert ? Mathf.Max(floor, y) : Mathf.Min(floor, y)) : y;
            any = true;
        }
        return any;
    }

    /// <summary>The home root (its Slide child moves) of the painted object <paramref name="name"/> that slides on its own (MachineSlideIn.Attach), or null.</summary>
    public Transform SliderOf(string name) => Find(name)?.root;

    /// <summary>The renderers of the bake's <paramref name="name"/> (its mesh and children).</summary>
    public Renderer[] RenderersOf(string name)
    {
        Transform mesh = Mesh(name);
        return mesh != null ? mesh.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>();
    }

    /// <summary>Keeps the bake's <paramref name="name"/> off (its owner's object stands there, or its home is not built yet).</summary>
    public void Hide(string name)
    {
        Transform mesh = Mesh(name);
        if (mesh != null)
            mesh.gameObject.SetActive(false);
    }

    /// <summary>
    /// The painted <paramref name="name"/> becomes the look of the gameplay
    /// object <paramref name="gameplay"/>: every renderer of the gameplay
    /// object switches off (its click boxes and its logic stay) but the
    /// painted desk's own and those under a part named in
    /// <paramref name="keep"/> (its light effects, its upgrades' add-ons), and
    /// the painted mesh rides <paramref name="rider"/> (the transform the
    /// gameplay object's own slide or carry moves), keeping where it was
    /// painted. Its clicks' hover outlines draw the painted mesh. Returns the
    /// painted renderers (none without the object or the gameplay one).
    /// </summary>
    public Renderer[] Reskin(string name, Transform gameplay, Transform rider, params string[] keep)
    {
        Transform mesh = Mesh(name);
        if (mesh == null || gameplay == null || rider == null)
            return Array.Empty<Renderer>();
        foreach (Renderer r in gameplay.GetComponentsInChildren<Renderer>(true))
            if (!IsPainted(r) && !Kept(r.transform, gameplay, keep))
                r.enabled = false;
        mesh.SetParent(rider, true);
        Renderer[] painted = mesh.GetComponentsInChildren<Renderer>(true);
        foreach (Clickable click in gameplay.GetComponentsInChildren<Clickable>(true))
            click.SetOutline(painted);
        return painted;
    }

    /// <summary>True when <paramref name="part"/> or one of its parents up to <paramref name="top"/> is named in <paramref name="keep"/>.</summary>
    private static bool Kept(Transform part, Transform top, string[] keep)
    {
        for (Transform t = part; t != null && t != top; t = t.parent)
            if (Array.IndexOf(keep, t.name) >= 0)
                return true;
        return false;
    }

    /// <summary>
    /// The chip reader's readout on the painted reader <paramref name="name"/>
    /// (desk v3 round 12's S1; the night of 2026-10-28, seam D): the old
    /// reader's live text <paramref name="readout"/> (its font and ink kept;
    /// ChipReader.Show writes it, the one source; Reskin switched its renderer
    /// off) is re-shown as a line on the painted reader's top face
    /// (PaintedReadout: its near third, leant toward the chair, its type free
    /// to grow to the line's height, under the painted mesh so it rides the
    /// reader's slide; the code review's R3 of 2026-10-28), and the readout's
    /// click box <paramref name="clickBox"/> (its BoxCollider and Clickable,
    /// which Silence may have switched off) is re-fitted over the line, on
    /// the Interactable layer, outlining the painted reader. The line is a
    /// VFD window (Saleh's playtest of 2026-11-07, item 15): a dark green
    /// glass quad the line's size behind the text and a near-black bezel quad
    /// round it (PaintedReadout.BezelSize), both children of the text so they
    /// ride and lean with it, the text inset in the glass
    /// (PaintedReadout.TextSize); the painted body stays under them. The line
    /// swaps its text as the old readout did (no fade: ChipReader sets the
    /// text). False without the painted object, its mesh bounds or the text.
    /// </summary>
    public bool WearReadout(string name, TMP_Text readout, Transform clickBox)
    {
        Transform mesh = Mesh(name);
        if (mesh == null || readout == null || !TryLocalBounds(mesh, out Bounds bounds))
            return false;
        var local = new PaintedReadout.Box(bounds.min.x, bounds.max.x, bounds.min.y, bounds.max.y, bounds.min.z, bounds.max.z);
        Transform line = readout.transform;
        // A rotated child of the painted mesh's non-uniform scale would shear (the lean came out at about 12 degrees and the letters
        // skewed): the line rides an unturned mount whose scale undoes the mesh's, so the line's own frame is square.
        line.SetParent(ReadoutMount(mesh), false);
        // A TextMeshPro shows on its -Z: forward down and away from the chair shows it up and toward the chair (PaintedReadout.Tilt),
        // its top edge up the lean, so it reads from the chair.
        var (forward, up) = PaintedReadout.LineFacing();
        Pose(line, V(PaintedReadout.LinePosition(local)), Quaternion.LookRotation(V(forward), V(up)));
        if (line is RectTransform rect)
        {
            (float width, float height) = PaintedReadout.TextSize(local);
            rect.sizeDelta = new Vector2(width, height);
        }
        // The old reader's auto-size cap (0.12: an em of 1.2 cm) kept READY about 21 x 3 px at 1080p; the type may now fill the glass.
        readout.enableAutoSizing = true;
        readout.fontSizeMax = PaintedReadout.FontSizeMax(local);
        // The window behind the text: the glass first, the bezel a step further back, so each shows through nothing in front of it.
        WindowQuad(line, "VfdGlass", PaintedReadout.LineSize(local), PaintedReadout.GlassDepth, VfdGlassColour);
        WindowQuad(line, "VfdBezel", PaintedReadout.BezelSize(local), PaintedReadout.BezelDepth, VfdBezelColour);
        foreach (Renderer r in readout.GetComponentsInChildren<Renderer>(true))
            r.enabled = true;
        // The worn line is the painted look's now: the old-parts audit and a later reskin count it as painted (GATE-26 of
        // 2026-10-28: it was not, so the audit warned on every office load and could not catch a real stray part).
        _worn.Add(line);
        readout.ForceMeshUpdate();
        if (clickBox == null)
            return true;
        clickBox.SetParent(mesh, false);
        Pose(clickBox, V(PaintedReadout.BoxCentre(local)), Quaternion.identity);
        int layer = LayerMask.NameToLayer(OfficeLayers.Interactable);
        if (layer >= 0)
            clickBox.gameObject.layer = layer;
        if (clickBox.TryGetComponent(out BoxCollider box))
        {
            box.center = Vector3.zero;
            box.size = V(PaintedReadout.BoxSize(local));
            box.enabled = true;
        }
        if (clickBox.TryGetComponent(out Clickable click))
        {
            click.enabled = true;
            click.Interactable = true;
            click.SetOutline(RenderersOf(name));
        }
        Physics.SyncTransforms(); // the re-fitted box is where physics looks for it at once: a ray or a bounds read this frame meets it over the line, not where it stood
        return true;
    }

    private static Vector3 V((float x, float y, float z) p) => new Vector3(p.x, p.y, p.z);

    /// <summary>Puts <paramref name="t"/> at <paramref name="local"/> in this desk's frame, turned by <paramref name="turn"/> from the desk's facing, at unit world scale: each of its axes is scaled back by the parent's scale along the parent axis the turn lays it on (a parent scaled unevenly, turned under by a right angle, as the readout line is: exact; any other turn, the nearest).</summary>
    /// <summary>
    /// The painted mesh's unturned child "ReadoutMount" (made once), its
    /// scale the inverse of the mesh's world scale, so a turned child under
    /// it keeps a square frame (no shear) and rides the reader's slide.
    /// </summary>
    public static Transform ReadoutMount(Transform mesh)
    {
        Transform mount = mesh.Find("ReadoutMount");
        if (mount == null)
        {
            mount = new GameObject("ReadoutMount").transform;
            mount.SetParent(mesh, false);
        }
        mount.localPosition = Vector3.zero;
        mount.localRotation = Quaternion.identity;
        Vector3 s = mesh.lossyScale;
        mount.localScale = new Vector3(Inverse(s.x), Inverse(s.y), Inverse(s.z));
        return mount;
    }

    private static float Inverse(float v) => Mathf.Abs(v) < 1e-6f ? 1f : 1f / v;

    /// <summary>The VFD window's glass (a dark green, the phosphor's bed) and its bezel (a light grey: the office's post stack shows an unlit value v at about 200 v luma, so 0.45 lands near 90, between the glass at about 7 and the cream body at about 155; a near-black bezel vanished into the glass).</summary>
    private static readonly Color VfdGlassColour = new Color(0.01f, 0.03f, 0.015f, 1f), VfdBezelColour = new Color(0.45f, 0.45f, 0.43f, 1f);

    /// <summary>
    /// A flat quad of <paramref name="size"/> (metres) in <paramref name="colour"/> under the readout <paramref name="line"/>, named <paramref name="name"/> (made once; found again by name), centred on the line and <paramref name="depth"/> behind its text (a TextMeshPro and Unity's quad both show on their -Z), unlit, no shadows, on the line's layer.
    /// </summary>
    private void WindowQuad(Transform line, string name, (float width, float height) size, float depth, Color colour)
    {
        Transform quad = line.Find(name);
        if (quad == null)
        {
            var made = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            made.GetComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var renderer = made.GetComponent<MeshRenderer>();
            // URP's own unlit in the flat colour: the glass is dark on its own, lit or not. Never the PC screen's material: by the time the desk
            // wears the readout (BindDesk, after BindPc), PcScreenClone has swapped it for its PlanarScreen instance, which has no _BaseColor
            // and draws the live desktop (the verifier of 2026-11-08: a 78 x 19 px copy of the PC's picture in the window's centre).
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = $"Readout {name} (runtime)" };
            material.SetColor("_BaseColor", colour);
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            quad = made.transform;
            quad.SetParent(line, false);
        }
        quad.gameObject.layer = line.gameObject.layer;
        quad.localPosition = new Vector3(0f, 0f, depth);
        quad.localRotation = Quaternion.identity;
        quad.localScale = new Vector3(size.width, size.height, 1f);
    }

    private void Pose(Transform t, Vector3 local, Quaternion turn)
    {
        t.SetPositionAndRotation(transform.TransformPoint(local), transform.rotation * turn);
        if (t.parent == null)
        {
            t.localScale = Vector3.one;
            return;
        }
        Vector3 parentScale = t.parent.lossyScale;
        Quaternion under = t.localRotation;
        var scale = Vector3.one;
        for (int i = 0; i < 3; i++)
        {
            Vector3 axis = under * (i == 0 ? Vector3.right : i == 1 ? Vector3.up : Vector3.forward);
            scale[i] = 1f / Mathf.Max(1e-6f, Vector3.Scale(axis, parentScale).magnitude);
        }
        t.localScale = scale;
    }

    /// <summary>The bounds of <paramref name="mesh"/> (its meshes and its children's) in this desk's frame (x across, y up, z away from the chair); false without a mesh.</summary>
    private bool TryLocalBounds(Transform mesh, out Bounds local)
    {
        local = default;
        bool any = false;
        foreach (MeshFilter filter in mesh.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null)
                continue;
            Bounds b = filter.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                Vector3 p = transform.InverseTransformPoint(filter.transform.TransformPoint(corner));
                if (any)
                    local.Encapsulate(p);
                else
                    local = new Bounds(p, Vector3.zero);
                any = true;
            }
        }
        return any;
    }

    /// <summary>Moves the bake's <paramref name="child"/> of <paramref name="name"/> (the intercom's Mic) to a direct child of <paramref name="parent"/>, where it stands (MachineSlideIn sways a "Mic" under the slide).</summary>
    public void MoveChild(string name, string child, Transform parent)
    {
        Transform mesh = Mesh(name);
        Transform part = mesh != null ? mesh.Find(child) : null;
        if (part != null && parent != null)
            part.SetParent(parent, true);
    }
}
