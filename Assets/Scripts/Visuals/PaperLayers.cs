using System;
using System.Collections.Generic;

/// <summary>
/// The one stacking rule of the desk's papers (Saleh's playtest 2026-10-07:
/// "documents overlap": a lower paper's photo drew over the paper on top).
/// Every part of a paper lies a fixed height over its sheet, in metres, never
/// scaled with the paper's size (DeskDocument keeps its sheet's depth scale
/// at 1): the seal and the face's patches, the fills, the hover and pick
/// quads, the lines, the photo (its frame, and the portrait an inset over
/// it), the texts, and on top the dater's ink (pressed onto the paper, it
/// lies over everything printed on it). All of them stay under
/// PartsDepth, and two stacked papers lie StackStep apart, more than that:
/// so the paper on top hides every part of the papers under it (the camera's
/// depth test), and raising a paper raises all of its parts with it.
/// </summary>
public static class PaperLayers
{
    /// <summary>Each part's height over its sheet (metres).</summary>
    public const float Seal = 0.0001f, Fill = 0.0002f, Highlight = 0.0003f, Line = 0.0004f, Photo = 0.0005f, Text = 0.0007f, Ink = 0.0008f;

    /// <summary>The portrait's height over its photo frame (the builder's Photo child of the PhotoSlot).</summary>
    public const float PhotoInset = 0.0001f;

    /// <summary>The highest any part of a paper lies over its sheet (metres): every layer above is under it.</summary>
    public const float PartsDepth = 0.001f;

    /// <summary>The least room between two stacked papers over the parts' depth (metres): the depth buffer tells them apart at the reading view's distance.</summary>
    public const float StackGap = 0.0004f;

    /// <summary>The height between two stacked papers: <paramref name="configured"/> (DeskConfigSO.paperStackStep), never less than the parts' depth and the gap.</summary>
    public static float StackStep(float configured) => Math.Max(configured, PartsDepth + StackGap);

    /// <summary>The least room between the desk top and the bottom of whatever lies lowest on it (metres): no paper meets the desk's surface for the depth buffer (Saleh's 1007d playtest: "sometimes they clip or render with the table").</summary>
    public const float DeskClearance = 0.0005f;

    /// <summary>
    /// Each stacked thing's face height over the desk, bottom first, from its
    /// <paramref name="thickness"/> (PaperEdge: a sheet, a booklet, a card, the
    /// folder): the lowest at least <see cref="DeskClearance"/> over the desk with
    /// its whole thickness under its face, and each next one's bottom at least
    /// <see cref="StackGap"/> over every part of the one below (its face plus
    /// <see cref="PartsDepth"/>); never closer than <see cref="StackStep"/>
    /// of <paramref name="configured"/> apart. So no paper meets the desk or
    /// the paper under it, at any thickness.
    /// </summary>
    public static float[] Lifts(IReadOnlyList<float> thickness, float configured)
    {
        var lifts = new float[thickness != null ? thickness.Count : 0];
        float step = StackStep(configured);
        for (int i = 0; i < lifts.Length; i++)
        {
            float t = Math.Max(0f, thickness[i]);
            lifts[i] = i == 0 ? Math.Max(step, DeskClearance + t) : lifts[i - 1] + Math.Max(step, PartsDepth + StackGap + t);
        }
        return lifts;
    }

    /// <summary>How far a paper tilted <paramref name="tiltXDegrees"/> about its width and <paramref name="tiltZDegrees"/> about its length dips at its lowest corner (metres; <paramref name="halfWidth"/> and <paramref name="halfDepth"/> its half sizes on the desk): a drag's lean or a drop's flutter lifts it by that much, so its edge never sinks into the desk or the paper under it.</summary>
    public static float TiltDrop(float halfWidth, float halfDepth, float tiltXDegrees, float tiltZDegrees) =>
        (float)(Math.Abs(halfDepth * Math.Sin(tiltXDegrees * Math.PI / 180.0)) + Math.Abs(halfWidth * Math.Sin(tiltZDegrees * Math.PI / 180.0)));

    /// <summary>The parts' heights in stacking order, bottom first (the tests check each is under PartsDepth and in order).</summary>
    public static readonly float[] Order = { Seal, Fill, Highlight, Line, Photo, Photo + PhotoInset, Text, Ink };
}
