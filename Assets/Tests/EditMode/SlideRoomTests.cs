using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// A track slider of the desk's slide-in stops short of a thing resting in
/// its strip (FOLD-01, Saleh's playtest of 2026-10-28, point 4; Papers,
/// Please: a thing let go lies where it is let go): SlideRoom gives the room
/// it has toward the mat, the share of its slide it makes and the share's
/// ease. The numbers are the painted desk's chip reader (fold_rest, day 12).
/// </summary>
public class SlideRoomTests
{
    private const float Eps = 1e-5f;

    /// <summary>The chip reader's footprint at rest in the desk's frame, sliding left (toward the mat) at most 4.3 cm.</summary>
    private static readonly DeskRect Reader = new DeskRect(0.97f, -0.40f, 0.26f, 0.26f);

    private const float Reach = 0.0432f;

    [Test]
    public void WithNothingInItsStrip_ItHasItsWholeReach()
    {
        Assert.AreEqual(Reach, SlideRoom.Room(Reader, -1f, Reach, new List<DeskRect>(), SlideRoom.Gap), Eps);
        Assert.AreEqual(Reach, SlideRoom.Room(Reader, -1f, Reach, null, SlideRoom.Gap), Eps);
        var farAway = new DeskRect(0.2f, -0.40f, 0.2f, 0.2f); // its right edge 0.3: 0.54 m from the reader
        Assert.AreEqual(Reach, SlideRoom.Room(Reader, -1f, Reach, new List<DeskRect> { farAway }, SlideRoom.Gap), Eps);
    }

    [Test]
    public void AThingRestingInItsStrip_LeavesItTheRoomUpToIt_LessTheGap()
    {
        // fold_rest's case i: the folder's right edge at 0.814, the reader's left edge at 0.84.
        var folder = new DeskRect(0.814f - 0.1125f, -0.39f, 0.225f, 0.259f);
        float room = SlideRoom.Room(Reader, -1f, Reach, new List<DeskRect> { folder }, SlideRoom.Gap);
        Assert.AreEqual(0.84f - 0.814f - SlideRoom.Gap, room, 1e-4f, "it stops a gap short of the folder");
        Assert.Less(room, Reach);
        var touching = new DeskRect(0.84f - 0.1f, -0.40f, 0.2f, 0.2f); // its right edge on the reader's left edge
        Assert.AreEqual(0f, SlideRoom.Room(Reader, -1f, Reach, new List<DeskRect> { touching }, SlideRoom.Gap), Eps, "no room: it does not slide");
    }

    [Test]
    public void OnlyThingsAcrossItsDepth_AndAheadOfIt_Count()
    {
        var above = new DeskRect(0.75f, -0.10f, 0.2f, 0.2f); // y -0.2..0: past the reader's far edge (-0.27)
        var behind = new DeskRect(1.25f, -0.40f, 0.2f, 0.2f); // on its far side, away from the mat
        var docked = new DeskRect(0.97f, -0.40f, 0.1f, 0.1f); // over its home: in it (a paper docked on its pad), not in its way
        Assert.AreEqual(Reach, SlideRoom.Room(Reader, -1f, Reach, new List<DeskRect> { above, behind, docked }, SlideRoom.Gap), Eps);
    }

    [Test]
    public void ATrackSlidingRight_MeasuresItsRoomToItsRight()
    {
        // The intercom and the BABEL tuner, left of the mat, slide right.
        var intercom = new DeskRect(-0.945f, 0.35f, 0.29f, 0.22f); // its right edge at -0.80
        var folder = new DeskRect(-0.79f + 0.1125f, 0.30f, 0.225f, 0.259f); // its left edge 1 cm right of it
        Assert.AreEqual(0.01f - SlideRoom.Gap, SlideRoom.Room(intercom, 1f, 0.030f, new List<DeskRect> { folder }, SlideRoom.Gap), 1e-4f);
        Assert.AreEqual(0.030f, SlideRoom.Room(intercom, 0f, 0.030f, new List<DeskRect> { folder }, SlideRoom.Gap), Eps, "no way: no limit");
    }

    [Test]
    public void TheShare_ShrinksTheWholeSlideToTheRoom()
    {
        Assert.AreEqual(1f, SlideRoom.Share(float.PositiveInfinity, Reach), Eps);
        Assert.AreEqual(1f, SlideRoom.Share(0.1f, Reach), Eps, "never more than all of it");
        Assert.AreEqual(0.5f, SlideRoom.Share(Reach / 2f, Reach), Eps);
        Assert.AreEqual(0f, SlideRoom.Share(0f, Reach), Eps);
        Assert.AreEqual(1f, SlideRoom.Share(0f, 0f), Eps, "a slider with no travel: all of nothing");
    }

    [Test]
    public void TheShare_EasesToANewRoom_NeverJumps_ExceptUnderReducedMotion()
    {
        // A paper let go in the strip while the slider is out: it backs off over a few frames, never in one.
        float share = 1f;
        float first = SlideRoom.EaseShare(share, 0.4f, 1f / 60f, false);
        Assert.Less(first, 1f);
        Assert.Greater(first, 0.4f + 0.3f, "one frame takes only part of the way");
        for (int i = 0; i < 60; i++)
            share = SlideRoom.EaseShare(share, 0.4f, 1f / 60f, false);
        Assert.AreEqual(0.4f, share, 1e-4f, "within a second it is there");
        Assert.AreEqual(0.4f, SlideRoom.EaseShare(1f, 0.4f, 1f / 60f, true), Eps, "Reduced Motion: at once (its slide is a cut too)");
        Assert.AreEqual(1f, SlideRoom.EaseShare(1f, 0.4f, 0f, false), Eps, "no time: no move");
    }
}
