using UnityEngine;

/// <summary>
/// The layers the gameplay layer owns (ProjectSettings/TagManager): the
/// physics layers Interactable (every office click box: the office camera's
/// PhysicsRaycaster hits nothing else, so the art's own colliders never block
/// a click) and PCDesktop (the desktop canvas: only the PC frame and clone
/// cameras draw it; the office camera never does), and the sorting layer
/// Gameplay (the traveller's figure and the desk notes, drawn in front of the
/// art's sprites whatever their orders). Build Office UI adds all three.
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
