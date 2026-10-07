using System.Collections.Generic;

/// <summary>
/// The PC's apps by id (the PC redesign DK1; Saleh 2026-09-29 added Orders,
/// the seventh, overriding DK1's "six", and the portals spec v3 Portals, the
/// eighth): the desktop holds exactly one icon
/// per app, in <see cref="DefaultOrder"/>, and nothing else, ever.
/// These ids are the one app contract: an icon, a Start menu entry and any
/// later link open an app through DesktopApps.OpenApp(id), and each app's
/// builder registers its window under its id. Pure.
/// </summary>
public static class DesktopAppIds
{
    /// <summary>The Investigation app (phase 16): every case source in one window.</summary>
    public const string Investigation = "investigation";

    /// <summary>Portals (the portals spec v3 PA1): today's portal schedule, read-only.</summary>
    public const string Portals = "portals";

    /// <summary>The Internet browser.</summary>
    public const string Internet = "internet";

    /// <summary>Mail.</summary>
    public const string Mail = "mail";

    /// <summary>The clerk's own Citizen Account.</summary>
    public const string CitizenAccount = "citizen_account";

    /// <summary>Orders: the upgrade tree (Saleh 2026-09-29; the portals spec v3 OR1-OR9).</summary>
    public const string Orders = "orders";

    /// <summary>Notes.</summary>
    public const string Notes = "notes";

    /// <summary>Settings.</summary>
    public const string Settings = "settings";

    /// <summary>The eight ids in the desktop's default order (Arrange lays them out in it; the Start menu lists them in it; the orchestrator's order of 2026-10-07): the case's app, Mail and Notes first, then Portals, the Internet, the account and the Orders that spend from it, Settings last.</summary>
    public static readonly IReadOnlyList<string> DefaultOrder = new[] { Investigation, Mail, Notes, Portals, Internet, CitizenAccount, Orders, Settings };
}
