using System.Collections.Generic;

/// <summary>
/// The Investigation app's tabs, one per case source (the PC redesign AP1,
/// AP5). The values are pinned (append only): the player's tab order is saved
/// by them once tabs can be reordered (phase 18).
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
    Rules = 5
}

/// <summary>
/// The order of the Investigation app's tabs (the PC redesign AP1, AP4) and
/// which of them are the case's. Pure; the app's pane lays its tab strip out
/// in this order (phase 18 lets the player reorder it).
/// </summary>
public static class TabOrder
{
    /// <summary>The default order: Documents, Records, Reference, Transcript, Report, Rules.</summary>
    public static readonly IReadOnlyList<AppTab> Default = new[]
    {
        AppTab.Documents, AppTab.Records, AppTab.Reference, AppTab.Transcript, AppTab.Report, AppTab.Rules
    };

    /// <summary>
    /// True for a case source (Documents, Transcript, Report): between
    /// travellers its view shows the no-case state (AP8). Records, Reference
    /// and Rules are the day's and still work between travellers.
    /// </summary>
    public static bool IsCaseSource(AppTab tab) =>
        tab == AppTab.Documents || tab == AppTab.Transcript || tab == AppTab.Report;
}
