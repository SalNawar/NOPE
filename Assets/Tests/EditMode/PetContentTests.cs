using System.Collections.Generic;
using NUnit.Framework;

/// <summary>The pet's words and the adoption's name rule (the Home pet spec PS1, PS4): needs in words picked by level, the tokens filled, the paper's lines, and what the content may not hold.</summary>
public class PetContentTests
{
    /// <summary>Sound pet words (shared with HomeContentTests).</summary>
    internal static PetContent Sound() => Content();

    private static PetContent Content() => new PetContent
    {
        nameMaxLength = 12,
        kinds = new List<PetKindContent>
        {
            new PetKindContent { kind = PetKind.Dog, word = "dog", suggestedName = "Biscuit", reactions = new List<string> { "{name} wags.", "{name} leans in." }, toyLines = new List<string> { "{name} chases the {toy}." }, coats = Coats("cream", "tan", "chocolate") },
            new PetKindContent { kind = PetKind.Cat, word = "cat", suggestedName = "Miso", reactions = new List<string> { "{name} purrs." }, toyLines = new List<string> { "{name} bats the {toy}." }, coats = Coats("white", "ginger") }
        },
        hunger = new List<string> { "{name} is fed.", "{name} is hungry.", "{name} is very hungry.", "{name} is starving." },
        cold = new List<string> { "{name} is warm.", "{name} is cold." },
        boredom = new List<string> { "{name} is playful.", "{name} is bored." },
        sickness = new List<string> { "{name} is healthy.", "{name} is sick." },
        worse = "{name} caught something.",
        better = "{name} is better.",
        welfareNotice = "A notice about {name}.",
        paperAdopted = "{name} the {kind} has a home.",
        paperWelfare = "Inspectors ask after a {kind} called {name}."
    };

    /// <summary>Coats with these ids, each named by its "adopt.coat.&lt;id&gt;" UI string.</summary>
    private static List<PetCoatContent> Coats(params string[] ids)
    {
        var coats = new List<PetCoatContent>();
        foreach (string id in ids)
            coats.Add(new PetCoatContent { id = id, nameKey = "adopt.coat." + id });
        return coats;
    }

    private static PetState Biscuit => new PetState { kind = PetKind.Dog, name = "Biscuit", adoptedDay = 1 };

    [Test]
    public void Need_TheWordForTheLevel_Filled()
    {
        PetContent c = Content();
        Assert.AreEqual("Biscuit is very hungry.", c.Need(PetNeed.Hunger, Biscuit, new PetNeeds(2, 0, 0, 0), 3));
        Assert.AreEqual("Biscuit is cold.", c.Need(PetNeed.Cold, Biscuit, new PetNeeds(0, 1, 0, 0), 3), "two words: any step is the second");
        Assert.AreEqual("Biscuit is healthy.", c.Need(PetNeed.Sickness, Biscuit, new PetNeeds(3, 3, 3, 0), 3));
    }

    [Test]
    public void Need_NeverANumber()
    {
        PetContent c = Content();
        foreach (PetNeed need in new[] { PetNeed.Hunger, PetNeed.Cold, PetNeed.Boredom, PetNeed.Sickness })
            for (int level = -1; level <= 5; level++)
                StringAssert.DoesNotMatch("[0-9%]", c.Need(need, Biscuit, new PetNeeds(level, level, level, level), 3));
    }

    [Test]
    public void InTurn_RoundAgain_WithTheToy()
    {
        PetContent c = Content();
        PetKindContent dog = c.Kind(PetKind.Dog);
        Assert.AreEqual("Biscuit wags.", c.InTurn(dog.reactions, 0, Biscuit));
        Assert.AreEqual("Biscuit leans in.", c.InTurn(dog.reactions, 3, Biscuit));
        Assert.AreEqual("Biscuit chases the ball.", c.InTurn(dog.toyLines, 7, Biscuit, "ball"));
        Assert.AreEqual(string.Empty, c.InTurn(null, 0, Biscuit));
    }

    [Test]
    public void PaperLine_TheAdoptionTheNextMorning_ThenTheWelfareOffice()
    {
        PetContent c = Content();
        Assert.AreEqual("Biscuit the dog has a home.", c.PaperLine(Biscuit, 2));
        Assert.AreEqual(string.Empty, c.PaperLine(Biscuit, 3));
        PetState neglected = Biscuit;
        neglected.welfareNights = 1;
        Assert.AreEqual("Inspectors ask after a dog called Biscuit.", c.PaperLine(neglected, 5));
        neglected.taken = true;
        Assert.AreEqual(string.Empty, c.PaperLine(neglected, 5), "taken: the ending says it");
        Assert.AreEqual(string.Empty, c.PaperLine(new PetState(), 2), "no pet adopted");
    }

    [Test]
    public void Problems_SoundContent_HasNone()
    {
        CollectionAssert.IsEmpty(Content().Problems());
    }

    [Test]
    public void Problems_AMissingKind_ABadSuggestedName_AShortNeed_AMissingToken()
    {
        PetContent c = Content();
        c.kinds.RemoveAt(1);
        c.kinds[0].suggestedName = "R2-D2";
        c.cold = new List<string> { "{name} is warm." };
        c.worse = "It caught something.";
        List<string> problems = c.Problems();
        Assert.IsTrue(problems.Exists(p => p.Contains("no Cat row")), string.Join("\n", problems));
        Assert.IsTrue(problems.Exists(p => p.Contains("R2-D2")));
        Assert.IsTrue(problems.Exists(p => p.Contains("home.pet.cold")));
        Assert.IsTrue(problems.Exists(p => p.Contains("home.pet.worse")));
    }

    [Test]
    public void CoatOf_AListedCoat_ElseTheKindsFirst()
    {
        PetContent c = Content();
        Assert.AreEqual("tan", c.CoatOf(PetKind.Dog, "tan"));
        Assert.AreEqual("ginger", c.CoatOf(PetKind.Cat, "ginger"));
        Assert.AreEqual("cream", c.CoatOf(PetKind.Dog, ""), "a save from before the coats: the first coat");
        Assert.AreEqual("cream", c.CoatOf(PetKind.Dog, null));
        Assert.AreEqual("white", c.CoatOf(PetKind.Cat, "tan"), "another kind's coat is not this kind's");
        Assert.AreEqual("white", c.CoatOf(PetKind.Cat, "tabby"), "a coat the content dropped");
        Assert.AreEqual(string.Empty, new PetContent().CoatOf(PetKind.Dog, "tan"), "no content: no coat");
    }

    [Test]
    public void Problems_AKindWithoutCoats_ARepeatedCoat_ACoatWithoutIdOrName()
    {
        PetContent c = Content();
        c.kinds[0].coats.Add(new PetCoatContent { id = "tan", nameKey = "adopt.coat.tan" });
        c.kinds[0].coats.Add(new PetCoatContent { id = " ", nameKey = "" });
        c.kinds[1].coats.Clear();
        List<string> problems = c.Problems();
        Assert.IsTrue(problems.Exists(p => p.Contains("coat 'tan' twice")), string.Join("\n", problems));
        Assert.IsTrue(problems.Exists(p => p.Contains("coats[4] has no id")));
        Assert.IsTrue(problems.Exists(p => p.Contains("coats[4] has no nameKey")));
        Assert.IsTrue(problems.Exists(p => p.Contains("Cat has no coats")));
    }

    /// <summary>
    /// The save's own serializer (SaveSystem writes WorldState with
    /// JsonUtility) keeps the coat, and a save written before the coats loads
    /// it blank, which reads as the kind's first coat. JsonUtility is native
    /// code: it runs in Unity's EditMode suite only, and the offline runner
    /// (outside Unity) skips the test.
    /// </summary>
    [Test]
    public void PetState_TheCoatRoundTripsInTheSave_AndAnOlderSaveLoadsItBlank()
    {
        var pet = new PetState { kind = PetKind.Cat, name = "Miso", coat = "ginger", adoptedDay = 1 };
        string json;
        try
        {
            json = UnityEngine.JsonUtility.ToJson(pet);
        }
        catch (System.Security.SecurityException)
        {
            return; // outside Unity: no native JsonUtility
        }
        StringAssert.Contains("\"coat\":\"ginger\"", json);
        PetState back = UnityEngine.JsonUtility.FromJson<PetState>(json);
        Assert.AreEqual("ginger", back.coat);
        Assert.AreEqual(PetKind.Cat, back.kind);

        PetState older = UnityEngine.JsonUtility.FromJson<PetState>(json.Replace(",\"coat\":\"ginger\"", string.Empty));
        Assert.AreEqual(string.Empty, older.coat, "a save from before the coats has none");
        Assert.AreEqual("white", Content().CoatOf(older.kind, older.coat), "and wears the kind's first coat");
    }

    [Test]
    public void HomeProblems_EveryBillOnce_NamedAndPriced()
    {
        var home = new HomeContent
        {
            pet = Content(),
            bills = new List<BillRow>
            {
                new BillRow { bill = HomeBill.Food, name = "Food", price = 10 },
                new BillRow { bill = HomeBill.Food, name = "Food again", price = 10 },
                new BillRow { bill = HomeBill.Heating, name = "", price = 8 },
                new BillRow { bill = HomeBill.Tv, name = "TV", price = -1 }
            }
        };
        List<string> problems = home.Problems(new string[0]);
        Assert.IsTrue(problems.Exists(p => p.Contains("Food twice")), string.Join("\n", problems));
        Assert.IsTrue(problems.Exists(p => p.Contains("Heating has no name")));
        Assert.IsTrue(problems.Exists(p => p.Contains("Tv costs below 0")));
        Assert.IsTrue(problems.Exists(p => p.Contains("no Electricity row")));
        Assert.IsTrue(problems.Exists(p => p.Contains("no Medicine row")));
        Assert.AreEqual(10, home.Bill(HomeBill.Food).price);
        Assert.IsNull(home.Bill(HomeBill.Medicine));
    }

    [TestCase("Biscuit", PetNameProblem.None)]
    [TestCase("  Mister   Whiskers ", PetNameProblem.None)]
    [TestCase("O'Malley-Jones", PetNameProblem.None)]
    [TestCase("فلفل", PetNameProblem.None)]
    [TestCase("ミソ", PetNameProblem.None)]
    [TestCase("", PetNameProblem.Empty)]
    [TestCase("   ", PetNameProblem.Empty)]
    [TestCase(null, PetNameProblem.Empty)]
    [TestCase("Bartholomew Tiberius", PetNameProblem.TooLong)]
    [TestCase("Rex2", PetNameProblem.BadCharacters)]
    [TestCase("<b>Rex</b>", PetNameProblem.BadCharacters)]
    [TestCase("Rex!", PetNameProblem.BadCharacters)]
    public void Names_Check(string raw, PetNameProblem expected)
    {
        Assert.AreEqual(expected, PetNames.Check(raw, 16));
    }

    [Test]
    public void Names_Clean_TrimsAndJoinsSpaces()
    {
        Assert.AreEqual("Mister Whiskers", PetNames.Clean("  Mister \t  Whiskers "));
        Assert.AreEqual(string.Empty, PetNames.Clean(null));
    }
}
