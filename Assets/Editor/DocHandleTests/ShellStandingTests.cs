using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// FOLD-01 round 2 (the verifier's F1; Saleh's playtest of 2026-10-28, point
/// 4: "You move the folder and it snaps somewhere else"): the rest keeps clear
/// of what the painted desk's shell draws beside the mat
/// (DeskPaintedMap.ShellStanding, DeskLayout.Standing) and of its back step's
/// lip (DeskPaintedMap.BackStep). These are numbers measured on the bake, so
/// this test measures the bake's own meshes again: every part of the shell and
/// the lamps standing more than 3.5 mm over the mat, from the reading view's
/// near edge to the back step, must lie inside one of them, and the mat itself
/// must stay free. A new bake that moves a rim, a lamp or a frame fails here.
/// Reads the prefab's meshes, so it runs in Unity's EditMode suite only
/// (NeedsEditor).
/// </summary>
public class ShellStandingTests
{
    private const string Prefab = "Assets/Art/Office/DeskPainted/Prefabs/DeskPainted.prefab";

    /// <summary>The height map's cell (metres) and the part of the desk it covers (the bake's frame: x right, y away from the chair).</summary>
    private const float Cell = 0.0025f, X0 = -1.05f, X1 = 1.25f, Y0 = -0.62f, Y1 = 0.60f;

    /// <summary>How high over the mat a part must stand to draw over a thing resting on it (metres).</summary>
    private const float Standing = 0.0035f;

    [Test, Category("NeedsEditor")]
    public void EveryPartOfTheShellStandingBesideTheMat_LiesInsideItsKeepOuts_AndTheMatStaysFree()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
        Assert.IsNotNull(prefab, "the painted desk's prefab");
        float plane = new SerializedObject(prefab.GetComponent<DeskPainted>()).FindProperty("paperPlane").floatValue;
        int nx = Mathf.CeilToInt((X1 - X0) / Cell), ny = Mathf.CeilToInt((Y1 - Y0) / Cell);
        var top = new float[nx * ny];
        for (int i = 0; i < top.Length; i++)
            top[i] = float.NegativeInfinity;
        int meshes = 0;
        foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(true))
        {
            string path = PathOf(filter.transform, prefab.transform);
            if (filter.sharedMesh == null || !(path.StartsWith("Shell/" + DeskPaintedMap.Shell) || path.StartsWith("Lamps/")))
                continue;
            Vector3[] v = filter.sharedMesh.vertices;
            int[] t = filter.sharedMesh.triangles;
            Matrix4x4 m = prefab.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            var w = v.Select(p => m.MultiplyPoint3x4(p)).ToArray();
            for (int i = 0; i + 2 < t.Length; i += 3)
                Triangle(top, nx, ny, w[t[i]], w[t[i + 1]], w[t[i + 2]]);
            meshes++;
        }
        Assert.Greater(meshes, 1, "the shell and the lamps were read");

        var rects = DeskPaintedMap.ShellStanding.Select(r => (r.x0, r.y0, r.x1, r.y1)).ToList();
        var outside = new List<string>();
        int raised = 0, onMat = 0;
        for (int j = 0; j < ny; j++)
            for (int i = 0; i < nx; i++)
            {
                if (!(top[j * nx + i] - plane > Standing))
                    continue;
                float x = X0 + (i + 0.5f) * Cell, y = Y0 + (j + 0.5f) * Cell;
                if (y + Cell / 2f > DeskPaintedMap.BackStep + 1e-5f)
                    continue; // the back step and its lip: the rest's area ends where they begin
                raised++;
                if (Mathf.Abs(x) < 0.72f)
                    onMat++;
                // Half a cell of grace: the cell's centre may lie just past a rectangle its part touches.
                if (!rects.Any(r => x >= r.x0 - Cell && x <= r.x1 + Cell && y >= r.y0 - Cell && y <= r.y1 + Cell))
                    outside.Add(FormattableString(x, y, top[j * nx + i] - plane));
            }
        Assert.Greater(raised, 1000, "the rail and the pods stand there");
        Assert.AreEqual(0, outside.Count, $"{outside.Count} cells of the shell stand over the mat outside DeskPaintedMap.ShellStanding and short of the back step, e.g. {string.Join("; ", outside.Take(8))}");
        Assert.AreEqual(0, onMat, "nothing of the shell stands over the mat itself, short of the back step's lip");
        foreach ((float x0, float y0, float x1, float y1) in rects)
            Assert.IsTrue(x1 <= -0.73f || x0 >= 0.73f, $"a keep-out ({x0}, {y0}, {x1}, {y1}) leaves the mat (x -0.73 to 0.73) free");
    }

    private static string FormattableString(float x, float y, float h) =>
        System.FormattableString.Invariant($"({x:0.0000}, {y:0.0000}) {h * 1000f:0.0} mm high");

    private static string PathOf(Transform t, Transform root)
    {
        string p = t.name;
        while (t.parent != null && t.parent != root)
        {
            t = t.parent;
            p = t.name + "/" + p;
        }
        return p;
    }

    /// <summary>Marks the cells under a triangle (Unity's x, z on the desk, y up) with its height there, and its edges (a wall stands on its edges).</summary>
    private static void Triangle(float[] top, int nx, int ny, Vector3 a, Vector3 b, Vector3 c)
    {
        Edge(top, nx, ny, a, b);
        Edge(top, nx, ny, b, c);
        Edge(top, nx, ny, c, a);
        float det = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
        if (Mathf.Abs(det) < 1e-10f)
            return;
        int i0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.x, Mathf.Min(b.x, c.x)) - X0) / Cell)), i1 = Mathf.Min(nx - 1, Mathf.FloorToInt((Mathf.Max(a.x, Mathf.Max(b.x, c.x)) - X0) / Cell));
        int j0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.z, Mathf.Min(b.z, c.z)) - Y0) / Cell)), j1 = Mathf.Min(ny - 1, Mathf.FloorToInt((Mathf.Max(a.z, Mathf.Max(b.z, c.z)) - Y0) / Cell));
        for (int j = j0; j <= j1; j++)
            for (int i = i0; i <= i1; i++)
            {
                float x = X0 + (i + 0.5f) * Cell, z = Y0 + (j + 0.5f) * Cell;
                float l1 = ((b.z - c.z) * (x - c.x) + (c.x - b.x) * (z - c.z)) / det;
                float l2 = ((c.z - a.z) * (x - c.x) + (a.x - c.x) * (z - c.z)) / det;
                float l3 = 1f - l1 - l2;
                if (l1 >= -1e-4f && l2 >= -1e-4f && l3 >= -1e-4f)
                    Mark(top, nx, i, j, l1 * a.y + l2 * b.y + l3 * c.y);
            }
    }

    private static void Edge(float[] top, int nx, int ny, Vector3 a, Vector3 b)
    {
        int n = Mathf.Max(1, Mathf.CeilToInt(new Vector2(b.x - a.x, b.z - a.z).magnitude / (Cell * 0.5f)));
        for (int k = 0; k <= n; k++)
        {
            Vector3 p = Vector3.Lerp(a, b, k / (float)n);
            int i = Mathf.FloorToInt((p.x - X0) / Cell), j = Mathf.FloorToInt((p.z - Y0) / Cell);
            if (i >= 0 && j >= 0 && i < nx && j < ny)
                Mark(top, nx, i, j, p.y);
        }
    }

    private static void Mark(float[] top, int nx, int i, int j, float h)
    {
        if (h > top[j * nx + i])
            top[j * nx + i] = h;
    }
}
