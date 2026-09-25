using TMPro;
using UnityEngine;

/// <summary>
/// The dark backing behind a floating note in the office (the day-1 hints:
/// "Click the traveller to talk and ask for papers." and the scan note), so
/// its light text reads over the pale morning crowds and the evening palette
/// alike: a quad just behind the text, fitted to the text's drawn bounds plus
/// a margin whenever the text changes. The quad's material (unlit, dark,
/// translucent, drawn before the text) and the margin come from Build Office UI.
/// </summary>
public sealed class NoteBacking : MonoBehaviour
{
    /// <summary>The note's text.</summary>
    [SerializeField] private TMP_Text text;

    /// <summary>The backing quad (a child of the note, facing the viewer with it).</summary>
    [SerializeField] private Transform backing;

    /// <summary>The margin around the text's bounds, in the note's local units (metres).</summary>
    [SerializeField] private Vector2 margin = new Vector2(0.03f, 0.015f);

    /// <summary>How far behind the text the quad sits (metres).</summary>
    [SerializeField] private float depth = 0.002f;

    /// <summary>The text the quad was last fitted to.</summary>
    private string _fitted;

    private void OnEnable() => Fit();

    /// <summary>Refits when the text changes (the notes are written once, at load).</summary>
    private void LateUpdate()
    {
        if (text != null && !ReferenceEquals(text.text, _fitted))
            Fit();
    }

    /// <summary>Sizes and places the quad over the text's drawn bounds plus the margin (hidden for an empty text).</summary>
    private void Fit()
    {
        if (text == null || backing == null)
            return;

        _fitted = text.text;
        text.ForceMeshUpdate();
        Bounds bounds = text.textBounds;
        bool shown = !string.IsNullOrWhiteSpace(_fitted) && bounds.size.x > 0f && bounds.size.y > 0f;
        backing.gameObject.SetActive(shown);
        if (!shown)
            return;

        backing.localPosition = new Vector3(bounds.center.x, bounds.center.y, depth);
        backing.localRotation = Quaternion.identity;
        backing.localScale = new Vector3(bounds.size.x + 2f * margin.x, bounds.size.y + 2f * margin.y, 1f);
    }
}
