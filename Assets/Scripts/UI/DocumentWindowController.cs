using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A paper's scanned copy on the PC (redesign phase 5, PC spec §2.4, FO1):
/// the Investigation app's Documents tab clones this page per paper
/// (DocumentsView). On the scanner's dark backing it shows (under the pane's
/// header, which names the document) the strip ("SCANNED 10:42 · DESK SCANNER 1", the shift clock's time
/// when the copy arrived; after an analysis pass "ANALYSED 10:44 · 1
/// CONTRADICTION MARKED", or "ANALYSED 10:44 ·" and the MATCH tag (the
/// compare's plate, ink and word) when the scanned papers agree; after a
/// re-scan of an analysed paper "SCANNED 10:50 · ALREADY ANALYSED"; the form
/// style's words), and the paper's form drawn by a FormView in its scroll
/// (FormPage) from the same DocumentForm the desk paper prints, so the copy
/// is the paper: its pages stacked in the scroll. A
/// click on a box picks the field for the compare (EvidencePicks.ForField,
/// the same pick as the held paper's box), the box under the pointer tints,
/// and a box lights while its field's key is picked, in every pane (phase
/// 18, CM3). A field with a smart link (SmartLinks.ForField: its book's
/// claimed row, the traveller's record, the Rules) has a ↗ that follows it
/// through the pane the copy is in (LK2); RevealField scrolls a field's box
/// to the middle and outlines it. Every value shows in English, as filled
/// (TR1). Each pickable box is marked with its field's key for the keys, the
/// copy and the pins (AppRow: the field's label and value, its link for
/// Enter), in the form's reading order.
/// </summary>
public sealed class DocumentWindowController : MonoBehaviour
{
    /// <summary>The strip on the backing above the copy.</summary>
    [SerializeField] private TMP_Text scanStrip;

    /// <summary>The MATCH tag after the strip (the compare bar's plate with its MATCH ink and word), shown after an analysis pass that found the scanned papers agree.</summary>
    [SerializeField] private GameObject matchTag;

    /// <summary>The copy: the paper's form in its scroll.</summary>
    [SerializeField] private FormPage page;

    /// <summary>Today's shift clock (the strip's time).</summary>
    [SerializeField] private ShiftClockDriver clock;

    private DocumentInstance _doc;
    private readonly List<(FormSlot slot, Button button)> _armed = new List<(FormSlot, Button)>();

    /// <summary>The document's index in the case (its fields' pick keys).</summary>
    private int _index;
    private CompareController _compare;

    /// <summary>The case's claim (the place facts' links).</summary>
    private CaseClaim _claim;

    /// <summary>The copy's form (null without a page).</summary>
    private FormView Form => page != null ? page.Form : null;

    private void Awake()
    {
        if (Form == null)
            return;
        Form.SlotClicked += Pick;
        Form.LinkClicked += Follow;
        page.Redrawn += MarkBoxes;
    }

    private void OnDestroy()
    {
        if (Form == null)
            return;
        Form.SlotClicked -= Pick;
        Form.LinkClicked -= Follow;
        page.Redrawn -= MarkBoxes;
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

        if (Form != null && paper != null)
        {
            Form.Bind(compare, slot => Field(slot) != null ? PickKeys.Field(_index, slot.Field) : null);
            page.Show(paper.Spec, paper.Data, slot => Field(slot) != null, LinkHint);
            Form.ShowPhoto(ArtLayout.ShowsPhoto(paper.Spec, paper.Data) ? look : null, art);
            MarkBoxes();
        }
    }

    /// <summary>The copy arrived on the PC (its scan finished): the strip reads the shift clock's time now.</summary>
    public void MarkScanned() => Strip(Form != null && Form.Style != null ? Form.Style.scanStrip : null);

    /// <summary>An analysis pass ended on this paper (the Analysis Scanner): the strip reads the time and whether a contradicting pair was marked (<paramref name="contradiction"/>), in the form style's words; with none the MATCH tag follows it.</summary>
    public void MarkAnalysed(bool contradiction)
    {
        Strip(Form != null && Form.Style != null ? (contradiction ? Form.Style.analysedStrip : Form.Style.analysedMatchStrip) : null);
        if (matchTag != null && matchTag.activeSelf == contradiction)
            matchTag.SetActive(!contradiction);
    }

    /// <summary>This paper was scanned by hand again after its analysis pass (the pass works once per document): the strip says so, in the form style's words; the MATCH tag and the marks stay as the pass left them.</summary>
    public void MarkAlreadyAnalysed() => Strip(Form != null && Form.Style != null ? Form.Style.alreadyAnalysedStrip : null);

    /// <summary>The analysis marks on this copy: the dashed outline on each of <paramref name="fields"/>' boxes (FormView.SetMarks).</summary>
    public void SetMarks(IReadOnlyList<int> fields)
    {
        if (Form != null)
            Form.SetMarks(fields);
    }

    /// <summary>Writes the strip from <paramref name="template"/> ({0}: the shift clock's time now, "--:--" without a clock); nothing without a strip or a template.</summary>
    private void Strip(string template)
    {
        if (scanStrip == null || template == null)
            return;
        string time = clock != null && clock.Clock != null ? ShiftClock.Format(clock.Clock.CurrentMinute) : "--:--";
        scanStrip.text = string.Format(UiText.DocumentWord(template), time);
    }

    /// <summary>
    /// Scrolls the box of the field <paramref name="fieldKey"/> names (a Field
    /// pick key of this document) to the middle and outlines it; any other key
    /// (or null) only clears the outline.
    /// </summary>
    public void RevealField(string fieldKey)
    {
        int slot = -1;
        PlacedForm placed = page != null ? page.Placed : null;
        if (placed != null && PickKeys.TryField(fieldKey, out int document, out int field) && document == _index)
            for (int s = 0; s < placed.Slots.Count && slot < 0; s++)
                if (placed.Slots[s].Field == field)
                    slot = s;
        if (page != null)
            page.Reveal(slot);
    }

    /// <summary>Marks each pickable box with its field's pick key, label and link (AppRow), as the box's click picks it; again after every redraw (a copy is bound while still inactive, before Awake listens).</summary>
    private void MarkBoxes()
    {
        if (Form == null || _doc == null)
            return;
        Form.ArmedSlots(_armed);
        foreach ((FormSlot slot, Button button) in _armed)
        {
            DocumentField field = _doc.fields[slot.Field];
            ComparePick pick = EvidencePicks.ForField(_index, new DocumentRow(slot.Field, field), _doc.DisplayName);
            AppRow.Mark(button.gameObject, AppTab.Documents, pick.Key, pick.Label, field.label, pick.Shown, button).SetLink(Link(slot));
        }
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
