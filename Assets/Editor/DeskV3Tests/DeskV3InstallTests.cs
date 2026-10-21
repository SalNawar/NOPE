using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// The painted desk's install in Unity (desk v3; the code review of round 6:
/// no test covered the changes to DeskPainted, the binder's install,
/// HoverHighlighter, DeskController or MachineSlideIn). The first test is
/// pure (the QA editor runner runs it outside Unity): on the painted desk
/// every decoration is cut. The "NeedsEditor" tests make Unity objects and
/// run only in Unity's EditMode suite: a gameplay object wearing its painted
/// look draws no old part but the kept ones; the audit names every old part
/// still drawn or clicked, and none on a clean desk; the old click boxes the painted
/// look has no part for (the daters' side buttons, the chip reader's readout)
/// take no click; the RING SERVICE panel shows nothing; the supply crate
/// wears painting-matched boxes over its own parts; the painted glasses and
/// book take the hover outline; only the next tool's lamp is lit; the hover
/// outline's mask draws before its hull and its shader compiles; a dater
/// stood on the painted rack goes home there; a paper carried over a
/// machine's swept strip keeps over it; a squashed lift keeps its base down,
/// and one raised when the obstacles are measured counts once.
/// </summary>
public class DeskV3InstallTests
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    [Test]
    public void OnThePaintedDesk_EveryDecoration_IsCut_SoNoneIsGatheredDrawnOrDragged()
    {
        // The review of the Unity capture (S1, 6): the form sorter and the lavender tray with its paper (DeskDecor's two items).
        var layout = new DeskLayoutSettings();
        CollectionAssert.AreEqual(DeskPaintedMap.CutParts(), layout.paintedCutParts, "the plan's default is the painted map's");
        Assert.IsNotEmpty(layout.decor);
        foreach (DeskDecorItem item in layout.decor)
            foreach (string prefix in item.parts)
                Assert.IsTrue(DeskPaintedMap.Cuts(prefix + "__Part", Array.Empty<string>(), layout.paintedCutParts), $"{item.id}: its part {prefix} is cut on the painted desk");
    }

    [Test, Category("NeedsEditor")]
    public void AGameplayObjectWearingItsPaintedLook_DrawsNoOldPartButTheKeptOnes_AndOutlinesThePaintedOne()
    {
        var root = new GameObject("DeskPainted");
        var scanner = new GameObject("Scanner");
        try
        {
            GameObject painted = Cube("Desk_Scanner", root.transform, Vector3.zero, Vector3.one * 0.1f);
            var desk = root.AddComponent<DeskPainted>();
            desk.Configure(new[] { new DeskPainted.Part { name = "Desk_Scanner", root = painted.transform, mesh = painted.transform } }, null, null, 1.069f, null);
            Renderer body = Cube("Body", scanner.transform, Vector3.zero, Vector3.one * 0.1f).GetComponent<Renderer>();
            Renderer sweep = Cube("SweepBar", scanner.transform, Vector3.up * 0.06f, new Vector3(0.1f, 0.002f, 0.01f)).GetComponent<Renderer>();
            Renderer readout = Cube("Readout", scanner.transform, Vector3.left * 0.06f, Vector3.one * 0.02f).GetComponent<Renderer>();
            var click = scanner.AddComponent<Clickable>();

            Renderer[] look = desk.Reskin("Desk_Scanner", scanner.transform, scanner.transform, DeskPaintedMap.Kept(DeskHomeMap.Scanner));
            Assert.IsFalse(body.enabled, "the old body");
            Assert.IsFalse(readout.enabled, "the old readout (the review: a grey box stuck to the scanner's left face)");
            Assert.IsTrue(sweep.enabled, "its sweep light stays (DeskPaintedMap.Kept)");
            Assert.AreSame(scanner.transform, painted.transform.parent, "the painted scanner rides it");
            Assert.IsTrue(painted.GetComponent<Renderer>().enabled);
            CollectionAssert.AreEqual(look, click.Outline, "its hover outline draws the painted scanner");
        }
        finally
        {
            Object.DestroyImmediate(scanner);
            Object.DestroyImmediate(root);
        }
    }

    [Test, Category("NeedsEditor")]
    public void TheAudit_NamesEveryOldPartStillDrawnOrClicked_AndNothingOnACleanDesk()
    {
        var root = new GameObject("DeskPainted");
        var rack = new GameObject("StampRack");
        var panel = new GameObject("RingPanel");
        try
        {
            GameObject painted = Cube("Desk_StampRack", root.transform, Vector3.zero, Vector3.one * 0.1f);
            var desk = root.AddComponent<DeskPainted>();
            desk.Configure(new[] { new DeskPainted.Part { name = "Desk_StampRack", root = painted.transform, mesh = painted.transform } }, null, null, 1.069f, null);
            Cube("Drawer", rack.transform, Vector3.zero, Vector3.one * 0.1f);
            var dater = new GameObject("Approved").transform;
            dater.SetParent(rack.transform, false);
            (BoxCollider button, _) = ClickBox("Button", dater);
            var standIn = new GameObject("StandIn_rolodex").transform;
            standIn.SetParent(rack.transform, false);
            Cube("Cards", standIn, Vector3.zero, Vector3.one * 0.05f);

            CollectionAssert.AreEquivalent(new[] { "Drawer", "Approved/Button (click box)" }, desk.OldParts(rack.transform, DeskHomeMap.StampRack), "the old drawer still drawn, the side button still live (a stand-in's box is no old part)");
            desk.Reskin("Desk_StampRack", rack.transform, rack.transform, DeskPaintedMap.Kept(DeskHomeMap.StampRack));
            DeskPainted.Silence(rack.transform, DeskPaintedMap.DeadClicks(DeskHomeMap.StampRack));
            Renderer standInCards = standIn.GetComponentInChildren<Renderer>();
            standInCards.enabled = true; // a stand-in draws; the reskin hid it with the rest in this made-up rack
            CollectionAssert.IsEmpty(desk.OldParts(rack.transform, DeskHomeMap.StampRack), "clean: the painted rack rides it, the button is off");
            Assert.IsTrue(button.gameObject.activeSelf);

            Cube("PanelFace", panel.transform, Vector3.zero, Vector3.one * 0.2f);
            CollectionAssert.AreEquivalent(new[] { "PanelFace", "PanelFace (click box)" }, desk.OldParts(panel.transform, DeskHomeMap.Switchboard), "an unshown object: anything drawn or live");
            DeskPainted.Unshow(panel.transform);
            CollectionAssert.IsEmpty(desk.OldParts(panel.transform, DeskHomeMap.Switchboard));
        }
        finally
        {
            Object.DestroyImmediate(rack);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(root);
        }
    }

    [Test, Category("NeedsEditor")]
    public void TheDatersSideButtons_AndTheReadoutsBox_TakeNoClick_AndTheRingPanelShowsNothing()
    {
        var dater = new GameObject("Approved");
        var reader = new GameObject("ChipReader");
        var panel = new GameObject("RingPanel");
        try
        {
            BoxCollider daterBox = dater.AddComponent<BoxCollider>();
            Clickable daterClick = dater.AddComponent<Clickable>();
            var body = new GameObject("Body").transform;
            body.SetParent(dater.transform, false);
            (BoxCollider buttonBox, Clickable button) = ClickBox("Button", body);
            DeskPainted.Silence(dater.transform, DeskPaintedMap.DeadClicks(DeskHomeMap.StampRack));
            Assert.IsFalse(buttonBox.enabled, "the side button's box: no ray reaches it");
            Assert.IsFalse(button.enabled, "nor does it outline");
            Assert.IsFalse(button.Interactable);
            Assert.IsTrue(daterBox.enabled, "the dater's own box (fitted to the painted dater) stays");
            Assert.IsTrue(daterClick.enabled);

            (BoxCollider readoutBox, Clickable readout) = ClickBox("ReadoutClick", reader.transform);
            DeskPainted.Silence(reader.transform, DeskPaintedMap.DeadClicks(DeskHomeMap.ChipReader));
            Assert.IsFalse(readoutBox.enabled, "the readout's box under the painted reader");
            Assert.IsFalse(readout.enabled);

            Renderer face = Cube("PanelFace", panel.transform, Vector3.zero, new Vector3(0.5f, 0.4f, 0.05f)).GetComponent<Renderer>();
            (BoxCollider breakerBox, Clickable breaker) = ClickBox("Breaker1Click", panel.transform);
            DeskPainted.Unshow(panel.transform);
            panel.SetActive(false);
            panel.SetActive(true); // its owner shows it on a day a repair is owned (UpgradeObjects.PanelShown)
            Assert.IsFalse(face.enabled, "the RING SERVICE panel draws nothing");
            Assert.IsFalse(face.GetComponent<Collider>().enabled);
            Assert.IsFalse(breakerBox.enabled, "nor takes a click");
            Assert.IsFalse(breaker.enabled);
        }
        finally
        {
            Object.DestroyImmediate(dater);
            Object.DestroyImmediate(reader);
            Object.DestroyImmediate(panel);
        }
    }

    [Test, Category("NeedsEditor")]
    public void TheSupplyCrate_WearsPaintingMatchedBoxesOverItsOwnParts_ItsTagStays_ItsTapeHides()
    {
        var root = new GameObject("DeskPainted");
        var crate = new GameObject("SupplyCrate");
        try
        {
            var desk = root.AddComponent<DeskPainted>();
            desk.Configure(Array.Empty<DeskPainted.Part>(), null, null, 1.069f, null);
            Renderer body = Cube("Crate", crate.transform, new Vector3(0f, 0.0645f, 0f), new Vector3(0.24f, 0.129f, 0.18f)).GetComponent<Renderer>();
            Renderer flap = Cube("Flap_Front", crate.transform, new Vector3(0f, 0.1285f, 0.0475f), new Vector3(0.233f, 0.001f, 0.085f)).GetComponent<Renderer>();
            Renderer tape = Cube("Tape", crate.transform, new Vector3(0f, 0.13f, 0f), new Vector3(0.045f, 0.002f, 0.18f)).GetComponent<Renderer>();
            GameObject labelQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            labelQuad.name = "Label";
            labelQuad.transform.SetParent(crate.transform, false);
            labelQuad.transform.localPosition = new Vector3(0f, 0.052f, -0.0903f);
            labelQuad.transform.localScale = new Vector3(0.105f, 0.07f, 1f);
            Renderer label = labelQuad.GetComponent<Renderer>();
            var tag = new GameObject("Tag").transform;
            tag.SetParent(crate.transform, false);
            Renderer card = Cube("Card", tag, Vector3.up * 0.1f, new Vector3(0.24f, 0.1f, 0.001f)).GetComponent<Renderer>();
            (_, Clickable click) = ClickBox("Click", crate.transform);
            Bounds bodyWas = body.bounds, flapWas = flap.bounds;

            Renderer[] boxes = desk.WearBoxes(crate.transform, DeskHomeMap.Crate);
            Assert.AreEqual(3, boxes.Length, "its body, its flap and its label");
            foreach (Renderer old in new[] { body, flap, label, tape })
                Assert.IsFalse(old.enabled, old.name + ": the old model never draws");
            Assert.IsNull(tape.transform.Find(DeskPainted.PartBoxName), "its tape: no box");
            Assert.IsTrue(card.enabled, "its tag, the delivery's live label, stays");
            CollectionAssert.AreEquivalent(boxes, click.Outline, "its click outlines the boxes");

            Bounds bodyBox = body.transform.Find(DeskPainted.PartBoxName).GetComponent<Renderer>().bounds;
            Assert.AreEqual(bodyWas.min.y, bodyBox.min.y, 1e-4f, "the body's box stands where it stood");
            Assert.AreEqual(bodyWas.max.y - 0.002f, bodyBox.max.y, 1e-4f, "its top 2 mm under the flaps (never two faces in one plane)");
            Assert.AreEqual(bodyWas.size.x, bodyBox.size.x, 1e-4f);
            Bounds flapBox = flap.transform.Find(DeskPainted.PartBoxName).GetComponent<Renderer>().bounds;
            Assert.AreEqual(flapWas.center.y, flapBox.center.y, 1e-4f, "the flap's box rides the flap");
            Assert.AreEqual(flapWas.size.z, flapBox.size.z, 1e-4f);
            Bounds labelBox = label.transform.Find(DeskPainted.PartBoxName).GetComponent<Renderer>().bounds;
            Assert.GreaterOrEqual(labelBox.size.z, 0.001f - 1e-5f, "a flat label still makes a box, 1 mm thick");

            Assert.AreEqual(3, desk.WearBoxes(crate.transform, DeskHomeMap.Crate).Length, "a second call builds nothing more");
            Assert.AreEqual(3, crate.GetComponentsInChildren<Renderer>(true).Count(r => r.name == DeskPainted.PartBoxName));
        }
        finally
        {
            Object.DestroyImmediate(crate);
            Object.DestroyImmediate(root);
        }
    }

    [Test, Category("NeedsEditor")]
    public void ThePaintedGlasses_TakeTheHoverOutline_AndAnswerAClick_Once()
    {
        var root = new GameObject("DeskPainted");
        try
        {
            GameObject glasses = Cube("Desk_Glasses", root.transform, Vector3.zero, new Vector3(0.12f, 0.04f, 0.05f));
            var book = new GameObject("Desk_RulebookFolded", typeof(MeshFilter), typeof(MeshRenderer));
            book.transform.SetParent(root.transform, false);
            book.GetComponent<MeshFilter>().sharedMesh = glasses.GetComponent<MeshFilter>().sharedMesh;
            var desk = root.AddComponent<DeskPainted>();
            desk.Configure(new[]
            {
                new DeskPainted.Part { name = "Desk_Glasses", root = glasses.transform, mesh = glasses.transform },
                new DeskPainted.Part { name = DeskPaintedMap.RulebookFolded, root = book.transform, mesh = book.transform },
            }, null, null, 1.069f, null);
            int clicks = 0;
            Clickable g = desk.EnsureClickable("Desk_Glasses", () => clicks++);
            Assert.IsNotNull(g, "on the glasses' own box");
            CollectionAssert.AreEqual(desk.RenderersOf("Desk_Glasses"), g.Outline, "the outline draws the painted glasses");
            Assert.AreSame(g, desk.EnsureClickable("Desk_Glasses", () => clicks++), "made once");
            g.onClick.Invoke();
            Assert.AreEqual(1, clicks, "one answer a click, never two");
            Clickable b = desk.EnsureClickable(DeskPaintedMap.RulebookFolded, null);
            Assert.IsNotNull(b, "the book gets its box and its outline");
            Assert.IsNotNull(book.GetComponent<BoxCollider>());
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test, Category("NeedsEditor")]
    public void ThePaintedDesk_LightsOnlyTheNextToolsLamp_NoneAtRest()
    {
        var root = new GameObject("DeskPainted");
        try
        {
            var lamps = new GameObject("Lamps").transform;
            lamps.SetParent(root.transform, false);
            string[] names = { "lamp_tray", "lamp_bell", "lamp_stamp", "lamp_scanner", "lamp_chip", "lamp_intercom" };
            foreach (string name in names)
            {
                var lamp = new GameObject(name).transform;
                lamp.SetParent(lamps, false);
                Cube("Jewel", lamp, Vector3.zero, Vector3.one * 0.02f);
            }
            var desk = root.AddComponent<DeskPainted>();
            Renderer Jewel(string lamp) => lamps.Find(lamp + "/Jewel").GetComponent<Renderer>();
            desk.Light(DeskHomeMap.Scanner);
            CollectionAssert.AreEqual(new[] { "lamp_scanner" }, names.Where(n => Jewel(n).enabled), "the scanner's lamp alone");
            desk.Light(DeskHomeMap.Bell);
            CollectionAssert.AreEqual(new[] { "lamp_bell" }, names.Where(n => Jewel(n).enabled));
            desk.Light(null);
            Assert.IsEmpty(names.Where(n => Jewel(n).enabled), "nothing next: every lamp unlit (the review: all six glowed at rest)");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test, Category("NeedsEditor")]
    public void TheHoverOutline_MasksTheSilhouetteBeforeItsHull_AndItsShaderCompiles()
    {
        Shader shader = Shader.Find("TimeDesk/HoverHull");
        Assert.IsNotNull(shader, "TimeDesk/HoverHull");
        Assert.IsFalse(ShaderUtil.ShaderHasError(shader), "it compiles");
        var hull = new Material(shader);
        var mask = new Material(shader);
        try
        {
            HoverHighlighter.Stencil(hull, false);
            HoverHighlighter.Stencil(mask, true);
            Assert.Less(mask.renderQueue, hull.renderQueue, "every mask before every hull");
            Assert.AreEqual(1f, mask.GetFloat("_Mask"));
            Assert.AreEqual((float)UnityEngine.Rendering.CompareFunction.Always, mask.GetFloat("_StencilComp"));
            Assert.AreEqual((float)UnityEngine.Rendering.StencilOp.Replace, mask.GetFloat("_StencilPass"));
            Assert.AreEqual(0f, mask.GetFloat("_ColorMask"), "the mask writes no colour");
            Assert.AreEqual(0f, hull.GetFloat("_Mask"));
            Assert.AreEqual((float)UnityEngine.Rendering.CompareFunction.NotEqual, hull.GetFloat("_StencilComp"), "the hull only where the mask is not");
            Assert.AreEqual((float)UnityEngine.Rendering.StencilOp.Keep, hull.GetFloat("_StencilPass"));
            Assert.AreEqual(15f, hull.GetFloat("_ColorMask"));
        }
        finally
        {
            Object.DestroyImmediate(hull);
            Object.DestroyImmediate(mask);
        }
    }

    [Test, Category("NeedsEditor")]
    public void ADaterStoodOnThePaintedRack_GoesHomeThere()
    {
        // The code review of round 6: StandDater not making the painted place the dater's home survived every test.
        var trayObject = new GameObject("StampTray");
        var rack = new GameObject("Rack").transform;
        var cradle = new GameObject("Cradle").transform;
        try
        {
            var tray = trayObject.AddComponent<DeskStampTray>();
            var dater = new GameObject("Approved");
            dater.transform.SetParent(cradle, false);
            dater.transform.localPosition = new Vector3(0.07f, 0f, 0.023f);
            Clickable click = dater.AddComponent<Clickable>();
            typeof(DeskStampTray).GetField("approvedStamp", Private).SetValue(tray, click);
            typeof(DeskStampTray).GetMethod("Awake", Private).Invoke(tray, null);

            Transform root = tray.StandDater(DeskStamp.Approved, rack, new Vector3(0.9f, 1.11f, -0.08f), Quaternion.Euler(0f, 90f, 0f));
            Assert.AreSame(dater.transform, root);
            Assert.AreSame(rack, root.parent, "out of its cradle (the drawer's fold no longer turns it)");
            object handle = ((Array)typeof(DeskStampTray).GetField("_handles", Private).GetValue(tray)).Cast<object>()
                .First(h => (Clickable)h.GetType().GetField("Click").GetValue(h) == click);
            Assert.AreEqual(root.localPosition, (Vector3)handle.GetType().GetField("Home").GetValue(handle), "its home is the painted place: a stroke and its way back end there");
            Assert.IsFalse((bool)handle.GetType().GetField("Returning").GetValue(handle));
            if (typeof(DeskStampTray).GetField("_thunk", Private).GetValue(tray) is Object thunk)
                Object.DestroyImmediate(thunk); // the refusal's tone Awake made
        }
        finally
        {
            Object.DestroyImmediate(trayObject);
            Object.DestroyImmediate(rack.gameObject);
            Object.DestroyImmediate(cradle.gameObject);
        }
    }

    [Test, Category("NeedsEditor")]
    public void APaperCarriedOverAMachinesSweptStrip_KeepsOverIt_BeyondItsRestFootprint()
    {
        // The code review (round 3, 5; round 6: no SweptBoxes.Add, a carry floor without TrySweptTop and empty strips all survived).
        var settings = new DeskLayoutSettings();
        var frame = new DeskFrame(new Vector3(0f, 1f, 0f), Vector3.forward);
        GameObject scanner = null, deskObject = null;
        try
        {
            DeskLayout.Bind(frame, settings.mat.Rect, settings);
            DeskHome home = settings.HomeOf(DeskHomeMap.Scanner);
            scanner = Cube("Scanner", null, frame.Point(new Vector2(home.x, home.y), 0.06f), new Vector3(0.4f, 0.12f, 0.2f));
            DeskLayout.Track(DeskHomeMap.Scanner, scanner.transform);
            DeskLayout.RefreshObstacles();

            deskObject = new GameObject("Desk");
            var desk = deskObject.AddComponent<DeskController>();
            var config = ScriptableObject.CreateInstance<DeskConfigSO>();
            typeof(DeskController).GetField("config", Private).SetValue(desk, config);
            MethodInfo carry = typeof(DeskController).GetMethod("CarryFloor", Private);
            DeskRect f = home.Footprint;
            Vector3 inStrip = frame.Point(new Vector2(f.CentreX - f.Width / 2f - 0.01f, f.CentreY), 0.001f);
            float floor = (float)carry.Invoke(desk, new object[] { inStrip, 0f, 0.001f });
            Assert.AreEqual(1.12f + config.dockClearance + 0.001f - inStrip.y, floor, 1e-4f, "over the strip the scanner slides into: over its box, as at its peak");
            Vector3 away = frame.Point(new Vector2(-0.3f, -0.3f), 0.001f);
            Assert.AreEqual(0f, (float)carry.Invoke(desk, new object[] { away, 0f, 0.001f }), 1e-6f, "over the bare mat: on it");
            Assert.IsNotEmpty(DeskLayout.SweptStrips(), "a paper let go has the scanner's strip to keep clear of");
            Object.DestroyImmediate(config);
        }
        finally
        {
            DeskLayout.Unbind();
            if (scanner != null)
                Object.DestroyImmediate(scanner);
            if (deskObject != null)
                Object.DestroyImmediate(deskObject);
        }
    }

    [Test, Category("NeedsEditor")]
    public void ASquashedLift_KeepsItsBaseDown_AndARaisedOne_CountsOnceInTheObstacles()
    {
        // The code review (round 3, 4; round 6: MachineSlideIn never applying SquashDrop survived): the UV torch's pivot is its grip
        // axis, 3.4 cm over its cradle.
        var settings = new DeskLayoutSettings();
        var frame = new DeskFrame(new Vector3(0f, 1f, 0f), Vector3.forward);
        var slideInObject = new GameObject("SlideIn");
        var torch = new GameObject("UvTorch");
        DeskSlideIn slideIn = null;
        try
        {
            DeskLayout.Bind(frame, settings.mat.Rect, settings);
            slideIn = slideInObject.AddComponent<DeskSlideIn>();
            slideIn.Bind(null, settings.slides, settings.Homes, null, () => false, null, null);
            DeskHome home = settings.HomeOf(DeskHomeMap.UvTorch);
            torch.transform.position = frame.Point(new Vector2(home.x, home.y), 0.034f);
            var slide = new GameObject("Slide").transform;
            slide.SetParent(torch.transform, false);
            Renderer body = Cube("Body", slide, Vector3.zero, new Vector3(0.2f, 0.068f, 0.05f)).GetComponent<Renderer>();
            MachineSlideIn machine = MachineSlideIn.Attach(DeskHomeMap.UvTorch, torch.transform);
            Assert.IsNotNull(machine, "the torch lifts on its home");
            typeof(MachineSlideIn).GetMethod("Measure", Private).Invoke(machine, new object[] { DeskLayout.Frame });
            Bounds rest = body.bounds;

            DeskSlider lift = settings.slides.Find(DeskHomeMap.UvTorch);
            machine.Pose = new SlidePose(SlidePhase.Out, 0f, settings.slides.squashPx, 0f, 0f, 0f, 1f);
            typeof(MachineSlideIn).GetMethod("Show", Private).Invoke(machine, null);
            Bounds squashed = body.bounds;
            Assert.AreEqual(rest.min.y, squashed.min.y, 1e-5f, "its base stays on its cradle");
            Assert.AreEqual(rest.max.y - settings.slides.squashPx * lift.MetresPerPx, squashed.max.y, 1e-4f, "its top comes down by the squash's px");

            typeof(MachineSlideIn).GetMethod("TakeBack", Private).Invoke(machine, null);
            Assert.AreEqual(rest.min.y, body.bounds.min.y, 1e-6f, "taken off, it stands as it did");
            Assert.AreEqual(rest.max.y, body.bounds.max.y, 1e-6f);

            // Raised to its slid spot when the obstacles are measured (a crate's pop; the code review, round 3, 7): its home counts it once.
            machine.Pose = new SlidePose(SlidePhase.Out, 1f, 0f, 0f, 0f, 0f, 1f);
            typeof(MachineSlideIn).GetMethod("Show", Private).Invoke(machine, null);
            Assert.AreEqual(rest.max.y + lift.travel, body.bounds.max.y, 1e-4f, "risen by its travel");
            DeskLayout.Track(DeskHomeMap.UvTorch, torch.transform);
            DeskLayout.RefreshObstacles();
            DeskRect pouch = home.Footprint;
            float reach = Math.Min(lift.travel * lift.Peak, lift.Reach(settings.slides.feltGap));
            Assert.AreEqual(rest.max.y - frame.Origin.y + reach, DeskLayout.Obstacles.HeightAt(pouch.CentreX, pouch.CentreY), 1e-4f, "its rest height raised by its peak, never by its rise twice");
        }
        finally
        {
            if (slideIn != null)
                typeof(DeskSlideIn).GetMethod("OnDisable", Private).Invoke(slideIn, null); // DeskSlideIn.Current let go
            DeskLayout.Unbind();
            Object.DestroyImmediate(torch);
            Object.DestroyImmediate(slideInObject);
        }
    }

    /// <summary>A cube named <paramref name="name"/> under <paramref name="parent"/> (world space when none) at <paramref name="at"/>, <paramref name="size"/> metres.</summary>
    private static GameObject Cube(string name, Transform parent, Vector3 at, Vector3 size)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        if (parent != null)
            cube.transform.SetParent(parent, false);
        cube.transform.localPosition = at;
        cube.transform.localScale = size;
        return cube;
    }

    /// <summary>A click box named <paramref name="name"/> under <paramref name="parent"/>.</summary>
    private static (BoxCollider box, Clickable click) ClickBox(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return (go.AddComponent<BoxCollider>(), go.AddComponent<Clickable>());
    }
}
