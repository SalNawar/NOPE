#if UNITY_EDITOR || TIMEDESK_QA
using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// UI-PC's scenario ops for the QA rig (docs/pending/UI-PC.md, the PC
/// session; FIX_PLAN v1 §3 UI-PC "Verification", v2 delta): each plays the
/// player's input through the rig's virtual mouse and keyboard and asserts
/// what the player would see.
/// <list type="bullet">
/// <item><c>uipc.esc</c>: Esc on the PC brings Settings to the front and the PC stays open (R3/R4).</item>
/// <item><c>uipc.bezel</c>: a click on the bezel leaves the PC (R3/R4: "press Q or click the bezel").</item>
/// <item><c>uipc.order</c>: a double-click on an orderable Orders card places exactly one order, charged once (BUG-58a, UI-25).</item>
/// <item><c>uipc.restore</c>: Orders minimised and restored keeps its scroll and its selection (BUG-58a, CODE-114).</item>
/// <item><c>uipc.tray</c>: the tray clock reads the shift clock's time over today's date, the CRT clock plate is gone, no credits in the tray (BUG-20a, 21a).</item>
/// </list>
/// Compiled only in the editor and QA player builds.
/// </summary>
public static class UiPcRigOps
{
    private const string Owner = "UI-PC";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        QaRunner.RegisterOp("uipc.esc", Esc);
        QaRunner.RegisterOp("uipc.bezel", Bezel);
        QaRunner.RegisterOp("uipc.order", Order);
        QaRunner.RegisterOp("uipc.restore", Restore);
        QaRunner.RegisterOp("uipc.tray", Tray);
    }

    /// <summary>Esc with nothing open on the desktop: Settings comes forward, focused, and the PC stays open.</summary>
    private static IEnumerator Esc(QaRunner runner, QaStep step)
    {
        OfficeViewController view = QaGame.Find<OfficeViewController>();
        DesktopWindow settings = Window<SettingsWindowController>();
        yield return runner.Input.Tap(Key.Escape);
        yield return QaGame.Frames(3);
        bool open = view != null && view.Current == OfficeView.MonitorFocus;
        bool front = settings != null && settings.IsOpen && !settings.IsMinimised;
        runner.Assert("esc", Owner, open && front, $"pc {(open ? "open" : "closed")}, settings {(front ? "in front" : "not shown")}",
            "Esc opens Settings everywhere (R3/R4); only Q and the bezel leave the PC");
    }

    /// <summary>A click on the bezel's left band (between its edge and the glass): the PC closes.</summary>
    private static IEnumerator Bezel(QaRunner runner, QaStep step)
    {
        OfficeViewController view = QaGame.Find<OfficeViewController>();
        PcFrame frame = QaGame.Find<PcFrame>();
        var glass = frame != null ? (RectTransform)typeof(PcFrame).GetField("glass", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(frame) : null;
        if (glass == null || !(glass.parent is RectTransform bezel))
        {
            runner.Assert("bezel", Owner, false, "no PC frame", "the frame's glass and bezel must be built");
            yield break;
        }
        var b = new Vector3[4];
        var g = new Vector3[4];
        bezel.GetWorldCorners(b);
        glass.GetWorldCorners(g);
        var point = new Vector2((b[0].x + g[0].x) / 2f, (g[0].y + g[1].y) / 2f); // the overlay canvas's world space is the screen
        yield return runner.Input.Play(QaPaths.Click(point));
        yield return QaGame.Frames(3);
        bool left = view != null && view.Current == OfficeView.OfficeFocus;
        runner.Assert("bezel", Owner, left, $"pc {(left ? "closed" : "still open")} after a click at {point}", "a click on the bezel leaves the PC");
    }

    /// <summary>Opens Orders, double-clicks the first orderable card: one order placed, its price charged once.</summary>
    private static IEnumerator Order(QaRunner runner, QaStep step)
    {
        WorldState world = RunManager.HasInstance ? RunManager.Instance.World : null;
        ContentLibrarySO lib = RunManager.HasInstance ? RunManager.Instance.Library : null;
        OrdersWindow orders = QaGame.Find<OrdersWindow>();
        DesktopApps apps = QaGame.Find<DesktopApps>();
        if (world == null || lib == null || orders == null || apps == null)
        {
            runner.Assert("order", Owner, false, "no run or no Orders app", "the PC session needs a run in the office");
            yield break;
        }
        apps.OpenApp(DesktopAppIds.Orders);
        yield return QaGame.Frames(10);
        UpgradeSO pick = null;
        foreach (UpgradeSO u in OrderBook.Catalogue(lib))
            if (OrderBook.StateOf(world, lib, u) == OrderState.Orderable)
            {
                pick = u;
                break;
            }
        Transform card = pick != null ? FindDeep(orders.transform, "Node_" + pick.id) : null;
        Vector2? at = card != null ? QaGame.ScreenPoint(card.gameObject) : null;
        if (!at.HasValue)
        {
            runner.Assert("order", Owner, false, pick == null ? "nothing orderable" : "the card is off screen",
                "give the run money (cheat GiveMoney) and a day with Orders before the step");
            yield break;
        }
        int ordersBefore = world.orders.Count, moneyBefore = world.money, price = OrderBook.Price(world, lib, pick);
        yield return runner.Input.Play(QaPaths.Click(at.Value, 1));
        yield return runner.Input.Play(QaPaths.Click(at.Value, 1));
        yield return QaGame.Frames(5);
        int placed = world.orders.Count - ordersBefore, charged = moneyBefore - world.money;
        bool once = placed == 1 && charged == price && Orders.Pending(world.orders, pick.id) != null;
        runner.Assert("order", Owner, once, $"orders +{placed}, charged {charged} (price {price})",
            "a double-click on an orderable card places exactly one order (one-click ordering; never order-then-cancel)");
    }

    /// <summary>Scrolls Orders' tree, minimises and restores the window: the scroll and the selection are kept.</summary>
    private static IEnumerator Restore(QaRunner runner, QaStep step)
    {
        OrdersWindow orders = QaGame.Find<OrdersWindow>();
        DesktopWindow window = orders != null ? orders.Window : null;
        ScrollRect scroll = orders != null ? orders.GetComponentInChildren<ScrollRect>(true) : null;
        if (window == null || scroll == null)
        {
            runner.Assert("restore", Owner, false, "no Orders window", "open Orders first (uipc.order)");
            yield break;
        }
        window.Open();
        yield return QaGame.Frames(5);
        scroll.normalizedPosition = new Vector2(0.5f, 0.5f);
        yield return QaGame.Frames(2);
        Vector2 before = scroll.content.anchoredPosition;
        string selected = (string)typeof(OrdersWindow).GetField("_selected", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(orders);
        window.Minimise();
        yield return QaGame.Frames(15);
        window.Open();
        yield return QaGame.Frames(15);
        Vector2 after = scroll.content.anchoredPosition;
        string kept = (string)typeof(OrdersWindow).GetField("_selected", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(orders);
        bool same = (after - before).sqrMagnitude < 1f && kept == selected;
        runner.Assert("restore", Owner, same, $"scroll {before} -> {after}, selection {selected ?? "none"} -> {kept ?? "none"}",
            "an app keeps its state across minimise and restore");
    }

    /// <summary>The tray: the clock reads the shift clock (within a minute), the date line is today's, the CRT plate is not shown, and no credits are in the tray.</summary>
    private static IEnumerator Tray(QaRunner runner, QaStep step)
    {
        yield return QaGame.Frames(2);
        ShiftClockDriver driver = QaGame.Find<ShiftClockDriver>();
        OfficeUIController office = QaGame.Find<OfficeUIController>();
        TMP_Text clock = Text("ClockText"), date = Text("DayText");
        CrtClock plate = QaGame.Find<CrtClock>();
        string expected = driver != null && driver.Clock != null ? ShiftClock.Format(driver.Clock.CurrentMinute) : null;
        string shownTime = clock != null ? clock.text : null;
        bool timeOk = expected != null && shownTime != null && Minutes(shownTime) >= 0 && System.Math.Abs(Minutes(shownTime) - Minutes(expected)) <= 1;
        bool dateOk = date != null && !string.IsNullOrWhiteSpace(date.text) && date.text.IndexOf('·') < 0;
        bool plateGone = plate == null || !plate.gameObject.activeInHierarchy;
        bool noCredits = Text("MoneyText") == null;
        runner.Assert("tray", Owner, timeOk && dateOk && plateGone && noCredits && office != null,
            $"tray '{shownTime}' / '{(date != null ? date.text : "none")}', shift {expected}, plate {(plateGone ? "gone" : "shown")}, credits {(noCredits ? "out" : "in")} the tray",
            "a Windows-style tray clock, time over date, matching the shift clock; no CRT box; credits and the river out of the tray");
    }

    /// <summary>The desktop window of the app whose controller is <typeparamref name="T"/>.</summary>
    private static DesktopWindow Window<T>() where T : Component
    {
        T controller = QaGame.Find<T>();
        return controller != null ? controller.GetComponent<DesktopWindow>() : null;
    }

    /// <summary>A tray text by name under the desktop's taskbar, or null.</summary>
    private static TMP_Text Text(string name)
    {
        DesktopShell shell = QaGame.Find<DesktopShell>();
        Transform t = shell != null ? FindDeep(shell.transform.Find("Taskbar"), name) : null;
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }

    /// <summary>The first descendant named <paramref name="name"/>, or null.</summary>
    private static Transform FindDeep(Transform root, string name)
    {
        if (root == null)
            return null;
        if (root.name == name)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), name);
            if (found != null)
                return found;
        }
        return null;
    }

    /// <summary>"HH:MM" as minutes of the day, or -1.</summary>
    private static int Minutes(string hhmm)
    {
        string[] parts = (hhmm ?? string.Empty).Split(':');
        return parts.Length == 2 && int.TryParse(parts[0], out int h) && int.TryParse(parts[1], out int m) ? h * 60 + m : -1;
    }
}
#endif
