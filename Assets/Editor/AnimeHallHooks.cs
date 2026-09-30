using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tools > TimeDesk > Add Anime Hall Hooks (art office): gives the anime hall
/// (AnimeHall.unity, art c75e1fe) what docs/SCENE_CONTRACT_GAMEPLAY.md says it
/// must carry for the game to be whole, in the open hall (edit mode, the
/// active scene, the art office RunConfig.officeSceneName names):
/// the five readouts as world-space TextMeshPro texts on the boards' display
/// faces, named as the contract finds them, under GameplayAnchors, which it
/// keeps the scene's first root so they come before the preserved desk's empty
/// namesakes in hierarchy order; the four static "Preview display" TextMeshes
/// they replace deleted; the desk view's Cinemachine camera (the contract's
/// Cameras/OfficeVCam, framing the office as the hall's camera does, its lens
/// copied) and a CinemachineBrain on that camera, tagged MainCamera; the
/// hall's daylight lighting the gameplay's layers (Default, Interactable);
/// the Departure Board's marker (GameplayAnchors/Anchor_DepartureBoard: an
/// empty RectTransform over the board display layer's opaque pixels, its
/// sprite's physics shape, which the gameplay draws the day's portal rows on;
/// the portals spec v3 BD1, authorised by Saleh 2026-09-30); the hall's
/// lights, shadows and dust through a day-night cycle (AnimeHallLightingHooks:
/// the HallLighting root, its knobs, the painted layers lit through the 2D
/// Renderer; docs/HALL_LIGHTING.md, Saleh 2026-09-30);
/// and, through Add Gameplay Anchors, an anchor for every place on its default
/// pose (the scanner, the traveller, the hand-over point). What exists is left
/// alone, so a second run changes nothing. The scene is marked dirty, not
/// saved. Run by the gameplay side once with Saleh's OK (2026-09-29); the art
/// side owns the hall and may move or restyle what it adds (the camera then
/// follows OfficeVCam). Reinstalling the hall's art (AnimeHallLayerInstaller)
/// rebuilds the scene without them: run it again.
/// </summary>
public static class AnimeHallHooks
{
    /// <summary>The anime hall's scene; the readouts' poses below are measured on its boards.</summary>
    public const string HallPath = "Assets/Art/Office/AnimeHallLayers/AnimeHall.unity";

    /// <summary>The prefix of the static preview texts the art's TerminalProduction.Label puts on the boards ("Preview display — 09:00").</summary>
    private const string PreviewPrefix = "Preview display — ";

    /// <summary>The tool's undo step.</summary>
    private const string UndoName = "Add anime hall hooks";

    /// <summary>The tag Camera.main finds.</summary>
    private const string MainCameraTag = "MainCamera";

    /// <summary>Dark digits on a light card: the art's display black (its 211F26 glass).</summary>
    private static readonly Color Ink = new Color(0.129f, 0.122f, 0.149f);

    /// <summary>Light digits on a dark display: the art's ivory (its FFF2D9 plastic).</summary>
    private static readonly Color Glow = new Color(1f, 0.949f, 0.851f);

    /// <summary>The smallest size a readout shrinks to for a longer value, as a share of its size.</summary>
    private const float MinSizeShare = 0.5f;

    /// <summary>One readout: the contract's text, where it sits on its board's display face, how it is drawn, and what it shows in edit mode.</summary>
    private readonly struct Readout
    {
        /// <summary>The contract's readout (the text is named as the contract finds it).</summary>
        public readonly OfficeAnchorId Id;

        /// <summary>The text's centre in world space, just in front of the face.</summary>
        public readonly Vector3 Position;

        /// <summary>The face's heading in degrees around world up (0 faces the office camera).</summary>
        public readonly float Yaw;

        /// <summary>The text's box on the face (metres).</summary>
        public readonly Vector2 Box;

        /// <summary>The TextMeshPro size (1 is a 0.1 m em); the most it grows to.</summary>
        public readonly float Size;

        /// <summary>True on a light card (dark ink), false on a dark display (light digits).</summary>
        public readonly bool OnLightCard;

        /// <summary>The text in edit mode (the game writes its own at load).</summary>
        public readonly string Sample;

        /// <summary>Creates a readout.</summary>
        public Readout(OfficeAnchorId id, Vector3 position, float yaw, Vector2 box, float size, bool onLightCard, string sample)
        {
            Id = id;
            Position = position;
            Yaw = yaw;
            Box = box;
            Size = size;
            OnLightCard = onLightCard;
            Sample = sample;
        }
    }

    /// <summary>
    /// The five readouts, measured on the hall's boards (art c75e1fe): each at the
    /// pose of the static preview text it replaces, 6 to 10 mm in front of its
    /// face, at the preview's size (the till, which had none: at its display
    /// glass, where the preserved desk's empty CreditsNumber stands, 2 mm in
    /// front); each box inside its face: the calendar's paper (0.21 × 0.31 m, the
    /// box under its trim), the clock's glass (0.40 × 0.13 m), the stability
    /// monitor's (0.29 × 0.16 m), the till's (0.19 × 0.065 m) and the NEXT sign's
    /// (0.44 × 0.13 m). Dark ink on the calendar's paper, light digits on the
    /// dark glasses, as the art draws them.
    /// </summary>
    private static readonly Readout[] Readouts =
    {
        new Readout(OfficeAnchorId.ReadoutDay, new Vector3(-2.4457f, 1.872f, 0.5584f), -30f, new Vector2(0.19f, 0.27f), 1.66f, true, "01"),
        new Readout(OfficeAnchorId.ReadoutStability, new Vector3(2.277f, 2.364f, 0.5462f), 30f, new Vector2(0.27f, 0.14f), 0.98f, false, "100%"),
        new Readout(OfficeAnchorId.ReadoutCredits, new Vector3(-1.0398f, 1.3036f, 0.4021f), -5f, new Vector2(0.18f, 0.065f), 0.56f, false, "50"),
        new Readout(OfficeAnchorId.ReadoutClock, new Vector3(-2.277f, 2.316f, 0.5462f), -30f, new Vector2(0.38f, 0.12f), 0.85f, false, "09:00"),
        new Readout(OfficeAnchorId.ReadoutNext, new Vector3(0.13f, 1.195f, 0.103f), 0f, new Vector2(0.42f, 0.12f), 0.83f, false, "NEXT"),
    };

    /// <summary>Adds the hall's missing hooks (see the class summary); logs what it added and deleted.</summary>
    [MenuItem("Tools/TimeDesk/Add Anime Hall Hooks (art office)")]
    public static void Add()
    {
        Scene hall = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || hall.path != HallPath || ArtOfficeScene.Path != HallPath)
        {
            Debug.LogError($"[AnimeHallHooks] Open {HallPath} (edit mode, the active scene, the art office RunConfig.officeSceneName names) to add its hooks. Nothing was changed.");
            return;
        }

        var contract = AssetDatabase.LoadAssetAtPath<OfficeSceneContractSO>(OfficeSceneContractTools.ContractPath);
        Transform office = contract != null ? OfficeAnchors.Resolve(hall, contract.Spec(OfficeAnchorId.OfficeCamera), OfficeAnchorId.OfficeCamera).Transform : null;
        Camera camera = office != null ? office.GetComponent<Camera>() : null;
        if (camera == null)
        {
            Debug.LogError($"[AnimeHallHooks] {(contract == null ? $"No contract at {OfficeSceneContractTools.ContractPath}: run Tools > TimeDesk > Build Office UI first" : "The hall has no office camera")}. Nothing was changed.");
            return;
        }

        var changes = new List<string>();
        Transform anchors = AnchorRoot(hall, changes);
        AddReadouts(contract, anchors, camera.gameObject.layer, changes);
        AddBoardMarker(hall, contract, anchors, camera.gameObject.layer, changes);
        DeletePreviews(hall, changes);
        AddDeskViewCamera(hall, contract, camera, changes);
        LightGameplayLayers(hall, changes);
        AnimeHallLightingHooks.Add(hall, camera, changes);
        if (changes.Count > 0)
            EditorSceneManager.MarkSceneDirty(hall);
        Debug.Log(changes.Count > 0
            ? $"[AnimeHallHooks] {string.Join("; ", changes)}. Save the scene."
            : "[AnimeHallHooks] The hall already carries its readouts, the desk view's camera and brain, the lit gameplay layers and its lights; nothing was changed.");

        OfficeSceneContractTools.AddAnchors();
    }

    /// <summary>The GameplayAnchors root (created when missing), kept the scene's first root so its readouts are found before the preserved desk's empty objects of the same names.</summary>
    private static Transform AnchorRoot(Scene hall, List<string> changes)
    {
        GameObject root = hall.GetRootGameObjects().FirstOrDefault(g => g.name == OfficeContract.AnchorRoot);
        if (root == null)
        {
            root = new GameObject(OfficeContract.AnchorRoot);
            Undo.RegisterCreatedObjectUndo(root, UndoName);
            changes.Add($"created {OfficeContract.AnchorRoot}");
        }

        if (root.transform.GetSiblingIndex() != 0)
        {
            Undo.SetSiblingIndex(root.transform, 0, UndoName);
            changes.Add($"made {OfficeContract.AnchorRoot} the first root");
        }
        return root.transform;
    }

    /// <summary>Each readout the root lacks, as a world-space TextMeshPro text on the art's layer.</summary>
    private static void AddReadouts(OfficeSceneContractSO contract, Transform root, int layer, List<string> changes)
    {
        foreach (Readout r in Readouts)
        {
            string name = contract.Spec(r.Id)?.fallbacks.FirstOrDefault(OfficeContract.IsBareName);
            if (string.IsNullOrEmpty(name) || root.Find(name) != null)
                continue;

            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshPro));
            Undo.RegisterCreatedObjectUndo(go, UndoName);
            go.layer = layer;
            go.transform.SetParent(root, false);
            go.transform.SetPositionAndRotation(r.Position, Quaternion.Euler(0f, r.Yaw, 0f));
            ((RectTransform)go.transform).sizeDelta = r.Box;

            var text = go.GetComponent<TextMeshPro>();
            text.text = r.Sample;
            text.fontSize = r.Size;
            text.enableAutoSizing = true;
            text.fontSizeMax = r.Size;
            text.fontSizeMin = r.Size * MinSizeShare;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.alignment = TextAlignmentOptions.Center;
            text.color = r.OnLightCard ? Ink : Glow;
            changes.Add($"added {OfficeContract.AnchorRoot}/{name}");
        }
    }

    /// <summary>
    /// The Departure Board's marker when the root lacks it: an empty
    /// RectTransform named Anchor_DepartureBoard, standing and facing as the
    /// board display layer (the contract's fallback) does, sized to its opaque
    /// pixels (OfficeAnchors.OpaqueRect), so the art side can move or resize
    /// where the rows print.
    /// </summary>
    private static void AddBoardMarker(Scene hall, OfficeSceneContractSO contract, Transform root, int layer, List<string> changes)
    {
        string name = OfficeContract.AnchorName(OfficeAnchorId.DepartureBoard);
        if (root.Find(name) != null)
            return;
        string display = contract.Spec(OfficeAnchorId.DepartureBoard)?.fallbacks.FirstOrDefault(OfficeContract.IsBareName);
        Transform found = string.IsNullOrEmpty(display) ? null : OfficeAnchors.Find(hall, display, includeInactive: false);
        SpriteRenderer sprite = found != null ? found.GetComponent<SpriteRenderer>() : null;
        if (sprite == null || sprite.sprite == null)
        {
            Debug.LogWarning($"[AnimeHallHooks] No board display '{display}' with a sprite: {name} was not added.");
            return;
        }

        Rect opaque = OfficeAnchors.OpaqueRect(sprite.sprite);
        Transform frame = sprite.transform;
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, UndoName);
        go.layer = layer;
        go.transform.SetParent(root, false);
        go.transform.SetPositionAndRotation(frame.TransformPoint(opaque.center), frame.rotation);
        Vector3 scale = frame.lossyScale, parent = root.lossyScale;
        ((RectTransform)go.transform).sizeDelta = new Vector2(opaque.width * scale.x / parent.x, opaque.height * scale.y / parent.y);
        changes.Add($"added {OfficeContract.AnchorRoot}/{name} over '{display}'");
    }

    /// <summary>Deletes the static preview TextMeshes (the readouts replace them; they would show stale values beside them).</summary>
    private static void DeletePreviews(Scene hall, List<string> changes)
    {
        List<TextMesh> previews = hall.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<TextMesh>(true))
            .Where(t => t.name.StartsWith(PreviewPrefix))
            .ToList();
        foreach (TextMesh preview in previews)
        {
            changes.Add($"deleted '{PathOf(preview.transform)}'");
            Undo.DestroyObjectImmediate(preview.gameObject);
        }
    }

    /// <summary>The desk view's Cinemachine camera at the contract's root path (framing the office as <paramref name="camera"/> does, its lens copied), a brain on the camera, and its MainCamera tag.</summary>
    private static void AddDeskViewCamera(Scene hall, OfficeSceneContractSO contract, Camera camera, List<string> changes)
    {
        string path = contract.Spec(OfficeAnchorId.OfficeVCam)?.fallbacks.FirstOrDefault(p => !OfficeContract.IsBareName(p));
        if (!string.IsNullOrEmpty(path))
        {
            int slash = path.IndexOf('/');
            string rootName = path.Substring(0, slash), childPath = path.Substring(slash + 1);
            GameObject root = hall.GetRootGameObjects().FirstOrDefault(g => g.name == rootName);
            if (root == null)
            {
                root = new GameObject(rootName);
                Undo.RegisterCreatedObjectUndo(root, UndoName);
            }

            if (root.transform.Find(childPath) == null && childPath.IndexOf('/') < 0)
            {
                var go = new GameObject(childPath);
                Undo.RegisterCreatedObjectUndo(go, UndoName);
                go.transform.SetParent(root.transform, false);
                go.transform.SetPositionAndRotation(camera.transform.position, camera.transform.rotation);
                go.AddComponent<CinemachineCamera>().Lens = LensSettings.FromCamera(camera);
                changes.Add($"added {path}");
            }
        }

        if (!camera.TryGetComponent(out CinemachineBrain _))
        {
            Undo.AddComponent<CinemachineBrain>(camera.gameObject);
            changes.Add($"added a CinemachineBrain on '{camera.name}'");
        }

        if (!camera.CompareTag(MainCameraTag))
        {
            Undo.RecordObject(camera.gameObject, UndoName);
            camera.gameObject.tag = MainCameraTag;
            changes.Add($"tagged '{camera.name}' {MainCameraTag}");
        }
    }

    /// <summary>The hall's daylight (its presentation's) also lights the gameplay's layers, so the papers and the traveller are lit as the art is.</summary>
    private static void LightGameplayLayers(Scene hall, List<string> changes)
    {
        AnimeHallPresentation presentation = hall.GetRootGameObjects()
            .Select(g => g.GetComponentInChildren<AnimeHallPresentation>(true))
            .FirstOrDefault(p => p != null);
        Light daylight = presentation != null ? presentation.daylight : null;
        int gameplay = OfficeLayers.GameplayMask;
        if (daylight == null || (daylight.cullingMask & gameplay) == gameplay)
            return;

        Undo.RecordObject(daylight, UndoName);
        daylight.cullingMask |= gameplay;
        changes.Add($"'{daylight.name}' lights Default and {OfficeLayers.Interactable}");
    }

    private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
}
