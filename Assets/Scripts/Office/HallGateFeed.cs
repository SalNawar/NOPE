using System.Reflection;

/// <summary>
/// The switchboard as the hall's rings see it, read only (the night of
/// 2026-11-07, DECISIONS D6: the hall portals speak the board's Stargate
/// language): the running office's board (SwitchboardView.Current) and its
/// rules' modules, each ring dialed by the module of its number
/// (PortalGateFeed.ModuleOf). SwitchboardView keeps its Switchboard private
/// and is another task's (stargate-switchboard), so the board is read through
/// its field of type Switchboard, found once by that type (a renamed field
/// still reads), and polled each frame: nothing is written to the board.
/// When the board publishes its routes' events, AnimeHallPortalLink can
/// take them instead.
/// </summary>
public static class HallGateFeed
{
    private static FieldInfo _field;
    private static bool _looked;

    /// <summary>True when the office has a working switchboard: the rings follow its modules, dark until its day begins.</summary>
    public static bool Present
    {
        get
        {
            SwitchboardView view = SwitchboardView.Current;
            return view != null && view.enabled && Field != null;
        }
    }

    /// <summary>The running board's rules (null: no switchboard, or its day has not begun).</summary>
    public static Switchboard Board
    {
        get
        {
            SwitchboardView view = SwitchboardView.Current;
            return view != null && Field != null ? Field.GetValue(view) as Switchboard : null;
        }
    }

    /// <summary>The module that dials ring <paramref name="portal"/> on <paramref name="board"/> (null: none).</summary>
    public static SwitchModule Module(Switchboard board, int portal)
    {
        int m = PortalGateFeed.ModuleOf(portal);
        return board != null && m >= 0 && m < board.Modules.Count ? board.Modules[m] : null;
    }

    /// <summary>SwitchboardView's one field of type Switchboard, found once.</summary>
    private static FieldInfo Field
    {
        get
        {
            if (_looked)
                return _field;
            _looked = true;
            foreach (FieldInfo f in typeof(SwitchboardView).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                if (f.FieldType == typeof(Switchboard))
                {
                    _field = f;
                    break;
                }
            if (_field == null)
                UnityEngine.Debug.LogWarning("[HallGateFeed] SwitchboardView holds no Switchboard field: the hall's rings follow the day's look, not the board.");
            return _field;
        }
    }
}
