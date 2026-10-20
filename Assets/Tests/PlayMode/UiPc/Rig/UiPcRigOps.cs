#if UNITY_EDITOR || TIMEDESK_QA
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

/// <summary>
/// UI-PC's scenario ops for the QA rig (docs/pending/UI-PC.md, the PC
/// session; FIX_PLAN v1 §3 UI-PC "Verification": "Open 8 windows; minimise
/// and restore each app; double-click Order; press Enter after clicks; zoom
/// 150%; compare balances across Account, Orders and the HUD"; v2 delta).
/// Each plays the player's input through the rig's virtual mouse and
/// keyboard (clicks, the wheel, keys and chords as Input System events,
/// never a handler called directly) and asserts what the player would see.
/// <list type="bullet">
/// <item><c>uipc.esc</c>: Esc on the PC brings Settings to the front and the PC stays open (R3/R4).</item>
/// <item><c>uipc.escdark</c>: with the screen turned off by the frame's power button, Esc turns it on and brings Settings forward, a right-click does nothing, and the PC stays open (R3/R4).</item>
/// <item><c>uipc.room</c>: a click on the room beside the frame does not leave the PC (R3/R4: only Q and the bezel).</item>
/// <item><c>uipc.bezel</c>: a click on the bezel leaves the PC (R3/R4: "press Q or click the bezel").</item>
/// <item><c>uipc.tray</c>: the tray clock reads the shift clock's time over today's date, the CRT clock plate is gone, no credits in the tray (BUG-20a, 21a).</item>
/// <item><c>uipc.windows</c>: every app opened from the Start menu, and the F1 card (at least 8 windows); the taskbar names each open window by a button or counts it behind the chevron, nothing over the tray, and the chevron brings each waiting window forward in turn (BUG-21a).</item>
/// <item><c>uipc.order</c>: a double-click on an orderable Orders card places exactly one order, charged once (BUG-58a, UI-25).</item>
/// <item><c>uipc.enter</c>: Enter on the chosen card acts once per press, and two quick presses act once (BUG-58a, CODE-117).</item>
/// <item><c>uipc.zoom</c>: Ctrl+= twice zooms the Orders tree to 150 %.</item>
/// <item><c>uipc.restore</c>: Orders scrolled by the wheel, minimised by its title bar's button and restored by its taskbar button keeps its scroll, its chosen card and its zoom (BUG-58a, CODE-114).</item>
/// <item><c>uipc.balance</c>: with the Citizen Account open and scrolled, money arriving redraws it in place, and the Account, Orders and the office's credits readout show the same balance (BUG-58a, CODE-121).</item>
/// </list>
/// Compiled only in the editor and QA player builds.
/// </summary>
public static class UiPcRigOps
{
    private const string Owner = "UI-PC";

    /// <summary>The wheel's step as the Input System reports one notch.</summary>
    private const float WheelNotch = 120f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        QaRunner.RegisterOp("uipc.esc", Esc);
        QaRunner.RegisterOp("uipc.escdark", EscDark);
        QaRunner.RegisterOp("uipc.room", Room);
        QaRunner.RegisterOp("uipc.bezel", Bezel);
        QaRunner.RegisterOp("uipc.windows", Windows);
        QaRunner.RegisterOp("uipc.order", Order);
        QaRunner.RegisterOp("uipc.enter", Enter);
        QaRunner.RegisterOp("uipc.zoom", Zoom);
        QaRunner.RegisterOp("uipc.restore", Restore);
        QaRunner.RegisterOp("uipc.balance", Balance);
        QaRunner.RegisterOp("uipc.tray", Tray);
    }

    // -----------------------------
    // The exits (BUG-50a, R3/R4)
    // -----------------------------

    /// <summary>Esc with nothing open on the desktop: Settings comes forward, focused, and the PC stays open.</summary>
    private static IEnumerator Esc(QaRunner runner, QaStep step)
    {
        OfficeViewController view = QaGame.Find<OfficeViewController>();
        DesktopWindow settings = Window<SettingsWindowController>();
        yield return runner.Input.Tap(Key.Escape);
        yield return QaGame.Frames(3);
        bool open = PcOpen(view);
        bool front = settings != null && settings.IsOpen && !settings.IsMinimised;
        runner.Assert("esc", Owner, open && front, $"pc {(open ? "open" : "closed")}, settings {(front ? "in front" : "not shown")}",
            "Esc opens Settings everywhere (R3/R4); only Q and the bezel leave the PC");
    }

    /// <summary>The frame's power button darkens the screen; then Esc turns it on and brings Settings forward, and a right-click does nothing; the PC stays open throughout.</summary>
    private static IEnumerator EscDark(QaRunner runner, QaStep step)
    {
        OfficeViewController view = QaGame.Find<OfficeViewController>();
        PcFrame frame = QaGame.Find<PcFrame>();
        MonitorScreen screen = QaGame.Find<MonitorScreen>();
        DesktopWindow settings = Window<SettingsWindowController>();
        Transform power = frame != null ? FindDeep(frame.transform, "PowerButton") : null;
        Vector2? at = power != null ? QaGame.ScreenPoint(power.gameObject) : null;
        if (screen == null || settings == null || !at.HasValue)
        {
            runner.Assert("escdark", Owner, false, "no PC frame, screen or Settings", "the PC must be open in the office");
            yield break;
        }
        if (settings.IsOpen)
            settings.Close(); // Settings must come back by the Esc, not stay from the step before
        yield return QaGame.Frames(2);
        yield return runner.Input.Play(QaPaths.Click(at.Value));
        yield return QaGame.Frames(3);
        bool dark = !screen.IsOn;
        yield return RightClick(runner);
        yield return QaGame.Frames(3);
        bool afterRight = PcOpen(view) && !screen.IsOn;
        yield return runner.Input.Tap(Key.Escape);
        yield return QaGame.Frames(4);
        bool lit = screen.IsOn, front = settings.IsOpen && !settings.IsMinimised, open = PcOpen(view);
        runner.Assert("escdark", Owner, dark && afterRight && lit && front && open,
            $"power button: screen {(dark ? "off" : "still on")}; right-click: {(afterRight ? "nothing" : "changed something")}; Esc: screen {(lit ? "on" : "off")}, settings {(front ? "in front" : "not shown")}, pc {(open ? "open" : "closed")}",
            "with the screen off, Esc still opens Settings (and turns the screen on) and nothing but Q and the bezel leaves the PC (R3/R4)");
    }

    /// <summary>A click on the room right of the frame: the PC stays open.</summary>
    private static IEnumerator Room(QaRunner runner, QaStep step)
    {
        OfficeViewController view = QaGame.Find<OfficeViewController>();
        PcFrame frame = QaGame.Find<PcFrame>();
        Transform bezel = frame != null ? FindDeep(frame.transform, "Frame") : null;
        Rect? rect = bezel != null ? QaGame.ScreenRect(bezel.gameObject) : null;
        if (!rect.HasValue)
        {
            runner.Assert("room", Owner, false, "no PC frame", "the PC must be open in the office");
            yield break;
        }
        var point = new Vector2((rect.Value.xMax + Screen.width) / 2f, Screen.height * 0.5f); // halfway between the frame and the screen's right edge
        yield return runner.Input.Play(QaPaths.Click(point));
        yield return QaGame.Frames(3);
        bool open = PcOpen(view);
        runner.Assert("room", Owner, open, $"pc {(open ? "still open" : "closed")} after a click on the room at {point}",
            "only Q and the bezel leave the PC (R3/R4, 2026-10-20; the 2026-09-30 click-outside exit is overridden)");
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

    // -----------------------------
    // The taskbar (BUG-20a, 21a)
    // -----------------------------

    /// <summary>Opens every app the Start menu lists today by clicking Start, then its entry; checks the taskbar's buttons and chevron against the open windows, then walks the chevron.</summary>
    private static IEnumerator Windows(QaRunner runner, QaStep step)
    {
        DesktopShell shell = QaGame.Find<DesktopShell>();
        DesktopWindowManager manager = QaGame.Find<DesktopWindowManager>();
        Transform start = shell != null ? FindDeep(shell.transform, "StartButton") : null;
        Transform menu = shell != null ? FindDeep(shell.transform, "StartMenu") : null;
        if (start == null || menu == null || manager == null)
        {
            runner.Assert("windows", Owner, false, "no Start button, menu or window manager", "the PC must be open in the office");
            yield break;
        }
        int opened = 0;
        foreach (string id in DesktopAppIds.DefaultOrder)
        {
            yield return ClickObject(runner, start);
            yield return QaGame.Frames(3);
            Transform entry = menu.Find("App_" + id);
            if (entry == null || !entry.gameObject.activeInHierarchy)
            {
                yield return runner.Input.Tap(Key.Escape); // not on the PC today: close the menu
                continue;
            }
            yield return ClickObject(runner, entry);
            yield return QaGame.Frames(6);
            opened++;
        }
        yield return runner.Input.Tap(Key.F1); // and the shortcut card, a window too
        yield return QaGame.Frames(5);

        int open = OpenWindows().Count;
        Transform strip = FindDeep(shell.transform, "WindowButtons");
        Transform tray = FindDeep(shell.transform, "Tray");
        Rect? trayRect = tray != null ? QaGame.ScreenRect(tray.gameObject) : null;
        int shown = 0, overTray = 0;
        foreach (Transform child in strip)
            if (child.name == "WindowButton" && child.gameObject.activeInHierarchy)
            {
                shown++;
                Rect? r = QaGame.ScreenRect(child.gameObject);
                if (r.HasValue && trayRect.HasValue && r.Value.xMax > trayRect.Value.xMin + 1f)
                    overTray++;
            }
        Transform chevron = strip.Find("OverflowButton");
        int waiting = chevron != null && chevron.gameObject.activeInHierarchy ? Count(chevron.GetComponentInChildren<TMP_Text>(true)) : 0;
        bool counted = open >= 8 && open >= opened && shown + waiting == open && overTray == 0;

        // The chevron brings each waiting window forward in turn: the focused window's button then shows.
        int walked = 0;
        for (int i = 0; i < waiting; i++)
        {
            yield return ClickObject(runner, chevron);
            yield return QaGame.Frames(5);
            DesktopWindow focused = manager.FocusedWindow;
            if (focused != null && ButtonOf(strip, focused) != null)
                walked++;
        }
        runner.Assert("windows", Owner, counted && walked == waiting,
            $"{opened} apps opened from Start and the F1 card, {open} windows open; {shown} buttons + {waiting} behind the chevron; {overTray} over the tray; the chevron brought {walked}/{waiting} forward",
            "8 windows: labelled buttons plus \"» N\", nothing over the tray, the chevron brings each waiting window forward (BUG-21a)");
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

    // -----------------------------
    // Orders (BUG-58a, 28d; UI-25)
    // -----------------------------

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
        if (!orders.Window.IsOpen || orders.Window.IsMinimised || orders.Window.Manager.FocusedWindow != orders.Window)
        {
            yield return ClickObject(runner, FindDeep(orders.Window.transform, "TitleText") ?? orders.Window.transform); // bring it forward as a player would: its title bar
            yield return QaGame.Frames(5);
        }
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
                "give the run money (cheat AddMoney) and a day with Orders before the step");
            yield break;
        }
        int ordersBefore = world.orders.Count, moneyBefore = world.money, price = OrderBook.Price(world, lib, pick);
        yield return runner.Input.Play(QaPaths.Click(at.Value, 1));
        yield return runner.Input.Play(QaPaths.Click(at.Value, 1));
        yield return QaGame.Frames(5);
        int placed = world.orders.Count - ordersBefore, charged = moneyBefore - world.money;
        bool once = placed == 1 && charged == price && Orders.Pending(world.orders, pick.id) != null && orders.Chosen == pick.id;
        runner.Assert("order", Owner, once, $"orders +{placed}, charged {charged} (price {price}), chosen {orders.Chosen ?? "none"}",
            "a double-click on an orderable card places exactly one order (one-click ordering; never order-then-cancel)");
    }

    /// <summary>On the card ordered by uipc.order: Enter cancels it once (refunded once), Enter again after the double-click time orders it once, and two quick Enters act once.</summary>
    private static IEnumerator Enter(QaRunner runner, QaStep step)
    {
        WorldState world = RunManager.HasInstance ? RunManager.Instance.World : null;
        ContentLibrarySO lib = RunManager.HasInstance ? RunManager.Instance.Library : null;
        OrdersWindow orders = QaGame.Find<OrdersWindow>();
        UpgradeSO upgrade = lib != null && orders != null && orders.Chosen != null ? lib.GetUpgradeById(orders.Chosen) : null;
        if (world == null || upgrade == null || Orders.Pending(world.orders, upgrade.id) == null)
        {
            runner.Assert("enter", Owner, false, "no ordered card chosen", "run uipc.order first");
            yield break;
        }
        float wait = orders.Window.Manager.DoubleClickSeconds + 0.15f;
        int money0 = world.money, price = OrderBook.Price(world, lib, upgrade);
        yield return runner.Input.Tap(Key.Enter);
        yield return QaGame.Frames(3);
        bool cancelled = Orders.Pending(world.orders, upgrade.id) == null && world.money == money0 + price;
        yield return new WaitForSecondsRealtime(wait);
        yield return runner.Input.Tap(Key.Enter);
        yield return QaGame.Frames(3);
        bool reordered = Orders.Pending(world.orders, upgrade.id) != null && world.money == money0;
        yield return new WaitForSecondsRealtime(wait);
        yield return runner.Input.Tap(Key.Enter, 1);
        yield return runner.Input.Tap(Key.Enter, 1);
        yield return QaGame.Frames(3);
        bool once = Orders.Pending(world.orders, upgrade.id) == null && world.money == money0 + price;
        yield return new WaitForSecondsRealtime(wait);
        yield return runner.Input.Tap(Key.Enter); // ordered again, for the steps after
        yield return QaGame.Frames(3);
        bool back = Orders.Pending(world.orders, upgrade.id) != null && world.money == money0;
        runner.Assert("enter", Owner, cancelled && reordered && once && back,
            $"Enter: {(cancelled ? "cancelled, refunded once" : "not one cancel")}; Enter: {(reordered ? "ordered, charged once" : "not one order")}; two quick Enters: {(once ? "one action" : "not one action")}; money {world.money} (start {money0})",
            "one action per Enter, and two Enters within the double-click time act once (BUG-58a, CODE-117: Enter is the desktop's keys only)");
    }

    /// <summary>Ctrl+= twice with Orders focused: the tree is at 150 %.</summary>
    private static IEnumerator Zoom(QaRunner runner, QaStep step)
    {
        OrdersWindow orders = QaGame.Find<OrdersWindow>();
        TreeScrollRect view = orders != null ? orders.GetComponentInChildren<TreeScrollRect>(true) : null;
        if (view == null)
        {
            runner.Assert("zoom", Owner, false, "no Orders tree", "open Orders first (uipc.order)");
            yield break;
        }
        yield return Chord(runner, Key.LeftCtrl, Key.Equals);
        yield return Chord(runner, Key.LeftCtrl, Key.Equals);
        yield return QaGame.Frames(5);
        runner.Assert("zoom", Owner, view.Level == 150, $"zoom {view.Level} %", "Ctrl+= steps the Orders tree 100 -> 125 -> 150 %");
    }

    /// <summary>Scrolls Orders' tree with the wheel, minimises it with its title bar's button and restores it with its taskbar button: the scroll, the chosen card and the zoom are kept.</summary>
    private static IEnumerator Restore(QaRunner runner, QaStep step)
    {
        OrdersWindow orders = QaGame.Find<OrdersWindow>();
        DesktopWindow window = orders != null ? orders.Window : null;
        TreeScrollRect view = orders != null ? orders.GetComponentInChildren<TreeScrollRect>(true) : null;
        DesktopShell shell = QaGame.Find<DesktopShell>();
        Transform strip = shell != null ? FindDeep(shell.transform, "WindowButtons") : null;
        Transform minimise = window != null ? FindDeep(window.transform, "MinBtn") : null;
        if (window == null || view == null || strip == null || minimise == null || !window.IsOpen)
        {
            runner.Assert("restore", Owner, false, "no open Orders window", "open Orders first (uipc.order)");
            yield break;
        }
        Vector2? over = QaGame.ScreenPoint(view.gameObject);
        if (over.HasValue)
        {
            yield return runner.Input.MoveTo(over.Value, 5);
            yield return Wheel(runner, -3f);
        }
        yield return QaGame.Frames(5);
        Vector2 before = view.content.anchoredPosition;
        string chosen = orders.Chosen;
        int level = view.Level;
        yield return ClickObject(runner, minimise);
        yield return QaGame.Frames(15);
        bool minimised = window.IsMinimised;
        Transform button = ButtonOf(strip, window);
        if (button != null)
            yield return ClickObject(runner, button);
        yield return QaGame.Frames(15);
        Vector2 after = view.content.anchoredPosition;
        bool same = minimised && !window.IsMinimised && (after - before).sqrMagnitude < 1f && orders.Chosen == chosen && view.Level == level;
        runner.Assert("restore", Owner, same,
            $"wheel scroll {before}; minimised {minimised}; restored by its taskbar button {(button != null ? "yes" : "no button")}: scroll {after}, chosen {chosen ?? "none"} -> {orders.Chosen ?? "none"}, zoom {level} -> {view.Level} %",
            "an app keeps its state across minimise and restore");
    }

    /// <summary>Opens the Citizen Account from Start, scrolls it with the wheel, then money arrives (the AddMoney cheat): the Account redraws in place and the Account, Orders and the office's credits readout show the same balance.</summary>
    private static IEnumerator Balance(QaRunner runner, QaStep step)
    {
        WorldState world = RunManager.HasInstance ? RunManager.Instance.World : null;
        DesktopShell shell = QaGame.Find<DesktopShell>();
        AccountWindow account = QaGame.Find<AccountWindow>();
        OrdersWindow orders = QaGame.Find<OrdersWindow>();
        Transform start = shell != null ? FindDeep(shell.transform, "StartButton") : null;
        Transform entry = shell != null ? FindDeep(shell.transform, "App_" + DesktopAppIds.CitizenAccount) : null;
        if (world == null || account == null || orders == null || start == null || entry == null)
        {
            runner.Assert("balance", Owner, false, "no run, Account, Orders or Start menu", "the PC must be open in the office with a run");
            yield break;
        }
        yield return ClickObject(runner, start);
        yield return QaGame.Frames(3);
        yield return ClickObject(runner, entry);
        yield return QaGame.Frames(8);
        ScrollRect scroll = account.GetComponentInChildren<ScrollRect>(true);
        Vector2? over = scroll != null ? QaGame.ScreenPoint(scroll.gameObject) : null;
        if (over.HasValue)
        {
            yield return runner.Input.MoveTo(over.Value, 5);
            yield return Wheel(runner, -4f);
        }
        yield return QaGame.Frames(5);
        Vector2 before = scroll != null ? scroll.content.anchoredPosition : Vector2.zero;
        DevCheats.Run(DevCheat.AddMoney, RunManager.Instance, 50, 0f, null);
        yield return QaGame.Frames(5);
        Vector2 after = scroll != null ? scroll.content.anchoredPosition : Vector2.zero;
        int money = world.money;
        bool kept = scroll != null && (after - before).sqrMagnitude < 1f;
        bool inAccount = Shows(account.transform, AccountWindow.Amount(money));
        TMP_Text wallet = (TMP_Text)QaGame.Field(orders, "walletText");
        bool inOrders = wallet != null && wallet.text == OrderLines.Wallet(world);
        OfficeReadouts readouts = QaGame.Find<OfficeReadouts>();
        TMP_Text credits = readouts != null ? (TMP_Text)QaGame.Field(readouts, "_creditsText") : null;
        bool inHud = credits == null || credits.text == money.ToString(CultureInfo.InvariantCulture);
        runner.Assert("balance", Owner, kept && inAccount && inOrders && inHud,
            $"money {money}: Account {(inAccount ? "shows it" : "stale")} (scroll {before} -> {after}), Orders '{(wallet != null ? wallet.text : "none")}', office credits '{(credits != null ? credits.text : "no readout")}'",
            "the wallet is the same everywhere and the Account follows it live without losing its place (BUG-58a, CODE-121)");
    }

    // -----------------------------
    // Input and lookups
    // -----------------------------

    /// <summary>A left click at the middle of <paramref name="target"/> (skipped when it is not on screen).</summary>
    private static IEnumerator ClickObject(QaRunner runner, Transform target)
    {
        Vector2? at = target != null ? QaGame.ScreenPoint(target.gameObject) : null;
        if (at.HasValue)
            yield return runner.Input.Play(QaPaths.Click(at.Value));
    }

    /// <summary>A right click where the pointer is (the virtual mouse's right button, two frames).</summary>
    private static IEnumerator RightClick(QaRunner runner)
    {
        InputSystem.QueueStateEvent(runner.Input.Mouse, new MouseState { position = runner.Input.Position }.WithButton(MouseButton.Right));
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(runner.Input.Mouse, new MouseState { position = runner.Input.Position });
        yield return null;
    }

    /// <summary>A chord: <paramref name="modifier"/> held with <paramref name="key"/> for two frames, then both let go.</summary>
    private static IEnumerator Chord(QaRunner runner, Key modifier, Key key)
    {
        InputSystem.QueueStateEvent(runner.Input.Keyboard, new KeyboardState(modifier, key));
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(runner.Input.Keyboard, new KeyboardState());
        yield return null;
    }

    /// <summary>The wheel turned <paramref name="notches"/> notches where the pointer is (negative: down), one notch a frame.</summary>
    private static IEnumerator Wheel(QaRunner runner, float notches)
    {
        int count = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(notches)));
        float sign = Mathf.Sign(notches);
        for (int i = 0; i < count; i++)
        {
            InputSystem.QueueStateEvent(runner.Input.Mouse, new MouseState { position = runner.Input.Position, scroll = new Vector2(0f, sign * WheelNotch) });
            yield return null;
            InputSystem.QueueStateEvent(runner.Input.Mouse, new MouseState { position = runner.Input.Position });
            yield return null;
        }
    }

    /// <summary>True while the PC is open (the office's view is on the monitor).</summary>
    private static bool PcOpen(OfficeViewController view) => view != null && view.Current == OfficeView.MonitorFocus;

    /// <summary>The desktop's open windows (minimised ones included).</summary>
    private static List<DesktopWindow> OpenWindows()
    {
        var open = new List<DesktopWindow>();
        foreach (DesktopWindow w in Object.FindObjectsByType<DesktopWindow>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (w.IsOpen)
                open.Add(w);
        return open;
    }

    /// <summary>The shown taskbar button that names <paramref name="window"/>, or null.</summary>
    private static Transform ButtonOf(Transform strip, DesktopWindow window)
    {
        foreach (Transform child in strip)
            if (child.name == "WindowButton" && child.gameObject.activeInHierarchy)
            {
                Transform label = child.Find("Label");
                if (label != null && label.GetComponent<TMP_Text>().text == window.Title)
                    return child;
            }
        return null;
    }

    /// <summary>The count in the chevron's label ("» 3" is 3), or 0.</summary>
    private static int Count(TMP_Text label)
    {
        string digits = string.Empty;
        foreach (char c in label != null ? label.text : string.Empty)
            if (char.IsDigit(c))
                digits += c;
        return int.TryParse(digits, out int n) ? n : 0;
    }

    /// <summary>True when a text under <paramref name="root"/> shows <paramref name="value"/>.</summary>
    private static bool Shows(Transform root, string value)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(false))
            if (text.text != null && text.text.Contains(value))
                return true;
        return false;
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
