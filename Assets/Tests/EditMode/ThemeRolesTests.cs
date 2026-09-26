using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The rule that keeps theming off evidence: exactly the diegetic roles are
/// skipped. The rule is an explicit list (PC spec TH2, audit R2-005), so a
/// chrome role appended after the diegetic ones stays chrome: every enum
/// value is checked here, and a new one must be added to one of the two sets.
/// Also the retired role (TH2: DeskDim keeps its int but takes no graphic;
/// StickyNote stays live for Notes' clipping cards).
/// </summary>
public class ThemeRolesTests
{
    private static readonly HashSet<ThemeRoleId> Diegetic = new HashSet<ThemeRoleId>
    {
        ThemeRoleId.DiegeticPaper,
        ThemeRoleId.DiegeticPhoto,
        ThemeRoleId.DiegeticRow,
        ThemeRoleId.DiegeticLabel,
        ThemeRoleId.DiegeticNote,
        ThemeRoleId.DiegeticBacking,
        ThemeRoleId.DiegeticBookRow,
        ThemeRoleId.DiegeticBubble,
        ThemeRoleId.DiegeticDevice,
        ThemeRoleId.DiegeticForm,
        ThemeRoleId.SiteContent
    };

    private static readonly HashSet<ThemeRoleId> Chrome = new HashSet<ThemeRoleId>
    {
        ThemeRoleId.Desktop, ThemeRoleId.ScreenStrip, ThemeRoleId.Taskbar, ThemeRoleId.TaskbarGloss, ThemeRoleId.StartButton,
        ThemeRoleId.Tray, ThemeRoleId.WindowBody, ThemeRoleId.TitleBar, ThemeRoleId.TitleGloss, ThemeRoleId.Button,
        ThemeRoleId.CloseButton, ThemeRoleId.AcceptButton, ThemeRoleId.DenyButton, ThemeRoleId.WheelButton, ThemeRoleId.SearchButton,
        ThemeRoleId.DesktopIcon, ThemeRoleId.DeskButton, ThemeRoleId.Panel, ThemeRoleId.ClaimStrip, ThemeRoleId.Alert,
        ThemeRoleId.StickyNote, ThemeRoleId.CompareBar, ThemeRoleId.CompareMatch, ThemeRoleId.CompareMismatch, ThemeRoleId.CompareNeutral,
        ThemeRoleId.SelectionHighlight, ThemeRoleId.StartMenu, ThemeRoleId.MenuEntry, ThemeRoleId.QuitEntry, ThemeRoleId.NewsletterBorder,
        ThemeRoleId.Newsletter, ThemeRoleId.NewsletterButton, ThemeRoleId.DeskDim, ThemeRoleId.InputField, ThemeRoleId.InputPlaceholder,
        ThemeRoleId.Tooltip, ThemeRoleId.ClickCatcher,
        ThemeRoleId.TabStrip, ThemeRoleId.Tab, ThemeRoleId.TabActive, ThemeRoleId.Sidebar, ThemeRoleId.SearchResults,
        ThemeRoleId.Badge, ThemeRoleId.Toast, ThemeRoleId.FocusRing, ThemeRoleId.IconSelection
    };

    [Test]
    public void EveryRole_IsInExactlyOneSet_AndIsDiegeticFollowsIt()
    {
        foreach (ThemeRoleId role in Enum.GetValues(typeof(ThemeRoleId)))
        {
            Assert.AreNotEqual(Diegetic.Contains(role), Chrome.Contains(role), $"{role} must be listed here as chrome or diegetic");
            Assert.AreEqual(Diegetic.Contains(role), ThemeRoles.IsDiegetic(role), role.ToString());
        }
    }

    [Test]
    public void AValueOutsideTheEnum_IsChrome()
    {
        Assert.IsFalse(ThemeRoles.IsDiegetic((ThemeRoleId)999), "an appended role is chrome until it is listed");
    }

    /// <summary>Every role in its serialized order (ThemeTag stores the int): append only.</summary>
    private static readonly ThemeRoleId[] InSerializedOrder =
    {
        ThemeRoleId.Desktop, ThemeRoleId.ScreenStrip, ThemeRoleId.Taskbar, ThemeRoleId.TaskbarGloss, ThemeRoleId.StartButton,
        ThemeRoleId.Tray, ThemeRoleId.WindowBody, ThemeRoleId.TitleBar, ThemeRoleId.TitleGloss, ThemeRoleId.Button,
        ThemeRoleId.CloseButton, ThemeRoleId.AcceptButton, ThemeRoleId.DenyButton, ThemeRoleId.WheelButton, ThemeRoleId.SearchButton,
        ThemeRoleId.DesktopIcon, ThemeRoleId.DeskButton, ThemeRoleId.Panel, ThemeRoleId.ClaimStrip, ThemeRoleId.Alert,
        ThemeRoleId.StickyNote, ThemeRoleId.CompareBar, ThemeRoleId.CompareMatch, ThemeRoleId.CompareMismatch, ThemeRoleId.CompareNeutral,
        ThemeRoleId.SelectionHighlight, ThemeRoleId.StartMenu, ThemeRoleId.MenuEntry, ThemeRoleId.QuitEntry, ThemeRoleId.NewsletterBorder,
        ThemeRoleId.Newsletter, ThemeRoleId.NewsletterButton, ThemeRoleId.DeskDim, ThemeRoleId.InputField, ThemeRoleId.InputPlaceholder,
        ThemeRoleId.Tooltip, ThemeRoleId.ClickCatcher, ThemeRoleId.DiegeticPaper, ThemeRoleId.DiegeticPhoto, ThemeRoleId.DiegeticRow,
        ThemeRoleId.DiegeticLabel, ThemeRoleId.DiegeticNote, ThemeRoleId.DiegeticBacking, ThemeRoleId.DiegeticBookRow, ThemeRoleId.DiegeticBubble,
        ThemeRoleId.DiegeticDevice, ThemeRoleId.TabStrip, ThemeRoleId.Tab, ThemeRoleId.TabActive, ThemeRoleId.Sidebar,
        ThemeRoleId.SearchResults, ThemeRoleId.Badge, ThemeRoleId.Toast, ThemeRoleId.FocusRing, ThemeRoleId.IconSelection,
        ThemeRoleId.DiegeticForm, ThemeRoleId.SiteContent
    };

    /// <summary>Audit R1-003 for this enum: ThemeTag serializes the role as an int, so inserting or reordering a role would silently remap every tag in OfficeGameplay. Desktop = 0 .. DiegeticDevice = 45, the PC redesign's appended roles 46 .. 56.</summary>
    [Test]
    public void EveryRole_KeepsItsSerializedInt()
    {
        for (int i = 0; i < InSerializedOrder.Length; i++)
            Assert.AreEqual(i, (int)InSerializedOrder[i], InSerializedOrder[i].ToString());
        Assert.AreEqual(37, (int)ThemeRoleId.DiegeticPaper);
        Assert.AreEqual(45, (int)ThemeRoleId.DiegeticDevice);
        Assert.AreEqual(54, (int)ThemeRoleId.IconSelection);
        Assert.AreEqual(56, (int)ThemeRoleId.SiteContent);
        Assert.AreEqual(InSerializedOrder.Length, Enum.GetValues(typeof(ThemeRoleId)).Length, "a new role is appended here too");
    }

    [Test]
    public void OnlyDeskDim_IsRetired()
    {
        CollectionAssert.AreEquivalent(new[] { ThemeRoleId.DeskDim },
                                       ((ThemeRoleId[])Enum.GetValues(typeof(ThemeRoleId))).Where(ThemeRoles.IsRetired).ToArray());
    }
}
