using System;
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The D6 framing focus's target (Saleh, D6: "when they slide in, we subtly
/// zoom in a bit not too hard just a small focus to frame the shot";
/// DESK-HOMES P3): it starts after the slide plus a 150 ms dwell, lets go
/// when the cursor leaves, re-aims to the next machine without going out and
/// back in, starts at most once every 0.8 s, never shows while it is not
/// allowed, and keeps its narrowing and aim inside their budgets. Driven at
/// 60 fps with the real assist.
/// </summary>
public class SlideFocusTests
{
    private const float Frame = 1f / 60f;
    private const string Scanner = DeskHomeMap.Scanner, Chip = DeskHomeMap.ChipReader;

    private sealed class Rig
    {
        public readonly DeskSlides Slides = DeskSlides.Approved();
        public readonly ProximityAssist Assist;
        public readonly SlideFocus Focus;
        public readonly HashSet<string> Busy = new HashSet<string>();
        public readonly List<string> Targets = new List<string>();
        public bool Allowed = true;
        public int FrameNo;

        public Rig()
        {
            Assist = new ProximityAssist(Slides, DeskHomeMap.Approved());
            Focus = new SlideFocus(Slides);
        }

        public void Step(string near = null, bool on = false)
        {
            FrameNo++;
            var all = new List<SlideCandidate>();
            foreach (string id in new[] { Scanner, Chip })
                all.Add(id == near ? new SlideCandidate(id, on ? 0 : -1, false, on ? 0f : 40f, Busy.Contains(id)) : SlideCandidate.Away(id, Busy.Contains(id)));
            Assist.Step(FrameNo * Frame, all, false, false);
            Focus.Step(FrameNo * Frame, Assist, Allowed);
            Targets.Add(Focus.Target);
        }

        public void Hold(int frames, string near = null, bool on = false)
        {
            for (int i = 0; i < frames; i++)
                Step(near, on);
        }

        public int FirstFrame(string target) => Targets.IndexOf(target) + 1;
    }

    [Test]
    public void TheFocus_StartsAfterTheSlidePlusADwell_AndLetsGoWhenTheCursorLeaves()
    {
        var rig = new Rig();
        rig.Hold(40, Scanner);
        float expected = (rig.Slides.outSeconds + rig.Slides.focusDwellSeconds) / Frame;
        Assert.That(rig.FirstFrame(Scanner), Is.InRange((int)expected, (int)expected + 2), "250 ms of slide, then 150 ms of dwell");
        rig.Step();
        Assert.IsNull(rig.Focus.Target, "the cursor left: it lets go at once (CameraFeel eases it out)");
    }

    [Test]
    public void MovingToTheNextMachine_ReAimsIt_NeverOutAndBackIn()
    {
        var rig = new Rig();
        rig.Hold(40, Scanner);
        Assert.AreEqual(Scanner, rig.Focus.Target);
        int moved = rig.FrameNo;
        rig.Hold(50, Chip, true);
        for (int i = moved; i < rig.Targets.Count; i++)
            Assert.IsNotNull(rig.Targets[i], $"frame {i + 1}: the focus holds through the hand-off");
        Assert.AreEqual(Chip, rig.Focus.Target, "and re-aims once the chip reader has settled");
        int reaimed = rig.FirstFrame(Chip) - moved;
        Assert.Greater(reaimed * Frame, rig.Slides.outSeconds + rig.Slides.focusDwellSeconds, "after its own slide and dwell");
    }

    [Test]
    public void AFocus_StartsAtMostOnceEvery800ms()
    {
        var rig = new Rig();
        rig.Hold(30, Scanner);
        int first = rig.FirstFrame(Scanner);
        rig.Step();
        Assert.IsNull(rig.Focus.Target);
        rig.Hold(5, Scanner);
        int again = first;
        for (int i = 0; i < 60; i++)
        {
            rig.Step(Scanner);
            if (rig.Focus.Target != null)
            {
                again = rig.FrameNo;
                break;
            }
        }
        Assert.GreaterOrEqual((again - first) * Frame, rig.Slides.focusEverySeconds - 1e-4f, "BUG-17: no pumping");
    }

    [Test]
    public void NotAllowed_NoFocus_AndABusyMachineAtHomeLetsItGo()
    {
        var rig = new Rig { Allowed = false };
        rig.Hold(40, Scanner);
        Assert.IsNull(rig.Focus.Target, "held, tilting, out of the reading view or still: no focus");
        rig.Allowed = true;
        rig.Hold(5, Scanner);
        Assert.IsNull(rig.Focus.Target, "allowed again: the dwell starts over");
        rig.Hold(10, Scanner);
        Assert.AreEqual(Scanner, rig.Focus.Target);

        rig.Busy.Add(Chip);
        rig.Hold(3, Chip, true);
        Assert.IsNull(rig.Focus.Target, "the cursor on a busy machine at home: nothing comes, the focus lets go");
    }

    [Test]
    public void TheBudgets_TheNarrowingInsideTheCap_TheAimAtMost3PercentOfTheFrame()
    {
        DeskSlides slides = DeskSlides.Approved();
        Assert.AreEqual(1.5f, SlideFocus.Narrowing(50f, 0f, slides.focusShare, slides.focusCap), 1e-5f, "3 % of the 50 degree reading view");
        Assert.AreEqual(1.5f, SlideFocus.Narrowing(50f, 2.5f, slides.focusShare, slides.focusCap), 1e-5f, "on top of the 2.5 degree push-in: 8 % in all, 46 degrees");
        Assert.AreEqual(0.5f, SlideFocus.Narrowing(50f, 3.5f, slides.focusShare, slides.focusCap), 1e-5f, "a bigger push-in leaves less: the cap holds");
        Assert.AreEqual(0f, SlideFocus.Narrowing(50f, 5f, slides.focusShare, slides.focusCap), 1e-5f);
        (float x, float y) far = SlideFocus.Aim(0.9f, 0.2f, slides.focusAim);
        Assert.AreEqual(slides.focusAim, Math.Sqrt(far.x * far.x + far.y * far.y), 1e-5f, "at most 3 % of the frame");
        Assert.Greater(far.x, 0f);
        Assert.Less(far.y, 0f, "toward the machine");
        (float x, float y) close = SlideFocus.Aim(0.51f, 0.49f, slides.focusAim);
        Assert.AreEqual(0.01f, close.x, 1e-5f, "a machine near the centre: straight to it");
        Assert.AreEqual(-0.01f, close.y, 1e-5f);
        (float pan, float tilt) = SlideFocus.Turn((0.03f, 0.03f), 50f, 16f / 9f);
        Assert.Greater(pan, 0f, "pans right toward it");
        Assert.Less(tilt, 0f, "tilts up toward it (the recomposer's tilt is positive downward)");
        Assert.Less(Math.Abs(pan), 3f);
        DeskSlides bad = DeskSlides.Approved();
        bad.focusShare = 0.2f;
        StringAssert.Contains("The framing focus narrows 0.2", string.Join("\n", bad.Problems(DeskHomeMap.Approved(), DeskHomeMap.SlideReach)));
    }
}
