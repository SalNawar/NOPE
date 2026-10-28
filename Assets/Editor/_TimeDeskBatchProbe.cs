#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Local batch automation for the first Unity batch on main (2026-10-27), never committed: LIGHT's U11 (the rack's style) and U21 (the slots at night), and the decor key cleared before desk_plan_homes.</summary>
internal static class _TimeDeskBatchProbe
{
    private const string Dir = @"E:\unity\NOPE-tools\reports\main_unity";
    private static string Shots => Path.Combine(Dir, "shots");
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private const string Owner = "LIGHT";

    /// <summary>Editor job: forgets the saved decoration places of this worktree (desk_plan_decor leaves the form sorter where desk_plan_homes then finds it in front of the ring panel: INTEGRATE_SIX.md row 14).</summary>
    public static void ClearDecor()
    {
        string key = PlayerPrefKeys.For("TimeDesk.DeskDecor");
        bool had = PlayerPrefs.HasKey(key);
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        Directory.CreateDirectory(Dir);
        File.AppendAllText(Path.Combine(Dir, "decor_clears.txt"), $"{DateTime.Now:HH:mm:ss} cleared {key}: had it {had}\n");
    }


    /// <summary>Editor job: forgets this worktree's stored UI-language preference (TimeDesk.UiLanguage: "always English" left by an earlier run makes every culture label English, so the Translation Lens finds nothing: tools_lens on NOPE-t5, 2026-10-27).</summary>
    public static void ClearLanguage()
    {
        string key = PlayerPrefKeys.For("TimeDesk.UiLanguage");
        bool had = PlayerPrefs.HasKey(key);
        string value = had ? PlayerPrefs.GetString(key) : "(none)";
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        Directory.CreateDirectory(Dir);
        File.AppendAllText(Path.Combine(Dir, "decor_clears.txt"), $"{DateTime.Now:HH:mm:ss} cleared {key}: had it {had}, value {value}\n");
    }

    [InitializeOnLoadMethod]
    private static void Register()
    {
        QaRunner.RegisterOp("batchrack", Rack);
        QaRunner.RegisterOp("batchslots", Slots);
        QaRunner.RegisterOp("batchlens", Lens);
    }

    private static float Luma(Color32 c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;

    private static void Append(string file, string text)
    {
        Directory.CreateDirectory(Dir);
        File.AppendAllText(Path.Combine(Dir, file), text);
    }

    private static Camera Cam() => QaGame.PointerCamera() ?? Camera.main;

    /// <summary>The screen rect (capture pixels, bottom-left origin) of world points, or null when every point is behind the camera.</summary>
    private static Rect? ScreenRect(IEnumerable<Vector3> points, Camera cam, int w, int h)
    {
        float x0 = float.PositiveInfinity, y0 = float.PositiveInfinity, x1 = float.NegativeInfinity, y1 = float.NegativeInfinity;
        bool any = false;
        foreach (Vector3 p in points)
        {
            Vector3 sp = cam.WorldToScreenPoint(p);
            if (sp.z <= 0f) continue;
            float sx = sp.x * w / Mathf.Max(1f, cam.pixelWidth), sy = sp.y * h / Mathf.Max(1f, cam.pixelHeight);
            x0 = Mathf.Min(x0, sx); x1 = Mathf.Max(x1, sx); y0 = Mathf.Min(y0, sy); y1 = Mathf.Max(y1, sy);
            any = true;
        }
        return any ? new Rect(x0, y0, x1 - x0, y1 - y0) : (Rect?)null;
    }

    private static IEnumerable<Vector3> Corners(MeshFilter mf)
    {
        Bounds b = mf.sharedMesh.bounds;
        for (int i = 0; i < 8; i++)
            yield return mf.transform.TransformPoint(new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z));
    }

    /// <summary>A crop of the capture (screen pixels, bottom-left origin), scaled up by <paramref name="scale"/> (nearest), as a PNG.</summary>
    private static void SaveCrop(Color32[] px, int w, int h, Rect r, int margin, int scale, string file)
    {
        int x0 = Mathf.Clamp(Mathf.FloorToInt(r.xMin) - margin, 0, w - 1), x1 = Mathf.Clamp(Mathf.CeilToInt(r.xMax) + margin, 1, w);
        int y0 = Mathf.Clamp(Mathf.FloorToInt(r.yMin) - margin, 0, h - 1), y1 = Mathf.Clamp(Mathf.CeilToInt(r.yMax) + margin, 1, h);
        int cw = Mathf.Max(1, x1 - x0), ch = Mathf.Max(1, y1 - y0);
        var tex = new Texture2D(cw * scale, ch * scale, TextureFormat.RGB24, false);
        var outPx = new Color32[cw * scale * ch * scale];
        for (int y = 0; y < ch * scale; y++)
            for (int x = 0; x < cw * scale; x++)
                outPx[y * cw * scale + x] = px[(y0 + y / scale) * w + (x0 + x / scale)];
        tex.SetPixels32(outPx);
        tex.Apply();
        Directory.CreateDirectory(Shots);
        File.WriteAllBytes(Path.Combine(Shots, file), tex.EncodeToPNG());
        UnityEngine.Object.Destroy(tex);
    }

    private static string R(Rect? r) => r.HasValue ? $"x {r.Value.xMin:0}-{r.Value.xMax:0} y {r.Value.yMin:0}-{r.Value.yMax:0}" : "off screen";

    private static HallLightingRig Rig() => UnityEngine.Object.FindAnyObjectByType<HallLightingRig>();

    /// <summary>U11: the stamp rack as drawn now (the view the scenario put on): its mesh parts' screen rect (StampRig's RackParts), a 3x crop for the eye, the brass materials' style knobs.</summary>
    private static IEnumerator Rack(QaRunner run, QaStep step)
    {
        yield return new WaitForEndOfFrame();
        Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();
        try
        {
            int w = tex.width, h = tex.height;
            Color32[] px = tex.GetPixels32();
            Camera cam = Cam();
            HallLightingRig rig = Rig();
            float hour = rig != null ? rig.Hour : float.NaN;
            DeskStampTray tray = UnityEngine.Object.FindAnyObjectByType<DeskStampTray>();
            Transform rack = tray != null ? tray.Rack : null;
            var sb = new StringBuilder($"== rack {step.name} frame {Time.frameCount} hour {hour:0.000} screen {w}x{h} cam {(cam != null ? cam.name : "null")} tray {(tray != null ? tray.name : "none")} rack {(rack != null ? rack.name : "none")} active {(rack != null && rack.gameObject.activeInHierarchy)}\n");
            if (rack == null || cam == null)
            {
                run.Assert($"rackSeen.{step.name}", Owner, false, "no rack transform", "the rack is on the screen for U11");
            }
            else
            {
                var parts = rack.GetComponentsInChildren<MeshFilter>(false).Where(mf => mf.sharedMesh != null && mf.name != "ContactShadow" && mf.GetComponent<Renderer>() != null && mf.GetComponent<Renderer>().enabled).ToList();
                Rect? rect = ScreenRect(parts.SelectMany(Corners), cam, w, h);
                float inside = 0f;
                if (rect.HasValue)
                {
                    Rect r = rect.Value;
                    float ix = Mathf.Max(0f, Mathf.Min(r.xMax, w) - Mathf.Max(r.xMin, 0f)), iy = Mathf.Max(0f, Mathf.Min(r.yMax, h) - Mathf.Max(r.yMin, 0f));
                    inside = r.width * r.height > 0f ? ix * iy / (r.width * r.height) : 0f;
                }
                sb.AppendLine($"   rack mesh parts {parts.Count}: screen rect {R(rect)}; share inside the frame {inside:0.000}");
                foreach (Material m in parts.SelectMany(p => p.GetComponent<Renderer>().sharedMaterials).Where(m => m != null).Distinct())
                    sb.AppendLine($"   material {m.name} shader {m.shader.name}: highlight {(m.HasProperty("_PaintedHighlight") ? m.GetFloat("_PaintedHighlight").ToString("0.###", Inv) : "-")} inkRim {(m.HasProperty("_InkRim") ? m.GetFloat("_InkRim").ToString("0.###", Inv) : "-")} base {(m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor").ToString() : "-")}");
                run.Assert($"rackSeen.{step.name}", Owner, rect.HasValue && inside > 0.95f, $"rect {R(rect)}, {inside:0.000} inside", "the rack is wholly on the screen (U11's capture)");
                if (rect.HasValue && inside > 0.05f)
                    SaveCrop(px, w, h, rect.Value, 24, 3, $"u11_rack_{step.name}_{w}x{h}.png");
            }
            Directory.CreateDirectory(Shots);
            File.WriteAllBytes(Path.Combine(Shots, $"u11_{step.name}_{w}x{h}.jpg"), tex.EncodeToJPG(90));
            Append("probe.txt", sb.ToString());
        }
        finally { UnityEngine.Object.Destroy(tex); }
    }


    /// <summary>The Translation Lens's inputs at this moment: the day, its reach, the culture's phrase count, the lens level ids and which are owned, the briefing panel's state.</summary>
    private static IEnumerator Lens(QaRunner run, QaStep step)
    {
        yield return null;
        var sb = new StringBuilder($"== lens {step.name} frame {Time.frameCount}: ");
        if (!RunManager.HasInstance || RunManager.Instance.World == null || RunManager.Instance.Library == null)
            sb.Append("no run, world or library");
        else
        {
            WorldState world = RunManager.Instance.World;
            ContentLibrarySO library = RunManager.Instance.Library;
            var ids = library.Translation != null && library.Translation.lens != null && library.Translation.lens.rules != null ? library.Translation.lens.rules.levelIds : null;
            sb.Append($"day {world.day}; reach {TranslationLensPresenter.CurrentReach}; levelIds {(ids == null ? "null" : string.Join(",", ids))}; owned {(ids == null ? "-" : string.Join(",", ids.Select(i => $"{i}={world.HasUpgrade(i)}")))}; ");
            CultureThemeService service = CultureThemeService.Instance;
            UiStrings strings = service != null ? service.Strings : null;
            sb.Append($"strings {(strings == null ? "null" : $"phrases {strings.Phrases.Count}")}; presenter {(TranslationLensPresenter.Instance != null ? "present" : "NULL")}; ");
            GameObject panel = GameObject.Find("OfficeOverlayCanvas/BriefingPanel");
            sb.Append($"briefing panel {(panel == null ? "not found" : panel.activeInHierarchy ? "active" : "inactive")}; pointer {(UnityEngine.InputSystem.Pointer.current != null ? UnityEngine.InputSystem.Pointer.current.position.ReadValue().ToString() : "none")}");
        }
        Append("probe.txt", sb.AppendLine().ToString());
        run.Assert($"lens.{step.name}", "TOOLS", true, sb.ToString().Substring(0, Math.Min(200, sb.Length)), "a diagnostic line, never red");
    }

    private static readonly string[] HallNames = { "_HallNightTint", "_HallPoolColour", "_HallPoolStrength", "_HallEmissionGain", "_HallFixtureCount", "_HallFixtureCenters", "_HallFixtureLevels", "_HallFixturePools", "_HallCityLights" };

    /// <summary>The painting's renderer (NOPE/Hall Deep Layout, the whole canvas) and its canvas size in pixels.</summary>
    private static Renderer Painting(out Vector2 canvas)
    {
        canvas = Vector2.zero;
        Renderer deep = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude)
            .FirstOrDefault(r => r.enabled && r.sharedMaterial != null && r.sharedMaterial.shader != null && r.sharedMaterial.shader.name == "NOPE/Hall Deep Layout" && r.sharedMaterial.HasProperty("_Region") && r.sharedMaterial.GetFloat("_Region") < 0.5f);
        if (deep == null) return null;
        if (deep.sharedMaterial.HasProperty("_CanvasSize")) canvas = deep.sharedMaterial.GetVector("_CanvasSize");
        if (canvas.x < 1f && deep is SpriteRenderer sr && sr.sprite != null) canvas = new Vector2(sr.sprite.texture.width, sr.sprite.texture.height);
        return deep;
    }

    /// <summary>U21: every hall slot that shows art now: its property block holds no _Hall* name, the luma step across its region's border, a crop of each for the eye. The region (HallSlotDef x, y, width, height on the canvas, top-left origin) is mapped through the painting's world bounds.</summary>
    private static IEnumerator Slots(QaRunner run, QaStep step)
    {
        yield return new WaitForEndOfFrame();
        Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();
        try
        {
            int w = tex.width, h = tex.height;
            Color32[] px = tex.GetPixels32();
            Camera cam = Cam();
            HallLightingRig rig = Rig();
            float hour = rig != null ? rig.Hour : float.NaN;
            Vector4 night = Shader.GetGlobalVector("_HallNightTint");
            Renderer painting = Painting(out Vector2 canvas);
            var sb = new StringBuilder($"== slots {step.name} frame {Time.frameCount} hour {hour:0.000} screen {w}x{h} nightTint ({night.x:0.0000},{night.y:0.0000},{night.z:0.0000},{night.w:0}) standIns {HallSlotsDev.ShowAllStandIns} painting {(painting != null ? painting.name : "none")} canvas {canvas.x:0}x{canvas.y:0} bounds {(painting != null ? painting.bounds.ToString() : "-")}\n");
            HallSlotsLink link = UnityEngine.Object.FindAnyObjectByType<HallSlotsLink>();
            if (link == null || cam == null || canvas.x < 1f)
            {
                sb.AppendLine("   no HallSlotsLink, camera or canvas size");
                run.Assert($"slotsFound.{step.name}", Owner, false, "no link or painting", "hall slots present for U21");
            }
            else
            {
                var slotsField = typeof(HallSlotsLink).GetField("_slots", BindingFlags.Instance | BindingFlags.NonPublic);
                var standInsField = typeof(HallSlotsLink).GetField("_standIns", BindingFlags.Instance | BindingFlags.NonPublic);
                var slots = slotsField?.GetValue(link) as IEnumerable;
                var standIns = standInsField?.GetValue(link) as IDictionary;
                var standInSprites = new HashSet<Sprite>();
                if (standIns != null) foreach (object v in standIns.Values) if (v is Sprite s) standInSprites.Add(s);
                int shown = 0, blocksWithHall = 0, measured = 0;
                float worstStep = 0f;
                var mpb = new MaterialPropertyBlock();
                if (slots != null)
                    foreach (object slot in slots)
                    {
                        Type st = slot.GetType();
                        var def = st.GetField("Def").GetValue(slot) as HallSlotDef;
                        foreach (string faceName in new[] { "Front", "Back" })
                        {
                            object face = st.GetField(faceName).GetValue(slot);
                            if (face == null) continue;
                            var r = face.GetType().GetField("Renderer").GetValue(face) as SpriteRenderer;
                            string file = face.GetType().GetField("File").GetValue(face) as string;
                            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy || r.sprite == null || r.color.a < 0.5f) continue;
                            bool standIn = standInSprites.Contains(r.sprite);
                            shown++;
                            r.GetPropertyBlock(mpb);
                            List<string> hall = HallNames.Where(n => mpb.HasVector(n) || mpb.HasFloat(n) || mpb.HasColor(n)).ToList();
                            if (hall.Count > 0) blocksWithHall++;
                            Rect? rect = null;
                            if (def != null && def.width > 0 && def.height > 0)
                            {
                                // The slot's sprite spans the whole 2172x724 canvas (the overlay's size), so its own bounds map the canvas to the world.
                                Bounds pb = r.bounds;
                                float wx0 = pb.min.x + def.x / canvas.x * pb.size.x, wx1 = pb.min.x + (def.x + def.width) / canvas.x * pb.size.x;
                                float wy1 = pb.max.y - def.y / canvas.y * pb.size.y, wy0 = pb.max.y - (def.y + def.height) / canvas.y * pb.size.y;
                                float wz = pb.center.z;
                                rect = ScreenRect(new[] { new Vector3(wx0, wy0, wz), new Vector3(wx1, wy0, wz), new Vector3(wx0, wy1, wz), new Vector3(wx1, wy1, wz) }, cam, w, h);
                            }
                            string line = $"   slot {def?.id} {faceName} file {file ?? "-"} sprite {r.sprite.name} {(standIn ? "STAND-IN" : "art")} alpha {r.color.a:0.00} shader {(r.sharedMaterial != null ? r.sharedMaterial.shader.name : "-")} block _Hall* names: {(hall.Count == 0 ? "none" : string.Join(",", hall))}; region {def?.x},{def?.y} {def?.width}x{def?.height} -> rect {R(rect)}";
                            if (rect.HasValue)
                            {
                                // Only the edges inside the frame are measured: at A1 the office view cuts the painting's top, so a slot near the canvas top shows its lower part only.
                                Rect o = rect.Value;
                                int x0 = Mathf.RoundToInt(Mathf.Clamp(o.xMin, 8, w - 9)), x1 = Mathf.RoundToInt(Mathf.Clamp(o.xMax, 8, w - 9));
                                int y0 = Mathf.RoundToInt(Mathf.Clamp(o.yMin, 8, h - 9)), y1 = Mathf.RoundToInt(Mathf.Clamp(o.yMax, 8, h - 9));
                                bool leftIn = o.xMin >= 8, rightIn = o.xMax <= w - 9, bottomIn = o.yMin >= 8, topIn = o.yMax <= h - 9;
                                var steps = new List<float>();
                                float inSum = 0f, outSum = 0f; int n = 0;
                                const int d = 4;
                                if (x1 - x0 > 12 && y1 - y0 > 12)
                                {
                                    for (int x = x0 + 6; x < x1 - 6; x += 3)
                                    {
                                        if (bottomIn) { steps.Add(Mathf.Abs(Luma(px[(y0 + d) * w + x]) - Luma(px[(y0 - d) * w + x]))); inSum += Luma(px[(y0 + d) * w + x]); outSum += Luma(px[(y0 - d) * w + x]); n++; }
                                        if (topIn) { steps.Add(Mathf.Abs(Luma(px[(y1 - d) * w + x]) - Luma(px[(y1 + d) * w + x]))); inSum += Luma(px[(y1 - d) * w + x]); outSum += Luma(px[(y1 + d) * w + x]); n++; }
                                    }
                                    for (int y = y0 + 6; y < y1 - 6; y += 3)
                                    {
                                        if (leftIn) { steps.Add(Mathf.Abs(Luma(px[y * w + x0 + d]) - Luma(px[y * w + x0 - d]))); inSum += Luma(px[y * w + x0 + d]); outSum += Luma(px[y * w + x0 - d]); n++; }
                                        if (rightIn) { steps.Add(Mathf.Abs(Luma(px[y * w + x1 - d]) - Luma(px[y * w + x1 + d]))); inSum += Luma(px[y * w + x1 - d]); outSum += Luma(px[y * w + x1 + d]); n++; }
                                    }
                                }
                                if (steps.Count > 0)
                                {
                                    steps.Sort();
                                    float med = steps[steps.Count / 2], p90 = steps[Mathf.Min(steps.Count - 1, (int)(steps.Count * 0.9f))];
                                    float inMean = 0f; int c = 0;
                                    for (int y = y0 + 2; y < y1 - 2; y += 2) for (int x = x0 + 2; x < x1 - 2; x += 2) { inMean += Luma(px[y * w + x]); c++; }
                                    inMean /= Mathf.Max(1, c);
                                    line += $"; edges in frame L{leftIn} R{rightIn} B{bottomIn} T{topIn}; border step median {med:0.0} p90 {p90:0.0} over {steps.Count} samples (inside ring {inSum / n:0.0} vs outside ring {outSum / n:0.0}); inside mean {inMean:0.0}";
                                    measured++;
                                    worstStep = Mathf.Max(worstStep, med);
                                    SaveCrop(px, w, h, new Rect(x0, y0, x1 - x0, y1 - y0), 40, 2, $"u21_{def?.id}_{faceName}_{step.name}_{w}x{h}.png");
                                }
                                else
                                    line += "; OFF THE FRAME (no edge inside): not measured in this view";
                            }
                            sb.AppendLine(line);
                        }
                    }
                sb.AppendLine($"   shown faces {shown}, blocks naming a _Hall* global {blocksWithHall}, measured borders {measured}, worst median step {worstStep:0.0}");
                run.Assert($"slotsNoHallBlock.{step.name}", Owner, shown > 0 && blocksWithHall == 0, $"{blocksWithHall} of {shown} shown faces", "no slot renderer's property block holds a _Hall* name (U21)");
                run.Assert($"slotsBorder.{step.name}", Owner, measured > 0 && worstStep <= 3f, $"worst median border step {worstStep:0.0} over {measured} slots", "the painting and each slot share one tint: luma step at the border <= 3 (U21)");
            }
            Directory.CreateDirectory(Shots);
            File.WriteAllBytes(Path.Combine(Shots, $"u21_hall_{step.name}_{w}x{h}.jpg"), tex.EncodeToJPG(92));
            Append("probe.txt", sb.ToString());
        }
        finally { UnityEngine.Object.Destroy(tex); }
    }
    /// <summary>Tonight's playable build (Saleh 2026-10-28: "a build in 4 hours that has everything playable"): the demo's scenes with the cheat menu, release, no demo gate (DemoBuild.BuildScratch), outside the project.</summary>
    public static void BuildPlayable()
    {
        string exe = @"E:\unity\NOPE-builds\TimeSorter_Night\TimeSorter.exe";
        var result = DemoBuild.BuildScratch(exe, new[] { DemoBuild.CheatsDefine }, false);
        Append("build.txt", $"{System.DateTime.Now:HH:mm:ss} BuildPlayable {result} -> {exe}" + System.Environment.NewLine);
        if (result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new System.Exception("BuildPlayable " + result);
    }
}
#endif
