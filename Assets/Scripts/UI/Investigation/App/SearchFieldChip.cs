using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Investigation app's search field's paste (the PC redesign CP3, SE5):
/// the field pastes the system clipboard itself (Ctrl+V), and when what it
/// got holds the desktop clipboard's untranslated line (a foreign clip) the
/// line leaves the text and shows as a chip at the field's right end instead
/// ("Nikias · line 7 · untranslated Greek", ✕ removes it), so search can
/// match it glyph to glyph without ever showing its English; a plain clip
/// stays plain text. Escape's ClearSearch clears the text and the chip.
/// Search reads the chip (redesign phase 19).
/// </summary>
public sealed class SearchFieldChip : MonoBehaviour
{
    /// <summary>The search field.</summary>
    [SerializeField] private TMP_InputField field;

    /// <summary>The chip (inactive until a foreign clip is pasted).</summary>
    [SerializeField] private GameObject chip;

    /// <summary>The chip's text.</summary>
    [SerializeField] private TMP_Text chipLabel;

    /// <summary>The chip's ✕.</summary>
    [SerializeField] private Button removeButton;

    /// <summary>The app (its clipboard).</summary>
    [SerializeField] private InvestigationApp app;

    private RectTransform _viewport;
    private float _viewportRight;
    private bool _ready;

    /// <summary>The pasted foreign clip, or null.</summary>
    public Clip Chip { get; private set; }

    private void Awake() => Init();

    /// <summary>Clears the typed text and the chip (Escape's ClearSearch).</summary>
    public void Clear()
    {
        Init();
        ShowChip(null);
        if (field != null)
            field.text = string.Empty;
    }

    /// <summary>Wires the field and the chip once.</summary>
    private void Init()
    {
        if (_ready)
            return;
        _ready = true;
        if (field != null)
        {
            field.onValueChanged.AddListener(Changed);
            _viewport = field.textViewport;
            if (_viewport != null)
                _viewportRight = _viewport.offsetMax.x;
        }
        if (removeButton != null)
            removeButton.onClick.AddListener(() => ShowChip(null));
        if (chip != null)
            chip.SetActive(false);
    }

    /// <summary>The field's text changed: a pasted foreign clip leaves the text for the chip.</summary>
    private void Changed(string text)
    {
        Clip clip = app != null ? app.Clipboard.Current : null;
        if (clip == null || !clip.Foreign || string.IsNullOrEmpty(text) || !text.Contains(clip.Text))
            return;
        field.SetTextWithoutNotify(text.Replace(clip.Text, string.Empty).Trim());
        ShowChip(clip);
    }

    /// <summary>Shows the chip for <paramref name="clip"/> (null hides it), the text area narrowed beside it.</summary>
    private void ShowChip(Clip clip)
    {
        Chip = clip;
        if (chip != null)
            chip.SetActive(clip != null);
        if (chipLabel != null)
            chipLabel.text = clip != null ? UiText.Format("search.chip", clip.SourceLabel, clip.TongueName) : string.Empty;
        if (_viewport != null && chip != null)
            _viewport.offsetMax = new Vector2(_viewportRight - (clip != null ? ((RectTransform)chip.transform).rect.width : 0f), _viewport.offsetMax.y);
    }
}
