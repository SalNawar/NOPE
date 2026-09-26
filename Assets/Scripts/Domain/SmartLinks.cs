using System;
using System.Collections.Generic;

/// <summary>The claimed place of the case a link starts from: the claim's nation and era ids (null when there is no case).</summary>
public readonly struct CaseClaim
{
    /// <summary>The claim of <paramref name="nationId"/> in <paramref name="eraId"/>.</summary>
    public CaseClaim(string nationId, string eraId)
    {
        NationId = nationId;
        EraId = eraId;
    }

    /// <summary>The claimed nation's id.</summary>
    public string NationId { get; }

    /// <summary>The claimed era's id.</summary>
    public string EraId { get; }

    /// <summary>True when both ids are set (a traveller is at the desk).</summary>
    public bool IsKnown => !string.IsNullOrEmpty(NationId) && !string.IsNullOrEmpty(EraId);
}

/// <summary>
/// Where a pane of the Investigation app goes (the PC redesign LK1, AP9, CM2):
/// a smart link's target, a pane's history entry (Back and Forward), a dock
/// side's source, and later a search hit's or a pin's place (phases 19, 20).
/// It names a tab and, in it, an item (a paper or a book by its chip; -1: the
/// view's own), a row (a pick key: a field, a book row, a line, a record row;
/// null: none) and, for Records, the lookup to run and the category of the
/// row to go to. None goes nowhere: no ↗ is drawn, a dock side is no link.
/// </summary>
public readonly struct LinkTarget : IEquatable<LinkTarget>
{
    private readonly bool _set;

    private LinkTarget(AppTab tab, int item, string key, string query, ClueCategory? recordRow)
    {
        _set = true;
        Tab = tab;
        Item = item < 0 ? -1 : item;
        Key = string.IsNullOrEmpty(key) ? null : key;
        Query = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        RecordRow = recordRow;
    }

    /// <summary>Nowhere.</summary>
    public static LinkTarget None => default;

    /// <summary>A tab, showing item <paramref name="item"/> (a chip's index; -1: whatever the view shows).</summary>
    public static LinkTarget ToTab(AppTab tab, int item = -1) => new LinkTarget(tab, item, null, null, null);

    /// <summary>A row of a tab by its pick key, in item <paramref name="item"/> (-1: the view finds the item from the key).</summary>
    public static LinkTarget ToRow(AppTab tab, string rowKey, int item = -1) => new LinkTarget(tab, item, rowKey, null, null);

    /// <summary>Records: the lookup <paramref name="query"/> run, at the found record's row of <paramref name="row"/> (null: the record's top).</summary>
    public static LinkTarget ToRecords(string query, ClueCategory? row = null) => new LinkTarget(AppTab.Records, -1, null, query, row);

    /// <summary>True for nowhere.</summary>
    public bool IsNone => !_set;

    /// <summary>The tab to show.</summary>
    public AppTab Tab { get; }

    /// <summary>The item to show (a chip's index), or -1.</summary>
    public int Item { get; }

    /// <summary>The row to go to (a pick key), or null.</summary>
    public string Key { get; }

    /// <summary>Records' lookup (a Citizen ID or a name), or null.</summary>
    public string Query { get; }

    /// <summary>The category of the found record's row to go to, or null.</summary>
    public ClueCategory? RecordRow { get; }

    /// <inheritdoc />
    public bool Equals(LinkTarget other) =>
        _set == other._set && Tab == other.Tab && Item == other.Item && Key == other.Key && Query == other.Query && RecordRow == other.RecordRow;

    /// <inheritdoc />
    public override bool Equals(object obj) => obj is LinkTarget other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            int h = _set ? 17 : 0;
            h = h * 31 + (int)Tab;
            h = h * 31 + Item;
            h = h * 31 + (Key != null ? StringComparer.Ordinal.GetHashCode(Key) : 0);
            h = h * 31 + (Query != null ? StringComparer.Ordinal.GetHashCode(Query) : 0);
            return h * 31 + (RecordRow.HasValue ? (int)RecordRow.Value + 1 : 0);
        }
    }

    /// <summary>A readable form for tests and logs ("Reference book:Currency:greece:ancient").</summary>
    public override string ToString() =>
        IsNone ? "None" : Tab + (Item >= 0 ? " #" + Item : "") + (Key != null ? " " + Key : "") + (Query != null ? " ?" + Query : "") +
                          (RecordRow.HasValue ? " @" + RecordRow.Value : "");
}

/// <summary>
/// Smart links (the PC redesign LK1, CM2, §4.3): from a statement to where it
/// is checked, and from a pick to where it was picked. A place-fact field or
/// answer (Currency, Language, Technology, Geography, Politics, Material, and
/// Culture: the Costume Guide) goes to its book's claimed-place row. Name,
/// Date of Birth and the record categories (Citizen ID, Destination: the
/// record's registered origin or booked departure, Incident, and a Citizen
/// Account's status, transponder, transponder class and debt) go to the
/// traveller's own record in Records, looked up by the paper's Citizen ID,
/// else its Name, at that category's row. The directive-only dates (a
/// departure date, a Valid Until) go to Rules. A category with no target (a
/// later ClueCategory until it is mapped here) gives None. A link only shows
/// where to look: it never picks. Pure; the app's rows and the compare dock
/// follow it.
/// </summary>
public static class SmartLinks
{
    /// <summary>A document field's link (<paramref name="paper"/>: all the fields of the field's own paper, for the record lookup).</summary>
    public static LinkTarget ForField(DocumentField field, IReadOnlyList<DocumentField> paper, CaseClaim claim) =>
        field == null ? LinkTarget.None : For(field.category, claim, RecordLookup(paper));

    /// <summary>
    /// A traveller's answer's link: the same target as a field of its
    /// category, the record looked up by <paramref name="caseLookup"/> (the
    /// case's primary paper's Citizen ID, else its Name: CaseLookup).
    /// </summary>
    public static LinkTarget ForAnswer(ClueCategory category, CaseClaim claim, string caseLookup) => For(category, claim, caseLookup);

    /// <summary>
    /// Where a pick was picked (the compare dock's sides; phase 20's pins and
    /// recents): a field to its scanned paper's row (a paper not scanned yet,
    /// held at the desk, links nowhere), a line to the Transcript, a book row
    /// to the Reference, a record row to Records (looked up by the record's
    /// id). A garment is picked at the wheel and has no place in the app.
    /// </summary>
    public static LinkTarget ForKey(string pickKey, CasePapers papers)
    {
        if (PickKeys.TryField(pickKey, out int document, out _))
            return papers != null && papers.State(document) == PaperState.Scanned ? LinkTarget.ToRow(AppTab.Documents, pickKey, document) : LinkTarget.None;
        if (PickKeys.TryLine(pickKey, out _))
            return LinkTarget.ToRow(AppTab.Transcript, pickKey);
        if (PickKeys.TryBookRow(pickKey, out _, out _, out _))
            return LinkTarget.ToRow(AppTab.Reference, pickKey);
        if (PickKeys.TryRecord(pickKey, out string recordId, out ClueCategory category))
            return LinkTarget.ToRecords(recordId, category);
        return LinkTarget.None;
    }

    /// <summary>A paper's record lookup: its Citizen ID field's value, else its Name field's, else null.</summary>
    private static string RecordLookup(IReadOnlyList<DocumentField> paper) =>
        Value(paper, ClueCategory.CitizenId) ?? Value(paper, ClueCategory.Name);

    /// <summary>The case's record lookup for answers: the first paper (in paper order) with a Citizen ID or a Name (RecordLookup), else <paramref name="travellerName"/>.</summary>
    public static string CaseLookup(IEnumerable<IReadOnlyList<DocumentField>> papers, string travellerName)
    {
        if (papers != null)
            foreach (IReadOnlyList<DocumentField> paper in papers)
            {
                string lookup = RecordLookup(paper);
                if (lookup != null)
                    return lookup;
            }
        return string.IsNullOrWhiteSpace(travellerName) ? null : travellerName.Trim();
    }

    /// <summary>A statement of <paramref name="category"/>'s target (§4.3).</summary>
    private static LinkTarget For(ClueCategory category, CaseClaim claim, string recordLookup)
    {
        switch (category)
        {
            case ClueCategory.Language:
            case ClueCategory.Material:
            case ClueCategory.Politics:
            case ClueCategory.Technology:
            case ClueCategory.Currency:
            case ClueCategory.Geography:
            case ClueCategory.Culture:
                return claim.IsKnown ? LinkTarget.ToRow(AppTab.Reference, PickKeys.BookRow(category, claim.NationId, claim.EraId)) : LinkTarget.None;
            case ClueCategory.Name:
            case ClueCategory.BirthDate:
            case ClueCategory.CitizenId:
            case ClueCategory.Destination:
            case ClueCategory.Incident:
            case ClueCategory.AccountStatus:
            case ClueCategory.TransponderId:
            case ClueCategory.TransponderClass:
            case ClueCategory.Debt:
                return string.IsNullOrWhiteSpace(recordLookup) ? LinkTarget.None : LinkTarget.ToRecords(recordLookup, category);
            case ClueCategory.DepartureDate:
            case ClueCategory.Expiry:
                return LinkTarget.ToTab(AppTab.Rules);
            default:
                return LinkTarget.None;
        }
    }

    /// <summary>The first non-blank value of a field of <paramref name="category"/> on the paper (trimmed), or null.</summary>
    private static string Value(IReadOnlyList<DocumentField> paper, ClueCategory category)
    {
        if (paper == null)
            return null;
        foreach (DocumentField field in paper)
            if (field != null && field.category == category && !string.IsNullOrWhiteSpace(field.value))
                return field.value.Trim();
        return null;
    }
}

/// <summary>
/// The Investigation app's pane layout rules (the PC redesign AP3): two panes
/// only when each gets a readable width, and a tab strip too narrow for every
/// label collapses its inactive tabs to glyphs. Pure; InvestigationApp and
/// AppPane apply them.
/// </summary>
public static class AppPanes
{
    /// <summary>True when a body <paramref name="bodyWidth"/> wide holds the sidebar and two panes of at least <paramref name="minPaneWidth"/> (a restored window has one pane).</summary>
    public static bool CanSplit(float bodyWidth, float sidebarWidth, float minPaneWidth) => bodyWidth >= sidebarWidth + 2f * minPaneWidth;

    /// <summary>True when a strip <paramref name="stripWidth"/> wide cannot give each of its <paramref name="tabs"/> tabs <paramref name="labelWidth"/>: the inactive tabs then show their glyphs.</summary>
    public static bool TabsNarrow(float stripWidth, int tabs, float labelWidth) => stripWidth < tabs * labelWidth;

    /// <summary>
    /// A vertical scroll's position (1: the top, 0: the bottom, as a uGUI
    /// ScrollRect reads it) that puts an item <paramref name="top"/> from the
    /// content's top and <paramref name="height"/> tall in the middle of a
    /// <paramref name="viewport"/>-tall window on a <paramref name="content"/>-tall
    /// content, as near as the ends allow (a link's row, SE4's jump).
    /// </summary>
    public static float ScrollToMiddle(float content, float viewport, float top, float height)
    {
        float travel = content - viewport;
        if (travel <= 0f)
            return 1f;
        float offset = Math.Max(0f, Math.Min(travel, top + height / 2f - viewport / 2f));
        return 1f - offset / travel;
    }
}
