using System;
using System.Collections.Generic;

/// <summary>
/// One install slot's record (WorldState.installs; Saleh 2026-09-29: "you can
/// only have one type of upgraded scanner installed at a time"): the upgrade
/// installed in the slot, and the one the clerk chose to swap in at the next
/// day's start ("" when none). Additive: an older save loads none, and the
/// slot's last owned upgrade counts as installed (Installs.InstalledIn).
/// </summary>
[Serializable]
public sealed class InstallEntry
{
    /// <summary>The slot (UpgradeSO.installSlot, "scanner").</summary>
    public string slot = string.Empty;

    /// <summary>The upgrade installed in it (in force today).</summary>
    public string installed = string.Empty;

    /// <summary>The upgrade that goes in at the next day's start, or "".</summary>
    public string next = string.Empty;
}

/// <summary>An owned upgrade's place in its install slot, as the Orders app shows it (never serialized).</summary>
public enum InstallState
{
    /// <summary>Not owned, or it has no slot: always in force once owned.</summary>
    None,

    /// <summary>Installed, and staying in tomorrow.</summary>
    Installed,

    /// <summary>Owned, in storage: the Install button.</summary>
    Stored,

    /// <summary>Chosen: it goes in at the next day's start (Cancel keeps the installed one).</summary>
    InstallsTomorrow,

    /// <summary>Installed today, swapped out at the next day's start (Keep cancels the swap).</summary>
    LeavesTomorrow
}

/// <summary>
/// One upgrade per install slot at a time (Saleh 2026-09-29): upgrades sharing
/// a slot (UpgradeSO.installSlot: the Auto-Feed and the Analysis Scanner
/// share "scanner") may all be owned, but only the slot's installed one is in
/// force (<see cref="InForce"/>: what ScannerDay reads at the day's start,
/// so the desk shows only its parts). A delivered upgrade arrives installed
/// (<see cref="Arrive"/>); the clerk chooses another owned one in Orders
/// (<see cref="Choose"/>) and it goes in at the next day's start
/// (<see cref="Turn"/>, DayCycle.AdvanceNight), like a delivery. Pure.
/// </summary>
public static class Installs
{
    /// <summary>
    /// The upgrade installed in <paramref name="slot"/>: its record's while
    /// owned, else the last of <paramref name="ownedInOrder"/> (unlock order)
    /// in the slot (a save from before installs, or an unlock by an effect or
    /// the debug panel); "" when the slot holds nothing owned.
    /// </summary>
    public static string InstalledIn(IReadOnlyList<InstallEntry> installs, string slot, IReadOnlyList<string> ownedInOrder, Func<string, string> slotOf)
    {
        if (string.IsNullOrEmpty(slot))
            return string.Empty;

        InstallEntry entry = Find(installs, slot);
        string last = string.Empty;
        foreach (string id in ownedInOrder ?? Array.Empty<string>())
        {
            if (string.IsNullOrEmpty(id) || SlotOf(slotOf, id) != slot)
                continue;
            if (entry != null && entry.installed == id)
                return id;
            last = id;
        }
        return last;
    }

    /// <summary>The upgrade chosen to go into <paramref name="slot"/> at the next day's start, or "".</summary>
    public static string NextIn(IReadOnlyList<InstallEntry> installs, string slot)
    {
        InstallEntry entry = Find(installs, slot);
        return entry != null ? entry.next ?? string.Empty : string.Empty;
    }

    /// <summary>The owned upgrades in force, in <paramref name="ownedInOrder"/>'s order: each without a slot, and each slot's installed one.</summary>
    public static List<string> InForce(IReadOnlyList<string> ownedInOrder, Func<string, string> slotOf, IReadOnlyList<InstallEntry> installs)
    {
        var inForce = new List<string>();
        foreach (string id in ownedInOrder ?? Array.Empty<string>())
        {
            string slot = SlotOf(slotOf, id);
            if (slot.Length == 0 || id == InstalledIn(installs, slot, ownedInOrder, slotOf))
                inForce.Add(id);
        }
        return inForce;
    }

    /// <summary>Where <paramref name="upgradeId"/> stands in <paramref name="slot"/>: None unless owned and slotted, else installed (staying or leaving tomorrow) or stored (going in tomorrow or not).</summary>
    public static InstallState StateOf(string upgradeId, string slot, IReadOnlyList<InstallEntry> installs, IReadOnlyList<string> ownedInOrder, Func<string, string> slotOf)
    {
        if (string.IsNullOrEmpty(slot) || !Owns(ownedInOrder, upgradeId))
            return InstallState.None;

        string next = NextIn(installs, slot);
        if (upgradeId == InstalledIn(installs, slot, ownedInOrder, slotOf))
            return next.Length > 0 && next != upgradeId ? InstallState.LeavesTomorrow : InstallState.Installed;
        return next == upgradeId ? InstallState.InstallsTomorrow : InstallState.Stored;
    }

    /// <summary>
    /// The clerk chooses <paramref name="upgradeId"/> for its slot: an owned
    /// one in storage goes in at the next day's start; the installed one
    /// cancels a pending swap. False (nothing changes) when it is not owned or
    /// not in <paramref name="slot"/>.
    /// </summary>
    public static bool Choose(List<InstallEntry> installs, string slot, string upgradeId, IReadOnlyList<string> ownedInOrder, Func<string, string> slotOf)
    {
        if (installs == null || string.IsNullOrEmpty(slot) || !Owns(ownedInOrder, upgradeId) || SlotOf(slotOf, upgradeId) != slot)
            return false;

        string installed = InstalledIn(installs, slot, ownedInOrder, slotOf);
        InstallEntry entry = Entry(installs, slot);
        entry.installed = installed;
        entry.next = upgradeId == installed ? string.Empty : upgradeId;
        return true;
    }

    /// <summary>A delivered upgrade of <paramref name="slot"/> arrives installed, dropping any swap (the one it replaces stays owned, in storage).</summary>
    public static void Arrive(List<InstallEntry> installs, string slot, string upgradeId)
    {
        if (installs == null || string.IsNullOrEmpty(slot) || string.IsNullOrEmpty(upgradeId))
            return;

        InstallEntry entry = Entry(installs, slot);
        entry.installed = upgradeId;
        entry.next = string.Empty;
    }

    /// <summary>The night's turn: each slot's chosen upgrade goes in.</summary>
    public static void Turn(List<InstallEntry> installs)
    {
        foreach (InstallEntry entry in installs ?? new List<InstallEntry>())
            if (entry != null && !string.IsNullOrEmpty(entry.next))
            {
                entry.installed = entry.next;
                entry.next = string.Empty;
            }
    }

    /// <summary>The slot's record, or null.</summary>
    private static InstallEntry Find(IReadOnlyList<InstallEntry> installs, string slot)
    {
        foreach (InstallEntry entry in installs ?? Array.Empty<InstallEntry>())
            if (entry != null && entry.slot == slot)
                return entry;
        return null;
    }

    /// <summary>The slot's record, made when missing.</summary>
    private static InstallEntry Entry(List<InstallEntry> installs, string slot)
    {
        InstallEntry entry = Find(installs, slot);
        if (entry == null)
        {
            entry = new InstallEntry { slot = slot };
            installs.Add(entry);
        }
        return entry;
    }

    /// <summary>An upgrade's slot ("" for none or without a lookup).</summary>
    private static string SlotOf(Func<string, string> slotOf, string upgradeId) => slotOf?.Invoke(upgradeId) ?? string.Empty;

    /// <summary>True when <paramref name="upgradeId"/> is owned.</summary>
    private static bool Owns(IReadOnlyList<string> ownedInOrder, string upgradeId)
    {
        if (string.IsNullOrEmpty(upgradeId))
            return false;
        foreach (string id in ownedInOrder ?? Array.Empty<string>())
            if (id == upgradeId)
                return true;
        return false;
    }
}
