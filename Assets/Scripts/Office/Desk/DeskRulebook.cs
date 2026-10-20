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
/// P0-A carve (docs/pending/CARVE.md): the folder's motion is in
/// DeskRulebook.Motion.cs. Owner: DOC-LOOK.
/// DOC-LOOK (1019a, Saleh's D3): the folder opens and closes (FolderCover):
/// it arrives shut each day (Show), its front board over the right sheet and
/// only the tabs over that sheet showing; a click on it or on a tab, a page
/// turn (ShowPage) or the guide opening a sheet swings it open; a click on its
/// open cover page swings it shut. While shut its pages lie under its face
/// (ShowShut: hidden and out of the raycast), its left board is cut from its
/// model and its contact shadow lies under the right sheet alone. A page
/// turn's flick bends the loose sheets and every printed row, line and mark of
/// the open page rides the bend (RidePrints, as a paper's prints do: BUG-12a,
/// 10c), so no text sinks under the curl or slides off its ruled line.
/// </summary>
public sealed partial class DeskRulebook : MonoBehaviour, IHoverLift
{
    /// <inheritdoc />
    public void HoverLift(float share)
    {
        if (depth != null)
            depth.HoverLift(share);
    }

    /// <summary>A page turn's flick of the folder's loose sheets (D3 retires "the folder never bends"; GAME_FEEL_BIBLE A3: a bow of at most 3 mm on the flip, curling from the corner, settling like a heavy paper): its bow and twist (metres) and how long it settles (seconds).</summary>
    private const float FlipBow = 0.003f, FlipTwist = 0.0015f, FlipSeconds = 0.35f;

    /// <summary>How far a printed part riding the flick stands over the bent face, besides its own lift (metres; a paper's bent print's, DeskDocument).</summary>
    private const float RideLift = 0.0003f;

    /// <summary>How far under the folder's face its pages lie while it is shut (metres, booklet space, down): inside its body, out of sight and under its click box.</summary>
    private const float ShutDepth = 0.004f;

    /// <summary>The folder's front board (D3: it opens and closes; FolderCover). None: the folder lies open, as before.</summary>
    [SerializeField] private FolderCover folderCover;

    /// <summary>The printed parts riding the flick now: each one's rest pose and the pose it was given (a part a layout moved since keeps its new place).</summary>
    private readonly List<(Transform part, Vector3 rest, Quaternion restRotation, Vector3 shown, Quaternion shownRotation)> _riding = new List<(Transform, Vector3, Quaternion, Vector3, Quaternion)>();

    /// <summary>A riding part's renderers (reused: the flick allocates nothing per frame).</summary>
    private readonly List<Renderer> _rideRenderers = new List<Renderer>();

    /// <summary>The pages' root as built (the parent of the pages) and its place, the folder's config (for its shadow).</summary>
    private Transform _pages;
    private Vector3 _pagesRest;
    private DeskConfigSO _config;

    /// <summary>True while the folder shows shut (its board over the right sheet, or swinging).</summary>
    public bool Shut => folderCover != null && folderCover.Shut;

    /// <summary>The booklet's visible part and its click boxes, lifted off the desk plane by the stack (SetLift); the root stays on the plane, where the drag moves it.</summary>
    [SerializeField] private Transform booklet;

    /// <summary>The folder's contact shadow (a child of its card, in its unit space; DeskPaperDepth: lifting while it is dragged; the card wears the folder's model, Saleh 2026-10-17: "use canva and blender to make actual 3d documents").</summary>
    [SerializeField] private DeskPaperDepth depth;

    /// <summary>Each tab's contact shadow, as tabPlates (a raised card: its model from the builder, its small shadow on the folder).</summary>
    [SerializeField] private DeskPaperDepth[] tabDepths = Array.Empty<DeskPaperDepth>();

    /// <summary>The rules page's title ("TODAY'S RULES").</summary>
    [SerializeField] private TMP_Text title;

    /// <summary>The rules' rows, top first: each a click box (Clickable, a collider) with its text as its child.</summary>
    [SerializeField] private Clickable[] rows = Array.Empty<Clickable>();

    /// <summary>The folder's right sheet every page prints on (metres, width by depth, centred on the pages' parent: the builder's measure of the art): no rule's text leaves it (LayRows).</summary>
    [SerializeField] private Vector2 sheetSize = new Vector2(0.18f, 0.2f);

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

    /// <summary>The folder's face (its art: ArtSlots.RulebookFolder; the clean face while a culture's language is read).</summary>
    [SerializeField] private Renderer folderFace;

    /// <summary>The cover's words over the clean face, each built with its English (shown only while a culture's language is read).</summary>
    [SerializeField] private TMP_Text[] coverWords = Array.Empty<TMP_Text>();

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

    /// <summary>The rows' ink, and their ink in inspect mode (comparable: a dark amber, readable on the manila sheet under the hall's bright hours; the old light amber washed out, leftover 2026-10-08).</summary>
    [SerializeField] private Color rowInk = new Color(0.13f, 0.12f, 0.15f), inspectInk = new Color(0.45f, 0.22f, 0f);

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
    public IReadOnlyList<Clickable> Clicks => rows.Concat(paperRows).Concat(sealRows).Concat(tabs).Append(guidePrev).Append(guideNext).Append(card)
        .Append(folderCover != null ? folderCover.CoverClick : null).Where(c => c != null).ToArray();

    /// <summary>The booklet's drag (DeskController lifts it above the stack while it runs).</summary>
    public DeskDraggable Drag => drag;

    /// <summary>The cover's words in English as built (ShowCover prints them in the reading language).</summary>
    private string[] _coverEnglish = Array.Empty<string>();

    /// <summary>The folder's own words in the reading language: its cover, the RULES and PAPERS pages' heads and empty lines (again whenever the labels' language changes).</summary>
    private void PrintLabels()
    {
        ShowCover();
        if (_indices.Count > 0)
            Paginate(_ruleSheet); // a rule may print words of the reading language (TravelRuleSO.Summary)
        PrintTitle();
        if (none != null)
            none.text = UiText.Get("desk.rulebook.none");
        if (papersTitle != null)
            papersTitle.text = UiText.Get("desk.rulebook.papers");
        if (papersNone != null)
            papersNone.text = UiText.Get("desk.rulebook.papersNone");
    }

    /// <summary>
    /// The folder's cover in the reading language (Saleh 2026-10-07: every
    /// document in the game's language): while a culture's language is read
    /// the folder takes its clean face and prints its cover's words in that
    /// language (UiText.DocumentWord); in English the art keeps its own.
    /// </summary>
    private void ShowCover()
    {
        bool translated = UiText.FormWords() != null;
        Texture2D clean = translated ? SlotArt.Texture(new[] { ArtSlots.RulebookFolderClean }) : null;
        bool worded = clean != null && folderFace != null;
        if (folderFace != null)
        {
            var block = new MaterialPropertyBlock();
            folderFace.GetPropertyBlock(block);
            // the art's own face back by name (a cleared block would drop the shut folder's cut and the flick's bend too)
            Texture own = folderFace.sharedMaterial != null ? folderFace.sharedMaterial.GetTexture(BaseMapId) : null;
            if (worded)
                block.SetTexture(BaseMapId, clean);
            else if (own != null)
                block.SetTexture(BaseMapId, own);
            folderFace.SetPropertyBlock(block);
        }
        if (folderCover != null)
            folderCover.Face(worded ? clean : null);
        for (int i = 0; i < coverWords.Length; i++)
        {
            if (coverWords[i] == null)
                continue;
            if (worded && i < _coverEnglish.Length)
                coverWords[i].text = UiText.DocumentWord(_coverEnglish[i]);
            coverWords[i].gameObject.SetActive(worded && !Shut); // on the cover page: gone with it while shut
        }
    }

    /// <summary>The page shown: 0 RULES, 1 PAPERS, 2 GUIDE, 3 SEALS.</summary>
    public int Page { get; private set; }

    private void Awake()
    {
        _coverEnglish = Array.ConvertAll(coverWords, w => w != null ? w.text : string.Empty);
        _pages = rulesPage != null ? rulesPage.transform.parent : null;
        if (_pages != null)
            _pagesRest = _pages.localPosition;
        if (depth != null)
            depth.Bent += RidePrints;
        if (folderCover != null)
            folderCover.ShutChanged += ShowShut;
        if (card != null)
            card.onClick.AddListener(() => Open());
        PrintLabels();
        CultureThemeService.LabelsChanged += PrintLabels;
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
                tabs[i].onClick.AddListener(() => TabClicked(page));
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

    /// <summary>Rules printed while the folder was hidden are laid again once it shows (a hidden text's measures are not to be trusted).</summary>
    private void OnEnable()
    {
        if (_laidHidden)
            Paginate(_ruleSheet);
    }

    /// <summary>True when the rules were last laid while the folder was hidden.</summary>
    private bool _laidHidden;

    private void OnDestroy()
    {
        CultureThemeService.LabelsChanged -= PrintLabels;
        if (depth != null)
            depth.Bent -= RidePrints;
        if (folderCover != null)
            folderCover.ShutChanged -= ShowShut;
        if (drag != null)
            drag.DragEnded -= Dropped;
    }

    /// <summary>The folder's thickness (metres; DeskConfigSO.folderThickness through ShowDepth, as deep as its model under the face its pages print on): its room in the papers' stack.</summary>
    public float Thickness { get; private set; } = 0.0055f;

    /// <summary>The folder's body on its card (shares of the card, centre and size): the art's opaque folder (measured on Forms/rulebook_folder), its tabs above it left out. The builder sizes the front board by them (FolderCover).</summary>
    public const float BodyCentreX = 0.003f, BodyCentreY = -0.0275f, BodyWidth = 0.957f, BodyHeight = 0.917f;

    /// <summary>A tab's outline on its plate (unit space, counter-clockwise from its bottom left; measured on the tab art: a slight trapezoid, its top corners rounded, as its model).</summary>
    private static readonly List<(float x, float y)> TabOutline = new List<(float x, float y)>
    {
        (-0.5f, -0.49f), (0.498f, -0.49f), (0.481f, -0.262f), (0.466f, 0.214f), (0.435f, 0.405f), (-0.447f, 0.405f), (-0.477f, 0.31f), (-0.483f, 0.024f)
    };

    /// <summary>A tab's raised card: its thickness (metres, its model's) and its shadow's share of the folder's darkness.</summary>
    private const float TabThickness = 0.0006f, TabShadow = 0.7f;

    /// <summary>
    /// The folder's contact shadows from <paramref name="config"/> (DeskController,
    /// at load; Saleh 2026-10-17: "use canva and blender to make actual 3d
    /// documents ... especially the folder"): the folder wears its model from
    /// Build Office UI (card boards with a creased spine, the loose sheets, the
    /// pocket flaps, the clip, its art on top), folderThickness deep under the
    /// face its pages print on; its shadow lies under its body (lifting while
    /// it is dragged) and each raised tab casts its own small one.
    /// </summary>
    public void ShowDepth(DeskConfigSO config)
    {
        if (config == null)
            return;
        _config = config;
        Thickness = config.folderThickness;
        BuildShadow();
        for (int i = 0; i < tabDepths.Length && i < tabPlates.Length; i++)
            if (tabDepths[i] != null && tabPlates[i] != null)
                tabDepths[i].Build(TabOutline, TabThickness, tabPlates[i].transform.localScale, config, TabShadow);
    }
    /// <summary>Turns to page <paramref name="page"/> (0 RULES, 1 PAPERS, 2 GUIDE, 3 SEALS once introduced), opening the folder first when it is shut (D3): its rows show, the others' hide; its tab looks open; a turn flicks the loose sheets. The GUIDE shows its sheet (read).</summary>
    public void ShowPage(int page)
    {
        Open();
        TurnTo(page, true);
    }

    /// <summary>Swings the folder open when it is shut (FolderCover; nothing without one); true when it started.</summary>
    public bool Open() => folderCover != null && folderCover.Shut && folderCover.Open();

    /// <summary>Turns to <paramref name="page"/> (ShowPage's turn), shut or open; <paramref name="flick"/>: a change of page flicks the loose sheets (their rows riding it).</summary>
    private void TurnTo(int page, bool flick)
    {
        int before = Page;
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
        if (flick && Page != before && depth != null && folderFace != null && isActiveAndEnabled)
            depth.Flick(new PaperBend(1f, 0f, FlipBow, 0.5f, FlipTwist, 0f, 0f, 0f), folderFace.transform.localScale, FlipSeconds);
    }

    /// <summary>
    /// The folder shut (<paramref name="shut"/>: its front board over the right
    /// sheet, or swinging; FolderCover.ShutChanged) or open: shut, its left
    /// board is cut from its model (DeskPaperDepth.ClipLeftOf at the spine), its
    /// contact shadow lies under the right sheet alone, its pages lie under its
    /// face (ShutDepth: out of sight and out of the raycast, so a click there
    /// meets the folder's own box and opens it), and its cover's words and the
    /// tabs over the left board hide; open, all of it shows again.
    /// </summary>
    private void ShowShut(bool shut)
    {
        if (depth != null)
            depth.ClipLeftOf(shut ? folderCover.Spine : -10f);
        BuildShadow();
        if (_pages != null)
            _pages.localPosition = shut ? _pagesRest + Vector3.down * ShutDepth : _pagesRest;
        foreach (TMP_Text word in coverWords)
            if (word != null && word.gameObject.activeSelf && shut)
                word.gameObject.SetActive(false);
        if (!shut)
            ShowCover();
        Transform face = folderFace != null ? folderFace.transform : null;
        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i] == null || face == null || (i == SealsPageIndex && !_sealsIntroduced))
                continue;
            bool show = !shut || face.InverseTransformPoint(tabs[i].transform.position).x > folderCover.Spine;
            tabs[i].gameObject.SetActive(show);
            if (i < tabPlates.Length && tabPlates[i] != null)
                tabPlates[i].gameObject.SetActive(show);
        }
    }

    /// <summary>The folder's contact shadow under its body (the right sheet alone while shut).</summary>
    private void BuildShadow()
    {
        if (_config == null || depth == null || folderFace == null)
            return;
        float left = BodyCentreX - BodyWidth / 2f, right = BodyCentreX + BodyWidth / 2f;
        if (Shut)
            left = folderCover.Spine;
        float width = right - left, centre = (left + right) / 2f;
        var outline = PaperEdge.Rectangle(width, BodyHeight).ConvertAll(p => (p.x + centre, p.y + BodyCentreY));
        depth.Build(outline, Thickness, folderFace.transform.localScale, _config);
    }

    /// <summary>
    /// A page turn's flick (DeskPaperDepth.Bent, every step of it): each
    /// printed part of the open page (its title, rows, lines and marks) and
    /// the cover's words go back to where they were printed, then ride the bent
    /// face under their centre, turned to its slope and lifted by how far the
    /// curl rises past their flat piece (PaperBend.Bulge), as a paper's prints
    /// do (BUG-12a); flat, they stay where they were printed. A part a layout
    /// moved meanwhile keeps its new place.
    /// </summary>
    private void RidePrints(PaperBend bend)
    {
        foreach ((Transform part, Vector3 rest, Quaternion restRotation, Vector3 shown, Quaternion shownRotation) in _riding)
            if (part != null && part.localPosition == shown && part.localRotation == shownRotation)
            {
                part.localPosition = rest;
                part.localRotation = restRotation;
            }
        _riding.Clear();
        if (bend.IsFlat || folderFace == null || _pages == null || depth == null)
            return;
        Transform face = folderFace.transform;
        Vector2 size = depth.BendSize;
        float w = Mathf.Max(size.x, 1e-4f), h = Mathf.Max(size.y, 1e-4f);
        foreach (Transform page in _pages)
            if (page.gameObject.activeSelf)
                foreach (Transform part in page)
                    if (part.gameObject.activeSelf)
                        Ride(part, bend, face, w, h);
        foreach (TMP_Text word in coverWords)
            if (word != null && word.gameObject.activeSelf)
                Ride(word.transform, bend, face, w, h);
    }

    /// <summary>One printed part on the bent face (RidePrints): its centre and half size in the face's frame from its renderers' bounds.</summary>
    private void Ride(Transform part, PaperBend bend, Transform face, float w, float h)
    {
        part.GetComponentsInChildren(false, _rideRenderers);
        if (_rideRenderers.Count == 0)
            return;
        Bounds bounds = _rideRenderers[0].bounds;
        for (int i = 1; i < _rideRenderers.Count; i++)
            bounds.Encapsulate(_rideRenderers[i].bounds);
        Vector3 centre = face.InverseTransformPoint(bounds.center);
        float halfX = 0f, halfY = 0f;
        for (int c = 0; c < 8; c++)
        {
            Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3((c & 1) == 0 ? -1f : 1f, (c & 2) == 0 ? -1f : 1f, (c & 4) == 0 ? -1f : 1f));
            Vector3 local = face.InverseTransformPoint(corner);
            halfX = Mathf.Max(halfX, Mathf.Abs(local.x - centre.x) * w);
            halfY = Mathf.Max(halfY, Mathf.Abs(local.y - centre.y) * h);
        }
        float x = Mathf.Clamp(centre.x * 2f, -1f, 1f), y = Mathf.Clamp(centre.y * 2f, -1f, 1f);
        float drop = bend.Depth(x, y, w, h);
        (float dx, float dy) = bend.Slope(x, y, w, h);
        float bulge = bend.Bulge(x, y, halfX, halfY, w, h);
        Vector3 rest = part.localPosition;
        Quaternion restRotation = part.localRotation;
        // the face's +z runs away from the viewer (into the desk), as a paper's sheet's does
        part.position += face.TransformDirection(Vector3.forward) * (drop - RideLift - bulge);
        part.rotation = face.rotation * Quaternion.Euler(Mathf.Atan(dy) * Mathf.Rad2Deg, -Mathf.Atan(dx) * Mathf.Rad2Deg, 0f) * Quaternion.Inverse(face.rotation) * part.rotation;
        _riding.Add((part, rest, restRotation, part.localPosition, part.localRotation));
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
            TurnTo(0, false);
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
                sealNames[r].text = UiText.DocumentWord(offices[r].name);
        }
    }

    /// <summary>The world bounds of seal row <paramref name="index"/> (a match line meets it there); false when it prints no row, its page is closed or the booklet is hidden.</summary>
    public bool TrySealBounds(int index, out Bounds bounds)
    {
        bounds = default;
        if (!isActiveAndEnabled || Shut || index < 0 || index >= _sealCount || index >= sealRows.Length || sealRows[index] == null ||
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
                text.text = UiText.Format(key, UiText.DocumentWord(open[r].Label));
        }
        if (papersNone != null)
            papersNone.gameObject.SetActive(_paperIds.Count == 0);
    }

    /// <summary>
    /// Prints today's directives (<paramref name="rules"/>, the day's list; a
    /// rule with no summary prints no row), one per row, on as many RULES
    /// sheets as they need (RuleSheets: those that do not fit a sheet at the
    /// least readable size go on the next; leftover 2026-10-08; every text
    /// measured on the sheet itself, Saleh's 1016a playtest: "when there are
    /// many rules the text overflows out of the folder"), and opens on RULES
    /// at the first sheet.
    /// </summary>
    public void Show(IReadOnlyList<TravelRuleSO> rules)
    {
        _rules = rules ?? Array.Empty<TravelRuleSO>();
        _indices.Clear();
        for (int i = 0; i < _rules.Count; i++)
            if (_rules[i] != null && !string.IsNullOrWhiteSpace(_rules[i].Summary()))
                _indices.Add(i);
        TurnTo(0, false);
        Paginate(0);
        if (none != null)
            none.gameObject.SetActive(_indices.Count == 0);
        if (folderCover != null)
            folderCover.ShutNow(); // the day's rules arrive in a shut folder (D3)
    }

    /// <summary>
    /// Splits the directives into RULES sheets (RuleSheets.Starts over
    /// LayRows, the RULES page shown while it measures so its texts measure
    /// live, the page shown before kept) and prints sheet
    /// <paramref name="sheet"/> (the last when there are fewer).
    /// </summary>
    private void Paginate(int sheet)
    {
        bool shown = rulesPage == null || rulesPage.activeSelf;
        if (rulesPage != null)
            rulesPage.SetActive(true);
        _laidHidden = !isActiveAndEnabled;
        _sheetStarts = RuleSheets.Starts(_indices.Count, rows.Length, (start, count) =>
        {
            PrintRows(start, count);
            return LayRows(count, false);
        });
        ShowRuleSheet(Mathf.Clamp(sheet, 0, _sheetStarts.Count - 1));
        if (rulesPage != null)
            rulesPage.SetActive(shown);
    }

    /// <summary>The first directive (of _indices) on each RULES sheet (RuleSheets.Starts).</summary>
    private List<int> _sheetStarts = new List<int> { 0 };

    /// <summary>The RULES sheet shown.</summary>
    private int _ruleSheet;

    /// <summary>The first directive (of _indices) the open RULES sheet prints.</summary>
    private int SheetStart => _sheetStarts[_ruleSheet];

    /// <summary>How many directives the open RULES sheet prints.</summary>
    private int SheetCount => (_ruleSheet + 1 < _sheetStarts.Count ? _sheetStarts[_ruleSheet + 1] : _indices.Count) - SheetStart;

    /// <summary>Prints and lays RULES sheet <paramref name="sheet"/> (wrapping), its number in the title when there are several.</summary>
    private void ShowRuleSheet(int sheet)
    {
        _ruleSheet = ((sheet % _sheetStarts.Count) + _sheetStarts.Count) % _sheetStarts.Count;
        PrintRows(SheetStart, SheetCount);
        LayRows(SheetCount, true);
        SetInspecting(_inspecting);
        PrintTitle();
    }

    /// <summary>Rows from the first print directives <paramref name="start"/> on, <paramref name="count"/> of them (numbered as the day's list); the other rows hide.</summary>
    private void PrintRows(int start, int count)
    {
        for (int r = 0; r < rows.Length; r++)
        {
            if (rows[r] == null)
                continue;
            bool on = r < count && start + r < _indices.Count;
            rows[r].gameObject.SetActive(on);
            if (on && rows[r].GetComponentInChildren<TMP_Text>(true) is TMP_Text text)
            {
                text.text = $"{start + r + 1}. {_rules[_indices[start + r]].Summary()}";
                // no word breaks across lines, measured at the full size LayRows starts from (it only shrinks from there); an
                // Arabic rule laid line by line (BUG-9, 37c)
                text.fontSize = text.fontSizeMax;
                TmpFormText.Unbreak(text, text.rectTransform.sizeDelta.x, shrink: false);
            }
        }
    }

    /// <summary>The RULES title: "TODAY'S RULES", and the open sheet's number ("1 / 2") when the rules take more than one (a click on the RULES tab turns to the next).</summary>
    private void PrintTitle()
    {
        if (title == null)
            return;
        string head = UiText.Get("desk.rulebook.title");
        title.text = _sheetStarts.Count > 1 ? $"{head}  {UiText.Format("desk.guide.page", _ruleSheet + 1, _sheetStarts.Count)}" : head;
    }

    /// <summary>A tab clicked: turns to its page (RULES at its first sheet); the RULES tab clicked on an open RULES page with several sheets turns to the next sheet.</summary>
    private void TabClicked(int page)
    {
        if (Shut && page == Page)
        {
            Open(); // the shut folder opens at its tab, without turning a sheet
            return;
        }
        if (page == 0 && Page == 0 && _sheetStarts.Count > 1)
        {
            ShowRuleSheet(_ruleSheet + 1);
            return;
        }
        if (page == 0 && _ruleSheet != 0)
            ShowRuleSheet(0);
        ShowPage(page);
    }

    /// <summary>A rule row as built: its place, its text's place and size, its click box's centre and size (taken on the first LayRows).</summary>
    private struct RowHome
    {
        public Vector3 Row, TextPosition, BoxCentre, BoxSize;
        public Vector2 TextSize;
    }

    /// <summary>The rule rows as built (empty until the first LayRows).</summary>
    private RowHome[] _rowHomes = Array.Empty<RowHome>();

    /// <summary>The least share of its built size a rule row's text shrinks to before the rows go on to the next sheet: none (BUG-9: the rulebook paginates before it shrinks; only a rule alone on its sheet ever shrinks, TextFit.Floor first, RowTextLeast at the very last).</summary>
    private const float RowTextFloor = 1f;

    /// <summary>The least share a rule alone on its sheet shrinks to when even at RowTextFloor it would leave the sheet (it never does: it shrinks instead).</summary>
    private const float RowTextLeast = 0.3f;

    /// <summary>How far inside the sheet's edges every rule's printed text stays (metres).</summary>
    private const float SheetMargin = 0.003f;

    /// <summary>The sheet's blank foot under the last ruled line the rows may run into (metres; the art's sheet ends 0.037 m below it): day 12's six rules ran off the page and the screen (leftover 2026-10-08).</summary>
    private const float RowsFoot = 0.03f;

    /// <summary>
    /// Lays the shown rule rows down the page (the orchestrator's catch on
    /// 2026-10-08: rule 3's last line printed over rule 4's first): each row
    /// grows downward to fit its wrapped text (its text's box, its click box)
    /// and pushes the rows under it down by as much; when the rows would run
    /// past the last row's place and the sheet's foot under it (RowsFoot), or
    /// any row's printed text, measured as drawn, would leave the sheet
    /// (sheetSize, SheetMargin inside its edges: a long word, a line the
    /// estimate missed; Saleh's 1016a playtest: "when there are many rules
    /// the text overflows out of the folder"), every row's text shrinks a
    /// step (down to RowTextFloor of its built size) and they are laid again.
    /// A row whose text fits keeps its place on the art's line. Lays the first
    /// <paramref name="count"/> rows; false when they run past the page even
    /// at the floor (RuleSheets then moves the last to the next sheet), unless
    /// <paramref name="final"/> or the rule is alone on its sheet: then it
    /// shrinks on, down to RowTextLeast, so no text ever leaves the sheet.
    /// </summary>
    private bool LayRows(int count, bool final)
    {
        if (_rowHomes.Length != rows.Length)
        {
            _rowHomes = new RowHome[rows.Length];
            for (int r = 0; r < rows.Length; r++)
            {
                if (rows[r] == null)
                    continue;
                TMP_Text t = rows[r].GetComponentInChildren<TMP_Text>(true);
                BoxCollider b = rows[r].GetComponent<BoxCollider>();
                _rowHomes[r] = new RowHome
                {
                    Row = rows[r].transform.localPosition,
                    TextPosition = t != null ? t.transform.localPosition : Vector3.zero,
                    TextSize = t != null ? t.rectTransform.sizeDelta : Vector2.zero,
                    BoxCentre = b != null ? b.center : Vector3.zero,
                    BoxSize = b != null ? b.size : Vector3.zero,
                };
            }
        }
        count = Mathf.Min(count, rows.Length);
        if (count == 0 || rows[rows.Length - 1] == null)
            return true;
        RowHome lastPlace = _rowHomes[rows.Length - 1];
        float pageBottom = lastPlace.Row.z - lastPlace.BoxSize.z / 2f - RowsFoot;
        float pitch = rows.Length > 1 ? Mathf.Abs(_rowHomes[0].Row.z - _rowHomes[1].Row.z) : 0f; // the art's ruled lines (BUG-10c)
        for (float share = 1f; ; share -= 0.05f)
        {
            float shift = 0f, bottom = float.MaxValue;
            for (int r = 0; r < count; r++)
            {
                if (rows[r] == null)
                    continue;
                RowHome home = _rowHomes[r];
                TMP_Text t = rows[r].GetComponentInChildren<TMP_Text>(true);
                float extra = 0f;
                if (t != null)
                {
                    t.enableAutoSizing = false;
                    t.fontSize = t.fontSizeMax * share;
                    // a wrapped rule's lines step down the ruled lines, and its row takes whole lines (BUG-10c: text sat through the rules)
                    if (t.font != null && t.font.faceInfo.pointSize > 0f)
                        t.lineSpacing = RuledLines.Spacing(pitch, t.fontSize * TmpFormText.WorldEmPerFontSize, t.font.faceInfo.lineHeight / t.font.faceInfo.pointSize);
                    extra = RuledLines.Snap(Mathf.Max(0f, t.GetPreferredValues(t.text, home.TextSize.x, 0f).y - home.TextSize.y), pitch);
                    t.rectTransform.sizeDelta = new Vector2(home.TextSize.x, home.TextSize.y + extra);
                    t.transform.localPosition = home.TextPosition - new Vector3(0f, 0f, extra / 2f); // its top stays on the row's top
                }
                rows[r].transform.localPosition = home.Row - new Vector3(0f, 0f, shift);
                if (rows[r].TryGetComponent(out BoxCollider b))
                {
                    b.center = home.BoxCentre - new Vector3(0f, 0f, extra / 2f);
                    b.size = home.BoxSize + new Vector3(0f, 0f, extra);
                }
                shift += extra;
                bottom = home.Row.z - shift - home.BoxSize.z / 2f; // its click box's foot
            }
            if (bottom >= pageBottom - 0.0001f && OnSheet(count))
                return true;
            if (share <= RowTextLeast + 0.001f || (share <= RowTextFloor + 0.001f && !final && count > 1))
                return false;
            if (share > TextFit.Floor + 0.001f && share - 0.05f < TextFit.Floor)
                share = TextFit.Floor + 0.05f; // the next step lands on the floor before it goes under it
        }
    }

    private readonly Vector3[] _corners = new Vector3[4];

    /// <summary>True when the printed text of each of the first <paramref name="count"/> rule rows, measured as drawn, lies on the sheet (sheetSize about the pages' parent, SheetMargin inside its edges).</summary>
    private bool OnSheet(int count)
    {
        Transform page = rulesPage != null ? rulesPage.transform.parent : null;
        if (page == null)
            return true;
        float hx = sheetSize.x / 2f - SheetMargin, hz = sheetSize.y / 2f - SheetMargin;
        for (int r = 0; r < count && r < rows.Length; r++)
        {
            TMP_Text t = rows[r] != null ? rows[r].GetComponentInChildren<TMP_Text>(true) : null;
            if (t == null)
                continue;
            t.ForceMeshUpdate(true);
            Bounds b = t.textBounds;
            if (t.textInfo.characterCount == 0)
                continue;
            _corners[0] = new Vector3(b.min.x, b.min.y, 0f);
            _corners[1] = new Vector3(b.max.x, b.min.y, 0f);
            _corners[2] = new Vector3(b.min.x, b.max.y, 0f);
            _corners[3] = new Vector3(b.max.x, b.max.y, 0f);
            foreach (Vector3 corner in _corners)
            {
                Vector3 local = page.InverseTransformPoint(t.transform.TransformPoint(corner));
                if (local.x < -hx || local.x > hx || local.z < -hz || local.z > hz)
                    return false;
            }
        }
        return true;
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
        {
            guideBody.text = sheet.Body ?? string.Empty;
            TmpFormText.Unbreak(guideBody, guideBody.rectTransform.sizeDelta.x, shrink: false); // auto-sized: only cut, never shrunk again on every sheet
        }
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

    /// <summary>
    /// Where the guide's arrow looks in the booklet: today's rules (its
    /// "rulebook" target: <paramref name="seals"/> false) or the Seal Register
    /// (its "seals" target): on that page, open, its row <paramref name="row"/>
    /// (the first when out of range), else the tab that turns to it; the shut
    /// folder's board while it is shut (a click there opens it, D3); false
    /// while the booklet (or the SEALS tab, before the register is introduced) is hidden.
    /// </summary>
    public bool TryRulesPoint(bool seals, int row, out Vector3 world)
    {
        world = default;
        if (!isActiveAndEnabled)
            return false;
        if (Shut && folderFace != null)
        {
            // the shut folder: the arrow looks at its board (a click there opens it)
            world = folderFace.transform.TransformPoint(new Vector3((folderCover.Spine + BodyCentreX + BodyWidth / 2f) / 2f, BodyCentreY, 0f));
            return true;
        }
        int page = seals ? SealsPageIndex : 0;
        Clickable[] open = seals ? sealRows : rows;
        Clickable at = Page == page && open.Length > 0 ? open[row >= 0 && row < open.Length ? row : 0] : null;
        if (at != null && at.gameObject.activeInHierarchy && at.TryGetComponent(out Collider box))
        {
            world = box.bounds.center;
            return true;
        }
        Clickable tab = page < tabs.Length ? tabs[page] : null;
        if (tab == null || !tab.gameObject.activeInHierarchy)
            return false;
        world = tab.TryGetComponent(out Collider tabBox) ? tabBox.bounds.center : tab.transform.position;
        return true;
    }

    /// <summary>The world bounds of the row printing directive <paramref name="ruleIndex"/> (a match line meets it there); false when it prints no row, its page is closed or the booklet is hidden.</summary>
    public bool TryRowBounds(int ruleIndex, out Bounds bounds)
    {
        bounds = default;
        int row = _indices.IndexOf(ruleIndex) - SheetStart;
        if (row >= SheetCount)
            return false;
        if (!isActiveAndEnabled || Shut || row < 0 || row >= rows.Length || rows[row] == null || !rows[row].gameObject.activeInHierarchy ||
            !rows[row].TryGetComponent(out Collider box))
            return false;
        bounds = box.bounds;
        return true;
    }

    /// <summary>A paper row clicked: its paper is flagged missing (the controller ignores one flagged already).</summary>
    private void FlagPaper(int row)
    {
        if (row < _paperIds.Count)
            PaperFlagged?.Invoke(_paperIds[row]);
    }

    private void Click(int row)
    {
        if (row < SheetCount)
            RowClicked?.Invoke(_indices[SheetStart + row], _rules[_indices[SheetStart + row]]);
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
