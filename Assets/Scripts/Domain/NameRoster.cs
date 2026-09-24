using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Hands out visitor names that are unique within one day, so Citizen Records
/// (first-match lookup) always finds the right person. Prefers unused pool
/// names; once a pool is exhausted, adds a numeral suffix ("Marcus II").
/// Make a new roster for every generated day.
/// </summary>
public sealed class NameRoster
{
    /// <summary>Names already given out today (trimmed, case-insensitive).</summary>
    private readonly HashSet<string> _used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Marks a fixed name (e.g. a legendary visitor) as taken today.</summary>
    /// <returns>False when the name is blank or already taken.</returns>
    public bool Reserve(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        return _used.Add(name.Trim());
    }

    /// <summary>True when someone already has this name today (trimmed, case-insensitive).</summary>
    public bool IsTaken(string name) =>
        !string.IsNullOrWhiteSpace(name) && _used.Contains(name.Trim());

    /// <summary>
    /// Takes a name nobody has today: a random unused pool name, or, once every
    /// pool name is taken, a pool name with the lowest free numeral suffix.
    /// Returns null when the pool holds no usable names.
    /// </summary>
    /// <param name="randomIndex">Returns an index in [0, count); out-of-range values are clamped.</param>
    public string Take(IReadOnlyList<string> pool, Func<int, int> randomIndex)
    {
        if (pool == null || randomIndex == null)
            return null;

        var bases = new List<string>();
        var unused = new List<string>();
        foreach (string raw in pool)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            string name = raw.Trim();
            bases.Add(name);
            if (!_used.Contains(name))
                unused.Add(name);
        }

        if (bases.Count == 0)
            return null;

        if (unused.Count > 0)
        {
            string pick = unused[Clamp(randomIndex(unused.Count), unused.Count)];
            _used.Add(pick);
            return pick;
        }

        string baseName = bases[Clamp(randomIndex(bases.Count), bases.Count)];
        for (int n = 2; ; n++)
        {
            string candidate = $"{baseName} {Roman(n)}";
            if (_used.Add(candidate))
                return candidate;
        }
    }

    /// <summary>Roman numeral for 1..3999 (name suffixes).</summary>
    public static string Roman(int number)
    {
        if (number < 1 || number > 3999)
            throw new ArgumentOutOfRangeException(nameof(number), number, "Roman numerals cover 1..3999.");

        int[] values = { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
        string[] numerals = { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };
        var sb = new StringBuilder();
        for (int i = 0; i < values.Length; i++)
        {
            while (number >= values[i])
            {
                sb.Append(numerals[i]);
                number -= values[i];
            }
        }
        return sb.ToString();
    }

    /// <summary>Clamps a random index into [0, count).</summary>
    private static int Clamp(int index, int count) => index < 0 ? 0 : index >= count ? count - 1 : index;
}
