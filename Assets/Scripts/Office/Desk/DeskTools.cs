using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The desk's tools' one switch (Saleh's playtest of 2026-10-28, point 11:
/// "flashlight and other machiences should only be usable on overhead
/// camera"): the UV torch, the scanner and the chip reader (a paper let go on
/// them, the Auto-Feed tray, the Analysis beacon, the reader's readout) and
/// the BABEL rack respond only while it is on. BoothCoordinator sets it from BoothRules' ToolsLive (the reading view,
/// the props live); off, each tool simply does not respond, as Papers,
/// Please's desk items do in the booth: no pick-up, no drag, no click, no
/// hover rim (each registered click box's Clickable goes inert), no slide
/// toward the cursor (Responds: DeskSlideIn), and a paper let go on a
/// machine lies on the desk beside it (DeskController.DockAt).
/// On by default: with no booth coordinator (a test, an older scene) every
/// tool works as before. The intercom (the wheel, from both views: Saleh's
/// D5), DETAIN and the stamps are not tools here. Nor are the Rolodex and the
/// protocol cards (integrate/s0800): they are no machines, and the painted
/// desk's reading view does not frame them (its frame runs x -1.09 to 1.11 m,
/// y -0.56 to 0.68 m at 16:9; the Rolodex lies at x -1.46 to -1.22 and the
/// cards at y 0.62 to 0.87 on the counter row), so in the reading view only
/// the player could never use them.
/// </summary>
public static class DeskTools
{
    /// <summary>The tools' homes (DeskHomeMap's ids): the objects there are tools (OfficeSceneBinder.RegisterTools) and their slide-in answers the cursor in the reading view only (Responds).</summary>
    public static readonly string[] Homes =
    {
        DeskHomeMap.Scanner, DeskHomeMap.ChipReader, DeskHomeMap.UvTorch, DeskHomeMap.BabelTuner,
    };

    /// <summary>True when the slider <paramref name="id"/> answers the cursor now (DeskSlideIn): a tool's in the reading view only (in the office view it does not slide toward the cursor, as it does not respond); every other one (the intercom, the glasses, the book, the daters) always.</summary>
    public static bool Responds(string id) => Live || Array.IndexOf(Homes, id) < 0;

    /// <summary>The tools' click boxes (Register): their Clickables follow the switch.</summary>
    private static readonly List<Clickable> Clicks = new List<Clickable>();

    /// <summary>True while the tools respond.</summary>
    public static bool Live { get; private set; } = true;

    /// <summary>Raised when the switch changes.</summary>
    public static event Action Changed;

    /// <summary>Forgets the switch and the click boxes when the game starts without a domain reload.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        Live = true;
        Clicks.Clear();
        Changed = null;
    }

    /// <summary>Sets the switch (BoothCoordinator, each time the booth's rules apply): every registered click box follows it (after the props' own rule, so a tool among the props is still dead in the office view).</summary>
    public static void Set(bool live)
    {
        bool changed = live != Live;
        Live = live;
        for (int i = Clicks.Count - 1; i >= 0; i--)
        {
            if (Clicks[i] == null)
                Clicks.RemoveAt(i);
            else
                Clicks[i].Interactable = live;
        }
        if (changed)
            Changed?.Invoke();
    }

    /// <summary>A tool's click box follows the switch from now on (taken at once); null is ignored.</summary>
    public static void Register(Clickable click)
    {
        if (click == null || Clicks.Contains(click))
            return;
        Clicks.Add(click);
        click.Interactable = Live;
    }

    /// <summary>Every Clickable under <paramref name="tool"/> follows the switch (Register).</summary>
    public static void RegisterAll(Component tool)
    {
        if (tool == null)
            return;
        foreach (Clickable click in tool.GetComponentsInChildren<Clickable>(true))
            Register(click);
    }
}
