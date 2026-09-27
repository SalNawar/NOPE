using UnityEngine;

/// <summary>
/// The two layers the gameplay layer owns (ProjectSettings/TagManager):
/// Interactable holds every office click box (the office camera's
/// PhysicsRaycaster hits nothing else, so the art's own colliders never block
/// a click), and PCDesktop holds the desktop canvas (only the PC frame and
/// clone cameras draw it; the office camera never does).
/// </summary>
public static class OfficeLayers
{
    /// <summary>The office click boxes' layer name.</summary>
    public const string Interactable = "Interactable";

    /// <summary>The desktop canvas's layer name.</summary>
    public const string PcDesktop = "PCDesktop";

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
