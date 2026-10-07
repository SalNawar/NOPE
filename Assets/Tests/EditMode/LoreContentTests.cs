using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;

/// <summary>
/// The scanner app spec §3 against today's world_source.json "lore": every
/// premade has its 3 to 4 lines, there are 30 or more flavour templates
/// across the tourist, labourer and displaced kinds, every template's slots
/// are valid (CitizenFile.Problems, Generate World's own check), and every
/// kind of traveller gets a full file with no slot left unfilled.
/// </summary>
public class LoreContentTests
{
    private const string SourcePath = "Assets/Data/World/world_source.json";

    private static ContentNode Source([CallerFilePath] string here = "")
    {
        string path = File.Exists(SourcePath) ? SourcePath : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", SourcePath);
        return ContentJson.Parse(File.ReadAllText(path));
    }

    private static List<string> Strings(ContentNode node) => node == null ? new List<string>() : node.Items.Select(i => i.Text).ToList();

    private static string Str(ContentNode node, string key) => node.Get(key)?.Text ?? string.Empty;

    /// <summary>The lore as Generate World writes it (kinds parsed by name).</summary>
    private static LoreContent Lore(ContentNode root)
    {
        ContentNode l = root.Get("lore");
        Assert.IsNotNull(l, "world_source.json has a \"lore\" section");
        return new LoreContent
        {
            clueChance = float.Parse(Str(l, "clueChance"), CultureInfo.InvariantCulture),
            linesMin = int.Parse(Str(l, "linesMin"), CultureInfo.InvariantCulture),
            linesMax = int.Parse(Str(l, "linesMax"), CultureInfo.InvariantCulture),
            yearMin = int.Parse(Str(l, "yearMin"), CultureInfo.InvariantCulture),
            yearMax = int.Parse(Str(l, "yearMax"), CultureInfo.InvariantCulture),
            relatives = Strings(l.Get("relatives")),
            premades = l.Get("premades").Items.Select(p => new LorePremade { premade = Str(p, "premade"), lines = Strings(p.Get("lines")) }).ToList(),
            templates = l.Get("templates").Items.Select(t => new LoreTemplate
            {
                id = Str(t, "id"),
                kinds = Strings(t.Get("kinds")).Select(k => (TravellerKind)Enum.Parse(typeof(TravellerKind), k)).ToList(),
                personality = Str(t, "personality"),
                traits = Strings(t.Get("traits")),
                clue = Str(t, "clue"),
                text = Str(t, "text")
            }).ToList(),
            threads = l.Get("threads").Items.Select(t => new LoreThread { id = Str(t, "id"), verdict = Str(t, "verdict"), text = Str(t, "text") }).ToList()
        };
    }

    [Test]
    public void TodaysLore_IsSound_EveryPremadeHasItsLines()
    {
        ContentNode root = Source();
        LoreContent lore = Lore(root);
        List<string> premades = root.Get("premades").Items.Select(p => Str(p, "id")).ToList();
        List<string> cast = root.Get("personalities").Items.Select(p => Str(p, "id")).ToList();
        List<string> problems = CitizenFile.Problems(lore, premades, cast, true);
        Assert.IsEmpty(problems, string.Join("\n", problems));
        foreach (LorePremade p in lore.premades)
            Assert.That(p.lines.Count, Is.InRange(3, 4), $"{p.premade}: 3 to 4 snippets (the brief)");
    }

    [Test]
    public void TodaysLore_ThirtyTemplatesOrMore_AcrossTheKinds()
    {
        LoreContent lore = Lore(Source());
        List<LoreTemplate> flavour = lore.templates.Where(t => string.IsNullOrEmpty(t.clue)).ToList();
        Assert.GreaterOrEqual(flavour.Count, 30);
        foreach (TravellerKind kind in new[] { TravellerKind.RichTourist, TravellerKind.PoorTourist, TravellerKind.Labourer, TravellerKind.Displaced })
            Assert.GreaterOrEqual(flavour.Count(t => t.kinds.Contains(kind)), 4, $"{kind} has templates of its own");
        Assert.IsTrue(lore.templates.Any(t => !string.IsNullOrEmpty(t.clue)), "some templates are clues");
    }

    [Test]
    public void TodaysLore_EveryKindGetsAFullFile_NoSlotLeft()
    {
        LoreContent lore = Lore(Source());
        var subjects = new[]
        {
            new LoreSubject { Kind = TravellerKind.RichTourist, Name = "Aster Vale", Place = "Periclean Athens (Ancient)", Era = "Ancient", Role = "Artist", Status = "Premium", Trips = 2, Transponder = "Hopper Mk II", Personality = "grand" },
            new LoreSubject { Kind = TravellerKind.PoorTourist, Name = "Pell Ward", Place = "Abbasid Baghdad (Medieval)", Era = "Medieval", Role = "Merchant", Status = "Standard", Debt = 4200, Personality = "anxious", Fault = DirectiveFault.FrozenAccount, Frozen = true },
            new LoreSubject { Kind = TravellerKind.Labourer, Name = "Ines Varga", Place = "Victorian London (Industrial)", Era = "Industrial", Role = "Soldier", Status = "Eligible", Debt = 88200, Employer = "Tyburn Mills Consortium", Wage = "420 cr", Term = "180 days", Lie = LieKind.ForgedContract },
            new LoreSubject { Kind = TravellerKind.Displaced, Name = "Nakht", Place = "New Kingdom Thebes (Ancient)", Era = "Ancient", Role = "Scientist", Incident = "R-0311-07", Found = "11 Mar 2150", Lie = LieKind.FakeDisplaced }
        };
        foreach (LoreSubject s in subjects)
            for (int seed = 1; seed <= 60; seed++)
            {
                List<string> file = CitizenFile.Lines(lore, s, Seeds.ForLore(seed), null, 5);
                Assert.That(file.Count, Is.InRange(2, 4), $"{s.Kind}: a file of 2 to 4 lines");
                foreach (string line in file)
                {
                    Assert.IsFalse(line.Contains("{") || line.Contains("}"), $"{s.Kind}: every slot filled: {line}");
                    Assert.LessOrEqual(line.Length, LoreContent.MaxLineLength + 40, $"{s.Kind}: one line: {line}");
                }
            }
    }
}
