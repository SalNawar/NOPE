using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// What the Investigation app's keys read from a pane (redesign phase 20,
/// the PC redesign KB4): its tab buttons (the tab strip region) and the
/// active view's chips as drawn (the pane header region).
/// </summary>
public sealed partial class AppPane
{
    /// <summary>The tab's button on the strip, or null.</summary>
    public Button TabButton(AppTab tab)
        => At(tabButtons, tab);

    /// <summary>The active view's chips as drawn, left to right.</summary>
    public IReadOnlyList<Button> ChipButtons => _chips;
}
