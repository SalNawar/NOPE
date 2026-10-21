/// <summary>
/// The switchboard's printed words (world_source.json ui.strings, English in
/// every culture: tier Full): the day-1 hint card, the modules' little
/// displays and the departure's tally. The view prints a key's words only
/// once Generate World has put them in the tables (a missing key prints
/// nothing, never the key).
/// </summary>
public static class SwitchboardWords
{
    /// <summary>The day-1 hint card pinned to the board.</summary>
    public const string Hint = "switchboard.hint";

    /// <summary>A module's display when its cords are in and MASTER waits.</summary>
    public const string Pull = "switchboard.display.pull";

    /// <summary>A module's display when its route is locked in but no power cord is in it.</summary>
    public const string Power = "switchboard.display.power";

    /// <summary>A module's display when its route is open and nobody has left yet.</summary>
    public const string Open = "switchboard.display.open";

    /// <summary>A module's display after its group left (the connection stays until it is cleared).</summary>
    public const string Done = "switchboard.display.done";

    /// <summary>A module's display while its fuse is popped.</summary>
    public const string Fuse = "switchboard.display.fuse";

    /// <summary>A module's display while it is not in service today.</summary>
    public const string Closed = "switchboard.display.closed";

    /// <summary>The tally rising from a module as its group leaves ({0}: how many).</summary>
    public const string Sent = "switchboard.sent";

    /// <summary>The group bonus rising after it ({0}: the amount, {1}: the wallet's short word).</summary>
    public const string Bonus = "switchboard.bonus";

    /// <summary>Every key above (the tests check each is in ui.strings).</summary>
    public static readonly string[] All = { Hint, Pull, Power, Open, Done, Fuse, Closed, Sent, Bonus };
}
