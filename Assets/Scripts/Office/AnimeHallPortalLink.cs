using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The anime hall's portal rings (the portals spec v3 VX1-VX7;
/// docs/SCENE_CONTRACT_GAMEPLAY.md; HALL-FX BUG-26a, 73d), driven as gates
/// (the night of 2026-11-07, DECISIONS D6: "portals", the Stargate idea). At
/// the day's start (when the shift's PortalDay arrives, GameManager.Portals)
/// each ring of the hall's portals (HallPortalsSO, the one source of the
/// rings, handed to every PortalEffect by Build Office UI) is placed on its
/// opening as the hall drawing paints it (Saleh's demo 1018a: "the vfx for
/// portals ... is not even alligned"): the opening and the painted-aperture
/// mask on the drawing's canvas (HallPortalRing), scaled to the drawing as
/// imported, drawn on its sorting layer just above it, following its
/// transform (the pan) at every aspect; without that drawing it falls back on
/// the ring layer's opaque rect, unmasked, at the secure bay's order less one.
/// Its chevrons go on the ring's metal (HallPortalRing.chevrons) and it takes
/// its colours: the ring's light (HallLightingSO.portalOpenLight, the Return
/// Gate's returnGateLight) and the close's fade (portalFadeSeconds). Then each
/// frame each ring is fed (PortalEffect.Drive): with a switchboard in the
/// office (HallGateFeed, read only), a departure ring on an open route (the
/// day's Glow look) follows module NN: locked in, its
/// chevrons light over the calibration; live, its event horizon opens; each
/// group the module sends plays a kawoosh (PortalEffect.Send); a cleared or
/// broken route closes it. The Return Gate in service keeps its warm event
/// horizon open all day (PortalRoute.Look). Without a switchboard the day's
/// look alone drives the rings, as before the board: an open departure portal
/// shows its event horizon, and an accepted traveller's portal
/// (GameManager.Departed) plays the kawoosh; with the board, the approval
/// plays none (the board's send does). A closed portal (CLOSED today or under
/// maintenance) stays a dark ring (Saleh 2026-09-29: "a closed portal has
/// nothing in the ring"). The cheat "Hall: portals open" (and the QA rig's
/// hallPortalsOpen) opens every ring's event horizon (<see cref="AllOpen"/>).
/// With the hall's lights (HallLightingRig) each ring's light follows its
/// event horizon (on while it shows, fading with it), each ring reads the
/// rig's night (HallLightingRig.PortalNight: brighter after dark), and the
/// effects move to the lights' art layer (HallBackdrop), so the 2D camera that
/// draws the painted layers draws them in their order. OfficeSceneBinder adds
/// it to the gameplay layer at load when the art office carries a
/// presentation, beside AnimeHallShiftLink. The metal rings stay as the art
/// drew them in every state but for the chevrons; the gameplay reads the art
/// only through its public hook FindLayer. A layer the hall lacks is warned
/// about once.
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
    private PortalLook[] _looks = System.Array.Empty<PortalLook>();
    private int[] _sent = System.Array.Empty<int>();
    private bool _board;

    /// <summary>True while every ring shows its event horizon whatever drives it (the cheat "Hall: portals open"; the QA rig's hallPortalsOpen).</summary>
    public static bool AllOpen { get; set; }

    /// <summary>Forgets the cheat when play starts (the editor without a domain reload keeps statics).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => AllOpen = false;

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
        Feed();
    }

    /// <summary>
    /// Each ring's feed this frame (PortalGateFeed.ForRing): a departure ring
    /// on an open route follows module NN of the office's switchboard (a
    /// module's new send plays its kawoosh), or the day's look without one; a
    /// closed ring stays dark whatever its module does; the Return Gate in
    /// service and the cheat keep the event horizon open. The ring's light
    /// follows its event horizon.
    /// </summary>
    private void Feed()
    {
        HallPortalRing[] rings = _portals.rings;
        _board = HallGateFeed.Present;
        Switchboard board = _board ? HallGateFeed.Board : null;
        float calibration = board != null ? board.Rules.CalibrationSeconds : 0f;
        for (int i = 0; rings != null && i < rings.Length && i < _effects.Length && i < _looks.Length; i++)
        {
            HallPortalRing ring = rings[i];
            PortalEffect effect = _effects[i];
            if (ring == null || effect == null)
                continue;
            bool returnGate = _looks[i] == PortalLook.ReturnGate;
            SwitchModule module = HallGateFeed.Module(board, ring.portal);
            PortalGateFeed feed = PortalGateFeed.ForRing(_looks[i], module, calibration, _board, AllOpen);
            effect.Drive(feed);
            if (PortalGateFeed.SentSince(module, ref _sent[i]) && PortalGateFeed.TakesDial(_looks[i]))
                effect.Send();
            effect.Night = _lights != null ? _lights.PortalNight(ring.portal) : 0f;
            if (_lights != null)
                _lights.SetPortal(ring.portal, feed.Horizon, returnGate);
        }
    }

    /// <summary>Each ring placed, its chevrons put on its metal and its colours set for <paramref name="day"/> (a day without portals: every ring dark).</summary>
    private void Apply(PortalDay day)
    {
        _shown = day;
        HallPortalRing[] rings = _portals.rings ?? System.Array.Empty<HallPortalRing>();
        if (_looks.Length != rings.Length)
        {
            _looks = new PortalLook[rings.Length];
            _sent = new int[rings.Length];
        }
        SpriteRenderer drawing = string.IsNullOrEmpty(_portals.drawing) ? null : _hall.FindLayer(_portals.drawing);
        PortalGateTuning gate = _portals.gate;
        HallLightingSO light = _lights != null ? _lights.Settings : null;
        for (int i = 0; i < rings.Length && i < _effects.Length; i++)
        {
            HallPortalRing ring = rings[i];
            PortalEffect effect = _effects[i];
            _looks[i] = PortalLook.Empty;
            if (ring == null || effect == null || !Place(effect, ring, drawing))
                continue;

            foreach (PortalRoute p in day.Portals)
            {
                if (p.Number != ring.portal)
                    continue;
                _looks[i] = p.Look;
                break;
            }

            bool returnGate = _looks[i] == PortalLook.ReturnGate;
            Color tone = light != null ? (returnGate ? light.returnGateLight : light.portalOpenLight) : returnGate ? gate.returnTone : gate.departureTone;
            effect.Look(returnGate, tone, light != null ? light.portalFadeSeconds : gate.fadeSeconds);
            Vector4 o = ring.opening, c = ring.chevrons;
            float mean = Mathf.Sqrt(Mathf.Max(1e-3f, o.z * o.w));
            effect.PlaceChevrons(PortalVortex.ChevronRing(o.x, o.y, o.z, o.w, c.x, c.y, c.z, c.w, gate.chevronGrow),
                                 ring.chevronStep > 0f ? ring.chevronStep : gate.chevronStep,
                                 ring.chevronSize > 0f ? ring.chevronSize / mean : gate.chevronSize,
                                 o.w > 0f ? o.z / o.w : 1f);
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

    /// <summary>An accepted traveller left through <paramref name="portal"/>: without a switchboard its ring plays the kawoosh (with one, the board's send does).</summary>
    private void HandleDeparted(int portal)
    {
        HallPortalRing[] rings = _portals != null ? _portals.rings : null;
        if (rings == null || _board)
            return;
        for (int i = 0; i < rings.Length && i < _effects.Length; i++)
            if (rings[i] != null && rings[i].portal == portal && _effects[i] != null)
                _effects[i].Send();
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
