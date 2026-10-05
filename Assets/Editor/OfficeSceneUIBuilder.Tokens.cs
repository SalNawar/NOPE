using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The PC's design tokens (the PC UX redesign's §3 and §4,
/// docs/superpowers/specs/2026-09-30-pc-ux-redesign.md): the type scale in
/// desktop units (1 u = 0.778 px at 1080p, 0.519 px at 720p; the smallest,
/// Caption, is the 720p floor of 12 px for a label), the 8-unit spacing grid,
/// and the helpers every PC builder sizes its chrome text with, so no chrome
/// label is drawn under the floor or cut. Colours stay the theme's (a role on
/// every graphic, coloured by the leading culture's seeds). Part of
/// <see cref="OfficeSceneUIBuilder"/>.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The type scale (desktop units).</summary>
    private static class PcType
    {
        /// <summary>Meta lines, counters, hints, placeholders, the tray: the floor (12.5 px at 720p).</summary>
        public const int Caption = 24;

        /// <summary>Navigator entries and items, list rows, buttons, menu entries, the checklist.</summary>
        public const int Body = 26;

        /// <summary>Pane titles and section headings (bold).</summary>
        public const int Title = 28;

        /// <summary>A window's page heading (bold).</summary>
        public const int Heading = 34;

        /// <summary>The idle line on the office PC's clone.</summary>
        public const int Display = 64;
    }

    /// <summary>The spacing grid and the chrome's fixed sizes (desktop units).</summary>
    private static class PcSize
    {
        /// <summary>The grid's small step.</summary>
        public const float S = 8f;

        /// <summary>The grid's medium step.</summary>
        public const float M = 12f;

        /// <summary>The grid's large step (a panel's side padding).</summary>
        public const float L = 16f;

        /// <summary>The Investigation app's toolbar row (Back, Forward, search, Accept, Deny).</summary>
        public const float Toolbar = 68f;

        /// <summary>A control's height inside a toolbar row.</summary>
        public const float Control = 44f;

            /// <summary>The navigator's width.</summary>
        public const float Nav = 308f;

        /// <summary>The navigator's case summary block.</summary>
        public const float NavCase = 96f;

        /// <summary>A source entry of the navigator.</summary>
        public const float NavEntry = 48f;

        /// <summary>An item under its source.</summary>
        public const float NavItem = 44f;

        /// <summary>An item's indent under its source.</summary>
        public const float NavIndent = 24f;

        /// <summary>A pane's header.</summary>
        public const float PaneHeader = 64f;

        /// <summary>A list row (the inbox, the day list, menus).</summary>
        public const float Row = 48f;

        /// <summary>A window control (minimise, maximise, close) on the title bar.</summary>
        public const float WindowControl = 52f;
    }

    /// <summary>
    /// Sets a chrome label to <paramref name="size"/> without auto-sizing:
    /// one line, or wrapping onto more when <paramref name="wrap"/> (never an
    /// ellipsis: a cut label hides the word the clerk looks for).
    /// </summary>
    private static TMP_Text Chrome(TMP_Text text, int size, bool wrap = false)
    {
        text.enableAutoSizing = false;
        text.fontSize = size;
        text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    /// <summary>A keyed label that fits by shrinking stops at the floor (<see cref="PcType.Caption"/>) instead of the theme's global minimum.</summary>
    private static TMP_Text FitNoSmaller(TMP_Text text, int size)
    {
        text.enableAutoSizing = true;
        text.fontSizeMax = size;
        text.fontSizeMin = Mathf.Min(size, PcType.Caption);
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    /// <summary>A button's label: the button's size in <paramref name="size"/>, aligned, with side room.</summary>
    private static TMP_Text ButtonLabel(Button button, int size, TextAlignmentOptions align = TextAlignmentOptions.Center, float side = 12f)
    {
        TMP_Text label = button.transform.Find("Label").GetComponent<TMP_Text>();
        Chrome(label, size);
        label.alignment = align;
        label.margin = new Vector4(side, 0f, side, 0f);
        label.raycastTarget = false;
        return label;
    }
}
