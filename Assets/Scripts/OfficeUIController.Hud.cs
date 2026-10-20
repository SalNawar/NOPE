using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office HUD (OfficeUIController's HUD half): the day, the date on the
/// tray and the CRT, and the credits. The verdict line and the citation slip
/// stay in OfficeUIController.cs.
/// Carved out of OfficeUIController.cs by P0-A (pure move). Owner: UI-PC.
/// </summary>
public sealed partial class OfficeUIController
{
    /// <summary>
    /// Refreshes the money/day HUD from world state (stability shows as the Helix River, HelixRiverMonitor, never a number).
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
        if (crtDateText != null)
            crtDateText.text = CrtDate(world.day);
    }

    /// <summary>The desk CRT's date for <paramref name="day"/>: today in the agency's calendar, in capitals ("14 MAR 2150"), or the plain day when the library has no readable first date.</summary>
    private static string CrtDate(int day) => Today(day) ?? UiText.Pc.Format("tray.day", day);

    /// <summary>Today (shift day <paramref name="day"/>) in the agency's calendar, in capitals ("14 MAR 2150"), or null when the library has no readable first date.</summary>
    private static string Today(int day)
    {
        ContentLibrarySO library = RunManager.HasInstance ? RunManager.Instance.Library : null;
        string today = library != null ? AgencyCalendar.Today(library.Agency.firstDate, day) : null;
        return today?.ToUpperInvariant();
    }

    /// <summary>The taskbar's date for <paramref name="day"/>: today in the agency's calendar, in capitals, and the day ("14 MAR 2150 · Day 1"), or the plain day when the library has no readable first date.</summary>
    private static string TrayDate(int day)
    {
        string today = Today(day);
        return today != null ? UiText.Pc.Format("tray.date", today, day) : UiText.Pc.Format("tray.day", day);
    }
}
