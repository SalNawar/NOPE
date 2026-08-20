using System.Text;

/// <summary>
/// Naming and color-derivation rules for the generated ranked-layer placeholder art.
/// Pure C# (no Unity types) so the hash stability and filename sanitization are
/// covered by EditMode tests — the editor builder consumes these.
/// </summary>
public static class RankedArtNaming
{
    /// <summary>Prefix on every generated file, so the set stays greppable and bulk-deletable.</summary>
    public const string Prefix = "ranked_";

    /// <summary>Separator standing in for ':' in profile-attribute ids (':' is illegal in Windows filenames).</summary>
    public const string PairSeparator = "__";

    // Muted bands keep generated colors in the grubby-office register instead of neon.
    private const float MinSaturation = 0.35f;
    private const float SaturationRange = 0.30f;
    private const float MinValue = 0.45f;
    private const float ValueRange = 0.30f;

    /// <summary>
    /// FNV-1a 32-bit over the UTF-8 bytes of the lowercased id. Deliberately not
    /// string.GetHashCode(), which is not contractually stable across runs or
    /// platforms — an unstable hash would silently churn generated art.
    /// </summary>
    public static uint Hash(string id)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;

        uint hash = offsetBasis;

        if (string.IsNullOrEmpty(id))
            return hash;

        byte[] bytes = Encoding.UTF8.GetBytes(id.ToLowerInvariant());

        foreach (byte b in bytes)
        {
            hash ^= b;
            hash *= prime;
        }

        return hash;
    }

    /// <summary>
    /// Deterministic HSV for an id with no authored palette entry. Varies saturation
    /// and value as well as hue, so two ids landing on nearby hues still read apart.
    /// </summary>
    public static void HsvFor(string id, out float h, out float s, out float v)
    {
        uint hash = Hash(id);

        h = (hash & 0xFFFF) / 65535f;
        s = MinSaturation + ((hash >> 16) & 0xFF) / 255f * SaturationRange;
        v = MinValue + ((hash >> 24) & 0xFF) / 255f * ValueRange;
    }

    /// <summary>File name (no extension) for one ranked-art variant.</summary>
    public static string FileNameFor(RankCategory category, string id)
    {
        string slot = category switch
        {
            RankCategory.TopNation => "nation",
            RankCategory.TopAttribute => "attr",
            RankCategory.TopProfileAttribute => "pair",
            _ => "unknown"
        };

        return $"{Prefix}{slot}_{Sanitize(id)}";
    }

    /// <summary>
    /// Makes an id safe for a filename: ':' becomes the pair separator, any other
    /// non-alphanumeric character becomes '_'. Lowercased so casing can't produce
    /// two files that collide on case-insensitive filesystems.
    /// </summary>
    public static string Sanitize(string id)
    {
        if (string.IsNullOrEmpty(id))
            return "unknown";

        var sb = new StringBuilder(id.Length + 2);

        foreach (char c in id)
        {
            if (c == ':')
                sb.Append(PairSeparator);
            else if (char.IsLetterOrDigit(c))
                sb.Append(char.ToLowerInvariant(c));
            else
                sb.Append('_');
        }

        return sb.ToString();
    }
}
