using System;
using System.Collections.Generic;

/// <summary>
/// The named places the gameplay layer needs in the art office
/// (docs/SCENE_CONTRACT_GAMEPLAY.md). The art side may mark each with an
/// object named Anchor_{id}; otherwise the gameplay finds an existing object
/// by the contract's fallback paths, or uses the contract's default pose.
/// Stored by value in OfficeSceneContractSO: append only.
/// </summary>
public enum OfficeAnchorId
{
    /// <summary>The PC: its glass renderer shows the desktop's live clone; a click on it opens the PC frame.</summary>
    PCScreen,

    /// <summary>The PC's power knob in the office view (a click turns the screen on or off).</summary>
    PCPower,

    /// <summary>The desk top papers land on (its renderer's top face; paper centres stay over it and the scanner).</summary>
    DeskSurface,

    /// <summary>The desk scanner: papers dropped on it are scanned to the PC.</summary>
    Scanner,

    /// <summary>Where the traveller stands (the feet), facing the desk.</summary>
    Traveller,

    /// <summary>Where papers slide in from and back to (the traveller's side of the desk).</summary>
    HandOver,

    /// <summary>The READY sign (the art's NEXT sign): a click calls the next traveller.</summary>
    NextSign,

    /// <summary>The desk intercom (a phone): a click opens the traveller wheel.</summary>
    Intercom,

    /// <summary>The stamp (a click reaction).</summary>
    Stamp,

    /// <summary>The credits till (a click shows the credits).</summary>
    Till,

    /// <summary>The timeline stability monitor (a click shows the stability).</summary>
    StabilityMonitor,

    /// <summary>The day calendar (a click shows the day).</summary>
    Calendar,

    /// <summary>The shift clock (a click shows the time).</summary>
    Clock,

    /// <summary>The day number text (TextMeshPro) the game writes.</summary>
    ReadoutDay,

    /// <summary>The stability percentage text the game writes and tints by band.</summary>
    ReadoutStability,

    /// <summary>The credits text the game writes.</summary>
    ReadoutCredits,

    /// <summary>The shift clock's digital text ("09:00") the game writes.</summary>
    ReadoutClock,

    /// <summary>The READY sign's caption the game writes.</summary>
    ReadoutNext,

    /// <summary>The office camera the player sees through (its raycasts reach the desk).</summary>
    OfficeCamera,

    /// <summary>The Cinemachine camera the office view shows (raised above every other one).</summary>
    OfficeVCam,

    /// <summary>A flavour prop (a click reaction).</summary>
    Calculator,

    /// <summary>A flavour prop (a click reaction).</summary>
    PenPot,

    /// <summary>A flavour prop (a click reaction).</summary>
    Stapler
}

/// <summary>Where a resolved anchor came from.</summary>
public enum AnchorSource
{
    /// <summary>An object named Anchor_{id} (the art side placed it).</summary>
    Anchor,

    /// <summary>An existing object found by one of the contract's fallback paths.</summary>
    Fallback,

    /// <summary>Nothing was found: the contract's default pose.</summary>
    Default,

    /// <summary>Nothing was found and the anchor has no default: the gameplay goes without it.</summary>
    Missing
}

/// <summary>The contract's naming and resolution-order rules (pure; the resolver applies them to a scene).</summary>
public static class OfficeContract
{
    /// <summary>The prefix of an explicit anchor object's name.</summary>
    public const string AnchorPrefix = "Anchor_";

    /// <summary>The top-level object Tools > TimeDesk > Add Gameplay Anchors creates the anchors under (never under a folder an art builder rebuilds).</summary>
    public const string AnchorRoot = "GameplayAnchors";

    /// <summary>The name of the explicit anchor object for <paramref name="id"/> ("Anchor_PCScreen").</summary>
    public static string AnchorName(OfficeAnchorId id) => AnchorPrefix + id;

    /// <summary>
    /// The paths to look for, in order: the explicit anchor's name, then each
    /// non-blank fallback path (trimmed). A path with '/' names an object from
    /// its scene root; a bare name matches an object of that name anywhere.
    /// </summary>
    public static List<string> Candidates(OfficeAnchorId id, IReadOnlyList<string> fallbacks)
    {
        var result = new List<string> { AnchorName(id) };
        if (fallbacks == null)
            return result;

        foreach (string path in fallbacks)
            if (!string.IsNullOrWhiteSpace(path))
                result.Add(path.Trim());
        return result;
    }

    /// <summary>True when a path is a bare object name (matched anywhere in the scene), not a root path.</summary>
    public static bool IsBareName(string path) => path != null && path.IndexOf('/') < 0;

    /// <summary>What a resolution found: candidate 0 is the explicit anchor, later ones are fallbacks; none (-1) is the default pose when the anchor has one, else missing.</summary>
    public static AnchorSource SourceOf(int candidateIndex, bool hasDefault)
    {
        if (candidateIndex == 0)
            return AnchorSource.Anchor;
        if (candidateIndex > 0)
            return AnchorSource.Fallback;
        return hasDefault ? AnchorSource.Default : AnchorSource.Missing;
    }

    /// <summary>True for a renderer or material name that marks a screen's glass ("CRT2_Glass", "CRT_Study_Glass", "Screen"): contains "glass" or "screen", any case.</summary>
    public static bool IsScreenName(string name) =>
        name != null && (name.IndexOf("glass", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         name.IndexOf("screen", StringComparison.OrdinalIgnoreCase) >= 0);
}
