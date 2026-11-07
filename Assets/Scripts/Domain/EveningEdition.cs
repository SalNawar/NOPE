using System.Collections.Generic;

/// <summary>The evening edition's text, composed (EveningEdition.Compose); the ending screen prints it. Not serialized.</summary>
public sealed class EveningEditionPage
{
    /// <summary>The paper's name ("The Temporal Times").</summary>
    public string Masthead = string.Empty;

    /// <summary>The edition's line ("Edition of day 9").</summary>
    public string Edition = string.Empty;

    /// <summary>The front page's headline: the ending's name.</summary>
    public string Headline = string.Empty;

    /// <summary>The story: the ending's text, the pet's name put in, its closing card set apart under it.</summary>
    public string Body = string.Empty;

    /// <summary>The pet's fate, from Home's own words ("" without an adopted pet).</summary>
    public string PetLine = string.Empty;
}

/// <summary>
/// The ending screen's evening edition (FLOW, BUG-56a; the bible's J6: "an
/// evening-edition epilogue with pictures and the pet's fate"): the run's last
/// paper, its headline the ending's name over the ending's picture and story,
/// with the pet's fate said in Home's words (its four needs as Home says them;
/// never a number; nothing for a pet the Welfare Office took, whose ending's
/// story tells it). Pure; Endings/EveningEditionView lays it out with Close
/// and Quit.
/// </summary>
public static class EveningEdition
{
    /// <summary>
    /// The page: <paramref name="masthead"/> and <paramref name="edition"/> as
    /// given, the headline (<paramref name="fallbackHeadline"/> for a blank
    /// one), the body with <paramref name="pet"/>'s name and kind put in
    /// (PetContent.Fill) and <paramref name="closingCard"/> set apart under it,
    /// and the pet's fate (<see cref="PetFate"/>).
    /// </summary>
    public static EveningEditionPage Compose(string masthead, string edition, string headline, string fallbackHeadline, string body, string closingCard,
                                             PetState pet, PetContent words, int needMax)
    {
        string story = (body ?? string.Empty).Trim();
        string card = (closingCard ?? string.Empty).Trim();
        if (card.Length > 0)
            story = story.Length > 0 ? story + "\n\n" + card : card;
        return new EveningEditionPage
        {
            Masthead = masthead ?? string.Empty,
            Edition = edition ?? string.Empty,
            Headline = string.IsNullOrWhiteSpace(headline) ? fallbackHeadline ?? string.Empty : headline.Trim(),
            Body = words != null ? words.Fill(story, pet) : story,
            PetLine = PetFate(pet, words, needMax)
        };
    }

    /// <summary>
    /// The pet's fate in Home's words: "" without an adopted pet or words, and
    /// "" for a pet the Welfare Office took (its ending's story says where the
    /// pet went; the paper's line PetContent.paperWelfare is the warning after
    /// a night at the worst, "inspectors are asking after ...", which reads as
    /// if the pet were still at home: qa-endings-saves, 2026-11-07); else each
    /// need's line at its level (PetContent.Need, up to
    /// <paramref name="needMax"/>, GameConfigSO.petNeedMax), joined by spaces.
    /// </summary>
    public static string PetFate(PetState pet, PetContent words, int needMax)
    {
        if (pet == null || !pet.Adopted || words == null || pet.taken)
            return string.Empty;
        var lines = new List<string>();
        PetNeeds needs = pet.Needs;
        foreach (PetNeed need in (PetNeed[])System.Enum.GetValues(typeof(PetNeed)))
        {
            string line = words.Need(need, pet, needs, needMax);
            if (line.Length > 0)
                lines.Add(line);
        }
        return string.Join(" ", lines);
    }
}
