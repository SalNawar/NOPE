using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The hall slots' art tools (the hall slots spec; Tools > Terminal Art > Hall Slots):
/// Export Templates crops each slot's region from the hall painting at
/// daylight and at night (the painting's own night tint) with its paintable
/// box, the window glass hatched and the rest dimmed, to
/// ArtDeliverables/TimeDesk/HallSlots/templates/&lt;slot&gt;.png, with the
/// whole-canvas guide, a blank canvas and a manifest of every slot's box and
/// variants; Validate checks the slots' asset (HallSlotPick.Problems against
/// the content's nations) and reports the art files present; Capture
/// Combinations (in play, the office loaded) sets the hall's variables
/// through HallSlotsDev to each of <see cref="Combos"/>, every slot's
/// stand-in shown, and saves a screenshot of each.
/// </summary>
public static class HallSlotsTools
{
    /// <summary>The slots' asset.</summary>
    public const string SettingsPath = "Assets/Data/Config/HallSlots_Default.asset";

    /// <summary>Where the templates are written.</summary>
    public const string TemplatesFolder = "ArtDeliverables/TimeDesk/HallSlots/templates";

    /// <summary>Where Capture Combinations writes by default (an ignored folder: review captures, not committed).</summary>
    public const string CombosFolder = "Logs/HallSlotsCombos";

    /// <summary>The representative combinations: a name, the culture, tier, phase, special, exhibit and the hall's hour.</summary>
    public static readonly (string name, string culture, StabilityTier tier, HallPhase phase, HallEvent special, string exhibit, float hour)[] Combos =
    {
        ("01_neutral_steady_day1", HallStates.Neutral, StabilityTier.Steady, HallPhase.Normal, HallEvent.None, HallStates.NoExhibit, 10f),
        ("02_egypt_strained_day9", "egypt", StabilityTier.Strained, HallPhase.Extended, HallEvent.None, "egypt", 11f),
        ("03_japan_breaching_ban_day12_night", "japan", StabilityTier.Breaching, HallPhase.Nights, HallEvent.Ban, "japan", 22f),
        ("04_neutral_collapsing_return_day15", HallStates.Neutral, StabilityTier.Collapsing, HallPhase.Nights, HallEvent.Return, "greece", 23f),
        ("05_china_steady_recall_day11", "china", StabilityTier.Steady, HallPhase.Extended, HallEvent.Recall, "china", 15f),
        ("06_greece_collapsing_nights", "greece", StabilityTier.Collapsing, HallPhase.Nights, HallEvent.None, "italy", 22f),
        ("07_britain_strained_day5", "britain", StabilityTier.Strained, HallPhase.Normal, HallEvent.None, "britain", 12f),
        ("08_germany_breaching_extended_dusk", "germany", StabilityTier.Breaching, HallPhase.Extended, HallEvent.None, "egypt", 18.5f),
        ("09_iraq_steady_nights", "iraq", StabilityTier.Steady, HallPhase.Nights, HallEvent.None, "iraq", 22f),
        ("10_italy_steady_return_morning", "italy", StabilityTier.Steady, HallPhase.Nights, HallEvent.Return, "italy", 9f),
        ("11_egypt_collapsing_ban_dusk", "egypt", StabilityTier.Collapsing, HallPhase.Nights, HallEvent.Ban, "japan", 18.5f),
        ("12_neutral_strained_recall", HallStates.Neutral, StabilityTier.Strained, HallPhase.Extended, HallEvent.Recall, HallStates.NoExhibit, 12f),
    };

    /// <summary>The painting's night tint (NOPE/Hall Deep Layout's night state), lifted so the template stays legible.</summary>
    private static readonly Color NightTint = new Color(0.24f * 1.6f, 0.32f * 1.6f, 0.49f * 1.6f, 1f);

    private static HallSlotsSO Settings()
    {
        var settings = AssetDatabase.LoadAssetAtPath<HallSlotsSO>(SettingsPath);
        if (settings == null)
            Debug.LogError($"[HallSlotsTools] No slots asset at {SettingsPath}.");
        return settings;
    }

    /// <summary>A readable copy of an imported texture, from its source file.</summary>
    private static Texture2D Readable(Texture2D source)
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(source)));
        return tex;
    }

    [MenuItem("Tools/Terminal Art/Hall Slots/Export Templates")]
    public static void ExportTemplates()
    {
        HallSlotsSO settings = Settings();
        if (settings == null || settings.painting == null)
            return;
        Directory.CreateDirectory(TemplatesFolder);
        Texture2D painting = Readable(settings.painting);
        Texture2D masks = settings.windowMasks != null ? Readable(settings.windowMasks) : null;
        int W = painting.width, H = painting.height;
        Color32[] day = painting.GetPixels32();
        Color32[] mask = masks != null && masks.width == W && masks.height == H ? masks.GetPixels32() : null;
        var night = new Color32[day.Length];
        for (int i = 0; i < day.Length; i++)
            night[i] = new Color(day[i].r / 255f * NightTint.r, day[i].g / 255f * NightTint.g, day[i].b / 255f * NightTint.b, 1f);

        var manifest = new StringBuilder();
        manifest.AppendLine($"Hall slots: templates of {settings.painting.name} ({W} x {H}). Each <slot>.png: left the hall at daylight (paint to match), right at night (the engine's tint); magenta: the paintable box; red hatch: window glass, never drawn; dimmed: outside the box.");
        manifest.AppendLine("Deliver Assets/Art/UI/Resources/Hall/Slots/<slot>/<variant>.png on the whole transparent canvas. See HALL_SLOTS_ART_REQUEST.md.");
        foreach (HallSlotDef s in settings.slots)
        {
            if (s == null || string.IsNullOrWhiteSpace(s.id))
                continue;
            int scale = Mathf.Max(s.width, s.height) < 160 ? 3 : 2;
            Texture2D a = Panel(day, mask, W, H, s, scale), b = Panel(night, mask, W, H, s, scale);
            var sheet = new Texture2D(a.width * 2 + 30, a.height + 20, TextureFormat.RGBA32, false);
            Fill(sheet, new Color32(243, 236, 222, 255));
            sheet.SetPixels32(10, 10, a.width, a.height, a.GetPixels32());
            sheet.SetPixels32(a.width + 20, 10, b.width, b.height, b.GetPixels32());
            File.WriteAllBytes(Path.Combine(TemplatesFolder, s.id + ".png"), sheet.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(a);
            UnityEngine.Object.DestroyImmediate(b);
            UnityEngine.Object.DestroyImmediate(sheet);
            manifest.AppendLine($"{s.id}: {s.width} x {s.height} px at ({s.x}, {s.y}), order {s.order}, {(s.IsNewOverlay ? "new overlay" : "varies '" + s.layer + "'")}; {s.shows}");
            foreach (HallVariantDef v in s.variants)
                manifest.AppendLine($"  {s.id}/{v.id}.png  when '{v.when}'  priority {v.priority}{(v.alternates > 1 ? $"  alternates {v.alternates}" : "")}: {v.describes}");
        }

        // The whole canvas: the hall dimmed, each box outlined (magenta: a registered layer's, cyan: a new overlay's).
        var guide = new Texture2D(W, H, TextureFormat.RGBA32, false);
        var g = new Color32[day.Length];
        for (int i = 0; i < day.Length; i++)
            g[i] = Color32.Lerp(day[i], new Color32(40, 30, 50, 255), 0.35f);
        guide.SetPixels32(g);
        foreach (HallSlotDef s in settings.slots)
            if (s != null)
                Outline(guide, s.x, H - s.y - s.height, s.width, s.height, s.IsNewOverlay ? new Color32(60, 220, 255, 255) : new Color32(255, 60, 200, 255), 2);
        File.WriteAllBytes(Path.Combine(TemplatesFolder, "_canvas_guide.png"), guide.EncodeToPNG());
        var blank = new Texture2D(W, H, TextureFormat.RGBA32, false);
        Fill(blank, new Color32(0, 0, 0, 0));
        File.WriteAllBytes(Path.Combine(TemplatesFolder, $"_blank_canvas_{W}x{H}.png"), blank.EncodeToPNG());
        File.WriteAllText(Path.Combine(TemplatesFolder, "_slots.txt"), manifest.ToString());
        UnityEngine.Object.DestroyImmediate(guide);
        UnityEngine.Object.DestroyImmediate(blank);
        UnityEngine.Object.DestroyImmediate(painting);
        if (masks != null)
            UnityEngine.Object.DestroyImmediate(masks);
        Debug.Log($"[HallSlotsTools] Exported {settings.slots.Count} slot templates, the canvas guide, the blank canvas and _slots.txt to {TemplatesFolder}.");
    }

    /// <summary>A slot's crop (a 40 px margin) of <paramref name="src"/> (rows bottom-up, as Unity stores them), dimmed outside its box, the window glass hatched red, the box outlined, scaled up by <paramref name="scale"/>.</summary>
    private static Texture2D Panel(Color32[] src, Color32[] mask, int W, int H, HallSlotDef s, int scale)
    {
        const int margin = 40;
        int x0 = Mathf.Max(0, s.x - margin), y0 = Mathf.Max(0, s.y - margin);
        int x1 = Mathf.Min(W, s.x + s.width + margin), y1 = Mathf.Min(H, s.y + s.height + margin);
        int w = x1 - x0, h = y1 - y0;
        var tex = new Texture2D(w * scale, h * scale, TextureFormat.RGBA32, false);
        var px = new Color32[w * scale * h * scale];
        var dim = new Color32(20, 10, 30, 255);
        var hatch = new Color32(220, 40, 40, 255);
        var box = new Color32(255, 0, 255, 255);
        for (int cy = 0; cy < h; cy++)
            for (int cx = 0; cx < w; cx++)
            {
                int sx = x0 + cx, syTop = y0 + cy;
                int index = (H - 1 - syTop) * W + sx;
                Color32 c = src[index];
                bool inside = sx >= s.x && sx < s.x + s.width && syTop >= s.y && syTop < s.y + s.height;
                if (!inside)
                    c = Color32.Lerp(c, dim, 0.45f);
                if (mask != null && Math.Max(mask[index].r, mask[index].g) > 128 && (cx + cy) % 8 < 2)
                    c = hatch;
                bool edge = inside && (sx == s.x || sx == s.x + s.width - 1 || syTop == s.y || syTop == s.y + s.height - 1);
                if (edge)
                    c = box;
                for (int ky = 0; ky < scale; ky++)
                    for (int kx = 0; kx < scale; kx++)
                        px[((h - 1 - cy) * scale + ky) * w * scale + cx * scale + kx] = c;
            }
        tex.SetPixels32(px);
        return tex;
    }

    private static void Fill(Texture2D tex, Color32 c)
    {
        var px = new Color32[tex.width * tex.height];
        for (int i = 0; i < px.Length; i++)
            px[i] = c;
        tex.SetPixels32(px);
    }

    private static void Outline(Texture2D tex, int x, int y, int w, int h, Color32 c, int thickness)
    {
        for (int t = 0; t < thickness; t++)
        {
            for (int i = x; i < x + w; i++)
            {
                Set(tex, i, y + t, c);
                Set(tex, i, y + h - 1 - t, c);
            }
            for (int j = y; j < y + h; j++)
            {
                Set(tex, x + t, j, c);
                Set(tex, x + w - 1 - t, j, c);
            }
        }
    }

    private static void Set(Texture2D tex, int x, int y, Color32 c)
    {
        if (x >= 0 && y >= 0 && x < tex.width && y < tex.height)
            tex.SetPixel(x, y, c);
    }

    /// <summary>The slots' problems and the art present, as lines (also logged); empty problems when the asset is sound.</summary>
    [MenuItem("Tools/Terminal Art/Hall Slots/Validate")]
    public static void ValidateMenu() => Validate(out _);

    /// <summary>Validates the slots' asset against the content's nations and lists the delivered art; returns the report and the problems' count.</summary>
    public static string Validate(out int problems)
    {
        problems = 0;
        HallSlotsSO settings = Settings();
        if (settings == null)
        {
            problems = 1;
            return "no slots asset";
        }
        var nations = new HashSet<string>(AssetDatabase.FindAssets("t:NationSO").Select(guid => AssetDatabase.LoadAssetAtPath<NationSO>(AssetDatabase.GUIDToAssetPath(guid))).Where(n => n != null && !string.IsNullOrWhiteSpace(n.id)).Select(n => n.id));
        List<string> found = HallSlotPick.Problems(settings.slots, settings.canvasWidth, settings.canvasHeight, nations.Count > 0 ? nations : null);
        problems = found.Count;
        var report = new StringBuilder($"[HallSlotsTools] {settings.slots.Count} slots, {settings.slots.Sum(s => s?.variants?.Count ?? 0)} variants, {nations.Count} nations; {found.Count} problem(s).\n");
        foreach (string p in found)
            report.AppendLine("  problem: " + p);
        int present = 0;
        foreach (HallSlotDef s in settings.slots)
            foreach (HallVariantDef v in s?.variants ?? new List<HallVariantDef>())
                for (int k = 1; k <= Math.Max(1, v.alternates); k++)
                {
                    string file = v.alternates > 1 ? v.id + "_" + k : v.id;
                    string path = ArtSlots.AssetRoot + ArtSlots.HallSlot(s.id, file) + ".png";
                    if (!File.Exists(path))
                        continue;
                    present++;
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    float aspect = sprite != null ? sprite.rect.width / sprite.rect.height : 0f;
                    float want = (float)settings.canvasWidth / settings.canvasHeight;
                    report.AppendLine($"  art: {path}{(Mathf.Abs(aspect - want) > 0.01f * want ? $"  WARNING: {sprite?.rect.width}x{sprite?.rect.height} is not the {settings.canvasWidth}x{settings.canvasHeight} canvas" : "")}");
                }
        report.AppendLine($"  {present} art file(s) delivered.");
        if (found.Count > 0)
            Debug.LogWarning(report.ToString());
        else
            Debug.Log(report.ToString());
        return report.ToString();
    }

    [MenuItem("Tools/Terminal Art/Hall Slots/Capture Combinations")]
    public static void CaptureMenu() => Capture(CombosFolder, null);

    /// <summary>
    /// In play with the office loaded: shows each of <see cref="Combos"/>
    /// (HallSlotsDev's overrides, the hall's hour by DevToolsState.ForcedHour,
    /// every slot's stand-in) for a crossfade and a moment, then saves
    /// <paramref name="folder"/>/&lt;name&gt;.png at the game view's size; restores
    /// the overrides and calls <paramref name="done"/> at the end.
    /// </summary>
    public static void Capture(string folder, Action done)
    {
        if (!EditorApplication.isPlaying || HallSlotsLink.Live == null)
        {
            Debug.LogError("[HallSlotsTools] Capture Combinations needs play mode with the office (and its hall slots) loaded.");
            done?.Invoke();
            return;
        }
        Directory.CreateDirectory(folder);
        bool stand = HallSlotsDev.ShowAllStandIns;
        float? hour = DevToolsState.ForcedHour;
        int index = -1;
        double next = 0;
        bool shot = false;
        const float wait = 1.4f; // the crossfade (0.6 s) and the lights settling
        void Step()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.update -= Step;
                done?.Invoke();
                return;
            }
            if (EditorApplication.timeSinceStartup < next)
                return;
            if (index >= 0 && !shot)
            {
                ScreenCapture.CaptureScreenshot(Path.Combine(folder, Combos[index].name + ".png"));
                Debug.Log($"[HallSlotsTools] {Combos[index].name}: {HallSlotsLink.Live?.State}");
                shot = true;
                next = EditorApplication.timeSinceStartup + 0.3;
                return;
            }
            index++;
            if (index >= Combos.Length)
            {
                EditorApplication.update -= Step;
                HallSlotsDev.Clear();
                HallSlotsDev.ShowAllStandIns = stand;
                HallSlotsDev.Changed();
                DevToolsState.ForcedHour = hour;
                Debug.Log($"[HallSlotsTools] Captured {Combos.Length} combinations to {folder}.");
                done?.Invoke();
                return;
            }
            var c = Combos[index];
            HallSlotsDev.ShowAllStandIns = true;
            HallSlotsDev.Culture = c.culture;
            HallSlotsDev.Tier = c.tier;
            HallSlotsDev.Phase = c.phase;
            HallSlotsDev.Event = c.special;
            HallSlotsDev.Exhibit = c.exhibit;
            HallSlotsDev.Changed();
            DevToolsState.ForcedHour = c.hour;
            shot = false;
            next = EditorApplication.timeSinceStartup + wait;
        }
        EditorApplication.update += Step;
    }
}
