using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Puts the gameplay layer on the art office at load (the scene contract,
/// docs/SCENE_CONTRACT_GAMEPLAY.md): resolves every anchor (OfficeAnchors),
/// then moves the layer's click boxes onto the art's props and fits them to
/// their renderers (the art objects get no components of ours), hands the
/// props' renderers to their outlines and reactions, puts the desktop's clone
/// on the PC's glass, sizes the desk, its catcher and the scanner, hands the
/// paper examiner the camera, stands the traveller,
/// binds the readouts to the art's texts (or shows the fallback HUD), and
/// readies the office camera (a PhysicsRaycaster on the Interactable layer,
/// the desktop's layer culled, its Cinemachine camera on top). Runs before
/// every other gameplay component (execution order -1000). An anchor found
/// by a fallback path is logged; one on its default pose or missing is
/// warned about once, naming the tool that adds anchors.
/// </summary>
[DefaultExecutionOrder(-1000)]
public sealed class OfficeSceneBinder : MonoBehaviour
{
    /// <summary>A desk prop's click box and what it stands for.</summary>
    [Serializable]
    public sealed class PropBinding
    {
        /// <summary>The art prop this box stands for.</summary>
        public OfficeAnchorId anchor;

        /// <summary>The prop's click (its BoxCollider sits on the same object).</summary>
        public Clickable click;

        /// <summary>True when the prop's tooltip shows a readout's text.</summary>
        public bool hasReadout;

        /// <summary>The readout whose text the tooltip shows (with <see cref="hasReadout"/>).</summary>
        public OfficeAnchorId readout;
    }

    /// <summary>Where each place is in the art office.</summary>
    [SerializeField] private OfficeSceneContractSO contract;

    /// <summary>The desk tuning (the traveller's height and tint, the READY caption).</summary>
    [SerializeField] private DeskConfigSO config;

    [Header("Office camera users")]
    /// <summary>The traveller wheel (placed through the office camera).</summary>
    [SerializeField] private TravellerWheel wheel;

    /// <summary>The overlay callouts (the speech bubble, the tooltip), placed through the office camera.</summary>
    [SerializeField] private OverlayCallout[] callouts;

    /// <summary>Poses the papers held in the hand in front of the office camera (piece 10; optional).</summary>
    [SerializeField] private PaperExaminer examiner;

    /// <summary>The stamp tray (piece 10; optional), placed over the stamp's click box through the office camera.</summary>
    [SerializeField] private StampTray stampTray;

    [Header("PC")]
    /// <summary>The desktop's clone on the PC's glass.</summary>
    [SerializeField] private PcScreenClone screenClone;

    /// <summary>The PC's click (opens the frame); its box is fitted to the PC.</summary>
    [SerializeField] private Clickable pc;

    /// <summary>The PC's power knob click; its box sits on the knob.</summary>
    [SerializeField] private Clickable pcPower;

    [Header("Desk")]
    /// <summary>The desk plane papers lie on.</summary>
    [SerializeField] private DeskSurface surface;

    /// <summary>The desk scanner (its click box and reaction are on the same object).</summary>
    [SerializeField] private DeskScanner scanner;

    /// <summary>The gameplay's stand-in scanner machine, shown when the art office has none.</summary>
    [SerializeField] private GameObject scannerPlaceholder;

    /// <summary>Where papers slide in from and back to.</summary>
    [SerializeField] private Transform handOver;

    /// <summary>The desk catcher's box (piece 10; optional): sized over the desk's clamp area, just under its plane, so papers and props above it win the raycast.</summary>
    [SerializeField] private BoxCollider deskCatcher;

    /// <summary>The day-1 scan note (floats over the scanner, facing the camera).</summary>
    [SerializeField] private Transform scanHint;

    [Header("Traveller")]
    /// <summary>The traveller (stood at the traveller anchor).</summary>
    [SerializeField] private TravellerView traveller;

    /// <summary>The day-1 wheel note (floats over the traveller's head, facing the camera).</summary>
    [SerializeField] private Transform wheelHint;

    [Header("READY sign")]
    /// <summary>The READY sign's click (GameManager's gate); its box is fitted to the art's NEXT sign.</summary>
    [SerializeField] private Clickable readySign;

    /// <summary>The gameplay's stand-in sign, shown when the art office has none.</summary>
    [SerializeField] private GameObject readyPlaceholder;

    [Header("Props")]
    /// <summary>The desk props' click boxes.</summary>
    [SerializeField] private PropBinding[] props;

    [Header("Readouts")]
    /// <summary>Day, stability and credits.</summary>
    [SerializeField] private OfficeReadouts readouts;

    /// <summary>The shift clock.</summary>
    [SerializeField] private ShiftClockReadouts clock;

    /// <summary>The fallback HUD on the overlay canvas, shown when the art office lacks a readout.</summary>
    [SerializeField] private GameObject fallbackHud;

    /// <summary>The fallback HUD's day text.</summary>
    [SerializeField] private TMP_Text hudDay;

    /// <summary>The fallback HUD's stability text.</summary>
    [SerializeField] private TMP_Text hudStability;

    /// <summary>The fallback HUD's credits text.</summary>
    [SerializeField] private TMP_Text hudCredits;

    /// <summary>The fallback HUD's clock text.</summary>
    [SerializeField] private TMP_Text hudClock;

    /// <summary>A desk anchor's size when it has no renderers (metres, XZ).</summary>
    private static readonly Vector2 DefaultDeskSize = new Vector2(1.6f, 1f);

    /// <summary>The child a prop's tooltip hangs from.</summary>
    private const string TooltipPointName = "TooltipPoint";

    /// <summary>The PC's click box when the PC has no renderers (metres).</summary>
    private const float PcBoxSize = 0.6f;

    /// <summary>The power knob's click box (metres).</summary>
    private const float PowerKnobSize = 0.08f;

    /// <summary>How far above the desk a hint floats (metres).</summary>
    private const float HintHeight = 0.28f;

    /// <summary>The desk catcher's thickness and its top's depth under the desk plane (metres): papers and props above it win the raycast.</summary>
    private const float DeskCatcherThickness = 0.001f;
    private const float DeskCatcherDepth = 0.001f;

    /// <summary>The raycaster's hit buffer: a point can cross every stacked paper, the scanner, a prop and the traveller.</summary>
    private const int RaycastHits = 16;

    private Dictionary<OfficeAnchorId, ResolvedAnchor> _anchors;

    private void Awake()
    {
        Scene art = OfficeScenes.ArtScene;
        if (art.IsValid())
            Bind(art);
        else
            SceneManager.sceneLoaded += HandleLoaded;
    }

    private void OnDestroy() => SceneManager.sceneLoaded -= HandleLoaded;

    /// <summary>The gameplay layer loaded first (played alone in the editor): bind when the art office arrives.</summary>
    private void HandleLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!OfficeScenes.IsArtOffice(scene))
            return;
        SceneManager.sceneLoaded -= HandleLoaded;
        Bind(scene);
    }

    private void Bind(Scene art)
    {
        _anchors = OfficeAnchors.Resolve(art, contract);

        Camera office = CameraOf(At(OfficeAnchorId.OfficeCamera));
        if (office == null)
            office = Camera.main;
        if (office == null)
        {
            Debug.LogError("[OfficeSceneBinder] The art office has no camera (Anchor_OfficeCamera, 'Main Camera'): nothing can be clicked. See docs/SCENE_CONTRACT_GAMEPLAY.md.", this);
            return;
        }

        ReadyCamera(office);
        if (wheel != null)
            wheel.SetCamera(office);
        foreach (OverlayCallout callout in callouts ?? Array.Empty<OverlayCallout>())
            if (callout != null)
                callout.SetCamera(office);
        if (examiner != null)
            examiner.SetCamera(office);
        if (stampTray != null)
            stampTray.SetCamera(office);

        Vector3 viewer = office.transform.position;
        BindPc(viewer);
        float deskTop = BindDesk(viewer);
        BindTraveller(viewer, deskTop);
        BindReadySign();
        BindReadouts();
        BindProps();
        Report();
    }

    // -----------------------------
    // Camera
    // -----------------------------

    /// <summary>The office camera raycasts the Interactable layer (and nothing else), never draws the desktop's layer, and shows the office's Cinemachine camera.</summary>
    private void ReadyCamera(Camera cam)
    {
        // Physics2DRaycaster derives from PhysicsRaycaster: find (or add) a 3D one by its exact type.
        PhysicsRaycaster raycaster = null;
        foreach (PhysicsRaycaster found in cam.GetComponents<PhysicsRaycaster>())
        {
            if (found is Physics2DRaycaster legacy2D)
                legacy2D.enabled = false;
            else if (raycaster == null)
                raycaster = found;
        }
        if (raycaster == null)
            raycaster = cam.gameObject.AddComponent<PhysicsRaycaster>();
        raycaster.eventMask = LayerMask.GetMask(OfficeLayers.Interactable);
        raycaster.maxRayIntersections = RaycastHits;

        int desktop = OfficeLayers.PcDesktopLayer;
        if (desktop >= 0)
            cam.cullingMask &= ~(1 << desktop);

        Transform vcam = At(OfficeAnchorId.OfficeVCam).Transform;
        if (vcam != null && vcam.TryGetComponent(out CinemachineCamera office))
            office.Priority = 100;
    }

    // -----------------------------
    // PC
    // -----------------------------

    /// <summary>The desktop's clone on the PC's glass, the PC's click box and the power knob's.</summary>
    private void BindPc(Vector3 viewer)
    {
        ResolvedAnchor anchor = At(OfficeAnchorId.PCScreen);
        (Renderer glass, int submesh) = anchor.Transform != null ? OfficeAnchors.FindGlass(anchor.Transform) : (null, 0);
        if (glass != null && screenClone != null)
            screenClone.Bind(glass, submesh, viewer);
        else if (anchor.Transform != null)
            Debug.LogWarning($"[OfficeSceneBinder] The PC ('{anchor.Path}') has no renderer or material named Glass or Screen: its screen shows no desktop. See docs/SCENE_CONTRACT_GAMEPLAY.md.", this);

        var pcBox = new Bounds(anchor.Position, Vector3.one * PcBoxSize);
        if (anchor.HasBounds)
            pcBox = anchor.Bounds;
        if (pc != null)
            PlaceBox(pc, anchor, Vector3.one * PcBoxSize);

        ResolvedAnchor power = At(OfficeAnchorId.PCPower);
        if (pcPower == null)
            return;
        if (power.Transform != null)
        {
            PlaceBox(pcPower, power, Vector3.one * PowerKnobSize);
        }
        else if (screenClone != null && screenClone.IsBound)
        {
            PlaceBox(pcPower.transform, screenClone.Frame.PowerKnob, Vector3.one * PowerKnobSize);
        }
        else
        {
            pcPower.gameObject.SetActive(false);
            return;
        }
        InFrontOf(pcPower.transform, pcBox, viewer);
    }

    /// <summary>
    /// A small click box inside a bigger one (the power knob on the PC) would
    /// never be hit first: slides it along the view ray to just in front of the
    /// big box, so it covers the same spot on screen and wins the raycast.
    /// </summary>
    private static void InFrontOf(Transform box, Bounds cover, Vector3 viewer)
    {
        Vector3 toBox = box.position - viewer;
        float distance = toBox.magnitude;
        if (distance < 1e-4f || !cover.Contains(box.position) || !cover.IntersectRay(new Ray(viewer, toBox / distance), out float entry))
            return;
        box.position = viewer + toBox / distance * Mathf.Max(0f, entry - PowerKnobSize);
    }

    // -----------------------------
    // Desk, scanner, hand-over
    // -----------------------------

    /// <summary>Sizes the desk plane (the desk anchor and the scanner's drop area together), places the scanner, the hand-over point and the scan note. Returns the desk top's height.</summary>
    private float BindDesk(Vector3 viewer)
    {
        ResolvedAnchor desk = At(OfficeAnchorId.DeskSurface);
        float top = desk.HasBounds ? desk.Bounds.max.y : desk.Position.y;
        Vector3 deskCentre = desk.HasBounds ? desk.Bounds.center : desk.Position;
        Vector2 deskSize = desk.HasBounds ? new Vector2(desk.Bounds.size.x, desk.Bounds.size.z) : DefaultDeskSize;
        var deskRect = new DeskRect(deskCentre.x, deskCentre.z, deskSize.x, deskSize.y);

        DeskRect area = deskRect;
        if (scanner != null)
        {
            ResolvedAnchor spot = At(OfficeAnchorId.Scanner);
            bool artScanner = spot.HasBounds;
            if (scannerPlaceholder != null)
                scannerPlaceholder.SetActive(!artScanner);

            Transform s = scanner.transform;
            if (artScanner)
            {
                s.SetPositionAndRotation(new Vector3(spot.Bounds.center.x, spot.Bounds.min.y, spot.Bounds.center.z), Quaternion.identity);
                scanner.Configure(new Vector2(spot.Bounds.size.x, spot.Bounds.size.z), new Vector3(0f, spot.Bounds.size.y + 0.002f, 0f));
            }
            else
            {
                s.SetPositionAndRotation(new Vector3(spot.Position.x, top, spot.Position.z), spot.Rotation);
            }

            if (scanner.TryGetComponent(out BoxCollider box))
            {
                box.center = new Vector3(0f, 0.06f, 0f);
                box.size = new Vector3(scanner.DropSize.x, 0.12f, scanner.DropSize.y);
            }
            if (scanner.TryGetComponent(out Clickable scannerClick))
                scannerClick.SetOutline(artScanner ? spot.Transform.GetComponentsInChildren<Renderer>(false) : RenderersOf(scannerPlaceholder));

            Bounds drop = new Bounds(s.position, Vector3.zero);
            drop.Encapsulate(s.TransformPoint(new Vector3(-scanner.DropSize.x / 2f, 0f, -scanner.DropSize.y / 2f)));
            drop.Encapsulate(s.TransformPoint(new Vector3(scanner.DropSize.x / 2f, 0f, scanner.DropSize.y / 2f)));
            area = DeskRect.Union(deskRect, new DeskRect(drop.center.x, drop.center.z, drop.size.x, drop.size.z));

            if (scanHint != null)
                FaceCamera(scanHint, scanner.BedPoint + Vector3.up * HintHeight, viewer);
        }

        if (surface != null)
        {
            surface.transform.SetPositionAndRotation(new Vector3(area.CentreX, top, area.CentreY), Quaternion.identity);
            surface.Configure(new Vector2(area.Width, area.Height),
                              new Vector2(deskRect.CentreX - area.CentreX, deskRect.CentreY - area.CentreY),
                              new Vector2(deskRect.Width, deskRect.Height));
        }

        if (deskCatcher != null)
            PlaceBox(deskCatcher.transform, new Vector3(area.CentreX, top - DeskCatcherDepth - DeskCatcherThickness / 2f, area.CentreY),
                     new Vector3(area.Width, DeskCatcherThickness, area.Height));

        if (handOver != null)
        {
            Vector3 from = At(OfficeAnchorId.HandOver).Position;
            handOver.position = new Vector3(from.x, top, from.z);
        }

        return top;
    }

    // -----------------------------
    // Traveller
    // -----------------------------

    private void BindTraveller(Vector3 viewer, float deskTop)
    {
        if (traveller == null || config == null)
            return;

        Vector3 feet = At(OfficeAnchorId.Traveller).Position;
        traveller.Stand(feet, viewer, config.travellerHeight, config.travellerTint, deskTop);
        if (wheelHint != null)
            FaceCamera(wheelHint, feet + Vector3.up * (config.travellerHeight + HintHeight), viewer);
    }

    // -----------------------------
    // READY sign, props, readouts
    // -----------------------------

    private void BindReadySign()
    {
        if (readySign == null)
            return;

        ResolvedAnchor sign = At(OfficeAnchorId.NextSign);
        bool art = sign.HasBounds;
        if (readyPlaceholder != null)
            readyPlaceholder.SetActive(!art);
        if (art)
        {
            PlaceBox(readySign, sign, Vector3.zero);
        }
        else
        {
            readySign.transform.SetPositionAndRotation(sign.Position, sign.Rotation);
            readySign.SetOutline(RenderersOf(readyPlaceholder));
        }
    }

    private void BindProps()
    {
        foreach (PropBinding prop in props ?? Array.Empty<PropBinding>())
        {
            if (prop == null || prop.click == null)
                continue;

            ResolvedAnchor anchor = At(prop.anchor);
            if (!anchor.HasBounds)
            {
                prop.click.gameObject.SetActive(false);
                continue;
            }

            PlaceBox(prop.click, anchor, Vector3.zero);
            if (prop.anchor == OfficeAnchorId.Stamp && stampTray != null)
                stampTray.SetFollow(prop.click.transform);
            if (prop.click.TryGetComponent(out DeskReaction reaction))
            {
                reaction.SetTarget(anchor.Transform);
                reaction.SetTooltipPoint(TopOf(prop.click.transform, anchor.Bounds));
                if (prop.hasReadout)
                    reaction.SetReadout(TextOf(prop.readout));
            }
        }
    }

    /// <summary>Binds each readout to the art's text, or to the fallback HUD's when the art has none (the HUD shows only then); writes the READY caption.</summary>
    private void BindReadouts()
    {
        bool hud = false;
        TMP_Text Pick(OfficeAnchorId id, TMP_Text fallback)
        {
            TMP_Text text = TextOf(id);
            if (text != null)
                return text;
            hud |= fallback != null;
            return fallback;
        }

        TMP_Text day = Pick(OfficeAnchorId.ReadoutDay, hudDay);
        TMP_Text stability = Pick(OfficeAnchorId.ReadoutStability, hudStability);
        TMP_Text credits = Pick(OfficeAnchorId.ReadoutCredits, hudCredits);
        TMP_Text time = Pick(OfficeAnchorId.ReadoutClock, hudClock);
        if (readouts != null)
            readouts.Bind(day, stability, credits);
        if (clock != null)
            clock.Bind(time);

        foreach (TMP_Text unused in new[] { hudDay, hudStability, hudCredits, hudClock })
            if (unused != null)
                unused.gameObject.SetActive(unused == day || unused == stability || unused == credits || unused == time);
        if (fallbackHud != null)
            fallbackHud.SetActive(hud);

        TMP_Text caption = TextOf(OfficeAnchorId.ReadoutNext);
        if (caption != null && config != null && !string.IsNullOrWhiteSpace(config.readyCaptionKey))
            caption.text = UiText.Get(config.readyCaptionKey);
    }

    // -----------------------------
    // Helpers
    // -----------------------------

    private ResolvedAnchor At(OfficeAnchorId id) => _anchors[id];

    private TMP_Text TextOf(OfficeAnchorId id)
    {
        Transform t = At(id).Transform;
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }

    private static Camera CameraOf(ResolvedAnchor anchor) =>
        anchor.Transform != null ? anchor.Transform.GetComponent<Camera>() : null;

    private static Renderer[] RenderersOf(GameObject go) =>
        go != null ? go.GetComponentsInChildren<Renderer>(false) : Array.Empty<Renderer>();

    /// <summary>Puts a click box on an art prop: at its renderers' bounds (world-aligned), its renderers outlined on hover; with no renderers, a box of <paramref name="fallbackSize"/> at the anchor's position.</summary>
    private static void PlaceBox(Clickable click, ResolvedAnchor anchor, Vector3 fallbackSize)
    {
        if (anchor.HasBounds)
        {
            PlaceBox(click.transform, anchor.Bounds.center, anchor.Bounds.size);
            click.SetOutline(anchor.Transform.GetComponentsInChildren<Renderer>(false));
        }
        else
        {
            PlaceBox(click.transform, anchor.Position, fallbackSize);
        }
    }

    /// <summary>A child point on top of <paramref name="bounds"/> (created once), where a prop's tooltip hangs.</summary>
    private static Transform TopOf(Transform box, Bounds bounds)
    {
        Transform point = box.Find(TooltipPointName);
        if (point == null)
        {
            point = new GameObject(TooltipPointName).transform;
            point.SetParent(box, false);
        }
        point.position = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
        return point;
    }

    /// <summary>A world-aligned box of <paramref name="size"/> centred on <paramref name="centre"/> (the object's BoxCollider).</summary>
    private static void PlaceBox(Transform box, Vector3 centre, Vector3 size)
    {
        box.SetPositionAndRotation(centre, Quaternion.identity);
        box.localScale = Vector3.one;
        if (box.TryGetComponent(out BoxCollider collider))
        {
            collider.center = Vector3.zero;
            collider.size = size;
        }
    }

    /// <summary>Puts a floating note at <paramref name="position"/>, turned to face <paramref name="viewer"/>.</summary>
    private static void FaceCamera(Transform note, Vector3 position, Vector3 viewer)
    {
        Vector3 away = position - viewer;
        note.SetPositionAndRotation(position, away.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(away, Vector3.up) : Quaternion.identity);
    }

    /// <summary>One log line for the anchors found by fallback paths, one warning for those on default poses or missing.</summary>
    private void Report()
    {
        List<ResolvedAnchor> fallbacks = _anchors.Values.Where(a => a.Source == AnchorSource.Fallback).ToList();
        List<ResolvedAnchor> stand = _anchors.Values.Where(a => a.Source == AnchorSource.Default || a.Source == AnchorSource.Missing).ToList();
        if (fallbacks.Count > 0)
            Debug.Log($"[OfficeSceneBinder] Office contract: {fallbacks.Count} anchor(s) found by fallback paths ({string.Join(", ", fallbacks.Select(a => $"{a.Id} = '{a.Path}'"))}).", this);
        if (stand.Count > 0)
            Debug.LogWarning($"[OfficeSceneBinder] The art office has no {string.Join(", ", stand.Select(a => $"{OfficeContract.AnchorName(a.Id)} ({(a.Source == AnchorSource.Default ? "default pose" : "left out")})"))}. Add them with Tools > TimeDesk > Add Gameplay Anchors (docs/SCENE_CONTRACT_GAMEPLAY.md).", this);
    }
}
