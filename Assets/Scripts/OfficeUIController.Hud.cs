using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office HUD (OfficeUIController's HUD half): the taskbar's tray clock,
/// Windows-style (BUG-20a, Saleh's note 10): the time (ShiftClockReadouts)
/// over today's date (TrayClock); the clock is a button that holds today on
/// the app's workbench, live only while the app is on the PC (no dead
/// affordance on days 1-4, BUG-21a). Credits and the Helix River left the
/// tray (BUG-21a: the river hangs on the wall monitor; the wallet is the desk
/// till's and the Citizen Account's), so a scene built without the tray's
/// credits writes none; the desk CRT's clock plate is retired (BUG-20a), so
/// its date is not written either. The verdict line and the citation slip
/// stay in OfficeUIController.cs.
/// Carved out of OfficeUIController.cs by P0-A (pure move). Owner: UI-PC.
/// </summary>
public sealed partial class OfficeUIController
{
    /// <summary>
    /// Refreshes the tray from world state (stability shows as the Helix River, HelixRiverMonitor, never a number).
    /// Safe to call with unwired HUD fields.
    /// </summary>
    public void UpdateHud(WorldState world)
    {
        if (world == null)
            return;

        if (moneyText != null)
            moneyText.text = UiText.Pc.Format("tray.money", UiText.Pc.Currency(UiText.WalletForm.Label), world.money);

        if (dayText != null)
            dayText.text = TrayDate(world.day);
        if (dateButton != null)
            dateButton.interactable = app != null && app.OnPc;
    }

    /// <summary>The tray clock's date for <paramref name="day"/>: today in the agency's calendar, in capitals ("14 MAR 2150", TrayClock), or the plain day when the library has no readable first date.</summary>
    private static string TrayDate(int day)
    {
        ContentLibrarySO library = RunManager.HasInstance ? RunManager.Instance.Library : null;
        return (library != null ? TrayClock.Date(library.Agency.firstDate, day) : null) ?? UiText.Pc.Format("tray.day", day);
    }
}
