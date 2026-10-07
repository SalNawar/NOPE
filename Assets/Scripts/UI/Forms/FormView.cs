using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// A form drawn with uGUI on the PC (redesign phase 5, PC spec FO1, §6.5): the
/// twin of the desk paper (DeskDocument). FormLayout places the form at the
/// width the caller gives (Show's width, else the view's own), and every size
/// follows it (FormLayout.Layout): a document copy takes 520 u, a page H =
/// 679 u tall (the PC UX redesign: a split pane holds it whole); a page kind in the Investigation app's pane takes the pane's
/// width, so its table cells reach 13 px at 720p (from about 564 u); a flow
/// page grows with its rows. Every document is drawn as wide and prints at
/// one size (FormLayout.PrintUnit; the travel documents spec, TD2): a look
/// narrower than the style's (FormLook.aspect) is a longer page, a wider one
/// (a ticket, a card) a shorter one. The view draws that placed form:
/// the paper (its kind's face on a document when the art exists,
/// ArtSlots.PaperFaces, else the look's tint), the seal faint behind the
/// header, or each named seal (the issuing office's in a document's header,
/// the Seal Register's) as its outline in its ink with its legend over it
/// (SealArt, pooled clones of the seal), the frame's bands, the fills and bands under the slots' tints and
/// every outline, rule, barcode bar, tick and the stamp area's dash over them
/// (FormPaint's quads, which the desk paper prints too, one FormStrokes graphic
/// per layer), a TextMeshPro per text cloned from one template and styled by
/// its role (TmpFormText; shrunk just enough that its widest word fits its
/// box, so a label never breaks mid-word), and the traveller's photo in its cell (under the
/// photo frame's art when it exists, as on the desk paper; the agency seal's
/// art likewise). The layout
/// measures with a hidden text of the template's font that the view makes
/// for itself (TextMeshPro measures only once awake, and a window is filled
/// while still inactive). Each pickable slot gets a button over its
/// box: a click raises SlotClicked, and the box under the pointer tints
/// (FormLayout.SlotAt, as on the desk). A box's pick tint follows its key
/// (the PC redesign CM3: Bind; CompareController.IsPicked and PicksChanged),
/// so a value shown in both panes lights in both and a redrawn form is lit
/// again. A slot with a smart link gets a ↗ at its box's top right (LK2: a
/// click raises LinkClicked; the link never picks), and a form whose slots
/// link leaves the ↗'s room at the right of its tables' last column
/// (FormLayout's row link room), so a row's ↗ sits after its text; a table
/// row's cells can each carry one (the Deviation Report's two sides:
/// CellLinkClicked), at the cell's top right; and MarkFound outlines the box
/// a link went to. A text the owner's script function gives a font (a
/// transcript line in its tongue's script) is measured and printed in that
/// font (TmpFormText.ScriptOf), so its box is as tall as its glyphs draw;
/// every other text is in the template's font. The Analysis Scanner's marks (SetMarks: a dashed
/// outline in the style's analysis colour on a field's box, FormPaint.MarkQuads)
/// draw over the lines. The view sets its own height to the form's, so a
/// scroll rect can hold it. Its parts are the builder's, tagged DiegeticForm
/// (forms are never themed), and pooled: a new Show reuses them. Always
/// English (TR1): values are written as they are.
/// </summary>
public sealed class FormView : MonoBehaviour, IPointerMoveHandler, IPointerExitHandler
{
    /// <summary>The forms' sizes and colours (the desk paper's style).</summary>
    [SerializeField] private FormStyleSO style;

    /// <summary>The page under the form (the view's own graphic, in the style's paper colour; the raycast target between the boxes).</summary>
    [SerializeField] private Image paper;

    /// <summary>The seal, printed faintly behind the header (inactive until a form places it).</summary>
    [SerializeField] private Image seal;

    /// <summary>The box fills and section bands, under the slots' tints.</summary>
    [SerializeField] private FormStrokes fills;

    /// <summary>Where the slots' buttons are cloned (spans the form).</summary>
    [SerializeField] private RectTransform slotsRoot;

    /// <summary>The inactive slot button cloned per pickable slot: a clear image (its hover and pick tint) and a button.</summary>
    [SerializeField] private Button slotTemplate;

    /// <summary>The outlines, rules, bars, ticks and the stamp dash, over the slots' tints.</summary>
    [SerializeField] private FormStrokes lines;

    /// <summary>The photo's cell (inactive on a form without a photo).</summary>
    [SerializeField] private RectTransform photoFrame;

    /// <summary>The traveller's photo inside the cell.</summary>
    [SerializeField] private TravellerPortraitView photo;

    /// <summary>The photo frame's art over the photo (inactive until its art, ArtSlots.PhotoFrame, is found).</summary>
    [SerializeField] private Image photoFrameArt;

    /// <summary>The inactive ↗ cloned per linked slot (a small button with the drawn glyph and its hover hint).</summary>
    [SerializeField] private Button linkTemplate;

    /// <summary>The found mark: an outline placed over the box a link went to (inactive otherwise).</summary>
    [SerializeField] private RectTransform found;

    /// <summary>The ↗'s side: its hit box, at the box's top right corner, and the room a linked table row's last column leaves it.</summary>
    [SerializeField, Min(1f)] private float linkSize = 28f;

    /// <summary>Where the texts are cloned (spans the form).</summary>
    [SerializeField] private RectTransform textsRoot;

    /// <summary>The inactive text every printed word clones (its font and material are also the measure's).</summary>
    [SerializeField] private TextMeshProUGUI textTemplate;

    /// <summary>One pooled slot button: which slot it shows, and whether its key is picked.</summary>
    private sealed class SlotPart
    {
        public Button Button;
        public Image Tint;
        public int Slot;
        public bool Picked;
    }

    /// <summary>One pooled ↗: which slot it follows, which of its cells (-1: the whole box), and its hover hint's text.</summary>
    private sealed class LinkPart
    {
        public Button Button;
        public TMP_Text Hint;
        public int Slot;
        public int Cell;
    }

    private readonly List<TextMeshProUGUI> _texts = new List<TextMeshProUGUI>();

    /// <summary>The named seals' pooled images (clones of the seal) and their legends.</summary>
    private readonly List<(Image mark, TextMeshProUGUI legend)> _seals = new List<(Image, TextMeshProUGUI)>();

    /// <summary>The emblems' pooled images (clones of the seal among the texts; the travel documents spec, TD3).</summary>
    private readonly List<Image> _emblems = new List<Image>();

    /// <summary>The watermarks' pooled images (clones of the seal just over the fills, under the slots' tints, the lines and the texts; TD1).</summary>
    private readonly List<Image> _watermarks = new List<Image>();

    /// <summary>The patches' pooled images (clones of the seal just over the paper; a document drawn on its art, ArtLayout).</summary>
    private readonly List<Image> _patches = new List<Image>();

    /// <summary>The blank faces' pieces as sprites, made once each (a texture's id and the piece in its pixels).</summary>
    private static readonly Dictionary<(int, Rect), Sprite> PatchSprites = new Dictionary<(int, Rect), Sprite>();

    /// <summary>A card's rounded paper (a 9-sliced sprite painted once; TD1).</summary>
    private static Sprite _cardPaper;

    /// <summary>The card paper sprite's side and its corner's radius, in pixels.</summary>
    private const int CardPaperSize = 64, CardPaperCorner = 24;

    /// <summary>Every document's width (the width of the first document shown): a later Show without a width draws at it.</summary>
    private float _documentWidth;
    private readonly List<SlotPart> _parts = new List<SlotPart>();
    private readonly List<LinkPart> _links = new List<LinkPart>();

    /// <summary>The shown form's own quads (FormPaint.Quads), redrawn with the marks.</summary>
    private List<FormQuad> _quads = new List<FormQuad>();

    /// <summary>The fields marked by the Analysis Scanner on this form.</summary>
    private readonly List<int> _marks = new List<int>();
    private TmpFormText _measure;
    private TextMeshProUGUI _measureText;
    private Sprite _sealRing;
    private PlacedForm _form;
    private SlotPart _hovered;
    private CompareController _compare;
    private Func<FormSlot, string> _keyOf;

    /// <summary>Raised when a pickable slot is clicked (its field, or its table row).</summary>
    public event Action<FormSlot> SlotClicked;

    /// <summary>Raised when a slot's ↗ is clicked.</summary>
    public event Action<FormSlot> LinkClicked;

    /// <summary>Raised when a ↗ on one cell of a table row's slot is clicked: the slot and the cell's index in its row.</summary>
    public event Action<FormSlot, int> CellLinkClicked;

    /// <summary>The form as last placed (null before the first Show).</summary>
    public PlacedForm Placed => _form;

    /// <summary>The forms' style the view prints in.</summary>
    public FormStyleSO Style => style;

    /// <summary>Fills <paramref name="into"/> with the shown form's pickable slots and their buttons, in slot order (the reading order: the keys' rows, AppRow).</summary>
    public void ArmedSlots(List<(FormSlot slot, Button button)> into)
    {
        into.Clear();
        if (_form == null)
            return;
        foreach (SlotPart part in _parts)
            if (part.Button.gameObject.activeSelf && part.Slot >= 0 && part.Slot < _form.Slots.Count)
                into.Add((_form.Slots[part.Slot], part.Button));
    }

    /// <summary>
    /// Tints each box while the key <paramref name="keyOf"/> gives it is
    /// picked in <paramref name="compare"/> (null key: never), now and on
    /// every change of the picks (CM3).
    /// </summary>
    public void Bind(CompareController compare, Func<FormSlot, string> keyOf)
    {
        if (_compare != compare)
        {
            if (_compare != null)
                _compare.PicksChanged -= Relight;
            _compare = compare;
            if (_compare != null)
                _compare.PicksChanged += Relight;
        }
        _keyOf = keyOf;
        Relight();
    }

    /// <summary>
    /// Draws <paramref name="spec"/> showing <paramref name="data"/> at
    /// <paramref name="width"/> (above 0: the view takes that width first;
    /// else its own, fixed by its anchors) and sets the view's height to the
    /// form's. Pass a document copy 520 u and a page kind its pane's width,
    /// and Show again when the pane's width changes (a maximise). The slots
    /// <paramref name="pickable"/> accepts (every slot when null) get a button
    /// and tint under the pointer, lit while their key is picked (Bind); the
    /// slots <paramref name="linkHint"/> gives a hint get a ↗ with that hint,
    /// and the cells of a table row <paramref name="cellLinkHint"/> gives a
    /// hint (the slot and the cell's index) get one each at the cell's top
    /// right; with <paramref name="linkHint"/> the tables' last column leaves
    /// the ↗'s room. A text <paramref name="scriptOf"/> gives a font (a
    /// transcript line in its tongue's script) is measured and printed in it.
    /// The found mark clears. Returns the placed form (null without a style or
    /// text template).
    /// </summary>
    public PlacedForm Show(FormSpec spec, FormData data, Func<FormSlot, bool> pickable = null, float width = 0f, Func<FormSlot, string> linkHint = null,
                           Func<FormSlot, int, string> cellLinkHint = null, Func<string, TMP_FontAsset> scriptOf = null)
    {
        _hovered = null;
        _form = null;
        _marks.Clear();
        if (style == null || textTemplate == null)
            return null;

        var rt = (RectTransform)transform;
        FormLook look = spec != null && spec.look != null ? spec.look : new FormLook();
        if (spec != null && spec.fixedPage)
        {
            if (_documentWidth <= 0f)
                _documentWidth = width > 0f ? width : rt.rect.width;
            width = width > 0f ? width : _documentWidth;
        }
        if (width > 0f)
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        _measure ??= new TmpFormText(MeasureText());
        data = UiText.InReadingLanguage(data);
        TMP_FontAsset culture = data != null && data.Translated && CultureThemeService.Instance != null ? CultureThemeService.Instance.CultureFont : null;
        _measure.ScriptOf = culture == null ? scriptOf : text => (scriptOf != null ? scriptOf(text) : null) ?? culture;
        _form = FormLayout.Layout(spec, data, rt.rect.width, style.metrics, _measure, linkHint != null ? linkSize : 0f);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _form.Height);
        FormPalette palette = look.Palette(style.Palette());
        FormArt art = ArtLayout.IsArt(spec) ? look.art : null;
        string formNumber = spec != null && spec.fixedPage && data != null ? data.FormNumber : null;
        ShowArt(formNumber, data != null ? data.Issuer : null,
                string.IsNullOrEmpty(look.paper) ? style.paper : new Color(palette.Paper.R, palette.Paper.G, palette.Paper.B, 1f),
                PaperSilhouette.Corner(look.frame, _form.Width, _form.PageHeight, _form.Unit), art != null, art != null && art.Worded(data));
        Texture2D blank = art != null ? SlotArt.Texture(new[] { ArtSlots.PaperBlank(formNumber) }) : null;

        int texts = 0, seals = 0, emblems = 0, watermarks = 0, patches = 0;
        bool sealShown = false, photoShown = false;
        Color cover = EmblemArt.Ink(data != null ? data.Cover : null, new Color(palette.Accent.R, palette.Accent.G, palette.Accent.B, 1f));
        foreach (FormItem item in _form.Items)
        {
            if (item.Kind == FormItemKind.Text)
                Print(texts++, item, art);
            else if (item.Kind == FormItemKind.Patch && seal != null)
                ShowPatch(patches++, item.Rect, blank);
            else if (item.Kind == FormItemKind.Emblem && seal != null)
                ShowMark(_emblems, emblems++, textsRoot, item.Rect, EmblemArt.Sprite(item.Text), cover);
            else if (item.Kind == FormItemKind.Watermark && seal != null)
                ShowWatermark(watermarks++, item, cover);
            else if (item.Kind == FormItemKind.Seal && seal != null && Seals.TryParse(item.Text, out Seal mark))
                ShowSeal(seals++, item.Rect, mark);
            else if (item.Kind == FormItemKind.Seal && seal != null)
            {
                Place(seal.rectTransform, item.Rect);
                seal.color = style.seal;
                sealShown = true;
            }
            else if (item.Kind == FormItemKind.Photo && photoFrame != null)
            {
                Place(photoFrame, item.Rect);
                photoShown = true;
            }
        }
        for (int i = texts; i < _texts.Count; i++)
            _texts[i].gameObject.SetActive(false);
        for (int i = seals; i < _seals.Count; i++)
            _seals[i].mark.gameObject.SetActive(false);
        for (int i = emblems; i < _emblems.Count; i++)
            _emblems[i].gameObject.SetActive(false);
        for (int i = watermarks; i < _watermarks.Count; i++)
            _watermarks[i].gameObject.SetActive(false);
        for (int i = patches; i < _patches.Count; i++)
            _patches[i].gameObject.SetActive(false);
        if (seal != null)
            seal.gameObject.SetActive(sealShown);
        if (photoFrame != null)
            photoFrame.gameObject.SetActive(photoShown);
        if (photoFrameArt != null)
            photoFrameArt.gameObject.SetActive(photoShown && photoFrameArt.sprite != null);

        _quads = FormPaint.Quads(_form, palette, style.metrics);
        if (fills != null)
            fills.Set(_quads, FormPaintLayer.Fill);
        DrawLines();
        ArmSlots(pickable, linkHint, cellLinkHint);
        MarkFound(-1);
        Relight();
        return _form;
    }

    /// <summary>Arms the placed form's slots: a button over each slot <paramref name="pickable"/> accepts (every slot when null), a ↗ on each <paramref name="linkHint"/> hints and on each table cell <paramref name="cellLinkHint"/> hints; the pooled parts left over hide.</summary>
    private void ArmSlots(Func<FormSlot, bool> pickable, Func<FormSlot, string> linkHint, Func<FormSlot, int, string> cellLinkHint)
    {
        int parts = 0, links = 0;
        for (int s = 0; s < _form.Slots.Count; s++)
        {
            FormSlot slot = _form.Slots[s];
            if (slot.Hidden)
                continue;
            if (pickable == null || pickable(slot))
                Arm(parts++, s);
            string hint = linkHint != null ? linkHint(slot) : null;
            if (hint != null)
                ArmLink(links++, s, -1, hint, slot.Hit);
            if (cellLinkHint == null || slot.Row < 0)
                continue;
            int cell = 0;
            foreach (FormItem item in _form.Items)
            {
                if (item.Kind != FormItemKind.Text || item.Slot != s || item.Role != FormTextRole.Cell)
                    continue;
                string cellHint = cellLinkHint(slot, cell);
                if (cellHint != null)
                    ArmLink(links++, s, cell, cellHint, item.Rect);
                cell++;
            }
        }
        for (int i = parts; i < _parts.Count; i++)
            _parts[i].Button.gameObject.SetActive(false);
        for (int i = links; i < _links.Count; i++)
            _links[i].Button.gameObject.SetActive(false);
    }

    /// <summary>Outlines slot <paramref name="slot"/>'s box (the one a link went to); -1 hides the mark.</summary>
    public void MarkFound(int slot)
    {
        if (found == null)
            return;
        bool on = _form != null && slot >= 0 && slot < _form.Slots.Count;
        if (on)
        {
            Place(found, _form.Slots[slot].Hit);
            found.SetAsLastSibling();
        }
        if (found.gameObject.activeSelf != on)
            found.gameObject.SetActive(on);
    }

    /// <summary>
    /// The form's art when delivered (redesign phase 27, ArtSlots): a
    /// document's face by <paramref name="formNumber"/> (a passport's for its
    /// holder's nation, <paramref name="issuer"/>, first; then its kind's, else
    /// the agency's plain face) on the paper, the agency seal's on the seal, the
    /// photo frame's over the photo. A missing file keeps a plain paper in
    /// <paramref name="tint"/> (the look's, else the style's), its corners
    /// rounded by <paramref name="corner"/> (a card's; the travel documents
    /// spec, TD1), the code-drawn ring and no frame; a page kind (no number)
    /// keeps the plain paper.
    /// </summary>
    private void ShowArt(string formNumber, string issuer, Color tint, float corner, bool onArt, bool clean)
    {
        if (paper != null)
        {
            Sprite face = formNumber != null ? SlotArt.Sprite(ArtSlots.PaperFaces(formNumber, issuer, clean).ToArray()) : null;
            bool rounded = face == null && corner > 0f;
            paper.sprite = face != null ? face : rounded ? CardPaper() : null;
            paper.type = rounded ? Image.Type.Sliced : Image.Type.Simple;
            paper.pixelsPerUnitMultiplier = rounded ? CardPaperCorner / corner : 1f;
            paper.color = face != null ? Color.white : tint;
        }
        if (seal != null)
        {
            if (_sealRing == null)
                _sealRing = seal.sprite;
            seal.sprite = SlotArt.Sprite(ArtSlots.AgencySeal) ?? _sealRing;
        }
        if (photoFrameArt != null)
            photoFrameArt.sprite = SlotArt.Sprite(onArt ? ArtSlots.PhotoHolo : ArtSlots.PhotoFrame);
    }

    /// <summary>
    /// Patch <paramref name="index"/> (FormItemKind.Patch; ArtSlots.PaperBlank):
    /// a pooled clone of the seal image just over the paper showing the piece
    /// of <paramref name="blank"/> under <paramref name="rect"/>, so a field not
    /// introduced yet shows no label on its art; hidden without the blank face.
    /// </summary>
    private void ShowPatch(int index, FaceRect rect, Texture2D blank)
    {
        if (index >= _patches.Count)
        {
            Image clone = Instantiate(seal, seal.transform.parent);
            clone.name = "Patch";
            clone.raycastTarget = false;
            _patches.Add(clone);
        }
        Image image = _patches[index];
        image.gameObject.SetActive(blank != null);
        if (blank == null)
            return;
        var piece = new Rect(rect.XMin / _form.Width * blank.width, (1f - rect.YMax / _form.PageHeight) * blank.height,
                             rect.Width / _form.Width * blank.width, rect.Height / _form.PageHeight * blank.height);
        if (!PatchSprites.TryGetValue((blank.GetInstanceID(), piece), out Sprite sprite) || sprite == null)
        {
            sprite = UnityEngine.Sprite.Create(blank, piece, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.name = "Patch";
            PatchSprites[(blank.GetInstanceID(), piece)] = sprite;
        }
        image.sprite = sprite;
        image.preserveAspect = false;
        image.color = Color.white;
        Place(image.rectTransform, rect);
        image.transform.SetSiblingIndex(paper != null && paper.transform.parent == image.transform.parent ? paper.transform.GetSiblingIndex() + 1 : 0);
    }

    /// <summary>The Analysis Scanner's marks (the PC redesign SC4): a dashed outline in the style's analysis colour over the box of each field in <paramref name="fields"/> (null or empty: none), over the form's own lines; the next Show drops them.</summary>
    public void SetMarks(IReadOnlyList<int> fields)
    {
        _marks.Clear();
        if (fields != null)
            _marks.AddRange(fields);
        DrawLines();
    }

    /// <summary>The lines layer: the form's outlines, rules, bars, ticks and dash, then the analysis marks over them.</summary>
    private void DrawLines()
    {
        if (lines == null)
            return;
        if (_marks.Count == 0 || _form == null)
        {
            lines.Set(_quads, FormPaintLayer.Line);
            return;
        }
        var all = new List<FormQuad>(_quads);
        all.AddRange(FormPaint.MarkQuads(_form, _marks, FormStyleSO.Rgb(style.analysis), style.metrics));
        lines.Set(all, FormPaintLayer.Line);
    }

    /// <summary>
    /// Named seal <paramref name="index"/> (the document design spec, D4): a
    /// pooled clone of the seal image over <paramref name="rect"/>, its
    /// outline (SealArt) in its ink, and its legend in bold capitals over it
    /// (a pooled text of the template), both among the texts, so a seal in a
    /// box (the Seal Register's) is drawn over the box's fill.
    /// </summary>
    private void ShowSeal(int index, FaceRect rect, Seal mark)
    {
        if (index >= _seals.Count)
        {
            Image image = Instantiate(seal, textsRoot != null ? textsRoot : seal.transform.parent);
            image.name = "OfficeSeal";
            image.raycastTarget = false;
            TextMeshProUGUI legend = Instantiate(textTemplate, textsRoot != null ? textsRoot : textTemplate.transform.parent);
            legend.name = "SealLegend";
            legend.raycastTarget = false;
            _seals.Add((image, legend));
        }
        (Image markImage, TextMeshProUGUI text) = _seals[index];
        markImage.gameObject.SetActive(true);
        markImage.sprite = SealArt.Sprite(mark.Shape);
        markImage.color = SealArt.Ink(mark.Ink);
        Place(markImage.rectTransform, rect);
        text.gameObject.SetActive(true);
        _measure.SetFont(text, mark.Legend);
        TmpFormText.Style(text, FormTextRole.Title, rect.Height * SealArt.LegendShare);
        text.text = mark.Legend;
        text.color = SealArt.Ink(mark.Ink);
        text.alignment = TextAlignmentOptions.Center;
        Place(text.rectTransform, rect);
    }

    /// <summary>Pooled image <paramref name="index"/> of <paramref name="pool"/> (a clone of the seal under <paramref name="parent"/>) showing <paramref name="sprite"/> in <paramref name="ink"/> over <paramref name="rect"/>; hidden without a sprite.</summary>
    private Image ShowMark(List<Image> pool, int index, Transform parent, FaceRect rect, Sprite sprite, Color ink)
    {
        if (index >= pool.Count)
        {
            Image clone = Instantiate(seal, parent != null ? parent : seal.transform.parent);
            clone.name = pool == _emblems ? "Emblem" : "Watermark";
            clone.raycastTarget = false;
            pool.Add(clone);
        }
        Image image = pool[index];
        image.gameObject.SetActive(sprite != null);
        image.sprite = sprite;
        image.preserveAspect = true;
        image.color = ink;
        Place(image.rectTransform, rect);
        return image;
    }

    /// <summary>
    /// Watermark <paramref name="index"/> (the travel documents spec, TD1):
    /// a seal's outline in its ink, or an emblem in <paramref name="cover"/>,
    /// faint (EmblemArt.WatermarkAlpha), just over the fills (so the boxes
    /// never hide it) and under the slots' tints, the lines and the texts.
    /// </summary>
    private void ShowWatermark(int index, FormItem item, Color cover)
    {
        bool isSeal = Seals.TryParse(item.Text, out Seal mark);
        Sprite sprite = isSeal ? SealArt.Sprite(mark.Shape) : EmblemArt.Sprite(item.Text);
        Color ink = isSeal ? SealArt.Ink(mark.Ink) : cover;
        ink.a = EmblemArt.WatermarkAlpha;
        Image image = ShowMark(_watermarks, index, seal.transform.parent, item.Rect, sprite, ink);
        image.transform.SetSiblingIndex((fills != null ? fills.transform : seal.transform).GetSiblingIndex() + 1);
    }

    /// <summary>A card's paper: a white rounded square, 9-sliced at its corners, painted once.</summary>
    private static Sprite CardPaper()
    {
        if (_cardPaper != null)
            return _cardPaper;
        var texture = new Texture2D(CardPaperSize, CardPaperSize, TextureFormat.RGBA32, false) { name = "CardPaper", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var pixels = new Color32[CardPaperSize * CardPaperSize];
        float r = CardPaperCorner;
        for (int y = 0; y < CardPaperSize; y++)
            for (int x = 0; x < CardPaperSize; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, r, CardPaperSize - r), cy = Mathf.Clamp(y + 0.5f, r, CardPaperSize - r);
                float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                pixels[y * CardPaperSize + x] = new Color32(255, 255, 255, (byte)(255f * Mathf.Clamp01(r + 0.5f - d)));
            }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        _cardPaper = UnityEngine.Sprite.Create(texture, new Rect(0f, 0f, CardPaperSize, CardPaperSize), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                                               new Vector4(CardPaperCorner, CardPaperCorner, CardPaperCorner, CardPaperCorner));
        _cardPaper.name = "CardPaper";
        return _cardPaper;
    }

    /// <summary>Shows the traveller's photo in the photo cell (a null look empties it).</summary>
    public void ShowPhoto(TravellerLook look, CharacterArt art)
    {
        if (photo == null)
            return;
        if (look != null)
            photo.Show(look, art);
        else
            photo.Clear();
    }

    /// <summary>The hidden measuring text goes with the view, and the picks stop being heard.</summary>
    private void OnDestroy()
    {
        if (_measureText != null)
            Destroy(_measureText.gameObject);
        if (_compare != null)
            _compare.PicksChanged -= Relight;
    }

    /// <summary>Each armed box's pick from its key (Bind), and its tint. A view destroyed before it ever woke (a copy never shown) gets no OnDestroy: it stops listening here.</summary>
    private void Relight()
    {
        if (this == null)
        {
            _compare.PicksChanged -= Relight;
            return;
        }
        foreach (SlotPart part in _parts)
        {
            if (!part.Button.gameObject.activeSelf)
                continue;
            part.Picked = _compare != null && _keyOf != null && _form != null && part.Slot < _form.Slots.Count && _compare.IsPicked(_keyOf(_form.Slots[part.Slot]));
            ApplyTint(part);
        }
    }

    /// <summary>The box under the pointer tints (a pickable slot's).</summary>
    public void OnPointerMove(PointerEventData eventData) => SetHovered(PartUnder(eventData));

    /// <summary>The pointer left the form: no box tints.</summary>
    public void OnPointerExit(PointerEventData eventData) => SetHovered(null);

    /// <summary>
    /// The text the layout measures with: a hidden, active object of its own
    /// (awake at once, so TextMeshPro measures in canvas units) in the
    /// template's font and material, made on the first Show.
    /// </summary>
    private TextMeshProUGUI MeasureText()
    {
        var go = new GameObject("FormMeasure") { hideFlags = HideFlags.HideAndDontSave };
        _measureText = go.AddComponent<TextMeshProUGUI>();
        _measureText.font = textTemplate.font;
        _measureText.fontSharedMaterial = textTemplate.fontSharedMaterial;
        _measureText.raycastTarget = false;
        return _measureText;
    }

    /// <summary>Prints text item <paramref name="index"/>: a pooled clone of the template in its role's style and ink, in the font it was measured in (its script's, else the template's again, whatever the clone printed before), over its rectangle.</summary>
    private void Print(int index, FormItem item, FormArt art)
    {
        if (index >= _texts.Count)
            _texts.Add(Instantiate(textTemplate, textsRoot != null ? textsRoot : textTemplate.transform.parent));
        TextMeshProUGUI text = _texts[index];
        text.gameObject.SetActive(true);
        text.name = item.Role.ToString();
        _measure.SetFont(text, item.Text);
        TmpFormText.Style(text, item.Role, item.Size);
        text.text = item.Text;
        if (art == null)
            FitWords(text, item);
        text.color = style.Ink(item.Role);
        text.alignment = item.Align == FormTextAlign.Right ? TextAlignmentOptions.TopRight
            : item.Align == FormTextAlign.Centre ? TextAlignmentOptions.Top
            : TextAlignmentOptions.TopLeft;
        if (art != null)
            TmpFormText.OnArt(text, item, style, art, _measureText);
        Place(text.rectTransform, item.Rect);
    }

    /// <summary>Shrinks printed <paramref name="text"/> (already styled as <paramref name="item"/>) just enough that its widest word fits the item's box, so a word wraps whole and never breaks in the middle; the box's place and size stay the layout's (the document design spec D2). Measured on the hidden measure text (a form is often filled while inactive).</summary>
    private void FitWords(TMP_Text text, FormItem item)
    {
        if (_measureText == null)
            return;
        _measure.SetFont(_measureText, item.Text);
        text.fontSize *= TmpFormText.WordFit(_measureText, item.Text, item.Role, item.Size, item.Rect.Width);
    }

    /// <summary>Arms pooled button <paramref name="index"/> over slot <paramref name="slot"/>'s box, clear and unpicked.</summary>
    private void Arm(int index, int slot)
    {
        if (index >= _parts.Count)
        {
            Button button = Instantiate(slotTemplate, slotsRoot != null ? slotsRoot : slotTemplate.transform.parent);
            var part = new SlotPart { Button = button, Tint = button.GetComponent<Image>() };
            button.onClick.AddListener(() => Click(part));
            _parts.Add(part);
        }
        SlotPart p = _parts[index];
        p.Slot = slot;
        p.Picked = false;
        p.Button.name = $"Slot_{slot}";
        p.Button.gameObject.SetActive(true);
        Place((RectTransform)p.Button.transform, _form.Slots[slot].Hit);
        ApplyTint(p);
    }

    /// <summary>A slot's button was clicked: SlotClicked with the slot.</summary>
    private void Click(SlotPart part)
    {
        if (_form != null && part.Slot >= 0 && part.Slot < _form.Slots.Count)
            SlotClicked?.Invoke(_form.Slots[part.Slot]);
    }

    /// <summary>Arms pooled ↗ <paramref name="index"/> at the top right of <paramref name="box"/> (slot <paramref name="slot"/>'s box, or one of its cells: <paramref name="cell"/>, -1 for the box), with its hover hint.</summary>
    private void ArmLink(int index, int slot, int cell, string hint, FaceRect box)
    {
        if (linkTemplate == null)
            return;
        if (index >= _links.Count)
        {
            Button button = Instantiate(linkTemplate, slotsRoot != null ? slotsRoot : linkTemplate.transform.parent);
            var part = new LinkPart { Button = button, Hint = button.GetComponentInChildren<TMP_Text>(true) };
            button.onClick.AddListener(() => FollowLink(part));
            _links.Add(part);
        }
        LinkPart p = _links[index];
        p.Slot = slot;
        p.Cell = cell;
        p.Button.name = cell < 0 ? $"Link_{slot}" : $"Link_{slot}_{cell}";
        if (p.Hint != null)
            p.Hint.text = hint;
        float side = Mathf.Min(linkSize, box.Width, box.Height);
        Place((RectTransform)p.Button.transform, new FaceRect(box.XMax - side, box.YMin, box.XMax, box.YMin + side));
        p.Button.transform.SetAsLastSibling();
        p.Button.gameObject.SetActive(true);
    }

    /// <summary>A ↗ was clicked: LinkClicked with its slot (CellLinkClicked with the slot and the cell for a cell's).</summary>
    private void FollowLink(LinkPart part)
    {
        if (_form == null || part.Slot < 0 || part.Slot >= _form.Slots.Count)
            return;
        if (part.Cell >= 0)
            CellLinkClicked?.Invoke(_form.Slots[part.Slot], part.Cell);
        else
            LinkClicked?.Invoke(_form.Slots[part.Slot]);
    }

    /// <summary>The armed part whose slot holds the pointer (FormLayout.SlotAt on the form), or null.</summary>
    private SlotPart PartUnder(PointerEventData eventData)
    {
        var rt = (RectTransform)transform;
        if (_form == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, eventData.position, eventData.enterEventCamera, out Vector2 local))
            return null;
        Rect r = rt.rect;
        int slot = FormLayout.SlotAt(_form, local.x - r.xMin, r.yMax - local.y);
        foreach (SlotPart p in _parts)
            if (p.Button.gameObject.activeSelf && p.Slot == slot)
                return p;
        return null;
    }

    /// <summary>Tints the hovered box (the old one back).</summary>
    private void SetHovered(SlotPart part)
    {
        if (part == _hovered)
            return;
        SlotPart old = _hovered;
        _hovered = part;
        if (old != null)
            ApplyTint(old);
        if (part != null)
            ApplyTint(part);
    }

    /// <summary>A box's tint: the compare's highlight while its key is picked, else the hover tint while hovered, else clear.</summary>
    private void ApplyTint(SlotPart part)
    {
        if (part.Tint != null)
            part.Tint.color = part.Picked && _compare != null ? _compare.HighlightColor : part == _hovered ? style.hoverTint : Color.clear;
    }

    /// <summary>Puts <paramref name="rt"/> over a form-space rectangle (from the form's top-left, y down).</summary>
    private static void Place(RectTransform rt, FaceRect r)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(r.XMin, -r.YMin);
        rt.sizeDelta = new Vector2(r.Width, r.Height);
    }
}
