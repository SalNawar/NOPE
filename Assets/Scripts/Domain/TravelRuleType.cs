/// <summary>
/// What a travel rule forbids: a closure (the first three) or a standing
/// procedure (traveller types P3). Serialized in the rule assets
/// (world_source.json rules[].type): append only (SerializedEnumsTests pins
/// every value). In Domain so the Directives' rules over the types are
/// decided and tested headless.
/// </summary>
public enum TravelRuleType
{
    /// <summary>No travel to a specific era today.</summary>
    EraForbidden,

    /// <summary>No travel to a specific nation today.</summary>
    NationForbidden,

    /// <summary>No travel to a specific nation+era combination today.</summary>
    NationEraForbidden,

    /// <summary>
    /// A standing procedure (traveller types P3, P5): a traveller must be
    /// dressed for their destination, or they would cause a panic there. It
    /// closes no destination; a 2150 citizen's costume error breaks it, a
    /// deviation fault proven against the Costume Guide (CostumeErrors).
    /// </summary>
    DressForDestination,

    /// <summary>
    /// A procedure line with no predicate (traveller types §5.3): what the
    /// desk checks for a kind ("Leisure departures: a Leisure Visa and a
    /// Departure Manifest. Every paper must match the Citizen Account."). It
    /// closes no destination and plans no violator; the liars break it (L1,
    /// L2: record lies proven against the account, RecordLies).
    /// </summary>
    Procedure,

    /// <summary>
    /// The displaced return home (traveller types §5.3): "The displaced return
    /// only to their own time: their declaration must match their origin."
    /// It closes no destination; a false origin (L7) or a fake displaced
    /// person (L8) breaks it, a deviation fault proven against the books and
    /// the registry. On its first day one such liar is guaranteed in the
    /// first half of the queue (Directives.Guarantees, P4).
    /// </summary>
    ReturnHome
}
