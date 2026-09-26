using System;
using System.Collections.Generic;

/// <summary>
/// The Investigation app's tabs, one per case source (the PC redesign AP1,
/// AP5). The values are pinned (append only): the player's tab order is saved
/// by their names (TabOrder.Save).
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
/// The order of the Investigation app's tabs (the PC redesign AP1, AP3, AP4):
/// one order shared by both panes' strips, which the player rearranges by
/// dragging a tab past a neighbour's middle (DragTarget) or with Move left,
/// Move right and Reset tab order, and which is saved per player by the tabs'
/// names (Save, Parse). Positions (Tabs) are what Ctrl+1…6 will follow
/// (phase 20). It also says which tabs are the case's. Pure; InvestigationApp owns the one instance.
/// </summary>
public sealed class TabOrder
{
    /// <summary>The default order: Documents, Records, Reference, Transcript, Report, Rules.</summary>
    public static readonly IReadOnlyList<AppTab> Default = new[]
    {
        AppTab.Documents, AppTab.Records, AppTab.Reference, AppTab.Transcript, AppTab.Report, AppTab.Rules
    };

    private readonly List<AppTab> _tabs = new List<AppTab>(Default);

    /// <summary>The tabs in their order.</summary>
    public IReadOnlyList<AppTab> Tabs => _tabs;

    /// <summary>
    /// A saved order ("Rules,Documents,…", Save's text): the tabs it names, in
    /// that order (an unknown or repeated name is skipped), then every tab it
    /// does not name, in the default order (so a tab added later joins at the
    /// end and the player's arrangement stays). Null or blank gives the
    /// default order.
    /// </summary>
    public static TabOrder Parse(string saved)
    {
        var order = new TabOrder();
        if (string.IsNullOrWhiteSpace(saved))
            return order;

        var named = new List<AppTab>();
        foreach (string token in saved.Split(','))
        {
            string name = token.Trim();
            foreach (AppTab tab in Default)
                if (string.Equals(tab.ToString(), name, StringComparison.Ordinal) && !named.Contains(tab))
                    named.Add(tab);
        }
        foreach (AppTab tab in Default)
            if (!named.Contains(tab))
                named.Add(tab);

        order._tabs.Clear();
        order._tabs.AddRange(named);
        return order;
    }

    /// <summary>The order as saved: the tabs' names joined by commas.</summary>
    public string Save() => string.Join(",", _tabs);

    /// <summary>The tab's position in the strip (0-based).</summary>
    public int PositionOf(AppTab tab) => _tabs.IndexOf(tab);

    /// <summary>Moves the tab at <paramref name="from"/> to <paramref name="to"/> (clamped into the strip), the others closing up; false when nothing moved.</summary>
    public bool Move(int from, int to)
    {
        if (from < 0 || from >= _tabs.Count)
            return false;
        to = Math.Max(0, Math.Min(_tabs.Count - 1, to));
        if (to == from)
            return false;
        AppTab tab = _tabs[from];
        _tabs.RemoveAt(from);
        _tabs.Insert(to, tab);
        return true;
    }

    /// <summary>Back to the default order ("Reset tab order"); false when it already was.</summary>
    public bool Reset()
    {
        bool changed = false;
        for (int i = 0; i < Default.Count; i++)
            changed |= _tabs[i] != Default[i];
        _tabs.Clear();
        _tabs.AddRange(Default);
        return changed;
    }

    /// <summary>
    /// Where a dragged tab belongs (AP4): the tab at <paramref name="from"/>
    /// goes past every other tab whose middle the pointer is past.
    /// <paramref name="middles"/> are the strip's tab middles in position order
    /// (the dragged tab's own included) and <paramref name="pointer"/> the
    /// pointer, both along the strip.
    /// </summary>
    public static int DragTarget(IReadOnlyList<float> middles, int from, float pointer)
    {
        int target = 0;
        for (int i = 0; i < middles.Count; i++)
            if (i != from && pointer > middles[i])
                target++;
        return target;
    }

    /// <summary>
    /// True for a case source (Documents, Transcript, Report): between
    /// travellers its view shows the no-case state (AP8), and a new case drops
    /// its places from the panes' histories. Records, Reference and Rules are
    /// the day's and still work between travellers.
    /// </summary>
    public static bool IsCaseSource(AppTab tab) =>
        tab == AppTab.Documents || tab == AppTab.Transcript || tab == AppTab.Report;
}
