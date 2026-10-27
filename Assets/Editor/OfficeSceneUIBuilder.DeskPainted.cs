using UnityEditor;
using UnityEngine;

/// <summary>
/// The office builder's painted desk (desk v3, Saleh 2026-10-20 on
/// mock_office.png: "yes but they need to slide"; DESK-ART, UNITY_PLAN 8).
/// Part of <see cref="OfficeSceneUIBuilder"/>. Owner: DESK-ART.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>
    /// The painted desk (DeskPaintedImporter's prefab, Tools > TimeDesk >
    /// Import Painted Desk) as an instance under the office root, where the
    /// office binder finds it (OfficeSceneBinder.paintedDesk): the binder puts
    /// it on the desk's frame and installs it over the art's desk when the
    /// plan's DeskArt.Painted toggle is on (DeskLayoutSettings.paintedDesk),
    /// else hides it. Null, with a warning, while the prefab is not built.
    /// Idempotent (a fresh instance each build).
    /// </summary>
    private static DeskPainted BuildPaintedDesk(Transform office)
    {
        DestroyChildIfPresent(office, "DeskPainted");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DeskPaintedImporter.PrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[TimeDesk] {DeskPaintedImporter.PrefabPath} is missing: the office keeps the art's desk. Run Tools > TimeDesk > Import Painted Desk.");
            return null;
        }
        var desk = (GameObject)PrefabUtility.InstantiatePrefab(prefab, office);
        desk.name = "DeskPainted";
        return desk.GetComponent<DeskPainted>();
    }
}
