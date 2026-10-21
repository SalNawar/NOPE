using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Settings window's PC basics (BUG-62, CODE-104; the feel bible's F3:
/// "Settings has volume, Camera sway, Reduced Motion, Motion intensity,
/// Reduce flashing"): a Sound group (Master, Music and Effects volume
/// sliders) and a Display group (Windowed / Borderless / Fullscreen, the
/// resolution stepped with ‹ › (glyphs every kit face draws), VSync Off / On, the brightness slider and
/// Reduce flashing Off / On), in the Settings window's own row, slider and
/// choice styles, wired to SettingsWindowController. The Settings window
/// itself is UI-PC's (.Apps.cs BuildSettingsWindow, its size DesktopConfigSO);
/// it calls <see cref="BuildSettingsBasics"/> with the window's
/// SettingsWindowController and the column the rows go in, before the note,
/// and gives the window room (a scroll or a second column: the column may sit
/// under a viewport, since the controller is passed in, never looked up).
/// Part of <see cref="OfficeSceneUIBuilder"/>. Owner: TEXT-LANG.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The volume rows' names and words, in SoundChannel order.</summary>
    private static readonly (string row, string key)[] VolumeRows =
    {
        ("MasterVolumeRow", "settings.volumeMaster"), ("MusicVolumeRow", "settings.volumeMusic"), ("EffectsVolumeRow", "settings.volumeEffects")
    };

    /// <summary>The window mode choices' names and words, in WindowMode order.</summary>
    private static readonly (string button, string key)[] WindowModeChoices =
    {
        ("WindowedButton", "settings.windowed"), ("BorderlessButton", "settings.borderless"), ("FullscreenButton", "settings.fullscreen")
    };

    /// <summary>
    /// Adds the Sound and Display groups to the Settings window's
    /// <paramref name="column"/> (its rows rebuilt fresh with the window) and
    /// wires them to <paramref name="controller"/>, the window's one
    /// SettingsWindowController (BUG-62). The controller is the caller's (UI-PC
    /// passes the one on the window), so a column moved under a scroll
    /// viewport or split in two never gets a second controller. Throws when
    /// either is missing, so a wrong wiring fails the build loudly.
    /// </summary>
    private static void BuildSettingsBasics(SettingsWindowController controller, Transform column)
    {
        if (controller == null)
            throw new System.ArgumentNullException(nameof(controller), "BuildSettingsBasics needs the Settings window's SettingsWindowController (UI-PC: pass GetOrAdd<SettingsWindowController>(win.gameObject)).");
        if (column == null)
            throw new System.ArgumentNullException(nameof(column), "BuildSettingsBasics needs the Settings column the rows go in.");

        SettingsHeading(column, "SoundLabel", "settings.sound");
        var volumes = new List<Object>();
        var volumeValues = new List<Object>();
        foreach ((string row, string key) in VolumeRows)
        {
            Slider slider = SettingsSliderRow(column, row, key, out TMP_Text value);
            volumes.Add(slider);
            volumeValues.Add(value);
        }

        SettingsHeading(column, "DisplayLabel", "settings.display");
        Transform modes = SettingsRow(column, "WindowModeRow");
        var modeButtons = new List<Object>();
        foreach ((string button, string key) in WindowModeChoices)
            modeButtons.Add(SettingsChoice(modes, button, key));

        Transform resolution = SettingsRow(column, "ResolutionRow");
        SettingsRowLabel(resolution, "ResolutionLabel", "settings.resolution");
        Button down = SettingsChoice(resolution, "ResolutionDownButton", "settings.resolutionDown");
        TMP_Text size = SettingsRowValue(resolution, "ResolutionValue", TextAlignmentOptions.Center, 1f);
        Button up = SettingsChoice(resolution, "ResolutionUpButton", "settings.resolutionUp");

        Transform vsync = SettingsRow(column, "VsyncRow");
        SettingsRowLabel(vsync, "VsyncLabel", "settings.vsync");
        Button vsyncOff = SettingsChoice(vsync, "VsyncOffButton", "settings.off");
        Button vsyncOn = SettingsChoice(vsync, "VsyncOnButton", "settings.on");

        Slider brightness = SettingsSliderRow(column, "BrightnessRow", "settings.brightness", out TMP_Text brightnessValue);

        Transform flashing = SettingsRow(column, "ReduceFlashingRow");
        SettingsRowLabel(flashing, "ReduceFlashingLabel", "settings.reduceFlashing");
        Button flashOff = SettingsChoice(flashing, "ReduceFlashingOffButton", "settings.off");
        Button flashOn = SettingsChoice(flashing, "ReduceFlashingOnButton", "settings.on");

        var so = new SerializedObject(controller);
        SerializedArrays.Set(so, "volumeSliders", volumes);
        SerializedArrays.Set(so, "volumeTexts", volumeValues);
        SerializedArrays.Set(so, "windowModeButtons", modeButtons);
        SetRef(so, "resolutionDownButton", down);
        SetRef(so, "resolutionUpButton", up);
        SetRef(so, "resolutionText", size);
        SetRef(so, "vsyncOffButton", vsyncOff);
        SetRef(so, "vsyncOnButton", vsyncOn);
        SetRef(so, "brightnessSlider", brightness);
        SetRef(so, "brightnessText", brightnessValue);
        SetRef(so, "reduceFlashingOffButton", flashOff);
        SetRef(so, "reduceFlashingOnButton", flashOn);
        so.ApplyModifiedProperties();
    }

    /// <summary>A Settings row of a keyed word, a slider (0 to 100) and its value ("80 %"), as the Motion intensity row; returns the slider.</summary>
    private static Slider SettingsSliderRow(Transform column, string name, string key, out TMP_Text value)
    {
        Transform row = SettingsRow(column, name);
        SettingsRowLabel(row, name.Replace("Row", "Label"), key);
        Slider slider = SettingsSlider(row, name.Replace("Row", "Slider"));
        value = SettingsRowValue(row, name.Replace("Row", "Value"), TextAlignmentOptions.MidlineRight, 0.5f);
        return slider;
    }

    /// <summary>A Settings row's keyed word on its left, at Body size, taking the room the choices leave.</summary>
    private static void SettingsRowLabel(Transform row, string name, string key)
    {
        TMP_Text label = Text(row, name, null, PcType.Body, TextAlignmentOptions.MidlineLeft, Vector2.zero, Vector2.one, Ink, ThemeRoleId.WindowBody, key);
        Chrome(label, PcType.Body);
        label.raycastTarget = false;
        GetOrAdd<LayoutElement>(label.gameObject).flexibleWidth = 1f;
    }

    /// <summary>A Settings row's value text (written at run time by SettingsWindowController), at Body size, <paramref name="share"/> of a word's width.</summary>
    private static TMP_Text SettingsRowValue(Transform row, string name, TextAlignmentOptions align, float share)
    {
        TMP_Text value = Text(row, name, null, PcType.Body, align, Vector2.zero, Vector2.one, Ink, ThemeRoleId.WindowBody);
        Chrome(value, PcType.Body);
        value.raycastTarget = false;
        GetOrAdd<LayoutElement>(value.gameObject).flexibleWidth = share;
        return value;
    }
}
