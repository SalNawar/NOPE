#if UNITY_EDITOR || TIMEDESK_QA
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// DOC-HANDLE's rig measures of a dropped thing's picture (FOLD-01 round 2,
/// the verifier's F1 and F2; Saleh's playtest of 2026-10-28, point 4: "You
/// move the folder and it snaps somewhere else"). The rig's drop op measured
/// the root only, so it saw neither a picture that jumps at the release (the
/// folder's lift, set at once) nor a thing that rests under a tray's rim (the
/// rest's own trace checks the same keep-outs the rest used).
/// <list type="bullet">
/// <item><see cref="Of"/>: the screen rect of what the thing draws opaque
/// (each renderer's own bounds through its own matrix, so a thing drawn
/// toward the camera above every prop, HeldOnTop, has the same picture as
/// where it lies; its contact shadow and its print, drawn transparent, are
/// left out).</item>
/// <item><see cref="Covered"/>: the share of the thing's visible face that
/// something else drawn opaque hides from the camera. It casts a ray from the
/// camera to points spread over the thing's faces that face the camera (one
/// about every 6 mm) inside its own click boxes (a shader may cut part of its
/// model away: the shut folder's left board), keeps the points the thing
/// alone would show, and finds
/// what lies on the ray first: the desk's own meshes (the shell's rims and
/// frames, the lamps), the machines and the props, never a paper or the
/// folder (Papers, Please lets them lie over each other). A geometry check of
/// the meshes as drawn, not of the keep-outs the rest used.</item>
/// </list>
/// Meshes Unity keeps on the GPU only are read back from their buffers; a
/// mesh that cannot be read is left out and named.
/// </summary>
public static class DropPicture
{
    /// <summary>The screen rect (pixels, y from the bottom, as the rig's screen points) of what <paramref name="go"/> draws opaque through <paramref name="camera"/>; null with nothing on the screen.</summary>
    public static Rect? Of(GameObject go, Camera camera)
    {
        if (go == null || camera == null || !go.activeInHierarchy)
            return null;
        float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
        bool any = false;
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(false))
        {
            if (!Opaque(r))
                continue;
            Bounds b = r.localBounds;
            Matrix4x4 m = r.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                Vector3 c = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                Vector3 s = camera.WorldToScreenPoint(m.MultiplyPoint3x4(c));
                if (s.z <= 0f)
                    continue;
                x0 = Mathf.Min(x0, s.x);
                y0 = Mathf.Min(y0, s.y);
                x1 = Mathf.Max(x1, s.x);
                y1 = Mathf.Max(y1, s.y);
                any = true;
            }
        }
        return any ? new Rect(x0, y0, x1 - x0, y1 - y0) : (Rect?)null;
    }

    /// <summary>True for a renderer drawn on, opaque (every material in the opaque queues) and with a mesh of its own (a MeshRenderer).</summary>
    private static bool Opaque(Renderer r)
    {
        if (r == null || !r.enabled || !r.gameObject.activeInHierarchy || !(r is MeshRenderer))
            return false;
        Material[] mats = r.sharedMaterials;
        if (mats == null || mats.Length == 0)
            return false;
        foreach (Material m in mats)
            if (m == null || m.renderQueue >= 2450)
                return false;
        return true;
    }

    /// <summary>One mesh's triangles in the world, with its owner's name.</summary>
    private sealed class Tris
    {
        public string Name;
        public readonly List<Vector3> A = new List<Vector3>(), B = new List<Vector3>(), C = new List<Vector3>();
        public readonly List<Rect> Screen = new List<Rect>();
    }

    /// <summary>
    /// The share (0 to 1) of <paramref name="go"/>'s face that faces
    /// <paramref name="camera"/> and that something else drawn opaque hides
    /// (see the class), the number of points weighed, and who hides it (each
    /// name with its share, the largest first; and the meshes that could not
    /// be read), and how many meshes could not be read (a measure with any is
    /// not to be trusted). Null share when nothing of it shows.
    /// </summary>
    public static (float? share, int points, string by, int unread) Covered(GameObject go, Camera camera)
    {
        if (go == null || camera == null)
            return (null, 0, "no thing or no camera", 0);
        Vector3 eye = camera.transform.position;
        var unread = new List<string>();
        // The thing's own faces: the points to look at and what hides its own lower faces.
        var self = new List<Tris>();
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(false))
            if (Opaque(r) && TryTris(r, camera, out Tris t, unread))
                self.Add(t);
        Rect? picture = Of(go, camera);
        if (self.Count == 0 || picture == null)
            return (null, 0, $"nothing of it on the screen ({self.Count} opaque meshes read, picture {(picture != null ? "on" : "off")} the screen)" + Unread(unread), unread.Count);
        var points = new List<(Vector3 p, float w)>();
        foreach (Tris t in self)
            for (int i = 0; i < t.A.Count; i++)
                Spread(t.A[i], t.B[i], t.C[i], eye, points);
        // Only what it shows: a shader may cut part of its model away (the shut folder's left board, DeskPaperDepth.ClipLeftOf),
        // so the points are kept to its own click boxes (the shut folder's box covers the board's side alone; a paper's, its sheet).
        var boxes = go.GetComponentsInChildren<BoxCollider>(false).Where(b => b.enabled).ToList();
        if (boxes.Count > 0)
            points = points.Where(q => boxes.Any(b => InBox(b, q.p, 0.005f))).ToList();
        if (points.Count == 0)
            return (null, 0, "no face toward the camera" + Unread(unread), unread.Count);
        float far = points.Max(q => (q.p - eye).magnitude);
        // What else is drawn opaque there: every renderer the camera draws whose picture meets the thing's and that is nearer.
        var others = new List<Tris>();
        foreach (Renderer r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
        {
            if (!Opaque(r) || r.transform.IsChildOf(go.transform) || (camera.cullingMask & (1 << r.gameObject.layer)) == 0
                || r.GetComponentInParent<DeskDocument>() != null || r.GetComponentInParent<DeskRulebook>() != null)
                continue;
            Bounds b = r.bounds;
            if (Vector3.Distance(eye, b.ClosestPoint(eye)) > far || !Meets(ScreenRect(b, camera), picture.Value))
                continue;
            if (TryTris(r, camera, out Tris t, unread))
                others.Add(t);
        }
        float shown = 0f, hidden = 0f;
        var by = new Dictionary<string, float>();
        foreach ((Vector3 p, float w) in points)
        {
            Vector3 way = p - eye;
            float length = way.magnitude;
            if (length < 1e-4f)
                continue;
            Vector3 dir = way / length;
            Vector3 sp = camera.WorldToScreenPoint(p);
            float before = length - 0.001f; // a millimetre short of the point: its own face never hides it
            if (Hit(self, eye, dir, before, sp) != null)
                continue; // the thing's own upper faces hide it: not a point it shows alone
            shown += w;
            string hider = Hit(others, eye, dir, before, sp);
            if (hider == null)
                continue;
            hidden += w;
            by[hider] = (by.TryGetValue(hider, out float had) ? had : 0f) + w;
        }
        if (shown <= 0f)
            return (null, points.Count, "nothing of it shows" + Unread(unread), unread.Count);
        string names = string.Join(", ", by.OrderByDescending(kv => kv.Value).Take(6).Select(kv => FormattableString.Invariant($"{kv.Key} {100f * kv.Value / shown:0.0}%")));
        return (hidden / shown, points.Count, (names.Length > 0 ? names : "nothing over it") + Unread(unread), unread.Count);
    }

    /// <summary>True when <paramref name="p"/> lies inside <paramref name="box"/> grown by <paramref name="grow"/> metres every way.</summary>
    private static bool InBox(BoxCollider box, Vector3 p, float grow)
    {
        Transform t = box.transform;
        Vector3 centre = t.TransformPoint(box.center), d = p - centre;
        for (int axis = 0; axis < 3; axis++)
        {
            Vector3 unit = axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;
            Vector3 half = t.TransformVector(unit * (box.size[axis] / 2f));
            float length = half.magnitude;
            if (length < 1e-6f)
            {
                if (Mathf.Abs(Vector3.Dot(d, t.TransformDirection(unit))) > grow)
                    return false;
                continue;
            }
            if (Mathf.Abs(Vector3.Dot(d, half / length)) > length + grow)
                return false;
        }
        return true;
    }

    private static string Unread(List<string> unread) => unread.Count > 0 ? "; unread meshes: " + string.Join(", ", unread.Distinct().Take(8)) : string.Empty;

    /// <summary>The first of <paramref name="sets"/> a ray from <paramref name="eye"/> along <paramref name="dir"/> meets before <paramref name="before"/> metres (its owner's name), or null. Each triangle's screen rect is tried first (<paramref name="sp"/>: the point's screen place).</summary>
    private static string Hit(List<Tris> sets, Vector3 eye, Vector3 dir, float before, Vector3 sp)
    {
        foreach (Tris t in sets)
            for (int i = 0; i < t.A.Count; i++)
            {
                Rect s = t.Screen[i];
                if (sp.x < s.xMin || sp.x > s.xMax || sp.y < s.yMin || sp.y > s.yMax)
                    continue;
                if (Ray(eye, dir, t.A[i], t.B[i], t.C[i], out float d) && d > 1e-4f && d < before)
                    return t.Name;
            }
        return null;
    }

    /// <summary>Möller and Trumbore's ray and triangle test: true with <paramref name="d"/> the distance along the ray where it meets the triangle (either side).</summary>
    private static bool Ray(Vector3 o, Vector3 dir, Vector3 a, Vector3 b, Vector3 c, out float d)
    {
        d = 0f;
        Vector3 e1 = b - a, e2 = c - a, p = Vector3.Cross(dir, e2);
        float det = Vector3.Dot(e1, p);
        if (Mathf.Abs(det) < 1e-12f)
            return false;
        float inv = 1f / det;
        Vector3 s = o - a;
        float u = Vector3.Dot(s, p) * inv;
        if (u < 0f || u > 1f)
            return false;
        Vector3 q = Vector3.Cross(s, e1);
        float v = Vector3.Dot(dir, q) * inv;
        if (v < 0f || u + v > 1f)
            return false;
        d = Vector3.Dot(e2, q) * inv;
        return true;
    }

    /// <summary>Points about every 6 mm over a triangle that faces <paramref name="eye"/>, each weighed by the area it stands for.</summary>
    private static void Spread(Vector3 a, Vector3 b, Vector3 c, Vector3 eye, List<(Vector3, float)> into)
    {
        Vector3 n = Vector3.Cross(b - a, c - a);
        float area = n.magnitude / 2f;
        if (area < 1e-7f || Vector3.Dot(n, eye - (a + b + c) / 3f) <= 0f)
            return;
        int k = Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(2f * area) / 0.006f), 1, 60);
        int count = 0;
        int start = into.Count;
        for (int i = 0; i < k; i++)
            for (int j = 0; j < k - i; j++)
            {
                float u = (i + 1f / 3f) / k, v = (j + 1f / 3f) / k;
                into.Add((a + (b - a) * u + (c - a) * v, 0f));
                count++;
            }
        float w = area / count;
        for (int i = start; i < into.Count; i++)
            into[i] = (into[i].Item1, w);
    }

    /// <summary>A renderer's triangles in the world (its mesh through its matrix) with each one's screen rect; false (named in <paramref name="unread"/>) when its mesh cannot be read.</summary>
    private static bool TryTris(Renderer r, Camera camera, out Tris tris, List<string> unread)
    {
        tris = null;
        MeshFilter filter = r.GetComponent<MeshFilter>();
        Mesh mesh = filter != null ? filter.sharedMesh : null;
        if (mesh == null)
            return false;
        Vector3[] v = null;
        int[] t = null;
        try
        {
            if (mesh.isReadable)
            {
                v = mesh.vertices;
                t = mesh.triangles;
            }
            else
                FromTheGpu(mesh, out v, out t);
        }
        catch (Exception)
        {
            v = null;
        }
        if (v == null || t == null || v.Length == 0)
        {
            unread.Add(r.name);
            return false;
        }
        Matrix4x4 m = r.localToWorldMatrix;
        var world = new Vector3[v.Length];
        var screen = new Vector3[v.Length];
        for (int i = 0; i < v.Length; i++)
        {
            world[i] = m.MultiplyPoint3x4(v[i]);
            screen[i] = camera.WorldToScreenPoint(world[i]);
        }
        tris = new Tris { Name = Path(r.transform) };
        for (int i = 0; i + 2 < t.Length; i += 3)
        {
            Vector3 sa = screen[t[i]], sb = screen[t[i + 1]], sc = screen[t[i + 2]];
            if (sa.z <= 0f && sb.z <= 0f && sc.z <= 0f)
                continue;
            float x0 = Mathf.Min(sa.x, Mathf.Min(sb.x, sc.x)), x1 = Mathf.Max(sa.x, Mathf.Max(sb.x, sc.x));
            float y0 = Mathf.Min(sa.y, Mathf.Min(sb.y, sc.y)), y1 = Mathf.Max(sa.y, Mathf.Max(sb.y, sc.y));
            if (sa.z <= 0f || sb.z <= 0f || sc.z <= 0f)
            {
                x0 = y0 = float.MinValue;
                x1 = y1 = float.MaxValue;
            }
            tris.A.Add(world[t[i]]);
            tris.B.Add(world[t[i + 1]]);
            tris.C.Add(world[t[i + 2]]);
            tris.Screen.Add(new Rect(x0 - 1f, y0 - 1f, x1 - x0 + 2f, y1 - y0 + 2f));
        }
        return true;
    }

    /// <summary>
    /// A mesh Unity keeps on the GPU only (imported with Read/Write off, or
    /// built and uploaded: the bake's shell, the lamps, the folder's and the
    /// papers' own meshes): its positions and triangles read back from its
    /// vertex and index buffers (mesh.vertices would log an error and give
    /// nothing in play mode). Null when its positions are not 32-bit floats.
    /// </summary>
    private static void FromTheGpu(Mesh mesh, out Vector3[] vertices, out int[] triangles)
    {
        vertices = null;
        triangles = null;
        int stream = mesh.GetVertexAttributeStream(VertexAttribute.Position);
        if (stream < 0 || mesh.GetVertexAttributeFormat(VertexAttribute.Position) != VertexAttributeFormat.Float32)
            return;
        int offset = mesh.GetVertexAttributeOffset(VertexAttribute.Position);
        using (GraphicsBuffer vb = mesh.GetVertexBuffer(stream))
        {
            var bytes = new byte[vb.count * vb.stride];
            vb.GetData(bytes);
            vertices = new Vector3[vb.count];
            for (int i = 0; i < vb.count; i++)
            {
                int at = i * vb.stride + offset;
                vertices[i] = new Vector3(BitConverter.ToSingle(bytes, at), BitConverter.ToSingle(bytes, at + 4), BitConverter.ToSingle(bytes, at + 8));
            }
        }
        int[] indices;
        using (GraphicsBuffer ib = mesh.GetIndexBuffer())
        {
            indices = new int[ib.count];
            if (ib.stride == 2)
            {
                var shorts = new ushort[ib.count];
                ib.GetData(shorts);
                for (int i = 0; i < shorts.Length; i++)
                    indices[i] = shorts[i];
            }
            else
                ib.GetData(indices);
        }
        var tris = new List<int>();
        for (int s = 0; s < mesh.subMeshCount; s++)
        {
            SubMeshDescriptor d = mesh.GetSubMesh(s);
            if (d.topology != MeshTopology.Triangles)
                continue;
            for (int k = d.indexStart; k < d.indexStart + d.indexCount && k < indices.Length; k++)
                tris.Add(indices[k] + d.baseVertex);
        }
        triangles = tris.ToArray();
    }

    /// <summary>The screen rect of world bounds (a huge rect when part of them is behind the camera).</summary>
    private static Rect ScreenRect(Bounds b, Camera camera)
    {
        float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
        for (int i = 0; i < 8; i++)
        {
            Vector3 s = camera.WorldToScreenPoint(new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z));
            if (s.z <= 0f)
                return new Rect(-1e6f, -1e6f, 2e6f, 2e6f);
            x0 = Mathf.Min(x0, s.x);
            y0 = Mathf.Min(y0, s.y);
            x1 = Mathf.Max(x1, s.x);
            y1 = Mathf.Max(y1, s.y);
        }
        return new Rect(x0, y0, x1 - x0, y1 - y0);
    }

    private static bool Meets(Rect a, Rect b) => a.xMin <= b.xMax && b.xMin <= a.xMax && a.yMin <= b.yMax && b.yMin <= a.yMax;

    /// <summary>A transform's path from its root (the hider's name in the measure).</summary>
    private static string Path(Transform t)
    {
        string p = t.name;
        for (int i = 0; i < 3 && t.parent != null; i++)
        {
            t = t.parent;
            p = t.name + "/" + p;
        }
        return p;
    }
}
#endif
