using System;
using System.Collections.Generic;
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
/// keeps one off (DeskPaintedMap: PaintedRole).
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
    /// frame's height (the papers lie there).
    /// </summary>
    public void Align(DeskFrame frame) => transform.SetPositionAndRotation(frame.Point(Vector2.zero, -paperPlane), frame.Facing(0f));

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
            if (!_painted.Contains(r) && !Kept(r.transform, gameplay, keep))
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

    /// <summary>Moves the bake's <paramref name="child"/> of <paramref name="name"/> (the intercom's Mic) to a direct child of <paramref name="parent"/>, where it stands (MachineSlideIn sways a "Mic" under the slide).</summary>
    public void MoveChild(string name, string child, Transform parent)
    {
        Transform mesh = Mesh(name);
        Transform part = mesh != null ? mesh.Find(child) : null;
        if (part != null && parent != null)
            part.SetParent(parent, true);
    }
}
