/// <summary>
/// A view of the Investigation app whose items and rows have entry keys (the
/// PC redesign PR1, PR2, SE4's jump): the item it shows (a paper, a book, a
/// record) for the pane's pin button and the recent items, and a jump to an
/// item or a row by its key (a pin or a recent item clicked). The Documents,
/// Records, Reference and Transcript views implement it.
/// </summary>
public interface IAppItems
{
    /// <summary>The shown item's entry key ("doc:0", "bookof:Currency", "rec:552-1804-33"), or null when none is shown (or the view has no items).</summary>
    string ItemKey { get; }

    /// <summary>The shown item's name for a pin or a recent item, or null.</summary>
    string ItemTitle { get; }

    /// <summary>
    /// Shows the item or row <paramref name="key"/> names: the item chosen, the
    /// row's page turned to (a filter that hides it turned off). False when
    /// the key is not this view's or its item is not there any more.
    /// </summary>
    bool Reveal(string key);
}
