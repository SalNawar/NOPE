using System.Collections.Generic;

/// <summary>One card of the Interview Protocols' ring: the question it asks (its interview choice id) and its printed label.</summary>
public readonly struct ProtocolCard
{
    /// <summary>A card.</summary>
    public ProtocolCard(string id, string label)
    {
        _id = id;
        _label = label;
    }

    private readonly string _id, _label;

    /// <summary>The question's interview choice id ("q:currency", a difference's question); empty for no card.</summary>
    public string Id => _id ?? string.Empty;

    /// <summary>The question's label, printed on the card; empty for no card.</summary>
    public string Label => _label ?? string.Empty;
}

/// <summary>
/// The Interview Protocols' card ring as a deck (Saleh 2026-10-18, demo 1018a
/// note 14: "every object the playyer unlocks should be useable"; the desk
/// machines spec §10): one card per question the traveller can be asked now
/// (the ask menu's open choices, DialogRunner.Offered), in the menu's order,
/// one of them at the front. A click on the ring asks the front card's
/// question, the diegetic twin of the wheel's ask menu; a drag flicks the
/// front card over to the next. A card asked leaves the deck and the next
/// takes the front; the front card stays the front while the deck changes
/// round it (a question unlocked by a finding joins at its place). Pure.
/// </summary>
public sealed class ProtocolDeck
{
    private readonly List<ProtocolCard> _cards = new List<ProtocolCard>();
    private int _front;

    /// <summary>The cards, in the ask menu's order.</summary>
    public IReadOnlyList<ProtocolCard> Cards => _cards;

    /// <summary>True when there is no question to ask.</summary>
    public bool Empty => _cards.Count == 0;

    /// <summary>The front card's index (0 when the deck is empty).</summary>
    public int FrontIndex => _front;

    /// <summary>The front card; default (an empty id) when the deck is empty.</summary>
    public ProtocolCard Front => _cards.Count > 0 ? _cards[_front] : default;

    /// <summary>
    /// The deck becomes <paramref name="cards"/> (null: none). The front card
    /// stays the front when it is still there; when it left (asked), the card
    /// that followed it takes the front (the first after the last).
    /// </summary>
    public void Set(IEnumerable<ProtocolCard> cards)
    {
        string frontId = _cards.Count > 0 ? _cards[_front].Id : null;
        int wasAt = _front;
        _cards.Clear();
        if (cards != null)
            _cards.AddRange(cards);
        _front = 0;
        if (_cards.Count == 0)
            return;
        int kept = frontId == null ? -1 : _cards.FindIndex(c => c.Id == frontId);
        _front = kept >= 0 ? kept : wasAt < _cards.Count ? wasAt : 0;
    }

    /// <summary>The front card flicks over to the back: the next card takes the front (the first after the last). False (nothing moves) with fewer than two cards.</summary>
    public bool Flick()
    {
        if (_cards.Count < 2)
            return false;
        _front = (_front + 1) % _cards.Count;
        return true;
    }
}
