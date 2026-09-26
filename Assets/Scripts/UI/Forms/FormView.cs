using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// A form drawn with uGUI on the PC (redesign phase 5, PC spec FO1, §6.5): the
/// twin of the desk paper (DeskDocument). FormLayout places the form at the
/// view's width (a document at 542 u is a page H = 708 u tall; a flow page
/// grows with its rows) and the view draws that placed form: the paper, the
/// seal behind the header, the fills and bands under the slots' tints and
/// every outline, rule, barcode bar, tick and the stamp area's dash over them
/// (FormPaint's quads, which the desk paper prints too, one FormStrokes graphic
/// per layer), a TextMeshPro per text cloned from one template and styled by
/// its role (TmpFormText), and the traveller's photo in its cell. The layout
/// measures with a hidden text of the template's font that the view makes
/// for itself (TextMeshPro measures only once awake, and a window is filled
/// while still inactive). Each pickable slot gets a button over its
/// box: a click raises SlotClicked with the slot and the box as a compare
/// highlight, and the box under the pointer tints (FormLayout.SlotAt, as on
/// the desk). The view sets its own height to the form's, so a scroll rect
/// can hold it. Its parts are the builder's, tagged DiegeticForm (forms are
/// never themed), and pooled: a new Show reuses them and drops the old picks.
/// Always English (TR1): values are written as they are.
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

    /// <summary>Where the texts are cloned (spans the form).</summary>
    [SerializeField] private RectTransform textsRoot;

    /// <summary>The inactive text every printed word clones (its font and material are also the measure's).</summary>
    [SerializeField] private TextMeshProUGUI textTemplate;

    /// <summary>One pooled slot button: which slot it shows, and its pick.</summary>
    private sealed class SlotPart
    {
        public Button Button;
        public Image Tint;
        public int Slot;
        public bool Picked;
        public Color PickColour;
    }

    /// <summary>A box of the view as a compare highlight: tints it while picked; nothing once the view is gone or shows another form.</summary>
    private sealed class SlotHighlight : ICompareHighlight
    {
        private readonly FormView _view;
        private readonly SlotPart _part;
        private readonly int _generation;

        public SlotHighlight(FormView view, SlotPart part)
        {
            _view = view;
            _part = part;
            _generation = view._generation;
        }

        public void Show(bool picked, Color colour)
        {
            if (_view == null || _view._generation != _generation)
                return;
            _part.Picked = picked;
            _part.PickColour = colour;
            _view.ApplyTint(_part);
        }
    }

    private readonly List<TextMeshProUGUI> _texts = new List<TextMeshProUGUI>();
    private readonly List<SlotPart> _parts = new List<SlotPart>();
    private TmpFormText _measure;
    private TextMeshProUGUI _measureText;
    private PlacedForm _form;
    private int _generation;
    private SlotPart _hovered;

    /// <summary>Raised when a pickable slot is clicked: the slot (its field, or its table row) and its box as a compare highlight.</summary>
    public event Action<FormSlot, ICompareHighlight> SlotClicked;

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
    /// Draws <paramref name="spec"/> showing <paramref name="data"/> at the
    /// view's width and sets the view's height to the form's. The slots
    /// <paramref name="pickable"/> accepts (every slot when null) get a button
    /// and tint under the pointer. Picks of an earlier form are dropped.
    /// Returns the placed form (null without a style or text template).
    /// </summary>
    public PlacedForm Show(FormSpec spec, FormData data, Func<FormSlot, bool> pickable = null)
    {
        _generation++;
        _hovered = null;
        _form = null;
        if (style == null || textTemplate == null)
            return null;

        var rt = (RectTransform)transform;
        _measure ??= new TmpFormText(MeasureText());
        _form = FormLayout.Layout(spec, data, rt.rect.width, style.metrics, _measure);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _form.Height);
        if (paper != null)
            paper.color = style.paper;

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

        List<FormQuad> quads = FormPaint.Quads(_form, style.Palette(), style.metrics);
        if (fills != null)
            fills.Set(quads, FormPaintLayer.Fill);
        if (lines != null)
            lines.Set(quads, FormPaintLayer.Line);

        int parts = 0;
        for (int s = 0; s < _form.Slots.Count; s++)
            if (pickable == null || pickable(_form.Slots[s]))
                Arm(parts++, s);
        for (int i = parts; i < _parts.Count; i++)
            _parts[i].Button.gameObject.SetActive(false);
        return _form;
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

    /// <summary>The hidden measuring text goes with the view.</summary>
    private void OnDestroy()
    {
        if (_measureText != null)
            Destroy(_measureText.gameObject);
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

    /// <summary>A slot's button was clicked: SlotClicked with the slot and its highlight.</summary>
    private void Click(SlotPart part)
    {
        if (_form != null && part.Slot >= 0 && part.Slot < _form.Slots.Count)
            SlotClicked?.Invoke(_form.Slots[part.Slot], new SlotHighlight(this, part));
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

    /// <summary>A box's tint: the pick's colour, else the hover tint while hovered, else clear.</summary>
    private void ApplyTint(SlotPart part)
    {
        if (part.Tint != null)
            part.Tint.color = part.Picked ? part.PickColour : part == _hovered ? style.hoverTint : Color.clear;
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
