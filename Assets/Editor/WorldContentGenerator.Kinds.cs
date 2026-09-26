using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Generate World's traveller-kinds part (the redesign's phase 3): the claim
/// line per kind (world_source.json interview.claims, one row per kind,
/// written into the library as KindLine entries "interview.claims.{Kind}"),
/// checked through the Domain rule the validator shares (Interview.ClaimProblems)
/// against the kinds the wired blueprints make.
/// </summary>
public static partial class WorldContentGenerator
{
    /// <summary>One row of interview.claims: a kind's name (TravellerKind) and its claim ({place}).</summary>
    [Serializable] private sealed class ClaimData
    {
        /// <summary>The kind's name ("Displaced").</summary>
        public string kind;

        /// <summary>The kind's claim line ({place}).</summary>
        public string text;
    }

    /// <summary>The id of a kind's claim line, "interview.claims.{kind}": BuildClaims writes it, CheckClaims checks it.</summary>
    private static string ClaimLineId(string kind) => InterviewLineId($"claims.{kind}");

    /// <summary>
    /// Checks interview.claims (errors added, nothing written): each row names
    /// a TravellerKind, its line is ASCII and its id is unique (through
    /// <paramref name="id"/>, CheckInterview's one id set), and
    /// Interview.ClaimProblems holds (a line per kind the wired blueprints
    /// make, none twice, each with {place}).
    /// </summary>
    private static void CheckClaims(InterviewData iv, Authored authored, List<string> errors, Action<string, string> id)
    {
        ClaimData[] claims = iv.claims ?? Array.Empty<ClaimData>();
        foreach (ClaimData c in claims)
        {
            if (c == null)
                continue;
            if (!ParseEnum(c.kind, out TravellerKind _))
                errors.Add($"interview.claims: '{c.kind}' is not a traveller kind ({string.Join(", ", Enum.GetNames(typeof(TravellerKind)))}).");
            CheckAscii(ClaimLineId(c.kind), c.text, errors);
            id(ClaimLineId(c.kind), $"interview.claims '{c.kind}'");
        }

        errors.AddRange(Interview.ClaimProblems(BuildClaims(claims), Blueprints(authored).Select(b => b.Kind).Distinct()));
    }

    /// <summary>The claim rows as the library holds them (rows naming no kind are left out; CheckClaims reports them).</summary>
    private static List<KindLine> BuildClaims(ClaimData[] claims) =>
        (claims ?? Array.Empty<ClaimData>())
            .Select(c => c == null ? null
                : ParseEnum(c.kind, out TravellerKind kind) ? new KindLine { kind = kind, line = new LineText(ClaimLineId(c.kind), c.text) }
                : null)
            .Where(k => k != null)
            .ToList();
}
