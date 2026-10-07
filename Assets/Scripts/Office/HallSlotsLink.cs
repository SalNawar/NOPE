using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// The anime hall's swappable slots at runtime (the hall slots spec,
/// docs/superpowers/specs/2026-10-07-hall-slots-design.md): OfficeSceneBinder
/// adds it to the gameplay layer at load when the art office carries the
/// hall's presentation and DeskConfigSO names a HallSlotsSO. For each slot it
/// makes two sprite renderers (for the crossfade) beside the hall painting
/// (HallSlotsSO.paintingLayer, found through the art's public hook
/// AnimeHallPresentation.FindLayer), on its layer, sorting layer and material,
/// registered to its source canvas, so the hall's pan carries them and its
/// time of day lights them like the painting (each frame the painting's
/// lighting values, written by the art's HallBakedLighting, are copied onto
/// them; nothing is allocated per frame). It reads the hall's variables from
/// the run (HallState: the leading culture of CultureThemeService, the
/// Helix River's tier of the stability, the phase of the day, today's
/// special of the day plans' rules, the famous travellers let through of the
/// verdict flags) whenever an input changes, applies the dev overrides
/// (HallSlotsDev), and when the state changes picks each slot's variant
/// (HallSlotPick) and crossfades to its art, Resources
/// Hall/Slots/&lt;slot&gt;/&lt;file&gt; (ArtSlots.HallSlot; Reduced Motion cuts). A
/// picked variant without art keeps the painting as it is; for a new overlay
/// (and for every slot while HallSlotsDev.ShowAllStandIns is on) the editor
/// and development builds draw a stand-in instead: a flat two-tone cel plate
/// on the slot's region with the slot and variant named on it; release builds
/// draw nothing. The art scene is never edited: the renderers live only in
/// play.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(210)]
public sealed class HallSlotsLink : MonoBehaviour
{
    /// <summary>One of a slot's two renderers (the crossfade's), with the stand-in's label when it shows one.</summary>
    private sealed class Face
    {
        public SpriteRenderer Renderer;
        public TextMeshPro Label;
        public string File;
    }

    /// <summary>A slot at runtime: its definition, its two faces (Front shows, Back fades in) and the fade's progress.</summary>
    private sealed class Slot
    {
        public HallSlotDef Def;
        public Face Front, Back;
        public float Fade = 1f;
    }

    private static readonly int StateWeightsId = Shader.PropertyToID("_StateWeights");
    private static readonly int ShadowRayId = Shader.PropertyToID("_HallShadowRay");
    private static readonly int LightingAmountId = Shader.PropertyToID("_LightingAmount");
    private static readonly int CloudMotionId = Shader.PropertyToID("_CloudMotion");
    private static readonly int PaletteAmountId = Shader.PropertyToID("_PaletteAmount");
    private static readonly int AmbientColorId = Shader.PropertyToID("_AmbientColor");

    private HallSlotsSO _settings;
    private SpriteRenderer _painting;
    private readonly List<Slot> _slots = new List<Slot>();
    private readonly List<Object> _made = new List<Object>();
    private readonly Dictionary<string, Sprite> _standIns = new Dictionary<string, Sprite>();
    private Dictionary<string, string> _nationOfPremade;
    private MaterialPropertyBlock _read, _write;

    private HallState _state;
    private int _day = -1, _stability = -1, _flags = -1, _version = -1;
    private string _culture;
    private bool _seen, _allStandIns;

    /// <summary>The hall's state now (null before the first read): the cheat menu and the capture tool report it.</summary>
    public HallState State => _state;

    /// <summary>The one link in play (null when the art office has no hall or no slots).</summary>
    public static HallSlotsLink Live { get; private set; }

    /// <summary>Points the link at the hall's presentation and the slots' asset, and builds the slots' renderers beside the painting (warns once and does nothing when the painting is missing).</summary>
    public void Configure(AnimeHallPresentation hall, HallSlotsSO settings)
    {
        _settings = settings;
        _painting = hall != null && settings != null ? hall.FindLayer(settings.paintingLayer) : null;
        if (_painting == null || _painting.sprite == null)
        {
            Debug.LogWarning($"[HallSlots] The hall has no painting layer '{settings?.paintingLayer}': its slots stay off.");
            enabled = false;
            return;
        }

        RunManager run = RunManager.Instance;
        ContentLibrarySO lib = run != null ? run.Library : null;
        var nations = new HashSet<string>();
        _nationOfPremade = new Dictionary<string, string>();
        if (lib != null)
        {
            foreach (NationSO n in lib.Nations)
                if (n != null && !string.IsNullOrWhiteSpace(n.id))
                    nations.Add(n.id);
            foreach (LegendarySO p in lib.Legendaries)
                if (p != null && !string.IsNullOrWhiteSpace(p.id) && p.nation != null)
                    _nationOfPremade[p.id] = p.nation.id;
        }
        foreach (string problem in HallSlotPick.Problems(settings.slots, settings.canvasWidth, settings.canvasHeight, nations.Count > 0 ? nations : null))
            Debug.LogWarning("[HallSlots] " + problem);

        _read = new MaterialPropertyBlock();
        _write = new MaterialPropertyBlock();
        foreach (HallSlotDef def in settings.slots)
            if (def != null && !string.IsNullOrWhiteSpace(def.id))
                _slots.Add(Build(def));
        Live = this;
    }

    private void OnDestroy()
    {
        if (Live == this)
            Live = null;
        foreach (Object made in _made)
            if (made != null)
                Destroy(made);
    }

    private Slot Build(HallSlotDef def)
    {
        var root = new GameObject("Hall slot " + def.id);
        _made.Add(root);
        Transform paint = _painting.transform;
        root.layer = _painting.gameObject.layer;
        root.transform.SetParent(paint.parent, false);
        root.transform.localPosition = paint.localPosition;
        root.transform.localRotation = paint.localRotation;
        root.transform.localScale = paint.localScale;
        return new Slot { Def = def, Front = MakeFace(root.transform, def, "A"), Back = MakeFace(root.transform, def, "B") };
    }

    private Face MakeFace(Transform root, HallSlotDef def, string name)
    {
        var go = new GameObject(name);
        go.layer = root.gameObject.layer;
        go.transform.SetParent(root, false);
        var r = go.AddComponent<SpriteRenderer>();
        r.sharedMaterial = _painting.sharedMaterial;
        r.sortingLayerID = _painting.sortingLayerID;
        r.sortingOrder = def.order;
        r.color = new Color(1f, 1f, 1f, 0f);
        return new Face { Renderer = r };
    }

    private void LateUpdate()
    {
        if (_painting == null)
            return;
        ReadState();
        float dt = Time.deltaTime;
        // The painting's lighting values as the art wrote them this frame (only those it holds: the material's own stand for the rest).
        _painting.GetPropertyBlock(_read);
        _write.Clear();
        if (_read.HasVector(StateWeightsId))
            _write.SetVector(StateWeightsId, _read.GetVector(StateWeightsId));
        if (_read.HasVector(ShadowRayId))
            _write.SetVector(ShadowRayId, _read.GetVector(ShadowRayId));
        if (_read.HasFloat(LightingAmountId))
            _write.SetFloat(LightingAmountId, _read.GetFloat(LightingAmountId));
        if (_read.HasFloat(CloudMotionId))
            _write.SetFloat(CloudMotionId, _read.GetFloat(CloudMotionId));
        if (_read.HasFloat(PaletteAmountId))
            _write.SetFloat(PaletteAmountId, _read.GetFloat(PaletteAmountId));
        if (_read.HasColor(AmbientColorId))
            _write.SetColor(AmbientColorId, _read.GetColor(AmbientColorId));
        for (int i = 0; i < _slots.Count; i++)
        {
            Slot s = _slots[i];
            if (s.Fade < 1f)
            {
                s.Fade = _settings.crossfadeSeconds > 0f ? Mathf.Min(1f, s.Fade + dt / _settings.crossfadeSeconds) : 1f;
                SetAlpha(s.Back, s.Fade);
                SetAlpha(s.Front, 1f - s.Fade);
                if (s.Fade >= 1f)
                {
                    Show(s.Front, null, null);
                    (s.Front, s.Back) = (s.Back, s.Front);
                }
            }
            s.Front.Renderer.SetPropertyBlock(_write);
            s.Back.Renderer.SetPropertyBlock(_write);
        }
    }

    /// <summary>Re-reads the state when an input changed; when it differs, every slot picks again.</summary>
    private void ReadState()
    {
        RunManager run = RunManager.Instance;
        WorldState world = run != null ? run.World : null;
        int day = world != null ? world.day : 0;
        int stability = world != null ? world.stabilityHundredths : -1;
        int flags = world != null ? world.flags.Count : 0;
        string culture = CultureThemeService.Instance != null ? CultureThemeService.Instance.ActiveCultureId : null;
        if (_seen && day == _day && stability == _stability && flags == _flags && culture == _culture && HallSlotsDev.Version == _version)
            return;
        _seen = true;
        _day = day;
        _stability = stability;
        _flags = flags;
        _culture = culture;
        _version = HallSlotsDev.Version;

        HallState state = Read(run, world, culture);
        HallSlotsDev.Apply(state);
        bool redraw = HallSlotsDev.ShowAllStandIns != _allStandIns;
        _allStandIns = HallSlotsDev.ShowAllStandIns;
        if (!redraw && _state != null && state.SameAs(_state))
            return;
        _state = state;
        Repick(world != null ? world.runSeed : 0, redraw);
    }

    /// <summary>The hall's variables from the run (neutral, steady, normal, no special and no exhibit without one).</summary>
    private HallState Read(RunManager run, WorldState world, string culture)
    {
        var state = new HallState { Culture = string.IsNullOrWhiteSpace(culture) ? null : culture };
        if (world == null)
            return state;
        GameConfigSO config = run.Config != null ? run.Config.gameConfig : null;
        if (config != null)
            state.Tier = HelixRiver.Tier(world.timelineStability, config.firedAtStability, config.stabilityWarningMargin, config.stabilityCriticalMargin,
                                         _settings.river != null ? _settings.river.knobs : null);
        ContentLibrarySO lib = run.Library;
        DayPlanSO plan = lib != null ? lib.GetDayPlan(world.day) : null;
        state.Phase = HallStates.PhaseOf(plan != null ? plan.Shift(config) : GameConfigSO.Standard(config), GameConfigSO.Standard(config));
        if (lib != null)
            state.Event = HallStates.EventOf(RuleTypes(lib.GetDayPlan(world.day)), world.day > 1 ? RuleTypes(lib.GetDayPlan(world.day - 1)) : null);
        state.Exhibits = HallStates.ExhibitsOf(world.flags, id => _nationOfPremade.TryGetValue(id, out string n) ? n : null, out state.RecentExhibit, out state.StrongestExhibit);
        return state;
    }

    private static List<TravelRuleType> RuleTypes(DayPlanSO plan)
    {
        var types = new List<TravelRuleType>();
        if (plan != null)
            foreach (TravelRuleSO rule in plan.ActiveTravelRules)
                if (rule != null)
                    types.Add(rule.type);
        return types;
    }

    /// <summary>Every slot's pick for the state; a changed one (every one when <paramref name="redraw"/>: the stand-ins' switch moved) fades to its art (or its stand-in, or nothing).</summary>
    private void Repick(int runSeed, bool redraw)
    {
        var log = new StringBuilder("[HallSlots] ").Append(_state);
        bool reduced = MotionPreference.Reduced;
        foreach (Slot s in _slots)
        {
            HallPick pick = HallSlotPick.Pick(s.Def, _state, runSeed);
            string file = pick.File;
            if (!redraw && file == s.Front.File && s.Fade >= 1f)
                continue;
            Sprite art = file != null ? SlotArt.Sprite(ArtSlots.HallSlot(s.Def.id, file)) : null;
            bool standIn = art == null && file != null && Debug.isDebugBuild && (s.Def.IsNewOverlay || HallSlotsDev.ShowAllStandIns);
            log.Append("; ").Append(s.Def.id).Append(" -> ").Append(file ?? "painting").Append(art != null ? " (art)" : standIn ? " (stand-in)" : file != null ? " (no art: painting)" : "");
            if (s.Fade < 1f)
            {
                Show(s.Front, null, null);
                (s.Front, s.Back) = (s.Back, s.Front);
                SetAlpha(s.Front, 1f);
            }
            Show(s.Back, file, art != null ? art : standIn ? StandIn(s.Def, file) : null, art == null && standIn ? s.Def.id + "\n" + file : null);
            if (reduced || _settings.crossfadeSeconds <= 0f)
            {
                Show(s.Front, null, null);
                (s.Front, s.Back) = (s.Back, s.Front);
                SetAlpha(s.Front, 1f);
                s.Fade = 1f;
            }
            else
            {
                s.Fade = 0f;
                SetAlpha(s.Back, 0f);
            }
        }
        Debug.Log(log.ToString());
    }

    /// <summary>Puts <paramref name="sprite"/> on a face, registered to the painting's canvas (a full-canvas painting by its bounds; a stand-in by its own pivot), with the stand-in's label or none.</summary>
    private void Show(Face face, string file, Sprite sprite, string label = null)
    {
        face.File = file;
        face.Renderer.sprite = sprite;
        Transform t = face.Renderer.transform;
        if (sprite != null && !_standIns.ContainsValue(sprite))
        {
            Bounds canvas = _painting.sprite.bounds, art = sprite.bounds;
            float scale = art.size.x > 0f ? canvas.size.x / art.size.x : 1f;
            t.localScale = new Vector3(scale, scale, 1f);
            t.localPosition = canvas.center - art.center * scale;
        }
        else
        {
            t.localScale = Vector3.one;
            t.localPosition = Vector3.zero;
        }

        if (label == null)
        {
            if (face.Label != null)
                face.Label.gameObject.SetActive(false);
            return;
        }
        if (face.Label == null)
        {
            var go = new GameObject("Stand-in label");
            go.layer = 0;
            go.transform.SetParent(t, false);
            face.Label = go.AddComponent<TextMeshPro>();
            face.Label.alignment = TextAlignmentOptions.Center;
            face.Label.enableAutoSizing = true;
            face.Label.fontSizeMin = 0.4f;
            face.Label.fontSizeMax = 3f;
            face.Label.color = _settings.standInInk;
            face.Label.sortingLayerID = face.Renderer.sortingLayerID;
            face.Label.sortingOrder = face.Renderer.sortingOrder + 1;
        }
        face.Label.gameObject.SetActive(true);
        face.Label.text = label;
        Bounds b = sprite.bounds;
        face.Label.rectTransform.localPosition = new Vector3(b.center.x, b.center.y, -0.001f);
        face.Label.rectTransform.sizeDelta = new Vector2(b.size.x * 0.92f, b.size.y * 0.92f);
    }

    private static void SetAlpha(Face face, float alpha)
    {
        face.Renderer.color = new Color(1f, 1f, 1f, alpha);
        if (face.Label != null && face.Label.gameObject.activeSelf)
            face.Label.alpha = alpha;
    }

    /// <summary>The stand-in of a slot's variant (made once): a two-tone cel plate on the slot's region with an ink edge, its light tone shifted by the variant so a change shows.</summary>
    private Sprite StandIn(HallSlotDef def, string file)
    {
        string key = def.id + "/" + file;
        if (_standIns.TryGetValue(key, out Sprite made))
            return made;
        int w = Mathf.Max(4, def.width), h = Mathf.Max(4, def.height);
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "Hall slot stand-in " + key, wrapMode = TextureWrapMode.Clamp };
        float hue = (Seeds.OfKey(file) & 0xFFFF) / 65535f;
        Color light = Color.Lerp(_settings.standInLight, Color.HSVToRGB(hue, 0.45f, 0.95f), 0.35f);
        light.a = _settings.standInLight.a;
        var pixels = new Color32[w * h];
        int shadeTop = Mathf.RoundToInt(h * 0.35f);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool edge = x < 2 || y < 2 || x >= w - 2 || y >= h - 2;
                pixels[y * w + x] = edge ? _settings.standInInk : y < shadeTop ? _settings.standInShade : light;
            }
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        _made.Add(tex);

        Bounds canvas = _painting.sprite.bounds;
        float pixelsPerUnit = _settings.canvasWidth / canvas.size.x;
        // The region's centre on the canvas (top-left origin) as the painting's local position.
        float cx = canvas.min.x + (def.x + w * 0.5f) / pixelsPerUnit;
        float cy = canvas.max.y - (def.y + h * 0.5f) / pixelsPerUnit;
        var pivot = new Vector2(0.5f - cx * pixelsPerUnit / w, 0.5f - cy * pixelsPerUnit / h);
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, w, h), pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect);
        sprite.name = tex.name;
        _made.Add(sprite);
        _standIns[key] = sprite;
        return sprite;
    }
}
