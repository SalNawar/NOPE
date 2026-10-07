using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The case board's presenter (the scanner app spec §2): from the case at the
/// desk, its scanned papers (the fields the day prints), the day's registry,
/// directives and papers menu, it builds the board's model (RecordLookup,
/// RulesCheck, CrossCheck, the traveller's file) and shows it in every pane's
/// board; a lookup that finds the record turns every Records view to it
/// (the scanned fields' links land on it: "pre-linked"), and one that finds
/// none raises NoRecord once per number (the façade logs the finding). The
/// façade calls Refresh whenever a paper is scanned or handed over or a
/// paper is flagged.
/// </summary>
public sealed class CaseBoardPresenter
{
    private readonly IReadOnlyList<CaseBoardView> _views;
    private readonly IReadOnlyList<RecordsView> _records;
    private CaseInstance _case;
    private string _lookedUp;
    private string _noRecord;

    /// <summary>A presenter over the panes' boards and Records views (null entries skipped).</summary>
    public CaseBoardPresenter(IReadOnlyList<CaseBoardView> views, IReadOnlyList<RecordsView> records)
    {
        _views = views ?? Array.Empty<CaseBoardView>();
        _records = records ?? Array.Empty<RecordsView>();
    }

    /// <summary>Raised once per case and number when the scanned papers name no record on file: the query looked up (the façade logs FindingKind.NoRecord).</summary>
    public event Action<string> NoRecord;

    /// <summary>The boards (the façade listens to their flags).</summary>
    public IReadOnlyList<CaseBoardView> Views => _views;

    /// <summary>A traveller is presented: the boards forget the last one.</summary>
    public void BeginCase(CaseInstance inst)
    {
        _case = inst;
        _lookedUp = null;
        _noRecord = null;
        foreach (CaseBoardView view in _views)
            if (view != null)
                view.Clear();
    }

    /// <summary>The traveller was decided: the boards empty.</summary>
    public void EndCase() => BeginCase(null);

    /// <summary>
    /// Builds the model from the case's <paramref name="papers"/> (where each
    /// is), <paramref name="documents"/> (their fields; <paramref name="shows"/>
    /// says which fields the day prints), the day's <paramref name="registry"/>,
    /// <paramref name="rules"/> and <paramref name="missing"/> papers menu as
    /// of <paramref name="day"/>, with <paramref name="agency"/>'s offices (the
    /// Seal Register's seals for the overlay), and shows it.
    /// </summary>
    public void Refresh(CasePapers papers, IReadOnlyList<CaseDocument> documents, Func<int, ClueCategory, bool> shows, CitizenRegistry registry,
                        IReadOnlyList<TravelRuleSO> rules, MissingPapers missing, int day, AgencyContent agency)
    {
        if (_case == null)
            return;
        var model = new CaseBoardModel { Traveller = _case.visitorDisplayName ?? string.Empty, RuleAssets = rules ?? Array.Empty<TravelRuleSO>() };
        for (int i = 0; papers != null && documents != null && i < documents.Count && i < papers.Count; i++)
            if (papers.State(i) == PaperState.Scanned && documents[i] != null)
                model.Scanned.Add(new BoardPaper(i, documents[i].name,
                    (documents[i].fields ?? Array.Empty<DocumentField>()).Select(f => f != null && (shows == null || shows(i, f.category)) ? f : null).ToList()));

        (model.Record, model.By, model.Query) = RecordLookup.Find(registry != null ? registry.Records : null, model.Scanned);
        if (model.Record != null && model.Query != _lookedUp)
        {
            _lookedUp = model.Query;
            foreach (RecordsView records in _records)
                if (records != null)
                    records.Reveal(model.Query, null);
        }
        if (model.By == LookupBy.NoRecord && model.Query != _noRecord)
        {
            _noRecord = model.Query;
            NoRecord?.Invoke(model.Query);
        }

        bool own = model.Record != null && model.Record.Id == _case.RecordKey;
        SeenBefore? flag = own ? Visits.Flag(_case.seenBefore, day) : null;
        if (flag.HasValue)
        {
            string word = UiText.Get("visit.verdict." + flag.Value.Verdict);
            model.Flag = UiText.Format("records.group.seen", Visits.FlagText(flag.Value, word == "visit.verdict." + flag.Value.Verdict ? null : word,
                                                                               UiText.Get("visit.flag.days"), UiText.Get("visit.flag.yesterday")));
        }
        if (own)
            model.File = CitizenFile.Groups(_case.file, _case.seenBefore, day, UiText.Get);

        var required = new List<RequiredPaper>();
        if (missing != null)
            foreach (FormRequest request in missing.Requests)
                if (_case.requiredForms.Contains(request.Id))
                    required.Add(new RequiredPaper(request.Id, request.Label, request.Carried && papers != null && papers.State(request.Document) != PaperState.NotHandedOver));
        model.Rules = RulesCheck.Rows(model.RuleAssets.Select(r => r != null ? r.Directive : new Directive(TravelRuleType.Procedure, null)).ToList(), _case.kind,
                                      _case.claimedNation != null ? _case.claimedNation.id : null, _case.claimedEra != null ? _case.claimedEra.id : null,
                                      _case.originLabel, model.Scanned, required, _case.facts != null ? _case.facts.Today : null);

        foreach (BoardPaper paper in model.Scanned)
            model.Columns.Add(new CrossColumn(paper.Name, false, paper.Document,
                paper.Fields.Select((f, i) => f != null ? new CrossValue(f.category, i, UiText.DocumentWord(f.label), f.value) : (CrossValue?)null).Where(v => v.HasValue).Select(v => v.Value)));
        if (model.Record != null)
            model.Columns.Add(new CrossColumn(UiText.Get("board.cross.record"), true, -1, RecordLookup.RecordValues(model.Record)));
        model.Cross = CrossCheck.Rows(model.Columns);
        model.Overlays = Overlays(model, agency);

        foreach (CaseBoardView view in _views)
            if (view != null)
                view.Show(model);
    }

    /// <summary>
    /// The overlay's sources (the scanner app spec §2.5): each scanned paper
    /// (its photo is who it shows, its seal its office's), the record found,
    /// the traveller's face (who stands at the desk, Looks.IdentityKey) and
    /// the Seal Register's seals of the offices the scanned papers name.
    /// </summary>
    private List<OverlaySource> Overlays(CaseBoardModel model, AgencyContent agency)
    {
        var sources = new List<OverlaySource>();
        var offices = new List<string>();
        foreach (BoardPaper paper in model.Scanned)
        {
            sources.Add(new OverlaySource(paper.Name, paper.Fields.Where(f => f != null).Select(f => new OverlayValue(f.category, UiText.DocumentWord(f.label), f.value, f.issuer))));
            foreach (DocumentField seal in paper.Fields.Where(f => f != null && f.category == ClueCategory.Seal && !string.IsNullOrEmpty(f.issuer)))
                if (!offices.Contains(seal.issuer))
                    offices.Add(seal.issuer);
        }
        if (model.Record != null)
            sources.Add(new OverlaySource(UiText.Get("board.cross.record"), RecordLookup.RecordValues(model.Record).Select(v => new OverlayValue(v.Category, v.Label, v.Value))));
        if (_case.look != null)
            sources.Add(new OverlaySource(UiText.Get("board.overlay.face"),
                                          new[] { new OverlayValue(ClueCategory.Photo, UiText.Get("board.overlay.faceLabel"), Looks.IdentityKey(_case.look)) }));
        var register = new List<OverlayValue>();
        foreach (string id in offices)
        {
            AgencyOffice office = agency != null && agency.offices != null ? agency.offices.FirstOrDefault(o => o != null && o.id == id) : null;
            if (office != null && office.TryGetSeal(out Seal seal))
                register.Add(new OverlayValue(ClueCategory.Seal, office.name, Seals.Describe(seal), office.id));
        }
        if (register.Count > 0)
            sources.Add(new OverlaySource(UiText.Get("board.overlay.register"), register));
        return sources;
    }
}
