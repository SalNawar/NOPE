using System;
using System.IO;

/// <summary>What went wrong with the save slot, worst last (SaveSafety.Worse): the player is told the worst one (SaveNotice).</summary>
public enum SaveProblem
{
    /// <summary>Nothing.</summary>
    None,

    /// <summary>The save was missing or unreadable and the run came back from the temp file or the backup.</summary>
    Recovered,

    /// <summary>The save is from before this build's format: set aside, a new run starts.</summary>
    TooOld,

    /// <summary>The save could not be read: set aside, kept for a report.</summary>
    Unreadable,

    /// <summary>A save could not be written (a full disk, a read-only folder): the run goes on, the last good save stands.</summary>
    WriteFailed
}

/// <summary>
/// The save slot's safety rules (RUN-SAFE, BUG-35a: "Never delete without
/// confirmation"; "an unreadable save is kept and reported, never silently
/// deleted"): a save file this build cannot continue (unreadable, or from
/// before its format) and the run a new run replaces are set aside beside
/// the slot under a name of their own (the reason and the time), never
/// deleted or written over; which problem the player is told about. Pure,
/// so every case is tested headless; SaveSystem applies them.
/// </summary>
public static class SaveSafety
{
    /// <summary>The reason a file the build cannot read is set aside under.</summary>
    public const string Unreadable = "unreadable";

    /// <summary>The reason a file from before this build's save format is set aside under.</summary>
    public const string TooOldReason = "old-format";

    /// <summary>The reason the run a new run replaces is set aside under.</summary>
    public const string Replaced = "replaced";

    /// <summary>
    /// The name a set-aside copy of <paramref name="fileName"/> takes: its
    /// name, the <paramref name="reason"/> and the time, then its extension
    /// ("nope_save.unreadable-20261020-153005.json"); a <paramref name="copy"/>
    /// above 1 is added for a second set-aside in the same second.
    /// </summary>
    public static string AsideName(string fileName, string reason, DateTime stamp, int copy = 1) =>
        $"{Path.GetFileNameWithoutExtension(fileName)}.{reason}-{stamp:yyyyMMdd-HHmmss}{(copy > 1 ? "-" + copy : string.Empty)}{Path.GetExtension(fileName)}";

    /// <summary>What a load makes of a slot file of <paramref name="version"/> (-1: unreadable): Unreadable, TooOld before <paramref name="minVersion"/>, else None (it is read).</summary>
    public static SaveProblem Check(int version, int minVersion) =>
        version < 0 ? SaveProblem.Unreadable : version < minVersion ? SaveProblem.TooOld : SaveProblem.None;

    /// <summary>The reason a file with <paramref name="problem"/> is set aside under (Unreadable or TooOldReason).</summary>
    public static string ReasonOf(SaveProblem problem) => problem == SaveProblem.TooOld ? TooOldReason : Unreadable;

    /// <summary>The worse of two problems (the later in SaveProblem's order): the one the player is told about.</summary>
    public static SaveProblem Worse(SaveProblem a, SaveProblem b) => (int)a >= (int)b ? a : b;

    /// <summary>The problem left after a save is written: a failed write is over; a set-aside or recovered file stays reported.</summary>
    public static SaveProblem AfterGoodSave(SaveProblem problem) => problem == SaveProblem.WriteFailed ? SaveProblem.None : problem;
}
