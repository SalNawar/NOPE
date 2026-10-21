using TMPro;
using UnityEngine;

/// <summary>
/// A text the office builder printed from a UI string that no theme tag
/// re-reads (a word on the desk's hardware, the rulebook folder's tabs and
/// page heads, the stamp rail): it reads its string again when the scene
/// starts and whenever the papers' language changes
/// (CultureThemeService.LabelsChanged). Each is the desk's chrome (or the
/// PC's), so it reads the player's language on any day (D7, BUG-3a:
/// UiText.Get on its surface, UiText.SurfaceOf).
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public sealed class KeyedText : MonoBehaviour
{
    /// <summary>The UI string key.</summary>
    [SerializeField] private string key;

    /// <summary>What the text shows, its string in place of {0} (rich text around it kept: the STAMPS tab's key line).</summary>
    [SerializeField] private string template = "{0}";

    /// <summary>Sets the key and the template (Build Office UI).</summary>
    public void Configure(string labelKey, string textTemplate)
    {
        key = labelKey;
        template = string.IsNullOrEmpty(textTemplate) ? "{0}" : textTemplate;
    }

    /// <summary>Reads the string in the reading language as the scene starts.</summary>
    private void Start() => Refresh();

    /// <summary>Follows a change of the labels' language (CultureThemeService.LabelsChanged), and reads the string again whenever it shows (a change while it was hidden).</summary>
    private void OnEnable()
    {
        CultureThemeService.LabelsChanged += Refresh;
        Refresh();
    }

    /// <summary>Stops following it.</summary>
    private void OnDisable() => CultureThemeService.LabelsChanged -= Refresh;

    /// <summary>Shows the key's string in the template.</summary>
    public void Refresh()
    {
        if (string.IsNullOrEmpty(key) || !TryGetComponent(out TMP_Text text))
            return;
        text.text = template.Replace("{0}", UiText.Get(key, UiText.SurfaceOf(text))); // the chrome's and the PC's words: the player's language
    }
}
