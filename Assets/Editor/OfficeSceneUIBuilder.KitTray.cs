using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// The office builder's tray and chrome skin: the kit piece a themed chrome
/// panel takes by its role (the taskbar, the Menu button, the tray's phosphor
/// glass, a window's title bar and body, the menus, tooltips, toasts, badges,
/// fields). RolePiece (OfficeSceneUIBuilder.Kit.cs) hands every role it does
/// not skin to ChromePiece.
/// Part of <see cref="OfficeSceneUIBuilder"/>; carved out of OfficeSceneUIBuilder.Kit.cs by P0-A (RolePiece's chrome cases, moved verbatim). Owner: UI-PC.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The kit piece a themed chrome panel takes by its role (RolePiece's chrome half), or null to keep its look.</summary>
    private static string ChromePiece(ThemeTag tag, Image image)
    {
        switch (tag.Role)
        {
            case ThemeRoleId.Taskbar:
                return image.name == "Taskbar" ? "taskbar" : null;
            case ThemeRoleId.StartButton:
                return "plate_ox";
            case ThemeRoleId.Tray:
                return image.name == "Tray" ? "lcd_glass" : null;
            case ThemeRoleId.TitleBar:
                return image.GetComponent<WindowDrag>() != null ? "titlebar_slate" : null;
            case ThemeRoleId.WindowBody:
                return image.GetComponent<DesktopWindow>() != null ? "panel_bone" : null;
            case ThemeRoleId.StartMenu:
                return "dropdown";
            case ThemeRoleId.Tooltip:
            case ThemeRoleId.CompareBar:
                return "tooltip";
            case ThemeRoleId.ScreenStrip:
                return image.name == "IdleScreen" ? "strip_slate" : null;
            case ThemeRoleId.Toast:
                return image.GetComponent<Button>() == null ? "panel_dark" : null;
            case ThemeRoleId.Badge:
                return "badge_red";
            case ThemeRoleId.InputField:
                return image.GetComponent<TMP_InputField>() != null ? "field" : null;
            default:
                return null;
        }
    }
}
