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
/// what that shows: ScanArrival); nothing here opens a window. A held
/// paper's row picked at the desk goes into the compare as its scanned
/// copy's row would. It subscribes to the desk it was given (only a
/// reachable one) and unsubscribes from that same instance (audit R4-003).
/// Plain C#; InvestigationUIController owns it.
/// </summary>
public sealed class CaseDocumentsPresenter
{
    private readonly IReadOnlyList<DocumentsView> _views;
    private readonly DeskController _desk;
    private readonly CompareController _compare;

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

    /// <summary>The app's Documents views (one per pane; null entries are skipped), the desk (null when it is not reachable: documents then reach the PC at the hand-over) and the compare.</summary>
    public CaseDocumentsPresenter(IReadOnlyList<DocumentsView> views, DeskController reachableDesk, CompareController compare)
    {
        _views = views ?? Array.Empty<DocumentsView>();
        _desk = reachableDesk;
        _compare = compare;
    }

    /// <summary>A paper reached the PC (its index): scanned at the desk, or handed over where no desk is wired.</summary>
    public event Action<int> Scanned;

    /// <summary>A paper was handed over or scanned (the counters change).</summary>
    public event Action PapersChanged;

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
        _listening.ScanFinished += Scan;
        _listening.FieldPicked += HandleFieldPicked;
    }

    /// <summary>Stops listening (to the instance it attached to).</summary>
    public void Detach()
    {
        if (_listening == null)
            return;
        _listening.ScanFinished -= Scan;
        _listening.FieldPicked -= HandleFieldPicked;
        _listening = null;
    }

    /// <summary>Sets the character art the passport photos are drawn with.</summary>
    public void SetCharacterArt(CharacterArt art) => _art = art;

    /// <summary>
    /// Presents the traveller's documents: handed over, never taken: those
    /// marked "on arrival" when the traveller steps up, the others through the
    /// traveller wheel. The Documents view gets a chip and a (hidden) copy per
    /// paper. With the desk, each becomes a paper whose scan brings its copy
    /// to the PC; without it, a paper reaches the PC at the hand-over. Each
    /// paper prints its document's form, headed with <paramref name="agency"/>'s
    /// name and programme (redesign phase 4), and its copy draws that same
    /// form (phase 5).
    /// </summary>
    public void Present(CaseInstance inst, AgencyContent agency)
    {
        _caseDocuments.Clear();
        _caseForms.Clear();
        if (inst != null)
            foreach (DocumentInstance doc in inst.documents)
            {
                _caseDocuments.Add(new CaseDocument
                {
                    name = doc != null ? doc.DisplayName : UiText.Get("document.untitled"),
                    fields = doc != null ? doc.fields : null,
                    handOver = doc != null && doc.template != null ? doc.template.handOver : DocumentHandOver.OnRequest,
                    showsPhoto = doc != null && doc.template != null && doc.template.showsPhoto
                });
                _caseForms.Add(DocumentForm.For(doc, agency));
            }

        _papers = new CasePapers(_caseDocuments.Count);
        CaseClaim claim = AppLinks.Claim(inst);
        foreach (DocumentsView view in _views)
            if (view != null)
                view.SetCase(inst != null ? inst.documents : null, _caseForms, _papers, _compare, inst != null ? inst.look : null, _art, claim);

        if (_desk != null)
            _desk.BeginCase(_caseDocuments, _caseForms, inst != null ? inst.look : null, _art);
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

    /// <summary>The decision (<paramref name="accepted"/>): the desk's papers leave with the traveller, inked with the verdict, and the Documents view empties.</summary>
    public void EndCase(bool accepted)
    {
        if (_desk != null)
            _desk.EndCase(accepted);
        _caseDocuments.Clear();
        _caseForms.Clear();
        _papers = new CasePapers(0);
        foreach (DocumentsView view in _views)
            if (view != null)
                view.Clear();
    }

    /// <summary>A paper handed over: onto the desk (its scan comes later), or scanned at once where no desk is reachable.</summary>
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

    /// <summary>A paper's copy reaches the PC (the desk's ScanFinished, or a hand-over where no desk is wired): once per paper; its strip reads the time.</summary>
    private void Scan(int index)
    {
        if (!_papers.Scan(index))
            return;
        foreach (DocumentsView view in _views)
            if (view != null)
            {
                view.MarkScanned(index);
                view.Refresh();
            }
        PapersChanged?.Invoke();
        Scanned?.Invoke(index);
    }

    /// <summary>A held paper's row picked at the desk: it goes into the compare (the same pick as its scanned copy's row).</summary>
    private void HandleFieldPicked(int index, DocumentRow row, ICompareHighlight highlight)
    {
        if (index < 0 || index >= _caseDocuments.Count || _compare == null)
            return;

        _compare.Select(EvidencePicks.ForField(index, row, _caseDocuments[index].name), highlight);
    }
}
