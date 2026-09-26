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
