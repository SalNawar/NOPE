using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The anime hall's swappable slots (the hall slots spec,
/// docs/superpowers/specs/2026-10-07-hall-slots-design.md; the art request
/// ArtDeliverables/TimeDesk/HallSlots/HALL_SLOTS_ART_REQUEST.md): each slot is
/// a region of the hall painting's source canvas whose art follows the hall's
/// variables (HallState: the leading culture, the Helix River's tier, the
/// phase, today's special, the famous travellers let through), drawn over the
/// painting by HallSlotsLink with the painting's own material, so the hall's
/// time of day lights it. One asset (Assets/Data/Config/HallSlots_Default.asset,
/// referenced by DeskConfigSO.hallSlots) that the art side edits in the
/// Inspector; Generate World never writes it.
/// </summary>
[CreateAssetMenu(menuName = "TimeDesk/Hall Slots", fileName = "HallSlots_Default")]
public sealed class HallSlotsSO : ScriptableObject
{
    /// <summary>The hall painting the slots are drawn over: its AnimeHallPresentation layer id (the live warm-stone composite).</summary>
    public string paintingLayer = "62 Approved deeper architecture";

    /// <summary>The painting's source file (the warm-stone composite, HallWarmStone.png): the art tools crop the slots' templates from it (HallSlotsTools).</summary>
    public Texture2D painting;

    /// <summary>The painting material's window masks (DeepRoomMasks.png: red and green mark the window glass it cuts out, so nothing drawn there shows): the templates hatch it.</summary>
    public Texture2D windowMasks;

    /// <summary>The painting's source canvas in pixels (the registered layers' and the composite's: 2172 x 724); slot regions are in it.</summary>
    public int canvasWidth = 2172, canvasHeight = 724;

    /// <summary>The first day of the extended hours (HallPhase.Extended) and of the night shifts (HallPhase.Nights).</summary>
    public int extendedFromDay = 8, nightsFromDay = 12;

    /// <summary>How long a slot takes to fade from one variant to the next (seconds; Reduced Motion cuts).</summary>
    [Min(0f)] public float crossfadeSeconds = 0.6f;

    /// <summary>The Helix River's knobs, whose thresholds give the tier (HelixRiver.Tier).</summary>
    public HelixRiverSO river;

    /// <summary>A stand-in's two cel tones and its ink (the hall palette: bone, lavender shade, ink), drawn for a picked variant whose art is missing (editor and development builds only).</summary>
    public Color standInLight = new Color(0.933f, 0.898f, 0.816f, 0.85f), standInShade = new Color(0.596f, 0.525f, 0.659f, 0.85f), standInInk = new Color(0.169f, 0.110f, 0.141f, 1f);

    /// <summary>The slots, in no particular order (each draws at its own sorting order).</summary>
    public List<HallSlotDef> slots = new List<HallSlotDef>();
}
