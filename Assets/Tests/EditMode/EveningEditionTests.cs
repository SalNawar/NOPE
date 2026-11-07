using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The ending screen's evening edition (FLOW, BUG-56a): the ending's headline
/// and story with the pet's name put in, the closing card set apart, and the
/// pet's fate in Home's own words (never a number).
/// </summary>
public class EveningEditionTests
{
    private static PetContent Words() => new PetContent
    {
        kinds = new List<PetKindContent> { new PetKindContent { kind = PetKind.Dog, word = "dog" } },
        hunger = new List<string> { "{name} is fed.", "{name} is hungry.", "{name} is starving." },
        cold = new List<string> { "{name} is warm.", "{name} is cold." },
        boredom = new List<string> { "{name} is content.", "{name} is bored." },
        sickness = new List<string> { "{name} is healthy.", "{name} is sick." },
        paperWelfare = "ANIMAL WELFARE: inspectors are asking after a {kind} called {name} in Block C."
    };

    private static PetState Biscuit() => new PetState { kind = PetKind.Dog, name = "Biscuit" };

    [Test]
    public void Compose_PrintsTheEndingsHeadlineAndStory_WithThePetsNameIn_AndTheCardSetApart()
    {
        EveningEditionPage page = EveningEdition.Compose("The Temporal Times", "Edition of day 9", "Fired", "The End",
                                                         "You are dismissed. {name} the {kind} waits at home.", "END OF DEMO", Biscuit(), Words(), 3);
        Assert.AreEqual("The Temporal Times", page.Masthead);
        Assert.AreEqual("Edition of day 9", page.Edition);
        Assert.AreEqual("Fired", page.Headline);
        Assert.AreEqual("You are dismissed. Biscuit the dog waits at home.\n\nEND OF DEMO", page.Body);
    }

    [Test]
    public void Compose_ABlankHeadlineTakesTheFallback_AndNoCardAddsNothing()
    {
        EveningEditionPage page = EveningEdition.Compose("M", "E", "  ", "The End", "Story.", "", null, null, 3);
        Assert.AreEqual("The End", page.Headline);
        Assert.AreEqual("Story.", page.Body);
        Assert.AreEqual(string.Empty, page.PetLine, "no pet, no fate line");
    }

    [Test]
    public void PetFate_SaysEachNeedInHomesWords_AtItsLevel()
    {
        PetState pet = Biscuit();
        pet.hunger = 3;
        pet.sickness = 1;
        Assert.AreEqual("Biscuit is starving. Biscuit is warm. Biscuit is content. Biscuit is sick.", EveningEdition.PetFate(pet, Words(), 3));
        StringAssert.DoesNotContain("3", EveningEdition.PetFate(pet, Words(), 3), "never a number");
    }

    /// <summary>A taken pet has no fate line: the Welfare Office's own story tells it, and the paper's line (paperWelfare, "inspectors are asking after a dog called Biscuit") is the warning of a night at the worst, which reads as if the pet were still at home (qa-endings-saves, 2026-11-07).</summary>
    [Test]
    public void PetFate_ATakenPet_SaysNothing_TheStoryTellsIt_AndNoPetSaysNothing()
    {
        PetState pet = Biscuit();
        pet.taken = true;
        pet.hunger = 3;
        pet.welfareNights = 2;
        Assert.AreEqual(string.Empty, EveningEdition.PetFate(pet, Words(), 3), "taken: the Welfare Office's story is the fate");
        Assert.AreEqual(string.Empty, EveningEdition.PetFate(new PetState(), Words(), 3), "never adopted");
        Assert.AreEqual(string.Empty, EveningEdition.PetFate(Biscuit(), null, 3));
    }

    [Test]
    public void Compose_ATakenPet_PrintsTheWelfareStory_AndNoWarningLine()
    {
        PetState pet = Biscuit();
        pet.taken = true;
        EveningEditionPage page = EveningEdition.Compose("M", "Edition of day 4", "The Welfare Office", "The End",
                                                         "{name} the {kind} has been rehomed.", null, pet, Words(), 3);
        Assert.AreEqual("Biscuit the dog has been rehomed.", page.Body);
        Assert.AreEqual(string.Empty, page.PetLine);
    }
}
