using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The builders' UI kit asset (Assets/Data/UI/UiKit_Default.asset): created
/// with the kit's fonts when missing, and its sprite list refreshed from the
/// kit's manifest (UiKitImporter) on every build, so a sprite added to the kit
/// reaches the scenes with the next build; the fonts, inks and scales a
/// designer set are kept.
/// </summary>
public static class UiKitAssets
{
    /// <summary>The kit asset's path.</summary>
    public const string Path = "Assets/Data/UI/UiKit_Default.asset";

    /// <summary>The kit asset, created or refreshed (null with an error when the kit or its fonts are not in the project).</summary>
    public static UiKitSO Ensure()
    {
        UiKitImporter.Manifest manifest = UiKitImporter.Load();
        if (manifest == null)
        {
            Debug.LogError("[TimeDesk] The UI kit's manifest (Assets/Art/UI/Kit/kit_manifest.json) is missing: merge the UI kit (feat/ui-kit-sprites). The UI keeps its flat look.");
            return null;
        }

        UiKitSO kit = AssetDatabase.LoadAssetAtPath<UiKitSO>(Path);
        if (kit == null)
        {
            PlaceholderPng.EnsureFolderTree("Assets/Data/UI");
            kit = ScriptableObject.CreateInstance<UiKitSO>();
            AssetDatabase.CreateAsset(kit, Path);
        }

        UiKitFonts.Build();
        kit.labelFont = kit.labelFont != null ? kit.labelFont : AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitFonts.Label);
        kit.readoutFont = kit.readoutFont != null ? kit.readoutFont : AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitFonts.Readout);
        kit.mastheadFont = kit.mastheadFont != null ? kit.mastheadFont : AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitFonts.Masthead);
        kit.bodyFont = kit.bodyFont != null ? kit.bodyFont : AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitFonts.Body);
        kit.bodyBoldFont = kit.bodyBoldFont != null ? kit.bodyBoldFont : AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitFonts.BodyBold);

        // The type scale: every role present (a designer's values kept, a new role takes its default).
        foreach ((KitText kind, float share, float min, float max) in KitTypeScale.Defaults)
            if (!kit.typeScale.Exists(s => s != null && s.kind == kind))
                kit.typeScale.Add(new UiKitSO.TextStyle { kind = kind, share = share, min = min, max = max });

        var sprites = new List<UiKitSO.KitSprite>();
        foreach (UiKitImporter.Entry entry in manifest.sprites)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(UiKitImporter.Folder + entry.name + ".png");
            if (sprite == null)
                Debug.LogError($"[TimeDesk] The UI kit's sprite {entry.name} is in the manifest but not imported as a sprite ({UiKitImporter.Folder}{entry.name}.png). Reimport the kit folder.");
            else
                sprites.Add(new UiKitSO.KitSprite { name = entry.name, sprite = sprite });
        }
        sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        kit.sprites = sprites;
        kit.ClearCache();
        EditorUtility.SetDirty(kit);
        return kit;
    }
}
