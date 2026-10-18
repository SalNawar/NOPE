using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// One of the Diplomatic Contacts on the Rolodex (world_source.json
/// agency.contacts; the desk machines spec §10): someone well placed who owes
/// the clerk a favour and watches one family of faults. Content, written into
/// the content library by Generate World.
/// </summary>
[Serializable]
public sealed class ContactEntry
{
    /// <summary>The row's key.</summary>
    public string id = string.Empty;

    /// <summary>The contact's name, printed on their card.</summary>
    public string name = string.Empty;

    /// <summary>Where they sit ("Seal Registry, night desk").</summary>
    public string post = string.Empty;

    /// <summary>The faults they watch: LieKind names (each lie watched by one contact at most).</summary>
    public List<string> lies = new List<string>();

    /// <summary>What they say on a day one of today's travellers carries a fault they watch; {place} is that traveller's claimed place.</summary>
    public string tip = string.Empty;

    /// <summary>What they say on a day none does.</summary>
    public string quiet = string.Empty;
}

/// <summary>One of today's travellers as the contacts hear of them: the lie they carry (null: none) and the place they claim.</summary>
public readonly struct ContactLead
{
    /// <summary>A traveller.</summary>
    public ContactLead(LieKind? lie, string place)
    {
        Lie = lie;
        Place = place ?? string.Empty;
    }

    /// <summary>The lie they carry, or null.</summary>
    public LieKind? Lie { get; }

    /// <summary>The place they claim ("Periclean Athens (Ancient)").</summary>
    public string Place { get; }
}

/// <summary>One card of the Rolodex today: the contact's name and post, what they say, and whether it is a tip (a fault they watch is on its way today).</summary>
public readonly struct ContactCard
{
    /// <summary>A card.</summary>
    public ContactCard(string name, string post, string line, bool tip)
    {
        _name = name;
        _post = post;
        _line = line;
        Tip = tip;
    }

    private readonly string _name, _post, _line;

    /// <summary>The contact's name.</summary>
    public string Name => _name ?? string.Empty;

    /// <summary>Where they sit.</summary>
    public string Post => _post ?? string.Empty;

    /// <summary>What they say today.</summary>
    public string Line => _line ?? string.Empty;

    /// <summary>True when the line is a tip about one of today's travellers.</summary>
    public bool Tip { get; }
}

/// <summary>
/// The Diplomatic Contacts' Rolodex (Saleh 2026-10-18, demo 1018a note 14:
/// "every object the playyer unlocks should be useable not just there like it
/// should have a function"; the orchestrator's idea: "its card gives a useful
/// fact"; the desk machines spec §10): each morning every contact says what
/// they hear about today's travellers. A contact one of whose watched faults a
/// traveller of today carries tips the clerk off with that traveller's
/// claimed place ("A bent seal press went missing. Expect one of its seals on
/// papers headed for {place}."); one none of whose faults travels today says
/// their quiet line. The day's cards are in authored order but for the gold
/// card on top, the lead: a contact with a tip. Draws, on the day's own
/// contacts stream (Seeds.ForContacts), so no traveller moves: for each
/// contact in authored order with two or more matching travellers one Range
/// draw picks whose place is named; then, with two or more tips, one Range
/// draw picks the lead. A tip names a place, never a traveller: the clerk
/// still has to find the fault and prove it. Pure.
/// </summary>
public static class ContactCards
{
    /// <summary>The token a tip names the place with.</summary>
    public const string PlaceToken = "{place}";

    /// <summary>Today's cards: one per contact (authored order, the lead first) from <paramref name="travellers"/> (today's, in queue order) on <paramref name="rng"/>; none without contacts.</summary>
    public static List<ContactCard> ForDay(IReadOnlyList<ContactEntry> contacts, IReadOnlyList<ContactLead> travellers, IRandomSource rng)
    {
        var cards = new List<ContactCard>();
        foreach (ContactEntry c in contacts ?? Array.Empty<ContactEntry>())
        {
            if (c == null)
                continue;
            List<ContactLead> matching = (travellers ?? Array.Empty<ContactLead>())
                .Where(t => t.Lie.HasValue && (c.lies ?? new List<string>()).Contains(t.Lie.Value.ToString())).ToList();
            if (matching.Count == 0)
            {
                cards.Add(new ContactCard(c.name, c.post, c.quiet, false));
                continue;
            }
            ContactLead lead = matching.Count == 1 || rng == null ? matching[0] : matching[rng.Range(0, matching.Count)];
            cards.Add(new ContactCard(c.name, c.post, (c.tip ?? string.Empty).Replace(PlaceToken, lead.Place), true));
        }
        List<int> tips = Enumerable.Range(0, cards.Count).Where(i => cards[i].Tip).ToList();
        if (tips.Count > 0)
        {
            int first = tips.Count == 1 || rng == null ? tips[0] : tips[rng.Range(0, tips.Count)];
            ContactCard top = cards[first];
            cards.RemoveAt(first);
            cards.Insert(0, top);
        }
        return cards;
    }

    /// <summary>What Generate World and the validator refuse in agency.contacts: a blank or repeated id, a blank name, post, tip or quiet line, a tip that names no {place}, a lie that is no LieKind, and a lie two contacts watch. Empty when sound, or with no contacts.</summary>
    public static List<string> Problems(IReadOnlyList<ContactEntry> contacts)
    {
        var problems = new List<string>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var watched = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (ContactEntry c in contacts ?? Array.Empty<ContactEntry>())
        {
            if (c == null)
                continue;
            string at = $"agency.contacts '{c.id}'";
            if (string.IsNullOrWhiteSpace(c.id))
                problems.Add("agency.contacts has a row with a blank id.");
            else if (!ids.Add(c.id))
                problems.Add($"{at}: the id is listed twice.");
            if (string.IsNullOrWhiteSpace(c.name) || string.IsNullOrWhiteSpace(c.post))
                problems.Add($"{at}: the name and the post are printed on the card; neither may be blank.");
            if (string.IsNullOrWhiteSpace(c.quiet))
                problems.Add($"{at}: the quiet line is blank (what they say on a day none of their faults travels).");
            if (string.IsNullOrWhiteSpace(c.tip) || !c.tip.Contains(PlaceToken))
                problems.Add($"{at}: the tip must name the traveller's place with {PlaceToken}.");
            if (c.lies == null || c.lies.Count == 0)
                problems.Add($"{at}: it watches no fault (lies: LieKind names).");
            foreach (string lie in c.lies ?? new List<string>())
            {
                if (!Enum.TryParse(lie, false, out LieKind _) || !Enum.IsDefined(typeof(LieKind), lie ?? string.Empty))
                    problems.Add($"{at}: '{lie}' is not a LieKind.");
                else if (watched.TryGetValue(lie, out string other))
                    problems.Add($"{at}: {lie} is already watched by '{other}'; one contact per fault.");
                else
                    watched[lie] = c.id;
            }
        }
        return problems;
    }
}
