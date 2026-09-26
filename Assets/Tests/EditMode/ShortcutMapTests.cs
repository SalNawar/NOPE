using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;

/// <summary>The desktop's one shortcut table (the PC redesign KB1, KB2, section 3.4): every row, the text-field filter, the frame gate, the open menu and the F1 card.</summary>
public class ShortcutMapTests
{
    private static KeyChord K(ShortcutKey key, bool ctrl = false, bool shift = false, bool alt = false) => new KeyChord(key, ctrl, shift, alt);

    /// <summary>The Investigation app has the focus (the frame open).</summary>
    private static readonly ShortcutContext App = new ShortcutContext(frameOpen: true, appFocused: true);

    /// <summary>The app's focus ring is on a list's item.</summary>
    private static readonly ShortcutContext AppList = new ShortcutContext(frameOpen: true, appFocused: true, listFocused: true);

    /// <summary>The app's focus ring is on its tab strip.</summary>
    private static readonly ShortcutContext AppTabs = new ShortcutContext(frameOpen: true, appFocused: true, tabStripFocused: true);

    /// <summary>No window has the focus; an icon is selected.</summary>
    private static readonly ShortcutContext Icons = new ShortcutContext(frameOpen: true, desktopFocused: true, iconSelected: true);

    private static AppCommand? Resolve(KeyChord chord, ShortcutContext context) =>
        ShortcutMap.Resolve(chord, context, out AppCommand command) ? command : (AppCommand?)null;

    [Test]
    public void AnywhereOnTheDesktop_CtrlF_Esc_F1()
    {
        foreach (ShortcutContext c in new[] { App, Icons, new ShortcutContext(frameOpen: true) })
        {
            Assert.AreEqual(AppCommand.FocusSearch, Resolve(K(ShortcutKey.F, ctrl: true), c));
            Assert.AreEqual(AppCommand.Escape, Resolve(K(ShortcutKey.Escape), c));
            Assert.AreEqual(AppCommand.Help, Resolve(K(ShortcutKey.F1), c));
        }
    }

    [Test]
    public void TheDesktopsIcons_ArrowsMoveTheSelection_EnterOpens()
    {
        Assert.AreEqual(AppCommand.IconLeft, Resolve(K(ShortcutKey.Left), Icons));
        Assert.AreEqual(AppCommand.IconRight, Resolve(K(ShortcutKey.Right), Icons));
        Assert.AreEqual(AppCommand.IconUp, Resolve(K(ShortcutKey.Up), Icons));
        Assert.AreEqual(AppCommand.IconDown, Resolve(K(ShortcutKey.Down), Icons));
        Assert.AreEqual(AppCommand.OpenIcon, Resolve(K(ShortcutKey.Enter), Icons));
    }

    [Test]
    public void TheIcons_WithNoneSelected_ArrowsStillSelect_EnterDoesNothing()
    {
        var none = new ShortcutContext(frameOpen: true, desktopFocused: true);
        Assert.AreEqual(AppCommand.IconDown, Resolve(K(ShortcutKey.Down), none));
        Assert.IsNull(Resolve(K(ShortcutKey.Enter), none));
    }

    [Test]
    public void TheIcons_WaitWhileAWindowHasTheFocus()
    {
        Assert.IsNull(Resolve(K(ShortcutKey.Down), App));
        Assert.IsNull(Resolve(K(ShortcutKey.Enter), App));
    }

    [Test]
    public void CtrlDigits_ShowTheTabAtThatPosition()
    {
        ShortcutKey[] digits = { ShortcutKey.Digit1, ShortcutKey.Digit2, ShortcutKey.Digit3, ShortcutKey.Digit4, ShortcutKey.Digit5, ShortcutKey.Digit6 };
        AppCommand[] tabs = { AppCommand.Tab1, AppCommand.Tab2, AppCommand.Tab3, AppCommand.Tab4, AppCommand.Tab5, AppCommand.Tab6 };
        for (int i = 0; i < digits.Length; i++)
        {
            Assert.AreEqual(tabs[i], Resolve(K(digits[i], ctrl: true), App));
            Assert.AreEqual(i + 1, ShortcutMap.TabPosition(tabs[i]));
        }
        Assert.AreEqual(0, ShortcutMap.TabPosition(AppCommand.Pin));
    }

    [Test]
    public void TheAppsChords()
    {
        Assert.AreEqual(AppCommand.NextTab, Resolve(K(ShortcutKey.Tab, ctrl: true), App));
        Assert.AreEqual(AppCommand.PrevTab, Resolve(K(ShortcutKey.Tab, ctrl: true, shift: true), App));
        Assert.AreEqual(AppCommand.MoveTabLeft, Resolve(K(ShortcutKey.PageUp, ctrl: true, shift: true), App));
        Assert.AreEqual(AppCommand.MoveTabRight, Resolve(K(ShortcutKey.PageDown, ctrl: true, shift: true), App));
        Assert.AreEqual(AppCommand.OtherPane, Resolve(K(ShortcutKey.F6), App));
        Assert.AreEqual(AppCommand.OtherPane, Resolve(K(ShortcutKey.F6, shift: true), App));
        Assert.AreEqual(AppCommand.ToggleSplit, Resolve(K(ShortcutKey.Backslash, ctrl: true), App));
        Assert.AreEqual(AppCommand.ToggleSidebar, Resolve(K(ShortcutKey.B, ctrl: true), App));
        Assert.AreEqual(AppCommand.ToggleSteps, Resolve(K(ShortcutKey.S, ctrl: true, shift: true), App));
        Assert.AreEqual(AppCommand.Back, Resolve(K(ShortcutKey.Left, alt: true), App));
        Assert.AreEqual(AppCommand.Forward, Resolve(K(ShortcutKey.Right, alt: true), App));
        Assert.AreEqual(AppCommand.NextRegion, Resolve(K(ShortcutKey.Tab), App));
        Assert.AreEqual(AppCommand.PrevRegion, Resolve(K(ShortcutKey.Tab, shift: true), App));
        Assert.AreEqual(AppCommand.Pin, Resolve(K(ShortcutKey.P, ctrl: true), App));
        Assert.AreEqual(AppCommand.ZoomIn, Resolve(K(ShortcutKey.Equals, ctrl: true), App));
        Assert.AreEqual(AppCommand.ZoomOut, Resolve(K(ShortcutKey.Minus, ctrl: true), App));
        Assert.AreEqual(AppCommand.ZoomReset, Resolve(K(ShortcutKey.Digit0, ctrl: true), App));
    }

    [Test]
    public void TheAppsChords_NeedTheApp()
    {
        foreach (KeyChord chord in new[] { K(ShortcutKey.Digit1, ctrl: true), K(ShortcutKey.Tab), K(ShortcutKey.B, ctrl: true), K(ShortcutKey.P, ctrl: true), K(ShortcutKey.Equals, ctrl: true) })
            Assert.IsNull(Resolve(chord, Icons), chord.ToString());
    }

    [Test]
    public void AListFocused_TheRowKeys()
    {
        Assert.AreEqual(AppCommand.RowUp, Resolve(K(ShortcutKey.Up), AppList));
        Assert.AreEqual(AppCommand.RowDown, Resolve(K(ShortcutKey.Down), AppList));
        Assert.AreEqual(AppCommand.RowUp, Resolve(K(ShortcutKey.Left), AppList));
        Assert.AreEqual(AppCommand.RowDown, Resolve(K(ShortcutKey.Right), AppList));
        Assert.AreEqual(AppCommand.RowFirst, Resolve(K(ShortcutKey.Home), AppList));
        Assert.AreEqual(AppCommand.RowLast, Resolve(K(ShortcutKey.End), AppList));
        Assert.AreEqual(AppCommand.PageUp, Resolve(K(ShortcutKey.PageUp), AppList));
        Assert.AreEqual(AppCommand.PageDown, Resolve(K(ShortcutKey.PageDown), AppList));
        Assert.AreEqual(AppCommand.Follow, Resolve(K(ShortcutKey.Enter), AppList));
        Assert.AreEqual(AppCommand.FollowOther, Resolve(K(ShortcutKey.Enter, ctrl: true), AppList));
        Assert.AreEqual(AppCommand.Pick, Resolve(K(ShortcutKey.Space), AppList));
        Assert.AreEqual(AppCommand.Copy, Resolve(K(ShortcutKey.C, ctrl: true), AppList));
        Assert.AreEqual(AppCommand.CopyRow, Resolve(K(ShortcutKey.C, ctrl: true, shift: true), AppList));
    }

    [Test]
    public void TheRowKeys_NeedAListFocused()
    {
        foreach (KeyChord chord in new[] { K(ShortcutKey.Up), K(ShortcutKey.Home), K(ShortcutKey.Space), K(ShortcutKey.Enter), K(ShortcutKey.C, ctrl: true) })
            Assert.IsNull(Resolve(chord, App), chord.ToString());
    }

    [Test]
    public void TheTabStripFocused_LeftAndRightSwitchTabs()
    {
        Assert.AreEqual(AppCommand.PrevTab, Resolve(K(ShortcutKey.Left), AppTabs));
        Assert.AreEqual(AppCommand.NextTab, Resolve(K(ShortcutKey.Right), AppTabs));
        Assert.IsNull(Resolve(K(ShortcutKey.Space), AppTabs));
    }

    [Test]
    public void AltArrows_AreHistory_EvenInAList() =>
        Assert.AreEqual(AppCommand.Back, Resolve(K(ShortcutKey.Left, alt: true), AppList));

    [Test]
    public void NotesFocused_CtrlVAddsTheClip()
    {
        var notes = new ShortcutContext(frameOpen: true, notesFocused: true);
        Assert.AreEqual(AppCommand.Paste, Resolve(K(ShortcutKey.V, ctrl: true), notes));
        Assert.IsNull(Resolve(K(ShortcutKey.V, ctrl: true), App));
    }

    [Test]
    public void ATextField_OnlyTheChordsThatCannotBeTypingPass()
    {
        var field = new ShortcutContext(frameOpen: true, appFocused: true, textFieldFocused: true, listFocused: true, notesFocused: true);
        Assert.AreEqual(AppCommand.FocusSearch, Resolve(K(ShortcutKey.F, ctrl: true), field));
        Assert.AreEqual(AppCommand.Tab3, Resolve(K(ShortcutKey.Digit3, ctrl: true), field));
        Assert.AreEqual(AppCommand.OtherPane, Resolve(K(ShortcutKey.F6), field));
        Assert.AreEqual(AppCommand.ToggleSplit, Resolve(K(ShortcutKey.Backslash, ctrl: true), field));
        Assert.AreEqual(AppCommand.ToggleSidebar, Resolve(K(ShortcutKey.B, ctrl: true), field));
        Assert.AreEqual(AppCommand.Escape, Resolve(K(ShortcutKey.Escape), field));
        Assert.AreEqual(AppCommand.Help, Resolve(K(ShortcutKey.F1), field));

        // The field keeps its own Ctrl+C/V, the arrows, Home, End, Space, Enter and Tab.
        foreach (KeyChord typing in new[] { K(ShortcutKey.C, ctrl: true), K(ShortcutKey.V, ctrl: true), K(ShortcutKey.Up), K(ShortcutKey.Home), K(ShortcutKey.Space),
                                            K(ShortcutKey.Enter), K(ShortcutKey.Tab), K(ShortcutKey.P, ctrl: true), K(ShortcutKey.Equals, ctrl: true), K(ShortcutKey.Left, alt: true) })
            Assert.IsNull(Resolve(typing, field), typing.ToString());
    }

    [Test]
    public void TheSearchField_TabAndShiftTabLeaveItForTheNextRegion()
    {
        var search = new ShortcutContext(frameOpen: true, appFocused: true, textFieldFocused: true, searchFocused: true);
        Assert.AreEqual(AppCommand.NextRegion, Resolve(K(ShortcutKey.Tab), search));
        Assert.AreEqual(AppCommand.PrevRegion, Resolve(K(ShortcutKey.Tab, shift: true), search));
        Assert.IsNull(Resolve(K(ShortcutKey.V, ctrl: true), search), "the field pastes itself");
        Assert.IsNull(Resolve(K(ShortcutKey.Tab), new ShortcutContext(frameOpen: true, appFocused: true, textFieldFocused: true)), "another field keeps Tab");
    }

    [Test]
    public void AMenuOpen_OnlyEscapePasses()
    {
        var menu = new ShortcutContext(frameOpen: true, appFocused: true, listFocused: true, menuOpen: true);
        Assert.AreEqual(AppCommand.Escape, Resolve(K(ShortcutKey.Escape), menu));
        Assert.IsNull(Resolve(K(ShortcutKey.F1), menu));
        Assert.IsNull(Resolve(K(ShortcutKey.Down), menu));
        Assert.IsNull(Resolve(K(ShortcutKey.F, ctrl: true), menu));
    }

    [Test]
    public void TheFrameClosed_NothingResolves()
    {
        var closed = new ShortcutContext(appFocused: true, listFocused: true, desktopFocused: true, iconSelected: true, notesFocused: true);
        foreach (ShortcutKey key in Enum.GetValues(typeof(ShortcutKey)))
            foreach (bool ctrl in new[] { false, true })
                foreach (bool shift in new[] { false, true })
                    Assert.IsNull(Resolve(K(key, ctrl, shift), closed), key.ToString());
    }

    [Test]
    public void UnknownChords_ResolveToNothing()
    {
        Assert.IsNull(Resolve(K(ShortcutKey.F), App));
        Assert.IsNull(Resolve(K(ShortcutKey.B), App));
        Assert.IsNull(Resolve(K(ShortcutKey.F1, ctrl: true), App));
        Assert.IsNull(Resolve(K(ShortcutKey.Escape, ctrl: true), App));
    }

    [Test]
    public void TheCard_ListsEveryCommandOnce()
    {
        var seen = new List<AppCommand>();
        foreach (ShortcutCardRow row in ShortcutMap.Card)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(row.Keys));
            Assert.IsFalse(string.IsNullOrWhiteSpace(row.TextKey));
            seen.AddRange(row.Commands);
        }
        CollectionAssert.AreEquivalent(Enum.GetValues(typeof(AppCommand)).Cast<AppCommand>().ToList(), seen);
    }

    [Test]
    public void TheCardsTexts_AreTodaysUiStrings()
    {
        HashSet<string> keys = TodaysUiKeys();
        foreach (ShortcutCardRow row in ShortcutMap.Card)
            Assert.IsTrue(keys.Contains(row.TextKey), row.TextKey);
    }

    [Test]
    public void EveryCommandOnTheCard_Resolves()
    {
        ShortcutContext[] contexts =
        {
            App, AppList, AppTabs, Icons, new ShortcutContext(frameOpen: true, notesFocused: true)
        };
        var reached = new HashSet<AppCommand>();
        foreach (ShortcutKey key in Enum.GetValues(typeof(ShortcutKey)))
            for (int mods = 0; mods < 8; mods++)
                foreach (ShortcutContext c in contexts)
                    if (ShortcutMap.Resolve(K(key, (mods & 1) != 0, (mods & 2) != 0, (mods & 4) != 0), c, out AppCommand command))
                        reached.Add(command);
        CollectionAssert.AreEquivalent(Enum.GetValues(typeof(AppCommand)).Cast<AppCommand>().ToList(), reached);
    }

    /// <summary>The keys of today's ui strings (world_source.json).</summary>
    private static HashSet<string> TodaysUiKeys([CallerFilePath] string here = "")
    {
        const string source = "Assets/Data/World/world_source.json";
        string path = File.Exists(source) ? source : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", source);
        return new HashSet<string>(ContentJson.Parse(File.ReadAllText(path)).Get("ui").Get("strings").Items.Select(e => e.Get("key").Text));
    }
}
