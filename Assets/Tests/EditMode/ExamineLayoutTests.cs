using System;
using NUnit.Framework;

/// <summary>
/// Where papers held in the hand sit (piece 10 X1, X2, X9, X10), in
/// screen-height units (x from the screen's centre, y from its bottom): the
/// two office slots, the dip under the open wheel, the region beside the open
/// PC frame, the pose in camera space, the safe distance (never inside the
/// near clip, never into the desk) and the easing. The paper is the default
/// 0.26 x 0.34 m (aspect 0.7647).
/// </summary>
public class ExamineLayoutTests
{
    private const float PaperAspect = 0.26f / 0.34f;
    private const float Eps = 1e-4f;
    private static readonly ExamineTuning T = new ExamineTuning();

    private static float Left(ScreenBox b) => b.CentreX - b.Height * PaperAspect / 2f;
    private static float Right(ScreenBox b) => b.CentreX + b.Height * PaperAspect / 2f;
    private static float Bottom(ScreenBox b) => b.CentreY - b.Height / 2f;
    private static float Top(ScreenBox b) => b.CentreY + b.Height / 2f;

    [Test]
    public void Pose_WorkedExample()
    {
        var box = new ScreenBox(-0.2f, 0.24f, 0.44f);
        (float x, float y, float z, float height) = ExamineLayout.Pose(box, 55f, 0.6f);
        Assert.AreEqual(-0.12494f, x, Eps);
        Assert.AreEqual(-0.16242f, y, Eps);
        Assert.AreEqual(0.6f, z, Eps);
        Assert.AreEqual(0.27486f, height, Eps);
    }

    [Test]
    public void OfficeSlots_Symmetric_BottomAtKnob()
    {
        ScreenBox left = ExamineLayout.OfficeSlot(false, PaperAspect, 16f / 9f, false, T);
        ScreenBox right = ExamineLayout.OfficeSlot(true, PaperAspect, 16f / 9f, false, T);
        Assert.AreEqual(-T.officeCentreX, left.CentreX, Eps);
        Assert.AreEqual(T.officeCentreX, right.CentreX, Eps);
        Assert.AreEqual(left.CentreY, right.CentreY, Eps);
        Assert.AreEqual(T.officeHeight, left.Height, Eps);
        Assert.AreEqual(T.officeBottom, Bottom(left), Eps);
        // X1 at 1080p: left 562-925 px, 22-497 px above the bottom.
        Assert.AreEqual(562f, 960f + Left(left) * 1080f, 1f);
        Assert.AreEqual(925f, 960f + Right(left) * 1080f, 1f);
        Assert.AreEqual(497f, Top(left) * 1080f, 1f);
    }

    [Test]
    public void OfficeSlots_NoOverlap_16x9_21x9()
    {
        foreach (float aspect in new[] { 16f / 9f, 2560f / 1080f })
        {
            ScreenBox left = ExamineLayout.OfficeSlot(false, PaperAspect, aspect, false, T);
            ScreenBox right = ExamineLayout.OfficeSlot(true, PaperAspect, aspect, false, T);
            Assert.Less(Right(left), Left(right), $"aspect {aspect}");
            Assert.GreaterOrEqual(Left(left), -aspect / 2f + T.margin - Eps);
            Assert.LessOrEqual(Right(right), aspect / 2f - T.margin + Eps);
        }
    }

    [Test]
    public void OfficeSlot_DipLowersByWheelDip()
    {
        ScreenBox up = ExamineLayout.OfficeSlot(true, PaperAspect, 16f / 9f, false, T);
        ScreenBox dipped = ExamineLayout.OfficeSlot(true, PaperAspect, 16f / 9f, true, T);
        Assert.AreEqual(up.CentreY - T.wheelDip, dipped.CentreY, Eps);
        Assert.AreEqual(up.CentreX, dipped.CentreX, Eps);
        Assert.AreEqual(up.Height, dipped.Height, Eps);
        Assert.AreEqual(281f, Top(dipped) * 1080f, 1f, "X9: the tops at about 281 px at 1080p");
    }

    [Test]
    public void OfficeSlot_ClampedAtNarrowAspect_5x4()
    {
        var wide = new ExamineTuning { officeCentreX = 0.5f };
        const float aspect = 5f / 4f;
        ScreenBox left = ExamineLayout.OfficeSlot(false, PaperAspect, aspect, false, wide);
        ScreenBox right = ExamineLayout.OfficeSlot(true, PaperAspect, aspect, false, wide);
        Assert.AreEqual(-aspect / 2f + wide.margin, Left(left), Eps, "pulled in to the margin");
        Assert.AreEqual(aspect / 2f - wide.margin, Right(right), Eps);
        ScreenBox plain = ExamineLayout.OfficeSlot(true, PaperAspect, aspect, false, T);
        Assert.AreEqual(T.officeCentreX, plain.CentreX, Eps, "the default slots fit at 5:4 unchanged");
    }

    [Test]
    public void InRegion_OneCentred()
    {
        ScreenBox b = ExamineLayout.InRegion(0, 1, 0.2778f, 0.8889f, PaperAspect, T, out bool fits);
        Assert.IsTrue(fits);
        Assert.AreEqual(T.frameHeight, b.Height, Eps, "capped at frameHeight");
        Assert.AreEqual((0.2778f + 0.8889f) / 2f, b.CentreX, Eps);
        Assert.AreEqual(0.5f, b.CentreY, Eps);
    }

    [Test]
    public void InRegion_TwoSideBySide_WideRegion()
    {
        const float left = 0.2778f, right = left + 1.2f;
        ScreenBox a = ExamineLayout.InRegion(0, 2, left, right, PaperAspect, T, out bool fitsA);
        ScreenBox b = ExamineLayout.InRegion(1, 2, left, right, PaperAspect, T, out bool fitsB);
        Assert.IsTrue(fitsA && fitsB);
        Assert.AreEqual(a.CentreY, b.CentreY, Eps, "side by side");
        Assert.Less(Right(a), Left(b));
        Assert.AreEqual(T.gap, Left(b) - Right(a), Eps);
        Assert.AreEqual(T.frameHeight, a.Height, Eps);
    }

    [Test]
    public void InRegion_TwoStacked_NarrowRegion()
    {
        const float left = 0.2778f, right = left + 0.611f;
        ScreenBox a = ExamineLayout.InRegion(0, 2, left, right, PaperAspect, T, out bool fits);
        ScreenBox b = ExamineLayout.InRegion(1, 2, left, right, PaperAspect, T, out _);
        Assert.IsTrue(fits);
        Assert.AreEqual(a.CentreX, b.CentreX, Eps, "stacked");
        Assert.Greater(a.CentreY, b.CentreY, "the first on top");
        Assert.AreEqual(T.gap, Bottom(a) - Top(b), Eps);
        Assert.AreEqual((1f - 2f * T.margin - T.gap) / 2f, a.Height, Eps);
    }

    [Test]
    public void InRegion_TooSmall_DoesNotFit()
    {
        ExamineLayout.InRegion(0, 1, 0.5f, 0.7f, PaperAspect, T, out bool fits);
        Assert.IsFalse(fits, "0.2 heights hold a paper 0.21 tall, under frameMinHeight");
        ExamineLayout.InRegion(0, 1, 0.5f, 0.4f, PaperAspect, T, out bool inverted);
        Assert.IsFalse(inverted, "a region of no width");
    }

    [Test]
    public void InRegion_BoxesStayInsideRegion()
    {
        foreach (float width in new[] { 0.3f, 0.45f, 0.611f, 0.9f, 1.2f, 2f })
            foreach (int count in new[] { 1, 2 })
                for (int i = 0; i < count; i++)
                {
                    const float left = 0.28f;
                    ScreenBox b = ExamineLayout.InRegion(i, count, left, left + width, PaperAspect, T, out _);
                    Assert.GreaterOrEqual(Left(b), left + T.margin - Eps, $"width {width} count {count} #{i}");
                    Assert.LessOrEqual(Right(b), left + width - T.margin + Eps, $"width {width} count {count} #{i}");
                    Assert.GreaterOrEqual(Bottom(b), T.margin - Eps);
                    Assert.LessOrEqual(Top(b), 1f - T.margin + Eps);
                    Assert.LessOrEqual(b.Height, T.frameHeight + Eps);
                }
    }

    [Test]
    public void SafeDistance_WorkedExample_NoCap()
    {
        ScreenBox left = ExamineLayout.OfficeSlot(false, PaperAspect, 16f / 9f, false, T);
        float d = ExamineLayout.SafeDistance(left, PaperAspect, 55f, 0.03f, 1.09f, 0f, 0.98481f, -0.17365f, T);
        Assert.AreEqual(0.6f, d, Eps, "the lower corners meet the desk at 1.637 m, so the cap is 1.31 m");
        Assert.AreEqual(1.637f, ExamineLayout.DeskDepth(left, PaperAspect, 55f, 1.09f, 0f, 0.98481f, -0.17365f), 1e-3f);
    }

    [Test]
    public void SafeDistance_CloseDesk_Caps()
    {
        ScreenBox left = ExamineLayout.OfficeSlot(false, PaperAspect, 16f / 9f, false, T);
        float d = ExamineLayout.SafeDistance(left, PaperAspect, 55f, 0.03f, 0.3f, 0f, 0.98481f, -0.17365f, T);
        float meet = 0.3f / 0.66581f;
        Assert.AreEqual(T.deskMargin * meet, d, 1e-3f, "a desk 0.3 m below the camera pulls the paper in");
        Assert.Less(d, T.distance);
    }

    [Test]
    public void SafeDistance_NearClipFloor()
    {
        ScreenBox left = ExamineLayout.OfficeSlot(false, PaperAspect, 16f / 9f, false, T);
        float d = ExamineLayout.SafeDistance(left, PaperAspect, 55f, 0.5f, 1.09f, 0f, 0.98481f, -0.17365f, T);
        Assert.AreEqual(0.75f, d, Eps, "raised to 1.5 x a 0.5 m near clip");
        float low = ExamineLayout.SafeDistance(left, PaperAspect, 55f, 0.5f, 0.1f, 0f, 0.98481f, -0.17365f, T);
        Assert.AreEqual(0.75f, low, Eps, "the near clip wins over the desk (a clipped paper shows nothing)");
    }

    [Test]
    public void SafeDistance_RaysUp_NoCap()
    {
        ScreenBox left = ExamineLayout.OfficeSlot(false, PaperAspect, 16f / 9f, false, T);
        var far = new ExamineTuning { distance = 50f };
        Assert.AreEqual(50f, ExamineLayout.SafeDistance(left, PaperAspect, 55f, 0.03f, 1.09f, 0f, -0.98481f, 0.17365f, far), Eps,
            "a desk plane above the camera (its normal pointing away) is never met by the rays");
        Assert.IsTrue(float.IsPositiveInfinity(ExamineLayout.DeskDepth(left, PaperAspect, 55f, 1.09f, 0f, 0f, 1f)),
            "a camera looking straight up never meets the desk");
    }

    [Test]
    public void Ease_EndpointsAndMonotonic()
    {
        Assert.AreEqual(0f, ExamineLayout.Ease(0f), Eps);
        Assert.AreEqual(1f, ExamineLayout.Ease(1f), Eps);
        Assert.AreEqual(0.5f, ExamineLayout.Ease(0.5f), Eps);
        Assert.AreEqual(0f, ExamineLayout.Ease(-3f), Eps);
        Assert.AreEqual(1f, ExamineLayout.Ease(7f), Eps);
        float last = -1f;
        for (int i = 0; i <= 100; i++)
        {
            float e = ExamineLayout.Ease(i / 100f);
            Assert.GreaterOrEqual(e, last);
            last = e;
        }
    }
}
