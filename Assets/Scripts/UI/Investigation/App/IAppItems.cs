/// <summary>
/// A view of the Investigation app whose items have entry keys (the PC
/// redesign PR1, PR2): the item it shows (a paper, a book, a record) for the
/// pane's pin button and the recent items. A jump to a pinned or recent key
/// is the pane's (SmartLinks.ForEntry, then IAppView.Reveal). The Documents,
/// Records, Reference and Transcript views implement it.
/// </summary>
public interface IAppItems
{
    /// <summary>The shown item's entry key ("doc:0", "bookof:Currency", "rec:MRV-552"), or null when none is shown (or the view has no items).</summary>
    string ItemKey { get; }

    /// <summary>The shown item's name for a pin or a recent item, or null.</summary>
    string ItemTitle { get; }
}
