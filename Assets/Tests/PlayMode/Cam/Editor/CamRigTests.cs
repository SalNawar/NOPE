using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// CAM's rig verdicts that need no Unity (CamOps; docs/pending/CAM.md,
/// rounds 4 to 8): a private collection is counted whatever its type (a
/// HashSet is no non-generic ICollection, so the chip read's "came back out"
/// wait never saw the reader's set), and a chip read counts only once its
/// paper is out (ChipReadOf); a chrome leg's whole report, from what its
/// camchrome steps show (ShownBy) and what its watch counted (LegLines): a
/// plate the leg showed that was never drawn is CAM's red line, never
/// silence, a drawn plate is judged under its owner outside CAM's chrome
/// gate, and a half of the leg that was never seen (nothing held, the
/// reading view never at rest) or a control not found fails CAM's line; and
/// how the watch places a held thing that is no Component (HeldMember: a
/// dragged decoration by its Root); and the view a framing leg names
/// (ReadingLeg: camframe judges a leg only on that view); and DeskView's
/// blend hook, which runs on every view change, allocating nothing (Hook,
/// measured per thread and read in its IL: BUG-65e/73e, round 8). Pure
/// (types and values only, no Unity object made; the hook test puts
/// Cinemachine's static blend hook back as it found it); in CAM's own Editor
/// folder (Assembly-CSharp-Editor, the editor runner's).
/// </summary>
public class CamRigTests
{
    private static readonly string[] None = Array.Empty<string>();

    [Test]
    public void Count_CountsAHashSet_AsWellAsAList()
    {
        Assert.AreEqual(2, CamOps.Count(new HashSet<int> { 3, 7 }), "DeskController._inReader is a HashSet<int>");
        Assert.AreEqual(3, CamOps.Count(new List<string> { "a", "b", "c" }), "TravellerWheel._said is a List");
        Assert.AreEqual(0, CamOps.Count(new HashSet<int>()));
        Assert.AreEqual(0, CamOps.Count(null));
        Assert.AreEqual(0, CamOps.Count(4), "not a collection");
    }

    [Test]
    public void ShownBy_ReadsTheLegsTarget_AndTheStampHintStep_AndRefusesAnUnknownKey()
    {
        CollectionAssert.AreEqual(new[] { "guide", "reading" }, CamOps.ShownBy(1, "guide, reading"), "the day-1 leg");
        CollectionAssert.AreEqual(new[] { CamOps.ShowsHeld, CamOps.ShowsReading }, CamOps.ShownBy(1, " held,reading "), "a day-12 leg");
        CollectionAssert.AreEqual(new[] { "stamphint" }, CamOps.ShownBy(2, null), "camchrome n = 2 shows the stamp hint, as the counter's bounce does");
        CollectionAssert.IsEmpty(CamOps.ShownBy(1, string.Empty));
        CollectionAssert.IsEmpty(CamOps.ShownBy(1, null));
        Assert.Throws<InvalidOperationException>(() => CamOps.ShownBy(1, "loupe"), "a typo must not drop the expectation silently");
        Assert.Throws<InvalidOperationException>(() => CamOps.ShownBy(3, null), "no such step");
        Assert.Throws<InvalidOperationException>(() => CamOps.ShownBy(0, null), "n = 0 ends a leg and shows nothing");
    }

    [Test]
    public void LegLines_ALegWhoseStampHintStepShowedThePlate_ButNeverDrew_ReportsARedCamLine()
    {
        // A day-12 leg: n = 1 "held, reading", then n = 2 (the stamp hint); the hint is found but drawn 0 frames.
        HashSet<string> shows = Shows((1, "held, reading"), (2, null));
        List<CamOps.ChromeLine> lines = CamOps.LegLines("a1", shows, Clean(), new[] { Seen("guide", 0, 0), Seen("stamphint", 0, 0) });

        CamOps.ChromeLine hint = Line(lines, "chrome.a1.stamphint");
        Assert.IsFalse(hint.Pass, "silence would let the stamp hint's gate pass by absence");
        Assert.AreEqual("CAM", hint.Owner, "nothing was measured: the rig's line, not the plate owner's");
        StringAssert.StartsWith("not measured", hint.Measured);
        StringAssert.Contains("shown 0 frames", hint.Measured);
        StringAssert.Contains("camchrome n = 2", hint.Detail);
        Assert.IsFalse(lines.Any(l => l.Id == "plate.a1.stamphint"), "no owner's verdict on a plate never drawn");
        Assert.IsFalse(lines.Any(l => l.Id.EndsWith(".guide", StringComparison.Ordinal)), "the guide plate was neither drawn nor shown: no line");
        Assert.IsTrue(Line(lines, "chrome.a1").Pass, "CAM's own chrome line stands apart");

        CamOps.ChromeLine missing = Line(CamOps.LegLines("a1", shows, Clean(), new[] { Seen("guide", 0, 0) }), "chrome.a1.stamphint");
        Assert.IsFalse(missing.Pass);
        Assert.AreEqual("CAM", missing.Owner);
        StringAssert.Contains("not found", missing.Measured, "a plate the watch could not find is not measured either");
    }

    [Test]
    public void LegLines_TheDay1LegsGuide_NeverDrawn_IsARedCamLine()
    {
        List<CamOps.ChromeLine> lines = CamOps.LegLines("a1_d1", Shows((1, "guide, reading")), Clean(holding: 0), new[] { Seen("guide", 0, 0), Seen("stamphint", 0, 0) });
        CamOps.ChromeLine guide = Line(lines, "chrome.a1_d1.guide");
        Assert.IsFalse(guide.Pass);
        Assert.AreEqual("CAM", guide.Owner);
        StringAssert.Contains("camchrome n = 1's target", guide.Detail);
        Assert.IsTrue(Line(lines, "chrome.a1_d1").Pass, "the day-1 leg shows no held thing, so 0 holding frames is not a gap");
    }

    [Test]
    public void LegLines_ADrawnPlate_IsJudgedUnderItsOwner_OutsideCamsChromeLines()
    {
        var notes = new[] { "frame 12: StampBar/Hint over the reading view's work" };
        List<CamOps.ChromeLine> lines = CamOps.LegLines("a1", Shows((1, "held, reading"), (2, null)), Clean(), new[] { Seen("guide", 40, 0), new CamOps.PlateSeen("stamphint", 40, 12, notes) });

        CamOps.ChromeLine hint = Line(lines, "plate.a1.stamphint");
        Assert.IsFalse(hint.Pass, "12 frames over the work");
        Assert.AreEqual("STAMP", hint.Owner);
        StringAssert.Contains("shown 40 frames, 12 over the work", hint.Measured);
        StringAssert.Contains(notes[0], hint.Detail);

        CamOps.ChromeLine guide = Line(lines, "plate.a1.guide");
        Assert.IsTrue(guide.Pass, "a plate drawn in a leg that did not show it is still judged");
        Assert.AreEqual("CONTENT", guide.Owner);

        Assert.IsTrue(lines.All(l => !l.Id.StartsWith("chrome.a1.", StringComparison.Ordinal)), "both plates were measured: no CAM plate line");
        CamOps.ChromeLine chrome = Line(lines, "chrome.a1");
        Assert.IsTrue(chrome.Pass, "the plates' lines are their owners', so CAM's chrome line stays green while they are red");
        StringAssert.Contains("plates: GuidePrompt/Plate shown 40 frames, StampBar/Hint shown 40 frames", chrome.Measured);
    }

    [Test]
    public void LegLines_CamsChromeLine_FailsOnChromeOverTheWork_AndOnAHalfTheLegNeverSaw()
    {
        HashSet<string> day12 = Shows((1, "held, reading"));
        Assert.IsTrue(Line(CamOps.LegLines("a1", day12, Clean(), null), "chrome.a1").Pass);

        CamOps.ChromeTally over = Clean();
        over.Over = 3;
        over.Violations = new[] { "frame 9: PcTab over the held Decor_form_sorter" };
        CamOps.ChromeLine covered = Line(CamOps.LegLines("a1", day12, over, null), "chrome.a1");
        Assert.IsFalse(covered.Pass);
        StringAssert.Contains("PcTab over the held", covered.Detail);

        CamOps.ChromeLine heldNothing = Line(CamOps.LegLines("a1", day12, Clean(holding: 0), null), "chrome.a1");
        Assert.IsFalse(heldNothing.Pass, "a day-12 leg whose drag held nothing tested only half of BUG-59");
        StringAssert.Contains("held nothing", heldNothing.Detail);

        CamOps.ChromeLine neverRested = Line(CamOps.LegLines("a1", day12, Clean(reading: 0), null), "chrome.a1");
        Assert.IsFalse(neverRested.Pass);
        StringAssert.Contains("reading view never came to rest", neverRested.Detail);

        CamOps.ChromeLine unwatched = Line(CamOps.LegLines("a1", new HashSet<string>(), Clean(frames: 0, holding: 0, reading: 0), null), "chrome.a1");
        Assert.IsFalse(unwatched.Pass, "a leg that watched no frame measured nothing");
        Assert.AreEqual("CAM", unwatched.Owner);

        CamOps.ChromeTally noTab = Clean();
        noTab.Missing = "StampBar/Tab";
        CamOps.ChromeLine missing = Line(CamOps.LegLines("a1", day12, noTab, null), "chrome.a1");
        Assert.IsFalse(missing.Pass, "a renamed or unbuilt control is never watched: no gate may go green by its absence");
        StringAssert.Contains("not found: StampBar/Tab", missing.Measured);
        StringAssert.Contains("StampBar/Tab not found", missing.Detail);
        CamOps.ChromeTally noTabDay1 = Clean(holding: 0);
        noTabDay1.Missing = "PcTab, CityLook";
        Assert.IsFalse(Line(CamOps.LegLines("a1_d1", Shows((1, "guide, reading")), noTabDay1, null), "chrome.a1_d1").Pass, "on day 1 too");
    }

    [Test]
    public void LegLines_AHoldTheWatchCouldNotPlace_IsNamed_NotBlamedOnTheDrag()
    {
        HashSet<string> day12 = Shows((1, "held, reading"));
        CamOps.ChromeTally unplaced = Clean(holding: 0);
        unplaced.UnplacedFrames = 40;
        unplaced.Unplaced = "Item";
        CamOps.ChromeLine line = Line(CamOps.LegLines("a1", day12, unplaced, null), "chrome.a1");
        Assert.IsFalse(line.Pass, "the held half went unmeasured");
        StringAssert.Contains("held on 40 frames (Item)", line.Detail);
        StringAssert.Contains("could not place it on screen", line.Detail);
        StringAssert.DoesNotContain("held nothing", line.Detail, "the drag held something: blaming a blocked press names the wrong cause");
        StringAssert.Contains("0 holding, 40 held but not placed (Item)", line.Measured);

        CamOps.ChromeTally some = Clean(holding: 25);
        some.UnplacedFrames = 3;
        some.Unplaced = "Item";
        CamOps.ChromeLine placedMostly = Line(CamOps.LegLines("a1", day12, some, null), "chrome.a1");
        Assert.IsTrue(placedMostly.Pass, "the held half was measured on 25 frames");
        StringAssert.Contains("25 holding, 3 held but not placed (Item)", placedMostly.Measured);
    }

    [Test]
    public void HeldMember_PlacesADraggedDecoration_ByItsRoot_AndAComponentByItself()
    {
        // The day-12 chrome legs drag a decoration: DeskDecor registers its plain Item (no Component) with HoldRegistry.Desk.
        Type item = typeof(DeskDecor).GetNestedType("Item", BindingFlags.NonPublic);
        Assert.IsNotNull(item, "DeskDecor's hold");
        Assert.IsTrue(typeof(IHoldable).IsAssignableFrom(item));
        MemberInfo root = CamOps.HeldMember(item);
        Assert.IsNotNull(root, "a dragged decoration must be placed on screen, or cam_chrome's held half is never measured");
        Assert.AreEqual("Root", root.Name, "its Root carries its parts, not its Owner (the DeskDecor component that holds every decoration)");

        Assert.IsNull(CamOps.HeldMember(typeof(Rolodex)), "a Component is placed by itself");
        Assert.AreEqual("Root", CamOps.HeldMember(typeof(OwnerFirst)).Name, "a Transform before another Component");
        Assert.AreEqual("Body", CamOps.HeldMember(typeof(ByProperty)).Name, "a GameObject property");
        Assert.AreEqual("Owner", CamOps.HeldMember(typeof(OnlyAComponent)).Name, "else another Component");
        Assert.IsNull(CamOps.HeldMember(typeof(NothingToPlace)), "nothing to read: the leg names the hold it could not place");
    }

    [Test]
    public void ChipReadOf_CountsAReadOnlyOnceItsPaperIsOut_AndAStuckReadEndsTheReads()
    {
        Assert.AreEqual(CamOps.ChipRead.Refused, CamOps.ChipReadOf(false, false), "no chip: it bounces back, the next paper is tried");
        Assert.AreEqual(CamOps.ChipRead.Refused, CamOps.ChipReadOf(false, true));
        Assert.AreEqual(CamOps.ChipRead.Stuck, CamOps.ChipReadOf(true, false), "a read whose paper never came out is no read (play.<name> red: reads < f)");
        Assert.AreEqual(CamOps.ChipRead.Read, CamOps.ChipReadOf(true, true));
    }

    [Test]
    public void DeskViewHook_AllocatesNothing_OnceInstalled()
    {
        // BUG-65e/73e (WAVE2_OPEN2 CAM #1): Hook runs at bind and on every view change (DeskView.Set: each tilt in, each
        // Return). A static method group converted to a delegate there made a new delegate on every call (Unity compiles
        // C# 9; only C# 11 caches such a delegate), so every reading toggle allocated inside cam_gc's 0 B window.
        var hook = (Action<DeskView>)Delegate.CreateDelegate(typeof(Action<DeskView>), HookMethod());
        FieldInfo owner = DeskViewStatic("_hookOwner"), before = DeskViewStatic("_before");
        object ownerWas = owner.GetValue(null), beforeWas = before.GetValue(null);
        CinemachineCore.GetBlendOverrideDelegate installed = CinemachineCore.GetBlendOverride;
        try
        {
            hook(null); // installs the hook (once) and compiles the call
            CinemachineCore.GetBlendOverrideDelegate ours = CinemachineCore.GetBlendOverride;
            Assert.IsNotNull(ours, "the hook is installed");
            long start = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 64; i++)
                hook(null);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - start;
            Assert.AreEqual(0L, allocated, "64 view changes allocated in DeskView.Hook: make our blend delegate once and compare against it");
            Assert.AreSame(ours, CinemachineCore.GetBlendOverride, "installed once: a later view change keeps the same hook");
        }
        finally
        {
            CinemachineCore.GetBlendOverride = installed;
            owner.SetValue(null, ownerWas);
            before.SetValue(null, beforeWas);
        }
    }

    [Test]
    public void DeskViewHook_MakesNoObject_InItsIL()
    {
        // The same, whatever a runtime reports per thread (Unity's Mono may report no bytes): Hook's own code makes no
        // object, array, box or delegate (the old body's 'ldftn; newobj' made a delegate on every call).
        List<OpCode> ops = OpCodesOf(HookMethod());
        CollectionAssert.IsNotEmpty(ops);
        foreach (OpCode op in new[] { OpCodes.Newobj, OpCodes.Newarr, OpCodes.Box, OpCodes.Ldftn })
            CollectionAssert.DoesNotContain(ops, op, $"DeskView.Hook's IL has {op.Name}: Hook runs on every view change and must allocate nothing");
    }

    [Test]
    public void ReadingLeg_ReadsTheViewTheFramingLegNames_AndRefusesAMissingOrUnknownView()
    {
        Assert.IsTrue(CamOps.ReadingLeg("reading"), "a *_reading leg is judged on the reading view's criteria, with its .bottom line");
        Assert.IsFalse(CamOps.ReadingLeg("office"));
        Assert.IsTrue(CamOps.ReadingLeg(" reading "));
        Assert.AreEqual(CamOps.ShowsReading, "reading", "the same word as a chrome leg's reading half");
        Assert.Throws<InvalidOperationException>(() => CamOps.ReadingLeg(null), "a leg that names no view would be judged on whichever view is on");
        Assert.Throws<InvalidOperationException>(() => CamOps.ReadingLeg(string.Empty));
        Assert.Throws<InvalidOperationException>(() => CamOps.ReadingLeg("city"), "camframe frames the desk's two poses only");
        Assert.Throws<InvalidOperationException>(() => CamOps.ReadingLeg("Reading"), "a typo must not pick a view silently");
    }

    private sealed class OwnerFirst : IHoldable
    {
        public DeskDecor Owner = null;
        public string Id = null;
        public Transform Root = null;

        public void CancelHold()
        {
        }
    }

    private sealed class ByProperty : IHoldable
    {
        public Rolodex Owner { get; set; }

        public GameObject Body { get; set; }

        public void CancelHold()
        {
        }
    }

    private sealed class OnlyAComponent : IHoldable
    {
        public Rolodex Owner = null;

        public void CancelHold()
        {
        }
    }

    private sealed class NothingToPlace : IHoldable
    {
        public int Id = 0;

        public void CancelHold()
        {
        }
    }

    /// <summary>DeskView's static blend hook installer (private).</summary>
    private static MethodInfo HookMethod()
    {
        MethodInfo hook = typeof(DeskView).GetMethod("Hook", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(hook, "DeskView.Hook(DeskView)");
        return hook;
    }

    /// <summary>A private static field of DeskView's blend hook.</summary>
    private static FieldInfo DeskViewStatic(string name)
    {
        FieldInfo field = typeof(DeskView).GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(field, $"DeskView.{name}");
        return field;
    }

    /// <summary>The op codes of <paramref name="method"/>'s IL, in order (each operand stepped over by its size).</summary>
    private static List<OpCode> OpCodesOf(MethodInfo method)
    {
        var table = new Dictionary<short, OpCode>();
        foreach (FieldInfo f in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            if (f.GetValue(null) is OpCode code)
                table[code.Value] = code;
        byte[] il = method.GetMethodBody().GetILAsByteArray();
        var ops = new List<OpCode>();
        for (int i = 0; i < il.Length;)
        {
            short value = il[i] == 0xFE ? unchecked((short)(0xFE00 | il[i + 1])) : il[i];
            i += il[i] == 0xFE ? 2 : 1;
            OpCode op = table[value];
            ops.Add(op);
            switch (op.OperandType)
            {
                case OperandType.InlineNone:
                    break;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    i += 1;
                    break;
                case OperandType.InlineVar:
                    i += 2;
                    break;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    i += 8;
                    break;
                case OperandType.InlineSwitch:
                    i += 4 + 4 * BitConverter.ToInt32(il, i);
                    break;
                default:
                    i += 4;
                    break;
            }
        }
        return ops;
    }

    /// <summary>What a leg shows after its camchrome steps (n, target), gathered as the op gathers it.</summary>
    private static HashSet<string> Shows(params (int n, string target)[] steps)
    {
        var shows = new HashSet<string>();
        foreach ((int n, string target) in steps)
            shows.UnionWith(CamOps.ShownBy(n, target));
        return shows;
    }

    /// <summary>A clean tally: frames watched, some holding, some in the reading view at rest, nothing over the work.</summary>
    private static CamOps.ChromeTally Clean(int frames = 120, int holding = 30, int reading = 50) =>
        new CamOps.ChromeTally { Frames = frames, HoldingFrames = holding, ReadingFrames = reading, Over = 0, Missing = string.Empty, ScreenSize = "1920x1080", Violations = None };

    private static CamOps.PlateSeen Seen(string key, int shown, int over) => new CamOps.PlateSeen(key, shown, over, None);

    private static CamOps.ChromeLine Line(List<CamOps.ChromeLine> lines, string id)
    {
        List<CamOps.ChromeLine> found = lines.Where(l => l.Id == id).ToList();
        Assert.AreEqual(1, found.Count, $"exactly one line {id}; the lines: {string.Join(", ", lines.Select(l => l.Id))}");
        return found[0];
    }
}
