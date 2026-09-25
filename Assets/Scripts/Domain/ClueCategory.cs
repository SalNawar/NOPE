/// <summary>
/// Category used to route clues into the “right kind” of document and to match
/// document fields against their reference book. Order is serialized — append
/// only, never reorder.
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
    BirthDate
}
