using System;
using UnityEngine;

/// <summary>
/// The office scene contract (docs/SCENE_CONTRACT_GAMEPLAY.md): where the
/// gameplay layer (Assets/Scenes/OfficeGameplay.unity) finds each named place
/// in the art office (RunConfig.officeSceneName: Assets/Scenes/OfficeScene.unity
/// or the anime hall, Assets/Art/Office/AnimeHallLayers/AnimeHall.unity), which
/// the art side owns. For every anchor: an object named Anchor_{id} first (the art side's
/// explicit mark), then these fallback paths to existing objects, then the
/// default pose. It also lists the art scene's leftover gameplay objects the
/// layer switches off at load. Created by Tools > TimeDesk > Build Office UI
/// (Assets/Data/Config/OfficeSceneContract.asset); read by the load hook
/// (through RunConfig), the gameplay layer's binder, and the editor's contract
/// tools (Add Gameplay Anchors, Check Office Scene Contract).
/// </summary>
[CreateAssetMenu(fileName = "OfficeSceneContract", menuName = "TimeDesk/Office/Office Scene Contract")]
public sealed class OfficeSceneContractSO : ScriptableObject
{
    /// <summary>One anchor: where to look for it, and where it is when nothing is found.</summary>
    [Serializable]
    public sealed class AnchorSpec
    {
        /// <summary>Which place this is.</summary>
        public OfficeAnchorId id;

        /// <summary>Existing objects to use when no Anchor_{id} exists, in order: a root path ("HybridOffice/Booth/Blender_Next") or a bare object name ("NextLabel"). Inactive objects are skipped.</summary>
        public string[] fallbacks = Array.Empty<string>();

        /// <summary>True when the gameplay can stand in with the default pose (a placeholder or a point); false when it goes without.</summary>
        public bool hasDefault;

        /// <summary>The default position in world space (also where Add Gameplay Anchors puts a new anchor).</summary>
        public Vector3 defaultPosition;

        /// <summary>The default heading in degrees around world up (0 faces +z, away from the office camera).</summary>
        public float defaultYaw;

        /// <summary>An empty spec (for the serializer and the inspector).</summary>
        public AnchorSpec()
        {
        }

        /// <summary>Creates a spec.</summary>
        public AnchorSpec(OfficeAnchorId id, string[] fallbacks, bool hasDefault, Vector3 defaultPosition, float defaultYaw = 0f)
        {
            this.id = id;
            this.fallbacks = fallbacks ?? Array.Empty<string>();
            this.hasDefault = hasDefault;
            this.defaultPosition = defaultPosition;
            this.defaultYaw = defaultYaw;
        }
    }

    /// <summary>The anime hall's credits readout, inside its till (Anchor_Till; the art side moved it there with the cashier's pulse).</summary>
    public const string TillReadoutPath = "Anime terminal - registered art and preserved desk/Approved 3D desk \u2014 preserved art/Anchor_Till/CreditsNumber";

    /// <summary>
    /// Every anchor the gameplay layer resolves. The first fallbacks are the old art
    /// office's (art ea62550: the desk layout of 633e2e5); the later bare names are the
    /// anime hall's preserved desk (art c75e1fe), whose props are flat lists of
    /// "Prop__Material" mesh parts, one part each here (the one whose bounds stand for
    /// the prop; the PC's must be its glass, which the desktop's clone binds to). The
    /// defaults fit both, as the hall keeps the same desk layout.
    /// </summary>
    public AnchorSpec[] anchors =
    {
        new AnchorSpec(OfficeAnchorId.PCScreen, new[] { "ImportedOfficeDress/Desk/Retro CRT", "CRT2_Glass" }, true, new Vector3(-1.64f, 1.43f, -0.13f), 160f),
        new AnchorSpec(OfficeAnchorId.PCPower, new[] { "ImportedOfficeDress/Desk/Retro CRT/Rebuilt CRT/CRT2_Orange", "CRT2_Orange" }, true, Vector3.zero),
        new AnchorSpec(OfficeAnchorId.DeskSurface, new[] { "HybridOffice/Booth/Finish_Mat", "Clean_Blotter__DeskClean_Pad" }, true, new Vector3(0f, 1.07f, -0.52f)),
        new AnchorSpec(OfficeAnchorId.Scanner, null, true, new Vector3(1.02f, 1.06f, -0.46f), 180f),
        new AnchorSpec(OfficeAnchorId.Traveller, null, true, new Vector3(0f, 0f, 1.6f), 180f),
        new AnchorSpec(OfficeAnchorId.HandOver, null, true, new Vector3(0.05f, 1.07f, 0.45f)),
        new AnchorSpec(OfficeAnchorId.NextSign, new[] { "HybridOffice/Booth/Blender_Next", "Clean_Next__DeskClean_ABS" }, true, new Vector3(0.13f, 1.06f, 0.16f), 180f),
        new AnchorSpec(OfficeAnchorId.Intercom, new[] { "ImportedOfficeDress/Desk/Clerk hotline", "HybridOffice/Booth/Finish_Intercom", "Clean_Phone__DeskClean_PhoneBody" }, false, Vector3.zero),
        new AnchorSpec(OfficeAnchorId.Stamp, new[] { "HybridOffice/Booth/Blender_Stamp", "Clean_Stamp__DeskClean_Wood" }, false, Vector3.zero),
        new AnchorSpec(OfficeAnchorId.Till, new[] { "HybridOffice/Booth/Finish_Till", "Clean_Till__DeskClean_Green" }, false, Vector3.zero),
        new AnchorSpec(OfficeAnchorId.StabilityMonitor, new[] { "HybridOffice/Booth/Blender_Stability", "Office_Stability__Plastic_WarmGrey" }, false, Vector3.zero),
        new AnchorSpec(OfficeAnchorId.Calendar, new[] { "HybridOffice/Booth/Blender_DayCalendar", "Office_Calendar__Office_TealDark" }, false, Vector3.zero),
        new AnchorSpec(OfficeAnchorId.Clock, new[] { "HybridOffice/Booth/Blender_Clock", "Office_Clock__Plastic_WarmGrey" }, false, Vector3.zero),
        new AnchorSpec(OfficeAnchorId.ReadoutDay, new[] { "GameplayAnchors/DayNumber", "DayNumber" }, false, Vector3.zero),
        new AnchorSpec(OfficeAnchorId.ReadoutStability, new[] { "GameplayAnchors/StabilityPercent", "StabilityPercent" }, false, Vector3.zero),
        // The hall's till holds its own readout since the hall-art pass (2026-10-07, the cashier pulse): first, as the bare name also matches a desk mesh.
        new AnchorSpec(OfficeAnchorId.ReadoutCredits, new[] { TillReadoutPath, "GameplayAnchors/CreditsNumber", "CreditsNumber" }, false, Vector3.zero),
        new AnchorSpec(OfficeAnchorId.ReadoutClock, new[] { "GameplayAnchors/ShiftClockDisplay", "ShiftClockDisplay" }, false, Vector3.zero),
        new AnchorSpec(OfficeAnchorId.ReadoutNext, new[] { "GameplayAnchors/NextLabel", "NextLabel" }, false, Vector3.zero),
        new AnchorSpec(OfficeAnchorId.OfficeCamera, new[] { "Main Camera", "Anime hall player preview" }, false, Vector3.zero),
        new AnchorSpec(OfficeAnchorId.OfficeVCam, new[] { "Cameras/OfficeVCam" }, false, Vector3.zero),
        new AnchorSpec(OfficeAnchorId.Calculator, new[] { "ImportedOfficeDress/Desk/Desk calculator", "Clean_Calculator__DeskClean_Case" }, false, Vector3.zero),
        new AnchorSpec(OfficeAnchorId.PenPot, new[] { "ImportedOfficeDress/Desk/Pen pot", "Clean_PenPot__DeskClean_ABS" }, false, Vector3.zero),
        new AnchorSpec(OfficeAnchorId.Stapler, new[] { "ImportedOfficeDress/Desk/Forms stapler", "Clean_Stapler__DeskClean_Case" }, false, Vector3.zero),
        new AnchorSpec(OfficeAnchorId.DepartureBoard, new[] { "16 Departure board blank display" }, false, Vector3.zero),
    };

    /// <summary>
    /// The art scene's leftover gameplay objects (copies from before the move:
    /// the old game manager, desktop, overlay, event system, 2D booth and the
    /// monitor push-in camera), root paths switched off when the art office
    /// loads, before any of them starts. The art side may delete them.
    /// </summary>
    public string[] legacyRoots =
    {
        "GameManager", "DaySystem", "EventSystem", "Canvas", "OfficeOverlayCanvas", "OfficeRoot", "Cameras/MonitorVCam"
    };

    /// <summary>The spec for an anchor, or null when the contract lists none.</summary>
    public AnchorSpec Spec(OfficeAnchorId id)
    {
        if (anchors == null)
            return null;
        foreach (AnchorSpec spec in anchors)
            if (spec != null && spec.id == id)
                return spec;
        return null;
    }
}
