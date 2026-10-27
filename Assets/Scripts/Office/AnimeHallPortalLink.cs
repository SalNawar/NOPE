using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The anime hall's portal rings (the portals spec v3 VX1-VX7;
/// docs/SCENE_CONTRACT_GAMEPLAY.md; HALL-FX BUG-26a, 73d): at the day's start
/// (when the shift's PortalDay arrives, GameManager.Portals) each ring of the
/// hall's portals (HallPortalsSO, the one source of the rings, handed to every
/// PortalEffect by Build Office UI) takes its look (PortalRoute.Look): its
/// PortalEffect opens its cel vortex (an open departure portal), the Return
/// Gate's amber one, or shuts (a closed portal: CLOSED or under maintenance).
/// Each vortex sits on its ring's opening as the hall drawing paints it
/// (Saleh's demo 1018a: "the vfx for portals ... is not even alligned"): the
/// opening and the painted-aperture mask on the drawing's canvas
/// (HallPortalRing), scaled to the drawing as imported, drawn on its sorting
/// layer just above it, following its transform (the pan) at every aspect;
/// without that drawing it falls back on the ring layer's opaque rect,
/// unmasked, at the secure bay's order less one. An accepted traveller's portal
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
    private HallPortalsSO _portals;
    private GameManager _game;
    private HallLightingRig _lights;
    private PortalEffect[] _effects;
    private PortalDay _shown;

    /// <summary>
    /// Points the link at the hall's presentation, the shift, the effects (one
    /// per HallPortalsSO ring, in its order; the rings are read from the
    /// effects' HallPortalsSO) and the hall's lights (null: none).
    /// <paramref name="config"/> is not read: the rings moved from
    /// DeskConfigSO to HallPortalsSO (HALL-FX; the argument goes at the
    /// binder's cleanup).
    /// </summary>
    public void Configure(AnimeHallPresentation hall, DeskConfigSO config, GameManager game, PortalEffect[] effects, HallLightingRig lights)
    {
        _hall = hall;
        _lights = lights;
        _effects = effects ?? new PortalEffect[0];
        _portals = null;
        foreach (PortalEffect e in _effects)
            if (e != null && e.Portals != null)
            {
                _portals = e.Portals;
                break;
            }
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
        if (_hall == null || _portals == null || _game == null)
            return;
        PortalDay day = _game.Portals;
        if (!ReferenceEquals(day, _shown))
            Apply(day);
        int layer = _lights != null ? _lights.ArtLayer : -1;
        if (layer != _layer)
            MoveEffects(layer);
        HallPortalRing[] rings = _portals.rings;
        for (int i = 0; rings != null && i < rings.Length && i < _effects.Length; i++)
            if (_effects[i] != null && rings[i] != null)
                _effects[i].Night = _lights != null ? _lights.PortalNight(rings[i].portal) : 0f;
    }

    /// <summary>Each ring's vortex for <paramref name="day"/> (a day without portals shows none).</summary>
    private void Apply(PortalDay day)
    {
        _shown = day;
        HallPortalRing[] rings = _portals.rings ?? System.Array.Empty<HallPortalRing>();
        SpriteRenderer drawing = string.IsNullOrEmpty(_portals.drawing) ? null : _hall.FindLayer(_portals.drawing);
        for (int i = 0; i < rings.Length && i < _effects.Length; i++)
        {
            HallPortalRing ring = rings[i];
            PortalEffect effect = _effects[i];
            if (ring == null || effect == null || !Place(effect, ring, drawing))
                continue;

            PortalLook look = PortalLook.Empty;
            foreach (PortalRoute p in day.Portals)
            {
                if (p.Number != ring.portal)
                    continue;
                look = p.Look;
                break;
            }

            bool open = look == PortalLook.Glow || look == PortalLook.ReturnGate;
            if (_lights != null)
                _lights.SetPortal(ring.portal, open, look == PortalLook.ReturnGate);
            effect.Show(open, look == PortalLook.ReturnGate, _portals.vortex, _portals.pulseSeconds);
        }
    }

    /// <summary>
    /// Puts <paramref name="effect"/> on its ring's opening: on the drawing
    /// (its canvas scaled to the sprite as imported) with the ring's mask when
    /// there is one and the opening has radii, else on the ring layer's opaque
    /// rect (its opening the inner share, unmasked) at the bay's order less
    /// one; false when neither is there.
    /// </summary>
    private bool Place(PortalEffect effect, HallPortalRing ring, SpriteRenderer drawing)
    {
        Vector4 o = ring.opening;
        if (drawing != null && drawing.sprite != null && o.z > 0f && o.w > 0f)
        {
            Sprite s = drawing.sprite;
            Rect rect = s.rect;
            float kx = _portals.canvas.x > 0f ? rect.width / _portals.canvas.x : 1f;
            float ky = _portals.canvas.y > 0f ? rect.height / _portals.canvas.y : 1f;
            PortalVortex.Box local = PortalVortex.LocalQuad(o.x * kx, o.y * ky, o.z * kx, o.w * ky, rect.width, rect.height, s.pivot.x, s.pivot.y, s.pixelsPerUnit);
            Rect crop = ring.maskCanvas, atlas = ring.maskRect;
            PortalVortex.Box map = PortalVortex.MaskMap(o.x, o.y, o.z, o.w, new PortalVortex.Box(crop.x, crop.y, crop.width, crop.height),
                                                        new PortalVortex.Box(atlas.x, atlas.y, atlas.width, atlas.height));
            effect.Place(drawing.transform, new Rect(local.X, local.Y, local.W, local.H), _portals.masks, map, atlas, ring.reach,
                         drawing.sortingLayerID, drawing.sortingOrder + 1);
            return true;
        }

        SpriteRenderer ringLayer = _hall.FindLayer(ring.ring);
        SpriteRenderer bay = _hall.FindLayer(ring.bay);
        if (ringLayer == null || bay == null || ringLayer.sprite == null)
            return false;
        // The ring's whole rect as the quad: its opening is the inner 1 / PortalVortex.Margin, about the painted hole.
        effect.Place(ringLayer.transform, OfficeAnchors.OpaqueRect(ringLayer.sprite), null, new PortalVortex.Box(0f, 0f, 1f, 1f), new Rect(0f, 0f, 1f, 1f), 1f,
                     bay.sortingLayerID, bay.sortingOrder - 1);
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
        HallPortalRing[] rings = _portals != null ? _portals.rings : null;
        if (rings == null)
            return;
        for (int i = 0; i < rings.Length && i < _effects.Length; i++)
            if (rings[i] != null && rings[i].portal == portal && _effects[i] != null)
                _effects[i].Pulse();
    }

    /// <summary>One warning naming a missing HallPortalsSO, the drawing and each ring layer the hall lacks (a renamed layer is a HallPortals.asset edit), and a missing effect.</summary>
    private void Warn()
    {
        if (_hall == null)
            return;
        if (_portals == null)
        {
            Debug.LogWarning("[AnimeHallPortalLink] The portal effects carry no HallPortalsSO: the rings keep the art's look. Run Build Office UI (it hands Assets/Data/Config/HallPortals.asset to every PortalEffect).", this);
            return;
        }
        var missing = new List<string>();
        if (!string.IsNullOrEmpty(_portals.drawing) && _hall.FindLayer(_portals.drawing) == null)
            missing.Add($"the drawing '{_portals.drawing}'");
        HallPortalRing[] rings = _portals.rings;
        for (int i = 0; rings != null && i < rings.Length; i++)
        {
            HallPortalRing ring = rings[i];
            if (ring == null)
                continue;
            foreach (string id in new[] { ring.bay, ring.ring, ring.glass })
                if (_hall.FindLayer(id) == null)
                    missing.Add($"'{id}'");
            if (i >= _effects.Length || _effects[i] == null)
                missing.Add($"the effect of portal {PortalText.Number(ring.portal)}");
        }
        if (missing.Count > 0)
            Debug.LogWarning($"[AnimeHallPortalLink] The hall lacks {string.Join(", ", missing)}: those rings keep the art's look. Fix HallPortals.asset's drawing / rings (or run Build Office UI). See docs/SCENE_CONTRACT_GAMEPLAY.md.", this);
    }
}
