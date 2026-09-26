/// <summary>
/// What a document field, an answer or a record row states: it decides what
/// the value can be compared with (a reference book, the traveller's record,
/// another paper) and how a report names it (ClueLabels). Order is serialized
/// (templates, books, places, effects, saves) — append only, never reorder
/// (SerializedEnumsTests pins every value).
/// </summary>
public enum ClueCategory
{
    Language,
    Material,
    Politics,
    Technology,
    Currency,
    Geography,

    /// <summary>A place's signature dress: the Costume Guide rows and worn garments (derived from the place's wardrobe).</summary>
    Culture,

    // Identity fields — validated against the agency's citizen records,
    // not the era reference books.
    Name,
    BirthDate,

    // The agency's numbers and the travel facts of the TC forms (traveller
    // types, F4). Invariant: an honest traveller has one value per compared
    // category, the same on every form, answer and record row.

    /// <summary>The agency's number for the person: a Displacement No. ("DP-4471-02") for the displaced, a Citizen ID ("418-0937-52") for a 2150 citizen.</summary>
    CitizenId,

    /// <summary>Where the traveller is sent: the claimed place's label (a displaced person's origin).</summary>
    Destination,

    /// <summary>The rift that displaced a person ("R-0311-07"): on their certificate, their return order and their registry entry.</summary>
    Incident,

    /// <summary>The date a departure is booked for. Directive-only: read against the desk calendar, never compared, never a proof.</summary>
    DepartureDate,

    /// <summary>The date a paper stops being valid. Directive-only: read against the desk calendar, never compared, never a proof.</summary>
    Expiry,

    // A 2150 citizen's account (traveller types F4, phase 6): compared with
    // the Citizen Account and with each other's papers.

    /// <summary>A citizen's account status as a visa or proof of means prints it ("Premium", "Standard", "Eligible").</summary>
    AccountStatus,

    /// <summary>The transponder a citizen travels on: its model and serial ("Hopper Mk II · HP-40718").</summary>
    TransponderId,

    /// <summary>The transponder's class ("Premium" or "Economy").</summary>
    TransponderClass,

    /// <summary>What a citizen owes, in credits ("0 cr", "212,000 cr").</summary>
    Debt
}
