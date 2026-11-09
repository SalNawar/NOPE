using System;

/// <summary>
/// The city's ad screens under the leading culture (D61): which screens show the culture's emblem and colours, and how
/// one such screen is laid out. Pure rules in plate px (the scene's screen rects); the stage only draws them.
/// </summary>
public static class CityR17Culture
{
    /// <summary>The emblem's crop in the culture's departure-board plate art (canvas px): the plate's centre and the height of its inner field, which holds the emblem and none of the frame.</summary>
    public const double PlateCenterX = 1061, PlateCenterY = 59, PlateFieldH = 22;

    /// <summary>The share of the box's height the colour band takes at its foot, and the widest emblem shape (width over height).</summary>
    public const double BandShare = 0.10, MaxAspect = 2.4;

    /// <summary>True when the emblem of a screen whose painted ad fills <paramref name="box"/> is at least <paramref name="minW"/> by <paramref name="minH"/> (the rest keep their ads).</summary>
    public static bool Takes(CityR17Rect box, double minW, double minH)
    {
        Layout(box, out _, out CityR17Rect emblem);
        return emblem.W >= minW && emblem.H >= minH;
    }

    /// <summary>The band at the foot of <paramref name="box"/> (the painted ad's opaque box) and the emblem's box above it, centred, from square to 2.4 wide, filling the box above the band (plate px).</summary>
    public static void Layout(CityR17Rect box, out CityR17Rect band, out CityR17Rect emblem)
    {
        double bandH = Math.Round(box.H * BandShare);
        band = new CityR17Rect(box.X, box.Y + box.H - bandH, box.W, bandH);
        double aw = box.W, ah = box.H - bandH;
        double aspect = ah > 0 ? Math.Min(MaxAspect, Math.Max(1.0, aw / ah)) : 1.0;
        double w = Math.Max(0, Math.Min(aw, ah * aspect)), h = w / aspect;
        emblem = new CityR17Rect(box.X + (box.W - w) / 2, box.Y + (ah - h) / 2, w, h);
    }

    /// <summary>The crop of the plate art (canvas px) that fills <paramref name="box"/>: the plate's centre, the field's height, the width of the box's aspect.</summary>
    public static CityR17Rect Crop(CityR17Rect box)
    {
        double h = PlateFieldH;
        double w = box.H > 0 ? h * box.W / box.H : h;
        return new CityR17Rect(PlateCenterX - w / 2, PlateCenterY - h / 2, w, h);
    }
}
