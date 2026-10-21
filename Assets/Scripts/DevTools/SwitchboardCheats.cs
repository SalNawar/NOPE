#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEMO_CHEATS
using UnityEngine;

/// <summary>
/// The switchboard's cheat in the cheat menu's "More (other tracks)"
/// (DevCheats.Register): "Switchboard: 3 test calls" puts modules 01 to 03
/// in service and rings three calls on today's board, two on one route and
/// one on another, as if three travellers were approved
/// (SwitchboardView.AddTestCalls), for review captures of the whole loop.
/// </summary>
public static class SwitchboardCheats
{
    /// <summary>Registers the cheat once, at startup.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register()
    {
        DevCheats.Register("Switchboard: 3 test calls", run =>
        {
            SwitchboardView board = SwitchboardView.Current;
            Debug.Log("[SwitchboardCheats] " + (board != null ? board.AddTestCalls() : "No switchboard in this scene (open the office)."));
        });
    }
}
#endif
