/// <summary>One of today's places as the lie rules see it: its ids and birth years.</summary>
public readonly struct HomeCandidate
{
    /// <summary>Nation id of the place (matches NationSO.id).</summary>
    public readonly string NationId;

    /// <summary>Era id of the place (matches EraSO.id).</summary>
    public readonly string EraId;

    /// <summary>Earliest birth year of a traveller from here (negative = BCE; 0..0 = none authored).</summary>
    public readonly int BirthYearMin;

    /// <summary>Latest birth year of a traveller from here (negative = BCE).</summary>
    public readonly int BirthYearMax;

    /// <summary>Creates a candidate.</summary>
    public HomeCandidate(string nationId, string eraId, int birthYearMin, int birthYearMax)
    {
        NationId = nationId;
        EraId = eraId;
        BirthYearMin = birthYearMin;
        BirthYearMax = birthYearMax;
    }
}
