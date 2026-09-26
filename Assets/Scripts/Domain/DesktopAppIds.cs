using System.Collections.Generic;

/// <summary>
/// The PC's six apps by id (the PC redesign DK1): the desktop holds exactly
/// one icon per app, in <see cref="DefaultOrder"/>, and nothing else, ever.
/// These ids are the one app contract: an icon, a Start menu entry and any
/// later link open an app through DesktopApps.OpenApp(id), and each app's
/// builder registers its window under its id. Pure.
/// </summary>
public static class DesktopAppIds
{
    /// <summary>The Investigation app (phase 16): every case source in one window.</summary>
    public const string Investigation = "investigation";

    /// <summary>The Internet browser.</summary>
    public const string Internet = "internet";

    /// <summary>Mail.</summary>
    public const string Mail = "mail";

    /// <summary>The clerk's own Citizen Account.</summary>
    public const string CitizenAccount = "citizen_account";

    /// <summary>Notes.</summary>
    public const string Notes = "notes";

    /// <summary>Settings.</summary>
    public const string Settings = "settings";

    /// <summary>The six ids in the desktop's default order (Arrange lays them out in it; the Start menu lists them in it).</summary>
    public static readonly IReadOnlyList<string> DefaultOrder = new[] { Investigation, Internet, Mail, CitizenAccount, Notes, Settings };
}
