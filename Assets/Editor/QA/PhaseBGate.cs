using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// The Phase B integrity rules DemoGate enforces (FIX_PLAN_v2 rules 9 and 13,
/// exit gate #10): a merged Phase B track needs Saleh's spec approval on
/// record, and a bundle ships whole or not at all. A track counts as merged
/// when any registered gate check names it as owner (each track registers its
/// checks at merge: definition of done #3). Pure (PhaseBGateTests).
/// </summary>
public static class PhaseBGate
{
    /// <summary>The Phase B tracks (FIX_PLAN_v2 §4; PP-STUDY is docs only and merges no code).</summary>
    public static readonly string[] Tracks =
    {
        "BELL-SIGN", "DIALOGUE", "DESK-HOMES", "DETAIN", "TIMELINE-FAIL", "LANGUAGE", "BABEL-TUNER", "GLASSES",
        "PC-LANG-FX", "NEWS-WEB", "HOME-3D", "PORTALS",
    };

    /// <summary>The bundles: members that land together or not at all (rule 13: the language bundle; PC-LANG-FX is optional and not a member).</summary>
    public static readonly Dictionary<string, string[]> Bundles = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["LanguageBundle"] = new[] { "LANGUAGE", "BABEL-TUNER", "GLASSES" },
    };

    /// <summary>The Phase B tracks among <paramref name="owners"/> (the owners of the registered checks).</summary>
    public static List<string> Merged(IEnumerable<string> owners)
    {
        var set = new HashSet<string>(owners.Where(o => o != null).Select(o => o.Trim().ToUpperInvariant()));
        return Tracks.Where(set.Contains).ToList();
    }

    /// <summary>
    /// The approved tracks in an approvals file: a line naming the track,
    /// then a colon, with the word "approved" after it (for example
    /// "- BELL-SIGN: approved by Saleh 2026-10-21, 'yes go'").
    /// </summary>
    public static HashSet<string> Approved(string approvalsText)
    {
        var approved = new HashSet<string>(StringComparer.Ordinal);
        foreach (string line in (approvalsText ?? string.Empty).Split('\n'))
            foreach (string track in Tracks)
                if (Regex.IsMatch(line, $@"(^|[^A-Z0-9-]){Regex.Escape(track)}\s*:.*\bapproved\b", RegexOptions.IgnoreCase) &&
                    !Regex.IsMatch(line, @"\bnot\s+approved\b", RegexOptions.IgnoreCase))
                    approved.Add(track);
        return approved;
    }

    /// <summary>Merged tracks with no approval on record.</summary>
    public static List<string> Unapproved(IEnumerable<string> merged, ISet<string> approved) => merged.Where(t => !approved.Contains(t)).ToList();

    /// <summary>Bundles with some but not all members merged, as "bundle: has X, lacks Y".</summary>
    public static List<string> PartialBundles(IEnumerable<string> merged)
    {
        var set = new HashSet<string>(merged);
        var partial = new List<string>();
        foreach (KeyValuePair<string, string[]> b in Bundles)
        {
            string[] have = b.Value.Where(set.Contains).ToArray();
            if (have.Length > 0 && have.Length < b.Value.Length)
                partial.Add($"{b.Key}: has {string.Join(", ", have)}, lacks {string.Join(", ", b.Value.Except(have))}");
        }
        return partial;
    }
}
