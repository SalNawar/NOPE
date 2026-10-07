using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// The rulebook on the desk, Papers, Please's booklet (Saleh 2026-10-06: "the
/// rulebook is a booklet on the desk: drag it, click its tabs to turn pages;
/// it is comparable in inspect mode"). It lies beside the mat (the office
/// binder places it: Place) and moves by left-drag like a document
/// (DeskDraggable; dropped on the counter it slides back: it is the
/// office's, not the traveller's). It lies in the papers' stack
/// (DeskController: SetLift lifts its Booklet a stack step at a time, so it
/// lies over or under each paper as they were last touched and never at the
/// blotter's own height; Saleh 2026-10-06: "documents on desk like the folder
/// with the rules are clipping with the desk"). Since run 7 it is Saleh's Canva
/// folder (ArtSlots.RulebookFolder: open, its page the right sheet, each tab
/// its own cut-out, ArtSlots.RulebookTab, shown only while its page is
/// available). The tabs on its top edge turn its pages
/// (ShowPage): RULES prints today's directives (the day's Directive Memo,
/// the PC's Rules), one row each; PAPERS lists the papers the traveller has
/// not handed over (Track C's MissingPapers: the day's papers menu, the same
/// list as the PC's Papers menu), and a click on one flags it missing
/// (PaperFlagged; Saleh: asking for a document must be flagged first), which
/// unlocks "Hand me your ..." on the wheel; a flagged or not-carried paper
/// says so. A click on a rules row raises RowClicked with the directive's
/// index in the day's list (the PC's EntryKeys.Rule index) and the rule: in
/// inspect mode (SetInspecting tints the rows as comparable) DeskInspect holds
/// it on the workbench (MatchBoard.PickRule), so it is judged against a value
/// like the PC's memo row; outside inspect mode a row is only read.
/// The third tab, GUIDE, is the help guide (Saleh 2026-10-06: "a help guide
/// that gets expanded every day like Papers, Please"): BASICS, then one
/// sheet per introduced rule or paper (SetGuide, from the guide director:
/// Guide.PagesOn), turned with PREV and NEXT, today's new ones marked NEW;
/// the tab wears a NEW badge while a page added today is unread
/// (SetGuideBadge), and each sheet shown is reported read (GuideRead).
/// The fourth tab, SEALS, is the Seal Register at the desk (from the day the
/// register is introduced, Feature.Book(Seal): day 4): each issuing office's
/// seal (its outline in its ink, its legend) and name, one row each
/// (ShowSeals); in inspect mode a row is comparable (SealClicked: DeskInspect
/// holds the office's true seal on the workbench, as the PC's register does).
/// Each new day opens on RULES. It shows from the day the rulebook is
/// introduced (Feature.Rulebook). Build Office UI builds the folder, its
/// tabs, pages and rows (placed on the art's measured sheet and lines) (a fixed number: more directives than rows print
/// only the first ones).
/// </summary>
public sealed class DeskRulebook : MonoBehaviour
{
    /// <summary>The booklet's visible part and its click boxes, lifted off the desk plane by the stack (SetLift); the root stays on the plane, where the drag moves it.</summary>
    [SerializeField] private Transform booklet;

    /// <summary>The folder's edge (a child of its card quad, in its unit space; a vertex-coloured material): its board and its pages seen from the side (PaperEdge; Saleh's 1007d playtest).</summary>
    [SerializeField] private MeshFilter edge;

    /// <summary>The rules page's title ("TODAY'S RULES").</summary>
    [SerializeField] private TMP_Text title;

    /// <summary>The rules' rows, top first: each a click box (Clickable, a collider) with its text as its child.</summary>
    [SerializeField] private Clickable[] rows = Array.Empty<Clickable>();

    /// <summary>The line printed when the day has no directive.</summary>
    [SerializeField] private TMP_Text none;

    /// <summary>The papers page's heading ("PAPERS NOT HANDED OVER").</summary>
    [SerializeField] private TMP_Text papersTitle;

    /// <summary>The papers rows, top first: each a click box with its text as its child (a paper not handed over; a click flags it missing).</summary>
    [SerializeField] private Clickable[] paperRows = Array.Empty<Clickable>();

    /// <summary>The line printed when no paper is left to flag.</summary>
    [SerializeField] private TMP_Text papersNone;

    /// <summary>The booklet's own click box (its hover outline; the drag's proxy: a drag from anywhere on it moves it).</summary>
    [SerializeField] private Clickable card;

    /// <summary>The RULES page (its title, rows and none line).</summary>
    [SerializeField] private GameObject rulesPage;

    /// <summary>The PAPERS page (its heading, rows and none line).</summary>
    [SerializeField] private GameObject papersPage;

    /// <summary>The GUIDE page (its sheet's title, body, number, NEW mark and PREV / NEXT).</summary>
    [SerializeField] private GameObject guidePage;

    /// <summary>The guide sheet's heading.</summary>
    [SerializeField] private TMP_Text guideTitle;

    /// <summary>The guide sheet's text (BASICS' lines, or a page's check, against and fault).</summary>
    [SerializeField] private TMP_Text guideBody;

    /// <summary>The sheet's number of the sheets ("2 / 6").</summary>
    [SerializeField] private TMP_Text guideNumber;

    /// <summary>The NEW mark on a sheet added today.</summary>
    [SerializeField] private GameObject guideNew;

    /// <summary>Turns to the previous guide sheet.</summary>
    [SerializeField] private Clickable guidePrev;

    /// <summary>Turns to the next guide sheet.</summary>
    [SerializeField] private Clickable guideNext;

    /// <summary>The NEW badge on the GUIDE tab (a page added today is unread).</summary>
    [SerializeField] private GameObject guideBadge;

    /// <summary>The SEALS page (the Seal Register: its title and a row per office).</summary>
    [SerializeField] private GameObject sealsPage;

    /// <summary>The seal rows, top first: each a click box with its mark, legend and office name as children.</summary>
    [SerializeField] private Clickable[] sealRows = Array.Empty<Clickable>();

    /// <summary>The seal rows' marks (a quad drawn with the seal's outline in its ink), as sealRows.</summary>
    [SerializeField] private Renderer[] sealMarks = Array.Empty<Renderer>();

    /// <summary>The seal rows' legends (printed inside the mark, in its ink), as sealRows.</summary>
    [SerializeField] private TMP_Text[] sealLegends = Array.Empty<TMP_Text>();

    /// <summary>The seal rows' office names, as sealRows.</summary>
    [SerializeField] private TMP_Text[] sealNames = Array.Empty<TMP_Text>();

    /// <summary>The tabs on the top edge, RULES, PAPERS, GUIDE, SEALS: a click turns to its page (SEALS only once introduced).</summary>
    [SerializeField] private Clickable[] tabs = Array.Empty<Clickable>();

    /// <summary>The tabs' plates, as tabs (the open page's plate is the paper's, the other's darker).</summary>
    [SerializeField] private Renderer[] tabPlates = Array.Empty<Renderer>();

    /// <summary>The booklet's drag (left-drag moves it on the desk).</summary>
    [SerializeField] private DeskDraggable drag;

    /// <summary>The desk plane it moves on.</summary>
    [SerializeField] private DeskSurface surface;

    /// <summary>The counter (optional): the booklet dropped there goes back to where it was picked up.</summary>
    [SerializeField] private DeskCounter counter;

    /// <summary>The rows' ink, and their ink in inspect mode (comparable).</summary>
    [SerializeField] private Color rowInk = new Color(0.13f, 0.12f, 0.15f), inspectInk = new Color(0.62f, 0.38f, 0.02f);

    /// <summary>The open tab's plate and the other tab's.</summary>
    [SerializeField] private Color openTab = new Color(0.93f, 0.9f, 0.8f), closedTab = new Color(0.66f, 0.62f, 0.52f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

    private readonly List<int> _indices = new List<int>();
    private IReadOnlyList<TravelRuleSO> _rules = Array.Empty<TravelRuleSO>();
    private readonly List<string> _paperIds = new List<string>();
    private MaterialPropertyBlock _block;
    private bool _inspecting;
    private IReadOnlyList<GuideSheet> _sheets = Array.Empty<GuideSheet>();
    private int _sheet;

    /// <summary>The GUIDE page's number.</summary>
    public const int GuidePageIndex = 2;

    /// <summary>The SEALS page's number.</summary>
    public const int SealsPageIndex = 3;

    private bool _sealsIntroduced;
    private int _sealCount;

    /// <summary>Raised when a seal row is clicked: its index in ShowSeals' list (DeskInspect picks it in inspect mode).</summary>
    public event Action<int> SealClicked;

    /// <summary>Raised when a guide sheet shows on the open GUIDE page: its id (GuideSheet.Id; BASICS' is empty).</summary>
    public event Action<string> GuideRead;

    /// <summary>The guide sheet shown (its index in SetGuide's list).</summary>
    public int GuideSheetIndex => _sheet;

    /// <summary>The guide sheets (SetGuide).</summary>
    public IReadOnlyList<GuideSheet> GuideSheets => _sheets;

    /// <summary>True while the GUIDE tab wears its NEW badge.</summary>
    public bool GuideBadge => guideBadge != null && guideBadge.activeSelf;

    /// <summary>Raised when a rules row is clicked in inspect mode: the directive's index in the day's list and the rule.</summary>
    public event Action<int, TravelRuleSO> RowClicked;

    /// <summary>Raised when a paper not handed over is clicked to flag it missing: its request's id (a FormRequest.Id).</summary>
    public event Action<string> PaperFlagged;

    /// <summary>Every click of the booklet (the rows, the papers' rows, the tabs, the booklet itself): the booth makes them live with the props.</summary>
    public IReadOnlyList<Clickable> Clicks => rows.Concat(paperRows).Concat(sealRows).Concat(tabs).Append(guidePrev).Append(guideNext).Append(card).Where(c => c != null).ToArray();

    /// <summary>The booklet's drag (DeskController lifts it above the stack while it runs).</summary>
    public DeskDraggable Drag => drag;

    /// <summary>The page shown: 0 RULES, 1 PAPERS, 2 GUIDE, 3 SEALS.</summary>
    public int Page { get; private set; }

    private void Awake()
    {
        if (title != null)
            title.text = UiText.Get("desk.rulebook.title");
        if (none != null)
            none.text = UiText.Get("desk.rulebook.none");
        if (papersTitle != null)
            papersTitle.text = UiText.Get("desk.rulebook.papers");
        if (papersNone != null)
            papersNone.text = UiText.Get("desk.rulebook.papersNone");
        for (int i = 0; i < rows.Length; i++)
        {
            int row = i;
            if (rows[i] != null)
                rows[i].onClick.AddListener(() => Click(row));
        }
        for (int i = 0; i < paperRows.Length; i++)
        {
            int row = i;
            if (paperRows[i] != null)
                paperRows[i].onClick.AddListener(() => FlagPaper(row));
        }
        for (int i = 0; i < tabs.Length; i++)
        {
            int page = i;
            if (tabs[i] != null)
                tabs[i].onClick.AddListener(() => ShowPage(page));
        }
        for (int i = 0; i < sealRows.Length; i++)
        {
            int row = i;
            if (sealRows[i] != null)
                sealRows[i].onClick.AddListener(() =>
                {
                    if (row < _sealCount)
                        SealClicked?.Invoke(row);
                });
        }
        SetSealsIntroduced(false);
        if (guidePrev != null)
            guidePrev.onClick.AddListener(() => OpenGuide(_sheet - 1));
        if (guideNext != null)
            guideNext.onClick.AddListener(() => OpenGuide(_sheet + 1));
        if (drag != null)
        {
            drag.Init(surface);
            drag.DragEnded += Dropped;
        }
        Show(_rules);
        ShowMissing(MissingPapers.None, null);
    }

    private void OnDestroy()
    {
        if (drag != null)
            drag.DragEnded -= Dropped;
        if (_edgeMesh != null)
            Destroy(_edgeMesh);
    }

    /// <summary>Lays the booklet on the desk at <paramref name="at"/> (on the desk plane), turned to <paramref name="rotation"/> (the office binder).</summary>
    public void Place(Vector3 at, Quaternion rotation) => transform.SetPositionAndRotation(at, rotation);

    /// <summary>The folder's thickness (metres; DeskConfigSO.folderThickness through ShowEdge): its room in the papers' stack.</summary>
    public float Thickness { get; private set; } = 0.0025f;

    /// <summary>The folder's body on its card (shares of the card, centre and size): the art's opaque folder, its tabs above it left out.</summary>
    private const float BodyCentreX = 0.005f, BodyCentreY = -0.04f, BodyWidth = 0.93f, BodyHeight = 0.88f;

    /// <summary>The board's and the pages' colours on the folder's edge.</summary>
    private static readonly Color BoardTone = new Color(0.55f, 0.43f, 0.27f, 1f), PageLight = new Color(0.93f, 0.9f, 0.82f, 1f), PageDark = new Color(0.8f, 0.76f, 0.68f, 1f);

    private Mesh _edgeMesh;

    /// <summary>Builds the folder's edge <paramref name="thickness"/> metres deep (PaperEdge.Folder: its pages' edges over its board) under its body (DeskController, at load).</summary>
    public void ShowEdge(float thickness)
    {
        Thickness = thickness;
        if (edge == null)
            return;
        var outline = PaperEdge.Rectangle(BodyWidth, BodyHeight).ConvertAll(p => (p.x + BodyCentreX, p.y + BodyCentreY));
        var vertices = new List<(float x, float y, float z)>();
        var tones = new List<EdgeTone>();
        var triangles = new List<int>();
        PaperEdge.Walls(outline, PaperEdge.Bands(PaperKind.Folder, thickness), vertices, tones, triangles);
        _edgeMesh ??= new Mesh { name = "FolderEdge" };
        _edgeMesh.Clear();
        _edgeMesh.SetVertices(vertices.ConvertAll(v => new Vector3(v.x, v.y, v.z)));
        _edgeMesh.SetColors(tones.ConvertAll(t => t == EdgeTone.Cover ? BoardTone : t == EdgeTone.PageDark ? PageDark : PageLight));
        _edgeMesh.SetTriangles(triangles, 0);
        _edgeMesh.RecalculateBounds();
        edge.sharedMesh = _edgeMesh;
    }

    /// <summary>Lifts the booklet <paramref name="height"/> metres off the desk plane (its place in the papers' stack, or the drag's lift: DeskController).</summary>
    public void SetLift(float height)
    {
        if (booklet != null)
            booklet.localPosition = new Vector3(0f, height, 0f);
    }

    /// <summary>Turns to page <paramref name="page"/> (0 RULES, 1 PAPERS, 2 GUIDE, 3 SEALS once introduced): its rows show, the others' hide; its tab looks open. The GUIDE shows its sheet (read).</summary>
    public void ShowPage(int page)
    {
        Page = Mathf.Clamp(page, 0, _sealsIntroduced ? SealsPageIndex : GuidePageIndex);
        if (sealsPage != null)
            sealsPage.SetActive(Page == SealsPageIndex);
        if (rulesPage != null)
            rulesPage.SetActive(Page == 0);
        if (papersPage != null)
            papersPage.SetActive(Page == 1);
        if (guidePage != null)
            guidePage.SetActive(Page == GuidePageIndex);
        if (Page == GuidePageIndex)
            ShowSheet();
        _block ??= new MaterialPropertyBlock();
        for (int i = 0; i < tabPlates.Length; i++)
        {
            if (tabPlates[i] == null)
                continue;
            tabPlates[i].GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, i == Page ? openTab : closedTab);
            tabPlates[i].SetPropertyBlock(_block);
        }
    }

    /// <summary>Inspect mode on or off (BoothCoordinator): the rules' rows and the seals' names print in the comparable ink, and a click on one compares.</summary>
    public void SetInspecting(bool inspecting)
    {
        _inspecting = inspecting;
        foreach (Clickable row in rows)
            if (row != null && row.GetComponentInChildren<TMP_Text>(true) is TMP_Text text)
                text.color = inspecting ? inspectInk : rowInk;
        foreach (TMP_Text name in sealNames)
            if (name != null)
                name.color = inspecting ? inspectInk : rowInk;
    }

    /// <summary>The SEALS tab shows (and its page can open) from the day the Seal Register is introduced (DeskInspect: Feature.Book(Seal)); before it the tab is hidden.</summary>
    public void SetSealsIntroduced(bool introduced)
    {
        _sealsIntroduced = introduced;
        if (tabs.Length > SealsPageIndex && tabs[SealsPageIndex] != null)
            tabs[SealsPageIndex].gameObject.SetActive(introduced);
        if (tabPlates.Length > SealsPageIndex && tabPlates[SealsPageIndex] != null)
            tabPlates[SealsPageIndex].gameObject.SetActive(introduced);
        if (!introduced && Page == SealsPageIndex)
            ShowPage(0);
    }

    /// <summary>Prints the Seal Register: each office's seal (its outline texture and ink, its legend) and name, one row each (more offices than rows print only the first).</summary>
    public void ShowSeals(IReadOnlyList<(string name, Seal seal)> offices)
    {
        offices ??= Array.Empty<(string, Seal)>();
        _sealCount = Mathf.Min(offices.Count, sealRows.Length);
        _block ??= new MaterialPropertyBlock();
        for (int r = 0; r < sealRows.Length; r++)
        {
            bool on = r < _sealCount;
            if (sealRows[r] != null)
                sealRows[r].gameObject.SetActive(on);
            if (!on)
                continue;
            Color ink = SealArt.Ink(offices[r].seal.Ink);
            if (r < sealMarks.Length && sealMarks[r] != null)
            {
                sealMarks[r].GetPropertyBlock(_block);
                _block.SetTexture(BaseMapId, SealArt.Texture(offices[r].seal.Shape));
                _block.SetColor(BaseColorId, ink);
                sealMarks[r].SetPropertyBlock(_block);
            }
            if (r < sealLegends.Length && sealLegends[r] != null)
            {
                sealLegends[r].text = offices[r].seal.Legend;
                sealLegends[r].color = ink;
            }
            if (r < sealNames.Length && sealNames[r] != null)
                sealNames[r].text = offices[r].name;
        }
    }

    /// <summary>The world bounds of seal row <paramref name="index"/> (a match line meets it there); false when it prints no row, its page is closed or the booklet is hidden.</summary>
    public bool TrySealBounds(int index, out Bounds bounds)
    {
        bounds = default;
        if (!isActiveAndEnabled || index < 0 || index >= _sealCount || index >= sealRows.Length || sealRows[index] == null ||
            !sealRows[index].gameObject.activeInHierarchy || !sealRows[index].TryGetComponent(out Collider box))
            return false;
        bounds = box.bounds;
        return true;
    }

    /// <summary>Lists the traveller's papers not handed over (<paramref name="missing"/>'s open requests over <paramref name="papers"/>), one per row, each with where it stands: to flag, flagged (ask for it on the wheel) or not carried; nothing between travellers.</summary>
    public void ShowMissing(MissingPapers missing, CasePapers papers)
    {
        List<FormRequest> open = (missing ?? MissingPapers.None).Open(papers);
        _paperIds.Clear();
        for (int r = 0; r < paperRows.Length; r++)
        {
            if (paperRows[r] == null)
                continue;
            bool on = r < open.Count;
            paperRows[r].gameObject.SetActive(on);
            if (!on)
                continue;
            _paperIds.Add(open[r].Id);
            string key = missing.State(open[r].Id) switch
            {
                MissingPaperState.Flagged => "desk.rulebook.paperFlagged",
                MissingPaperState.NotCarried => "desk.rulebook.paperNotCarried",
                _ => "desk.rulebook.paperFlag"
            };
            if (paperRows[r].GetComponentInChildren<TMP_Text>(true) is TMP_Text text)
                text.text = UiText.Format(key, open[r].Label);
        }
        if (papersNone != null)
            papersNone.gameObject.SetActive(_paperIds.Count == 0);
    }

    /// <summary>Prints today's directives (<paramref name="rules"/>, the day's list; a rule with no summary prints no row), one per row, and opens on RULES.</summary>
    public void Show(IReadOnlyList<TravelRuleSO> rules)
    {
        _rules = rules ?? Array.Empty<TravelRuleSO>();
        _indices.Clear();
        for (int i = 0; i < _rules.Count; i++)
            if (_rules[i] != null && !string.IsNullOrWhiteSpace(_rules[i].Summary()))
                _indices.Add(i);
        for (int r = 0; r < rows.Length; r++)
        {
            if (rows[r] == null)
                continue;
            bool on = r < _indices.Count;
            rows[r].gameObject.SetActive(on);
            if (on && rows[r].GetComponentInChildren<TMP_Text>(true) is TMP_Text text)
                text.text = $"{r + 1}. {_rules[_indices[r]].Summary()}";
        }
        if (none != null)
            none.gameObject.SetActive(_indices.Count == 0);
        SetInspecting(_inspecting);
        ShowPage(0);
    }

    /// <summary>The GUIDE's sheets (BASICS first, then the day's pages: the guide director); the sheet open stays open (by id; the first when gone).</summary>
    public void SetGuide(IReadOnlyList<GuideSheet> sheets)
    {
        string open = _sheet < _sheets.Count ? _sheets[_sheet].Id : null;
        _sheets = sheets ?? Array.Empty<GuideSheet>();
        int keep = -1;
        for (int i = 0; i < _sheets.Count && keep < 0; i++)
            if (_sheets[i].Id == open)
                keep = i;
        _sheet = Mathf.Max(0, keep);
        if (Page == GuidePageIndex)
            ShowSheet();
    }

    /// <summary>Turns to the GUIDE at sheet <paramref name="index"/> (clamped to the sheets).</summary>
    public void OpenGuide(int index)
    {
        _sheet = Mathf.Clamp(index, 0, Mathf.Max(0, _sheets.Count - 1));
        ShowPage(GuidePageIndex);
    }

    /// <summary>Turns to the GUIDE at the sheet <paramref name="id"/> (the first when none has it).</summary>
    public void OpenGuide(string id)
    {
        int index = 0;
        for (int i = 0; i < _sheets.Count; i++)
            if (_sheets[i].Id == id)
                index = i;
        OpenGuide(index);
    }

    /// <summary>The GUIDE tab's NEW badge on or off.</summary>
    public void SetGuideBadge(bool on)
    {
        if (guideBadge != null)
            guideBadge.SetActive(on);
    }

    /// <summary>Prints the open guide sheet, its number, its NEW mark and whether PREV and NEXT turn, and reports it read.</summary>
    private void ShowSheet()
    {
        bool any = _sheets.Count > 0;
        GuideSheet sheet = any ? _sheets[Mathf.Clamp(_sheet, 0, _sheets.Count - 1)] : default;
        if (guideTitle != null)
            guideTitle.text = sheet.Title ?? string.Empty;
        if (guideBody != null)
            guideBody.text = sheet.Body ?? string.Empty;
        if (guideNumber != null)
            guideNumber.text = any ? UiText.Format("desk.guide.page", _sheet + 1, _sheets.Count) : string.Empty;
        if (guideNew != null)
            guideNew.SetActive(any && sheet.IsNew);
        if (guidePrev != null)
            guidePrev.gameObject.SetActive(_sheet > 0);
        if (guideNext != null)
            guideNext.gameObject.SetActive(_sheet < _sheets.Count - 1);
        if (any)
            GuideRead?.Invoke(sheet.Id);
    }

    /// <summary>The world bounds of the row printing directive <paramref name="ruleIndex"/> (a match line meets it there); false when it prints no row, its page is closed or the booklet is hidden.</summary>
    public bool TryRowBounds(int ruleIndex, out Bounds bounds)
    {
        bounds = default;
        int row = _indices.IndexOf(ruleIndex);
        if (!isActiveAndEnabled || row < 0 || row >= rows.Length || rows[row] == null || !rows[row].gameObject.activeInHierarchy ||
            !rows[row].TryGetComponent(out Collider box))
            return false;
        bounds = box.bounds;
        return true;
    }

    /// <summary>The booklet dropped: on the counter it goes back to where it was picked up (it stays on the clerk's side).</summary>
    private void Dropped(DeskDraggable dragged, Vector3 released)
    {
        if (counter != null && counter.Contains(released))
            transform.position = dragged.PickUpPosition;
    }

    /// <summary>A paper row clicked: its paper is flagged missing (the controller ignores one flagged already).</summary>
    private void FlagPaper(int row)
    {
        if (row < _paperIds.Count)
            PaperFlagged?.Invoke(_paperIds[row]);
    }

    private void Click(int row)
    {
        if (row < _indices.Count)
            RowClicked?.Invoke(_indices[row], _rules[_indices[row]]);
    }
}

/// <summary>One sheet of the rulebook's GUIDE: its id (a GuidePage's; BASICS' is empty), heading, text and whether it was added today.</summary>
public readonly struct GuideSheet
{
    /// <summary>A sheet.</summary>
    public GuideSheet(string id, string title, string body, bool isNew)
    {
        Id = id ?? string.Empty;
        Title = title;
        Body = body;
        IsNew = isNew;
    }

    /// <summary>The page's id (empty for BASICS).</summary>
    public string Id { get; }

    /// <summary>The heading.</summary>
    public string Title { get; }

    /// <summary>The text.</summary>
    public string Body { get; }

    /// <summary>True when added today (its NEW mark).</summary>
    public bool IsNew { get; }
}
