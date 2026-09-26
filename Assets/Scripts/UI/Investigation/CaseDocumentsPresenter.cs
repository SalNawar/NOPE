using System;
using System.Collections.Generic;

/// <summary>
/// The current traveller's documents (the PC redesign RF1, AP6): the papers
/// the Investigation app's Documents tab shows (a chip each; a scanned one's
/// copy), where each paper is (CasePapers: not handed over, on the desk,
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
    private readonly DocumentsView _view;
    private readonly DeskController _desk;
    private readonly CompareController _compare;

    /// <summary>The current traveller's documents in paper order (name, fields, hand-over, photo).</summary>
    private readonly List<CaseDocument> _caseDocuments = new List<CaseDocument>();

    /// <summary>Where each of the current traveller's papers is.</summary>
    private CasePapers _papers = new CasePapers(0);

    /// <summary>Character art: the passport photos on the papers and the scanned copies.</summary>
    private CharacterArt _art;

    /// <summary>The desk whose events this listens to (null while detached).</summary>
    private DeskController _listening;

    /// <summary>The app's Documents view, the desk (null when it is not reachable: documents then reach the PC at the hand-over) and the compare.</summary>
    public CaseDocumentsPresenter(DocumentsView view, DeskController reachableDesk, CompareController compare)
    {
        _view = view;
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
    /// to the PC; without it, a paper reaches the PC at the hand-over.
    /// </summary>
    public void Present(CaseInstance inst)
    {
        _caseDocuments.Clear();
        if (inst != null)
            foreach (DocumentInstance doc in inst.documents)
                _caseDocuments.Add(new CaseDocument
                {
                    name = doc != null && doc.template != null ? doc.template.displayName : UiText.Get("document.untitled"),
                    fields = doc != null ? doc.fields : null,
                    handOver = doc != null && doc.template != null ? doc.template.handOver : DocumentHandOver.OnRequest,
                    showsPhoto = doc != null && doc.template != null && doc.template.showsPhoto
                });

        _papers = new CasePapers(_caseDocuments.Count);
        if (_view != null)
            _view.SetCase(inst != null ? inst.documents : null, _papers, _compare, inst != null ? inst.look : null, _art);

        if (_desk != null)
            _desk.BeginCase(_caseDocuments, inst != null ? inst.look : null, _art);
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

    /// <summary>The decision: the desk's papers leave with the traveller, and the Documents view empties.</summary>
    public void EndCase()
    {
        if (_desk != null)
            _desk.EndCase();
        _caseDocuments.Clear();
        _papers = new CasePapers(0);
        if (_view != null)
            _view.Clear();
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
        if (_view != null)
            _view.Refresh();
        PapersChanged?.Invoke();
    }

    /// <summary>A paper's copy reaches the PC (the desk's ScanFinished, or a hand-over where no desk is wired): once per paper.</summary>
    private void Scan(int index)
    {
        if (!_papers.Scan(index))
            return;
        if (_view != null)
            _view.Refresh();
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
