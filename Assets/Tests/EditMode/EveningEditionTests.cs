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
        paperWelfare = "ANIMAL WELFARE: inspectors took a {kind} called {name} from Block C."
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

    [Test]
    public void PetFate_ATakenPet_IsTheWelfareOfficesLine_AndNoPetSaysNothing()
    {
        PetState pet = Biscuit();
        pet.taken = true;
        Assert.AreEqual("ANIMAL WELFARE: inspectors took a dog called Biscuit from Block C.", EveningEdition.PetFate(pet, Words(), 3));
        Assert.AreEqual(string.Empty, EveningEdition.PetFate(new PetState(), Words(), 3), "never adopted");
        Assert.AreEqual(string.Empty, EveningEdition.PetFate(Biscuit(), null, 3));
    }
}
