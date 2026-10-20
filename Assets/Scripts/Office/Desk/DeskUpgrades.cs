using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The upgrades' objects on the desk (the desk machines spec §4; Saleh
/// 2026-10-17: "every upgrade should have a visual representation on the
/// desk" "and a satysfing interaction"): the supply crate on a delivery
/// morning, the Interview Protocols' card ring, the BABEL rack of the Speech
/// translators, the Diplomatic Contacts' Rolodex and the RING SERVICE
/// panel of the portals' repairs (the
/// scanner's two upgrades sit on the scanner: ScannerAddOns). GameManager
/// starts each day with the world (BeginDay): each object stands on the desk
/// while its upgrade is owned; one delivered this morning appears when the
/// supply crate is opened (a click on it, or the shift opening), and so do
/// the scanner's add-ons delivered that morning (BUG-60a: one arrival path,
/// nothing shows before the crate opens). The crate never brings what
/// already stood on the desk (DeskDeliveries.ToUnpack: BUG-28c/28d). Each
/// object is laid on its home of the desk home map (DeskLayoutSettings,
/// Lay, through DeskHardware.Lay), shrunk to fit it when it is bigger, and
/// tracked there (DeskLayout: its obstacle follows whether it shows). Built
/// by Build Office UI under DeskHardware from the props' models
/// (tools/props/machines).
/// </summary>
public sealed class DeskUpgrades : MonoBehaviour
{
    /// <summary>The upgrade ids the objects show (the hand-authored upgrades').</summary>
    public const string ProtocolsId = "interview_protocols", ContactsId = "diplo_contacts";

    [SerializeField] private SupplyCrate crate;
    [SerializeField] private ProtocolRing protocolRing;
    [SerializeField] private BabelRack babelRack;
    [SerializeField] private Rolodex rolodex;
    [SerializeField] private RingPanel ringPanel;

    /// <summary>The scanner's add-ons (the Auto-Feed tray, the Analysis beacon; optional): one delivered this morning waits for the crate too.</summary>
    [SerializeField] private ScannerAddOns scannerAddOns;

    /// <summary>The desk tuning (the objects' places).</summary>
    [SerializeField] private DeskConfigSO config;

    /// <summary>The overlay's tooltip (shared with the desk props' reactions): an object reads out what it tells there (the RING SERVICE panel's ring, the Rolodex's card).</summary>
    [SerializeField] private OverlayCallout tooltip;

    /// <summary>How long a read-out stays (seconds), and how far over its point it hangs (canvas px).</summary>
    [SerializeField] private float sayingSeconds = 6f;

    /// <inheritdoc cref="sayingSeconds"/>
    [SerializeField] private Vector2 sayingOffset = new Vector2(0f, 60f);

    private readonly List<Transform> _appear = new List<Transform>();
    private readonly List<Vector3> _appearScale = new List<Vector3>();
    private Spring _pop;
    private bool _popping;
    private readonly Dictionary<string, int> _bayOfTongue = new Dictionary<string, int>();

    /// <summary>The upgrades whose objects stood on the desk at the last day's start (null before the first): the crate never brings one of them again.</summary>
    private HashSet<string> _onDesk;

    /// <summary>The supply crate (null without its art).</summary>
    public SupplyCrate Crate => crate;

    /// <summary>Wires the objects that work with the investigation (GameManager, once, before the first day): the card ring asks its questions, the BABEL rack replays the traveller's last line and lights the cartridge that covers their tongue.</summary>
    public void Bind(InvestigationUIController investigation)
    {
        _investigation = investigation;
        if (protocolRing != null)
            protocolRing.Bind(investigation);
        if (babelRack != null)
            babelRack.Bind(investigation != null ? investigation.ReplayLastLine : (System.Func<bool>)null);
    }

    private InvestigationUIController _investigation;

    private void Awake()
    {
        if (ringPanel != null)
            ringPanel.Bind(Say);
        if (rolodex != null)
            rolodex.Bind(Say);
    }

    /// <summary>An object reads <paramref name="text"/> out over <paramref name="at"/> in the overlay's tooltip.</summary>
    private void Say(string text, Transform at)
    {
        if (tooltip != null && !string.IsNullOrEmpty(text))
            tooltip.Show(text, at != null ? at : transform, sayingOffset, sayingSeconds);
    }

    private void OnEnable()
    {
        TravellerWheel.TongueTranslated += HandleTongue;
    }

    private void OnDisable()
    {
        TravellerWheel.TongueTranslated -= HandleTongue;
    }

    /// <summary>
    /// Starts a day (GameManager, with the day's world, its library, its
    /// portals, its travellers and its seed): each object while its upgrade
    /// is owned; the morning's deliveries (Orders.DeliveredOn) in the crate,
    /// their objects waiting for it to open; the Rolodex's cards, what each
    /// contact hears of <paramref name="travellers"/> (ContactCards.ForDay on
    /// the day's contacts stream, Seeds.ForContacts of <paramref name="daySeed"/>).
    /// </summary>
    public void BeginDay(WorldState world, ContentLibrarySO library, PortalDay portals, IReadOnlyList<CaseInstance> travellers, int daySeed)
    {
        if (world == null || library == null)
            return;
        System.Func<string, bool> owns = world.HasUpgrade;
        List<string> delivered = DeskDeliveries.ToUnpack(Orders.DeliveredOn(world.orders, world.day), _onDesk);
        var today = new HashSet<string>(delivered);
        _onDesk = new HashSet<string>();
        foreach (UpgradeSO u in library.Upgrades)
            if (u != null && owns(u.id))
                _onDesk.Add(u.id);
        _appear.Clear();
        _appearScale.Clear();
        if (scannerAddOns != null)
        {
            if (today.Contains(ScannerDay.AutoFeedUpgradeId))
                Wait(scannerAddOns.Tray);
            if (today.Contains(ScannerDay.AnalysisUpgradeId))
                Wait(scannerAddOns.Beacon);
        }

        if (protocolRing != null)
            Show(protocolRing, owns(ProtocolsId), today.Contains(ProtocolsId));
        if (rolodex != null)
        {
            Show(rolodex, owns(ContactsId), today.Contains(ContactsId));
            var leads = new List<ContactLead>();
            foreach (CaseInstance c in travellers ?? new CaseInstance[0])
                if (c != null)
                    leads.Add(new ContactLead(c.lie, c.originLabel));
            rolodex.SetDay(ContactCards.ForDay(library.Agency.contacts, leads, new SeededRandom(Seeds.ForContacts(daySeed))));
        }

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
        if (ringPanel != null)
        {
            IReadOnlyList<PortalRoute> routes = portals != null ? portals.Portals : null;
            UpgradeObjects.Module[] modules = UpgradeObjects.Modules(routes, owns, today);
            var lines = new string[UpgradeObjects.RingModules];
            foreach (PortalRoute p in routes ?? new PortalRoute[0])
                if (p.Number >= 1 && p.Number <= lines.Length)
                    lines[p.Number - 1] = PortalText.TooltipLine(p, r => PortalsWindow.EraName(library, r), r => PortalsWindow.PlaceName(library, r), UiText.Get);
            ringPanel.SetDay(modules, lines);
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
        DeskLayout.RefreshObstacles();
    }

    /// <summary>Shows <paramref name="prop"/> when <paramref name="owned"/>; one new this morning waits, shrunk, for the crate.</summary>
    private void Show(Component prop, bool owned, bool newToday)
    {
        prop.gameObject.SetActive(owned);
        if (owned && newToday)
            Wait(prop.transform);
    }

    /// <summary><paramref name="part"/> came this morning: it waits, shrunk, for the crate to open (with no crate, it simply shows).</summary>
    private void Wait(Transform part)
    {
        if (part == null || crate == null || _appear.Contains(part))
            return;
        _appear.Add(part);
        _appearScale.Add(part.localScale);
        part.localScale = Vector3.zero;
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
        _pop.Kick(3f, UiMotion.Amount);
        _popping = true;
        Sounds.Play(SoundCues.UiPopup);
    }

    /// <summary>The shift opened (the AVAILABLE sign): a crate left shut opens itself.</summary>
    public void ShiftOpened()
    {
        if (crate != null && crate.Present && !crate.Opened)
            crate.Open();
    }

    /// <summary>A new traveller: the protocol ring's front card flicks over; the BABEL rack's cartridge that covers their tongue (when it is foreign today) keeps its pilot lit.</summary>
    public void BeginCase()
    {
        if (protocolRing != null && protocolRing.gameObject.activeInHierarchy)
            protocolRing.FlickFront();
        if (babelRack != null)
        {
            string tongue = _investigation != null ? _investigation.ForeignTongue : null;
            babelRack.SetSpeaker(tongue != null && _bayOfTongue.TryGetValue(tongue, out int bay) ? bay : -1);
        }
    }

    /// <summary>The traveller left: the rack's pilot goes out.</summary>
    public void EndCase()
    {
        if (babelRack != null)
            babelRack.SetSpeaker(-1);
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
            DeskLayout.RefreshObstacles();
        }
    }

    /// <summary>
    /// Lays the objects on their homes (DeskHardware.Lay; the desk home map,
    /// DeskLayoutSettings in <paramref name="frame"/>): the crate on its
    /// delivery spot right of the rail, the protocol cards and the Rolodex on
    /// the counter's row left of DETAIN, the BABEL rack on the BABEL tuner's
    /// home left of the mat (BABEL-TUNER swaps the tuner in there), the RING
    /// SERVICE panel on the switchboard's home at the booth's right end, each
    /// at DeskConfigSO.upgradeScale, shrunk to fit its home when bigger, and
    /// tracked there; the laid roots are added to <paramref name="laid"/>.
    /// </summary>
    public void Lay(DeskFrame frame, List<Transform> laid)
    {
        if (config == null)
            return;
        LayOne(crate, frame, DeskHomeMap.Crate, laid);
        LayOne(protocolRing, frame, DeskHomeMap.ProtocolRing, laid);
        LayOne(babelRack, frame, DeskHomeMap.BabelTuner, laid);
        LayOne(rolodex, frame, DeskHomeMap.Rolodex, laid);
        LayOne(ringPanel, frame, DeskHomeMap.Switchboard, laid);
        if (crate != null)
            crate.Laid();
    }

    private void LayOne(Component prop, DeskFrame frame, string homeId, List<Transform> laid)
    {
        DeskHome home = config.layout.HomeOf(homeId);
        if (prop == null || home == null || !frame.Place(prop.transform, new DeskBay(home), 0f))
            return;
        Vector3 was = prop.transform.localScale;
        prop.transform.localScale = Vector3.one * config.upgradeScale; // measured at its size (one waiting for the crate keeps its zero)
        float fit = DeskLayout.FitScale(prop.transform, home, frame);
        prop.transform.localScale = was;
        Placed(prop, Vector3.one * config.upgradeScale * fit, laid);
        DeskLayout.Track(homeId, prop.transform);
    }

    /// <summary>A laid object takes its <paramref name="scale"/> (one still waiting for the crate pops to it) and joins <paramref name="laid"/>.</summary>
    private void Placed(Component prop, Vector3 scale, List<Transform> laid)
    {
        int waiting = _appear.IndexOf(prop.transform);
        if (waiting >= 0)
            _appearScale[waiting] = scale; // still waiting for the crate: it pops to this scale
        else
            prop.transform.localScale = scale;
        laid.Add(prop.transform);
    }
}
