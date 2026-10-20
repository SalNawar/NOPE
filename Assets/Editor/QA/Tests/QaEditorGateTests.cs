using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The QA gates that need the editor (category NeedsEditor: Unity's EditMode
/// suite runs them; the offline runner skips them): the missing-script gate
/// (P0-B "the missing-script gate test"), broken materials and retired
/// objects on a scene of their own, every scenario file read with its ops
/// known, and the check registry finding QA-RIG's checks.
/// </summary>
public class QaEditorGateTests
{
    private const string TempFolder = "Assets/__QaRigTemp";

    /// <summary>A scene file holding one object whose script does not exist (a GUID no asset has).</summary>
    private const string MissingScriptScene =
        "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n" +
        "--- !u!1 &100\nGameObject:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n" +
        "  serializedVersion: 6\n  m_Component:\n  - component: {fileID: 101}\n  - component: {fileID: 102}\n  m_Layer: 0\n  m_Name: QaMissing\n" +
        "  m_TagString: Untagged\n  m_Icon: {fileID: 0}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: 1\n" +
        "--- !u!4 &101\nTransform:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n" +
        "  m_GameObject: {fileID: 100}\n  serializedVersion: 2\n  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n  m_LocalPosition: {x: 0, y: 0, z: 0}\n" +
        "  m_LocalScale: {x: 1, y: 1, z: 1}\n  m_ConstrainProportionsScale: 0\n  m_Children: []\n  m_Father: {fileID: 0}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n" +
        "--- !u!114 &102\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n" +
        "  m_GameObject: {fileID: 100}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: 0badc0de0badc0de0badc0de0badc0de, type: 3}\n" +
        "  m_Name: \n  m_EditorClassIdentifier: \n" +
        "--- !u!1660057539 &9223372036854775807\nSceneRoots:\n  m_ObjectHideFlags: 0\n  m_Roots:\n  - {fileID: 101}\n";

    [Test, Category("NeedsEditor")]
    public void MissingScriptGate_FindsTheObject()
    {
        Directory.CreateDirectory(TempFolder);
        string path = TempFolder + "/QaMissingScript.unity";
        File.WriteAllText(path, MissingScriptScene);
        AssetDatabase.ImportAsset(path);
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            QaSceneScan.Findings f = QaSceneScan.Scan(scene);
            Assert.AreEqual(1, f.MissingScripts.Count, string.Join("; ", f.MissingScripts));
            StringAssert.EndsWith("QaMissing", f.MissingScripts[0]);
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
            AssetDatabase.DeleteAsset(TempFolder);
        }
    }

    [Test, Category("NeedsEditor")]
    public void IntegrityScan_FindsNullMaterials_AndRetiredObjects()
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var broken = new GameObject("NoMaterial", typeof(MeshFilter), typeof(MeshRenderer));
            broken.GetComponent<MeshRenderer>().sharedMaterials = new Material[1];
            SceneManager.MoveGameObjectToScene(broken, scene);
            var office = new GameObject("Office");
            new GameObject("GateLever").transform.SetParent(office.transform);
            SceneManager.MoveGameObjectToScene(office, scene);
            QaSceneScan.Findings f = QaSceneScan.Scan(scene);
            Assert.AreEqual(1, f.BrokenMaterials.Count);
            CollectionAssert.AreEqual(new[] { scene.name + ": Office/GateLever" }, f.RetiredPresent);
            Assert.IsEmpty(f.MissingScripts);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    [Test, Category("NeedsEditor")]
    public void EveryScenario_Reads_WithKnownOps()
    {
        string folder = Path.Combine(SoFieldReadersTests.ProjectRoot(), "tools", "qa", "scenarios");
        string[] files = Directory.GetFiles(folder, "*.json");
        Assert.IsNotEmpty(files);
        foreach (string file in files)
        {
            QaScenario s = QaScenario.Load(file);
            Assert.IsNotEmpty(s.steps, file);
            CollectionAssert.IsEmpty(QaRunner.UnknownOps(s), file);
            foreach (QaStep step in s.steps.Where(x => x.op == "drag"))
                Assert.DoesNotThrow(() => QaRunner.ParseKind(step.path), file);
        }
    }

    [Test, Category("NeedsEditor")]
    public void Registry_FindsQaRigsChecks_WithNoRegistrationFault()
    {
        var checks = GateChecks.All();
        Assert.IsTrue(checks.Any(c => c.Info.Id == "qa.quality.parity"));
        Assert.IsTrue(checks.Any(c => c.Info.Id == "qa.bp.smoke" && c.Info.Kind == GateKind.BuiltPlayer && c.Info.DemoOnly));
        CollectionAssert.IsEmpty(GateChecks.Problems(checks).Select(p => p.ToString()));
    }
}
