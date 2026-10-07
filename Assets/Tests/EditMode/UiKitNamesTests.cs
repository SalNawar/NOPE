using NUnit.Framework;

/// <summary>The UI kit's sprite-name grammar and its dark faces (docs/UI_KIT.md).</summary>
public class UiKitNamesTests
{
    [Test]
    public void Of_AppendsTheStateWord()
    {
        Assert.AreEqual("plate_ox_rest", UiKitNames.Of("plate_ox", KitState.Rest));
        Assert.AreEqual("plate_ox_hover", UiKitNames.Of("plate_ox", KitState.Hover));
        Assert.AreEqual("pulltab_left_pressed", UiKitNames.Of("pulltab_left", KitState.Pressed));
        Assert.AreEqual("card_locked", UiKitNames.Of("card", KitState.Locked));
    }

    [Test]
    public void DarkFace_DarkPlatesPrintInBone()
    {
        Assert.IsTrue(UiKitNames.DarkFace("plate_ox"));
        Assert.IsTrue(UiKitNames.DarkFace("plate_slate_hover"));
        Assert.IsTrue(UiKitNames.DarkFace("ribbon_red"));
        Assert.IsTrue(UiKitNames.DarkFace("pulltab_right"));
        Assert.IsTrue(UiKitNames.DarkFace("titlebar_slate"));
    }

    [Test]
    public void UpgradeCard_OneCardPerState()
    {
        Assert.AreEqual("upgradecard_owned", UiKitNames.UpgradeCard(OrderState.Owned));
        Assert.AreEqual("upgradecard_buyable", UiKitNames.UpgradeCard(OrderState.Orderable));
        Assert.AreEqual("upgradecard_buyable", UiKitNames.UpgradeCard(OrderState.InTransit));
        Assert.AreEqual("upgradecard_dear", UiKitNames.UpgradeCard(OrderState.TooDear));
        Assert.AreEqual("upgradecard_locked", UiKitNames.UpgradeCard(OrderState.Locked));
    }

    [Test]
    public void UpgradeBadge_TickClockPadlockOrNone()
    {
        Assert.AreEqual("roundbadge_tick", UiKitNames.UpgradeBadge(OrderState.Owned));
        Assert.AreEqual("roundbadge_clock", UiKitNames.UpgradeBadge(OrderState.TooDear));
        Assert.AreEqual("roundbadge_clock", UiKitNames.UpgradeBadge(OrderState.InTransit));
        Assert.AreEqual("roundbadge_padlock", UiKitNames.UpgradeBadge(OrderState.Locked));
        Assert.IsNull(UiKitNames.UpgradeBadge(OrderState.Orderable));
    }

    [Test]
    public void VerdictRibbon_RightWrongWarningNotice()
    {
        Assert.AreEqual("ribbon_green", UiKitNames.VerdictRibbon(true, false));
        Assert.AreEqual("ribbon_red", UiKitNames.VerdictRibbon(false, false));
        Assert.AreEqual("ribbon_brass", UiKitNames.VerdictRibbon(false, true));
        Assert.AreEqual("ribbon_brass", UiKitNames.VerdictRibbon(null, false));
    }

    [Test]
    public void WheelTile_ByChoiceKind()
    {
        Assert.AreEqual("tile_eye", UiKitNames.WheelTile(DialogChoiceKind.Look));
        Assert.AreEqual("tile_idcard", UiKitNames.WheelTile(DialogChoiceKind.Request));
        Assert.AreEqual("tile_speech", UiKitNames.WheelTile(DialogChoiceKind.Question));
        Assert.AreEqual("tile_speech", UiKitNames.WheelTile(DialogChoiceKind.Dialog));
        Assert.AreEqual("tile_person", UiKitNames.WheelTile(DialogChoiceKind.Normal));
    }

    [Test]
    public void GuidePill_RedTutorialGreenMoment()
    {
        Assert.AreEqual("pill_red", UiKitNames.GuidePill(true));
        Assert.AreEqual("pill_green", UiKitNames.GuidePill(false));
    }

    [Test]
    public void DarkFace_LightFacesPrintInInk()
    {
        Assert.IsFalse(UiKitNames.DarkFace("card"));
        Assert.IsFalse(UiKitNames.DarkFace("ribbon_brass"));
        Assert.IsFalse(UiKitNames.DarkFace("plate_bone"));
        Assert.IsFalse(UiKitNames.DarkFace("miniplate_bone"));
        Assert.IsFalse(UiKitNames.DarkFace("panel_bone"));
        Assert.IsFalse(UiKitNames.DarkFace("listrow_selected"));
        Assert.IsFalse(UiKitNames.DarkFace("wheelpill_rest"));
        Assert.IsFalse(UiKitNames.DarkFace(null));
    }
}
