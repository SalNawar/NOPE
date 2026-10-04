using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One document on the shelf: its group's ui string key, its name, where it opens and whether it can be read yet.</summary>
public readonly struct ShelfItem
{
    /// <summary>A document.</summary>
    public ShelfItem(string groupKey, string label, LinkTarget target, bool available)
    {
        GroupKey = groupKey;
        Label = label;
        Target = target;
        Available = available;
    }

    /// <summary>Its group's ui string key ("shelf.papers").</summary>
    public string GroupKey { get; }

    /// <summary>Its name as the chip shows it.</summary>
    public string Label { get; }

    /// <summary>Where it opens (its source and item).</summary>
    public LinkTarget Target { get; }

    /// <summary>False for a paper not scanned yet: the chip is dimmed and opening it shows why.</summary>
    public bool Available { get; }
}

/// <summary>
/// The document shelf (the PC workbench spec IA4, IA5; lesson D5: every
/// document is held against every other the same way): the documents in
/// their groups (a group's label in small capitals, kept on the row of its
/// first document, then a chip per document), wrapping onto as many rows as
/// they need (FlowLayoutGroup; the shelf's height follows and HeightChanged
/// tells the app, whose work area takes the rest). A chip reads the
/// document's name, the side it is open on ("Left", "Right") and a dot until
/// it was first opened (or while something new waits in it); a paper not
/// readable yet is dimmed. A click opens the document on the target side
/// (Opened; the app swaps it with the other side when it is open there). The
/// app says what is on the shelf (Show) and marks it (Mark).
/// </summary>
public sealed class ShelfView : MonoBehaviour
{
    /// <summary>The flow the labels and chips wrap in.</summary>
    [SerializeField] private RectTransform flow;

    /// <summary>A group's label (inactive; FlowKeepWithNext), cloned before its first document.</summary>
    [SerializeField] private TMP_Text labelTemplate;

    /// <summary>A chip (inactive): its Label, its Unread dot, its Side plate with its Text.</summary>
    [SerializeField] private Button chipTemplate;

    /// <summary>The shelf's padding above and below its rows.</summary>
    [SerializeField, Min(0f)] private float padding = 16f;

    private readonly List<ShelfItem> _items = new List<ShelfItem>();
    private readonly List<Button> _chips = new List<Button>();
    private readonly List<GameObject> _made = new List<GameObject>();
    private float _height = -1f;

    /// <summary>Raised when a chip is clicked.</summary>
    public event Action<ShelfItem> Opened;

    /// <summary>Raised when the shelf's height changes (the rows it needs).</summary>
    public event Action<float> HeightChanged;

    /// <summary>The shelf's height (its rows and padding).</summary>
    public float Height => _height;

    /// <summary>The chips shown, in order (the keys' Shelf region).</summary>
    public IReadOnlyList<Button> Chips => _chips;

    /// <summary>The documents shown, in order.</summary>
    public IReadOnlyList<ShelfItem> Items => _items;

    /// <summary>Puts <paramref name="items"/> on the shelf in their groups (in the order given; a group's label before its first document) and lays the rows out.</summary>
    public void Show(IReadOnlyList<ShelfItem> items)
    {
        if (labelTemplate != null)
            labelTemplate.gameObject.SetActive(false);
        if (chipTemplate != null)
            chipTemplate.gameObject.SetActive(false);
        foreach (GameObject made in _made)
            if (made != null)
            {
                made.SetActive(false); // out of this frame's layout (Destroy waits for the frame's end)
                Destroy(made);
            }
        _made.Clear();
        _chips.Clear();
        _items.Clear();
        if (items != null)
            _items.AddRange(items);

        string group = null;
        for (int i = 0; i < _items.Count; i++)
        {
            ShelfItem item = _items[i];
            if (item.GroupKey != group && labelTemplate != null)
            {
                TMP_Text label = Instantiate(labelTemplate, flow);
                label.gameObject.name = "Group_" + item.GroupKey;
                label.text = UiText.Get(item.GroupKey);
                label.gameObject.SetActive(true);
                _made.Add(label.gameObject);
                group = item.GroupKey;
            }
            Button chip = Instantiate(chipTemplate, flow);
            chip.gameObject.name = "Chip_" + i;
            chip.gameObject.SetActive(true);
            chip.transform.Find("Label").GetComponent<TMP_Text>().text = item.Label;
            if (chip.TryGetComponent(out CanvasGroup dim))
                dim.alpha = item.Available ? 1f : 0.55f;
            int index = i;
            chip.onClick.AddListener(() => Opened?.Invoke(_items[index]));
            _chips.Add(chip);
            _made.Add(chip.gameObject);
        }
        Relayout();
    }
    /// <summary>Marks each chip: the side it is open on (<paramref name="sideOf"/>: "Left", "Right" or null) and its dot (<paramref name="unread"/>).</summary>
    public void Mark(Func<ShelfItem, string> sideOf, Func<ShelfItem, bool> unread)
    {
        for (int i = 0; i < _chips.Count && i < _items.Count; i++)
        {
            string side = sideOf != null ? sideOf(_items[i]) : null;
            Transform plate = _chips[i].transform.Find("Side");
            if (plate != null)
            {
                if (plate.gameObject.activeSelf != (side != null))
                    plate.gameObject.SetActive(side != null);
                if (side != null)
                    plate.Find("Text").GetComponent<TMP_Text>().text = side;
            }
            Transform dot = _chips[i].transform.Find("Unread");
            bool on = unread != null && unread(_items[i]);
            if (dot != null && dot.gameObject.activeSelf != on)
                dot.gameObject.SetActive(on);
        }
        Relayout();
    }

    /// <summary>The window was resized: the rows wrap again.</summary>
    private void OnRectTransformDimensionsChange()
    {
        if (isActiveAndEnabled && flow != null)
            Relayout();
    }

    /// <summary>Lays the rows out now and follows their height (HeightChanged when it changes).</summary>
    private void Relayout()
    {
        if (flow == null)
            return;
        LayoutRebuilder.ForceRebuildLayoutImmediate(flow);
        float height = LayoutUtility.GetPreferredHeight(flow) + 2f * padding;
        if (Mathf.Abs(height - _height) < 0.5f)
            return;
        _height = height;
        HeightChanged?.Invoke(height);
    }
}
