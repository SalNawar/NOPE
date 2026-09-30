using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The anime hall's portal rings (the portals spec v3 VX1-VX7;
/// docs/SCENE_CONTRACT_GAMEPLAY.md): at the day's start (when the shift's
/// PortalDay arrives, GameManager.Portals) each ring of DeskConfigSO's
/// hallPortalLayers takes its look (PortalRoute.Look): its PortalEffect inside
/// the ring shows the glow (an open departure portal), the Return Gate's
/// spiral, or nothing (a closed portal: CLOSED or under maintenance), drawn on
/// the art's Default sorting layer at the secure bay's order less one (read
/// from the bay's renderer), placed on the ring's opaque rect. An accepted
/// traveller's portal (GameManager.Departed) flares its effect. With the
/// hall's lights (HallLightingRig) each ring's state also sets its light
/// (on while the ring shows a glow or the spiral, off while it is closed),
/// and the effects move to the lights' art layer (HallBackdrop), so the 2D
/// camera that draws the painted layers draws them in their order. The effects'
/// art comes from the slots Office/portal_glow and Office/portal_return_glow,
/// else PortalGlowPlaceholder's. OfficeSceneBinder adds it to the gameplay
/// layer at load when the art office carries a presentation, beside
/// AnimeHallShiftLink. The metal rings stay as the art drew them in every
/// state: each ring's registered layer also carries the wall, pillar and bay
/// pixels around and below its frame (the layers are mutually exclusive
/// masks), so a tint on it greys that whole disc (Saleh 2026-09-29: "a closed
/// portal has nothing in the ring"); the gameplay reads the art only through
/// its public hook FindLayer. A layer the hall lacks is warned about once.
/// </summary>
[DisallowMultipleComponent]
public sealed class AnimeHallPortalLink : MonoBehaviour
{
    private AnimeHallPresentation _hall;
    private DeskConfigSO _config;
    private GameManager _game;
    private HallLightingRig _lights;
    private PortalEffect[] _effects;
    private PortalDay _shown;
    private Sprite _glow;
    private Sprite _spiral;
    private readonly List<Object> _made = new List<Object>();

    /// <summary>Points the link at the hall's presentation, the config's ring knobs, the shift, the effects (one per entry of the config's hallPortalLayers, in its order) and the hall's lights (null: none).</summary>
    public void Configure(AnimeHallPresentation hall, DeskConfigSO config, GameManager game, PortalEffect[] effects, HallLightingRig lights)
    {
        _hall = hall;
        _config = config;
        _lights = lights;
        _effects = effects ?? new PortalEffect[0];
        if (_game != null)
            _game.Departed -= HandleDeparted;
        _game = game;
        if (_game != null)
            _game.Departed += HandleDeparted;
        _glow = SlotArt.Sprite(ArtSlots.PortalGlow) ?? Placeholder(PortalGlowPlaceholder.Glow(), "portal_glow");
        _spiral = SlotArt.Sprite(ArtSlots.ReturnGateGlow) ?? Placeholder(PortalGlowPlaceholder.ReturnSpiral(), "portal_return_glow");
        _shown = null;
        Warn();
    }

    private void OnDestroy()
    {
        if (_game != null)
            _game.Departed -= HandleDeparted;
        foreach (Object made in _made)
            Destroy(made);
    }

    private void LateUpdate()
    {
        if (_hall == null || _config == null || _game == null)
            return;
        PortalDay day = _game.Portals;
        if (!ReferenceEquals(day, _shown))
            Apply(day);
        int layer = _lights != null ? _lights.ArtLayer : -1;
        if (layer != _layer)
            MoveEffects(layer);
    }

    /// <summary>Each ring's effect for <paramref name="day"/> (a day without portals shows none).</summary>
    private void Apply(PortalDay day)
    {
        _shown = day;
        HallPortalLayers[] layers = _config.hallPortalLayers ?? System.Array.Empty<HallPortalLayers>();
        for (int i = 0; i < layers.Length && i < _effects.Length; i++)
        {
            HallPortalLayers l = layers[i];
            PortalEffect effect = _effects[i];
            SpriteRenderer ring = l != null ? _hall.FindLayer(l.ring) : null;
            SpriteRenderer bay = l != null ? _hall.FindLayer(l.bay) : null;
            if (effect == null || ring == null || bay == null || ring.sprite == null)
                continue;

            PortalLook look = PortalLook.Empty;
            foreach (PortalRoute p in day.Portals)
            {
                if (p.Number != l.portal)
                    continue;
                look = p.Look;
                break;
            }

            effect.Place(ring.transform, OfficeAnchors.OpaqueRect(ring.sprite), _config.hallPortalGlowSize, bay.sortingLayerID, bay.sortingOrder - 1);
            Sprite sprite = look == PortalLook.Glow ? _glow : look == PortalLook.ReturnGate ? _spiral : null;
            if (_lights != null)
                _lights.SetPortal(l.portal, sprite != null, look == PortalLook.ReturnGate);
            effect.Show(sprite, look == PortalLook.ReturnGate ? _config.hallReturnGateTint : _config.hallPortalGlowTint,
                        _config.hallPortalSpinDegrees, _config.hallPortalPulseScale, _config.hallPortalPulseSeconds);
        }
    }

    /// <summary>The layer the effects were last put on (-2 before the first move; -1: their own, the office camera's).</summary>
    private int _layer = -2;

    /// <summary>The layer each effect was built on (restored when the lights' 2D pass goes off).</summary>
    private int[] _ownLayers;

    /// <summary>Puts every effect on <paramref name="layer"/> (the lights' art layer), or back on its own (-1).</summary>
    private void MoveEffects(int layer)
    {
        _layer = layer;
        if (_ownLayers == null)
        {
            _ownLayers = new int[_effects.Length];
            for (int i = 0; i < _effects.Length; i++)
                _ownLayers[i] = _effects[i] != null ? _effects[i].gameObject.layer : 0;
        }
        for (int i = 0; i < _effects.Length; i++)
            if (_effects[i] != null)
                foreach (Transform t in _effects[i].GetComponentsInChildren<Transform>(true))
                    t.gameObject.layer = layer >= 0 ? layer : _ownLayers[i];
    }

    /// <summary>A traveller left through <paramref name="portal"/>: its ring's effect flares.</summary>
    private void HandleDeparted(int portal)
    {
        HallPortalLayers[] layers = _config != null ? _config.hallPortalLayers : null;
        if (layers == null)
            return;
        for (int i = 0; i < layers.Length && i < _effects.Length; i++)
            if (layers[i] != null && layers[i].portal == portal && _effects[i] != null)
                _effects[i].Pulse();
    }

    /// <summary>One warning naming each configured layer the hall lacks (a renamed layer is a DeskConfigSO.hallPortalLayers edit), and a missing effect.</summary>
    private void Warn()
    {
        var missing = new List<string>();
        HallPortalLayers[] layers = _config != null ? _config.hallPortalLayers : null;
        for (int i = 0; layers != null && i < layers.Length; i++)
        {
            HallPortalLayers l = layers[i];
            if (l == null)
                continue;
            foreach (string id in new[] { l.bay, l.ring, l.glass })
                if (_hall.FindLayer(id) == null)
                    missing.Add($"'{id}'");
            if (i >= _effects.Length || _effects[i] == null)
                missing.Add($"the effect of portal {PortalText.Number(l.portal)}");
        }
        if (missing.Count > 0)
            Debug.LogWarning($"[AnimeHallPortalLink] The hall lacks {string.Join(", ", missing)}: those rings keep the art's look. Fix DeskConfigSO.hallPortalLayers (or run Build Office UI). See docs/SCENE_CONTRACT_GAMEPLAY.md.", this);
    }

    /// <summary>A sprite of a placeholder effect (made once, destroyed with the link).</summary>
    private Sprite Placeholder(byte[] rgba, string name)
    {
        int size = PortalGlowPlaceholder.Size;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp };
        texture.LoadRawTextureData(rgba);
        texture.Apply(false, true);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        sprite.name = name;
        _made.Add(texture);
        _made.Add(sprite);
        return sprite;
    }
}
