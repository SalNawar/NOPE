using System.Collections.Generic;
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
/// the same pick as the held paper's box) and the box under the pointer tints.
/// Every value shows in English, as filled (TR1). Each pickable box is marked
/// with its field's key for the keys, the copy and the pins (AppRow: the
/// field's label and value), in the form's reading order.
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
    private readonly List<(FormSlot slot, Button button)> _armed = new List<(FormSlot, Button)>();

    /// <summary>The document's index in the case (its fields' pick keys).</summary>
    private int _index;
    private CompareController _compare;

    private void Awake()
    {
        if (form != null)
            form.SlotClicked += Pick;
    }

    private void OnDestroy()
    {
        if (form != null)
            form.SlotClicked -= Pick;
    }

    /// <summary>
    /// Binds document <paramref name="index"/> of the case: draws
    /// <paramref name="paper"/> (the form its desk paper prints) with the
    /// traveller's photo when it has one, scrolled to its top.
    /// </summary>
    public void SetDocument(DocumentInstance doc, int index, DocumentForm paper, CompareController compare, TravellerLook look, CharacterArt art)
    {
        _doc = doc;
        _index = index;
        _compare = compare;

        if (titleText != null)
            titleText.text = doc != null ? doc.DisplayName : UiText.Get("document.untitled");

        if (form != null && paper != null)
        {
            form.Show(paper.Spec, paper.Data, slot => slot.Field >= 0 && doc != null && slot.Field < doc.fields.Count && doc.fields[slot.Field] != null);
            form.ShowPhoto(paper.Data.HasPhoto ? look : null, art);
            MarkBoxes();
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

    /// <summary>Marks each pickable box with its field's pick key and label (AppRow), as the box's click picks it.</summary>
    private void MarkBoxes()
    {
        form.ArmedSlots(_armed);
        foreach ((FormSlot slot, Button button) in _armed)
        {
            DocumentField field = _doc.fields[slot.Field];
            ComparePick pick = EvidencePicks.ForField(_index, new DocumentRow(slot.Field, field), _doc.DisplayName);
            AppRow.Mark(button.gameObject, AppTab.Documents, pick.Key, pick.Label, field.label, field.value, button);
        }
    }

    /// <summary>A box was clicked: its field goes into the compare, lit on this copy.</summary>
    private void Pick(FormSlot slot, ICompareHighlight highlight)
    {
        if (_compare == null || _doc == null || slot.Field < 0 || slot.Field >= _doc.fields.Count || _doc.fields[slot.Field] == null)
            return;
        _compare.Select(EvidencePicks.ForField(_index, new DocumentRow(slot.Field, _doc.fields[slot.Field]), _doc.DisplayName), highlight);
    }
}
