using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>A seal's outline (the document design spec, D4). Named in world_source.json agency.offices; SealOutlines (Visuals) draws each.</summary>
public enum SealShape
{
    /// <summary>A ring.</summary>
    Circle,

    /// <summary>A six-sided ring, flat top and bottom.</summary>
    Hexagon,

    /// <summary>A heater shield: flat top, pointed foot.</summary>
    Shield,

    /// <summary>A square stood on its corner.</summary>
    Diamond,

    /// <summary>An eight-sided ring.</summary>
    Octagon,

    /// <summary>A square.</summary>
    Square
}

/// <summary>A seal's ink (the document design spec, D4): each a dark shade that reads at the body-text minimum on every paper (Build Office UI checks it).</summary>
public enum SealInk
{
    /// <summary>Oxblood.</summary>
    Red,

    /// <summary>Navy.</summary>
    Blue,

    /// <summary>Bottle green.</summary>
    Green,

    /// <summary>Plum.</summary>
    Violet,

    /// <summary>Umber.</summary>
    Brown,

    /// <summary>Carbon black.</summary>
    Black
}

/// <summary>How a forged seal differs from the true one (the document design spec, D4; the fault canon's ForgedSeal variants).</summary>
public enum SealForgery
{
    /// <summary>Another outline, the same ink and legend.</summary>
    Shape,

    /// <summary>Another ink, the same outline and legend.</summary>
    Ink,

    /// <summary>The office's authored wrong legend (agency.offices forgedLegend), the same outline and ink.</summary>
    Legend
}

/// <summary>One seal as printed: its outline, its ink and the legend (an office's initials) inside it.</summary>
public readonly struct Seal : IEquatable<Seal>
{
    /// <summary>A seal from its parts (the legend trimmed and in capitals).</summary>
    public Seal(SealShape shape, SealInk ink, string legend)
    {
        Shape = shape;
        Ink = ink;
        Legend = (legend ?? string.Empty).Trim().ToUpperInvariant();
    }

    /// <summary>The outline.</summary>
    public SealShape Shape { get; }

    /// <summary>The ink.</summary>
    public SealInk Ink { get; }

    /// <summary>The initials printed inside ("VO").</summary>
    public string Legend { get; }

    /// <summary>True when every part is the same.</summary>
    public bool Equals(Seal other) => Shape == other.Shape && Ink == other.Ink && Legend == other.Legend;

    /// <summary>True when <paramref name="obj"/> is the same seal.</summary>
    public override bool Equals(object obj) => obj is Seal other && Equals(other);

    /// <summary>A hash of the parts.</summary>
    public override int GetHashCode() => ((int)Shape * 31 + (int)Ink) * 31 + Legend.GetHashCode();

    /// <summary>Seals.Describe's words.</summary>
    public override string ToString() => Seals.Describe(this);
}

/// <summary>
/// One issuing office of the agency (the document design spec, D4;
/// world_source.json agency.offices): the programme line its forms print, its
/// seal (outline, ink, legend), the wrong legend a forger prints, and the
/// forms it issues (every traveller form is issued by exactly one office).
/// Content, written into the content library by Generate World.
/// </summary>
[Serializable]
public sealed class AgencyOffice
{
    /// <summary>The office's id ("visa").</summary>
    public string id = string.Empty;

    /// <summary>Its printed name, the programme line of the forms it issues and the Seal Register's row ("Visa Office").</summary>
    public string name = string.Empty;

    /// <summary>The seal's outline, a SealShape name.</summary>
    public string shape = string.Empty;

    /// <summary>The seal's ink, a SealInk name.</summary>
    public string ink = string.Empty;

    /// <summary>The initials inside the seal ("VO"): one to three capitals.</summary>
    public string legend = string.Empty;

    /// <summary>The initials a forger prints instead (SealForgery.Legend): as many capitals, never the true legend or another office's.</summary>
    public string forgedLegend = string.Empty;

    /// <summary>The form numbers it issues ("TC-101").</summary>
    public List<string> forms = new List<string>();

    /// <summary>The office's true seal; false (a default seal) when its shape or ink names no value.</summary>
    public bool TryGetSeal(out Seal seal)
    {
        seal = default;
        if (!Enum.TryParse(shape, false, out SealShape s) || !Enum.IsDefined(typeof(SealShape), s) ||
            !Enum.TryParse(ink, false, out SealInk i) || !Enum.IsDefined(typeof(SealInk), i))
            return false;
        seal = new Seal(s, i, legend);
        return true;
    }
}

/// <summary>
/// The seals (the document design spec, D4): each office's seal is printed
/// in the header of every form it issues and pictured in the Seal Register
/// (a reference book); a seal's canonical value is its description ("Blue
/// hexagon · VO"), so a paper's seal and a register row compare as any two
/// values do (Values.Match), and a forged seal (Forge) differs in exactly one
/// part. Pure, so every rule and draw is tested headless.
/// </summary>
public static class Seals
{
    /// <summary>The separator between the outline and the legend in a description.</summary>
    public const string Separator = " · ";

    /// <summary>The most characters a legend has (it must read inside the seal at the body-text minimum).</summary>
    public const int MaxLegend = 3;

    /// <summary>
    /// The seal as its canonical value and printed words: its ink and outline,
    /// then its legend ("Blue hexagon · VO"). Parse reads it back.
    /// </summary>
    public static string Describe(Seal seal) =>
        seal.Ink + " " + seal.Shape.ToString().ToLowerInvariant() + Separator + seal.Legend;

    /// <summary>Reads a description (Describe's words, any case, trimmed); false with a default seal for anything else.</summary>
    public static bool TryParse(string value, out Seal seal)
    {
        seal = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;
        int cut = value.IndexOf(Separator, StringComparison.Ordinal);
        if (cut < 0)
            return false;
        string[] words = value.Substring(0, cut).Trim().Split(' ');
        string legend = value.Substring(cut + Separator.Length).Trim();
        if (words.Length != 2 || legend.Length == 0 ||
            !Enum.TryParse(words[0], true, out SealInk ink) || !Enum.IsDefined(typeof(SealInk), ink) ||
            !Enum.TryParse(words[1], true, out SealShape shape) || !Enum.IsDefined(typeof(SealShape), shape))
            return false;
        seal = new Seal(shape, ink, legend);
        return true;
    }

    /// <summary>The ink's colour, "#RRGGBB" (dark enough for text on every paper: Build Office UI checks each against every form's paper).</summary>
    public static string InkHex(SealInk ink)
    {
        switch (ink)
        {
            case SealInk.Red: return "#8A1C1C";
            case SealInk.Blue: return "#1C3A78";
            case SealInk.Green: return "#1D5A36";
            case SealInk.Violet: return "#4F2A70";
            case SealInk.Brown: return "#5E3A12";
            default: return "#1E1E1E";
        }
    }

    /// <summary>The office that issues form <paramref name="formNumber"/> (the first that lists it), or null.</summary>
    public static AgencyOffice OfficeOf(IReadOnlyList<AgencyOffice> offices, string formNumber)
    {
        if (offices == null || string.IsNullOrEmpty(formNumber))
            return null;
        foreach (AgencyOffice office in offices)
            if (office != null && office.forms != null && office.forms.Contains(formNumber))
                return office;
        return null;
    }

    /// <summary>
    /// A forged seal of <paramref name="office"/>, differing from its true seal
    /// in <paramref name="forgery"/>'s part alone: another outline or another
    /// ink (one Range draw over the other values, in enum order), or the
    /// office's authored wrong legend (no draw). The true seal, with no draw,
    /// when the office has no valid seal.
    /// </summary>
    public static Seal Forge(AgencyOffice office, SealForgery forgery, IRandomSource rng)
    {
        if (office == null || !office.TryGetSeal(out Seal truth))
            return default;
        switch (forgery)
        {
            case SealForgery.Shape:
            {
                List<SealShape> others = Enum.GetValues(typeof(SealShape)).Cast<SealShape>().Where(s => s != truth.Shape).ToList();
                return new Seal(others[rng.Range(0, others.Count)], truth.Ink, truth.Legend);
            }
            case SealForgery.Ink:
            {
                List<SealInk> others = Enum.GetValues(typeof(SealInk)).Cast<SealInk>().Where(i => i != truth.Ink).ToList();
                return new Seal(truth.Shape, others[rng.Range(0, others.Count)], truth.Legend);
            }
            default:
                return new Seal(truth.Shape, truth.Ink, office.forgedLegend);
        }
    }

    /// <summary>
    /// What Generate World and the validator refuse in the offices: a blank or
    /// repeated id or name; a shape or ink that names no value; a legend of
    /// one to MaxLegend capitals; a wrong legend of as many capitals, never the
    /// true one or another office's; two offices with one outline and ink (the
    /// register must tell every office apart by its outline and ink alone); a
    /// form issued by no office or by two. <paramref name="formNumbers"/> are
    /// the forms the agency hands out. Empty when sound.
    /// </summary>
    public static List<string> Problems(IReadOnlyList<AgencyOffice> offices, IEnumerable<string> formNumbers)
    {
        var problems = new List<string>();
        var ids = new HashSet<string>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var looks = new Dictionary<(SealShape, SealInk), string>();
        var legends = new HashSet<string>((offices ?? Array.Empty<AgencyOffice>()).Where(o => o != null).Select(o => (o.legend ?? string.Empty).Trim().ToUpperInvariant()));
        var issuers = new Dictionary<string, string>();
        foreach (AgencyOffice office in offices ?? Array.Empty<AgencyOffice>())
        {
            if (office == null)
                continue;
            string who = $"agency.offices '{office.id}'";
            if (string.IsNullOrWhiteSpace(office.id) || !ids.Add(office.id))
                problems.Add($"{who}: the id is blank or listed twice.");
            if (string.IsNullOrWhiteSpace(office.name) || !names.Add(office.name.Trim()))
                problems.Add($"{who}: the name '{office.name}' is blank or another office's.");
            else if (office.name.Length > FactTable.MaxValueLength)
                problems.Add($"{who}: the name '{office.name}' is {office.name.Length} characters; a programme line and a register row hold {FactTable.MaxValueLength}.");
            if (!office.TryGetSeal(out Seal seal))
                problems.Add($"{who}: the shape '{office.shape}' or the ink '{office.ink}' names no value ({string.Join(", ", Enum.GetNames(typeof(SealShape)))}; {string.Join(", ", Enum.GetNames(typeof(SealInk)))}).");
            else if (looks.TryGetValue((seal.Shape, seal.Ink), out string twin))
                problems.Add($"{who}: a {Describe(seal)} seal has the outline and ink of '{twin}'s; every office's outline and ink differ.");
            else
                looks[(seal.Shape, seal.Ink)] = office.id;
            string legend = (office.legend ?? string.Empty).Trim();
            string forged = (office.forgedLegend ?? string.Empty).Trim();
            if (!IsLegend(legend))
                problems.Add($"{who}: the legend '{office.legend}' is not 1 to {MaxLegend} capitals.");
            if (!IsLegend(forged) || forged.Length != legend.Length || legends.Contains(forged.ToUpperInvariant()))
                problems.Add($"{who}: the forged legend '{office.forgedLegend}' must be {legend.Length} capitals, differing from every office's legend.");
            foreach (string form in office.forms ?? new List<string>())
            {
                if (issuers.TryGetValue(form, out string first))
                    problems.Add($"{who}: form {form} is issued by '{first}' too; a form has one issuing office.");
                else
                    issuers[form] = office.id;
            }
        }
        foreach (string form in formNumbers ?? Array.Empty<string>())
            if (!string.IsNullOrEmpty(form) && !issuers.ContainsKey(form))
                problems.Add($"agency.offices: no office issues form {form}; every traveller form carries its office's seal.");
        return problems;
    }

    /// <summary>True for one to MaxLegend capital letters or digits.</summary>
    private static bool IsLegend(string legend) =>
        legend.Length >= 1 && legend.Length <= MaxLegend && legend.All(c => (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'));
}
