/// <summary>The inputs that act on the chosen Orders card (OrderInputs).</summary>
public enum OrderInput
{
    /// <summary>A click on a card (one-click ordering: it acts only on a card that can be ordered now).</summary>
    Card,

    /// <summary>The detail card's action button.</summary>
    Button,

    /// <summary>Enter (the desktop's keys).</summary>
    Key,
}

/// <summary>
/// One action per input on the Orders window (BUG-58a; UI-25: "places then
/// cancels on a double-click", "Enter fires buttons twice"), each input on
/// its own clock: the button's double-click acts once (it turns into Cancel
/// order under the second click, which must not cancel), two quick Enters
/// act once, but a card click never refuses the button or Enter that follow
/// it (the review of the fix round: one shared clock silently refused the
/// clerk's Enter within 0.4 s of a card click that ordered). A card click
/// needs no clock: it acts only on a card that can be ordered, so its
/// double-click finds the card ordered and does nothing (OrdersWindow).
/// Times are the caller's clock (unscaled seconds). Pure.
/// </summary>
public sealed class OrderInputs
{
    private readonly ActionDebounce _button, _key;

    /// <summary>Inputs refused within <paramref name="seconds"/> of their own last action.</summary>
    public OrderInputs(float seconds)
    {
        _button = new ActionDebounce(seconds);
        _key = new ActionDebounce(seconds);
    }

    /// <summary>True when <paramref name="input"/> may act at <paramref name="now"/> (and records it); false within its own double-click time.</summary>
    public bool TryAct(OrderInput input, float now)
    {
        switch (input)
        {
            case OrderInput.Button: return _button.TryAct(now);
            case OrderInput.Key: return _key.TryAct(now);
            default: return true;
        }
    }
}
