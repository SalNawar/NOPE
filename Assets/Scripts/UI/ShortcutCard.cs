using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The shortcut card (the PC redesign KB1, section 3.4; F1, the app's Keys
/// button, Settings' Show shortcuts): a desktop window listing the one
/// shortcut table, ShortcutMap.Card, a row per line (the keys, then what
/// they do, a ui string), filled from the table the first time it shows so
/// the card can never disagree with the keys. Escape closes it (the chain's
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
        foreach (ShortcutCardRow line in ShortcutMap.Card)
        {
            RectTransform row = Instantiate(rowTemplate, rowsRoot);
            row.gameObject.name = "Row";
            row.gameObject.SetActive(true);
            Write(row, "Keys", line.Keys);
            Write(row, "Text", UiText.Get(line.TextKey));
            _rows.Add(row.gameObject);
        }
    }

    private static void Write(Transform row, string child, string text)
    {
        Transform t = row.Find(child);
        if (t != null && t.TryGetComponent(out TMP_Text label))
            label.text = text;
    }
}
