using System.Collections.Generic;

/// <summary>Where a traveller of the reference run came from (the narrative workbook's Cases sheet).</summary>
public enum NarrativeCaseSource
{
    /// <summary>Drawn by the day (CaseFactory's random draws): a generated traveller.</summary>
    Generated,

    /// <summary>An authored forced slot of the day plan (world_source.json days[].forced).</summary>
    Forced,

    /// <summary>A premade drawn from the day's premade pool (days[].premades at days[].premadeChance).</summary>
    Pool
}

/// <summary>
/// One traveller of the narrative workbook's reference run, as the game made them
/// (CaseFactory) and as they would speak (InterviewScript and Voices): who they are,
/// where the slot came from, their fault, and every line they would say per slot. Filled
/// by the editor's export (it needs the generated assets); the Domain only lays it out.
/// </summary>
public sealed class NarrativeCase
{
    /// <summary>The run seed.</summary>
    public int Seed;

    /// <summary>The day (1-based).</summary>
    public int Day;

    /// <summary>The queue slot (1-based).</summary>
    public int Slot;

    /// <summary>Where the slot came from.</summary>
    public NarrativeCaseSource Source;

    /// <summary>The forced appearance's id (days 7-15), or "(slot)" for an unnamed forced slot; blank for a drawn traveller.</summary>
    public string Appearance = string.Empty;

    /// <summary>The premade's id; blank for a generated traveller.</summary>
    public string Premade = string.Empty;

    /// <summary>The name the desk sees.</summary>
    public string Name = string.Empty;

    /// <summary>The traveller kind.</summary>
    public string Kind = string.Empty;

    /// <summary>The personality's id (blank for a premade, who speaks its own lines).</summary>
    public string Personality = string.Empty;

    /// <summary>The claimed place's label.</summary>
    public string Claimed = string.Empty;

    /// <summary>Where a liar's tells really come from (blank for an honest traveller).</summary>
    public string TrueHome = string.Empty;

    /// <summary>The lie (a LieKind name; blank for none).</summary>
    public string Lie = string.Empty;

    /// <summary>The fault reason (Faults.Reason; blank for none) and its kind in words.</summary>
    public string Fault = string.Empty;

    /// <summary>The correct call: Accept or Deny.</summary>
    public string Correct = string.Empty;

    /// <summary>The desk's opener.</summary>
    public string Intro = string.Empty;

    /// <summary>The claim as they step up.</summary>
    public string Claim = string.Empty;

    /// <summary>The small-talk reply (blank: none).</summary>
    public string SmallTalk = string.Empty;

    /// <summary>A liar's slip after small talk (blank: none).</summary>
    public string Slip = string.Empty;

    /// <summary>Each askable question and the answer, one per line ("Currency: ...").</summary>
    public string Answers = string.Empty;

    /// <summary>Each paper request and the reply (a hand-over or a refusal), one per line.</summary>
    public string Papers = string.Empty;

    /// <summary>Each spoken request and the reply, one per line.</summary>
    public string Spoken = string.Empty;

    /// <summary>The reaction if stamped Accepted.</summary>
    public string IfAccepted = string.Empty;

    /// <summary>The reaction if stamped Denied.</summary>
    public string IfDenied = string.Empty;

    /// <summary>The narrative dialogs offered while they are at the desk (ids, "|" between).</summary>
    public string Dialogs = string.Empty;
}

/// <summary>Something the reference run saw happen: a story beat fired, a dialog offered, a flag set.</summary>
public sealed class NarrativeEvent
{
    /// <summary>An event of <paramref name="seed"/> on <paramref name="day"/>: <paramref name="kind"/> of <paramref name="id"/>.</summary>
    public NarrativeEvent(int seed, int day, string kind, string id)
    {
        Seed = seed;
        Day = day;
        Kind = kind;
        Id = id;
    }

    /// <summary>A story beat fired that night (Id: the history rule's id).</summary>
    public const string BeatFired = "beat";

    /// <summary>A dialog was on offer that day (Id: the dialog's id).</summary>
    public const string DialogOffered = "dialog";

    /// <summary>The run seed.</summary>
    public int Seed { get; }

    /// <summary>The day (for a beat, the night after it).</summary>
    public int Day { get; }

    /// <summary><see cref="BeatFired"/> or <see cref="DialogOffered"/>.</summary>
    public string Kind { get; }

    /// <summary>What it names.</summary>
    public string Id { get; }
}

/// <summary>What the narrative workbook reads besides world_source.json's tables: the reference run's travellers and events and the effects' meaning (from the generated assets, by the editor's export).</summary>
public sealed class NarrativeContext
{
    /// <summary>The reference run's travellers, in seed, day and slot order.</summary>
    public List<NarrativeCase> Cases { get; } = new List<NarrativeCase>();

    /// <summary>What the reference run saw happen.</summary>
    public List<NarrativeEvent> Events { get; } = new List<NarrativeEvent>();

    /// <summary>Each effect a dialog choice may name, by asset name: what it does in words.</summary>
    public Dictionary<string, string> Effects { get; } = new Dictionary<string, string>();

    /// <summary>Each effect's flags it sets (story beats and appearances read them).</summary>
    public Dictionary<string, List<string>> EffectFlags { get; } = new Dictionary<string, List<string>>();

    /// <summary>How the reference run was played, for the README ("seeds 12345, 7919 · Perfect play · the whole queue").</summary>
    public string RunNote = string.Empty;
}
