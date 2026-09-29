using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Portals app (the portals spec v3 PA1-PA3, §4): today's portal
/// schedule as the form TC-970 (Form_PortalSchedule, a landscape page across
/// the window), read-only: the day's date, one row per portal from the day's
/// PortalDay (GameManager.Portals: number, ring, era, place and state; a
/// portal under maintenance its repair's Orders state and price, or "locked:
/// repair 02 first"; PortalText.AppRows and RepairLine), and the footer (the
/// Directorate sets the routes; until the Return Gate is repaired the
/// displaced leave through the default portal). Its rows are not search
/// entries and not pickable. Redrawn each time the window shows; setting a
/// route waits for Saleh's next portals pass (SM1).
/// </summary>
public sealed class PortalsWindow : MonoBehaviour
{
    /// <summary>The scroll the page sits in.</summary>
    [SerializeField] private ScrollRect scroll;

    /// <summary>The schedule's page.</summary>
    [SerializeField] private FormView page;

    /// <summary>The page kind (Form_PortalSchedule, TC-970, landscape).</summary>
    [SerializeField] private FormSpecSO form;

    /// <summary>The shift, whose day-start PortalDay the page prints.</summary>
    [SerializeField] private GameManager game;

    private void OnEnable() => Draw();

    /// <summary>Prints today's schedule on the page and fits the scroll to it.</summary>
    private void Draw()
    {
        if (page == null || form == null)
            return;

        WorldState world = RunManager.HasInstance ? RunManager.Instance.World : null;
        ContentLibrarySO library = RunManager.HasInstance ? RunManager.Instance.Library : null;
        PortalDay day = game != null ? game.Portals : PortalDay.None;
        int today = world != null ? world.day : 1;
        string date = library != null ? AgencyCalendar.Today(library.Agency.firstDate, today) : null;

        FormData data = form.Page(library != null ? library.Agency : null);
        data.Text = new Dictionary<string, string>
        {
            { "date", UiText.Format("portals.date", UiText.Format("desk.calendar", date ?? string.Empty, today)) },
            { "footer", UiText.Format("portals.footer", PortalText.Number(day.Default)) }
        };
        data.Rows = new Dictionary<string, IReadOnlyList<string[]>>
        {
            { "rows", PortalText.AppRows(day, p => EraName(library, p), p => PlaceName(library, p), UiText.Get, p => RepairLine(world, library, day, p)) }
        };
        page.Show(form.form, data, _ => false);

        if (scroll != null && scroll.content != null)
        {
            scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ((RectTransform)page.transform).rect.height);
            scroll.verticalNormalizedPosition = 1f;
        }
    }

    /// <summary>A route's era by its display name.</summary>
    internal static string EraName(ContentLibrarySO library, PlaceRef place)
    {
        EraSO era = library != null ? library.GetEraById(place.EraId) : null;
        return era != null ? era.displayName : place.EraId;
    }

    /// <summary>A route's place by its display name.</summary>
    internal static string PlaceName(ContentLibrarySO library, PlaceRef place)
    {
        NationEraProfileSO profile = library != null ? library.GetProfile(place) : null;
        return profile != null ? profile.displayName : place.ToString();
    }

    /// <summary>A portal under maintenance: its repair's Orders state (OrderBook.StateOf), its price, and the portal whose repair it needs first.</summary>
    private static string RepairLine(WorldState world, ContentLibrarySO library, PortalDay day, PortalRoute portal)
    {
        UpgradeSO repair = library != null ? library.GetUpgradeById(portal.Repair) : null;
        if (repair == null || world == null)
            return PortalText.RepairLine(false, OrderState.Locked, null, null, UiText.Get);

        List<string> missing = UpgradeTree.Missing(repair.Node, world.HasUpgrade);
        string needs = missing.Count > 0 ? NeedsName(library, day, missing[0]) : null;
        return PortalText.RepairLine(true, OrderBook.StateOf(world, library, repair), AccountWindow.Amount(OrderBook.Price(world, library, repair)), needs, UiText.Get);
    }

    /// <summary>A missing prerequisite by the portal it repairs ("02"), else by its name.</summary>
    private static string NeedsName(ContentLibrarySO library, PortalDay day, string upgradeId)
    {
        foreach (PortalRoute p in day.Portals)
            if (p.Repair == upgradeId)
                return PortalText.Number(p.Number);
        UpgradeSO upgrade = library.GetUpgradeById(upgradeId);
        return upgrade != null ? upgrade.displayName : upgradeId;
    }
}
