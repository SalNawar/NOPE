using System;

/// <summary>How the taskbar's window buttons fit their strip (TaskbarStrip.Fit). Not serialized.</summary>
public readonly struct TaskbarFit
{
    /// <summary>How many buttons show in the strip, in open order from the first.</summary>
    public readonly int Shown;

    /// <summary>Each shown button's width (desktop units).</summary>
    public readonly float Width;

    /// <summary>True when some windows do not fit: the chevron shows after the shown buttons and lists the rest.</summary>
    public readonly bool Chevron;

    /// <summary>How many windows sit behind the chevron.</summary>
    public int Hidden(int count) => Math.Max(0, count - Shown);

    /// <summary>A fit.</summary>
    public TaskbarFit(int shown, float width, bool chevron)
    {
        Shown = shown;
        Width = width;
        Chevron = chevron;
    }
}

/// <summary>
/// The taskbar's window buttons (BUG-21a, UI-PC; UI-16 and UI-30): every
/// button carries its glyph and its title, so each is at least wide enough
/// for a few letters; they share the strip at their preferred width, shrink
/// together down to their least when more windows open, and past that the
/// windows that do not fit go behind a chevron at the strip's end (Windows'
/// overflow), so no button is ever cut or drawn over the tray. Pure;
/// DesktopWindowManager lays the strip out with it.
/// </summary>
public static class TaskbarStrip
{
    /// <summary>
    /// Fits <paramref name="count"/> buttons in a strip <paramref name="room"/>
    /// wide, <paramref name="spacing"/> apart, each <paramref name="preferred"/>
    /// wide when there is room, never under <paramref name="least"/>; the
    /// chevron (<paramref name="chevron"/> wide, after a gap) takes its room
    /// only when some do not fit.
    /// </summary>
    public static TaskbarFit Fit(int count, float room, float preferred, float least, float spacing, float chevron)
    {
        if (count <= 0 || room <= 0f)
            return new TaskbarFit(0, 0f, count > 0);
        least = Math.Max(1f, Math.Min(least, preferred));
        spacing = Math.Max(0f, spacing);

        float all = WidthFor(count, room, spacing);
        if (all >= least)
            return new TaskbarFit(count, Math.Min(preferred, all), false);

        float left = room - chevron - spacing;
        int shown = left < least ? 0 : (int)Math.Floor((left + spacing) / (least + spacing));
        shown = Math.Max(0, Math.Min(shown, count - 1));
        float width = shown > 0 ? Math.Min(preferred, WidthFor(shown, left, spacing)) : 0f;
        return new TaskbarFit(shown, width, true);
    }

    /// <summary>The chevron's label: the overflow glyph and how many windows wait behind it ("» 3"; empty for none). A glyph and a number, so it has no words to translate and no string key a theme refresh would write over it.</summary>
    public static string ChevronLabel(int hidden) => hidden > 0 ? "» " + hidden : string.Empty;

    /// <summary>The width each of <paramref name="n"/> buttons gets sharing <paramref name="room"/>.</summary>
    private static float WidthFor(int n, float room, float spacing) => (room - (n - 1) * spacing) / n;
}
