using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The Investigation app's Rules tab (the PC redesign AP5, FO9, §2.9): the
/// day's travel directives as their Directive Memo (Form_DirectiveMemo,
/// TC-940, on a FormPage at the pane's width): TO all desk officers, DATE
/// today in the agency's calendar, FROM the Customs Directorate, REF the
/// day's memo (the same reference Mail's copy carries), then a numbered
/// table of the directives (TravelRuleSO.Summary), the Directorate's
/// issuing line and the stamp area; with no directive, the line saying every
/// destination is cleared. A closure of one place has a ↗ to that place's
/// row in the first reference book (SmartLinks.ForPlace), once the books are
/// built. Nothing on the memo says whether a rule applies to the current
/// traveller. A day source: it works between travellers; Mail's directive
/// memo links here. Each pane has one; DayReference writes them all.
/// </summary>
public sealed class RulesView : AppView
{
    /// <summary>The Directive Memo in its scroll.</summary>
    [SerializeField] private FormPage page;

    /// <summary>The Directive Memo's page kind (Form_DirectiveMemo, TC-940).</summary>
    [SerializeField] private FormSpecSO memoForm;

    private IReadOnlyList<TravelRuleSO> _rules = System.Array.Empty<TravelRuleSO>();
    private ClueCategory? _firstBook;

    /// <inheritdoc />
    public override AppTab Tab => AppTab.Rules;

    /// <summary>The memo's form (null without a page).</summary>
    private FormView Form => page != null ? page.Form : null;

    private void Awake()
    {
        if (Form != null)
            Form.LinkClicked += Follow;
    }

    private void OnDestroy()
    {
        if (Form != null)
            Form.LinkClicked -= Follow;
    }

    /// <summary>
    /// Draws the memo: <paramref name="rules"/> (the day's directives, in
    /// order) headed with <paramref name="agency"/>'s block and dated for
    /// <paramref name="day"/>; a closure of one place links to its row in the
    /// book of <paramref name="firstBook"/> (null: no book built yet, no ↗).
    /// </summary>
    public void Show(IReadOnlyList<TravelRuleSO> rules, AgencyContent agency, int day, ClueCategory? firstBook)
    {
        _rules = rules ?? System.Array.Empty<TravelRuleSO>();
        _firstBook = firstBook;
        if (page == null || Form == null || memoForm == null)
            return;
        var directives = new List<string>(_rules.Count);
        foreach (TravelRuleSO rule in _rules)
            directives.Add(rule != null ? rule.Summary() : null);
        List<string[]> rows = DirectiveMemoPage.Rows(directives);

        FormData data = memoForm.Page(agency);
        string date = agency != null ? AgencyCalendar.Today(agency.firstDate, day) : null;
        data.Text = new Dictionary<string, string>
        {
            { DirectiveMemoPage.ToSlot, UiText.Get("rules.memo.to") },
            { DirectiveMemoPage.DateSlot, date ?? string.Empty },
            { DirectiveMemoPage.FromSlot, UiText.Get("mail.from.directorate") },
            { DirectiveMemoPage.RefSlot, MailText.DirectiveRef(day) },
            { DirectiveMemoPage.NoneSlot, rows.Count == 0 ? UiText.Get("directives.none") : string.Empty }
        };
        data.Rows = new Dictionary<string, IReadOnlyList<string[]>> { { DirectiveMemoPage.RowsSlot, rows } };
        page.Show(memoForm.form, data, _ => false, LinkHint);
    }

    /// <summary>The rule a table row's slot shows (the rows count the directives with a summary, in order), or null.</summary>
    private TravelRuleSO RuleOf(FormSlot slot)
    {
        if (slot.Source != DirectiveMemoPage.RowsSlot || slot.Row < 0)
            return null;
        int at = 0;
        foreach (TravelRuleSO rule in _rules)
        {
            if (rule == null || string.IsNullOrWhiteSpace(rule.Summary()))
                continue;
            if (at++ == slot.Row)
                return rule;
        }
        return null;
    }

    /// <summary>A directive's link: its place's row in the first book (SmartLinks.ForPlace); None for a closure of a whole era or nation, or a procedure.</summary>
    private LinkTarget Link(FormSlot slot)
    {
        TravelRuleSO rule = RuleOf(slot);
        return rule != null ? SmartLinks.ForPlace(rule.nation != null ? rule.nation.id : null, rule.era != null ? rule.era.id : null, _firstBook) : LinkTarget.None;
    }

    /// <summary>The ↗'s hover hint of a directive with a place ("Open the Reference: Florence (Medieval)"), or null (no ↗).</summary>
    private string LinkHint(FormSlot slot)
    {
        if (Link(slot).IsNone)
            return null;
        TravelRuleSO rule = RuleOf(slot);
        return UiText.Format("app.link.place", OriginLabels.Format(rule.nation.displayName, rule.era.displayName));
    }

    /// <summary>A ↗ was clicked: the directive's place, through the pane this memo is in (LK2).</summary>
    private void Follow(FormSlot slot)
    {
        LinkTarget link = Link(slot);
        AppPane pane = GetComponentInParent<AppPane>();
        if (!link.IsNone && pane != null)
            pane.FollowLink(link);
    }
}
