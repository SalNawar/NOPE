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
/// follows it (FormLayout.Layout): a document copy takes 542 u, a page H =
/// 708 u tall; a page kind in the Investigation app's pane takes the pane's
/// width, so its table cells reach 13 px at 720p (from about 564 u); a flow
/// page grows with its rows. The view draws that placed form: the paper (its
/// kind's face on a document when the art exists, ArtSlots.PaperFaces), the
/// seal behind the header, the fills and bands under the slots' tints and
/// every outline, rule, barcode bar, tick and the stamp area's dash over them
/// (FormPaint's quads, which the desk paper prints too, one FormStrokes graphic
/// per layer), a TextMeshPro per text cloned from one template and styled by
/// its role (TmpFormText), and the traveller's photo in its cell (under the
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
/// click raises LinkClicked; the link never picks), and MarkFound outlines
/// the box a link went to. The view sets its own height to the form's, so a
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

    /// <summary>The ↗'s side: its hit box, at the box's top right corner.</summary>
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

    /// <summary>One pooled ↗: which slot it follows, and its hover hint's text.</summary>
    private sealed class LinkPart
    {
        public Button Button;
        public TMP_Text Hint;
        public int Slot;
    }

    private readonly List<TextMeshProUGUI> _texts = new List<TextMeshProUGUI>();
    private readonly List<SlotPart> _parts = new List<SlotPart>();
    private readonly List<LinkPart> _links = new List<LinkPart>();
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

    /// <summary>The form as last placed (null before the first Show).</summary>
    public PlacedForm Placed => _form;

    /// <summary>The forms' style the view prints in.</summary>
    public FormStyleSO Style => style;

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
    /// form's. Pass a document copy 542 u and a page kind its pane's width,
    /// and Show again when the pane's width changes (a maximise). The slots
    /// <paramref name="pickable"/> accepts (every slot when null) get a button
    /// and tint under the pointer, lit while their key is picked (Bind); the
    /// slots <paramref name="linkHint"/> gives a hint get a ↗ with that hint.
    /// The found mark clears. Returns the placed form (null without a style or
    /// text template).
    /// </summary>
    public PlacedForm Show(FormSpec spec, FormData data, Func<FormSlot, bool> pickable = null, float width = 0f, Func<FormSlot, string> linkHint = null)
    {
        _hovered = null;
        _form = null;
        if (style == null || textTemplate == null)
            return null;

        var rt = (RectTransform)transform;
        if (width > 0f)
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        _measure ??= new TmpFormText(MeasureText());
        _form = FormLayout.Layout(spec, data, rt.rect.width, style.metrics, _measure);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _form.Height);
        ShowArt(spec != null && spec.fixedPage && data != null ? data.FormNumber : null);

        int texts = 0;
        bool sealShown = false, photoShown = false;
        foreach (FormItem item in _form.Items)
        {
            if (item.Kind == FormItemKind.Text)
                Print(texts++, item);
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
        if (seal != null)
            seal.gameObject.SetActive(sealShown);
        if (photoFrame != null)
            photoFrame.gameObject.SetActive(photoShown);
        if (photoFrameArt != null)
            photoFrameArt.gameObject.SetActive(photoShown && photoFrameArt.sprite != null);

        List<FormQuad> quads = FormPaint.Quads(_form, style.Palette(), style.metrics);
        if (fills != null)
            fills.Set(quads, FormPaintLayer.Fill);
        if (lines != null)
            lines.Set(quads, FormPaintLayer.Line);

        int parts = 0, links = 0;
        for (int s = 0; s < _form.Slots.Count; s++)
        {
            if (pickable == null || pickable(_form.Slots[s]))
                Arm(parts++, s);
            string hint = linkHint != null ? linkHint(_form.Slots[s]) : null;
            if (hint != null)
                ArmLink(links++, s, hint);
        }
        for (int i = parts; i < _parts.Count; i++)
            _parts[i].Button.gameObject.SetActive(false);
        for (int i = links; i < _links.Count; i++)
            _links[i].Button.gameObject.SetActive(false);
        MarkFound(-1);
        Relight();
        return _form;
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
    /// document's face by <paramref name="formNumber"/> (its kind's, else the
    /// agency's plain face) on the paper, the agency seal's on the seal, the
    /// photo frame's over the photo. A missing file keeps the style's plain
    /// paper, the code-drawn ring and no frame; a page kind (no number) keeps
    /// the plain paper.
    /// </summary>
    private void ShowArt(string formNumber)
    {
        if (paper != null)
        {
            Sprite face = formNumber != null ? SlotArt.Sprite(ArtSlots.PaperFaces(formNumber).ToArray()) : null;
            paper.sprite = face;
            paper.color = face != null ? Color.white : style.paper;
        }
        if (seal != null)
        {
            if (_sealRing == null)
                _sealRing = seal.sprite;
            seal.sprite = SlotArt.Sprite(ArtSlots.AgencySeal) ?? _sealRing;
        }
        if (photoFrameArt != null)
            photoFrameArt.sprite = SlotArt.Sprite(ArtSlots.PhotoFrame);
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

    /// <summary>Prints text item <paramref name="index"/>: a pooled clone of the template in its role's style and ink, over its rectangle.</summary>
    private void Print(int index, FormItem item)
    {
        if (index >= _texts.Count)
        {
            TextMeshProUGUI clone = Instantiate(textTemplate, textsRoot != null ? textsRoot : textTemplate.transform.parent);
            _texts.Add(clone);
        }
        TextMeshProUGUI text = _texts[index];
        text.gameObject.SetActive(true);
        text.name = item.Role.ToString();
        TmpFormText.Style(text, item.Role, item.Size);
        text.text = item.Text;
        text.color = style.Ink(item.Role);
        text.alignment = item.Align == FormTextAlign.Right ? TextAlignmentOptions.TopRight
            : item.Align == FormTextAlign.Centre ? TextAlignmentOptions.Top
            : TextAlignmentOptions.TopLeft;
        Place(text.rectTransform, item.Rect);
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

    /// <summary>Arms pooled ↗ <paramref name="index"/> at slot <paramref name="slot"/>'s box's top right, with its hover hint.</summary>
    private void ArmLink(int index, int slot, string hint)
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
        p.Button.name = $"Link_{slot}";
        if (p.Hint != null)
            p.Hint.text = hint;
        FaceRect box = _form.Slots[slot].Hit;
        float side = Mathf.Min(linkSize, box.Width, box.Height);
        Place((RectTransform)p.Button.transform, new FaceRect(box.XMax - side, box.YMin, box.XMax, box.YMin + side));
        p.Button.transform.SetAsLastSibling();
        p.Button.gameObject.SetActive(true);
    }

    /// <summary>A ↗ was clicked: LinkClicked with its slot.</summary>
    private void FollowLink(LinkPart part)
    {
        if (_form != null && part.Slot >= 0 && part.Slot < _form.Slots.Count)
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
