using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// The upgrades' objects on the desk (the desk machines spec §4; Saleh
/// 2026-10-17: "every upgrade should have a visual representation on the
/// desk" "and a satysfing interaction"): the supply crate on a delivery
/// morning, the Interview Protocols' card ring, the BABEL rack of the Speech
/// translators, the Translation Lens's stand, the Diplomatic Contacts'
/// Rolodex and the RING SERVICE panel of the portals' repairs (the
/// scanner's two upgrades sit on the scanner: ScannerAddOns). GameManager
/// starts each day with the world (BeginDay): each object stands on the desk
/// while its upgrade is owned; one delivered this morning appears when the
/// supply crate is opened (a click on it, or the shift opening). Each is
/// laid where the office view shows its place at 16:9 (DeskConfigSO, Lay,
/// through DeskHardware.Lay). Built by Build Office UI under DeskHardware
/// from the props' models (tools/props/machines).
/// </summary>
public sealed class DeskUpgrades : MonoBehaviour
{
    /// <summary>The upgrade ids the objects show (the hand-authored upgrades').</summary>
    public const string ProtocolsId = "interview_protocols", ContactsId = "diplo_contacts";

    [SerializeField] private SupplyCrate crate;
    [SerializeField] private ProtocolRing protocolRing;
    [SerializeField] private BabelRack babelRack;
    [SerializeField] private LensKit lensKit;
    [SerializeField] private Rolodex rolodex;
    [SerializeField] private RingPanel ringPanel;

    /// <summary>The desk tuning (the objects' places).</summary>
    [SerializeField] private DeskConfigSO config;

    private readonly List<Transform> _appear = new List<Transform>();
    private readonly List<Vector3> _appearScale = new List<Vector3>();
    private Spring _pop;
    private bool _popping;
    private readonly Dictionary<string, int> _bayOfTongue = new Dictionary<string, int>();

    /// <summary>The supply crate (null without its art).</summary>
    public SupplyCrate Crate => crate;

    /// <summary>The objects on the desk today (the binder keeps them clear of the scanner; the probes read them).</summary>
    public IEnumerable<Component> Shown
    {
        get
        {
            foreach (Component c in new Component[] { crate, protocolRing, babelRack, lensKit, rolodex, ringPanel })
                if (c != null && c.gameObject.activeSelf)
                    yield return c;
        }
    }

    private void OnEnable()
    {
        TravellerWheel.TongueTranslated += HandleTongue;
        TranslationLensPresenter.WordTurned += HandleWord;
    }

    private void OnDisable()
    {
        TravellerWheel.TongueTranslated -= HandleTongue;
        TranslationLensPresenter.WordTurned -= HandleWord;
    }

    /// <summary>
    /// Starts a day (GameManager, with the day's world, its library and its
    /// portals): each object while its upgrade is owned; the morning's
    /// deliveries (Orders.DeliveredOn) in the crate, their objects waiting
    /// for it to open.
    /// </summary>
    public void BeginDay(WorldState world, ContentLibrarySO library, PortalDay portals)
    {
        if (world == null || library == null)
            return;
        System.Func<string, bool> owns = world.HasUpgrade;
        List<string> delivered = Orders.DeliveredOn(world.orders, world.day);
        var today = new HashSet<string>(delivered);
        _appear.Clear();
        _appearScale.Clear();

        if (protocolRing != null)
            Show(protocolRing, owns(ProtocolsId), today.Contains(ProtocolsId));
        if (rolodex != null)
            Show(rolodex, owns(ContactsId), today.Contains(ContactsId));

        TranslationSettings translation = library.Translation;
        List<TranslatorPack> packs = translation.rules != null ? translation.rules.packs : null;
        if (babelRack != null)
        {
            var packIds = new List<string>();
            foreach (TranslatorPack p in packs ?? new List<TranslatorPack>())
                packIds.Add(p != null ? p.id : null);
            UpgradeObjects.Bay[] bays = UpgradeObjects.Bays(packIds, owns, today);
            bool rackNew = UpgradeObjects.RackNew(bays);
            _bayOfTongue.Clear();
            if (translation.rules != null && translation.rules.tongues != null)
                foreach (Tongue t in translation.rules.tongues)
                    if (t != null && !string.IsNullOrEmpty(t.id))
                        _bayOfTongue[t.id] = packIds.IndexOf(t.pack);
            Show(babelRack, babelRack.SetDay(bays, rackNew), rackNew);
        }
        if (lensKit != null)
        {
            List<string> levels = translation.lens != null && translation.lens.rules != null ? translation.lens.rules.levelIds : null;
            LensReach reach = TranslationLens.Reach(world.day, library.Introductions, levels, owns);
            bool fresh = levels != null && levels.Count > 0 && today.Contains(levels[0]);
            Show(lensKit, lensKit.SetReach(reach), fresh);
        }
        if (ringPanel != null)
        {
            UpgradeObjects.Module[] modules = UpgradeObjects.Modules(portals != null ? portals.Portals : null, owns, today);
            ringPanel.SetDay(modules);
            Show(ringPanel, UpgradeObjects.PanelShown(modules), UpgradeObjects.PanelNew(modules));
        }

        if (crate != null)
        {
            var names = new List<string>();
            foreach (string id in delivered)
            {
                UpgradeSO u = library.GetUpgradeById(id);
                names.Add(u != null && !string.IsNullOrEmpty(u.displayName) ? u.displayName : id);
            }
            crate.Deliver(names);
            crate.Opening -= Unpacked;
            crate.Opening += Unpacked;
            if (!crate.Present)
                Unpacked();
        }
        else
        {
            Unpacked();
        }
    }

    /// <summary>Shows <paramref name="prop"/> when <paramref name="owned"/>; one new this morning waits, shrunk, for the crate.</summary>
    private void Show(Component prop, bool owned, bool newToday)
    {
        prop.gameObject.SetActive(owned);
        if (!owned || !newToday || crate == null)
            return;
        _appear.Add(prop.transform);
        _appearScale.Add(prop.transform.localScale);
        prop.transform.localScale = Vector3.zero;
    }

    /// <summary>The crate opened: this morning's objects pop onto the desk and the new cartridges drop in.</summary>
    private void Unpacked()
    {
        if (babelRack != null)
            babelRack.Unpacked();
        if (_appear.Count == 0)
            return;
        _pop = Spring.At(0f);
        DeskProps.Aim(ref _pop, 1f);
        _pop.Kick(3f);
        _popping = true;
        Sounds.Play(SoundCues.UiPopup);
    }

    /// <summary>The shift opened (the AVAILABLE sign): a crate left shut opens itself.</summary>
    public void ShiftOpened()
    {
        if (crate != null && crate.Present && !crate.Opened)
            crate.Open();
    }

    /// <summary>A new traveller: the protocol ring's front card flicks over.</summary>
    public void BeginCase()
    {
        if (protocolRing != null && protocolRing.gameObject.activeInHierarchy)
            protocolRing.FlickFront();
    }

    /// <summary>A traveller went through portal <paramref name="portal"/>: the panel's lamp flashes.</summary>
    public void Departed(int portal)
    {
        if (ringPanel != null && ringPanel.gameObject.activeInHierarchy)
            ringPanel.Departed(portal);
    }

    private void HandleTongue(string tongueId)
    {
        if (babelRack != null && babelRack.gameObject.activeInHierarchy && tongueId != null && _bayOfTongue.TryGetValue(tongueId, out int bay))
            babelRack.Spoke(bay);
    }

    private void HandleWord()
    {
        if (lensKit != null && lensKit.gameObject.activeInHierarchy)
            lensKit.Tick();
    }

    private void Update()
    {
        if (!_popping)
            return;
        float dt = UiMotion.Delta(Time.unscaledDeltaTime);
        DeskProps.Step(ref _pop, dt, MotionFeel.Elastic);
        for (int i = 0; i < _appear.Count; i++)
            if (_appear[i] != null)
                _appear[i].localScale = _appearScale[i] * Mathf.Max(0f, _pop.Value);
        if (_pop.AtRest)
        {
            _popping = false;
            _appear.Clear();
            _appearScale.Clear();
        }
    }

    /// <summary>
    /// Lays the objects on the desk (DeskHardware.Lay): each where the office
    /// view shows its viewport point at 16:9 (DeskConfigSO), turned by its
    /// yaw, at DeskConfigSO.upgradeScale; the laid roots are added to
    /// <paramref name="laid"/>.
    /// </summary>
    public void Lay(CinemachineCamera office, float aspect, float deskTop, List<Transform> laid)
    {
        if (config == null || office == null)
            return;
        LayOne(crate, office, aspect, deskTop, config.supplyCrateView, config.supplyCrateYaw, laid);
        LayOne(protocolRing, office, aspect, deskTop, config.protocolRingView, config.protocolRingYaw, laid);
        LayOne(babelRack, office, aspect, deskTop, config.babelRackView, config.babelRackYaw, laid);
        LayOne(lensKit, office, aspect, deskTop, config.lensKitView, config.lensKitYaw, laid);
        LayOne(rolodex, office, aspect, deskTop, config.rolodexView, config.rolodexYaw, laid);
        LayOne(ringPanel, office, aspect, deskTop, config.ringPanelView, config.ringPanelYaw, laid);
        if (crate != null)
            crate.Laid();
    }

    private void LayOne(Component prop, CinemachineCamera office, float aspect, float deskTop, Vector2 view, float yaw, List<Transform> laid)
    {
        if (prop == null || !DeskLay.Lay(prop.transform, office, aspect, deskTop, view, yaw))
            return;
        Vector3 scale = Vector3.one * config.upgradeScale;
        int waiting = _appear.IndexOf(prop.transform);
        if (waiting >= 0)
            _appearScale[waiting] = scale; // still waiting for the crate: it pops to this scale
        else
            prop.transform.localScale = scale;
        laid.Add(prop.transform);
    }
}
