using System;
using System.Collections.Generic;

/// <summary>How a RING SERVICE module's pilot lamp shows its ring today (the desk machines spec §10: the panel is the desk's own departure board).</summary>
public enum RingLamp
{
    /// <summary>Dark: the ring is under maintenance, or the day has no such portal.</summary>
    Dark,

    /// <summary>Green: a departure ring on an open route.</summary>
    Open,

    /// <summary>Red: a departure ring whose route is CLOSED today.</summary>
    Closed,

    /// <summary>Amber: a departure ring in service with no route today.</summary>
    NoRoute,

    /// <summary>Blue: the Return Gate in service.</summary>
    ReturnGate
}

/// <summary>
/// What the upgrades' desk objects show (the desk machines spec §4; the
/// objects themselves are DeskUpgrades'): the BABEL rack's bays (a cartridge
/// per owned translator pack, in the packs' order; one delivered this
/// morning waits for the supply crate) and the RING SERVICE panel's modules
/// (a portal per module by its number: its lamp lit while in service, its
/// sticker once its repair is owned). Pure: tested in UpgradeObjectsTests.
/// </summary>
public static class UpgradeObjects
{
    /// <summary>The modules on the RING SERVICE panel (portals 1 to 5).</summary>
    public const int RingModules = 5;

    /// <summary>One bay of the BABEL rack: its cartridge is seated (owned) and whether it came this morning.</summary>
    public readonly struct Bay
    {
        public readonly bool Owned, NewToday;

        public Bay(bool owned, bool newToday)
        {
            Owned = owned;
            NewToday = owned && newToday;
        }
    }

    /// <summary>One module of the RING SERVICE panel: its portal in service today, its repair owned (the SERVICED sticker), and how its pilot lamp shows the ring's route today.</summary>
    public readonly struct Module
    {
        public readonly bool InService, Serviced, NewToday;
        public readonly RingLamp Lamp;

        public Module(bool inService, bool serviced, bool newToday, RingLamp lamp = RingLamp.Dark)
        {
            InService = inService;
            Serviced = serviced;
            NewToday = serviced && newToday;
            Lamp = inService ? lamp : RingLamp.Dark;
        }
    }

    /// <summary>How a module's lamp shows <paramref name="p"/> today: dark under maintenance, the Return Gate's blue, a departure ring green on an open route, red on a CLOSED one, amber with none.</summary>
    public static RingLamp LampOf(PortalRoute p) =>
        p.State != PortalState.InService ? RingLamp.Dark
        : p.Role == PortalRole.Returns ? RingLamp.ReturnGate
        : !p.Place.HasValue ? RingLamp.NoRoute
        : p.Closed ? RingLamp.Closed
        : RingLamp.Open;

    /// <summary>The rack's bays for <paramref name="packIds"/> (translation.packs' order): each owned when its Speech translator is (Translation.UpgradeId), new when it was delivered today (<paramref name="today"/>).</summary>
    public static Bay[] Bays(IReadOnlyList<string> packIds, Func<string, bool> owns, ICollection<string> today)
    {
        int n = packIds != null ? packIds.Count : 0;
        var bays = new Bay[n];
        for (int i = 0; i < n; i++)
        {
            string id = Translation.UpgradeId(packIds[i]);
            bays[i] = new Bay(owns != null && owns(id), today != null && today.Contains(id));
        }
        return bays;
    }

    /// <summary>True when every owned bay is new today: the rack itself came this morning (it waits for the crate whole).</summary>
    public static bool RackNew(IReadOnlyList<Bay> bays)
    {
        bool any = false;
        foreach (Bay b in bays ?? Array.Empty<Bay>())
        {
            if (b.Owned && !b.NewToday)
                return false;
            any |= b.Owned;
        }
        return any;
    }

    /// <summary>The panel's modules for the day's <paramref name="portals"/>: module i shows portal i + 1 (a portal the day lacks: dark, no sticker), its lamp the ring's route today (LampOf).</summary>
    public static Module[] Modules(IReadOnlyList<PortalRoute> portals, Func<string, bool> owns, ICollection<string> today)
    {
        var modules = new Module[RingModules];
        foreach (PortalRoute p in portals ?? Array.Empty<PortalRoute>())
        {
            int i = p.Number - 1;
            if (i < 0 || i >= RingModules)
                continue;
            bool repair = !string.IsNullOrEmpty(p.Repair);
            modules[i] = new Module(p.State == PortalState.InService, repair && owns != null && owns(p.Repair), repair && today != null && today.Contains(p.Repair), LampOf(p));
        }
        return modules;
    }

    /// <summary>True when the panel is on the desk: any ring's repair is owned.</summary>
    public static bool PanelShown(IReadOnlyList<Module> modules)
    {
        foreach (Module m in modules ?? Array.Empty<Module>())
            if (m.Serviced)
                return true;
        return false;
    }

    /// <summary>True when the panel came this morning: every serviced module's repair was delivered today.</summary>
    public static bool PanelNew(IReadOnlyList<Module> modules)
    {
        bool any = false;
        foreach (Module m in modules ?? Array.Empty<Module>())
        {
            if (m.Serviced && !m.NewToday)
                return false;
            any |= m.Serviced;
        }
        return any;
    }
}
