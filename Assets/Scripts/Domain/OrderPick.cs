/// <summary>
/// Which Orders card the detail card shows and which one its action acts on
/// (UI-25: "order straight from the card (one click), with the detail on
/// hover"; BUG-58a): the pointer over a card shows that card in the detail
/// card, but only a click (or the arrows) chooses the card the action
/// button and Enter act on, so the pointer passing over other cards on its
/// way to the action button never changes what the button does; once the
/// pointer has left the cards the detail card shows the chosen card again.
/// Pure; OrdersWindow keeps one.
/// </summary>
public sealed class OrderPick
{
    /// <summary>The chosen card (a click or the arrows): what the action acts on; null before any.</summary>
    public string Target { get; private set; }

    /// <summary>The card under the pointer, or null.</summary>
    public string Hovered { get; private set; }

    /// <summary>The card the detail card shows: the one under the pointer, else the chosen one.</summary>
    public string Shown => Hovered ?? Target;

    /// <summary>The pointer came over a card; true when the detail card changes.</summary>
    public bool Hover(string id)
    {
        if (Hovered == id)
            return false;
        string shown = Shown;
        Hovered = id;
        return Shown != shown;
    }

    /// <summary>The pointer left a card; true when the detail card changes (a leave of a card it is not over changes nothing).</summary>
    public bool Leave(string id)
    {
        if (Hovered == null || Hovered != id)
            return false;
        string shown = Shown;
        Hovered = null;
        return Shown != shown;
    }

    /// <summary>A click or the arrows choose a card; true when the chosen card or the detail card changes.</summary>
    public bool Choose(string id)
    {
        if (Target == id)
            return false;
        Target = id;
        return true;
    }
}
