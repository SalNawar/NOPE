using System;
using System.Collections.Generic;

/// <summary>
/// The current traveller's documents (the PC redesign RF1, AP6): the papers
/// the Investigation app's Documents tabs show (one per pane: a chip each; a
/// scanned one's copy, its fields linking by the case's claim), where each paper is (CasePapers: not handed over, on the desk,
/// scanned; the app's counters), the documents as the desk and the interview
/// read them, the hand-over and the scan. With the desk, each document
/// becomes a paper (those handed over on arrival land at once) whose finished
/// scan brings its copy to the PC; without it, a document reaches the PC when
/// it is handed over. A paper reaching the PC raises Scanned (the app decides
/// what that shows: ScanArrival) and puts the paper and its fields into
/// search's case layer (redesign phase 19: a document joins when it reaches
/// the PC); nothing here opens a window. A held paper's row picked at the
/// desk goes into the compare as its scanned copy's row would; a paper
/// lifted into the hand is announced (Examined).
/// An analysis pass (the Analysis Scanner, a scan by hand) marks the first
/// contradicting pair the Deviation Report does not hold on both scanned
/// copies for the rest of the case, and the analysed copy's strip says so
/// (PaperAnalysis; the PC redesign SC4, SC5): nothing picked, logged or
/// opened. It subscribes to the desk it was given (only a
/// reachable one) and unsubscribes from that same instance (audit R4-003).
/// Plain C#; InvestigationUIController owns it.
/// </summary>
public sealed class CaseDocumentsPresenter
{
    private readonly IReadOnlyList<DocumentsView> _views;
    private readonly DeskController _desk;
    private readonly CompareController _compare;
    private readonly CaseIndex _index;

    /// <summary>The Deviation Report's documented categories (the evidence presenter's), read at each analysis pass.</summary>
    private readonly Func<IReadOnlyCollection<ClueCategory>> _documented;

    /// <summary>The case's analysis marks so far, each once (a mark lasts for the case).</summary>
    private readonly List<AnalysisMark> _marks = new List<AnalysisMark>();

    /// <summary>The current traveller's documents in paper order (name, fields, hand-over, photo).</summary>
    private readonly List<CaseDocument> _caseDocuments = new List<CaseDocument>();

    /// <summary>The form each of the current traveller's papers prints (redesign phase 4), in paper order.</summary>
    private readonly List<DocumentForm> _caseForms = new List<DocumentForm>();

    /// <summary>Where each of the current traveller's papers is.</summary>
    private CasePapers _papers = new CasePapers(0);

    /// <summary>Character art: the passport photos on the papers and the scanned copies.</summary>
    private CharacterArt _art;

    /// <summary>The desk whose events this listens to (null while detached).</summary>
    private DeskController _listening;

    /// <summary>The app's Documents views (one per pane; null entries are skipped), the desk (null when it is not reachable: documents then reach the PC at the hand-over), the compare, the Deviation Report's documented categories (what an analysis pass skips) and search's index (null: nothing indexed).</summary>
    public CaseDocumentsPresenter(IReadOnlyList<DocumentsView> views, DeskController reachableDesk, CompareController compare, Func<IReadOnlyCollection<ClueCategory>> documented, CaseIndex index)
    {
        _views = views ?? Array.Empty<DocumentsView>();
        _desk = reachableDesk;
        _compare = compare;
        _documented = documented ?? throw new ArgumentNullException(nameof(documented));
        _index = index;
    }

    /// <summary>A paper reached the PC (its index): scanned at the desk, or handed over where no desk is wired.</summary>
    public event Action<int> Scanned;

    /// <summary>A paper was handed over or scanned (the counters change).</summary>
    public event Action PapersChanged;

    /// <summary>A paper (its index) was lifted into the hand at the desk to be read.</summary>
    public event Action<int> Examined;

    /// <summary>The current traveller's documents, in paper order.</summary>
    public IReadOnlyList<CaseDocument> Documents => _caseDocuments;

    /// <summary>Where each of the current traveller's papers is.</summary>
    public CasePapers Papers => _papers;

    /// <summary>Starts listening to the desk's finished scans and picked rows.</summary>
    public void Attach()
    {
        if (_desk == null)
            return;
        _listening = _desk;
        _listening.ScanFinished += HandleScanFinished;
        _listening.FieldPicked += HandleFieldPicked;
        _listening.PaperExamined += HandleExamined;
    }

    /// <summary>Stops listening (to the instance it attached to).</summary>
    public void Detach()
    {
        if (_listening == null)
            return;
        _listening.ScanFinished -= HandleScanFinished;
        _listening.FieldPicked -= HandleFieldPicked;
        _listening.PaperExamined -= HandleExamined;
        _listening = null;
    }

    /// <summary>Sets the character art the passport photos are drawn with.</summary>
    public void SetCharacterArt(CharacterArt art) => _art = art;

    /// <summary>A traveller's document as the desk, the PC and the interview read it: its name, fields, hand-over, photo, form number and request group (the game's papers and the narrative workbook's Cases sheet).</summary>
    public static CaseDocument DocumentOf(DocumentInstance doc)
    {
        DocumentTemplateSO template = doc != null ? doc.template : null;
        return new CaseDocument
        {
            name = doc != null ? doc.DisplayName : UiText.Get("document.untitled"),
            fields = doc != null ? doc.fields : null,
            handOver = template != null ? template.handOver : DocumentHandOver.OnRequest,
            showsPhoto = template != null && template.showsPhoto,
            formNumber = template != null ? template.formNumber : string.Empty,
            askGroup = template != null ? template.askGroup : string.Empty
        };
    }

    /// <summary>
    /// Presents the traveller's documents: handed over, never taken: those
    /// marked "on arrival" when the traveller steps up, the others through the
    /// traveller wheel. The Documents view gets a chip and a (hidden) copy per
    /// paper. With the desk, each becomes a paper whose scan brings its copy
    /// to the PC; without it, a paper reaches the PC at the hand-over. Each
    /// paper prints its document's form, headed with <paramref name="agency"/>'s
    /// name and programme (redesign phase 4), and its copy draws that same
    /// form (phase 5). A paper not handed over is named as the desk asks for
    /// it: its request group's label (<paramref name="interview"/>'s
    /// askGroups, "Proof of means") until it is handed over, else its own name.
    /// Its boxes keep the room of their longest values (an origin at
    /// <paramref name="longestOrigin"/> characters; the document design spec,
    /// D2), and a photo shows who the papers say (CaseInstance.PhotoLook: a
    /// stranger for someone else's photo, D8).
    /// </summary>
    public void Present(CaseInstance inst, AgencyContent agency, InterviewLines interview, int longestOrigin)
    {
        _caseDocuments.Clear();
        _caseForms.Clear();
        _marks.Clear();
        var requestNames = new List<string>();
        if (inst != null)
            foreach (DocumentInstance doc in inst.documents)
            {
                DocumentTemplateSO template = doc != null ? doc.template : null;
                CaseDocument document = DocumentOf(doc);
                _caseDocuments.Add(document);
                requestNames.Add(FormRequests.RequestLabel(template != null ? template.askGroup : null, document.name, interview != null ? interview.askGroups : null));
                _caseForms.Add(DocumentForm.For(doc, agency, longestOrigin, inst.passportNation, _shows));
            }

        _papers = new CasePapers(_caseDocuments.Count);
        CaseClaim claim = AppLinks.Claim(inst);
        foreach (DocumentsView view in _views)
            if (view != null)
                view.SetCase(inst != null ? inst.documents : null, _caseForms, _papers, _compare, inst != null ? inst.PhotoLook : null, _art, claim, requestNames);

        if (_desk != null)
            _desk.BeginCase(_caseDocuments, _caseForms, inst != null ? inst.PhotoLook : null, _art);
        foreach (int i in CaseDocuments.ArrivalIndices(_caseDocuments))
            Receive(i);
        PapersChanged?.Invoke();
    }

    /// <summary>A document handed over through the wheel: a paper onto the desk, or straight to the PC where no desk is reachable.</summary>
    public void HandOver(int index)
    {
        if (_desk != null)
            _desk.HandOver(index);
        Receive(index);
    }

    /// <summary>The decision (<paramref name="decision"/>): the desk's papers leave with the traveller (inked with the verdict when nobody stamped them), and the Documents view empties.</summary>
    public void EndCase(DeskStamp decision)
    {
        if (_desk != null)
            _desk.EndCase(decision);
        _caseDocuments.Clear();
        _caseForms.Clear();
        _marks.Clear();
        _papers = new CasePapers(0);
        foreach (DocumentsView view in _views)
            if (view != null)
                view.Clear();
    }

    /// <summary>
    /// The day's introductions (the desk-first redesign, item 3): which
    /// fields the papers print (<paramref name="shows"/>: a form number and a
    /// category, Introductions.ShowsField; null: every field). A paper's copy
    /// reaches the PC only when it is scanned (items 5 and 11: the papers are
    /// checked at the desk; a scan, from the scanner's day, unlocks the PC's
    /// lookups for it).
    /// </summary>
    public void SetDay(Func<string, ClueCategory, bool> shows) => _shows = shows;

    /// <summary>Which fields today's papers print (SetDay; null: every field).</summary>
    private Func<string, ClueCategory, bool> _shows;

    /// <summary>A paper handed over: onto the desk (its copy reaches the PC when it is scanned), or scanned at once where no desk is reachable.</summary>
    private void Receive(int index)
    {
        if (_desk == null)
        {
            Scan(index);
            return;
        }
        if (!_papers.HandOver(index))
            return;
        foreach (DocumentsView view in _views)
            if (view != null)
                view.Refresh();
        PapersChanged?.Invoke();
    }

    /// <summary>The desk's scan finished: the copy reaches the PC (once per paper); an analysis pass (the Analysis Scanner, a scan by hand) then reads the scanned papers; a re-scan of a paper analysed already only says so on its strip (its marks stay).</summary>
    private void HandleScanFinished(int index, ScanPass pass)
    {
        Scan(index);
        if (pass == ScanPass.Analysis)
            Analyse(index);
        else if (pass == ScanPass.AlreadyAnalysed)
            foreach (DocumentsView view in _views)
                if (view != null)
                    view.MarkAlreadyAnalysed(index);
    }

    /// <summary>
    /// The analysis pass (the PC redesign SC4, SC5): the first contradicting
    /// pair among the scanned papers whose category the Deviation Report does
    /// not hold (PaperAnalysis.First) is marked on both scanned copies for the
    /// rest of the case, in every pane's Documents view, and paper
    /// <paramref name="index"/>'s strip says whether a pair was marked. Nothing
    /// is picked, logged, opened or switched: the player still compares the
    /// two fields.
    /// </summary>
    private void Analyse(int index)
    {
        AnalysisMark? mark = PaperAnalysis.First(_caseDocuments, _papers, _documented());
        if (mark.HasValue && !_marks.Contains(mark.Value))
            _marks.Add(mark.Value);
        foreach (DocumentsView view in _views)
            if (view != null)
            {
                if (mark.HasValue)
                    view.ShowMarks(_marks);
                view.MarkAnalysed(index, mark.HasValue);
            }
    }

    /// <summary>A paper's copy reaches the PC (the desk's ScanFinished, or a hand-over where no desk is wired): once per paper; its strip reads the time, and it joins search.</summary>
    private void Scan(int index)
    {
        if (!_papers.Scan(index))
            return;
        IndexPaper(index);
        foreach (DocumentsView view in _views)
            if (view != null)
            {
                view.MarkScanned(index);
                view.Refresh();
            }
        PapersChanged?.Invoke();
        Scanned?.Invoke(index);
    }

    /// <summary>Paper <paramref name="index"/> and its fields into search's case layer ("paper · label", every value as filled: always English).</summary>
    private void IndexPaper(int index)
    {
        if (_index == null || index < 0 || index >= _caseDocuments.Count)
            return;
        CaseDocument doc = _caseDocuments[index];
        IReadOnlyList<bool> hidden = index < _caseForms.Count && _caseForms[index] != null ? _caseForms[index].Data.FieldHidden : null;
        var fields = new List<(string label, string value)>();
        foreach (DocumentField field in doc.fields ?? new List<DocumentField>())
        {
            bool shown = field != null && (hidden == null || fields.Count >= hidden.Count || !hidden[fields.Count]);
            fields.Add(shown ? (field.label, field.category == ClueCategory.Photo ? UiText.Get("compare.photoShown") : field.value) : (null, null));
        }
        foreach (IndexEntry entry in IndexEntries.Paper(index, doc.name, fields, UiText.Get("search.title.row")))
            _index.Add(entry);
    }

    /// <summary>A paper lifted into the hand: announced.</summary>
    private void HandleExamined(int index) => Examined?.Invoke(index);

    /// <summary>A held paper's row picked at the desk: it goes into the compare (the same pick as its scanned copy's row).</summary>
    private void HandleFieldPicked(int index, DocumentRow row, ICompareHighlight highlight)
    {
        if (index < 0 || index >= _caseDocuments.Count || _compare == null)
            return;

        _compare.Select(EvidencePicks.ForField(index, row, _caseDocuments[index].name), highlight);
    }
}
