using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A paper's scanned copy on the PC (redesign phase 5, PC spec §2.4, FO1):
/// the Investigation app's Documents tab clones this page per paper
/// (DocumentsView). On the scanner's dark backing it shows the document's
/// name, the strip ("SCANNED 10:42 · DESK SCANNER 1", the shift clock's time
/// when the copy arrived), and the paper's form drawn by a FormView from the
/// same DocumentForm the desk paper prints, so the copy is the paper: its
/// pages stacked in a scroll. A
/// click on a box picks the field for the compare (EvidencePicks.ForField,
/// the same pick as the held paper's box), the box under the pointer tints,
/// and a box lights while its field's key is picked, in every pane (phase
/// 18, CM3). A field with a smart link (SmartLinks.ForField: its book's
/// claimed row, the traveller's record, the Rules) has a ↗ that follows it
/// through the pane the copy is in (LK2); RevealField scrolls a field's box
/// to the middle and outlines it. Every value shows in English, as filled
/// (TR1).
/// </summary>
public sealed class DocumentWindowController : MonoBehaviour
{
    /// <summary>The document's name over the copy.</summary>
    [SerializeField] private TMP_Text titleText;

    /// <summary>The strip on the backing above the copy.</summary>
    [SerializeField] private TMP_Text scanStrip;

    /// <summary>The scroll the copy's pages stack in.</summary>
    [SerializeField] private ScrollRect scroll;

    /// <summary>The copy: the paper's form.</summary>
    [SerializeField] private FormView form;

    /// <summary>Today's shift clock (the strip's time).</summary>
    [SerializeField] private ShiftClockDriver clock;

    private DocumentInstance _doc;

    /// <summary>The document's index in the case (its fields' pick keys).</summary>
    private int _index;
    private CompareController _compare;

    /// <summary>The case's claim (the place facts' links).</summary>
    private CaseClaim _claim;

    private void Awake()
    {
        if (form == null)
            return;
        form.SlotClicked += Pick;
        form.LinkClicked += Follow;
    }

    private void OnDestroy()
    {
        if (form == null)
            return;
        form.SlotClicked -= Pick;
        form.LinkClicked -= Follow;
    }

    /// <summary>
    /// Binds document <paramref name="index"/> of the case: draws
    /// <paramref name="paper"/> (the form its desk paper prints) with the
    /// traveller's photo when it has one, scrolled to its top; its boxes light
    /// by their keys in <paramref name="compare"/> and its fields link by the
    /// case's <paramref name="claim"/>.
    /// </summary>
    public void SetDocument(DocumentInstance doc, int index, DocumentForm paper, CompareController compare, TravellerLook look, CharacterArt art, CaseClaim claim)
    {
        _doc = doc;
        _index = index;
        _compare = compare;
        _claim = claim;

        if (titleText != null)
            titleText.text = doc != null ? doc.DisplayName : UiText.Get("document.untitled");

        if (form != null && paper != null)
        {
            form.Bind(compare, slot => Field(slot) != null ? PickKeys.Field(_index, slot.Field) : null);
            form.Show(paper.Spec, paper.Data, slot => Field(slot) != null, linkHint: LinkHint);
            form.ShowPhoto(paper.Data.HasPhoto ? look : null, art);
        }
        if (scroll != null)
            scroll.verticalNormalizedPosition = 1f;
    }

    /// <summary>The copy arrived on the PC (its scan finished): the strip reads the shift clock's time now.</summary>
    public void MarkScanned()
    {
        if (scanStrip == null || form == null || form.Style == null)
            return;
        string time = clock != null && clock.Clock != null ? ShiftClock.Format(clock.Clock.CurrentMinute) : "--:--";
        scanStrip.text = string.Format(form.Style.scanStrip, time);
    }

    /// <summary>
    /// Scrolls the box of the field <paramref name="fieldKey"/> names (a Field
    /// pick key of this document) to the middle and outlines it; any other key
    /// (or null) only clears the outline.
    /// </summary>
    public void RevealField(string fieldKey)
    {
        int slot = -1;
        PlacedForm placed = form != null ? form.Placed : null;
        if (placed != null && PickKeys.TryField(fieldKey, out int document, out int field) && document == _index)
            for (int s = 0; s < placed.Slots.Count && slot < 0; s++)
                if (placed.Slots[s].Field == field)
                    slot = s;
        if (form != null)
            form.MarkFound(slot);
        if (slot < 0 || scroll == null || scroll.viewport == null)
            return;

        FaceRect box = placed.Slots[slot].Hit;
        scroll.verticalNormalizedPosition = AppPanes.ScrollToMiddle(placed.Height, scroll.viewport.rect.height, box.YMin, box.Height);
    }

    /// <summary>The field a slot shows (null for a table row or a slot past the document's fields).</summary>
    private DocumentField Field(FormSlot slot) =>
        _doc != null && slot.Field >= 0 && slot.Field < _doc.fields.Count ? _doc.fields[slot.Field] : null;

    /// <summary>A field's link (SmartLinks.ForField; None for a slot without a field).</summary>
    private LinkTarget Link(FormSlot slot)
    {
        DocumentField field = Field(slot);
        return field != null ? SmartLinks.ForField(field, _doc.fields, _claim) : LinkTarget.None;
    }

    /// <summary>The ↗'s hover hint of a slot with a link, or null (no ↗).</summary>
    private string LinkHint(FormSlot slot)
    {
        LinkTarget link = Link(slot);
        return link.IsNone ? null : AppLinks.Hint(link, Field(slot).category);
    }

    /// <summary>A box was clicked: its field goes into the compare (the box lights by its key).</summary>
    private void Pick(FormSlot slot)
    {
        DocumentField field = Field(slot);
        if (_compare == null || field == null)
            return;
        _compare.Select(EvidencePicks.ForField(_index, new DocumentRow(slot.Field, field), _doc.DisplayName), null);
    }

    /// <summary>A ↗ was clicked: its field's link, through the pane this copy is in (LK2).</summary>
    private void Follow(FormSlot slot)
    {
        LinkTarget link = Link(slot);
        AppPane pane = GetComponentInParent<AppPane>();
        if (!link.IsNone && pane != null)
            pane.FollowLink(link);
    }
}
