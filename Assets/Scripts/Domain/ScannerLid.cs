using System;

/// <summary>
/// The cream scanner's lid (Track BR, Saleh 2026-10-08: "the cream: it auto
/// opens when you drag a document near and makes a xerox sound as it quickly
/// scans, then opens again"): shut while a scan runs (the xerox sweep shows
/// through its smoked glass); open while a dragged paper's pointer is near
/// the scanner (its drop area grown by MotionKnobs.scannerLidNear on every
/// side), so it shuts again if the paper leaves without dropping; open again
/// for MotionKnobs.scannerLidIdleSeconds after a scan ends, then it shuts by
/// itself. Pure: DeskScanner swings the lid on a spring toward it.
/// </summary>
public static class ScannerLid
{
    /// <summary>True when the lid should stand open: never while <paramref name="scanning"/>; else while a dragged paper is near (<paramref name="dragNear"/>) or for <paramref name="idleSeconds"/> after the last scan ended (<paramref name="sinceScan"/> seconds ago; infinity: none yet).</summary>
    public static bool Open(bool scanning, bool dragNear, float sinceScan, float idleSeconds) =>
        !scanning && (dragNear || sinceScan < idleSeconds);

    /// <summary>True when a point at (<paramref name="x"/>, <paramref name="z"/>) in the scanner's horizontal plane (its centre at 0, 0) lies within its drop area (<paramref name="width"/> by <paramref name="depth"/>) grown by <paramref name="margin"/> on every side.</summary>
    public static bool Near(float x, float z, float width, float depth, float margin) =>
        MathF.Abs(x) <= width / 2f + margin && MathF.Abs(z) <= depth / 2f + margin;

    /// <summary>The green readout's count for a scan at <paramref name="progress"/> (0 to 1; below 0: no scan, the last count stays): 000 to 100, never past it.</summary>
    public static int Count(float progress) => progress <= 0f ? 0 : progress >= 1f ? 100 : (int)MathF.Floor(progress * 100f);
}
