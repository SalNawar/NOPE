using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// BUG-65a, the measurement: steady-state GC allocation per frame and per
/// call site, read from the editor's profiler while a QA run plays (promoted
/// from the audit's _TimeDeskAuditProfile). A scenario marks the window with
/// the ops "mark gcsites.start" and "mark gcsites.stop"; this records the
/// profiler between them, walks every frame's PlayerLoop for GC.Alloc
/// samples by the path of samples above them, and writes gc_sites.tsv into
/// the run's folder plus the result "perf.gcsites" (always green: it is the
/// baseline; the 0 B/frame gate is the built player's "gc" assertion). Every
/// site is tagged with the track that owns its fix (BUG-65 parts 65b-65l,
/// <see cref="OwnerOf"/>), so a red perf gate names the track.
/// </summary>
[InitializeOnLoad]
public static class QaGcSites
{
    /// <summary>The mark that starts the window.</summary>
    public const string StartMark = "gcsites.start";

    /// <summary>The mark that ends it.</summary>
    public const string StopMark = "gcsites.stop";

    private static bool _recording;
    private static bool _wasEnabled;

    static QaGcSites()
    {
        QaSignals.Raised += OnMark;
    }

    /// <summary>
    /// The track that owns the fix of an allocation at <paramref name="sitePath"/>
    /// (FIX_PLAN §1a, BUG-65 parts), or "unassigned" (INTEGRATION assigns
    /// it). Matching is by the script or sample names in the path.
    /// </summary>
    public static string OwnerOf(string sitePath)
    {
        string p = sitePath ?? string.Empty;
        foreach ((string needle, string owner) in Owners)
            if (p.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                return owner;
        return "unassigned";
    }

    /// <summary>BUG-65's parts: a name in the sample path, and the track that fixes allocations there.</summary>
    private static readonly (string needle, string owner)[] Owners =
    {
        ("DebugPanelController", "RUN-SAFE"),       // 65b the IMGUI cheat overlay
        ("OnGUI", "RUN-SAFE"),                      // 65b IMGUI
        ("DeskMagnifier", "TOOLS"),                 // 65c (cut in P0-C; kept so an old build still names it)
        ("MicroLens", "TOOLS"),                     // 65c
        ("ChipReader", "DOC-HANDLE"),               // 65d
        ("MachineCloseUp", "CAM"),                  // 65e (cut in P0-C)
        ("CharacterArt", "FEEDBACK"),               // 65f call-next loads
        ("DeskController", "DOC-HANDLE"),           // 65g whole-paper instantiation
        ("DeskDocument", "DOC-HANDLE"),             // 65g
        ("TmpFormText", "DOC-LOOK"),                // 65g the setup cost
        ("DeskPaperDepth", "DOC-LOOK"),             // 65g
        ("HallBackdrop", "LIGHT"),                  // 65h
        ("HallFourState", "LIGHT"),                 // 65h
        ("HallBakedLighting", "LIGHT"),             // 65h
        ("UiKit", "UI-PC"),                         // 65i
        ("MatchLines", "UI-PC"),                    // 65j
        ("MatchBoard", "UI-PC"),                    // 65j
        ("Mail", "UI-PC"),                          // 65j
        ("TimelineService", "FLOW"),                // 65k
        ("HallCityExterior", "HALL-FX"),            // 65l
    };

    /// <summary>One call site's totals over a window.</summary>
    public sealed class Site
    {
        /// <summary>The sample path above the GC.Alloc.</summary>
        public string Path;

        /// <summary>Bytes allocated there in the window.</summary>
        public double Bytes;

        /// <summary>Frames it allocated in.</summary>
        public int Frames;

        /// <summary>Allocation calls in the window.</summary>
        public double Calls;
    }

    /// <summary>Adds one frame's sites (path → bytes, calls) into <paramref name="totals"/> (pure).</summary>
    public static void Accumulate(Dictionary<string, Site> totals, Dictionary<string, (double bytes, double calls)> frame)
    {
        foreach (KeyValuePair<string, (double bytes, double calls)> kv in frame)
        {
            if (!totals.TryGetValue(kv.Key, out Site s))
                totals[kv.Key] = s = new Site { Path = kv.Key };
            s.Bytes += kv.Value.bytes;
            s.Calls += kv.Value.calls;
            s.Frames++;
        }
    }

    /// <summary>The table: one row per site, heaviest first, with the bytes per frame over <paramref name="frames"/> frames and the owner (pure).</summary>
    public static List<string> Table(IEnumerable<Site> sites, int frames)
    {
        var rows = new List<string> { "bytesPerFrame\tframesPresent\tframes\tcallsPerFrame\towner\tsite" };
        foreach (Site s in sites.OrderByDescending(x => x.Bytes).ThenBy(x => x.Path, StringComparer.Ordinal))
            rows.Add(string.Join("\t", (frames > 0 ? s.Bytes / frames : 0).ToString("0", CultureInfo.InvariantCulture), s.Frames, frames,
                (s.Frames > 0 ? s.Calls / s.Frames : 0).ToString("0.#", CultureInfo.InvariantCulture), OwnerOf(s.Path), s.Path));
        return rows;
    }

    private static void OnMark(string channel, string name)
    {
        if (channel != "qa" || !Application.isEditor || !EditorApplication.isPlaying)
            return;
        if (name == StartMark && !_recording)
        {
            _wasEnabled = ProfilerDriver.enabled;
            ProfilerDriver.profileEditor = false;
            ProfilerDriver.ClearAllFrames();
            ProfilerDriver.enabled = true;
            _recording = true;
        }
        else if (name == StopMark && _recording)
        {
            ProfilerDriver.enabled = _wasEnabled;
            _recording = false;
            Write(QaRunner.Active != null ? QaRunner.Active.OutDir : GateResults.Folder);
        }
    }

    /// <summary>Walks the recorded frames, writes gc_sites.tsv into <paramref name="dir"/> and the perf.gcsites result.</summary>
    private static void Write(string dir)
    {
        var totals = new Dictionary<string, Site>(StringComparer.Ordinal);
        var perFrame = new List<double>();
        int first = Math.Max(0, ProfilerDriver.firstFrameIndex), last = ProfilerDriver.lastFrameIndex;
        for (int i = first; i <= last; i++)
        {
            using (HierarchyFrameDataView view = ProfilerDriver.GetHierarchyFrameDataView(i, 0, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName, HierarchyFrameDataView.columnGcMemory, false))
            {
                if (view == null || !view.valid)
                    continue;
                var children = new List<int>();
                view.GetItemChildren(view.GetRootItemID(), children);
                foreach (int id in children)
                {
                    if (view.GetItemName(id) != "PlayerLoop")
                        continue;
                    perFrame.Add(view.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnGcMemory));
                    var frame = new Dictionary<string, (double bytes, double calls)>(StringComparer.Ordinal);
                    Walk(view, id, "PlayerLoop", frame, 0);
                    Accumulate(totals, frame);
                }
            }
        }
        Directory.CreateDirectory(dir);
        File.WriteAllLines(Path.Combine(dir, "gc_sites.tsv"), Table(totals.Values, perFrame.Count));
        perFrame.Sort();
        string measured = perFrame.Count == 0 ? "no profiled frames"
            : $"{perFrame.Count} frames; PlayerLoop GC median {perFrame[perFrame.Count / 2]:0} B, max {perFrame[perFrame.Count - 1]:0} B; {totals.Count} sites";
        string top = string.Join("; ", totals.Values.OrderByDescending(s => s.Bytes).Take(8)
            .Select(s => $"{(perFrame.Count > 0 ? s.Bytes / perFrame.Count : 0):0} B/frame [{OwnerOf(s.Path)}] {s.Path}"));
        GateResults.Write(new QaGateResult
        {
            id = "perf.gcsites",
            owner = "QA-RIG",
            pass = perFrame.Count > 0,
            measured = measured,
            detail = (top.Length > 0 ? top : "no allocating site") + $" (editor profiler; table: {Path.Combine(dir, "gc_sites.tsv")})",
            commit = GitHead.Read(GateResults.ProjectRoot),
            written = DateTime.Now.ToString("s"),
            source = dir,
        });
    }

    /// <summary>Collects, under <paramref name="id"/>, the bytes of every GC.Alloc sample by the path of samples above it.</summary>
    private static void Walk(HierarchyFrameDataView view, int id, string path, Dictionary<string, (double bytes, double calls)> sites, int depth)
    {
        var kids = new List<int>();
        view.GetItemChildren(id, kids);
        foreach (int k in kids)
        {
            string name = view.GetItemName(k);
            float gc = view.GetItemColumnDataAsFloat(k, HierarchyFrameDataView.columnGcMemory);
            if (gc <= 0f)
                continue;
            if (name == "GC.Alloc")
            {
                sites.TryGetValue(path, out (double bytes, double calls) s);
                sites[path] = (s.bytes + gc, s.calls + view.GetItemColumnDataAsFloat(k, HierarchyFrameDataView.columnCalls));
            }
            else if (depth < 40)
            {
                Walk(view, k, path + " > " + name, sites, depth + 1);
            }
        }
    }
}
