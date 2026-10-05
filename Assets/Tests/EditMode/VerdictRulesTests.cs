using NUnit.Framework;

/// <summary>The verdict table (traveller types §5.2): the two fault kinds, and the evidence gate on every right denial (Saleh, 2026-10-05).</summary>
public class VerdictRulesTests
{
    [TestCase(false, false, true, Description = "no fault: accept")]
    [TestCase(true, false, false, Description = "a deviation fault (a lie, a costume error): deny")]
    [TestCase(false, true, false, Description = "a directive fault (a closed destination): deny")]
    [TestCase(true, true, false, Description = "both (never generated, K5): deny")]
    public void ShouldAccept_OnlyATravellerWithNoFault(bool deviation, bool directive, bool expected)
    {
        Assert.AreEqual(expected, VerdictRules.ShouldAccept(deviation, directive));
    }

    // Row 1 is the one unproven denial; every other row flips one input of it.
    [TestCase(true, 0, false, true, false, true)]
    [TestCase(false, 0, false, true, false, false, Description = "the gate is off")]
    [TestCase(true, -1, false, true, false, false, Description = "no evidence system")]
    [TestCase(true, 1, false, true, false, false, Description = "a deviation logged")]
    [TestCase(true, 0, true, true, false, false, Description = "accepted")]
    [TestCase(true, 0, false, false, false, false, Description = "no fault: a wrong denial, not an unproven one")]
    [TestCase(true, 0, false, true, true, true, Description = "both kinds of fault, nothing logged")]
    [TestCase(true, 0, false, false, true, true, Description = "a directive denial needs evidence too (Saleh, 2026-10-05)")]
    [TestCase(true, 1, false, false, true, false, Description = "a directive fault logged (a rule broken, a date that fails)")]
    public void IsUnprovenDenial_AnUnevidencedDenialOfAnyFault(bool requireEvidence, int evidenceCount, bool accepted, bool deviation, bool directive, bool expected)
    {
        Assert.AreEqual(expected, VerdictRules.IsUnprovenDenial(requireEvidence, evidenceCount, accepted, deviation, directive));
    }

    /// <summary>
    /// Redesign phase 23 (Saleh, 2026-09-29: "clerk is fined for any mistake on application the same either approval or rejection"):
    /// every wrong decision past the day's free warnings costs the one penalty, the fifth as much as the first.
    /// </summary>
    [TestCase(1, 0, 10, 10, Description = "no free warnings: the first mistake is fined")]
    [TestCase(5, 0, 10, 10, Description = "no escalation: the fifth costs what the first did")]
    [TestCase(1, 1, 10, 0, Description = "a free warning costs nothing")]
    [TestCase(2, 1, 10, 10, Description = "the warnings used, the one penalty")]
    [TestCase(9, 1, 10, 10)]
    [TestCase(3, 0, 0, 0, Description = "a penalty of 0 fines nothing")]
    [TestCase(3, 0, -5, 0, Description = "never below 0")]
    public void WrongDecisionPenalty_TheOnePenaltyPastTheFreeWarnings(int citationNumberToday, int freeWarnings, int penalty, int expected)
    {
        Assert.AreEqual(expected, VerdictRules.WrongDecisionPenalty(citationNumberToday, freeWarnings, penalty));
    }

    /// <summary>Phase 23 part 1b (Saleh: "one free warning but configurable and tunable"): with one free warning a day, the day's first wrong decision is the warning and every later one the one penalty; the next day starts over.</summary>
    [Test]
    public void OneFreeWarning_ThenTheOnePenalty_EachDay()
    {
        for (int day = 1; day <= 2; day++)
        {
            Assert.IsTrue(VerdictRules.IsFreeWarning(1, 1), $"day {day}: the first mistake is the warning");
            Assert.AreEqual(0, VerdictRules.WrongDecisionPenalty(1, 1, 10));
            for (int n = 2; n <= 6; n++)
                Assert.AreEqual(10, VerdictRules.WrongDecisionPenalty(n, 1, 10), $"day {day}: mistake {n} costs the one penalty");
        }
    }

    [TestCase(1, 0, false)]
    [TestCase(1, 1, true)]
    [TestCase(2, 1, false)]
    [TestCase(2, 2, true)]
    public void IsFreeWarning_WhileTheDaysWarningsLast(int citationNumberToday, int freeWarnings, bool expected)
    {
        Assert.AreEqual(expected, VerdictRules.IsFreeWarning(citationNumberToday, freeWarnings));
    }
}
