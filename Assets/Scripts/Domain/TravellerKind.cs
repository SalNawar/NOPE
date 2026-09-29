using System;

/// <summary>
/// The four kinds of traveller of the debt dystopia (traveller types, K1):
/// each kind has one blueprint (CaseBlueprintSO.kind), its own papers and
/// its own claim line. The famous displaced are Displaced premades.
/// Serialized in CaseBlueprintSO.kind and DocumentTemplateSO.askableBy: append
/// only (SerializedEnumsTests pins every value).
/// </summary>
public enum TravellerKind
{
    /// <summary>A 2150 citizen with money, travelling for fun (a Leisure Visa and a Departure Manifest).</summary>
    RichTourist,

    /// <summary>A 2150 citizen travelling on credit, savings or insurance (four papers, an Economy transponder).</summary>
    PoorTourist,

    /// <summary>A 2150 citizen in debt, sent to work it off (a Labour Contract).</summary>
    Labourer,

    /// <summary>A person pulled out of their own time, sent home (TC-610, TC-620, TC-630).</summary>
    Displaced
}

/// <summary>
/// The kinds' rules (traveller types K1, K2, K4): who is a 2150 citizen (a
/// Citizen Account, a 2150 name, English speech), which kind a premade may be
/// drawn as, and who may carry a place lie. Pure, so each is tested headless.
/// </summary>
public static class TravellerKinds
{
    /// <summary>True for a 2150 citizen (every kind but the displaced): they come from the present and hold a Citizen Account.</summary>
    public static bool IsCitizen(TravellerKind kind) => kind != TravellerKind.Displaced;

    /// <summary>
    /// A blueprint's weight in the day's kind pick (DayPlanSO kinds, one
    /// weighted draw on the case stream): the day's weight, never below 0,
    /// and 0 for every kind but <paramref name="onlyKind"/> in a slot that is
    /// that kind's (a premade's: the premade's kind, the famous being
    /// displaced, K1, a story character a 2150 citizen, days 7-15 B2; or the
    /// planned liar's of the displaced's return home, Directives.Guarantees),
    /// so the pick stays one draw. Null: every kind at its weight.
    /// </summary>
    public static float PickWeight(TravellerKind kind, float dayWeight, TravellerKind? onlyKind) =>
        onlyKind != null && kind != onlyKind.Value ? 0f : Math.Max(0f, dayWeight);
}
