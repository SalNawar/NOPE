/// <summary>
/// The UI string keys of the texts moving to the player's language (P0-A seam;
/// owner TEXT-LANG; HOME, UI-PC and FEEDBACK consume them): each constant is
/// the UiStringTableSO key the code passes to UiText today, so a key
/// resolves to today's string. TEXT-LANG adds the rest of the surfaces.
/// </summary>
public static class UiKeys
{
    /// <summary>The verdict line for a correct decision (OfficeUIController.ShowVerdict).</summary>
    public const string VerdictCorrect = "verdict.correct";

    /// <summary>The verdict line for a wrong decision (OfficeUIController.ShowVerdict).</summary>
    public const string VerdictWrong = "verdict.wrong";

    /// <summary>The verdict line for a wrong decision with its penalty (OfficeUIController.ShowVerdict).</summary>
    public const string VerdictWrongPenalty = "verdict.wrongPenalty";

    /// <summary>The Home bills panel's title (HomeManager).</summary>
    public const string HomeTitle = "home.title";

    /// <summary>The Home bills panel's Pay button (HomeManager).</summary>
    public const string HomePay = "home.pay";
}
