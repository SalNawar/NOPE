using System;

/// <summary>The PC's look (PcThemeSO.look; D2, Saleh 2026-11-07). Serialized: append only.</summary>
public enum PcLook
{
    /// <summary>The cel kit as built (the look before the night of 2026-11-07).</summary>
    Cel,

    /// <summary>The 80s retro sci-fi terminal: dark glass, phosphor cyan, magenta accent, amber for money and warnings.</summary>
    Cyberpunk
}

/// <summary>Which drawn piece of the cyberpunk PC takes the place of a cel kit sprite (PcSkinRules.Of).</summary>
public enum PcPiece
{
    /// <summary>The cel sprite stays (art the theme does not redraw: the readout glass, the logo, the slot machine, ribbons, the desk's own pieces).</summary>
    Keep,

    /// <summary>A window body, a sheet, a menu, a card, a tooltip: dark glass with a thin edge and a lit top edge.</summary>
    Panel,

    /// <summary>A window's title bar: raised glass, a lit top edge, a rule under it.</summary>
    TitleBar,

    /// <summary>The taskbar: the bottom deck, the darkest glass with a lit top edge.</summary>
    Deck,

    /// <summary>A button, a key, a chip, a tab: raised glass with a coloured edge and a glow under the pointer.</summary>
    Plate,

    /// <summary>A list row: clear at rest, a tinted band under the pointer or when chosen.</summary>
    Row,

    /// <summary>A text field: sunk glass with a coloured underline.</summary>
    Field,

    /// <summary>A round count badge: a filled disc.</summary>
    Badge,

    /// <summary>A scrollbar's thumb or track.</summary>
    Scroll,

    /// <summary>A checkbox.</summary>
    Check,

    /// <summary>A small mark (glyph_*): the theme's own neon glyph of that name, when it has one.</summary>
    Glyph,

    /// <summary>A pictogram tile (tile_*): the theme's own neon tile of that name, when it has one.</summary>
    Tile
}

/// <summary>Which colour of the theme a drawn piece is keyed in (PcThemeSO's tokens).</summary>
public enum PcTone
{
    /// <summary>The quiet edge on glass (most panels and plates).</summary>
    Quiet,

    /// <summary>Phosphor cyan: the primary (owned, a match, focus).</summary>
    Primary,

    /// <summary>The accent (magenta, tinted by the leading culture): the chosen option, the Start key, a buyable card.</summary>
    Accent,

    /// <summary>Amber: warnings and money (a card too dear, a brass plate).</summary>
    Amber,

    /// <summary>The alert red: a close key, an error field.</summary>
    Danger,

    /// <summary>Dimmed: locked or retired.</summary>
    Muted
}

/// <summary>A cel sprite's place in the cyberpunk PC: the piece drawn for it, its tone and its state.</summary>
public readonly struct PcPart : IEquatable<PcPart>
{
    /// <summary>The drawn piece (Keep: the cel sprite stays).</summary>
    public readonly PcPiece Piece;

    /// <summary>The colour it is keyed in.</summary>
    public readonly PcTone Tone;

    /// <summary>Its state (a control's rest, hover, pressed or locked look).</summary>
    public readonly KitState State;

    /// <summary>A part.</summary>
    public PcPart(PcPiece piece, PcTone tone, KitState state)
    {
        Piece = piece;
        Tone = tone;
        State = state;
    }

    /// <summary>The cel sprite stays.</summary>
    public static readonly PcPart Keep = new PcPart(PcPiece.Keep, PcTone.Quiet, KitState.Rest);

    /// <inheritdoc />
    public bool Equals(PcPart other) => Piece == other.Piece && Tone == other.Tone && State == other.State;

    /// <inheritdoc />
    public override bool Equals(object obj) => obj is PcPart other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => ((int)Piece * 31 + (int)Tone) * 31 + (int)State;

    /// <inheritdoc />
    public override string ToString() => $"{Piece}/{Tone}/{State}";
}

/// <summary>
/// The cyberpunk PC's grammar (D2, the night of 2026-11-07): which drawn
/// piece replaces each cel kit sprite on the PC, by the sprite's name
/// (docs/UI_KIT.md's naming, component_variant_state), and the taskbar's
/// glyph-only rule. The cel kit's colour words map to the theme's tones:
/// oxblood (the kit's chosen and primary plates) to the accent, red to the
/// alert, green to phosphor, brass to amber, bone, slate and lavender to the
/// quiet edge. Pure, so the runtime skin (PcSkin) and its tests share one
/// table.
/// </summary>
public static class PcSkinRules
{
    /// <summary>The cyberpunk part for the cel sprite <paramref name="sprite"/> (Keep for null, for art the theme keeps, and for any name it does not know).</summary>
    public static PcPart Of(string sprite)
    {
        if (string.IsNullOrEmpty(sprite))
            return PcPart.Keep;
        KitState state = StateOf(sprite);
        if (Starts(sprite, "glyph_"))
            return new PcPart(PcPiece.Glyph, PcTone.Primary, state);
        if (Starts(sprite, "tile_"))
            return new PcPart(PcPiece.Tile, PcTone.Primary, state);
        if (Starts(sprite, "titlebar_"))
            return new PcPart(PcPiece.TitleBar, PcTone.Quiet, KitState.Rest);
        if (sprite == "taskbar")
            return new PcPart(PcPiece.Deck, PcTone.Quiet, KitState.Rest);
        if (Starts(sprite, "upgradecard_"))
            return new PcPart(PcPiece.Panel, CardTone(sprite), KitState.Rest);
        if (Starts(sprite, "panel_") || Starts(sprite, "dropdown") || Starts(sprite, "content_inset") || sprite == "tooltip" || Starts(sprite, "folder_body")
            || Starts(sprite, "segmented_track") || Starts(sprite, "strip_"))
            return new PcPart(PcPiece.Panel, Starts(sprite, "strip_") ? Tone(sprite) : PcTone.Quiet, KitState.Rest);
        if (Starts(sprite, "choicecard_"))
            return new PcPart(PcPiece.Panel, sprite.EndsWith("_selected", StringComparison.Ordinal) ? PcTone.Accent : PcTone.Quiet, state);
        if (Starts(sprite, "listrow_") || sprite == "row_highlight")
            return new PcPart(PcPiece.Row, sprite.EndsWith("_selected", StringComparison.Ordinal) ? PcTone.Accent : PcTone.Primary,
                              sprite.EndsWith("_rest", StringComparison.Ordinal) ? KitState.Rest : KitState.Hover);
        if (Starts(sprite, "field_"))
            return new PcPart(PcPiece.Field, sprite == "field_focus" ? PcTone.Primary : sprite == "field_error" ? PcTone.Danger : PcTone.Quiet, KitState.Rest);
        if (Starts(sprite, "badge_"))
            return new PcPart(PcPiece.Badge, Starts(sprite, "badge_brass") ? PcTone.Amber : PcTone.Accent, KitState.Rest);
        if (Starts(sprite, "scroll_"))
            return new PcPart(PcPiece.Scroll, sprite == "scroll_thumb" ? PcTone.Primary : PcTone.Quiet, KitState.Rest);
        if (Starts(sprite, "checkbox_"))
            return new PcPart(PcPiece.Check, sprite == "checkbox_on" ? PcTone.Accent : PcTone.Quiet, KitState.Rest);
        if (Starts(sprite, "segment_") || Starts(sprite, "chip_") || Starts(sprite, "foldertab_"))
            return new PcPart(PcPiece.Plate, Chosen(sprite) ? PcTone.Accent : PcTone.Quiet, Chosen(sprite) ? KitState.Rest : state);
        if (Starts(sprite, "plate_") || Starts(sprite, "miniplate_") || Starts(sprite, "iconkey") || Starts(sprite, "keycap_") || Starts(sprite, "pill_")
            || Starts(sprite, "card_") || Starts(sprite, "inspect_"))
            return new PcPart(PcPiece.Plate, Tone(sprite), state);
        return PcPart.Keep;
    }

    /// <summary>
    /// True when a taskbar window button <paramref name="width"/> units wide
    /// shows its title beside its glyph; narrower, it shows the glyph only and
    /// the title moves to its hover hint (UI-14: a title is never cut mid-letter).
    /// </summary>
    public static bool TaskbarShowsLabel(float width, float labelLeast) => width >= labelLeast;

    /// <summary>The state a sprite's name ends in (rest when it names none).</summary>
    public static KitState StateOf(string sprite)
    {
        if (sprite.EndsWith("_hover", StringComparison.Ordinal))
            return KitState.Hover;
        if (sprite.EndsWith("_pressed", StringComparison.Ordinal))
            return KitState.Pressed;
        if (sprite.EndsWith("_locked", StringComparison.Ordinal))
            return KitState.Locked;
        return KitState.Rest;
    }

    /// <summary>A plate's tone by the kit's colour word in its name.</summary>
    private static PcTone Tone(string sprite)
    {
        if (sprite.Contains("_ox"))
            return PcTone.Accent;
        if (sprite.Contains("_red"))
            return PcTone.Danger;
        if (sprite.Contains("_green"))
            return PcTone.Primary;
        if (sprite.Contains("_brass"))
            return PcTone.Amber;
        return PcTone.Quiet;
    }

    /// <summary>An upgrade card's tone by its state: owned phosphor, buyable the accent, too dear amber, locked dimmed.</summary>
    private static PcTone CardTone(string sprite)
    {
        if (sprite.EndsWith("_owned", StringComparison.Ordinal))
            return PcTone.Primary;
        if (sprite.EndsWith("_dear", StringComparison.Ordinal))
            return PcTone.Amber;
        if (sprite.EndsWith("_locked", StringComparison.Ordinal))
            return PcTone.Muted;
        return PcTone.Accent;
    }

    /// <summary>True for the chosen member of a set (a segment on, an active chip, the selected folder tab).</summary>
    private static bool Chosen(string sprite) =>
        sprite == "segment_on" || sprite == "chip_active" || sprite.EndsWith("_selected", StringComparison.Ordinal) || sprite.EndsWith("_new", StringComparison.Ordinal);

    private static bool Starts(string s, string prefix) => s.StartsWith(prefix, StringComparison.Ordinal);
}
