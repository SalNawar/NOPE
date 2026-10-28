using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI-PC's P0-A seams, as UI-PC ships them: PcFrame.CanOpen (true while the
/// screen is on, MonitorScreen telling it; true with no screen; the F1 guard
/// reads it, BUG-58c) and OrderBook.CancelPendingFor (OrderBook's own
/// same-day cancel and refund, Cancel: a pending order is always today's).
/// The tray (Saleh 2026-10-28, "Task bar still has this black strip on it"):
/// it has no plate of its own, so the taskbar's cream body shows through it,
/// its clock is the bar's dark ink, and it is as wide as its words and the
/// Helix River's strip (its clock button forced every child wide, which made
/// the tray flexible: it took half the bar's free room under the kit's dark
/// glass). Tests in the "NeedsEditor" category create Unity objects and run
/// only inside Unity.
/// </summary>
public class UiPcSeamTests
{
    private const string Item = "seam.test.item";

    [Test]
    public void PcFrame_CanOpen_FollowsTheScreen()
    {
        Assert.IsTrue(PcFrame.CanOpen, "no screen has said otherwise");
        try
        {
            PcFrame.ScreenPowered(false);
            Assert.IsFalse(PcFrame.CanOpen, "F1 never opens a dark frame");
            PcFrame.ScreenPowered(true);
            Assert.IsTrue(PcFrame.CanOpen);
        }
        finally
        {
            PcFrame.ScreenPowered(true);
        }
    }

    [Test]
    public void CancelPendingFor_RefusesWithoutAWorldOrAnItem_AsCancel()
    {
        var world = new WorldState();
        Assert.IsFalse(OrderBook.CancelPendingFor(null, null));
        Assert.IsFalse(OrderBook.CancelPendingFor(world, null));
        Assert.AreEqual(OrderBook.Cancel(world, null), OrderBook.CancelPendingFor(world, null));
    }

    [Test, Category("NeedsEditor")]
    public void CancelPendingFor_IsCancel_TodaysOrderRefunded_YesterdaysKept()
    {
        var item = ScriptableObject.CreateInstance<UpgradeSO>();
        item.id = Item;
        try
        {
            foreach (int orderedDay in new[] { 3, 2 })
            {
                WorldState viaSeam = Ordered(orderedDay), viaCancel = Ordered(orderedDay);
                bool seam = OrderBook.CancelPendingFor(viaSeam, item);
                bool cancel = OrderBook.Cancel(viaCancel, item);
                Assert.AreEqual(cancel, seam, $"ordered on day {orderedDay}");
                Assert.AreEqual(viaCancel.money, viaSeam.money, $"ordered on day {orderedDay}");
                Assert.AreEqual(viaCancel.orders.Count, viaSeam.orders.Count, $"ordered on day {orderedDay}");
            }

            WorldState today = Ordered(3);
            Assert.IsTrue(OrderBook.CancelPendingFor(today, item));
            Assert.AreEqual(50, today.money, "the price charged comes back");
            Assert.AreEqual(0, today.orders.Count);
        }
        finally
        {
            Object.DestroyImmediate(item);
        }
    }

    /// <summary>A world on day 3 with 10 cr and the test item ordered (40 cr, in transit) on <paramref name="orderedDay"/>.</summary>
    private static WorldState Ordered(int orderedDay)
    {
        var world = new WorldState { day = 3, money = 10 };
        world.orders.Add(new OrderEntry { upgradeId = Item, orderedDay = orderedDay, price = 40 });
        return world;
    }

    /// <summary>The builder's source file <paramref name="name"/> (Assets/Editor), as text.</summary>
    private static string EditorSource(string name, [CallerFilePath] string here = "")
    {
        string path = Path.Combine("Assets", "Editor", name);
        if (!File.Exists(path))
            path = Path.Combine(Path.GetDirectoryName(here), "..", name);
        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    [Test]
    public void TheTray_HasNoPlate_ItsClockIsTheBarsInk_AndItIsNotFlexible()
    {
        string taskbar = EditorSource("OfficeSceneUIBuilder.Taskbar.cs");
        StringAssert.Contains("Transform tray = Panel(trayRow, \"Tray\", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);", taskbar,
                              "the tray is built with no image: nothing for the kit or the theme to paint dark");
        StringAssert.Contains("traySize.flexibleWidth = 0f;", taskbar, "the tray never takes the bar's free room");
        StringAssert.Contains("stack.childForceExpandWidth = false;", taskbar, "the clock button does not force its lines wide (a forced child makes the tray flexible)");
        StringAssert.DoesNotContain("stack.childForceExpandWidth = true;", taskbar);
        StringAssert.DoesNotContain("\"lcd_glass\"", EditorSource("OfficeSceneUIBuilder.KitTray.cs"), "the kit gives the tray no phosphor glass");
        StringAssert.Contains("KitType(text, KitText.Readout, trayRow, _kit.inkOnLight);", EditorSource("OfficeSceneUIBuilder.Kit.cs"),
                              "the clock and the date in the bar's dark ink");
    }

    [Test, Category("NeedsEditor")]
    public void BuildTaskbar_TheTrayIsAsWideAsItsWordsAndTheRiver_TheWindowButtonsTakeTheRest()
    {
        var canvas = new GameObject("UiPcSeamTests_Desktop", typeof(RectTransform), typeof(Canvas));
        try
        {
            ((RectTransform)canvas.transform).sizeDelta = new Vector2(1920f, 1080f);
            MethodInfo build = typeof(OfficeSceneUIBuilder).GetMethod("BuildTaskbar", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(build, "OfficeSceneUIBuilder.BuildTaskbar");
            build.Invoke(null, new object[] { canvas.transform, null, null, null, null });
            var row = (RectTransform)canvas.transform.Find("Taskbar/TrayRow");
            var tray = (RectTransform)row.Find("Tray");
            Assert.IsNull(tray.GetComponent<Image>(), "the tray has no plate: the taskbar's cream body shows through it");
            // The window buttons' strip (BuildWindowManager): flexible, as built.
            var buttons = new GameObject("WindowButtons", typeof(RectTransform), typeof(LayoutElement));
            buttons.transform.SetParent(row, false);
            buttons.transform.SetAsFirstSibling();
            buttons.GetComponent<LayoutElement>().flexibleWidth = 1f;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)canvas.transform.Find("Taskbar"));
            LayoutRebuilder.ForceRebuildLayoutImmediate(row);
            Assert.AreEqual(0f, LayoutUtility.GetFlexibleWidth(tray), 1e-4f, "the tray is not flexible");
            Assert.AreEqual(LayoutUtility.GetPreferredWidth(tray), tray.rect.width, 1f, "the tray is as wide as its river and its words");
            Assert.Less(tray.rect.width, row.rect.width * 0.4f, "far from the half of the bar it took");
        }
        finally
        {
            Object.DestroyImmediate(canvas);
        }
    }
}
