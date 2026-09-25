/// <summary>
/// The role of a themed UI graphic (piece 6): which theme colours it takes.
/// Serialized by ThemeTag as an int: append only once shipped. Roles from
/// DiegeticPaper on are diegetic (evidence and what the traveller says): the
/// theme never touches them (ThemeRoles.IsDiegetic).
/// </summary>
public enum ThemeRoleId
{
    /// <summary>The desktop wallpaper image (the theme's wallpaper sprite, else its fill).</summary>
    Desktop,

    /// <summary>A strip behind a text on the wallpaper (the verdict line, the idle line), and that text.</summary>
    ScreenStrip,

    /// <summary>The taskbar.</summary>
    Taskbar,

    /// <summary>The taskbar's and Start button's gloss.</summary>
    TaskbarGloss,

    /// <summary>The Start button and its label.</summary>
    StartButton,

    /// <summary>The system tray and its readouts.</summary>
    Tray,

    /// <summary>A window's body and its texts.</summary>
    WindowBody,

    /// <summary>A window's title bar and title.</summary>
    TitleBar,

    /// <summary>A title bar's gloss.</summary>
    TitleGloss,

    /// <summary>A default button (window controls, Prev/Next, Acknowledge, the Settings choices).</summary>
    Button,

    /// <summary>A window's close button.</summary>
    CloseButton,

    /// <summary>The Accept button, its label and its tick.</summary>
    AcceptButton,

    /// <summary>The Deny button, its label and its cross.</summary>
    DenyButton,

    /// <summary>A traveller wheel choice.</summary>
    WheelButton,

    /// <summary>The Records SEARCH button.</summary>
    SearchButton,

    /// <summary>A desktop icon tile.</summary>
    DesktopIcon,

    /// <summary>The taskbar's "&lt; Desk" button.</summary>
    DeskButton,

    /// <summary>The runtime text-fallback panel.</summary>
    Panel,

    /// <summary>The claim strip and the claim banner.</summary>
    ClaimStrip,

    /// <summary>The citation slip and its text.</summary>
    Alert,

    /// <summary>The Directives sticky note and its text.</summary>
    StickyNote,

    /// <summary>The compare bar and its text.</summary>
    CompareBar,

    /// <summary>The compare bar's MATCH ink (CompareController).</summary>
    CompareMatch,

    /// <summary>The compare bar's MISMATCH and DEVIATION LOGGED ink (CompareController).</summary>
    CompareMismatch,

    /// <summary>The compare bar's ink while one value is picked (CompareController).</summary>
    CompareNeutral,

    /// <summary>The tint of a selected compare row (CompareController).</summary>
    SelectionHighlight,

    /// <summary>The Start menu.</summary>
    StartMenu,

    /// <summary>A Start menu entry.</summary>
    MenuEntry,

    /// <summary>The Start menu's Quit entry.</summary>
    QuitEntry,

    /// <summary>A newsletter's border and rule.</summary>
    NewsletterBorder,

    /// <summary>A newsletter's paper and texts.</summary>
    Newsletter,

    /// <summary>A newsletter's button (START SHIFT, GO HOME).</summary>
    NewsletterButton,

    /// <summary>The translucent dim behind the investigation desk.</summary>
    DeskDim,

    /// <summary>An input field's box and text.</summary>
    InputField,

    /// <summary>An input field's placeholder.</summary>
    InputPlaceholder,

    /// <summary>The desk props' tooltip.</summary>
    Tooltip,

    /// <summary>A transparent click catcher (the traveller wheel's).</summary>
    ClickCatcher,

    /// <summary>Diegetic: the scanned document page.</summary>
    DiegeticPaper,

    /// <summary>Diegetic: the document's photo box and its label.</summary>
    DiegeticPhoto,

    /// <summary>Diegetic: document, record and transcript rows and their texts, the record's origin.</summary>
    DiegeticRow,

    /// <summary>Diegetic: record row labels.</summary>
    DiegeticLabel,

    /// <summary>Diegetic: the record's clerk note.</summary>
    DiegeticNote,

    /// <summary>Diegetic: the document window's dark scanner backing and its page footer.</summary>
    DiegeticBacking,

    /// <summary>Diegetic: reference-book rows and their texts.</summary>
    DiegeticBookRow,

    /// <summary>Diegetic: the traveller's speech bubble (spoken evidence).</summary>
    DiegeticBubble
}

/// <summary>The rule that keeps theming off evidence (piece 6 Z4).</summary>
public static class ThemeRoles
{
    /// <summary>True for the diegetic roles (DiegeticPaper and after): the theme never recolours or refonts them, and cultures may not override them.</summary>
    public static bool IsDiegetic(ThemeRoleId role) => role >= ThemeRoleId.DiegeticPaper;
}
