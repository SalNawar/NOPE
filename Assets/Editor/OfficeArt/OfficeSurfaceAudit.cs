using System.IO;
using System.Linq;
using System.Text;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

// Read-only authoring diagnostic. Records actual imported normals and UVs rather
// than inferring their direction from a Game-view screenshot.
public static class OfficeSurfaceAudit
{
    [MenuItem("Tools/Office Art/Audit Surfaces")]
    public static void Audit()
    {
        var output=new StringBuilder();
        var root=GameObject.Find("ImportedOfficeDress");
        foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
        {
            if(!filter.transform.IsChildOf(root.transform.Find("Hall floor")))continue;
            using var data=Mesh.AcquireReadOnlyMeshData(filter.sharedMesh);
            using var normals=new NativeArray<Vector3>(data[0].vertexCount,Allocator.Temp);
            using var uv=new NativeArray<Vector2>(data[0].vertexCount,Allocator.Temp);
            data[0].GetNormals(normals);data[0].GetUVs(0,uv);
            var r=filter.GetComponent<Renderer>();
            output.AppendLine($"{filter.transform.parent.parent.name}/{filter.name}: bounds {r.bounds} material {string.Join(",",r.sharedMaterials.Select(m=>m.name))}");
            for(int n=0;n<Mathf.Min(4,normals.Length);n++)
                output.AppendLine($" normalWS={filter.transform.TransformDirection(normals[n])} UV={uv[n]}");
        }
        File.WriteAllText("ArtDeliverables/TimeDesk/ImportedOffice/surface_audit.txt",output.ToString());
    }
}
