using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// Pose frames on dialogue beats (Saleh 2026-10-07, TravellerPose), their key
/// names (LookKeys.Posed, Hands, TryParsePose), the moving set's all-or-
/// nothing fallback, and the art set a traveller is drawn from (LookArtSets:
/// never a mix of the 80s and the classic styles). The look: an Egyptian man
/// of the Old Kingdom (kilt, bobbed wig, wesekh collar), skin 1, face a,
/// brown hair.
/// </summary>
public class TravellerPoseTests
{
    private static TravellerLook Egyptian(bool collar = true)
    {
        var wardrobe = new PlaceWardrobe
        {
            male = new GenderLook
            {
                signature = LookSlot.Accessory,
                outfit = new LookItem { label = "pleated linen kilt" },
                hair = new LookItem { label = "bobbed wig", wig = true },
                accessory = collar ? new LookItem { label = "wesekh collar", leakable = true } : new LookItem()
            },
            female = new GenderLook { outfit = new LookItem { label = "linen sheath dress" } }
        };
        var claim = new LookSource { NationId = "egypt", EraId = "ancient", PlaceId = "egypt_ancient", Wardrobe = wardrobe, CultureValue = "wesekh collar" };
        var rules = new LookRules { faceBands = { new FaceBand { minAge = 18, faces = { "a" } } } };
        var weights = new LookWeights { skin = new[] { 1f, 0f, 0f, 0f, 0f }, hair = { new HairColourWeight { colour = "brown", weight = 1f } } };
        return Looks.Compose(claim, null, TravellerGender.Male, null, -2500, weights, rules, new SeededRandom(1));
    }

    private static TravellerLook Caesar() => Looks.Whole("caesar", null, new LookRules { wholeFigureLabel = "Period dress" });

    private static System.Func<string, bool> Drawn(params string[] names)
    {
        var set = new HashSet<string>(names);
        return set.Contains;
    }

    private static string KeyOn(TravellerLook look, LookLayer layer) => look.PartOn(layer)?.Key.Name;

    // --- Key names and parsing ---

    [Test]
    public void Posed_AppendsThePoseAfterTwoUnderscores_AndKeepsTheKeysParts()
    {
        LookKey outfit = LookKeys.Garment(LookLayer.Outfit, TravellerGender.Male, "egypt", "ancient", null);
        LookKey posed = LookKeys.Posed(outfit, "explaining_a");
        Assert.AreEqual("outfit_m_egypt_ancient__explaining_a", posed.Name);
        Assert.AreEqual(LookLayer.Outfit, posed.Layer);
        Assert.AreEqual("egypt", posed.NationId);
        Assert.AreEqual("explaining_a", posed.Pose);
        Assert.IsNull(outfit.Pose, "a neutral key has no pose");
        Assert.AreEqual("body_f_skin3__thinking_c", LookKeys.Posed(LookKeys.Body(TravellerGender.Female, 3), "thinking_c").Name);
    }

    [Test]
    public void Posed_PremadeWholePicture_DropsTheExpression()
    {
        Assert.AreEqual("premade_caesar__objecting_a", LookKeys.Posed(LookKeys.Premade("caesar", "angry"), "objecting_a").Name);
        Assert.AreEqual("premade_caesar__objecting_a", LookKeys.Posed(LookKeys.Premade("caesar", "neutral"), "objecting_a").Name);
    }

    [Test]
    public void Hands_AreNamedBySkinAndPose_OnTheHandsLayer()
    {
        LookKey hands = LookKeys.Hands(TravellerGender.Female, 4, "thinking_b");
        Assert.AreEqual("hands_f_skin4__thinking_b", hands.Name);
        Assert.AreEqual(LookLayer.Hands, hands.Layer);
        Assert.AreEqual("thinking_b", hands.Pose);
    }

    [Test]
    public void TryParsePose_SplitsAtTheSeparator_AndRejectsNeutralKeys()
    {
        Assert.IsTrue(LookKeys.TryParsePose("outfit_m_egypt_ancient__explaining_a", out string neutral, out string pose));
        Assert.AreEqual("outfit_m_egypt_ancient", neutral);
        Assert.AreEqual("explaining_a", pose);
        Assert.IsTrue(LookKeys.TryParsePose("premade_caesar__thinking_a", out neutral, out pose));
        Assert.AreEqual("premade_caesar", neutral);
        Assert.IsFalse(LookKeys.TryParsePose("hair_m_civil_2150_brown", out neutral, out pose));
        Assert.AreEqual("hair_m_civil_2150_brown", neutral);
        Assert.IsNull(pose);
        Assert.IsFalse(LookKeys.TryParsePose("outfit_m_x_y__", out _, out pose), "a blank pose is no pose");
        Assert.IsFalse(LookKeys.TryParsePose(null, out _, out _));
    }

    [Test]
    public void TryParse_PoseIds_CategoryAndVariant()
    {
        Assert.IsTrue(TravellerPose.TryParse("objecting_c", out string category, out string variant));
        Assert.AreEqual(TravellerPose.Objecting, category);
        Assert.AreEqual("c", variant);
        Assert.IsFalse(TravellerPose.TryParse("neutral", out _, out _));
        Assert.IsFalse(TravellerPose.TryParse("dancing_a", out _, out _));
        Assert.IsFalse(TravellerPose.TryParse("thinking_d", out _, out _));
        Assert.AreEqual("thinking_b", TravellerPose.Id(TravellerPose.Thinking, "b"));
    }

    [Test]
    public void APoseFrame_HasNoStandIn()
    {
        var table = new LookArtFallbackTable();
        var universe = new LookArtUniverse(new[] { "egypt", "iraq" }, new[] { "ancient", "medieval" }, new[] { "a", "b" });
        LookKey posed = LookKeys.Posed(LookKeys.Garment(LookLayer.Outfit, TravellerGender.Male, "egypt", "ancient", null), "explaining_a");
        CollectionAssert.AreEqual(new[] { posed.Name }, LookArtFallback.Candidates(posed, table, universe).Select(k => k.Name).ToArray());
    }

    // --- Beats ---

    [Test]
    public void ForLine_AnswerExplains_GestureWins_OtherLinesAreNeutral()
    {
        var answer = DialogLine.Answer("a1", "I pay in grain.", new InterviewAnswer());
        var claim = new DialogLine("claim", DialogSpeaker.Traveller, "Memphis.");
        Assert.AreEqual(TravellerPose.Explaining, TravellerPose.ForLine(answer));
        Assert.AreEqual(TravellerPose.Neutral, TravellerPose.ForLine(claim));
        Assert.AreEqual(TravellerPose.Objecting, TravellerPose.ForLine(claim.WithGesture(TravellerPose.Objecting)));
        Assert.AreEqual(TravellerPose.Neutral, TravellerPose.ForLine(null));
    }

    [Test]
    public void ForBeat_TheLineFirst_ThenThinkingWhileTheWheelIsOpen_ElseNeutral()
    {
        var answer = DialogLine.Answer("a1", "I pay in grain.", new InterviewAnswer());
        var claim = new DialogLine("claim", DialogSpeaker.Traveller, "Memphis.");
        Assert.AreEqual(TravellerPose.Neutral, TravellerPose.ForBeat(null, false), "arrival, idle, after a response");
        Assert.AreEqual(TravellerPose.Thinking, TravellerPose.ForBeat(null, true), "waiting on a question");
        Assert.AreEqual(TravellerPose.Thinking, TravellerPose.ForBeat(claim, true), "a neutral line keeps them thinking while the wheel is open");
        Assert.AreEqual(TravellerPose.Explaining, TravellerPose.ForBeat(answer, true));
        Assert.AreEqual(TravellerPose.Objecting, TravellerPose.ForBeat(claim.WithGesture(TravellerPose.Objecting), false));
    }

    [Test]
    public void WithGesture_KeepsTheLine()
    {
        var line = new DialogLine("r1", DialogSpeaker.Traveller, "That is not fair!", "angry");
        DialogLine objecting = line.WithGesture(TravellerPose.Objecting);
        Assert.AreEqual(line.Id, objecting.Id);
        Assert.AreEqual(line.Text, objecting.Text);
        Assert.AreEqual("angry", objecting.Expression);
        Assert.AreEqual(TravellerPose.Objecting, objecting.Gesture);
        Assert.IsNull(line.Gesture);
    }

    // --- Frames and the all-or-nothing fallback ---

    [Test]
    public void Posed_Generated_SwapsBodyOutfitAndAccessory_ForTheOutfitsVariant()
    {
        TravellerLook look = Egyptian();
        TravellerLook posed = TravellerPose.Posed(look, TravellerPose.Explaining, Drawn(
            "outfit_m_egypt_ancient__explaining_a", "body_m_skin1__explaining_a", "accessory_m_egypt_ancient__explaining_a"));
        Assert.AreEqual("body_m_skin1__explaining_a", KeyOn(posed, LookLayer.Body));
        Assert.AreEqual("outfit_m_egypt_ancient__explaining_a", KeyOn(posed, LookLayer.Outfit));
        Assert.AreEqual("accessory_m_egypt_ancient__explaining_a", KeyOn(posed, LookLayer.Accessory));
        Assert.AreEqual(KeyOn(look, LookLayer.Head), KeyOn(posed, LookLayer.Head), "the head stays");
        Assert.AreEqual(KeyOn(look, LookLayer.Hair), KeyOn(posed, LookLayer.Hair), "the hair stays");
        Assert.AreEqual(look.Garments, posed.Garments, "the garments the player looks at do not change");
        Assert.AreEqual(Looks.IdentityKey(look), Looks.IdentityKey(posed));
    }

    [Test]
    public void Posed_TakesTheVariantTheOutfitIsDrawnIn()
    {
        TravellerLook posed = TravellerPose.Posed(Egyptian(collar: false), TravellerPose.Objecting, Drawn(
            "outfit_m_egypt_ancient__objecting_c", "body_m_skin1__objecting_a", "body_m_skin1__objecting_c"));
        Assert.AreEqual("outfit_m_egypt_ancient__objecting_c", KeyOn(posed, LookLayer.Outfit));
        Assert.AreEqual("body_m_skin1__objecting_c", KeyOn(posed, LookLayer.Body));
    }

    [Test]
    public void Posed_AMissingMovingLayer_KeepsTheWholeSetNeutral()
    {
        TravellerLook look = Egyptian();
        // No posed body (GPT's body pose sources are still to come): nothing moves, never an outfit's arms over a neutral body.
        Assert.AreSame(look, TravellerPose.Posed(look, TravellerPose.Explaining, Drawn(
            "outfit_m_egypt_ancient__explaining_a", "accessory_m_egypt_ancient__explaining_a")));
        // No posed accessory: the same.
        Assert.AreSame(look, TravellerPose.Posed(look, TravellerPose.Explaining, Drawn(
            "outfit_m_egypt_ancient__explaining_a", "body_m_skin1__explaining_a")));
        // No posed outfit at all.
        Assert.AreSame(look, TravellerPose.Posed(look, TravellerPose.Explaining, Drawn("body_m_skin1__explaining_a")));
    }

    [Test]
    public void Posed_AHandOverFacePose_NeedsItsHands_DrawnOnTop()
    {
        TravellerLook look = Egyptian(collar: false);
        string[] frame = { "outfit_m_egypt_ancient__thinking_b", "body_m_skin1__thinking_b" };
        Assert.AreSame(look, TravellerPose.Posed(look, TravellerPose.Thinking, Drawn(frame)), "no hands: neutral");

        TravellerLook posed = TravellerPose.Posed(look, TravellerPose.Thinking, Drawn(frame.Append("hands_m_skin1__thinking_b").ToArray()));
        Assert.AreEqual("hands_m_skin1__thinking_b", KeyOn(posed, LookLayer.Hands));
        Assert.AreEqual(LookLayer.Hands, posed.Parts.Last().Layer, "drawn over everything");

        TravellerLook linked = TravellerPose.Posed(look, TravellerPose.Thinking, Drawn("outfit_m_egypt_ancient__thinking_c", "body_m_skin1__thinking_c"));
        Assert.AreEqual("body_m_skin1__thinking_c", KeyOn(linked, LookLayer.Body));
        Assert.IsNull(KeyOn(linked, LookLayer.Hands), "hands linked at the waist cross no face");
    }

    [Test]
    public void Posed_NeutralOrUnknownCategory_IsTheLookItself()
    {
        TravellerLook look = Egyptian();
        var all = Drawn("outfit_m_egypt_ancient__explaining_a", "body_m_skin1__explaining_a", "accessory_m_egypt_ancient__explaining_a");
        Assert.AreSame(look, TravellerPose.Posed(look, TravellerPose.Neutral, all));
        Assert.AreSame(look, TravellerPose.Posed(look, "dancing", all));
        Assert.IsNull(TravellerPose.Posed(null, TravellerPose.Explaining, all));
    }

    [Test]
    public void Posed_Premade_SwapsTheWholePictureForItsFrame()
    {
        TravellerLook caesar = Caesar();
        TravellerLook posed = TravellerPose.Posed(caesar, TravellerPose.Thinking, Drawn("premade_caesar__thinking_a"));
        Assert.AreEqual("premade_caesar__thinking_a", KeyOn(posed, LookLayer.Whole));
        Assert.AreEqual("caesar", posed.PremadeId);
        Assert.IsNull(KeyOn(posed, LookLayer.Hands), "a premade's frame is whole: no hands layer");
        Assert.AreSame(caesar, TravellerPose.Posed(caesar, TravellerPose.Objecting, Drawn("premade_caesar__thinking_a")), "no objecting frame: neutral");
    }

    // --- Art sets ---

    [Test]
    public void ArtSet_Retro_OnlyWhenEveryKeyIsDrawnInIt()
    {
        TravellerLook look = Egyptian();
        string[] keys = look.Keys.Select(k => k.Name).ToArray();
        Assert.AreEqual(LookArtSets.Retro, LookArtSets.For(look, Drawn(keys)));
        Assert.AreEqual(LookArtSets.Classic, LookArtSets.For(look, Drawn(keys.Skip(1).ToArray())), "one key missing: the whole look stays classic");
        Assert.AreEqual(LookArtSets.Classic, LookArtSets.For(null, Drawn(keys)));
        Assert.AreEqual(LookArtSets.Retro, LookArtSets.For(Caesar(), Drawn("premade_caesar_neutral")));
    }

    [Test]
    public void ArtSet_Paths()
    {
        Assert.AreEqual("80s/body_m_skin1", LookArtSets.PathOf(LookArtSets.Retro, "body_m_skin1"));
        Assert.AreEqual("body_m_skin1", LookArtSets.PathOf(LookArtSets.Classic, "body_m_skin1"));
    }
}
