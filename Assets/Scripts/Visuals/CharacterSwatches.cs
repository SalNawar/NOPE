/// <summary>
/// The character art brief's colours: its five skin swatches and its five
/// hair colours. The art is baked to them (tools/characters reads this file:
/// bodies and heads of every tone are recoloured to the swatches, natural
/// hair and beards to the hair colours); the game never tints. Pure, so the
/// values are pinned by tests.
/// </summary>
public static class CharacterSwatches
{
    /// <summary>The art brief's skin swatches, tones 1..5 (#F1D3C0, #E0B394, #C39A6B, #94653F, #5C3A24).</summary>
    private static readonly (byte r, byte g, byte b)[] SkinSwatches =
    {
        (0xF1, 0xD3, 0xC0), (0xE0, 0xB3, 0x94), (0xC3, 0x9A, 0x6B), (0x94, 0x65, 0x3F), (0x5C, 0x3A, 0x24)
    };

    /// <summary>A neutral grey for anything unknown.</summary>
    public static readonly (byte r, byte g, byte b) Unknown = (0x80, 0x80, 0x80);

    /// <summary>The skin swatch of a tone 1..5 (grey outside that range).</summary>
    public static (byte r, byte g, byte b) Skin(int tone) =>
        tone >= 1 && tone <= SkinSwatches.Length ? SkinSwatches[tone - 1] : Unknown;

    /// <summary>A hair colour by its LookKeys token (black, brown, blond, red, grey); grey for anything else (a wig's own colour).</summary>
    public static (byte r, byte g, byte b) Hair(string colour)
    {
        switch (colour)
        {
            case "black": return (0x2B, 0x25, 0x22);
            case "brown": return (0x6B, 0x4A, 0x2F);
            case "blond": return (0xD8, 0xB5, 0x6A);
            case "red": return (0xA8, 0x45, 0x2A);
            case "grey": return (0xB4, 0xB4, 0xB4);
            default: return Unknown;
        }
    }
}
