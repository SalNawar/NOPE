using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The shortcut card (the PC redesign KB1, section 3.4; F1, the app's Keys
/// button, Settings' Show shortcuts; F1 at the desk opens the PC with it):
/// a desktop window listing the desk's keys (ControlRules.DeskCard: Papers,
/// Please's controls, the same keys the tabs print) under "At the desk", then
/// the one shortcut table, ShortcutMap.Card, under "On the PC", a row per
/// line (the keys, then what they do, a ui string), filled from the tables
/// the first time it shows so the card can never disagree with the keys. Escape closes it (the chain's
/// CloseCard), and so does F1 again.
/// </summary>
public sealed class ShortcutCard : MonoBehaviour
{
    /// <summary>Where the rows go (a vertical layout).</summary>
    [SerializeField] private RectTransform rowsRoot;

    /// <summary>A row (inactive): a "Keys" text and a "Text" text.</summary>
    [SerializeField] private RectTransform rowTemplate;

    private readonly List<GameObject> _rows = new List<GameObject>();

    private void OnEnable()
    {
        if (_rows.Count > 0 || rowsRoot == null || rowTemplate == null)
            return;
        rowTemplate.gameObject.SetActive(false);
        Add("", "<b>" + UiText.Get("keys.section.desk") + "</b>");
        foreach (ShortcutCardRow line in ControlRules.DeskCard)
            Add(line.Keys, UiText.Get(line.TextKey));
        Add("", "<b>" + UiText.Get("keys.section.pc") + "</b>");
        foreach (ShortcutCardRow line in ShortcutMap.Card)
            Add(line.Keys, UiText.Get(line.TextKey));
    }

    /// <summary>One row: <paramref name="keys"/>, then <paramref name="text"/> (a section's heading has no keys).</summary>
    private void Add(string keys, string text)
    {
        RectTransform row = Instantiate(rowTemplate, rowsRoot);
        row.gameObject.name = "Row";
        row.gameObject.SetActive(true);
        Write(row, "Keys", keys);
        Write(row, "Text", text);
        _rows.Add(row.gameObject);
    }

    private static void Write(Transform row, string child, string text)
    {
        Transform t = row.Find(child);
        if (t != null && t.TryGetComponent(out TMP_Text label))
            label.text = text;
    }
}
