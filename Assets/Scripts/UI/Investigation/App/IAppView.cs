using System;
using System.Collections.Generic;

/// <summary>One chip in a pane's header: an item its view can show (a paper, a book).</summary>
public readonly struct AppChip
{
    /// <summary>A chip reading <paramref name="label"/>; <paramref name="available"/> is false for an item not readable yet.</summary>
    public AppChip(string label, bool available)
    {
        Label = label;
        Available = available;
    }

    /// <summary>The chip's text.</summary>
    public string Label { get; }

    /// <summary>False for an item that cannot be read yet (a paper not scanned): the chip shows dimmed, and choosing it shows why.</summary>
    public bool Available { get; }
}

/// <summary>
/// One tab's view in the Investigation app's pane (the PC redesign AP5): what
/// the pane needs from a view, whatever draws it. The pane shows and hides the
/// view with its tab, lays the view's chips out in its header and forwards a
/// chip's click. Today's views host the desk's existing lists and texts; the
/// forms engine's views (FormView, phase 5) implement the same interface, so
/// moving a tab onto forms replaces that tab's view component and nothing in
/// the pane, the app or the presenters.
/// </summary>
public interface IAppView
{
    /// <summary>The tab the view fills.</summary>
    AppTab Tab { get; }

    /// <summary>The view's items as the pane header's chips (empty for a view without items).</summary>
    IReadOnlyList<AppChip> Chips { get; }

    /// <summary>The chosen item's index, or -1 (none chosen).</summary>
    int Selected { get; }

    /// <summary>Raised when the chips change (a paper scanned, the books built, another item chosen).</summary>
    event Action ChipsChanged;

    /// <summary>Shows the view in the pane (its tab is active) or hides it.</summary>
    void SetVisible(bool visible);

    /// <summary>Shows item <paramref name="index"/> (its chip was clicked).</summary>
    void Select(int index);
}
