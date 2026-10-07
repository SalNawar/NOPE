using System.Collections.Generic;

/// <summary>
/// The Investigation app's tabs, one per case source (the PC redesign AP1,
/// AP5; the PC workbench spec's shelf). The values are pinned (append only):
/// the checklist's content names them (StepSpec.tab).
/// </summary>
public enum AppTab
{
    /// <summary>The traveller's papers: a chip per paper, the scanned copy of the chosen one.</summary>
    Documents = 0,

    /// <summary>Citizen Records: the lookup by name or number and the record's extract.</summary>
    Records = 1,

    /// <summary>The reference books: a chip per book, the book's register of today's places.</summary>
    Reference = 2,

    /// <summary>The interview's transcript.</summary>
    Transcript = 3,

    /// <summary>The Deviation Report: the case's documented deviations.</summary>
    Report = 4,

    /// <summary>The day's travel directives.</summary>
    Rules = 5,

    /// <summary>The agency calendar: today's date as a value to click and match (the PC workbench spec IA6; lesson D10).</summary>
    Calendar = 6,

    /// <summary>The case board (the scanner app spec §2): the traveller's scanned papers against their record and today's rules (the record found, the rules check, the cross-check table, the overlay, their file and history).</summary>
    Board = 7
}

/// <summary>
/// The Investigation app's sources in their one order (the search's groups,
/// the views of a pane) and which are the case's. The PC workbench spec
/// retired the player's own order of the navigator (its drag, its menu and
/// its saved preference): the shelf groups the documents instead. Pure.
/// </summary>
public static class TabOrder
{
    /// <summary>The order: Documents, Records, Reference, Transcript, Report, Rules, Calendar, Board.</summary>
    public static readonly IReadOnlyList<AppTab> Default = new[]
    {
        AppTab.Documents, AppTab.Records, AppTab.Reference, AppTab.Transcript, AppTab.Report, AppTab.Rules, AppTab.Calendar, AppTab.Board
    };

    /// <summary>
    /// True for a case source (Documents, Transcript, Report, the case board): between
    /// travellers its view shows the no-case state (AP8), and a new case drops
    /// its places from the panes' histories. Records, Reference, Rules and the
    /// Calendar are the day's and still work between travellers.
    /// </summary>
    public static bool IsCaseSource(AppTab tab) =>
        tab == AppTab.Documents || tab == AppTab.Transcript || tab == AppTab.Report || tab == AppTab.Board;
}
