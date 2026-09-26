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
/// One tab's view in a pane of the Investigation app (the PC redesign AP5,
/// AP9, LK1): what the pane needs from a view, whatever draws it. The pane
/// shows and hides the view with its tab, lays the view's chips out in its
/// header, forwards a chip's click, sends the view where a link, Back or a
/// jump goes (Reveal) and records where the view is (Spot) in its history.
/// Each pane has its own views. Today's views host the desk's existing lists
/// and texts; the forms engine's views (FormView, phase 5) implement the same
/// interface, so moving a tab onto forms replaces that tab's view component
/// and nothing in the pane, the app or the presenters.
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

    /// <summary>Where the view is, for the pane's history: its tab and chosen item (Records: its lookup).</summary>
    LinkTarget Spot { get; }

    /// <summary>Raised when the player moves the view without the pane (Records: a lookup run): the pane records the new Spot.</summary>
    event Action Moved;

    /// <summary>
    /// Goes where <paramref name="target"/> says (its tab is this view's): its
    /// item (-1: keep the one shown), then its row (a pick key: the page
    /// turned and a filter lifted until the row shows, the row marked found;
    /// null: nothing marked) or, in Records, its lookup and the found record's
    /// row of its category. A link, Back, Forward and a jump all come here;
    /// nothing is ever picked.
    /// </summary>
    void Reveal(LinkTarget target);
}
