using System;
using System.Collections.Generic;

/// <summary>
/// Home's content block (the Home upgrades spec §5, HU3, and the Home pet
/// spec; written by Generate World from world_source.json "home"): which
/// house upgrade is the radio and the radio's lines, the night's optional
/// bills and their prices (home.bills), and the pet's words (home.pet). The
/// house upgrades and the toys are generated upgrade and effect assets in the
/// library's lists.
/// </summary>
[Serializable]
public sealed class HomeContent
{
    /// <summary>The id of the house upgrade whose ownership plays a radio line ("" for none).</summary>
    public string radioUpgrade = string.Empty;

    /// <summary>The radio's lines, in the order the nights play them.</summary>
    public List<string> radio = new();

    /// <summary>The night's optional bills (one row per HomeBill): their names, prices and lines.</summary>
    public List<BillRow> bills = new();

    /// <summary>The pet's words: the adoption's name limit, each kind's word, suggested name and reactions, the needs in words, the night's and the paper's lines.</summary>
    public PetContent pet = new();

    /// <summary>The radio's line on <paramref name="day"/>: the lines in order, day 1 the first, round again after the last (no random draw); "" without lines.</summary>
    public string RadioLine(int day)
    {
        if (radio == null || radio.Count == 0)
            return string.Empty;
        int i = (day - 1) % radio.Count;
        return radio[i < 0 ? i + radio.Count : i] ?? string.Empty;
    }

    /// <summary>The row of <paramref name="bill"/>, or null when the content lists none.</summary>
    public BillRow Bill(HomeBill bill)
    {
        foreach (BillRow row in bills ?? new List<BillRow>())
            if (row != null && row.bill == bill)
                return row;
        return null;
    }

    /// <summary>
    /// What Generate World and the validator refuse: a blank radio line, a
    /// radio upgrade that is none of <paramref name="houseUpgradeIds"/>, a
    /// bill missing, listed twice, unnamed or priced below 0, and the pet's
    /// problems (PetContent.Problems). Empty when sound.
    /// </summary>
    public List<string> Problems(IEnumerable<string> houseUpgradeIds)
    {
        var problems = new List<string>();
        for (int i = 0; i < (radio?.Count ?? 0); i++)
            if (string.IsNullOrWhiteSpace(radio[i]))
                problems.Add($"home.radio[{i}] is blank: each is a line the sleep panel plays.");
        if (!string.IsNullOrEmpty(radioUpgrade) && !new HashSet<string>(houseUpgradeIds ?? Array.Empty<string>()).Contains(radioUpgrade))
            problems.Add($"home.radioUpgrade '{radioUpgrade}' is no house upgrade's id (home.upgrades).");

        var seen = new HashSet<HomeBill>();
        foreach (BillRow row in bills ?? new List<BillRow>())
        {
            if (row == null)
                continue;
            if (!seen.Add(row.bill))
                problems.Add($"home.bills lists {row.bill} twice: one row per bill.");
            if (string.IsNullOrWhiteSpace(row.name))
                problems.Add($"home.bills {row.bill} has no name: the bills panel prints it.");
            if (row.price < 0)
                problems.Add($"home.bills {row.bill} costs below 0 cr.");
        }
        foreach (HomeBill bill in (HomeBill[])Enum.GetValues(typeof(HomeBill)))
            if (!seen.Contains(bill))
                problems.Add($"home.bills has no {bill} row: every night offers the five bills.");

        problems.AddRange((pet ?? new PetContent()).Problems());
        return problems;
    }
}

/// <summary>One of the night's optional bills (world_source.json home.bills): which bill, its name on the bills panel, its price in cr and its line.</summary>
[Serializable]
public sealed class BillRow
{
    /// <summary>The bill.</summary>
    public HomeBill bill;

    /// <summary>Its name on the bills panel.</summary>
    public string name = string.Empty;

    /// <summary>Its price a night in cr (the Medicine's with the house's CareCost ops).</summary>
    public int price;

    /// <summary>A short line beside its name on the bills panel.</summary>
    public string line = string.Empty;
}

/// <summary>One kind of pet's words (world_source.json home.pet.kinds).</summary>
[Serializable]
public sealed class PetKindContent
{
    /// <summary>The kind.</summary>
    public PetKind kind;

    /// <summary>The kind in a sentence ("dog"): every line's {kind}.</summary>
    public string word = string.Empty;

    /// <summary>The name the adoption panel suggests (a valid name, PetNames.Check).</summary>
    public string suggestedName = string.Empty;

    /// <summary>What it does when petted ({name}): one a pat, in turn.</summary>
    public List<string> reactions = new();

    /// <summary>What it does with a toy ({name}, {toy}): in turn by the day.</summary>
    public List<string> toyLines = new();

    /// <summary>The coats the adoption offers, in the panel's order (the first is the default: a run started outside the Title, a save from before the coats; the Home pet spec PS11).</summary>
    public List<PetCoatContent> coats = new();
}

/// <summary>One coat a kind of pet can have (world_source.json home.pet.kinds[].coats): its id names its art (ArtSlots.PetSprite) and is saved (PetState.coat); its name is a UI string.</summary>
[Serializable]
public sealed class PetCoatContent
{
    /// <summary>The coat's id ("ginger"): saved, and the art's name (Home/pet_cat_ginger_idle).</summary>
    public string id = string.Empty;

    /// <summary>The key of its name in ui.strings ("adopt.coat.ginger"), so the Translation Lens and the reading language reach it.</summary>
    public string nameKey = string.Empty;
}

/// <summary>
/// The pet's words (the Home pet spec PS4: "in words, never numbers";
/// world_source.json home.pet): the adoption's name limit, each kind's words,
/// a line per level of each need (best first), last night's change, the
/// Welfare Office's notice and the morning paper's lines. Every line takes
/// {name} and {kind}.
/// </summary>
[Serializable]
public sealed class PetContent
{
    /// <summary>The token of the pet's name in every line.</summary>
    public const string NameToken = "{name}";

    /// <summary>The token of the pet's kind word ("dog").</summary>
    public const string KindToken = "{kind}";

    /// <summary>The token of a toy's name in a toy line.</summary>
    public const string ToyToken = "{toy}";

    /// <summary>The adoption panel's name limit, in characters (PetNames.Check).</summary>
    public int nameMaxLength = 16;

    /// <summary>The kinds' words, one row per PetKind.</summary>
    public List<PetKindContent> kinds = new();

    /// <summary>Hunger in words, best first ("{name} is fed.").</summary>
    public List<string> hunger = new();

    /// <summary>Cold in words, best first.</summary>
    public List<string> cold = new();

    /// <summary>Boredom in words, best first.</summary>
    public List<string> boredom = new();

    /// <summary>Sickness in words, best first.</summary>
    public List<string> sickness = new();

    /// <summary>Home's line after a night it got sicker.</summary>
    public string worse = string.Empty;

    /// <summary>Home's line after a night it got better.</summary>
    public string better = string.Empty;

    /// <summary>Home's warning after a night left at the worst (PetState.welfareNights above 0).</summary>
    public string welfareNotice = string.Empty;

    /// <summary>The paper's line the morning after the adoption.</summary>
    public string paperAdopted = string.Empty;

    /// <summary>The paper's line the morning after a night left at the worst.</summary>
    public string paperWelfare = string.Empty;

    /// <summary>The words of <paramref name="kind"/> (null when the content lists none).</summary>
    public PetKindContent Kind(PetKind kind)
    {
        foreach (PetKindContent k in kinds ?? new List<PetKindContent>())
            if (k != null && k.kind == kind)
                return k;
        return null;
    }

    /// <summary>
    /// The coat a <paramref name="kind"/> wears for <paramref name="coat"/>:
    /// that coat when the kind lists it, else the kind's first coat (a blank
    /// coat from a save made before the coats, an id the content dropped);
    /// "" when the kind lists none.
    /// </summary>
    public string CoatOf(PetKind kind, string coat)
    {
        List<PetCoatContent> listed = Kind(kind)?.coats;
        if (listed == null)
            return string.Empty;
        foreach (PetCoatContent c in listed)
            if (c != null && !string.IsNullOrEmpty(coat) && c.id == coat)
                return c.id;
        foreach (PetCoatContent c in listed)
            if (c != null && !string.IsNullOrWhiteSpace(c.id))
                return c.id;
        return string.Empty;
    }

    /// <summary>A need's line for <paramref name="pet"/> at <paramref name="needs"/>: the need's words picked by its level (PetRules.Band up to <paramref name="max"/>), filled (<see cref="Fill"/>); "" without words.</summary>
    public string Need(PetNeed need, PetState pet, PetNeeds needs, int max)
    {
        List<string> words = need switch
        {
            PetNeed.Hunger => hunger,
            PetNeed.Cold => cold,
            PetNeed.Boredom => boredom,
            _ => sickness
        };
        if (words == null || words.Count == 0)
            return string.Empty;
        return Fill(words[PetRules.Band(needs.Of(need), max, words.Count)], pet);
    }

    /// <summary><paramref name="template"/> with the pet's name and kind word put in ("" for a blank template).</summary>
    public string Fill(string template, PetState pet)
    {
        if (string.IsNullOrEmpty(template))
            return string.Empty;
        PetKindContent k = pet != null ? Kind(pet.kind) : null;
        return template.Replace(NameToken, pet != null ? pet.name : string.Empty).Replace(KindToken, k != null ? k.word : string.Empty);
    }

    /// <summary>The <paramref name="turn"/>th of <paramref name="lines"/>, round again after the last, filled with the pet and <paramref name="toy"/>; "" without lines.</summary>
    public string InTurn(List<string> lines, int turn, PetState pet, string toy = "")
    {
        if (lines == null || lines.Count == 0)
            return string.Empty;
        int i = turn % lines.Count;
        return Fill(lines[i < 0 ? i + lines.Count : i], pet).Replace(ToyToken, toy ?? string.Empty);
    }

    /// <summary>
    /// The morning paper's pet line for <paramref name="tomorrow"/>: the
    /// adoption the morning after the day it was adopted
    /// (PetState.adoptedDay), else the Welfare Office's when the pet ended the
    /// night at the worst; "" otherwise, or without an adopted pet.
    /// </summary>
    public string PaperLine(PetState pet, int tomorrow)
    {
        if (pet == null || !pet.Adopted || pet.taken)
            return string.Empty;
        if (tomorrow == pet.adoptedDay + 1)
            return Fill(paperAdopted, pet);
        return pet.welfareNights > 0 ? Fill(paperWelfare, pet) : string.Empty;
    }

    /// <summary>What Generate World and the validator refuse in home.pet: a name limit below 1, a kind missing or listed twice, a blank kind word, a suggested name the adoption would refuse, a kind without reactions or toy lines, a kind without coats, a coat with a blank or repeated id or a blank name key, a need with fewer than two words, and a blank line or one that misses {name}. Empty when sound.</summary>
    public List<string> Problems()
    {
        var problems = new List<string>();
        if (nameMaxLength < 1)
            problems.Add("home.pet.nameMaxLength is below 1.");

        var seen = new HashSet<PetKind>();
        foreach (PetKindContent k in kinds ?? new List<PetKindContent>())
        {
            if (k == null)
                continue;
            if (!seen.Add(k.kind))
                problems.Add($"home.pet.kinds lists {k.kind} twice.");
            if (string.IsNullOrWhiteSpace(k.word))
                problems.Add($"home.pet.kinds {k.kind} has no word (every line's {KindToken}).");
            PetNameProblem name = PetNames.Check(k.suggestedName, nameMaxLength);
            if (name != PetNameProblem.None)
                problems.Add($"home.pet.kinds {k.kind}: the suggested name '{k.suggestedName}' would be refused ({name}).");
            Lines(problems, $"home.pet.kinds {k.kind} reactions", k.reactions, 1);
            Lines(problems, $"home.pet.kinds {k.kind} toyLines", k.toyLines, 1);
            Coats(problems, k);
        }
        foreach (PetKind kind in (PetKind[])Enum.GetValues(typeof(PetKind)))
            if (!seen.Contains(kind))
                problems.Add($"home.pet.kinds has no {kind} row: the adoption offers both.");

        Lines(problems, "home.pet.hunger", hunger, 2);
        Lines(problems, "home.pet.cold", cold, 2);
        Lines(problems, "home.pet.boredom", boredom, 2);
        Lines(problems, "home.pet.sickness", sickness, 2);
        foreach ((string path, string line) in new[] { ("worse", worse), ("better", better), ("welfareNotice", welfareNotice), ("paperAdopted", paperAdopted), ("paperWelfare", paperWelfare) })
            if (string.IsNullOrWhiteSpace(line) || !line.Contains(NameToken))
                problems.Add($"home.pet.{path} is blank or misses {NameToken}.");
        return problems;
    }

    /// <summary>Adds a problem for a kind without coats, and for a coat whose id is blank or repeated within the kind or whose name key is blank.</summary>
    private static void Coats(List<string> problems, PetKindContent k)
    {
        if (k.coats == null || k.coats.Count == 0)
        {
            problems.Add($"home.pet.kinds {k.kind} has no coats: the adoption offers at least one.");
            return;
        }
        var ids = new HashSet<string>();
        for (int i = 0; i < k.coats.Count; i++)
        {
            PetCoatContent c = k.coats[i];
            if (c == null || string.IsNullOrWhiteSpace(c.id))
                problems.Add($"home.pet.kinds {k.kind} coats[{i}] has no id.");
            else if (!ids.Add(c.id))
                problems.Add($"home.pet.kinds {k.kind} lists the coat '{c.id}' twice.");
            if (c != null && string.IsNullOrWhiteSpace(c.nameKey))
                problems.Add($"home.pet.kinds {k.kind} coats[{i}] has no nameKey (its name in ui.strings).");
        }
    }

    /// <summary>Adds a problem for fewer than <paramref name="least"/> lines, and for a blank line or one that misses {name}.</summary>
    private static void Lines(List<string> problems, string path, List<string> lines, int least)
    {
        if (lines == null || lines.Count < least)
            problems.Add($"{path} needs at least {least} line(s).");
        for (int i = 0; i < (lines?.Count ?? 0); i++)
            if (string.IsNullOrWhiteSpace(lines[i]) || !lines[i].Contains(NameToken))
                problems.Add($"{path}[{i}] is blank or misses {NameToken}.");
    }
}
