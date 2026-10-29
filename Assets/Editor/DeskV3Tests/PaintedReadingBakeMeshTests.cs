using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// DESK-01 and DESK-02 on the installed meshes (bake v4): the shell imports
/// with two submeshes (its v3 faces, then its level faces in the reading
/// frames and the gap strips), and the two strips close the mat's side gaps
/// (the black slit at the mat's right edge): level faces 0.2 mm under the
/// mat's lowest point (y 1.0688 in the prefab's frame), from under the mat's
/// edge to the rail's and the trays' inner faces, the mat's length. Reads the
/// prefab's meshes, so it runs in Unity's EditMode suite only (NeedsEditor).
/// </summary>
public class PaintedReadingBakeMeshTests
{
    private const string Prefab = "Assets/Art/Office/DeskPainted/Prefabs/DeskPainted.prefab";

    [Test, Category("NeedsEditor")]
    public void TheShell_HasTwoSubmeshes_AndTheStripsCloseTheMatsSideGaps()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
        Assert.IsNotNull(prefab, "the painted desk's prefab");
        MeshFilter shell = prefab.GetComponentsInChildren<MeshFilter>(true).First(f => f.name == DeskPaintedMap.Shell);
        Mesh mesh = shell.sharedMesh;
        Assert.AreEqual(2, mesh.subMeshCount, "v3's faces, then the level faces and the strips");
        Material[] mats = shell.GetComponent<MeshRenderer>().sharedMaterials;
        Assert.AreEqual(new[] { "DeskPainted_Shell", "DeskPainted_ShellTop" }, mats.Select(m => m.name).ToArray());

        Matrix4x4 m = prefab.transform.worldToLocalMatrix * shell.transform.localToWorldMatrix;
        Vector3[] v = mesh.vertices.Select(p => m.MultiplyPoint3x4(p)).ToArray();
        int[] top = mesh.GetTriangles(1);
        foreach (float side in new[] { 1f, -1f })
        {
            // the strip's area along the gap: every point in it lies on a level triangle of slot 1 at the strip's height
            float x = side > 0 ? 0.7375f : -0.736f;
            foreach (float z in new[] { -0.50f, -0.2f, 0.1f, 0.4f })
            {
                bool hit = false;
                for (int i = 0; i + 2 < top.Length && !hit; i += 3)
                {
                    Vector3 a = v[top[i]], b = v[top[i + 1]], c = v[top[i + 2]];
                    if (Mathf.Abs(a.y - 1.0688f) > 0.0005f || Mathf.Abs(b.y - 1.0688f) > 0.0005f || Mathf.Abs(c.y - 1.0688f) > 0.0005f)
                        continue;
                    hit = Inside(new Vector2(x, z), new Vector2(a.x, a.z), new Vector2(b.x, b.z), new Vector2(c.x, c.z));
                }
                Assert.IsTrue(hit, $"the {(side > 0 ? "right" : "left")} strip covers the gap at ({x}, {z})");
            }
        }
    }

    private static bool Inside(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Cross(p, a, b), d2 = Cross(p, b, c), d3 = Cross(p, c, a);
        bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
        return !(neg && pos);
    }

    private static float Cross(Vector2 p, Vector2 a, Vector2 b) => (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
}
