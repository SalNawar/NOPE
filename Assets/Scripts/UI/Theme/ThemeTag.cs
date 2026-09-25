using TMPro;
using UnityEngine;

/// <summary>Which of a role's colours a graphic takes: an Image its fill (a glyph bar its ink); a text always its ink.</summary>
public enum ThemePart
{
    /// <summary>The role's fill.</summary>
    Fill,

    /// <summary>The role's ink.</summary>
    Ink
}

/// <summary>Which of the theme's style additions a text takes.</summary>
public enum ThemeTextKind
{
    /// <summary>Body text: no addition.</summary>
    Body,

    /// <summary>A heading (title, masthead): the theme's heading addition.</summary>
    Heading,

    /// <summary>A button label: the theme's button addition.</summary>
    Button
}

/// <summary>
/// Marks one UI graphic as themed (piece 6): its role, which of the role's
/// colours it takes, and for a text its label key, base style and kind, and
/// whether it shrinks to fit. CultureThemeService walks these in any loaded
/// scene and applies the active theme; diegetic roles are never touched.
/// The office builder stamps them on the UI it creates; UI built at runtime
/// calls Configure and then CultureThemeService.ApplyTo.
/// </summary>
[DisallowMultipleComponent]
public sealed class ThemeTag : MonoBehaviour
{
    /// <summary>The graphic's role.</summary>
    [SerializeField] private ThemeRoleId role;

    /// <summary>Which of the role's colours an Image takes.</summary>
    [SerializeField] private ThemePart part;

    /// <summary>Text only: the UI string key the theme writes; empty = the text is written at runtime by a controller.</summary>
    [SerializeField] private string labelKey;

    /// <summary>Text only: the style it was built with (the theme strips italics or adds to it).</summary>
    [SerializeField] private FontStyles baseStyle;

    /// <summary>Text only: which style addition applies.</summary>
    [SerializeField] private ThemeTextKind textKind;

    /// <summary>Text only: never wraps and shrinks from its size to CultureUiSettings.labelMinScale of it, so longer words, glosses and currencies fit.</summary>
    [SerializeField] private bool shrinkToFit;

    /// <summary>The size the text had when first themed (the fit's maximum); -1 until then.</summary>
    private float _baseSize = -1f;

    /// <summary>The graphic's role.</summary>
    public ThemeRoleId Role => role;

    /// <summary>Which of the role's colours an Image takes.</summary>
    public ThemePart Part => part;

    /// <summary>The UI string key the theme writes (empty: none).</summary>
    public string LabelKey => labelKey;

    /// <summary>The text's built style.</summary>
    public FontStyles BaseStyle => baseStyle;

    /// <summary>Which style addition applies.</summary>
    public ThemeTextKind TextKind => textKind;

    /// <summary>Whether the text shrinks to fit instead of wrapping.</summary>
    public bool ShrinkToFit => shrinkToFit;

    /// <summary>Sets the tag (the builder, and runtime-built UI such as the investigation text fallback).</summary>
    public void Configure(ThemeRoleId role, ThemePart part, string labelKey, FontStyles baseStyle, ThemeTextKind kind, bool shrinkToFit)
    {
        this.role = role;
        this.part = part;
        this.labelKey = labelKey ?? string.Empty;
        this.baseStyle = baseStyle;
        textKind = kind;
        this.shrinkToFit = shrinkToFit;
    }

    /// <summary>The size a shrink-to-fit text may grow back to: its size (or auto-size maximum) the first time it is asked.</summary>
    public float FitSize(TMP_Text text)
    {
        if (_baseSize < 0f)
            _baseSize = text.enableAutoSizing ? text.fontSizeMax : text.fontSize;
        return _baseSize;
    }
}
