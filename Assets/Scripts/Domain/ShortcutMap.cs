using System.Collections.Generic;

/// <summary>What a desktop shortcut does (the PC redesign KB1, section 3.4). Not serialized.</summary>
public enum AppCommand
{
    /// <summary>Ctrl+F: opens or restores the Investigation app and focuses its search field.</summary>
    FocusSearch,
    /// <summary>Ctrl+1: the tab at position 1.</summary>
    Tab1,
    /// <summary>Ctrl+2: the tab at position 2.</summary>
    Tab2,
    /// <summary>Ctrl+3: the tab at position 3.</summary>
    Tab3,
    /// <summary>Ctrl+4: the tab at position 4.</summary>
    Tab4,
    /// <summary>Ctrl+5: the tab at position 5.</summary>
    Tab5,
    /// <summary>Ctrl+6: the tab at position 6.</summary>
    Tab6,
    /// <summary>Ctrl+Tab (and → on the tab strip): the next tab.</summary>
    NextTab,
    /// <summary>Ctrl+Shift+Tab (and ← on the tab strip): the previous tab.</summary>
    PrevTab,
    /// <summary>Ctrl+Shift+PgUp: moves the active tab left.</summary>
    MoveTabLeft,
    /// <summary>Ctrl+Shift+PgDn: moves the active tab right.</summary>
    MoveTabRight,
    /// <summary>F6, Shift+F6: the other pane becomes active.</summary>
    OtherPane,
    /// <summary>Ctrl+\: the split on or off.</summary>
    ToggleSplit,
    /// <summary>Ctrl+B: the sidebar on or off.</summary>
    ToggleSidebar,
    /// <summary>Ctrl+Shift+S: the steps on or off.</summary>
    ToggleSteps,
    /// <summary>Alt+←: back in the active pane.</summary>
    Back,
    /// <summary>Alt+→: forward in the active pane.</summary>
    Forward,
    /// <summary>Tab: the next region.</summary>
    NextRegion,
    /// <summary>Shift+Tab: the previous region.</summary>
    PrevRegion,
    /// <summary>Enter on a focused item: follows its link, or opens it (a chip, a pin, a button).</summary>
    Follow,
    /// <summary>Ctrl+Enter on a focused item: the same, in the other pane.</summary>
    FollowOther,
    /// <summary>Space on a focused row: picks it for compare.</summary>
    Pick,
    /// <summary>Ctrl+C on a focused row: copies its value as shown.</summary>
    Copy,
    /// <summary>Ctrl+Shift+C on a focused row: copies "Label: value".</summary>
    CopyRow,
    /// <summary>Ctrl+V with Notes focused and no field focused: adds the clip as a clipping.</summary>
    Paste,
    /// <summary>Ctrl+P: pins or unpins the focused row, or the pane's item.</summary>
    Pin,
    /// <summary>Ctrl+=: zoom in.</summary>
    ZoomIn,
    /// <summary>Ctrl+-: zoom out.</summary>
    ZoomOut,
    /// <summary>Ctrl+0: zoom back to the Settings default.</summary>
    ZoomReset,
    /// <summary>F1: shows or hides the shortcut card.</summary>
    Help,
    /// <summary>Esc: the Escape chain (DesktopEscapeRule), then the frame.</summary>
    Escape,
    /// <summary>Enter on the desktop: opens the selected icon.</summary>
    OpenIcon,
    /// <summary>← on the desktop: the nearest icon to the left.</summary>
    IconLeft,
    /// <summary>→ on the desktop: the nearest icon to the right.</summary>
    IconRight,
    /// <summary>↑ on the desktop: the nearest icon above.</summary>
    IconUp,
    /// <summary>↓ on the desktop: the nearest icon below.</summary>
    IconDown,
    /// <summary>↑ (or ←) in a focused list: the previous item.</summary>
    RowUp,
    /// <summary>↓ (or →) in a focused list: the next item.</summary>
    RowDown,
    /// <summary>Home in a focused list: the first item.</summary>
    RowFirst,
    /// <summary>End in a focused list: the last item.</summary>
    RowLast,
    /// <summary>PgUp in a focused list: a page up.</summary>
    PageUp,
    /// <summary>PgDn in a focused list: a page down.</summary>
    PageDown
}

/// <summary>The keys the desktop's shortcuts use (DesktopKeyboard maps the Input System's keys onto these; the numpad's Enter, plus and minus count as Enter, = and -).</summary>
public enum ShortcutKey
{
    /// <summary>F.</summary>
    F,
    /// <summary>1.</summary>
    Digit1,
    /// <summary>2.</summary>
    Digit2,
    /// <summary>3.</summary>
    Digit3,
    /// <summary>4.</summary>
    Digit4,
    /// <summary>5.</summary>
    Digit5,
    /// <summary>6.</summary>
    Digit6,
    /// <summary>Tab.</summary>
    Tab,
    /// <summary>Page Up.</summary>
    PageUp,
    /// <summary>Page Down.</summary>
    PageDown,
    /// <summary>F6.</summary>
    F6,
    /// <summary>\.</summary>
    Backslash,
    /// <summary>B.</summary>
    B,
    /// <summary>S.</summary>
    S,
    /// <summary>←.</summary>
    Left,
    /// <summary>→.</summary>
    Right,
    /// <summary>↑.</summary>
    Up,
    /// <summary>↓.</summary>
    Down,
    /// <summary>Home.</summary>
    Home,
    /// <summary>End.</summary>
    End,
    /// <summary>Enter.</summary>
    Enter,
    /// <summary>Space.</summary>
    Space,
    /// <summary>C.</summary>
    C,
    /// <summary>V.</summary>
    V,
    /// <summary>P.</summary>
    P,
    /// <summary>= (zoom in).</summary>
    Equals,
    /// <summary>- (zoom out).</summary>
    Minus,
    /// <summary>0.</summary>
    Digit0,
    /// <summary>F1.</summary>
    F1,
    /// <summary>Esc.</summary>
    Escape
}

/// <summary>One key press with its modifiers.</summary>
public readonly struct KeyChord
{
    /// <summary>The key pressed.</summary>
    public readonly ShortcutKey Key;

    /// <summary>Ctrl is held.</summary>
    public readonly bool Ctrl;

    /// <summary>Shift is held.</summary>
    public readonly bool Shift;

    /// <summary>Alt is held.</summary>
    public readonly bool Alt;

    /// <summary>A chord.</summary>
    public KeyChord(ShortcutKey key, bool ctrl = false, bool shift = false, bool alt = false)
    {
        Key = key;
        Ctrl = ctrl;
        Shift = shift;
        Alt = alt;
    }

    /// <summary>"Ctrl+Shift+Tab" (for test messages).</summary>
    public override string ToString() => (Ctrl ? "Ctrl+" : "") + (Shift ? "Shift+" : "") + (Alt ? "Alt+" : "") + Key;
}

/// <summary>What has the keyboard on the desktop (the PC redesign KB2): the frame, the focused window, a text field, an open menu, the app's focus ring.</summary>
public readonly struct ShortcutContext
{
    /// <summary>The PC frame is open and the desktop takes input (BoothRules.DesktopInteractive): without it nothing resolves.</summary>
    public readonly bool FrameOpen;

    /// <summary>No window has the focus: the desktop's icons take the arrows and Enter.</summary>
    public readonly bool DesktopFocused;

    /// <summary>An icon is selected (Enter opens it).</summary>
    public readonly bool IconSelected;

    /// <summary>The Investigation app's window has the focus.</summary>
    public readonly bool AppFocused;

    /// <summary>The Notes window has the focus.</summary>
    public readonly bool NotesFocused;

    /// <summary>A text field has the keyboard: only the chords that cannot be typing pass.</summary>
    public readonly bool TextFieldFocused;

    /// <summary>The app's search field has the keyboard: Tab and Shift+Tab pass too (they leave it for the next region, KB4).</summary>
    public readonly bool SearchFocused;

    /// <summary>A context menu or the Start menu is open: only Escape passes.</summary>
    public readonly bool MenuOpen;

    /// <summary>The app's focus ring is on an item of a list (a row, a chip, a pin, the decision's buttons).</summary>
    public readonly bool ListFocused;

    /// <summary>The app's focus ring is on its tab strip.</summary>
    public readonly bool TabStripFocused;

    /// <summary>A context.</summary>
    public ShortcutContext(bool frameOpen = false, bool desktopFocused = false, bool iconSelected = false, bool appFocused = false, bool notesFocused = false,
                           bool textFieldFocused = false, bool searchFocused = false, bool menuOpen = false, bool listFocused = false,
                           bool tabStripFocused = false)
    {
        FrameOpen = frameOpen;
        DesktopFocused = desktopFocused;
        IconSelected = iconSelected;
        AppFocused = appFocused;
        NotesFocused = notesFocused;
        TextFieldFocused = textFieldFocused;
        SearchFocused = searchFocused;
        MenuOpen = menuOpen;
        ListFocused = listFocused;
        TabStripFocused = tabStripFocused;
    }
}

/// <summary>One row of the F1 card: the keys as printed, the commands they give and the ui string saying what they do.</summary>
public sealed class ShortcutCardRow
{
    /// <summary>A row.</summary>
    public ShortcutCardRow(string keys, string textKey, params AppCommand[] commands)
    {
        Keys = keys;
        TextKey = textKey;
        Commands = commands;
    }

    /// <summary>The keys as printed on the card ("Ctrl+1 … Ctrl+6").</summary>
    public string Keys { get; }

    /// <summary>The ui string key of what they do ("keys.tabs").</summary>
    public string TextKey { get; }

    /// <summary>The commands the row stands for.</summary>
    public IReadOnlyList<AppCommand> Commands { get; }
}

/// <summary>
/// The desktop's one shortcut table (the PC redesign KB1, KB2, section 3.4;
/// the R5-005 lesson: one tested map instead of keys scattered over
/// components). Resolve turns a chord in a context into a command: nothing
/// while the frame is closed; only Escape while a menu is open; while a text
/// field has the keyboard only the chords that cannot be typing pass (Ctrl+F,
/// Ctrl+1…6, F6, Ctrl+\, Ctrl+B, Esc, F1; the field keeps its own
/// Ctrl+C/V/X/A, arrows and Enter), and in the app's search field Tab and
/// Shift+Tab (the next region: KB4's order starts there). The icons take the arrows and Enter
/// while no window has the focus; the app's chords need the app focused, its
/// row keys a focused list, ← → the focused tab strip; Ctrl+V adds a
/// clipping with Notes focused. Card is the F1 card: every command once.
/// DesktopKeyboard polls the keys and runs what this returns. Pure.
/// </summary>
public static class ShortcutMap
{
    /// <summary>The F1 card, in the order of section 3.4: every command on exactly one row.</summary>
    public static readonly IReadOnlyList<ShortcutCardRow> Card = new[]
    {
        new ShortcutCardRow("Ctrl+F", "keys.focusSearch", AppCommand.FocusSearch),
        new ShortcutCardRow("Esc", "keys.escape", AppCommand.Escape),
        new ShortcutCardRow("F1", "keys.help", AppCommand.Help),
        new ShortcutCardRow("Enter", "keys.openIcon", AppCommand.OpenIcon),
        new ShortcutCardRow("← → ↑ ↓", "keys.icons", AppCommand.IconLeft, AppCommand.IconRight, AppCommand.IconUp, AppCommand.IconDown),
        new ShortcutCardRow("Ctrl+1 … Ctrl+6", "keys.tabs", AppCommand.Tab1, AppCommand.Tab2, AppCommand.Tab3, AppCommand.Tab4, AppCommand.Tab5, AppCommand.Tab6),
        new ShortcutCardRow("Ctrl+Tab, Ctrl+Shift+Tab", "keys.nextTab", AppCommand.NextTab, AppCommand.PrevTab),
        new ShortcutCardRow("Ctrl+Shift+PgUp, Ctrl+Shift+PgDn", "keys.moveTab", AppCommand.MoveTabLeft, AppCommand.MoveTabRight),
        new ShortcutCardRow("F6, Shift+F6", "keys.otherPane", AppCommand.OtherPane),
        new ShortcutCardRow("Ctrl+\\", "keys.split", AppCommand.ToggleSplit),
        new ShortcutCardRow("Ctrl+B", "keys.sidebar", AppCommand.ToggleSidebar),
        new ShortcutCardRow("Ctrl+Shift+S", "keys.steps", AppCommand.ToggleSteps),
        new ShortcutCardRow("Alt+←, Alt+→", "keys.history", AppCommand.Back, AppCommand.Forward),
        new ShortcutCardRow("Tab, Shift+Tab", "keys.regions", AppCommand.NextRegion, AppCommand.PrevRegion),
        new ShortcutCardRow("↑ ↓ Home End PgUp PgDn", "keys.rows", AppCommand.RowUp, AppCommand.RowDown, AppCommand.RowFirst, AppCommand.RowLast,
                            AppCommand.PageUp, AppCommand.PageDown),
        new ShortcutCardRow("Enter", "keys.follow", AppCommand.Follow),
        new ShortcutCardRow("Ctrl+Enter", "keys.followOther", AppCommand.FollowOther),
        new ShortcutCardRow("Space", "keys.pick", AppCommand.Pick),
        new ShortcutCardRow("Ctrl+C, Ctrl+Shift+C", "keys.copy", AppCommand.Copy, AppCommand.CopyRow),
        new ShortcutCardRow("Ctrl+V", "keys.paste", AppCommand.Paste),
        new ShortcutCardRow("Ctrl+P", "keys.pin", AppCommand.Pin),
        new ShortcutCardRow("Ctrl+=, Ctrl+-, Ctrl+0", "keys.zoom", AppCommand.ZoomIn, AppCommand.ZoomOut, AppCommand.ZoomReset),
    };

    /// <summary>The tab position (1-6) a Tab1…Tab6 command shows, else 0.</summary>
    public static int TabPosition(AppCommand command) =>
        command >= AppCommand.Tab1 && command <= AppCommand.Tab6 ? command - AppCommand.Tab1 + 1 : 0;

    /// <summary>The command <paramref name="chord"/> gives in <paramref name="context"/>; false when it gives none.</summary>
    public static bool Resolve(KeyChord chord, ShortcutContext context, out AppCommand command)
    {
        command = default;
        if (!context.FrameOpen || !Lookup(chord, context, out AppCommand found))
            return false;
        if (context.MenuOpen && found != AppCommand.Escape)
            return false;
        if (context.TextFieldFocused && !PassesInField(found) &&
            !(context.SearchFocused && (found == AppCommand.NextRegion || found == AppCommand.PrevRegion)))
            return false;
        command = found;
        return true;
    }

    /// <summary>The chords that cannot be typing: they pass while a text field has the keyboard.</summary>
    private static bool PassesInField(AppCommand command) =>
        command == AppCommand.FocusSearch || TabPosition(command) > 0 || command == AppCommand.OtherPane || command == AppCommand.ToggleSplit ||
        command == AppCommand.ToggleSidebar || command == AppCommand.Escape || command == AppCommand.Help;

    /// <summary>The table: the chord's command in the context, before the menu and field filters.</summary>
    private static bool Lookup(KeyChord k, ShortcutContext c, out AppCommand command)
    {
        bool plain = !k.Ctrl && !k.Shift && !k.Alt;
        bool ctrl = k.Ctrl && !k.Shift && !k.Alt;
        bool ctrlShift = k.Ctrl && k.Shift && !k.Alt;

        if (k.Key == ShortcutKey.Escape && plain)
            return Is(AppCommand.Escape, out command);
        if (k.Key == ShortcutKey.F1 && plain)
            return Is(AppCommand.Help, out command);
        if (k.Key == ShortcutKey.F && ctrl)
            return Is(AppCommand.FocusSearch, out command);

        if (c.DesktopFocused && plain && Icons(k.Key, c.IconSelected, out command))
            return true;
        if (c.NotesFocused && k.Key == ShortcutKey.V && ctrl)
            return Is(AppCommand.Paste, out command);
        if (!c.AppFocused)
            return None(out command);

        if (ctrl && App(k.Key, out command))
            return true;
        if (ctrlShift && AppShift(k.Key, out command))
            return true;
        if (k.Key == ShortcutKey.F6 && !k.Ctrl && !k.Alt)
            return Is(AppCommand.OtherPane, out command);
        if (k.Alt && !k.Ctrl && !k.Shift && k.Key == ShortcutKey.Left)
            return Is(AppCommand.Back, out command);
        if (k.Alt && !k.Ctrl && !k.Shift && k.Key == ShortcutKey.Right)
            return Is(AppCommand.Forward, out command);
        if (k.Key == ShortcutKey.Tab && !k.Ctrl && !k.Alt)
            return Is(k.Shift ? AppCommand.PrevRegion : AppCommand.NextRegion, out command);
        if (c.TabStripFocused && plain && (k.Key == ShortcutKey.Left || k.Key == ShortcutKey.Right))
            return Is(k.Key == ShortcutKey.Left ? AppCommand.PrevTab : AppCommand.NextTab, out command);
        if (c.ListFocused)
            return List(k, plain, ctrl, ctrlShift, out command);
        return None(out command);
    }

    /// <summary>The desktop's icons: the arrows (a first one selects an icon), Enter on the selected one.</summary>
    private static bool Icons(ShortcutKey key, bool selected, out AppCommand command)
    {
        switch (key)
        {
            case ShortcutKey.Left: return Is(AppCommand.IconLeft, out command);
            case ShortcutKey.Right: return Is(AppCommand.IconRight, out command);
            case ShortcutKey.Up: return Is(AppCommand.IconUp, out command);
            case ShortcutKey.Down: return Is(AppCommand.IconDown, out command);
            case ShortcutKey.Enter when selected: return Is(AppCommand.OpenIcon, out command);
            default: return None(out command);
        }
    }

    /// <summary>The app's Ctrl chords.</summary>
    private static bool App(ShortcutKey key, out AppCommand command)
    {
        switch (key)
        {
            case ShortcutKey.Digit1: return Is(AppCommand.Tab1, out command);
            case ShortcutKey.Digit2: return Is(AppCommand.Tab2, out command);
            case ShortcutKey.Digit3: return Is(AppCommand.Tab3, out command);
            case ShortcutKey.Digit4: return Is(AppCommand.Tab4, out command);
            case ShortcutKey.Digit5: return Is(AppCommand.Tab5, out command);
            case ShortcutKey.Digit6: return Is(AppCommand.Tab6, out command);
            case ShortcutKey.Tab: return Is(AppCommand.NextTab, out command);
            case ShortcutKey.Backslash: return Is(AppCommand.ToggleSplit, out command);
            case ShortcutKey.B: return Is(AppCommand.ToggleSidebar, out command);
            case ShortcutKey.P: return Is(AppCommand.Pin, out command);
            case ShortcutKey.Equals: return Is(AppCommand.ZoomIn, out command);
            case ShortcutKey.Minus: return Is(AppCommand.ZoomOut, out command);
            case ShortcutKey.Digit0: return Is(AppCommand.ZoomReset, out command);
            default: return None(out command);
        }
    }

    /// <summary>The app's Ctrl+Shift chords.</summary>
    private static bool AppShift(ShortcutKey key, out AppCommand command)
    {
        switch (key)
        {
            case ShortcutKey.Tab: return Is(AppCommand.PrevTab, out command);
            case ShortcutKey.PageUp: return Is(AppCommand.MoveTabLeft, out command);
            case ShortcutKey.PageDown: return Is(AppCommand.MoveTabRight, out command);
            case ShortcutKey.S: return Is(AppCommand.ToggleSteps, out command);
            default: return None(out command);
        }
    }

    /// <summary>A focused list: the arrows, Home, End, the page keys, Enter, Space and the copies.</summary>
    private static bool List(KeyChord k, bool plain, bool ctrl, bool ctrlShift, out AppCommand command)
    {
        if (plain)
            switch (k.Key)
            {
                case ShortcutKey.Up:
                case ShortcutKey.Left: return Is(AppCommand.RowUp, out command);
                case ShortcutKey.Down:
                case ShortcutKey.Right: return Is(AppCommand.RowDown, out command);
                case ShortcutKey.Home: return Is(AppCommand.RowFirst, out command);
                case ShortcutKey.End: return Is(AppCommand.RowLast, out command);
                case ShortcutKey.PageUp: return Is(AppCommand.PageUp, out command);
                case ShortcutKey.PageDown: return Is(AppCommand.PageDown, out command);
                case ShortcutKey.Enter: return Is(AppCommand.Follow, out command);
                case ShortcutKey.Space: return Is(AppCommand.Pick, out command);
            }
        if (ctrl && k.Key == ShortcutKey.Enter)
            return Is(AppCommand.FollowOther, out command);
        if (ctrl && k.Key == ShortcutKey.C)
            return Is(AppCommand.Copy, out command);
        if (ctrlShift && k.Key == ShortcutKey.C)
            return Is(AppCommand.CopyRow, out command);
        return None(out command);
    }

    private static bool Is(AppCommand found, out AppCommand command)
    {
        command = found;
        return true;
    }

    private static bool None(out AppCommand command)
    {
        command = default;
        return false;
    }
}
