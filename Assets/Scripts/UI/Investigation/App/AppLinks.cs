/// <summary>
/// The engine side of the Investigation app's smart links (the PC redesign
/// LK1, LK2): a case's claim as SmartLinks reads it, and the ↗'s hover hint
/// saying where a link goes ("Open the Reference: Currency, the claimed
/// place"; "Open Records: 552-1804-33"; "Open the Rules"). The rows' links
/// themselves are Domain SmartLinks'.
/// </summary>
public static class AppLinks
{
    /// <summary>The case's claimed place (none between cases).</summary>
    public static CaseClaim Claim(CaseInstance inst) =>
        new CaseClaim(inst != null && inst.claimedNation != null ? inst.claimedNation.id : null,
                      inst != null && inst.claimedEra != null ? inst.claimedEra.id : null);

    /// <summary>The hover hint of a ↗ to <paramref name="link"/> from a statement of <paramref name="category"/> (null for none).</summary>
    public static string Hint(LinkTarget link, ClueCategory category)
    {
        if (link.IsNone)
            return null;
        switch (link.Tab)
        {
            case AppTab.Reference:
                return UiText.Format("app.link.reference", UiText.Category(category));
            case AppTab.Records:
                return UiText.Format("app.link.records", link.Query);
            case AppTab.Rules:
                return UiText.Get("app.link.rules");
            default:
                return null;
        }
    }
}
