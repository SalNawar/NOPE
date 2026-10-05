using System;

/// <summary>The companion a run adopts at New Run (the Home pet spec; Saleh 2026-10-05: "adopt a dog or a cat"). Saved in WorldState.pet: append only.</summary>
public enum PetKind
{
    /// <summary>A dog.</summary>
    Dog,

    /// <summary>A cat.</summary>
    Cat
}

/// <summary>One of the pet's four needs, each a level from 0 (well) to GameConfigSO.petNeedMax (the worst), said in words only (PetContent.Need).</summary>
public enum PetNeed
{
    /// <summary>Hunger: fed by the Food bill.</summary>
    Hunger,

    /// <summary>Cold: warmed by the Heating bill (with Electricity).</summary>
    Cold,

    /// <summary>Boredom: eased by the TV (with Electricity) and by playing with a toy.</summary>
    Boredom,

    /// <summary>Sickness: treated by the Medicine bill; a neglected pet falls sick more often.</summary>
    Sickness
}

/// <summary>One of the night's optional bills at Home (the Home pet spec PS2; priced in world_source.json home.bills). Content names them; append only.</summary>
public enum HomeBill
{
    /// <summary>Food: the pet is fed tonight.</summary>
    Food,

    /// <summary>Heating: the pet is warm tonight, with Electricity.</summary>
    Heating,

    /// <summary>Electricity: powers the Heating and the TV.</summary>
    Electricity,

    /// <summary>TV: company for the evening, with Electricity.</summary>
    Tv,

    /// <summary>Medicine: one step better tonight, and no worse.</summary>
    Medicine
}

/// <summary>How the pet looks in its corner (the art slots' four states, ArtSlots.PetSprite; the code-drawn stand-in's poses).</summary>
public enum PetLook
{
    /// <summary>Neither well looked after nor in want.</summary>
    Idle,

    /// <summary>Every need met.</summary>
    Happy,

    /// <summary>Hungry, cold or bored past the first step.</summary>
    Sad,

    /// <summary>Unwell or worse.</summary>
    Sick
}

/// <summary>The pet's four need levels at one moment (0 well; PetRules caps each at the run's maximum).</summary>
public readonly struct PetNeeds
{
    /// <summary>How hungry.</summary>
    public readonly int Hunger;

    /// <summary>How cold.</summary>
    public readonly int Cold;

    /// <summary>How bored.</summary>
    public readonly int Boredom;

    /// <summary>How sick.</summary>
    public readonly int Sickness;

    /// <summary>Needs at these levels.</summary>
    public PetNeeds(int hunger, int cold, int boredom, int sickness)
    {
        Hunger = hunger;
        Cold = cold;
        Boredom = boredom;
        Sickness = sickness;
    }

    /// <summary>The level of <paramref name="need"/>.</summary>
    public int Of(PetNeed need) => need switch
    {
        PetNeed.Hunger => Hunger,
        PetNeed.Cold => Cold,
        PetNeed.Boredom => Boredom,
        _ => Sickness
    };
}

/// <summary>
/// The run's pet (the Home pet spec; saved in WorldState.pet; an older save
/// loads it unadopted and RunManager adopts the run config's default): what it
/// is, its name, its four needs, how many nights in a row it ended at the
/// worst of hunger, cold or sickness, whether the Welfare Office took it (the
/// failure ending), and last night's change in its health.
/// </summary>
[Serializable]
public sealed class PetState
{
    /// <summary>Dog or cat.</summary>
    public PetKind kind;

    /// <summary>The name the player gave it (PetNames.Clean; "" until adopted).</summary>
    public string name = string.Empty;

    /// <summary>The day it was adopted (the run's first, or the day an older save adopted the default): the next morning's paper announces it.</summary>
    public int adoptedDay;

    /// <summary>Hunger level (0 fed).</summary>
    public int hunger;

    /// <summary>Cold level (0 warm).</summary>
    public int cold;

    /// <summary>Boredom level (0 playful).</summary>
    public int boredom;

    /// <summary>Sickness level (0 healthy).</summary>
    public int sickness;

    /// <summary>Nights in a row that ended with hunger, cold or sickness at the worst (PetRules.Neglected); 0 after a night that did not.</summary>
    public int welfareNights;

    /// <summary>The Animal Welfare Office took the pet (PetRules.Taken): the run's failure ending EndingConditionType.PetTaken.</summary>
    public bool taken;

    /// <summary>Last night's change in sickness: +1 worse, -1 better, 0 none (said at the next Home).</summary>
    public int lastChange;

    /// <summary>True once the pet has a name.</summary>
    public bool Adopted => !string.IsNullOrWhiteSpace(name);

    /// <summary>The four needs now.</summary>
    public PetNeeds Needs => new PetNeeds(hunger, cold, boredom, sickness);

    /// <summary>Sets the four needs.</summary>
    public void SetNeeds(PetNeeds needs)
    {
        hunger = needs.Hunger;
        cold = needs.Cold;
        boredom = needs.Boredom;
        sickness = needs.Sickness;
    }
}

/// <summary>Tonight's care: the bills paid (HomeBill) and whether the clerk played with the pet with a toy.</summary>
public readonly struct PetCare
{
    /// <summary>The Food bill is paid.</summary>
    public readonly bool Food;

    /// <summary>The Heating bill is paid.</summary>
    public readonly bool Heating;

    /// <summary>The Electricity bill is paid.</summary>
    public readonly bool Electricity;

    /// <summary>The TV bill is paid.</summary>
    public readonly bool Tv;

    /// <summary>The Medicine bill is paid.</summary>
    public readonly bool Medicine;

    /// <summary>The clerk played with the pet with a toy tonight.</summary>
    public readonly bool Played;

    /// <summary>Care with these bills paid.</summary>
    public PetCare(bool food, bool heating, bool electricity, bool tv, bool medicine, bool played = false)
    {
        Food = food;
        Heating = heating;
        Electricity = electricity;
        Tv = tv;
        Medicine = medicine;
        Played = played;
    }

    /// <summary>Warm tonight: the heating paid and powered.</summary>
    public bool Warm => Heating && Electricity;

    /// <summary>The TV on tonight: paid and powered.</summary>
    public bool Watched => Tv && Electricity;

    /// <summary>Whether <paramref name="bill"/> is paid.</summary>
    public bool Pays(HomeBill bill) => bill switch
    {
        HomeBill.Food => Food,
        HomeBill.Heating => Heating,
        HomeBill.Electricity => Electricity,
        HomeBill.Tv => Tv,
        _ => Medicine
    };

    /// <summary>This care with <paramref name="bill"/> paid or not.</summary>
    public PetCare With(HomeBill bill, bool paid) => new PetCare(
        bill == HomeBill.Food ? paid : Food,
        bill == HomeBill.Heating ? paid : Heating,
        bill == HomeBill.Electricity ? paid : Electricity,
        bill == HomeBill.Tv ? paid : Tv,
        bill == HomeBill.Medicine ? paid : Medicine,
        Played);

    /// <summary>This care with the toy played or not.</summary>
    public PetCare WithPlay(bool played) => new PetCare(Food, Heating, Electricity, Tv, Medicine, played);

    /// <summary>
    /// The bills panel's toggle of <paramref name="bill"/>: electricity
    /// powers the heating and the TV, so paying either pays the electricity
    /// too, and dropping the electricity drops them.
    /// </summary>
    public PetCare Toggle(HomeBill bill)
    {
        bool on = !Pays(bill);
        PetCare care = With(bill, on);
        if (on && (bill == HomeBill.Heating || bill == HomeBill.Tv))
            care = care.With(HomeBill.Electricity, true);
        if (!on && bill == HomeBill.Electricity)
            care = care.With(HomeBill.Heating, false).With(HomeBill.Tv, false);
        return care;
    }
}

/// <summary>
/// The pet's rules (the Home pet spec PS3-PS6), pure so they are tested
/// headless; HomeEconomy and DayCycle apply them to the run's pet, wallet and
/// config. Tonight's care settles the needs (a need met falls to well, one
/// not met rises a step), then the night's sickness roll (the household's
/// stream, HomeRules.Worsens) and recovery roll (HomeRules.Recovers) move
/// its health; a pet left at the worst of hunger, cold or sickness for
/// GameConfigSO.welfareNights nights in a row is taken by the Animal Welfare
/// Office. The player never sees a level as a number (PetContent.Need).
/// </summary>
public static class PetRules
{
    /// <summary>
    /// The needs after tonight's care: hunger to 0 when fed, else a step up;
    /// cold to 0 when warm (Heating with Electricity), else a step up; boredom
    /// a step down for the TV (with Electricity) and a step down for a toy
    /// played with, else a step up; sickness a step down with Medicine. Each
    /// between 0 and <paramref name="max"/>.
    /// </summary>
    public static PetNeeds Settle(PetNeeds needs, PetCare care, int max)
    {
        int cap = Math.Max(0, max);
        int company = (care.Watched ? 1 : 0) + (care.Played ? 1 : 0);
        return new PetNeeds(
            care.Food ? 0 : Clamp(needs.Hunger + 1, cap),
            care.Warm ? 0 : Clamp(needs.Cold + 1, cap),
            company > 0 ? Clamp(needs.Boredom - company, cap) : Clamp(needs.Boredom + 1, cap),
            care.Medicine ? HomeRules.Treated(Clamp(needs.Sickness, cap)) : Clamp(needs.Sickness, cap));
    }

    /// <summary>
    /// Tonight's chance the pet gets a step sicker: <paramref name="chance"/>
    /// (GameConfigSO.conditionWorsenChance) plus <paramref name="perNeed"/> for
    /// every step of hunger and cold after tonight's care, with the house's
    /// SicknessChance ops (<paramref name="sicknessBonus"/>) and less the
    /// mood's share (HomeRules.WorsenChance), the mood being the house's and
    /// the toys' Mood ops less the pet's boredom; never below 0.
    /// </summary>
    public static float SicknessChance(float chance, PetNeeds settled, float perNeed, float sicknessBonus, float mood, float perMood, float cap) =>
        HomeRules.WorsenChance(chance + Math.Max(0f, perNeed) * (settled.Hunger + settled.Cold), sicknessBonus, Mood(mood, settled), perMood, cap);

    /// <summary>Tonight's chance a sick pet gets a step better on its own: the mood's share (HomeRules.MoodShare of the house's and toys' Mood ops less the pet's boredom).</summary>
    public static float RecoveryChance(PetNeeds settled, float mood, float perMood, float cap) =>
        HomeRules.MoodShare(Mood(mood, settled), perMood, cap);

    /// <summary>The pet's mood for the night's rolls: the Mood ops in force less its boredom after tonight's care.</summary>
    public static float Mood(float mood, PetNeeds settled) => mood - settled.Boredom;

    /// <summary>
    /// The night's health after the rolls: medicine tonight means no worse
    /// tonight; else a step sicker when <paramref name="worsens"/> (up to
    /// <paramref name="max"/>), else a step better when it is sick and
    /// <paramref name="recovers"/> (HomeRules.Night).
    /// </summary>
    public static int Sickness(int settled, bool medicine, bool worsens, bool recovers, int max) =>
        HomeRules.Night(settled, worsens && !medicine, recovers, Math.Max(0, max));

    /// <summary>True when the night ends with hunger, cold or sickness at <paramref name="max"/> (boredom alone is no neglect).</summary>
    public static bool Neglected(PetNeeds needs, int max) =>
        needs.Hunger >= max || needs.Cold >= max || needs.Sickness >= max;

    /// <summary>The nights in a row at the worst after tonight: one more when <paramref name="neglected"/>, else none.</summary>
    public static int WelfareNights(int before, bool neglected) => neglected ? Math.Max(0, before) + 1 : 0;

    /// <summary>The Welfare Office takes the pet once it has been left at the worst for <paramref name="nightsToTake"/> nights in a row (0: never).</summary>
    public static bool Taken(int welfareNights, int nightsToTake) => nightsToTake > 0 && welfareNights >= nightsToTake;

    /// <summary>How it looks: sick when at all unwell; else sad when hunger, cold or boredom is two steps or more; else happy when every need is met; else idle.</summary>
    public static PetLook Look(PetNeeds needs)
    {
        if (needs.Sickness > 0)
            return PetLook.Sick;
        if (needs.Hunger >= 2 || needs.Cold >= 2 || needs.Boredom >= 2)
            return PetLook.Sad;
        return needs.Hunger == 0 && needs.Cold == 0 && needs.Boredom == 0 ? PetLook.Happy : PetLook.Idle;
    }

    /// <summary>The bills panel's starting choice: food, heating and electricity, and medicine when the pet is unwell; never the TV.</summary>
    public static PetCare DefaultCare(PetNeeds needs) => new PetCare(true, true, true, false, needs.Sickness > 0);

    /// <summary>What tonight's bills cost: the price of each paid bill (<paramref name="price"/>, by bill).</summary>
    public static int Total(PetCare care, Func<HomeBill, int> price)
    {
        int total = 0;
        foreach (HomeBill bill in (HomeBill[])Enum.GetValues(typeof(HomeBill)))
            if (care.Pays(bill))
                total += Math.Max(0, price != null ? price(bill) : 0);
        return total;
    }

    /// <summary>
    /// Which of <paramref name="count"/> words a level is (0 the best word):
    /// the range 0 to <paramref name="max"/> spread over the words, any step
    /// above 0 at least the second word, so "hungry" is never said "fed".
    /// </summary>
    public static int Band(int level, int max, int count)
    {
        if (count <= 1)
            return 0;
        int cap = Math.Max(1, max);
        int clamped = Math.Max(0, Math.Min(cap, level));
        return Math.Min(count - 1, (clamped * (count - 1) + cap - 1) / cap);
    }

    /// <summary>A level between 0 and <paramref name="cap"/>.</summary>
    private static int Clamp(int level, int cap) => Math.Max(0, Math.Min(cap, level));
}
