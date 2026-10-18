using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The anime hall's portal rings (the portals spec v3 VX1-VX7;
/// docs/SCENE_CONTRACT_GAMEPLAY.md): at the day's start (when the shift's
/// PortalDay arrives, GameManager.Portals) each ring of DeskConfigSO's
/// hallPortalLayers takes its look (PortalRoute.Look): its PortalEffect opens
/// its cel vortex (an open departure portal), the Return Gate's amber one, or
/// shuts (a closed portal: CLOSED or under maintenance). Each vortex sits on
/// its ring's opening as the hall drawing paints it (Saleh's demo 1018a: "the
/// vfx for portals ... is not even alligned"): the opening measured in the
/// drawing's canvas pixels (HallPortalLayers.opening, its fence rail
/// fenceY) on DeskConfigSO.hallPortalDrawing, drawn on its sorting layer just
/// above it, following its transform (the pan) at every aspect; without that
/// drawing or a measured opening it falls back on the ring layer's opaque rect
/// at the secure bay's order less one. An accepted traveller's portal
/// (GameManager.Departed) flares its vortex. With the hall's lights
/// (HallLightingRig) each ring's state also sets its light (on while the ring
/// shows a vortex, off while it is closed), each vortex reads the rig's night
/// (HallLightingRig.PortalNight: brighter, more sparkles after dark), and the
/// effects move to the lights' art layer (HallBackdrop), so the 2D camera that
/// draws the painted layers draws them in their order. OfficeSceneBinder adds
/// it to the gameplay layer at load when the art office carries a
/// presentation, beside AnimeHallShiftLink. The metal rings stay as the art
/// drew them in every state (Saleh 2026-09-29: "a closed portal has nothing in
/// the ring"); the gameplay reads the art only through its public hook
/// FindLayer. A layer the hall lacks is warned about once.
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
        _shown = null;
        Warn();
    }

    private void OnDestroy()
    {
        if (_game != null)
            _game.Departed -= HandleDeparted;
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
        HallPortalLayers[] layers = _config.hallPortalLayers;
        for (int i = 0; layers != null && i < layers.Length && i < _effects.Length; i++)
            if (_effects[i] != null && layers[i] != null)
                _effects[i].Night = _lights != null ? _lights.PortalNight(layers[i].portal) : 0f;
    }

    /// <summary>Each ring's vortex for <paramref name="day"/> (a day without portals shows none).</summary>
    private void Apply(PortalDay day)
    {
        _shown = day;
        HallPortalLayers[] layers = _config.hallPortalLayers ?? System.Array.Empty<HallPortalLayers>();
        SpriteRenderer drawing = string.IsNullOrEmpty(_config.hallPortalDrawing) ? null : _hall.FindLayer(_config.hallPortalDrawing);
        for (int i = 0; i < layers.Length && i < _effects.Length; i++)
        {
            HallPortalLayers l = layers[i];
            PortalEffect effect = _effects[i];
            if (l == null || effect == null || !Place(effect, l, drawing))
                continue;

            PortalLook look = PortalLook.Empty;
            foreach (PortalRoute p in day.Portals)
            {
                if (p.Number != l.portal)
                    continue;
                look = p.Look;
                break;
            }

            bool open = look == PortalLook.Glow || look == PortalLook.ReturnGate;
            if (_lights != null)
                _lights.SetPortal(l.portal, open, look == PortalLook.ReturnGate);
            effect.Show(open, look == PortalLook.ReturnGate, _config.hallPortalVortex, _config.hallPortalPulseSeconds);
        }
    }

    /// <summary>
    /// Puts <paramref name="effect"/> on its ring's opening: measured on the
    /// drawing when there is one and the opening has radii, else on the ring
    /// layer's opaque rect (its opening the inner share, no fence) at the bay's
    /// order less one; false when neither is there.
    /// </summary>
    private bool Place(PortalEffect effect, HallPortalLayers l, SpriteRenderer drawing)
    {
        Vector4 o = l.opening;
        if (drawing != null && drawing.sprite != null && o.z > 0f && o.w > 0f)
        {
            Sprite s = drawing.sprite;
            Rect rect = s.rect;
            PortalVortex.Box local = PortalVortex.LocalQuad(o.x, o.y, o.z, o.w, rect.width, rect.height, s.pivot.x, s.pivot.y, s.pixelsPerUnit);
            PortalVortex.Box uv = PortalVortex.TextureQuad(rect.x + o.x, (s.texture != null ? s.texture.height - rect.yMax : 0f) + o.y, o.z, o.w,
                                                           s.texture != null ? s.texture.width : rect.width, s.texture != null ? s.texture.height : rect.height);
            effect.Place(drawing.transform, new Rect(local.X, local.Y, local.W, local.H), s.texture, new Rect(uv.X, uv.Y, uv.W, uv.H),
                         PortalVortex.FenceV(o.y, o.w, l.fenceY), drawing.sortingLayerID, drawing.sortingOrder + 1);
            return true;
        }

        SpriteRenderer ring = _hall.FindLayer(l.ring);
        SpriteRenderer bay = _hall.FindLayer(l.bay);
        if (ring == null || bay == null || ring.sprite == null)
            return false;
        // The ring's whole rect as the quad: its opening is the inner 1 / PortalVortex.Margin, about the painted hole.
        effect.Place(ring.transform, OfficeAnchors.OpaqueRect(ring.sprite), null, new Rect(0f, 0f, 1f, 1f), 0f, bay.sortingLayerID, bay.sortingOrder - 1);
        return true;
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

    /// <summary>A traveller left through <paramref name="portal"/>: its ring's vortex flares.</summary>
    private void HandleDeparted(int portal)
    {
        HallPortalLayers[] layers = _config != null ? _config.hallPortalLayers : null;
        if (layers == null)
            return;
        for (int i = 0; i < layers.Length && i < _effects.Length; i++)
            if (layers[i] != null && layers[i].portal == portal && _effects[i] != null)
                _effects[i].Pulse();
    }

    /// <summary>One warning naming the drawing and each configured layer the hall lacks (a renamed layer is a DeskConfigSO edit), and a missing effect.</summary>
    private void Warn()
    {
        var missing = new List<string>();
        if (_config == null || _hall == null)
            return;
        if (!string.IsNullOrEmpty(_config.hallPortalDrawing) && _hall.FindLayer(_config.hallPortalDrawing) == null)
            missing.Add($"the drawing '{_config.hallPortalDrawing}'");
        HallPortalLayers[] layers = _config.hallPortalLayers;
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
            Debug.LogWarning($"[AnimeHallPortalLink] The hall lacks {string.Join(", ", missing)}: those rings keep the art's look. Fix DeskConfigSO.hallPortalDrawing / hallPortalLayers (or run Build Office UI). See docs/SCENE_CONTRACT_GAMEPLAY.md.", this);
    }
}
