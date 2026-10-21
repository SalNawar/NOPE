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
/// on the PC's glass, sizes the desk, the mat and the scanner, poses the desk
/// view from the art's Cinemachine camera and the mat, lays the counter and
/// the rulebook and the 3D stamp bar's rack, binds the desk's slide-in to
/// the machines and tools on their homes (DeskSlideIn, BindSlideIn), installs
/// the painted desk over the art's when the plan's DeskArt.Painted toggle is on
/// (DeskPainted: its homes, its look on the gameplay objects, the art desk
/// hidden; InstallPainted), stands the traveller,
/// binds the readouts to the art's texts (or shows the fallback HUD), lays the
/// Helix River over the stability monitor's text (HelixRiverMonitor.Cover; the
/// fallback HUD's river when the art has none), has the
/// AVAILABLE sign's caption follow the desk's availability (AvailableSignLink), points
/// the anime hall's presentation at the shift clock (AnimeHallShiftLink) and
/// its portal rings at the day's portals (AnimeHallPortalLink) when the art
/// carries one, puts the Departure Board's rows and click box on its display
/// (DepartureBoardView), and readies the office camera (a PhysicsRaycaster on
/// the Interactable layer, the gameplay's layers drawn and the desktop's
/// culled, an AudioListener when no scene has one, its Cinemachine camera on
/// top) and orders the desktop's cameras around it (the PC frame's right
/// after it, the clone's right before it, whatever depth the art gave its
/// camera). Runs before
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

    /// <summary>The desk tuning (the traveller's height and tint, the AVAILABLE sign's caption and paused ink, the anime hall's evening).</summary>
    [SerializeField] private DeskConfigSO config;

    [Header("Office camera users")]
    /// <summary>The traveller wheel (placed through the office camera).</summary>
    [SerializeField] private TravellerWheel wheel;

    /// <summary>The overlay callouts (the speech bubble, the tooltip), placed through the office camera.</summary>
    [SerializeField] private OverlayCallout[] callouts;

    /// <summary>The city view (the desk-first redesign, item 6; optional): bound to the anime hall's left pan and living city.</summary>
    [SerializeField] private CityView cityView;

    /// <summary>Inspection at the desk (the desk-first redesign, item 11; optional): the values' places are seen through the office camera, and the rulebook lies on the desk.</summary>
    [SerializeField] private DeskInspect deskInspect;

    /// <summary>The desk's guide (optional): its arrows are placed through the office camera.</summary>
    [SerializeField] private GuideDirector guide;

    /// <summary>The rulebook on the desk (optional): laid beside the mat in the office view's frame (DeskConfigSO.rulebookAt).</summary>
    [SerializeField] private DeskRulebook rulebook;

    /// <summary>The stamp rack (Papers, Please's stamps with the art's 3D daters, Saleh 2026-10-06; optional): laid on its bay beside the counter (DeskLayoutSettings.stampRack).</summary>
    [SerializeField] private DeskStampTray stampTray;

    /// <summary>The DETAIN button (optional; the desk machine spec §2): laid on its bay beside the AVAILABLE sign (DeskLayoutSettings.detain).</summary>
    [SerializeField] private DetainButton detainButton;

    /// <summary>The desk machines (the desk machines spec §1): laid on their bays (DeskLayoutSettings), their first-day notes over them.</summary>
    [SerializeField] private DeskHardware hardware;

    /// <summary>The counter (optional): laid along the desk's far edge in the office view's frame, once the desk view is posed.</summary>
    [SerializeField] private DeskCounter counter;

    /// <summary>The painted desk (desk v3, DESK-ART; optional): installed over the art's desk when DeskLayoutSettings.paintedDesk is on, else hidden (InstallPainted).</summary>
    [SerializeField] private DeskPainted paintedDesk;

    [Header("PC")]
    /// <summary>The desktop's clone on the PC's glass.</summary>
    [SerializeField] private PcScreenClone screenClone;

    /// <summary>The PC frame (its camera draws the desktop into the glass right after the office camera).</summary>
    [SerializeField] private PcFrame frame;

    /// <summary>Keeps the office's edge pull tabs clear of the calendar (optional): bound to the office camera and the office view's Cinemachine camera.</summary>
    [SerializeField] private EdgeTabClearance tabClearance;

    /// <summary>The PC's click (opens the frame); its box is fitted to the PC.</summary>
    [SerializeField] private Clickable pc;

    /// <summary>The PC's power knob click; its box sits on the knob.</summary>
    [SerializeField] private Clickable pcPower;

    [Header("Desk")]
    /// <summary>The papers' desk (optional): told which way the office camera looks over the scanner, so no paper is left where the scanner hides it.</summary>
    [SerializeField] private DeskController desk;

    /// <summary>The desk plane papers lie on.</summary>
    [SerializeField] private DeskSurface surface;

    /// <summary>The desk scanner (its click box and reaction are on the same object).</summary>
    [SerializeField] private DeskScanner scanner;

    /// <summary>The gameplay's stand-in scanner machine, shown when the art office has none.</summary>
    [SerializeField] private GameObject scannerPlaceholder;

    /// <summary>Where papers slide in from and back to.</summary>
    [SerializeField] private Transform handOver;

    /// <summary>The mat's click box (the desk view's toggle; piece 10; optional): sized over the desk's clamp area, just under its plane, so papers and props above it win the raycast.</summary>
    [SerializeField] private BoxCollider matCatcher;

    /// <summary>The desk view (piece 10; optional): posed from the art's Cinemachine camera and the mat.</summary>
    [SerializeField] private DeskView deskView;

    /// <summary>The day-1 scan note (floats over the scanner, facing the camera).</summary>
    [SerializeField] private Transform scanHint;

    [Header("Traveller")]
    /// <summary>The traveller (stood at the traveller anchor).</summary>
    [SerializeField] private TravellerView traveller;

    /// <summary>The day-1 wheel note (floats over the traveller's head, facing the camera).</summary>
    [SerializeField] private Transform wheelHint;

    [Header("AVAILABLE sign")]
    /// <summary>The AVAILABLE sign's click (GameManager's toggle); its box is fitted to the art's NEXT sign.</summary>
    [SerializeField] private Clickable readySign;

    /// <summary>The gameplay's stand-in sign, shown when the art office has none.</summary>
    [SerializeField] private GameObject readyPlaceholder;

    [Header("Props")]
    /// <summary>The desk props' click boxes.</summary>
    [SerializeField] private PropBinding[] props;

    [Header("Readouts")]
    /// <summary>Day and credits.</summary>
    [SerializeField] private OfficeReadouts readouts;

    /// <summary>The Helix River's quad, laid over the art's stability text (ReadoutStability); hidden when the art has none.</summary>
    [SerializeField] private HelixRiverMonitor deskRiver;

    /// <summary>The shift clock.</summary>
    [SerializeField] private ShiftClockReadouts clock;

    /// <summary>The fallback HUD on the overlay canvas, shown when the art office lacks a readout.</summary>
    [SerializeField] private GameObject fallbackHud;

    /// <summary>The fallback HUD's day text.</summary>
    [SerializeField] private TMP_Text hudDay;

    /// <summary>The fallback HUD's Helix River (shown when the art has no stability text).</summary>
    [SerializeField] private HelixRiverMonitor hudRiver;

    /// <summary>The fallback HUD's credits text.</summary>
    [SerializeField] private TMP_Text hudCredits;

    /// <summary>The fallback HUD's clock text.</summary>
    [SerializeField] private TMP_Text hudClock;

    [Header("Portals (the portals spec v3)")]
    /// <summary>The shift, whose day-start portals the board and the rings show and whose desk availability the AVAILABLE sign shows.</summary>
    [SerializeField] private GameManager game;

    /// <summary>The Departure Board's rows and click box (placed on the DepartureBoard anchor; hidden without one).</summary>
    [SerializeField] private DepartureBoardView board;

    /// <summary>The portal rings' effects, one per DeskConfigSO.hallPortalLayers entry, in its order (driven by AnimeHallPortalLink in the anime hall).</summary>
    [SerializeField] private PortalEffect[] portalEffects;

    /// <summary>A desk anchor's size when it has no renderers (metres, XZ).</summary>
    private static readonly Vector2 DefaultDeskSize = new Vector2(1.6f, 1f);

    /// <summary>The child a prop's tooltip hangs from.</summary>
    private const string TooltipPointName = "TooltipPoint";

    /// <summary>The PC's click box when the PC has no renderers (metres).</summary>
    private const float PcBoxSize = 0.6f;

    /// <summary>The power knob's click box (metres).</summary>
    private const float PowerKnobSize = 0.08f;

    /// <summary>The art office camera's priority; the desk view sits one above it (DeskView; audit R5-015).</summary>
    private const int OfficeCameraPriority = 100;

    /// <summary>How far above the art scanner's top its bed lies (metres), so a scanning paper clears the glass.</summary>
    private const float ScannerBedLift = 0.002f;

    /// <summary>How far above the desk a hint floats (metres).</summary>
    private const float HintHeight = 0.28f;

    /// <summary>The mat's thickness and its top's depth under the desk plane (metres): papers and props above it win the raycast.</summary>
    private const float MatThickness = 0.001f;
    private const float MatDepth = 0.001f;

    /// <summary>The raycaster's hit buffer: a point can cross every stacked paper, the scanner, a prop and the traveller.</summary>
    private const int RaycastHits = 16;

    private Dictionary<OfficeAnchorId, ResolvedAnchor> _anchors;

    /// <summary>The art office's camera, once bound.</summary>
    private Camera _office;

    /// <summary>The art's office Cinemachine camera when the office camera has a brain to blend it (the desk view needs both), else null.</summary>
    private CinemachineCamera _officeVcam;

    /// <summary>The office Cinemachine camera's field of view and pose at 16:9 (the art's, framed by the desk plan: ReadyCamera), and the aspect the lens was last fitted to (FitLens).</summary>
    private float _officeFov, _fittedAspect;
    private Quaternion _officeRotation;

    /// <summary>The desk's frame (the mat's centre on the desk's top, the office view's level axes: ArrangeArt), every bay of the desk's plan placed in it; valid once _hasFrame.</summary>
    private DeskFrame _frame;
    private bool _hasFrame;

    /// <summary>The art office's renderers (ArrangeArt): the decorations' parts and the materials the built furniture borrows.</summary>
    private Renderer[] _artRenderers = Array.Empty<Renderer>();

    /// <summary>The decorations the player moves (ArrangeArt, BindDecor), or null.</summary>
    private DeskDecor _decor;

    /// <summary>True while the painted desk replaces the art's (ArrangeArt: DeskLayoutSettings.paintedDesk and a painted desk in the scene).</summary>
    private bool _painted;

    /// <summary>The art desk's renderers the painted desk replaces (DeskLayoutSettings.paintedCutParts): hidden once everything is bound (anchors were measured on them).</summary>
    private readonly List<Renderer> _paintedCut = new List<Renderer>();

    private void Awake()
    {
        Scene art = OfficeScenes.ArtScene;
        if (art.IsValid())
            Bind(art);
        else
            SceneManager.sceneLoaded += HandleLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleLoaded;
        DeskLayout.Unbind();
        if (config != null)
            config.layout.UsePainted(null, null);
    }

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

        _office = office;
        ReadyCamera(office);
        if (deskInspect != null)
            deskInspect.SetCamera(office);
        if (guide != null)
            guide.SetCamera(office);
        if (wheel != null)
        {
            wheel.SetCamera(office);
            wheel.KeepClearOf(wheelHint != null ? wheelHint.GetComponent<TMP_Text>() : null);
        }
        foreach (OverlayCallout callout in callouts ?? Array.Empty<OverlayCallout>())
            if (callout != null)
                callout.SetCamera(office);
        if (frame != null)
            frame.DrawAfter(office);
        if (tabClearance != null)
            tabClearance.Bind(office, _officeVcam);

        Vector3 viewer = office.transform.position;
        ArrangeArt(art);
        BindPc(office);
        float deskTop = BindDesk(viewer);
        BindTraveller(viewer, deskTop);
        BindReadySign();
        BindReadouts();
        BindAvailableSign();
        BindProps();
        BindDecor();
        BindBoard();
        BindHall(art);
        HidePaintedCut();
        Report();
    }

    // -----------------------------
    // Camera
    // -----------------------------

    /// <summary>The office camera raycasts the Interactable layer (and nothing else), draws the gameplay's layers but never the desktop's, hears the game when nothing else does, and shows the office's Cinemachine camera.</summary>
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

        // An art camera culled to its own layers (the anime hall's draws only its art) would hide the papers and the traveller.
        cam.cullingMask |= OfficeLayers.GameplayMask;
        int desktop = OfficeLayers.PcDesktopLayer;
        if (desktop >= 0)
            cam.cullingMask &= ~(1 << desktop);

        if (FindAnyObjectByType<AudioListener>() == null)
            cam.gameObject.AddComponent<AudioListener>();

        Transform vcam = At(OfficeAnchorId.OfficeVCam).Transform;
        if (vcam != null && vcam.TryGetComponent(out CinemachineCamera office))
        {
            office.Priority = OfficeCameraPriority;
            if (cam.TryGetComponent(out CinemachineBrain _))
            {
                _officeVcam = office;
                _officeFov = office.Lens.FieldOfView;
                _officeRotation = office.transform.rotation;
                if (config != null)
                {
                    // The office view's framing (the desk layout spec §4, Saleh's "Yes"): a wider lens, looking further down, so the whole desk shows; FitLens applies it.
                    DeskLayoutSettings layout = config.layout;
                    if (layout.officeFieldOfView > 0f)
                        _officeFov = layout.officeFieldOfView;
                    _officeRotation = Quaternion.AngleAxis(layout.officeExtraPitch, _officeRotation * Vector3.right) * _officeRotation;
                    LensSettings lens = office.Lens;
                    lens.FieldOfView = _officeFov;
                    office.Lens = lens;
                    office.transform.rotation = _officeRotation;
                }
            }
        }
    }

    private void Update() => FitLens();

    /// <summary>
    /// The office view keeps the desk's width at a screen narrower than 16:9
    /// (leftover 2026-10-08: at 5:4 it cut off the PC and the phone): whenever
    /// the screen's aspect changes, the office Cinemachine camera's field of
    /// view widens and the camera tilts down by LensFit.Fit (its top edge
    /// kept, down to DeskConfigSO.officeViewBottom); at 16:9 and wider the
    /// lens and the pose are the desk plan's framing (DeskLayoutSettings:
    /// the art's pose looking officeExtraPitch further down, officeFieldOfView).
    /// </summary>
    private void FitLens()
    {
        if (_officeVcam == null || config == null || Screen.height <= 0)
            return;
        float aspect = (float)Screen.width / Screen.height;
        if (aspect == _fittedAspect)
            return;
        _fittedAspect = aspect;
        (float fov, float tilt) = LensFit.Fit(_officeFov, LensFit.DesignAspect, aspect, config.officeViewBottom);
        LensSettings lens = _officeVcam.Lens;
        lens.FieldOfView = fov;
        _officeVcam.Lens = lens;
        _officeVcam.transform.rotation = Quaternion.AngleAxis(tilt, _officeRotation * Vector3.right) * _officeRotation;
    }

    // -----------------------------
    // PC
    // -----------------------------

    /// <summary>The desktop's clone on the PC's glass (drawn before <paramref name="office"/>, the office camera), the PC's click box and the power knob's; the painted PC's when the painted desk shows (BindPaintedPc).</summary>
    private void BindPc(Camera office)
    {
        if (_painted && paintedDesk.Screen != null)
        {
            BindPaintedPc(office);
            return;
        }
        ResolvedAnchor anchor = At(OfficeAnchorId.PCScreen);
        (Renderer glass, int submesh) = anchor.Transform != null ? OfficeAnchors.FindGlass(anchor.Transform) : (null, 0);
        if (glass != null && screenClone != null)
            screenClone.Bind(glass, submesh, office);
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
        InFrontOf(pcPower.transform, pcBox, office.transform.position);
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
                scanner.Configure(new Vector2(spot.Bounds.size.x, spot.Bounds.size.z), new Vector3(0f, spot.Bounds.size.y + ScannerBedLift, 0f));
            }
            else if (_hasFrame)
            {
                // The gameplay's machine stands on its home on the rail right of the mat (the desk home map): its front (local -Z) and its readout toward the chair.
                _frame.Place(s, config.layout.Bay(DeskHomeMap.Scanner), 0f);
                DeskLayout.Track(DeskHomeMap.Scanner, s);
            }
            Renderer[] painted = !artScanner && _painted ? PaintScanner(s) : null;
            if (artScanner && _painted)
                paintedDesk.Hide(PaintedScanner);

            if (scanner.TryGetComponent(out BoxCollider box))
            {
                box.center = new Vector3(0f, DeskScanner.BoxHeight / 2f, 0f);
                box.size = new Vector3(scanner.DropSize.x, DeskScanner.BoxHeight, scanner.DropSize.y);
            }
            if (scanner.TryGetComponent(out Clickable scannerClick))
                scannerClick.SetOutline(artScanner ? spot.Transform.GetComponentsInChildren<Renderer>(false) : painted ?? RenderersOf(scannerPlaceholder));

            // Every corner of the drop area, so a marker scanner yawed off the desk's axes is covered whole (audit R5-010).
            Vector2 half = scanner.DropSize / 2f;
            Bounds drop = new Bounds(s.position, Vector3.zero);
            foreach (Vector3 corner in new[] { new Vector3(-half.x, 0f, -half.y), new Vector3(half.x, 0f, -half.y), new Vector3(-half.x, 0f, half.y), new Vector3(half.x, 0f, half.y) })
                drop.Encapsulate(s.TransformPoint(corner));
            area = DeskRect.Union(deskRect, new DeskRect(drop.center.x, drop.center.z, drop.size.x, drop.size.z));

            if (scanHint != null)
                FaceCamera(scanHint, scanner.BedPoint + Vector3.up * HintHeight, viewer);

            // The shadow behind the scanner its body hides from the office camera (the desk-first redesign, item 4).
            if (this.desk != null && config != null)
            {
                float body = artScanner ? spot.Bounds.size.y : scanner.MachineHeight;
                Vector3 toScanner = s.position + Vector3.up * body - viewer;
                float elevation = Mathf.Atan2(-toScanner.y, new Vector2(toScanner.x, toScanner.z).magnitude) * Mathf.Rad2Deg;
                this.desk.SetScannerView(toScanner, ScannerClearance.Shadow(body, elevation, config.scannerShadowMax));
            }
        }

        if (surface != null)
        {
            surface.transform.SetPositionAndRotation(new Vector3(area.CentreX, top, area.CentreY), Quaternion.identity);
            surface.Configure(new Vector2(area.Width, area.Height),
                              new Vector2(deskRect.CentreX - area.CentreX, deskRect.CentreY - area.CentreY),
                              new Vector2(deskRect.Width, deskRect.Height));
        }

        if (matCatcher != null)
            PlaceBox(matCatcher.transform, new Vector3(area.CentreX, top - MatDepth - MatThickness / 2f, area.CentreY), new Vector3(area.Width, MatThickness, area.Height));

        if (deskView != null)
        {
            if (_officeVcam != null)
                deskView.Bind(_officeVcam, deskCentre);
            else
                Debug.LogWarning("[OfficeSceneBinder] The art office has no Cinemachine camera with a brain on the office camera (Anchor_OfficeVCam): the desk view stays off. See docs/SCENE_CONTRACT_GAMEPLAY.md.", this);
        }

        // The counter lies along the desk's far edge in the office view's frame, ending where the desk view shows the desk (Papers, Please's zones).
        if (counter != null && _office != null)
            counter.Bind(_office.transform.forward);
        // Everything else stands on its home (the desk home map, DeskLayoutSettings): the stamp rack at the back of the rail by the counter, its daters front to back; DETAIN left of the AVAILABLE sign; the machines and the upgrades' objects, each first-day note over its machine; the deal tray right above the mat; the intercom is the art's phone on its home (a stand-in is built when the art has none).
        Transform standIn = null;
        if (_hasFrame)
        {
            DeskLayoutSettings layout = config.layout;
            if (stampTray != null)
            {
                stampTray.Lay(_frame, layout.Bay(DeskHomeMap.StampRack));
                DeskLayout.Track(DeskHomeMap.StampRack, stampTray.Rack);
            }
            if (detainButton != null)
            {
                _frame.Place(detainButton.transform, layout.Bay(DeskHomeMap.Detain), 0f);
                DeskLayout.Track(DeskHomeMap.Detain, detainButton.transform);
            }
            standIn = DeskFurniture.Build(transform, _frame, layout, MaterialNamed, OpenWheel, !_painted && At(OfficeAnchorId.Intercom).HasBounds);
            if (guide != null && standIn != null)
                guide.SetIntercom(standIn);
        }
        if (hardware != null && _hasFrame)
        {
            hardware.Lay(_frame);
            if (hardware.UvNote != null && hardware.Torch != null)
                FaceCamera(hardware.UvNote, hardware.Torch.transform.position + Vector3.up * HintHeight, viewer);
            if (hardware.ChipNote != null && hardware.Reader != null)
                FaceCamera(hardware.ChipNote, hardware.Reader.transform.position + Vector3.up * HintHeight, viewer);
        }
        if (_painted && _hasFrame)
            InstallPainted(standIn);
        DeskLayout.RefreshObstacles();
        BindSlideIn(standIn);
        if (rulebook != null && config != null && _office != null)
        {
            Vector3 level = Vector3.ProjectOnPlane(_office.transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, level);
            Vector3 at = new Vector3(deskCentre.x, top, deskCentre.z) + right * config.rulebookAt.x + level * config.rulebookAt.y; // on the desk plane: the stack lifts it (DeskController)
            rulebook.Place(at, Quaternion.LookRotation(level, Vector3.up));
        }

        // The papers come in and go back over the deal tray (Saleh, DESK APPROVED); without its home, from the art's hand-over point.
        if (handOver != null)
        {
            if (DeskLayout.TryPose(DeskHomeMap.DealTray, out Vector3 tray, out _))
            {
                handOver.position = new Vector3(tray.x, top, tray.z);
            }
            else
            {
                Vector3 from = At(OfficeAnchorId.HandOver).Position;
                handOver.position = new Vector3(from.x, top, from.z);
            }
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
    // AVAILABLE sign, props, readouts
    // -----------------------------

    private void BindReadySign()
    {
        if (readySign == null)
            return;

        if (_painted && paintedDesk.TryBounds(PaintedSign, out Bounds painted))
        {
            // The painted desk's AVAILABLE sign (its word is painted; DeskPaintedMap: the sign's fixed art).
            if (readyPlaceholder != null)
                readyPlaceholder.SetActive(false);
            PlaceBox(readySign.transform, painted.center, painted.size);
            readySign.SetOutline(paintedDesk.RenderersOf(PaintedSign));
            return;
        }
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
            if (!anchor.HasBounds || IsCut(anchor.Transform))
            {
                prop.click.gameObject.SetActive(false);
                continue;
            }

            PlaceBox(prop.click, anchor, Vector3.zero);
            if (prop.anchor == OfficeAnchorId.Intercom)
                BindIntercom(prop.click);
            if (prop.click.TryGetComponent(out DeskReaction reaction))
            {
                reaction.SetTarget(anchor.Transform);
                reaction.SetTooltipPoint(TopOf(prop.click.transform, anchor.Bounds));
                if (prop.hasReadout)
                    reaction.SetReadout(TextOf(prop.readout));
            }
        }
    }

    /// <summary>Binds each readout to the art's text, or to the fallback HUD's when the art has none (the HUD shows only then); the Helix River covers the art's stability text, or shows in the HUD.</summary>
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
        TMP_Text credits = Pick(OfficeAnchorId.ReadoutCredits, hudCredits);
        TMP_Text time = Pick(OfficeAnchorId.ReadoutClock, hudClock);
        if (readouts != null)
            readouts.Bind(day, credits);
        if (clock != null)
            clock.Bind(time);

        TMP_Text stabilityScreen = TextOf(OfficeAnchorId.ReadoutStability);
        GrowStabilityMonitor(stabilityScreen);
        if (deskRiver != null)
        {
            deskRiver.Cover(stabilityScreen);
            deskRiver.gameObject.SetActive(stabilityScreen != null);
        }
        if (hudRiver != null)
        {
            hudRiver.gameObject.SetActive(stabilityScreen == null);
            hud |= stabilityScreen == null;
        }

        foreach (TMP_Text unused in new[] { hudDay, hudCredits, hudClock })
            if (unused != null)
                unused.gameObject.SetActive(unused == day || unused == credits || unused == time);
        if (fallbackHud != null)
            fallbackHud.SetActive(hud);
    }

    /// <summary>
    /// The stability monitor drawn big enough to read the Helix River on
    /// (Saleh 2026-10-07: "the timeline helix needs to be bigger; the screen
    /// itself is too small"): the art's monitor parts (the StabilityMonitor
    /// anchor and its siblings named with its prefix, "Office_Stability__")
    /// and its stability text grow DeskConfigSO.stabilityMonitorScale times
    /// about DeskConfigSO.stabilityMonitorPivot of the monitor's bounds, at
    /// load (the art scene is never edited); the river then covers the grown
    /// glass (HelixRiverMonitor.Cover), and the monitor's click box and
    /// tooltip take its new bounds.
    /// </summary>
    private void GrowStabilityMonitor(TMP_Text readout)
    {
        ResolvedAnchor anchor = At(OfficeAnchorId.StabilityMonitor);
        Transform monitor = anchor.Transform;
        float k = config != null ? config.stabilityMonitorScale : 1f;
        if (monitor == null || monitor.parent == null || readout == null || !anchor.HasBounds || Mathf.Approximately(k, 1f))
            return;

        int cut = monitor.name.IndexOf("__", StringComparison.Ordinal);
        string prefix = cut > 0 ? monitor.name.Substring(0, cut + 2) : monitor.name;
        var parts = new List<Transform>();
        foreach (Transform part in monitor.parent)
            if (part.name.StartsWith(prefix, StringComparison.Ordinal))
                parts.Add(part);
        if (!parts.Exists(p => readout.transform.IsChildOf(p)))
            parts.Add(readout.transform);

        Bounds b = anchor.Bounds;
        foreach (Transform part in parts)
            if (OfficeAnchors.TryBounds(part, out Bounds more))
                b.Encapsulate(more);
        Vector2 share = config.stabilityMonitorPivot;
        Vector3 pivot = new Vector3(Mathf.Lerp(b.min.x, b.max.x, share.x), Mathf.Lerp(b.min.y, b.max.y, share.y), b.center.z);
        foreach (Transform part in parts)
        {
            part.position = pivot + (part.position - pivot) * k;
            part.localScale *= k;
        }
        anchor.HasBounds = OfficeAnchors.TryBounds(monitor, out anchor.Bounds);
    }

    /// <summary>The AVAILABLE sign's caption and lit or paused ink follow the desk's availability (AvailableSignLink): on the art's NEXT label, else on the stand-in sign's when it shows.</summary>
    private void BindAvailableSign()
    {
        if (game == null || config == null)
            return;

        TMP_Text caption = TextOf(OfficeAnchorId.ReadoutNext);
        if (caption == null && readyPlaceholder != null && readyPlaceholder.activeSelf)
            caption = readyPlaceholder.GetComponentInChildren<TMP_Text>(true);
        if (caption != null)
            gameObject.AddComponent<AvailableSignLink>().Configure(caption, config, game.Desk);
    }

    // -----------------------------
    // The anime hall
    // -----------------------------

    /// <summary>The Departure Board's rows and click box on the DepartureBoard anchor's rect (a sized marker, or the display layer's opaque pixels); hidden without one (the 3D room).</summary>
    private void BindBoard()
    {
        if (board == null)
            return;
        ResolvedAnchor anchor = At(OfficeAnchorId.DepartureBoard);
        if (!OfficeAnchors.TryLocalRect(anchor.Transform, out Transform frame, out Rect rect))
        {
            board.gameObject.SetActive(false);
            return;
        }
        board.Bind(frame, rect, anchor.Transform.GetComponentsInChildren<Renderer>(false));
    }

    /// <summary>The anime hall's lights (HallLightingRig, added by Add Anime Hall Hooks), or null when the hall has none.</summary>
    private static HallLightingRig FindLights(Scene art)
    {
        foreach (GameObject root in art.GetRootGameObjects())
        {
            HallLightingRig rig = root.GetComponentInChildren<HallLightingRig>(false);
            if (rig != null)
                return rig;
        }
        return null;
    }

    /// <summary>When the art office carries an anime hall presentation, a link on this object drives its time from the shift clock (and the hall's lights, their day-night cycle, and the traveller's shade with it), and the ink of the art's calendar with it (the config's hall knobs), and a second link its portal rings (and their lights) from the day's portals, a third its swappable slots from the hall's variables (HallSlotsLink, when the config names a HallSlotsSO), and the city view is bound to it (its left pan, its living city); an art office without one needs nothing (the city view stays off).</summary>
    private void BindHall(Scene art)
    {
        if (config == null)
            return;

        foreach (GameObject root in art.GetRootGameObjects())
        {
            AnimeHallPresentation hall = root.GetComponentInChildren<AnimeHallPresentation>(false);
            if (hall == null)
                continue;
            HallLightingRig lights = FindLights(art);
            gameObject.AddComponent<AnimeHallShiftLink>().Configure(hall, config, TextOf(OfficeAnchorId.ReadoutDay), lights, traveller);
            gameObject.AddComponent<AnimeHallPortalLink>().Configure(hall, config, game, portalEffects, lights);
            if (config.hallSlots != null)
                gameObject.AddComponent<HallSlotsLink>().Configure(hall, config.hallSlots);
            if (cityView != null)
                cityView.Bind(hall, hall.GetComponentInChildren<HallCityExterior>(false), lights);
            return;
        }
    }

    // -----------------------------
    // Helpers
    // -----------------------------

    private ResolvedAnchor At(OfficeAnchorId id) => _anchors[id];

    // -----------------------------
    // The desk's plan
    // -----------------------------

    /// <summary>
    /// The art's desk arranged to the desk home map before anything is bound
    /// to it (the art scene is never edited, its objects are moved at run
    /// time): the desk's frame from the mat and the office view; the art's
    /// cut props switched off (DeskLayoutSettings.cutParts: Saleh's "we will
    /// cut all but decor objects"); the desk bound (DeskLayout: the frame,
    /// the art mat, the homes); the art objects the map moves onto their
    /// homes (the phone as the intercom, the mouse), shrunk to fit when
    /// bigger; and the decorations to where the player put them or the decor
    /// home (DeskDecor). The anchors are resolved again when anything moved,
    /// so the props' click boxes follow them.
    /// </summary>
    private void ArrangeArt(Scene art)
    {
        if (config == null || _office == null)
            return;
        ResolvedAnchor mat = At(OfficeAnchorId.DeskSurface);
        Vector3 centre = mat.HasBounds ? new Vector3(mat.Bounds.center.x, mat.Bounds.max.y, mat.Bounds.center.z) : mat.Position;
        _frame = new DeskFrame(centre, _office.transform.forward);
        _hasFrame = true;
        _artRenderers = art.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Renderer>(true)).ToArray();

        DeskLayoutSettings layout = config.layout;
        _painted = layout.paintedDesk && paintedDesk != null;
        if (paintedDesk != null)
            paintedDesk.gameObject.SetActive(_painted);
        layout.UsePainted(_painted ? paintedDesk.Homes : null, _painted ? paintedDesk.FixedArt : null);
        _paintedCut.Clear();
        if (_painted)
        {
            paintedDesk.Align(_frame);
            foreach (Renderer r in _artRenderers)
                if (r != null && layout.paintedCutParts.Any(prefix => !string.IsNullOrEmpty(prefix) && r.name.StartsWith(prefix, StringComparison.Ordinal)))
                    _paintedCut.Add(r);
        }
        bool moved = CutArtProps(layout.cutParts);
        DeskLayout.Bind(_frame, mat.HasBounds ? _frame.Footprint(mat.Bounds) : layout.mat.Rect, layout);
        foreach (DeskHome home in layout.Homes ?? Array.Empty<DeskHome>())
            if (home != null && !string.IsNullOrEmpty(home.artParts) && MoveArtProp(home))
            {
                moved = true;
                DeskLayout.Register(home.id, home.Footprint, home.height);
            }
        _decor = gameObject.AddComponent<DeskDecor>();
        moved |= _decor.Arrange(_frame, layout, _artRenderers);
        if (moved)
            _anchors = OfficeAnchors.Resolve(art, contract);
    }

    /// <summary>Switches off the art's parts named "prefix__…" for every prefix of <paramref name="prefixes"/> (the cut props); true when any was on.</summary>
    private bool CutArtProps(IReadOnlyList<string> prefixes)
    {
        bool cut = false;
        foreach (Renderer r in _artRenderers)
        {
            if (r == null || !r.gameObject.activeSelf)
                continue;
            foreach (string prefix in prefixes ?? Array.Empty<string>())
                if (!string.IsNullOrEmpty(prefix) && r.name.StartsWith(prefix + "__", StringComparison.Ordinal))
                {
                    r.gameObject.SetActive(false);
                    cut = true;
                    break;
                }
        }
        return cut;
    }

    /// <summary>Moves the art's object made of the parts named "<paramref name="home"/>.artParts__…" (as one) onto its home: its footprint's centre on the home's, its base on the desk, shrunk about its base to fit the home's footprint when bigger; true when it was found.</summary>
    private bool MoveArtProp(DeskHome home)
    {
        string prefix = home.artParts;
        List<Transform> parts = _artRenderers.Where(r => r != null && r.gameObject.activeSelf && r.name.StartsWith(prefix + "__", StringComparison.Ordinal))
                                             .Select(r => r.transform).Distinct().ToList();
        if (parts.Count == 0)
            return false;
        Bounds b = parts[0].GetComponent<Renderer>().bounds;
        foreach (Transform part in parts)
            b.Encapsulate(part.GetComponent<Renderer>().bounds);
        List<Transform> tops = parts.Where(part => !parts.Exists(other => other != part && part.IsChildOf(other))).ToList();

        DeskRect have = _frame.Footprint(b), room = home.Footprint;
        float k = 1f;
        if (have.Width > room.Width && have.Width > 1e-5f)
            k = Mathf.Min(k, room.Width / have.Width);
        if (have.Height > room.Height && have.Height > 1e-5f)
            k = Mathf.Min(k, room.Height / have.Height);
        var pivot = new Vector3(b.center.x, b.min.y, b.center.z);
        if (k < 1f)
            foreach (Transform part in tops)
            {
                part.position = pivot + (part.position - pivot) * k;
                part.localScale *= k;
            }

        Vector3 to = _frame.Point(new Vector2(home.x, home.y));
        Vector3 by = new Vector3(to.x - pivot.x, to.y - pivot.y, to.z - pivot.z);
        foreach (Transform part in tops)
            part.position += by;
        return true;
    }

    /// <summary>
    /// The slide-in (desk v3, Saleh: "yes but they need to slide"; DESK-HOMES):
    /// the desk's DeskSlideIn on the office camera with the plan's slides and
    /// homes (nothing near while the PC is open; the forced retract when the
    /// day is over: the DayOrchestrator's OnDayCompleted, never at closing
    /// time itself, while the last traveller may still be served; the contact
    /// shadows in the papers' own shadow material, the rulebook's), today's
    /// sliders on their homes, each from where it stands: the gameplay scanner
    /// (an art office's own scanner stays put: its meshes are the art's),
    /// busy while its outside shows it occupied (Occupied), and the chip
    /// reader, busy while it reads, each the dock its paper rides home from;
    /// the intercom when it is the built stand-in and its home declares a
    /// slide (the painted desk's map declares it, and the painted intercom
    /// rides the stand-in; the art office's phone is loose parts with no slide
    /// transform: it stays put, said in the log); the BABEL rack, the Rolodex
    /// and the protocol cards; the UV torch's body, a layer over the pose
    /// UvTorch gives it, giving way when it is picked up; with the painted
    /// desk, its glasses in their stand (they slide on their own until
    /// GLASSES gives them an object). Not here: the daters (STAMP #7 lifts and
    /// tips them itself: their slide waits for its merge) and the rulebook
    /// (its pocket is not built yet).
    /// </summary>
    private void BindSlideIn(Transform intercomStandIn)
    {
        if (!_hasFrame || config == null || _office == null)
            return;
        if (!TryGetComponent(out DeskSlideIn slideIn))
            slideIn = gameObject.AddComponent<DeskSlideIn>();
        Transform paperShadow = rulebook != null ? Descendant(rulebook.transform, "Shadow") : null;
        Material shadow = paperShadow != null && paperShadow.TryGetComponent(out Renderer shade) ? shade.sharedMaterial : null;
        if (shadow == null)
            Debug.LogWarning("[OfficeSceneBinder] The rulebook has no Shadow part (Build Office UI's PaperDepth shadow), so the slide-in draws no contact shadows.", this);
        DeskSlides slides = config.layout.slides;
        slideIn.Bind(_office, slides, config.layout.Homes, desk, () => (frame != null && frame.IsOpen) || InputGate.Blocked, FindAnyObjectByType<DayOrchestrator>(), shadow);
        if (scanner != null && DeskLayout.ObjectOf(DeskHomeMap.Scanner) == scanner.transform)
            MachineSlideIn.Attach(DeskHomeMap.Scanner, scanner.transform, busy: Occupied(scanner.transform, slides), dock: scanner);
        ChipReader reader = hardware != null ? hardware.Reader : null;
        if (reader != null)
            MachineSlideIn.Attach(DeskHomeMap.ChipReader, reader.transform, busy: () => reader.Busy, dock: reader);
        if (intercomStandIn != null)
            MachineSlideIn.Attach(DeskHomeMap.Intercom, intercomStandIn);
        else if (DeskLayout.ObjectOf(DeskHomeMap.Intercom) != null && slides.Find(DeskHomeMap.Intercom) is DeskSlider intercom && intercom.MovesOn(DeskLayout.HomeOf(DeskHomeMap.Intercom)))
            Debug.LogWarning("[OfficeSceneBinder] The intercom's home declares a slide, but the intercom is the art's phone (loose parts, no Slide transform): it stays put (DESK-PLAN open call 1, DESK-ART's painted intercom).", this);
        foreach (string id in new[] { DeskHomeMap.BabelTuner, DeskHomeMap.Rolodex, DeskHomeMap.ProtocolRing })
            MachineSlideIn.Attach(id, DeskLayout.ObjectOf(id));
        UvTorch torch = hardware != null ? hardware.Torch : null;
        Transform body = torch != null ? Descendant(torch.transform, "UvTorch") : null;
        if (body != null)
            MachineSlideIn.Attach(DeskHomeMap.UvTorch, torch.transform, body, layered: true, taken: () => torch.Held);
        if (_painted)
            MachineSlideIn.Attach(DeskHomeMap.Glasses, paintedDesk.SliderOf(PaintedGlasses));
    }

    /// <summary>
    /// The scanner's occupancy as its outside shows it (SlideOccupancy): its
    /// scan light (the art's "SweepBar", the builder's "Spill": DeskScanner
    /// shows them only while a scan runs) and its "Lid" standing open past
    /// DeskSlides.lidOpenDegrees from where it rests now (shut), while no
    /// paper is held: a queued or Auto-Fed paper carried onto the bed (the
    /// sweep hides while it slides) and the moments after a scan when the next
    /// may follow. A stand-in read until DOC-HANDLE exposes the scanner's own
    /// state (DeskScanner keeps it private); docs/pending/DESK-V3.md hands it
    /// over.
    /// </summary>
    private Func<bool> Occupied(Transform scanner, DeskSlides slides)
    {
        Transform bar = Descendant(scanner, "SweepBar"), spill = Descendant(scanner, "Spill"), lid = Descendant(scanner, "Lid");
        Quaternion shut = lid != null ? lid.localRotation : Quaternion.identity;
        var occupancy = new SlideOccupancy();
        return () => occupancy.Step((bar != null && bar.gameObject.activeInHierarchy) || (spill != null && spill.gameObject.activeInHierarchy),
                                    lid != null && Quaternion.Angle(lid.localRotation, shut) > slides.lidOpenDegrees,
                                    desk != null && desk.IsDragging);
    }

    // -----------------------------
    // The painted desk (desk v3, Saleh: "yes but they need to slide"; UNITY_PLAN 4 and 8)
    // -----------------------------

    /// <summary>The bake's objects the install hands to the gameplay objects or binds itself.</summary>
    private const string PaintedScanner = "Desk_Scanner", PaintedReader = "Desk_ChipReader", PaintedIntercom = "Desk_Intercom", PaintedTorch = "Desk_UvTorch",
                         PaintedDetain = "Desk_Detain", PaintedGuard = "Desk_DetainGuard", PaintedGlasses = "Desk_Glasses", PaintedBell = "Desk_Bell",
                         PaintedPocket = "Desk_Pocket", PaintedPc = "PC_Crt", PaintedSign = "Desk_AvailableSign";

    /// <summary>The gameplay parts that stay drawn under a painted look: the scanner's scan light and its upgrades' add-ons, the UV torch's pool and beam.</summary>
    private static readonly string[] ScannerKept = { "Spill", "SweepBar", "AutoFeed", "AnalysisBeacon" }, TorchKept = { "UvPool", "BeamFX" };

    /// <summary>
    /// The painted scanner on the gameplay scanner <paramref name="s"/> (laid
    /// on its painted home): it stands on the painted one's base, takes its
    /// look (DeskPainted.Reskin: the stand-in machine hides, the painted one
    /// rides it; its scan light and add-ons stay) and its bed on the painted
    /// body's top, over the body's centre. Returns the painted renderers.
    /// </summary>
    private Renderer[] PaintScanner(Transform s)
    {
        StandOnPainted(DeskHomeMap.Scanner, s);
        Renderer[] look = paintedDesk.Reskin(PaintedScanner, s, s, ScannerKept);
        Transform body = paintedDesk.Mesh(PaintedScanner);
        if (body != null && body.TryGetComponent(out Renderer shell))
        {
            Bounds b = shell.bounds;
            scanner.Configure(scanner.DropSize, s.InverseTransformPoint(new Vector3(b.center.x, b.max.y + ScannerBedLift, b.center.z)));
        }
        return look;
    }

    /// <summary>
    /// The painted desk takes over the laid desk (its homes are the map
    /// already: ArrangeArt). What it does not show yet hides (PaintedRole.Hidden:
    /// STAMP's own rack and daters stand on the painted tray, the folded
    /// rulebook waits for DESK-HOMES' pocket, the glass's sheen for its
    /// repaint). Each gameplay object stands on its painted home's floor
    /// (StandOnPainted) and the painted look rides it: the chip reader, DETAIN
    /// and its guard, the stand-in intercom (its mic a direct child, so the
    /// slide sways it; its click box fitted to the painted intercom), the UV
    /// torch's body (its pool and beam kept). STAMP's rack stands on its
    /// painted tray and the upgrades' objects in their locked homes; the
    /// built deal tray's plate hides (the painted desk has its tray). The
    /// painted objects no gameplay object stands for (the glasses, the bell,
    /// the rulebook's pocket) are tracked on their homes (obstacles; the
    /// glasses slide: BindSlideIn).
    /// </summary>
    private void InstallPainted(Transform intercomStandIn)
    {
        foreach (PaintedObject o in DeskPaintedMap.Objects)
            if (o.role == PaintedRole.Hidden)
                paintedDesk.Hide(o.name);
        ChipReader reader = hardware != null ? hardware.Reader : null;
        if (reader != null)
        {
            StandOnPainted(DeskHomeMap.ChipReader, reader.transform);
            paintedDesk.Reskin(PaintedReader, reader.transform, reader.transform);
        }
        UvTorch torch = hardware != null ? hardware.Torch : null;
        Transform body = torch != null ? Descendant(torch.transform, "UvTorch") : null;
        if (body != null)
            paintedDesk.Reskin(PaintedTorch, torch.transform, body, TorchKept);
        if (detainButton != null)
        {
            StandOnPainted(DeskHomeMap.Detain, detainButton.transform);
            paintedDesk.Reskin(PaintedDetain, detainButton.transform, detainButton.transform);
            paintedDesk.Reskin(PaintedGuard, detainButton.transform, detainButton.transform);
        }
        if (intercomStandIn != null)
        {
            StandOnPainted(DeskHomeMap.Intercom, intercomStandIn);
            Renderer[] look = paintedDesk.Reskin(PaintedIntercom, intercomStandIn, intercomStandIn);
            paintedDesk.MoveChild(PaintedIntercom, DeskPaintedMap.Mic, intercomStandIn);
            FitBox(intercomStandIn, look);
        }
        if (stampTray != null)
            StandOnPainted(DeskHomeMap.StampRack, stampTray.Rack);
        foreach (string id in DeskPaintedMap.Inserts)
            StandOnPainted(id, DeskLayout.ObjectOf(id));
        Transform tray = transform.Find("DeskFurniture/DealTray");
        if (tray != null)
            foreach (Renderer r in tray.GetComponentsInChildren<Renderer>(true))
                r.enabled = false;
        DeskLayout.Track(DeskHomeMap.Glasses, paintedDesk.SliderOf(PaintedGlasses));
        DeskLayout.Track(DeskHomeMap.Bell, paintedDesk.Mesh(PaintedBell));
        DeskLayout.Track(DeskHomeMap.RulebookPocket, paintedDesk.Mesh(PaintedPocket));
    }

    /// <summary>Lifts the gameplay object <paramref name="t"/>, laid on the desk's top, onto the painted home <paramref name="home"/>'s floor (DeskPainted.TryFloor: its painted object's base, or its locked insert's top).</summary>
    private void StandOnPainted(string home, Transform t)
    {
        if (t != null && paintedDesk.TryFloor(home, out float floor))
            t.position += Vector3.up * (floor - _frame.Origin.y);
    }

    /// <summary>Fits <paramref name="host"/>'s click box (its BoxCollider) around <paramref name="look"/>'s renderers.</summary>
    private static void FitBox(Transform host, Renderer[] look)
    {
        if (look == null || look.Length == 0 || !host.TryGetComponent(out BoxCollider box))
            return;
        Bounds b = look[0].bounds;
        foreach (Renderer r in look)
            b.Encapsulate(r.bounds);
        Vector3 size = host.InverseTransformVector(b.size);
        box.center = host.InverseTransformPoint(b.center);
        box.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
    }

    /// <summary>The painted PC (the painted desk shows): the desktop's clone on its screen quad, its click box on the CRT, the power knob's where the clone's frame puts it, in front of the CRT's box.</summary>
    private void BindPaintedPc(Camera office)
    {
        if (screenClone != null)
            screenClone.Bind(paintedDesk.Screen, 0, office);
        bool crt = paintedDesk.TryBounds(PaintedPc, out Bounds box);
        if (pc != null && crt)
        {
            PlaceBox(pc.transform, box.center, box.size);
            pc.SetOutline(paintedDesk.RenderersOf(PaintedPc));
        }
        if (pcPower == null)
            return;
        if (screenClone == null || !screenClone.IsBound)
        {
            pcPower.gameObject.SetActive(false);
            return;
        }
        PlaceBox(pcPower.transform, screenClone.Frame.PowerKnob, Vector3.one * PowerKnobSize);
        if (crt)
            InFrontOf(pcPower.transform, box, office.transform.position);
    }

    /// <summary>True when the painted desk shows and <paramref name="t"/> is (or holds) an art desk part it replaces (DeskLayoutSettings.paintedCutParts): its click box, tooltip and readout go.</summary>
    private bool IsCut(Transform t) => _painted && t != null && _paintedCut.Exists(r => r != null && r.transform.IsChildOf(t));

    /// <summary>Hides the art desk's parts the painted desk replaces (last, once every anchor was measured on them).</summary>
    private void HidePaintedCut()
    {
        foreach (Renderer r in _paintedCut)
            if (r != null)
                r.enabled = false;
    }

    /// <summary>The first descendant of <paramref name="root"/> (not itself) named <paramref name="name"/>, or null.</summary>
    private static Transform Descendant(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t != root && t.name == name)
                return t;
        return null;
    }

    /// <summary>The decorations made draggable once the props are bound: a prop's click box (its reaction) goes with its decoration (DeskDecor.MakeHandles).</summary>
    private void BindDecor()
    {
        if (_decor == null)
            return;
        var boxes = new List<KeyValuePair<Transform, Collider>>();
        foreach (PropBinding prop in props ?? Array.Empty<PropBinding>())
            if (prop != null && prop.click != null && prop.click.gameObject.activeSelf && prop.click.TryGetComponent(out Collider box))
                boxes.Add(new KeyValuePair<Transform, Collider>(At(prop.anchor).Transform, box));
        _decor.MakeHandles(boxes);
    }

    /// <summary>The art office's material named <paramref name="name"/> (the built furniture's, DeskFurniture), else the mat's.</summary>
    private Material MaterialNamed(string name)
    {
        foreach (Renderer r in _artRenderers)
            if (r != null)
                foreach (Material m in r.sharedMaterials)
                    if (m != null && m.name == name)
                        return m;
        Transform mat = At(OfficeAnchorId.DeskSurface).Transform;
        return mat != null && mat.TryGetComponent(out Renderer own) ? own.sharedMaterial : null;
    }

    /// <summary>The intercom's click: the questions open where the player is (the traveller wheel).</summary>
    private void OpenWheel()
    {
        if (wheel != null)
            wheel.Open();
    }

    /// <summary>The art's phone, moved onto the intercom's home, is the intercom (Saleh's D5: "intercom to bring out dialogue options"; FIX_PLAN_v2: the art-scene model is the intercom's fallback art): its click box asks the traveller where the player is, and the guide points at it.</summary>
    private void BindIntercom(Clickable click)
    {
        click.onClick.RemoveListener(OpenWheel);
        click.onClick.AddListener(OpenWheel);
        if (guide != null)
            guide.SetIntercom(click.transform);
        DeskLayout.Track(DeskHomeMap.Intercom, click.transform);
    }

    /// <summary>The art's text at the anchor <paramref name="id"/>, or null (none, or a part the painted desk replaces: its readout goes to the fallback HUD).</summary>
    private TMP_Text TextOf(OfficeAnchorId id)
    {
        Transform t = At(id).Transform;
        return t != null && !IsCut(t) ? t.GetComponent<TMP_Text>() : null;
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

    /// <summary>One log line for the anchors found by fallback paths, one warning for those on default poses or missing (an anchor the contract lists no spec for, the cut old stamp's, is wanted by nothing and left out of it).</summary>
    private void Report()
    {
        List<ResolvedAnchor> fallbacks = _anchors.Values.Where(a => a.Source == AnchorSource.Fallback).ToList();
        List<ResolvedAnchor> stand = _anchors.Values.Where(a => (a.Source == AnchorSource.Default || a.Source == AnchorSource.Missing) && (contract == null || contract.Spec(a.Id) != null)).ToList();
        if (fallbacks.Count > 0)
            Debug.Log($"[OfficeSceneBinder] Office contract: {fallbacks.Count} anchor(s) found by fallback paths ({string.Join(", ", fallbacks.Select(a => $"{a.Id} = '{a.Path}'"))}).", this);
        if (stand.Count > 0)
            Debug.LogWarning($"[OfficeSceneBinder] The art office has no {string.Join(", ", stand.Select(a => $"{OfficeContract.AnchorName(a.Id)} ({(a.Source == AnchorSource.Default ? "default pose" : "left out")})"))}. Add them with Tools > TimeDesk > Add Gameplay Anchors (docs/SCENE_CONTRACT_GAMEPLAY.md).", this);
    }
}
