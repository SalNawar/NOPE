/// <summary>
/// The switchboard as the hall's rings see it, read only (the night of
/// 2026-11-07, DECISIONS D6: the hall portals speak the board's Stargate
/// language): the running office's board (SwitchboardView.Current and its
/// Board) and its rules' modules, each ring dialed by the module of its
/// number (PortalGateFeed.ModuleOf), polled each frame: nothing is written
/// to the board. When the board publishes its routes' events,
/// AnimeHallPortalLink can take them instead.
/// </summary>
public static class HallGateFeed
{
    /// <summary>True when the office has a working switchboard: the rings follow its modules, dark until its day begins.</summary>
    public static bool Present
    {
        get
        {
            SwitchboardView view = SwitchboardView.Current;
            return view != null && view.enabled;
        }
    }

    /// <summary>The running board's rules (null: no switchboard, or its day has not begun).</summary>
    public static Switchboard Board => SwitchboardView.Current != null ? SwitchboardView.Current.Board : null;

    /// <summary>The module that dials ring <paramref name="portal"/> on <paramref name="board"/> (null: none).</summary>
    public static SwitchModule Module(Switchboard board, int portal)
    {
        int m = PortalGateFeed.ModuleOf(portal);
        return board != null && m >= 0 && m < board.Modules.Count ? board.Modules[m] : null;
    }
}
