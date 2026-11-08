using System;

/// <summary>
/// The city's ad screens under the leading culture (D61): which screens show the culture's emblem and colours, and how
/// one such screen is laid out. Pure rules in plate px (the scene's screen rects); the stage only draws them.
/// </summary>
public static class CityR17Culture
{
    /// <summary>The emblem's crop in the culture's departure-board plate art (canvas px): the plate's centre and the height of its inner field, which holds the emblem and none of the frame.</summary>
    public const double PlateCenterX = 1061, PlateCenterY = 59, PlateFieldH = 22;

    /// <summary>The share of a screen's height the colour band takes at its foot, and the share of its width the emblem is inset on each side.</summary>
    public const double BandShare = 0.14, InsetShare = 0.08;

    /// <summary>True when a screen of <paramref name="r"/> is big enough to carry an emblem (the rest keep their ads).</summary>
    public static bool Takes(CityR17Rect r, double minW, double minH) => r.W >= minW && r.H >= minH;

    /// <summary>The part of the screen the base colour covers (the whole rect), the band at its foot and the emblem's box above the band (plate px).</summary>
    public static void Layout(CityR17Rect screen, out CityR17Rect band, out CityR17Rect emblem)
    {
        double bandH = Math.Round(screen.H * BandShare);
        band = new CityR17Rect(screen.X, screen.Y + screen.H - bandH, screen.W, bandH);
        double inset = Math.Round(screen.W * InsetShare);
        emblem = new CityR17Rect(screen.X + inset, screen.Y + inset, screen.W - 2 * inset, screen.H - bandH - 2 * inset);
    }

    /// <summary>The crop of the plate art (canvas px) that fills <paramref name="box"/>: the plate's centre, the field's height, the width of the box's aspect.</summary>
    public static CityR17Rect Crop(CityR17Rect box)
    {
        double h = PlateFieldH;
        double w = box.H > 0 ? h * box.W / box.H : h;
        return new CityR17Rect(PlateCenterX - w / 2, PlateCenterY - h / 2, w, h);
    }
}
