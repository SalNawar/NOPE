using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;

/// <summary>
/// The desk's slide-in data (desk v3; Saleh: "yes but they need to slide"):
/// the approved sliders are the desk pipeline's render/slides.json (its copy
/// in tools/props/desk/slides.json, so the tests hold the gate's own numbers),
/// their curves play the gate's frames, every track keeps its felt and every
/// slider its lamps clear (the chip reader 3 px at its peak), they agree with
/// the desk home map (a track slides the way its home declares: the map is
/// the one source of the direction), a home's obstacle is its footprint
/// swept by its tracks and raised by its lifts, and every cue sounds now,
/// for every slider (its stand-in a bank clip or a placeholder).
/// </summary>
public class DeskSlidesTests
{
    private const string GoldenPath = "tools/props/desk/slides.json";

    /// <summary>The pipeline's slides.json (the gate's numbers).</summary>
    private static ContentNode Golden([CallerFilePath] string here = "")
    {
        string path = File.Exists(GoldenPath) ? GoldenPath : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", GoldenPath);
        return ContentJson.Parse(File.ReadAllText(path));
    }

    private static float Num(ContentNode node) => float.Parse(node.Text, CultureInfo.InvariantCulture);

    private static float Num(ContentNode node, string key) => Num(node.Get(key));

    private static IEnumerable<KeyValuePair<string, ContentNode>> GoldenSliders() => Golden().Get("sliders").Members;

    /// <summary>A file of the project, from the tests' working folder or this file's.</summary>
    private static string Repo(string relative, [CallerFilePath] string here = "") =>
        File.ReadAllText(File.Exists(relative) ? relative : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", relative));

    /// <summary>The gate's felt rule on a measure (UNITY_PLAN 5.1, track_check.py): at least feltGap metres and the gate's felt_gap_px at 1080p.</summary>
    private static bool FeltHolds(DeskSlides slides, float metres, float px) => metres >= slides.feltGap - 1e-5f && px >= Num(Golden(), "felt_gap_px");

    /// <summary>The gate's lamp rule on a measure (UNITY_PLAN 4.3): lamp_gap_px clear settled and at the peak, or a pair already closer at rest getting no closer; a lamp nearer the camera is an occluder, not a contact.</summary>
    private static bool LampHolds(float rest, float settled, float peak, bool lampInFront)
    {
        float closest = Math.Min(settled, peak);
        return lampInFront || closest >= Num(Golden(), "lamp_gap_px") || closest >= rest;
    }

    [Test]
    public void TheApprovedSlides_AreThePipelinesSlidesJson()
    {
        ContentNode golden = Golden();
        DeskSlides slides = DeskSlides.Approved();
        Assert.AreEqual(Num(golden.Get("retract"), "seconds"), slides.retractSeconds, 1e-5f);
        Assert.AreEqual(Num(golden.Get("retract"), "winner_after"), slides.winnerAfter, 1e-5f);
        Assert.AreEqual(Num(golden.Get("retract"), "winner_below"), slides.winnerBelow, 1e-5f);
        Assert.AreEqual(Num(golden.Get("out"), "seconds"), slides.outSeconds, 1e-5f);
        Assert.AreEqual(Num(golden.Get("out"), "anticipation_s"), slides.anticipationSeconds, 1e-5f);
        Assert.AreEqual(Num(golden.Get("back"), "linger"), slides.lingerSeconds, 1e-5f);
        Assert.AreEqual(Num(golden.Get("back"), "seconds"), slides.backSeconds, 1e-5f);
        Assert.AreEqual(Num(golden.Get("back"), "seat_s"), slides.seatSeconds, 1e-5f);
        Assert.AreEqual("easeInCubic", golden.Get("back").Get("ease").Text, "the way home arrives at speed (rereview 5)");
        Assert.AreEqual("easeInOutQuad", golden.Get("retract").Get("ease").Text);
        Assert.AreEqual(Num(golden, "felt_gap_m"), slides.feltGap, 1e-6f);
        Assert.AreEqual(Num(golden.Get("lift_squash"), "px"), slides.squashPx, 1e-6f);
        Assert.AreEqual(Num(golden.Get("lift_squash"), "release_s"), slides.squashRelease, 1e-6f);
        Assert.AreEqual(Num(golden.Get("lift_shadow"), "alpha"), slides.liftShadowAlpha, 1e-6f);
        Assert.AreEqual(Num(golden.Get("lift_shadow"), "fade"), slides.liftShadowFade, 1e-6f);
        Assert.AreEqual(Num(golden.Get("lift_shadow"), "blur_px"), slides.liftShadowBlurPx, 1e-6f);
        Assert.AreEqual(Num(golden.Get("lift_shadow"), "blur_per_px"), slides.liftShadowBlurPerPx, 1e-6f);
        Assert.AreEqual(Num(golden.Get("lift_shadow"), "widen_per_px"), slides.liftShadowWidenPerPx, 1e-6f);

        CollectionAssert.AreEquivalent(GoldenSliders().Select(m => m.Key), slides.sliders.Select(s => s.id), "one row per slider of slides.json");
        foreach (KeyValuePair<string, ContentNode> row in GoldenSliders())
        {
            string id = row.Key;
            ContentNode g = row.Value;
            DeskSlider s = slides.Find(id);
            Assert.AreEqual(g.Get("kind").Text, s.kind.ToString().ToLowerInvariant(), id);
            Assert.AreEqual(Num(g, "travel"), s.travel, 1e-6f, id);
            if (s.kind == SlideKind.Track)
            {
                Assert.AreEqual(0f, Num(g.Get("dir").Items[1]), 1e-6f, id + ": sideways along the desk");
                float way = s.Way(DeskHomeMap.Find(DeskHomeMap.Approved(), s.Home));
                if (way != 0f)
                    Assert.AreEqual(Num(g.Get("dir").Items[0]), way, 1e-6f, id + ": the home map declares the way the pipeline slides it");
                else
                    Assert.AreEqual(DeskHomeMap.Intercom, id, "only the intercom waits for its home to declare its slide (DESK-PLAN's open call)");
                Assert.AreEqual(Num(g, "track_cut"), s.trackCut, 1e-6f, id);
                ContentNode clearance = g.Get("clearance");
                float felt = clearance.Kind == ContentNodeKind.Null ? 0f : Num(clearance.Get("rest"), "cm") / 100f;
                Assert.AreEqual(felt, s.feltAtRest, 1e-6f, id + ": the felt at rest the gate measured");
            }
            ContentNode curve = g.Get("curve");
            if (curve.Kind == ContentNodeKind.Null)
            {
                Assert.AreEqual(SlideKind.Fixed, s.kind, id);
                continue;
            }
            Assert.AreEqual(Num(curve, "travel_px"), s.travelPx, 1e-4f, id);
            Assert.AreEqual(Num(curve, "anticipation"), s.anticipation, 1e-4f, id);
            Assert.AreEqual(Num(curve, "overshoot"), s.overshoot, 1e-4f, id);
            Assert.AreEqual(Num(curve, "seat"), s.seat, 1e-4f, id);
            Assert.AreEqual(Num(curve, "damping"), s.damping, 1e-4f, id);
            Assert.AreEqual(Num(curve, "peak_s"), s.peakSeconds, 1e-4f, id);
        }
        DeskSlider glasses = slides.Find(DeskHomeMap.Glasses);
        ContentNode tip = golden.Get("tip").Get("glasses");
        Assert.AreEqual(Num(tip.Items[0]), glasses.tipDegrees, 1e-6f);
        Assert.AreEqual(Num(tip.Items[1].Items[1]), glasses.tipY, 1e-6f, "the glasses tip toward the chair");
        ContentNode sway = golden.Get("secondary").Get("intercom");
        DeskSlider intercom = slides.Find(DeskHomeMap.Intercom);
        Assert.AreEqual(Num(sway, "px"), intercom.swayPx, 1e-6f);
        Assert.AreEqual(Num(sway, "hz"), intercom.swayHz, 1e-6f);
        Assert.AreEqual(Num(sway, "decay"), intercom.swayDecay, 1e-6f);
    }

    [Test]
    public void TheCurves_PlayTheGatesFrames_OutAndBack()
    {
        DeskSlides slides = DeskSlides.Approved();
        foreach (KeyValuePair<string, ContentNode> row in GoldenSliders())
        {
            ContentNode curve = row.Value.Get("curve");
            if (curve.Kind == ContentNodeKind.Null)
                continue;
            DeskSlider s = slides.Find(row.Key);
            IReadOnlyList<ContentNode> outFrames = curve.Get("out_60fps").Items, backFrames = curve.Get("back_60fps").Items;
            for (int i = 0; i < outFrames.Count; i++)
                Assert.AreEqual(Num(outFrames[i]), SlideCurve.Out(slides, s, i / 60f), 1e-3f, $"{row.Key} out, frame {i}");
            for (int i = 0; i < backFrames.Count; i++)
                Assert.AreEqual(Num(backFrames[i]), SlideCurve.Back(slides, s, i / 60f, 1f), 1e-3f, $"{row.Key} back, frame {i}");
        }
    }

    [Test]
    public void EveryTrack_KeepsItsFeltAtThePeak_InCentimetresAndPixels_AndTheGameNeverClampsIt()
    {
        DeskSlides slides = DeskSlides.Approved();
        foreach (KeyValuePair<string, ContentNode> row in GoldenSliders())
        {
            ContentNode clearance = row.Value.Get("clearance");
            if (clearance.Kind == ContentNodeKind.Null)
                continue;
            DeskSlider s = slides.Find(row.Key);
            ContentNode peak = clearance.Get("peak");
            Assert.AreEqual(s.Peak, Num(peak, "f"), 0.002f, row.Key + ": the gate measured the peak the game plays");
            Assert.IsTrue(FeltHolds(slides, Num(peak, "cm") / 100f, Num(peak, "px")), $"{row.Key}: {Num(peak, "cm")} cm, {Num(peak, "px")} px of felt at its peak");
            Assert.LessOrEqual(s.travel * s.Peak, s.Reach(slides.feltGap), row.Key + ": the felt-gap clamp never cuts the approved overshoot");
        }
        Assert.IsFalse(FeltHolds(slides, 0.0046f, 2f), "rereview 4: the scanner at 4.3 cm kept 0.46 cm but only 2 px");
        Assert.IsFalse(FeltHolds(slides, 0.003f, 5f), "under 0.4 cm");
    }

    [Test]
    public void EverySlider_KeepsItsLampsClear_TheChipReader3pxAtItsPeak()
    {
        DeskSlides slides = DeskSlides.Approved();
        foreach (KeyValuePair<string, ContentNode> row in GoldenSliders())
        {
            ContentNode lamps = row.Value.Get("lamp_clearance");
            if (lamps.Kind == ContentNodeKind.Null)
                continue;
            Assert.AreEqual(slides.Find(row.Key).Peak, Num(lamps, "peak_f"), 0.002f, row.Key + ": measured at the peak the game plays");
            foreach (KeyValuePair<string, ContentNode> lamp in lamps.Get("lamps").Members)
            {
                ContentNode m = lamp.Value;
                Assert.IsTrue(LampHolds(Num(m, "rest"), Num(m, "settled"), Num(m, "peak"), m.Get("lamp_in_front").Text == "true"),
                              $"{row.Key} against {lamp.Key}: rest {m.Get("rest").Text}, settled {m.Get("settled").Text}, peak {m.Get("peak").Text}");
            }
        }
        ContentNode chip = Golden().Get("sliders").Get(DeskHomeMap.ChipReader).Get("lamp_clearance").Get("lamps").Get("lamp_chip");
        Assert.AreEqual(3f, Num(Golden(), "lamp_gap_px"), 1e-6f, "the gate's 3 px");
        Assert.GreaterOrEqual(Num(chip, "peak"), 3f, "the chip reader's overshoot stays 3 px clear of its lamp");
        Assert.AreEqual(0.037f, slides.Find(DeskHomeMap.ChipReader).travel, 1e-6f, "3.7 cm: at 4.5 cm it touched lamp_chip at its peak");
        Assert.IsFalse(LampHolds(26f, 2f, 1f, false), "the old 4.5 cm: 0-1 px at the peak, 2 px settled");
        Assert.IsTrue(LampHolds(1f, 0f, 0f, true), "a lamp nearer the camera is an occluder, not a contact (the intercom's)");
        Assert.IsTrue(LampHolds(0f, 2f, 2f, false), "a pair touching at rest that gets no closer (the APPROVED dater's)");
    }

    [Test]
    public void TheMachines_KeepTheScreenPxMinimums_OrTheirCap()
    {
        foreach (DeskSlider s in DeskSlides.Approved().sliders.Where(s => s.kind == SlideKind.Track))
        {
            float floor = Math.Min(3.3f, 0.2f * s.travelPx) - 0.02f;
            Assert.GreaterOrEqual(s.anticipation * s.travelPx, floor, s.id + ": the anticipation shows on screen");
            Assert.GreaterOrEqual(s.overshoot * s.travelPx, floor, s.id + ": the overshoot shows on screen");
            Assert.LessOrEqual(s.overshoot, 0.2f + 1e-4f, s.id + ": beyond 20 % the spring reads as wobble");
        }
    }

    [Test]
    public void TheApprovedSliders_AgreeWithTheHomeMap()
    {
        List<string> problems = DeskSlides.Approved().Problems(DeskHomeMap.Approved(), DeskHomeMap.SlideReach);
        CollectionAssert.IsEmpty(problems, string.Join("\n", problems));
    }

    [Test]
    public void Problems_NameEveryKindOfFault()
    {
        var homes = new[]
        {
            new DeskHome("left", 1f, 0f, 0.2f, 0.2f, 0f, 0.1f, HomeShow.Always, HomeSlide.Left),
            new DeskHome("declared", -1f, 0f, 0.2f, 0.2f, 0f, 0.1f, HomeShow.Always, HomeSlide.Right),
            new DeskHome("pouch", -1f, -0.5f, 0.2f, 0.2f, 0f, 0.1f, HomeShow.Always),
        };
        DeskSlider Good(string id) => new DeskSlider(id, null, SlideKind.Track, 0.03f)
        {
            trackCut = 0.045f, feltAtRest = 0.05f, travelPx = 15f, anticipation = 0.2f, overshoot = 0.2f, seat = 0.15f, damping = 0.495f, peakSeconds = 0.18f,
        };
        DeskSlider far = Good("pouch");
        far.travel = 0.08f;
        far.trackCut = 0.06f;
        DeskSlider pastFelt = Good("pastFelt");
        pastFelt.home = "left";
        pastFelt.feltAtRest = 0.03f;
        DeskSlider stiff = new DeskSlider("stiff", "pouch", SlideKind.Lift, 0.01f) { travelPx = 4f, overshoot = 0.2f, damping = 1.2f, peakSeconds = 0.3f };
        var slides = new DeskSlides
        {
            sliders = new[] { Good("left"), Good("left"), far, pastFelt, stiff, new DeskSlider("nowhere", null, SlideKind.Lift, 0.01f) },
            winnerAfter = 0.2f,
            winnerBelow = 0.6f,
        };
        string all = string.Join("\n", slides.Problems(homes, 0.06f));
        StringAssert.Contains("'left' is listed twice", all);
        StringAssert.Contains("'pouch' slides 8.0 cm, further than 6.0 cm", all);
        StringAssert.Contains("'pouch' overshoots to 9.60 cm, out of its 6.0 cm painted track", all);
        StringAssert.Contains("'pastFelt' overshoots to 3.60 cm, past its felt gap at 2.60 cm", all);
        StringAssert.Contains("'stiff' has a damping of 1.2", all);
        StringAssert.Contains("'stiff' peaks at 0.3 s", all);
        StringAssert.Contains("'nowhere' rests in 'nowhere', which is no home of the map", all);
        StringAssert.Contains("'declared' declares a slide but no track slides it", all);
        StringAssert.Contains("waits up to 0.2 s", all);
        StringAssert.Contains("below 0.6 of the loser's travel", all);
        StringAssert.DoesNotContain("'pouch' declares", all, "a track resting in a home that declares no slide is no fault: it stays put until the map declares one");
    }

    [Test]
    public void ATrack_SlidesTheWayItsHomeDeclares_AndStaysPutWhileItsHomeDeclaresNone()
    {
        DeskSlides slides = DeskSlides.Approved();
        DeskHome[] homes = DeskHomeMap.Approved();
        DeskSlider scanner = slides.Find(DeskHomeMap.Scanner), tuner = slides.Find(DeskHomeMap.BabelTuner), intercom = slides.Find(DeskHomeMap.Intercom);
        Assert.AreEqual(-1f, scanner.Way(DeskHomeMap.Find(homes, DeskHomeMap.Scanner)), "right of the mat: left toward it");
        Assert.AreEqual(1f, tuner.Way(DeskHomeMap.Find(homes, DeskHomeMap.BabelTuner)), "left of the mat: right toward it");
        DeskHome intercomHome = DeskHomeMap.Find(homes, DeskHomeMap.Intercom);
        Assert.AreEqual(0f, intercom.Way(intercomHome));
        Assert.IsFalse(intercom.MovesOn(intercomHome), "the intercom's home declares no slide on today's map: it waits for DESK-PLAN");
        intercomHome.slide = HomeSlide.Right;
        Assert.IsTrue(intercom.MovesOn(intercomHome), "the day the map declares it, it slides with no change here");
        Assert.AreEqual(1f, intercom.Way(intercomHome));
        Assert.IsTrue(slides.Find(DeskHomeMap.UvTorch).MovesOn(DeskHomeMap.Find(homes, DeskHomeMap.UvTorch)), "a lift needs only its home");
        Assert.IsFalse(slides.Find("stamp_approve").MovesOn(null), "no home, no move");
        Assert.AreEqual(0f, slides.Find(DeskHomeMap.UvTorch).Way(DeskHomeMap.Find(homes, DeskHomeMap.UvTorch)), "a lift has no way");
    }

    [Test]
    public void AHomesObstacle_IsItsFootprintSweptByItsTracks_AndRaisedByItsLifts()
    {
        DeskSlides slides = DeskSlides.Approved();
        DeskHome[] homes = DeskHomeMap.Approved();
        DeskHome scannerHome = DeskHomeMap.Find(homes, DeskHomeMap.Scanner);
        DeskSlider scanner = slides.Find(DeskHomeMap.Scanner);
        (DeskRect swept, float h) = slides.Obstacle(scannerHome, 0.13f);
        Assert.AreEqual(scannerHome.Footprint.Width + scanner.travel * scanner.Peak, swept.Width, 1e-5f, "swept to its overshoot peak toward the mat");
        Assert.AreEqual(scannerHome.Footprint.CentreX - scanner.travel * scanner.Peak / 2f, swept.CentreX, 1e-5f, "the way its home declares: left");
        Assert.AreEqual(0.13f, h, 1e-6f);
        DeskHome torchHome = DeskHomeMap.Find(homes, DeskHomeMap.UvTorch);
        DeskSlider torch = slides.Find(DeskHomeMap.UvTorch);
        (DeskRect same, float top) = slides.Obstacle(torchHome, 0.06f);
        Assert.AreEqual(torchHome.Footprint.Width, same.Width, 1e-6f);
        Assert.AreEqual(0.06f + torch.travel * torch.Peak, top, 1e-6f, "a lift raises its top");
        DeskSlider dater = slides.Find("stamp_approve");
        Assert.AreEqual(0.08f + dater.travel * dater.Peak, slides.Obstacle(DeskHomeMap.Find(homes, DeskHomeMap.StampRack), 0.08f).height, 1e-6f,
                        "the rack rises by a dater at its 1.44 cm peak: a paper carried over it clears it");
        DeskSlider book = slides.Find("rulebook");
        Assert.AreEqual(0.03f + book.travel * book.Peak, slides.Obstacle(DeskHomeMap.Find(homes, DeskHomeMap.RulebookPocket), 0.03f).height, 1e-6f, "the pocket by the rulebook's 2.15 cm");
        Assert.AreEqual(0.09f, slides.Obstacle(DeskHomeMap.Find(homes, DeskHomeMap.Detain), 0.09f).height, 1e-6f, "DETAIN never moves");
        DeskHome intercomHome = DeskHomeMap.Find(homes, DeskHomeMap.Intercom);
        Assert.AreEqual(intercomHome.Footprint.Width, slides.Obstacle(intercomHome, 0.12f).footprint.Width, 1e-6f, "the intercom stays put until its home declares a slide");
    }

    [Test]
    public void EveryCue_SoundsForEverySlider_ItsStandInABankClipOrAPlaceholder_UntilTheSoundListDeliversIt()
    {
        // The review: the bank has no machine_slide_unlatch, machine_slide_stop or machine_seat, and drawer_close is no bank cue, so the
        // unlatch tick, the knock and the arrival clack never sounded and lifts were silent. Each now falls back as CueSounds does.
        DeskSlides slides = DeskSlides.Approved();
        Assert.AreEqual(("machine_slide_unlatch", SoundCues.RelayClack), Pair(slides.Sound(SlideCueKind.Unlatch)), "the unlatch: the relay's clack, low");
        Assert.AreEqual(("machine_slide", SoundCues.PaperSlide), Pair(slides.Sound(SlideCueKind.Slide)), "the felt shush: the paper slide");
        Assert.AreEqual(("machine_slide_stop", SoundCues.CartridgeIn), Pair(slides.Sound(SlideCueKind.Peak)), "the knock: the cartridge's thunk");
        Assert.AreEqual(("machine_slide_back", SoundCues.PaperSlide), Pair(slides.Sound(SlideCueKind.Back)), "the reversed shush");
        Assert.AreEqual(("machine_seat", SoundCues.DetainCover), Pair(slides.Sound(SlideCueKind.Seat)), "the seat's clack: the cover's snap");
        Assert.AreEqual(("machine_slide_back", SoundCues.PaperSlide), Pair(slides.Sound(SlideCueKind.HandOff)), "the hand-off loser: the reversed shush, cut short");
        Assert.Less(slides.Sound(SlideCueKind.Unlatch).pitch, 1f, "the tick very low");
        Assert.Less(slides.Sound(SlideCueKind.Peak).pitch, 1f, "the knock muted");

        string bank = Repo("Assets/Data/Config/SoundBank_Default.asset").Replace("\r\n", "\n");
        string cueSounds = Repo("Assets/Scripts/UI/Motion/CueSounds.cs");
        Dictionary<string, string> names = typeof(SoundCues).GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.IsLiteral)
                                                           .ToDictionary(f => (string)f.GetRawConstantValue(), f => f.Name);
        bool Clip(string id) => bank.Split(new[] { "\n  - id: " }, StringSplitOptions.None).Skip(1)
                                    .Any(block => block.Split('\n')[0].Trim() == id && block.Contains("fileID: 8300000"));
        foreach (SlideCueKind kind in Enum.GetValues(typeof(SlideCueKind)))
        {
            string standIn = slides.Sound(kind).standIn;
            bool placeholder = names.TryGetValue(standIn, out string name) && cueSounds.Contains($"case SoundCues.{name}:");
            Assert.IsTrue(Clip(standIn) || placeholder, $"{kind}: its stand-in '{standIn}' has a bank clip or a CueSounds placeholder, so it sounds today");
        }
        Assert.IsTrue(Clip(SoundCues.PaperSlide), "the shush is the bank's own clip");
        Assert.IsFalse(Clip(slides.seatCue) || Clip(slides.unlatchCue) || Clip(slides.stopCue),
                       "the sound list has not delivered the machines' own clips yet (when it does, retire this line: they play instead of the stand-ins)");
    }

    private static (string, string) Pair((string id, string standIn, float pitch) sound) => (sound.id, sound.standIn);

    [Test]
    public void TheSerializersDefault_IsTheApprovedSlides_AFreshCopyEachTime()
    {
        var made = new DeskSlides();
        DeskSlides approved = DeskSlides.Approved();
        CollectionAssert.AreEqual(approved.sliders.Select(s => s.id), made.sliders.Select(s => s.id));
        made.sliders[0].travel = 1f;
        Assert.AreNotEqual(1f, DeskSlides.Approved().sliders[0].travel, "no shared rows");
    }
}
