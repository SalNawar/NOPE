using UnityEngine;

/// <summary>
/// The layers the gameplay layer owns (ProjectSettings/TagManager): the
/// physics layers Interactable (every office click box: the office camera's
/// PhysicsRaycaster hits nothing else, so the art's own colliders never block
/// a click) and PCDesktop (the desktop canvas: only the PC frame and clone
/// cameras draw it; the office camera never does), and the sorting layer
/// Gameplay (the traveller's figure and the desk notes, drawn in front of the
/// art's sprites whatever their orders). Build Office UI adds all three; Add
/// Anime Hall Hooks adds the anime hall's HallBackdrop layer.
/// </summary>
public static class OfficeLayers
{
    /// <summary>The office click boxes' layer name.</summary>
    public const string Interactable = "Interactable";

    /// <summary>The desktop canvas's layer name.</summary>
    public const string PcDesktop = "PCDesktop";

    /// <summary>
    /// The sorting layer of the gameplay layer's world sprites and notes (the
    /// traveller's figure, the day-1 desk notes), listed after Default so they
    /// draw over the art's sprites (the anime hall's 58 registered layers use
    /// orders 0..57 on Default, which would otherwise cover a traveller standing
    /// in front of them: transparent objects sort by layer and order before depth).
    /// </summary>
    public const string SortingLayer = "Gameplay";

    /// <summary>
    /// The anime hall's painted layers' layer (with the portal rings' effects,
    /// the hall's Light2Ds and its dust): only the hall's 2D backdrop camera
    /// draws it (HallBackdrop), never the office camera, which shows that
    /// camera's picture behind the desk instead. Add Anime Hall Hooks adds it.
    /// </summary>
    public const string HallBackdrop = "HallBackdrop";

    /// <summary>
    /// The sorting layer of the anime hall's sky (its exterior layer), listed
    /// before Default: the hall's layers are mutually exclusive masks, so the
    /// sky still draws where it did, but the interior's lights (fixtures,
    /// screens, shafts) never light it; its own global light gives it the
    /// day's sky (HallLightKind.Sky). Add Anime Hall Hooks adds it.
    /// </summary>
    public const string SkySortingLayer = "HallSky";

    /// <summary>
    /// The sorting layer of the anime hall's Departure Board display, listed
    /// before Default like HallSky: only the hall's global light and the board's
    /// own light reach it, so the ceiling fixtures above it never wash out the
    /// day's rows printed on it. Add Anime Hall Hooks adds it.
    /// </summary>
    public const string DisplaySortingLayer = "HallDisplays";

    /// <summary>The HallBackdrop layer's index (-1 when the project lacks it).</summary>
    public static int HallBackdropLayer => LayerMask.NameToLayer(HallBackdrop);

    /// <summary>The Interactable layer's index (-1 when the project lacks it).</summary>
    public static int InteractableLayer => LayerMask.NameToLayer(Interactable);

    /// <summary>The PCDesktop layer's index (-1 when the project lacks it).</summary>
    public static int PcDesktopLayer => LayerMask.NameToLayer(PcDesktop);

    /// <summary>
    /// The layers the gameplay layer's visible objects live on, which the office
    /// camera must draw whatever the art culled it to: Default (the traveller,
    /// the placeholders, the notes) and Interactable (the papers' sheets, the
    /// click boxes). The binder adds them to the art camera's culling mask.
    /// </summary>
    public static int GameplayMask => 1 | (InteractableLayer >= 0 ? 1 << InteractableLayer : 0);
}
