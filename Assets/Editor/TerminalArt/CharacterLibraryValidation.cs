using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Checks every required character key through the actual Unity import and Resources routes.</summary>
public static class CharacterLibraryValidation
{
    [Serializable] private sealed class SourceTask { public string name; public string[] produces; }
    [Serializable] private sealed class Queue { public SourceTask[] items; }
    [Serializable] private sealed class Coverage { public string[] requiredFlat; }
    [Serializable] private sealed class Report
    {
        public int expectedKeys;
        public int importedKeys;
        public int resourceKeys;
        public List<string> errors = new List<string>();
    }

    [MenuItem("Tools/Terminal Art/Completion/Validate Character Library")]
    public static void Validate() => Run();

    public static void ValidateInBatch()
    {
        Report report = Run();
        EditorApplication.Exit(report.errors.Count == 0 ? 0 : 1);
    }

    private static Report Run()
    {
        string root = Path.GetDirectoryName(Application.dataPath);
        string completion = Path.Combine(root, "ArtDeliverables/TimeDesk/Characters/Completion");
        var required = new HashSet<string>(JsonUtility.FromJson<Coverage>(File.ReadAllText(
            Path.Combine(root, "ArtDeliverables/TimeDesk/Characters/Production/coverage.json"))).requiredFlat);
        Queue queue = JsonUtility.FromJson<Queue>("{\"items\":" + File.ReadAllText(Path.Combine(completion, "prompt-queue.json")) + "}");
        foreach (SourceTask task in queue.items)
            foreach (string key in task.produces)
                required.Add(key);
        var report = new Report { expectedKeys = required.Count };
        if (required.Count != 944)
            report.errors.Add($"Expected 944 required keys; found {required.Count}.");

        foreach (string key in required)
        {
            string path = CharacterArt.AssetFolder + "/" + key + ".png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                report.errors.Add($"Missing imported sprite: {key}");
                continue;
            }
            report.importedKeys++;
            if (sprite.rect.width != LookCanvas.Width || sprite.rect.height != LookCanvas.Height)
                report.errors.Add($"Wrong sprite canvas: {key} ({sprite.rect})");
            if (Mathf.Abs(sprite.pixelsPerUnit - LookCanvas.Height) > 0.01f ||
                Mathf.Abs(sprite.pivot.x - LookCanvas.CenterX) > 0.01f ||
                Mathf.Abs(sprite.pivot.y - (LookCanvas.Height - LookCanvas.Feet)) > 0.01f)
                report.errors.Add($"Wrong registration pivot or pixels per unit: {key}");

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single ||
                settings.spriteMeshType != SpriteMeshType.FullRect || !importer.isReadable || !importer.mipmapEnabled ||
                importer.filterMode != FilterMode.Trilinear || !importer.alphaIsTransparency ||
                importer.textureCompression != TextureImporterCompression.CompressedHQ || importer.crunchedCompression)
                report.errors.Add($"Importer differs from the approved character contract: {key}");

            Sprite resource = Resources.Load<Sprite>(CharacterArt.ResourcesFolder + "/" + key);
            if (resource == null)
                report.errors.Add($"Runtime Resources lookup failed: {key}");
            else
            {
                report.resourceKeys++;
                // Avoid retaining the entire readable library during the audit.
                Resources.UnloadAsset(resource.texture);
            }
        }
        Directory.CreateDirectory(completion);
        File.WriteAllText(Path.Combine(completion, "unity-import-validation.json"), JsonUtility.ToJson(report, true));
        Debug.Log($"[CharacterLibraryValidation] {report.importedKeys}/{report.expectedKeys} imported; {report.resourceKeys} Resources keys; {report.errors.Count} errors.");
        return report;
    }
}
