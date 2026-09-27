using System.Collections.Generic;
using System.Linq;

/// <summary>One place's name lists (its id, its men's and its women's names).</summary>
public sealed class NameList
{
    /// <summary>A place's lists (copied; null is none).</summary>
    public NameList(string id, IEnumerable<string> male, IEnumerable<string> female)
    {
        Id = id;
        Male = male != null ? male.ToArray() : new string[0];
        Female = female != null ? female.ToArray() : new string[0];
    }

    /// <summary>The place's id ("egypt_future").</summary>
    public string Id { get; }

    /// <summary>Its men's names.</summary>
    public IReadOnlyList<string> Male { get; }

    /// <summary>Its women's names.</summary>
    public IReadOnlyList<string> Female { get; }
}

/// <summary>
/// The names of 2150 citizens (traveller types K4): the Future places' lists
/// together, whoever leads. A citizen's gender follows the merged list their
/// name is on (TravellerGenders.FromNameLists), and the list that gave the
/// name is their family's country: their skin and hair weights (C1) and their
/// account's lineage come from it. Pure, so the lists are checked headless.
/// </summary>
public sealed class CitizenNames
{
    /// <summary>The names of <paramref name="lists"/>, in order (null is none).</summary>
    public CitizenNames(IEnumerable<NameList> lists)
    {
        Lists = (lists ?? Enumerable.Empty<NameList>()).Where(l => l != null).ToArray();
        Male = Lists.SelectMany(l => l.Male).ToArray();
        Female = Lists.SelectMany(l => l.Female).ToArray();
        All = Male.Concat(Female).ToArray();
    }

    /// <summary>The source lists, in order.</summary>
    public IReadOnlyList<NameList> Lists { get; }

    /// <summary>Every list's men's names, in list order.</summary>
    public IReadOnlyList<string> Male { get; }

    /// <summary>Every list's women's names, in list order.</summary>
    public IReadOnlyList<string> Female { get; }

    /// <summary>The name pool a citizen's name is taken from (NameRoster.Take): every man's name, then every woman's.</summary>
    public IReadOnlyList<string> All { get; }

    /// <summary>The gender of a citizen named <paramref name="name"/>: the merged list the name is on.</summary>
    public TravellerGender GenderOf(string name) => TravellerGenders.FromNameLists(name, Male, Female);

    /// <summary>
    /// The index in <see cref="Lists"/> of the first list holding
    /// <paramref name="name"/> (the scanner comparison; a NameRoster suffix
    /// counts as its pool name), or -1.
    /// </summary>
    public int SourceOf(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return -1;

        int index = IndexHolding(name);
        return index >= 0 ? index : IndexHolding(NameRoster.BaseName(name));
    }

    /// <summary>
    /// What Generate World and the validator refuse: no names at all, and a
    /// name on both merged lists (its gender could not be told), naming the
    /// lists that hold it.
    /// </summary>
    public List<string> Problems()
    {
        var problems = new List<string>();
        if (All.Count == 0)
        {
            problems.Add("The 2150 citizens have no names: the Future places' name lists are empty.");
            return problems;
        }

        foreach (NameList men in Lists)
            foreach (string name in men.Male)
                foreach (NameList women in Lists)
                    if (women.Female.Any(f => DiscrepancyLog.ValuesMatch(f, name)))
                        problems.Add($"The 2150 name '{name}' is a man's name in '{men.Id}' and a woman's in '{women.Id}', so a citizen's gender could not be told from it.");
        return problems;
    }

    /// <summary>The first list holding the name exactly (the scanner comparison), or -1.</summary>
    private int IndexHolding(string name)
    {
        for (int i = 0; i < Lists.Count; i++)
            if (Lists[i].Male.Concat(Lists[i].Female).Any(n => DiscrepancyLog.ValuesMatch(n, name)))
                return i;
        return -1;
    }
}
