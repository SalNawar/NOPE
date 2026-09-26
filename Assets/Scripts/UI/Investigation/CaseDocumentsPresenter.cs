using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The current traveller's documents (the PC redesign RF1): one scanned-copy
/// window per document (hidden until it opens, cascading from the origin), the
/// documents as the desk and the interview read them, the hand-over and the
/// scan. With the desk, each document becomes a paper (those handed over on
/// arrival land at once) whose finished scan opens its window; without it, a
/// document's window opens when it is handed over. The first time a window
/// opens in a case, the document gets a desktop tile at the top of the grid.
/// A held paper's row picked at the desk goes into the compare as its scanned
/// copy's row would. It subscribes to the desk it was given (only a reachable
/// one) and unsubscribes from that same instance (audit R4-003). Plain C#;
/// InvestigationUIController owns it.
/// </summary>
public sealed class CaseDocumentsPresenter
{
    /// <summary>Where the document windows go (the façade's serialized parts and knobs).</summary>
    public struct Windows
    {
        /// <summary>The document window template (inactive).</summary>
        public DocumentWindowController template;

        /// <summary>The window layer the document windows open on.</summary>
        public RectTransform windowLayer;

        /// <summary>Where the first document window opens (desktop units from the centre).</summary>
        public Vector2 origin;

        /// <summary>The offset from one document window to the next.</summary>
        public Vector2 step;
    }

    private readonly Windows _windows;
    private readonly DeskController _desk;
    private readonly CompareController _compare;
    private readonly DesktopTiles _tiles;

    /// <summary>The current traveller's document windows, in paper order.</summary>
    private readonly List<DocumentWindowController> _docWindows = new();
    private readonly List<GameObject> _docIcons = new();

    /// <summary>The current traveller's documents in paper order (name, fields, hand-over, photo).</summary>
    private readonly List<CaseDocument> _caseDocuments = new();

    /// <summary>The form each of the current traveller's papers prints (redesign phase 4), in paper order.</summary>
    private readonly List<DocumentForm> _caseForms = new();

    /// <summary>Papers whose window already has a desktop tile this case.</summary>
    private readonly HashSet<int> _iconedDocuments = new();

    /// <summary>Character art: the passport photos on the papers and the scanned pages.</summary>
    private CharacterArt _art;

    /// <summary>The desk whose events this listens to (null while detached).</summary>
    private DeskController _listening;

    /// <summary>The document windows' parts, the desk (null when it is not reachable: documents then open at the hand-over), the compare and the desktop's tiles.</summary>
    public CaseDocumentsPresenter(Windows windows, DeskController reachableDesk, CompareController compare, DesktopTiles tiles)
    {
        _windows = windows;
        _desk = reachableDesk;
        _compare = compare;
        _tiles = tiles;
    }

    /// <summary>The current traveller's documents, in paper order.</summary>
    public IReadOnlyList<CaseDocument> Documents => _caseDocuments;

    /// <summary>Starts listening to the desk's finished scans and picked rows.</summary>
    public void Attach()
    {
        if (_desk == null)
            return;
        _listening = _desk;
        _listening.ScanFinished += OpenDocumentWindow;
        _listening.FieldPicked += HandleFieldPicked;
    }

    /// <summary>Stops listening (to the instance it attached to).</summary>
    public void Detach()
    {
        if (_listening == null)
            return;
        _listening.ScanFinished -= OpenDocumentWindow;
        _listening.FieldPicked -= HandleFieldPicked;
        _listening = null;
    }

    /// <summary>Sets the character art the passport photos are drawn with.</summary>
    public void SetCharacterArt(CharacterArt art) => _art = art;

    /// <summary>Removes the last traveller's document windows and tiles (their windows are closed first by the façade).</summary>
    public void Clear()
    {
        foreach (DocumentWindowController w in _docWindows)
            if (w != null)
                Object.Destroy(w.gameObject);
        _docWindows.Clear();

        foreach (GameObject ic in _docIcons)
            if (ic != null)
                Object.Destroy(ic);
        _docIcons.Clear();
        _iconedDocuments.Clear();
        _caseDocuments.Clear();
        _caseForms.Clear();
    }

    /// <summary>
    /// Presents the traveller's documents: handed over, never taken: those
    /// marked "on arrival" when the traveller steps up, the others through the
    /// traveller wheel. With the desk, each becomes a paper whose scan opens
    /// its window; without it, the window opens at the hand-over. Windows
    /// spawn hidden. Each paper prints its document's form, headed with
    /// <paramref name="agency"/>'s name and programme (redesign phase 4), and
    /// its window draws that same form (phase 5).
    /// </summary>
    public void Present(CaseInstance inst, AgencyContent agency)
    {
        if (inst != null)
        {
            int i = 0;
            foreach (DocumentInstance doc in inst.documents)
            {
                DocumentWindowController clone = Object.Instantiate(_windows.template, _windows.windowLayer);
                clone.gameObject.SetActive(false);
                if (clone.transform is RectTransform rt)
                    rt.anchoredPosition = _windows.origin + i * _windows.step;
                DocumentForm form = DocumentForm.For(doc, agency);
                clone.SetDocument(doc, i, form, _compare, inst.look, _art);
                _docWindows.Add(clone);
                _caseDocuments.Add(new CaseDocument
                {
                    name = doc != null ? doc.DisplayName : UiText.Get("document.untitled"),
                    fields = doc != null ? doc.fields : null,
                    handOver = doc != null && doc.template != null ? doc.template.handOver : DocumentHandOver.OnRequest,
                    showsPhoto = doc != null && doc.template != null && doc.template.showsPhoto
                });
                _caseForms.Add(form);
                i++;
            }
        }

        if (_desk != null)
        {
            _desk.BeginCase(_caseDocuments, _caseForms, inst != null ? inst.look : null, _art);
        }
        else
        {
            foreach (int i in CaseDocuments.ArrivalIndices(_caseDocuments))
                OpenDocumentWindow(i);
        }
    }

    /// <summary>A document handed over through the wheel: a paper onto the desk, or straight to its window where no desk is reachable.</summary>
    public void HandOver(int index)
    {
        if (_desk != null)
            _desk.HandOver(index);
        else
            OpenDocumentWindow(index);
    }

    /// <summary>The decision: the desk's papers leave with the traveller.</summary>
    public void EndCase()
    {
        if (_desk != null)
            _desk.EndCase();
    }

    /// <summary>
    /// Opens a paper's scanned window and raises it (the desk's ScanFinished,
    /// or a hand-over where no desk is wired); its strip reads the time the
    /// copy arrived. The first time it opens this case, the paper also gets a
    /// desktop tile at the top of the grid, which reopens the window after it
    /// is closed.
    /// </summary>
    private void OpenDocumentWindow(int index)
    {
        DocumentWindowController window = index >= 0 && index < _docWindows.Count ? _docWindows[index] : null;
        if (window == null || !window.TryGetComponent(out DesktopWindow chrome))
            return;

        window.MarkScanned();
        chrome.Open();
        if (_iconedDocuments.Add(index))
        {
            GameObject tile = _tiles.Add(_caseDocuments[index].name, chrome, true);
            if (tile != null)
                _docIcons.Add(tile);
        }
    }

    /// <summary>A held paper's row picked at the desk: it goes into the compare (the same pick as its scanned copy's row).</summary>
    private void HandleFieldPicked(int index, DocumentRow row, ICompareHighlight highlight)
    {
        if (index < 0 || index >= _caseDocuments.Count || _compare == null)
            return;

        _compare.Select(EvidencePicks.ForField(index, row, _caseDocuments[index].name), highlight);
    }
}
