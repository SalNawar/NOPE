using NUnit.Framework;

/// <summary>
/// FOLD-01 round 2 (the verifier's F2; Saleh's playtest of 2026-10-28, point
/// 4): the folder's drag lift comes off at the drop on DeskLiftEase, so the
/// frame of the release moves its picture less than a pixel (DOC-HANDLE: "a
/// frame diff at the press and release frames ≤1 px"). Set at once, it moved
/// the picture 8 to 11 px in that frame.
/// </summary>
public class DeskLiftEaseTests
{
    private const float Frame = 1f / 30f;

    [Test]
    public void TheLift_StartsAndEndsAtNoSpeed_AndLandsExactly()
    {
        Assert.AreEqual(0f, DeskLiftEase.Share(0f), 1e-6f);
        Assert.AreEqual(1f, DeskLiftEase.Share(DeskLiftEase.Seconds), 1e-6f);
        Assert.AreEqual(1f, DeskLiftEase.Share(DeskLiftEase.Seconds * 3f), 1e-6f, "never past its place");
        Assert.AreEqual(0.5f, DeskLiftEase.Share(DeskLiftEase.Seconds / 2f), 1e-6f, "half way at half time");
        Assert.AreEqual(1f, DeskLiftEase.Share(0f, 0f), "no time: at once (Reduced Motion)");
        float last = 0f;
        for (float t = Frame; t <= DeskLiftEase.Seconds + 1e-6f; t += Frame)
        {
            float share = DeskLiftEase.Share(t);
            Assert.GreaterOrEqual(share, last, "it only goes one way");
            last = share;
        }
    }

    [Test]
    public void TheReleaseFrame_MovesTheFoldersPicture_UnderAPixel()
    {
        // The verifier's largest one-frame jump: 10.8 px at 1080p (the office view), the whole drop of the drag's lift.
        const float wholeDrop = 10.8f;
        float first = DeskLiftEase.Share(Frame) * wholeDrop;
        Assert.Less(first, 1f, "the first frame after the release moves the picture under DOC-HANDLE's 1 px");
        float last = (1f - DeskLiftEase.Share(DeskLiftEase.Seconds - Frame)) * wholeDrop;
        Assert.Less(last, 1f, "and it lands with no bump");
        Assert.AreEqual(0.02f + 0.5f * (0.01f - 0.02f), DeskLiftEase.At(0.02f, 0.01f, DeskLiftEase.Seconds / 2f), 1e-6f, "At goes from the drag's lift to the stack's");
    }
}
