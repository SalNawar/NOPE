using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The Night Slots machine at Home (Saleh 2026-10-07: "the slot machine looks
/// terrible", make it a real 80s/90s machine in the cel kit), built by
/// HomeSceneBuilder: the marquee's chasing bulbs, three reels behind glass
/// (each a strip of the slot outcomes' symbols, shaded as a cylinder, the
/// payline across), the LCD strip with the night's line, the coin slot and
/// its price plate, the credits readout, the SPIN plate with its SPACE
/// keycap, the payout tray, and the lever on the right side. The lever is
/// the main way to spin: dragged down it ratchets and fires at the bottom,
/// let go it springs back (SlotLever); a click on it, the SPIN plate or
/// SPACE pulls it by itself. The outcome is drawn the moment the lever
/// fires (the onSpin callback: HomeManager's own seeded stream, unchanged);
/// the reels then only show it: SlotSpinSchedule spins them up, blurs them,
/// and stops them left to right on SlotReels' faces with a bounce and a
/// clunk. A win holds a beat (the hit-stop), flashes the bulbs, pulses the
/// winning symbols, drops coins into the tray and rolls the credits up; a
/// loss womps and dims the bulbs. The spin's cost and its payout show in
/// the machine's credits and the HUD's wallet together. Without the credits
/// for a spin the lever hangs chained and padlocked, the price plate blinks
/// "Insert credits", the SPIN plate is the kit's locked screentone, and a
/// pull says "no". Reduced Motion cuts the reels to the result and holds the
/// bulbs still; the Motion intensity scales the bounces (MotionKnobs.slots).
/// </summary>
public sealed class SlotMachineView : MonoBehaviour
{
    /// <summary>The symbol cells per reel: the payline's, and two above and two below as the strip turns.</summary>
    public const int CellsPerReel = 5;

    /// <summary>The kit sprite of an outcome's reel symbol is this plus the outcome's id (else the default symbol).</summary>
    private const string SymbolPrefix = "slot_sym_";

    [Header("Kit")]
    /// <summary>The cel UI kit: the symbols, bulbs, plates and keycap faces this view swaps.</summary>
    [SerializeField] private UiKitSO kit;

    [Header("Marquee")]
    /// <summary>The marquee's lit plate (dimmed on a loss).</summary>
    [SerializeField] private Image marqueeFace;

    /// <summary>The marquee's title (live text).</summary>
    [SerializeField] private TMP_Text titleText;

    /// <summary>The marquee's bulbs' faces, in chase order round the plate.</summary>
    [SerializeField] private Image[] bulbs = Array.Empty<Image>();

    [Header("Reels")]
    /// <summary>The reels' windows (each masks its strip; their height is two symbol pitches).</summary>
    [SerializeField] private RectTransform[] reels = Array.Empty<RectTransform>();

    /// <summary>The symbol cells, reel by reel (<see cref="CellsPerReel"/> each): moved, squashed round the drum and given their symbol every frame.</summary>
    [SerializeField] private RectTransform[] cells = Array.Empty<RectTransform>();

    /// <summary>The cells' kit faces (the symbol sprite), in the same order.</summary>
    [SerializeField] private Image[] cellFaces = Array.Empty<Image>();

    /// <summary>Each reel's win glow behind its payline symbol.</summary>
    [SerializeField] private RectTransform[] glows = Array.Empty<RectTransform>();

    [Header("Deck")]
    /// <summary>The LCD strip's line: the invitation, "Good luck...", the spin's result, or why it cannot spin.</summary>
    [SerializeField] private TMP_Text lcdText;

    /// <summary>The machine's credits readout.</summary>
    [SerializeField] private TMP_Text creditsText;

    /// <summary>The price plate's face (lit red while the wallet cannot pay) and its text.</summary>
    [SerializeField] private Image priceFace;

    /// <summary>The price plate's text: the spin's price, or "Insert credits".</summary>
    [SerializeField] private TMP_Text priceText;

    /// <summary>The SPIN plate (a click pulls the lever).</summary>
    [SerializeField] private Button spinButton;

    /// <summary>The SPACE keycap's face (pressed when SPACE pulls the lever, locked without credits).</summary>
    [SerializeField] private Image keyFace;

    [Header("Lever")]
    /// <summary>The lever as a whole (shaken sideways for a "no").</summary>
    [SerializeField] private RectTransform lever;

    /// <summary>The lever's rod, pivoting at the hub: foreshortened through 0 to point down as it is pulled toward the viewer.</summary>
    [SerializeField] private RectTransform leverArm;

    /// <summary>The lever's ball, kept on the rod's tip (and nearer, so larger, as it comes down).</summary>
    [SerializeField] private RectTransform leverBall;

    /// <summary>The lever's grip (its hit area).</summary>
    [SerializeField] private SlotLeverHandle leverHandle;

    /// <summary>The chain and padlock shown while the wallet cannot pay for a spin.</summary>
    [SerializeField] private GameObject leverLock;

    [Header("Tray")]
    /// <summary>The space the payout coins fall through and rest in (from the chute down to the tray's floor), between the tray's inside and its lip.</summary>
    [SerializeField] private RectTransform coinsRoot;

    /// <summary>The coin each payout coin is copied from (kept hidden).</summary>
    [SerializeField] private RectTransform coinTemplate;

    private enum Phase { Idle, Spinning, Hold }

    private sealed class Coin
    {
        public RectTransform Rect;
        public Vector2 At, Speed;
        public float Wait, Floor;
        public bool Resting;
    }

    private readonly SlotLever _lever = new SlotLever();
    private readonly List<Coin> _coins = new List<Coin>();
    private readonly float[] _pos = new float[SlotReels.Count];
    private readonly bool[] _landed = new bool[SlotReels.Count];
    private Sprite[] _symbols = Array.Empty<Sprite>(), _blurred = Array.Empty<Sprite>();
    private WorldState _world;
    private Func<HomeUIController.SpinView> _onSpin;
    private Action<int> _showWallet;
    private int _cost;
    private Phase _phase;
    private SlotSpinSchedule _schedule;
    private HomeUIController.SpinView _result;
    private int[] _faces = new int[SlotReels.Count];
    private float _clock, _hold, _flashUntil, _dimUntil, _since, _rollTime, _inviteAt, _keyUntil;
    private int _rollFrom, _rollTo, _shown;
    private bool _broke, _hitStopped, _paid, _dragging;
    private float _dragFrom, _dragMoved;
    private Spring _shake;
    private Vector2 _leverRest;
    private Color _marqueeColour = Color.white, _titleColour = Color.white;

    /// <summary>True while the reels spin (or hold on a win's hit-stop).</summary>
    public bool Spinning => _phase != Phase.Idle;

    /// <summary>The line for a wallet that cannot pay for a spin (the LCD's, and HomeManager's refusal).</summary>
    public static string BrokeLine() => $"Not enough {UiText.Currency(UiText.WalletForm.Inline)} to spin.";

    private void Awake()
    {
        if (lever != null)
            _leverRest = lever.anchoredPosition;
        if (marqueeFace != null)
            _marqueeColour = marqueeFace.color;
        if (titleText != null)
            _titleColour = titleText.color;
        if (coinTemplate != null)
            coinTemplate.gameObject.SetActive(false);
        if (leverHandle != null)
        {
            leverHandle.Pressed += HandleLeverPressed;
            leverHandle.Dragged += HandleLeverDragged;
            leverHandle.Released += HandleLeverReleased;
        }
        if (spinButton != null)
            spinButton.onClick.AddListener(PullLever);
    }

    private void OnDestroy()
    {
        if (leverHandle != null)
        {
            leverHandle.Pressed -= HandleLeverPressed;
            leverHandle.Dragged -= HandleLeverDragged;
            leverHandle.Released -= HandleLeverReleased;
        }
    }

    /// <summary>The panel closed mid-spin: the reels and the wallet cut to the result.</summary>
    private void OnDisable() => Finish();

    /// <summary>
    /// Shows the machine for tonight: <paramref name="world"/>'s wallet,
    /// <paramref name="cost"/> a spin, the reels' symbols from
    /// <paramref name="outcomeIds"/> (the library's slot outcomes, in order:
    /// SlotReels' faces index them), <paramref name="intro"/> on the LCD.
    /// <paramref name="onSpin"/> draws and applies a spin when the lever
    /// fires; <paramref name="showWallet"/> shows a wallet in the HUD (kept in
    /// step with the machine's credits).
    /// </summary>
    public void Show(WorldState world, int cost, IReadOnlyList<string> outcomeIds, string intro, Func<HomeUIController.SpinView> onSpin, Action<int> showWallet)
    {
        _world = world;
        _cost = cost;
        _onSpin = onSpin;
        _showWallet = showWallet;
        _phase = Phase.Idle;
        _schedule = null;
        _flashUntil = _dimUntil = 0f;
        _since = 0f;
        _inviteAt = UiMotion.Knobs.slots.leverInviteSeconds;
        _lever.Reset();
        _shake.Snap(0f);
        ClearCoins();
        LoadSymbols(outcomeIds);
        _faces = SlotReels.Faces(0, false, _symbols.Length);
        for (int i = 0; i < _pos.Length; i++)
            _pos[i] = _faces[i];
        if (titleText != null)
            titleText.text = "Night Slots";
        _rollTime = -1f;
        _shown = int.MinValue;
        ShowWallet(world != null ? world.money : 0);
        RefreshBroke();
        if (lcdText != null)
            lcdText.text = _broke ? BrokeLine() : intro;
        ShowGlows(false, 0f);
        Render(0f);
    }

    /// <summary>Cuts a running spin to its result (the reels on their faces, the line, the wallet), as when the panel closes.</summary>
    public void Finish()
    {
        if (_phase == Phase.Idle && _rollTime < 0f)
            return;
        if (_phase != Phase.Idle && !_paid && lcdText != null)
            lcdText.text = _result.Line;
        _paid = true;
        Settle();
        _rollTime = -1f;
        ShowWallet(_world != null ? _world.money : 0);
        RefreshBroke();
    }

    /// <summary>A click on the SPIN plate or SPACE: the lever pulls itself (it fires the spin at the bottom), or says "no" without the credits.</summary>
    private void PullLever()
    {
        if (_phase != Phase.Idle || _lever.Held)
            return;
        if (_broke)
        {
            Refuse();
            return;
        }
        _lever.PullAll(UiMotion.Amount);
    }

    private void HandleLeverPressed(PointerEventData data)
    {
        if (_broke || _phase != Phase.Idle)
        {
            Refuse();
            return;
        }
        _dragging = true;
        _dragFrom = data.position.y;
        _dragMoved = 0f;
    }

    private void HandleLeverDragged(PointerEventData data)
    {
        if (!_dragging)
            return;
        float scale = Mathf.Max(0.01f, CanvasScale());
        float down = (_dragFrom - data.position.y) / scale;
        _dragMoved = Mathf.Max(_dragMoved, Mathf.Abs(down));
        SlotLever.Move move = _lever.Drag(Mathf.Max(0f, down), UiMotion.Knobs.slots);
        Ratchet(move);
    }

    private void HandleLeverReleased(PointerEventData data)
    {
        if (!_dragging)
            return;
        _dragging = false;
        if (_dragMoved < 6f && !_lever.Held)
            _lever.PullAll(UiMotion.Amount); // a click: it pulls itself
        else
            _lever.Release(UiMotion.Amount);
    }

    /// <summary>The ratchet's clicks for the notches a move crossed; a spin when it fired.</summary>
    private void Ratchet(SlotLever.Move move)
    {
        for (int i = 0; i < move.Notches; i++)
            Sounds.Play(SoundCues.SlotLever);
        if (move.Fire)
            Spin();
    }

    /// <summary>The "no": the lever shakes sideways with the error cue.</summary>
    private void Refuse()
    {
        Sounds.Play(SoundCues.UiError);
        MotionKnobs knobs = UiMotion.Knobs;
        MotionAmount amount = UiMotion.Amount;
        if (!amount.Still)
            _shake.Kick(knobs.Get(knobs.refuseFeel).KickFor(knobs.refuseShake * amount.Share));
    }

    /// <summary>The lever fired: the outcome is drawn now (onSpin, the run's own stream), the cost shows, and the reels start toward its faces.</summary>
    private void Spin()
    {
        if (_phase != Phase.Idle || _onSpin == null || _world == null)
            return;
        int before = _world.money;
        HomeUIController.SpinView spin = _onSpin.Invoke();
        if (spin.Outcome < 0)
        {
            if (lcdText != null && !string.IsNullOrEmpty(spin.Line))
                lcdText.text = spin.Line;
            ShowWallet(_world.money);
            RefreshBroke();
            return;
        }
        _result = spin;
        _faces = SlotReels.Faces(spin.Outcome, spin.Win, _symbols.Length);
        _rollFrom = before - _cost;
        _rollTo = _world.money;
        _rollTime = -1f;
        _shown = int.MinValue; // the HUD was just set to the result (HomeManager.RefreshHud): show the cost alone in both
        ShowWallet(_rollFrom);
        ClearCoins();
        ShowGlows(false, 0f);
        _flashUntil = _dimUntil = 0f;
        for (int i = 0; i < _landed.Length; i++)
            _landed[i] = false;
        SlotSpinKnobs knobs = UiMotion.Knobs.slots;
        _schedule = new SlotSpinSchedule(_pos, _faces, _symbols.Length, knobs, UiMotion.Knobs.Get(knobs.landFeel), UiMotion.Amount);
        _clock = 0f;
        _hitStopped = _paid = false;
        _phase = Phase.Spinning;
        if (lcdText != null)
            lcdText.text = "Good luck...";
        Sounds.Play(SoundCues.SlotSpin);
        if (_schedule.Cut)
        {
            Payout();
            Settle();
        }
    }

    /// <summary>The reels rest on their faces exactly (the next spin starts from there).</summary>
    private void Settle()
    {
        for (int i = 0; i < _pos.Length; i++)
            _pos[i] = _faces[i];
        _phase = Phase.Idle;
        _schedule = null;
    }

    /// <summary>Every reel has landed (after a win's hit-stop): the result line, then the win's payout or the loss's dim, and the credits roll.</summary>
    private void Payout()
    {
        _paid = true;
        SlotSpinKnobs knobs = UiMotion.Knobs.slots;
        if (lcdText != null)
            lcdText.text = _result.Line;
        if (_result.Win)
        {
            Sounds.Play(SoundCues.SlotWin);
            _flashUntil = _since + knobs.winFlashSeconds;
            DropCoins(Mathf.Clamp((_rollTo - _rollFrom) / Mathf.Max(1, knobs.coinValue), 1, Mathf.Max(1, knobs.maxCoins)));
        }
        else
        {
            Sounds.Play(SoundCues.SlotLose);
            _dimUntil = _since + knobs.loseDimSeconds;
        }
        _rollTime = UiMotion.Amount.Reduced ? -1f : 0f;
        if (_rollTime < 0f)
            ShowWallet(_rollTo);
        RefreshBroke();
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        _since += dt;
        MotionKnobs motion = UiMotion.Knobs;
        SlotSpinKnobs knobs = motion.slots;
        MotionAmount amount = UiMotion.Amount;

        Keyboard keys = Keyboard.current;
        bool space = keys != null && keys.spaceKey.wasPressedThisFrame;
        if (space)
        {
            _keyUntil = _since + 0.15f;
            PullLever();
        }

        Ratchet(_lever.Step(dt, knobs, motion, amount));
        if (_phase == Phase.Idle && !_broke && !_lever.Held && !_lever.Moving && _since >= _inviteAt)
        {
            _lever.Nod(knobs.leverInvite, knobs, motion, amount);
            _inviteAt = _since + knobs.leverInviteSeconds;
        }
        _shake.Step(dt, motion.Get(motion.refuseFeel), motion.settleValue * 100f, motion.settleSpeed * 100f);

        if (_phase == Phase.Hold)
        {
            _hold -= dt;
            if (_hold <= 0f)
                _phase = Phase.Spinning;
        }
        else if (_phase == Phase.Spinning && _schedule != null)
        {
            _clock += dt;
            for (int i = 0; i < _pos.Length; i++)
            {
                _pos[i] = _schedule.Position(i, _clock);
                if (!_landed[i] && _clock >= _schedule.LandTime(i))
                {
                    _landed[i] = true;
                    Sounds.Play(SoundCues.SlotStop);
                }
            }
            bool allLanded = Array.TrueForAll(_landed, l => l);
            if (allLanded && _result.Win && !_hitStopped && knobs.winHitStopSeconds > 0f && !amount.Still)
            {
                _hitStopped = true;
                _hold = knobs.winHitStopSeconds;
                _phase = Phase.Hold;
            }
            else
            {
                if (allLanded && !_paid)
                    Payout();
                if (_clock >= _schedule.Duration)
                    Settle();
            }
        }

        if (_rollTime >= 0f)
        {
            _rollTime += dt;
            float t = Mathf.Clamp01(_rollTime / Mathf.Max(0.01f, knobs.rollSeconds));
            ShowWallet(Mathf.RoundToInt(Mathf.Lerp(_rollFrom, _rollTo, UiMotion.Ease(t, MotionFeel.Balanced, knobs.rollSeconds))));
            if (t >= 1f)
                _rollTime = -1f;
        }

        StepCoins(dt, knobs);
        Render(dt);
    }

    /// <summary>Draws the reels, the lever, the bulbs, the glows, the price plate and the keycap as they stand.</summary>
    private void Render(float dt)
    {
        SlotSpinKnobs knobs = UiMotion.Knobs.slots;
        MotionAmount amount = UiMotion.Amount;
        bool flashing = _since < _flashUntil;
        float pulse = flashing && !amount.Still ? 1f + knobs.winPulse * amount.Share * Mathf.Abs(Mathf.Sin(_since * Mathf.PI * knobs.winPulseRate)) : 1f;
        ShowGlows(flashing, pulse);
        for (int r = 0; r < reels.Length && r < _pos.Length; r++)
        {
            float speed = _phase == Phase.Spinning && _schedule != null ? _schedule.Speed(r, _clock) : 0f;
            DrawReel(r, _pos[r], speed, flashing ? pulse : 1f, knobs, amount);
        }
        DrawLever();
        DrawBulbs(knobs, amount, flashing);
        DrawDeck(knobs, amount);
    }

    /// <summary>One reel at <paramref name="position"/>: each cell shows its symbol at its height on the drum (squashed toward the window's edges), smeared and stretched when fast.</summary>
    private void DrawReel(int reel, float position, float speed, float pulse, SlotSpinKnobs knobs, MotionAmount amount)
    {
        RectTransform window = reels[reel];
        if (window == null)
            return;
        float pitch = window.rect.height / 2f;
        float radius = window.rect.height * 0.62f;
        int count = _symbols.Length;
        int nearest = Mathf.RoundToInt(position);
        float frac = position - nearest;
        bool blur = speed > knobs.blurSpeed;
        float stretch = amount.Reduced ? 0f : Mathf.Min(knobs.maxStretch, speed * knobs.stretchPerSpeed);
        for (int k = 0; k < CellsPerReel; k++)
        {
            int index = reel * CellsPerReel + k;
            if (index >= cells.Length || cells[index] == null)
                continue;
            int offset = k - CellsPerReel / 2;
            float height = (offset - frac) * pitch;
            float angle = height / radius;
            bool seen = Mathf.Abs(angle) < Mathf.PI / 2f - 0.02f && count > 0;
            RectTransform cell = cells[index];
            if (cell.gameObject.activeSelf != seen)
                cell.gameObject.SetActive(seen);
            if (!seen)
                continue;
            int symbol = ((nearest + offset) % count + count) % count;
            Image face = index < cellFaces.Length ? cellFaces[index] : null;
            Sprite sprite = blur && _blurred[symbol] != null ? _blurred[symbol] : _symbols[symbol];
            if (face != null && sprite != null && face.sprite != sprite)
                face.sprite = sprite;
            cell.anchoredPosition = new Vector2(0f, radius * Mathf.Sin(angle));
            float grow = offset == 0 ? pulse : 1f;
            cell.localScale = new Vector3(grow, Mathf.Cos(angle) * (1f + stretch) * grow, 1f);
        }
    }

    /// <summary>The lever's rod foreshortened by its pull (it rotates toward the viewer), the ball on its tip, the "no" shake.</summary>
    private void DrawLever()
    {
        float pull = _lever.Pull;
        if (leverArm != null)
        {
            float tilt = 1f - 1.85f * pull;
            leverArm.localScale = new Vector3(1f, Mathf.Abs(tilt) < 0.02f ? 0.02f : tilt, 1f);
            if (leverBall != null)
            {
                leverBall.anchoredPosition = leverArm.anchoredPosition + new Vector2(0f, leverArm.rect.height * tilt);
                float near = 1f + 0.28f * Mathf.Clamp01(pull);
                leverBall.localScale = new Vector3(near, near, 1f);
            }
        }
        if (lever != null)
            lever.anchoredPosition = _leverRest + new Vector2(_shake.Value, 0f);
    }

    /// <summary>The marquee's bulbs: chasing at rest, all flashing on a win, out on a loss (Reduced Motion: steady).</summary>
    private void DrawBulbs(SlotSpinKnobs knobs, MotionAmount amount, bool flashing)
    {
        Sprite on = Kit("slot_bulb_on"), off = Kit("slot_bulb_off");
        bool dim = _since < _dimUntil;
        int chase = amount.Still ? 0 : Mathf.FloorToInt(_since / Mathf.Max(0.02f, knobs.chaseStepSeconds));
        bool flashOn = amount.Still || Mathf.FloorToInt(_since / Mathf.Max(0.02f, knobs.flashStepSeconds)) % 2 == 0;
        for (int i = 0; i < bulbs.Length; i++)
        {
            if (bulbs[i] == null)
                continue;
            bool lit = dim ? false : flashing ? flashOn : amount.Still ? i % 2 == 0 : (i + chase) % 3 == 0;
            Sprite sprite = lit ? on : off;
            if (sprite != null && bulbs[i].sprite != sprite)
                bulbs[i].sprite = sprite;
        }
        if (marqueeFace != null)
            marqueeFace.color = dim || _broke ? _marqueeColour * new Color(0.72f, 0.66f, 0.66f, 1f) : _marqueeColour;
        if (titleText != null)
            titleText.color = dim || _broke ? new Color(_titleColour.r * 0.75f, _titleColour.g * 0.75f, _titleColour.b * 0.75f, _titleColour.a) : _titleColour;
    }

    /// <summary>The price plate ("Insert credits" blinking red and brass without the credits) and the SPACE keycap's face.</summary>
    private void DrawDeck(SlotSpinKnobs knobs, MotionAmount amount)
    {
        bool blinkOn = !_broke || amount.Reduced || Mathf.FloorToInt(_since / Mathf.Max(0.05f, knobs.insertBlinkSeconds)) % 2 == 0;
        if (priceFace != null)
        {
            Sprite plate = Kit(_broke && blinkOn ? "slot_priceplate_alert" : "slot_priceplate");
            if (plate != null && priceFace.sprite != plate)
                priceFace.sprite = plate;
        }
        if (priceText != null)
            priceText.color = _broke && blinkOn ? (kit != null ? kit.inkOnDark : Color.white) : (kit != null ? kit.inkOnLight : Color.black); // the plate blinks red and brass, the words stay readable on both
        if (keyFace != null)
        {
            Sprite cap = Kit(_broke ? "keycap_bone_locked" : _since < _keyUntil ? "keycap_bone_pressed" : "keycap_bone_rest");
            if (cap != null && keyFace.sprite != cap)
                keyFace.sprite = cap;
        }
    }

    /// <summary>Whether the wallet can pay for a spin: the SPIN plate, the lever's chain, the price plate's words.</summary>
    private void RefreshBroke()
    {
        _broke = _world != null && _symbols.Length > 0 && _world.money < _cost;
        if (spinButton != null)
            spinButton.interactable = !_broke;
        if (leverLock != null)
            leverLock.SetActive(_broke);
        if (priceText != null)
            priceText.text = _broke ? $"Insert {UiText.Currency(UiText.WalletForm.Inline)}" : $"{_cost} {UiText.Currency(UiText.WalletForm.Short)}";
        if (_broke)
            _lever.Reset();
    }

    /// <summary>Shows <paramref name="money"/> on the machine's credits and the HUD's wallet together.</summary>
    private void ShowWallet(int money)
    {
        if (money == _shown)
            return;
        _shown = money;
        if (creditsText != null)
            creditsText.text = $"{money} {UiText.Currency(UiText.WalletForm.Short)}";
        _showWallet?.Invoke(money);
    }

    /// <summary>The win glows behind the payline symbols (on while the win flashes, pulsing).</summary>
    private void ShowGlows(bool on, float pulse)
    {
        for (int i = 0; i < glows.Length; i++)
        {
            if (glows[i] == null)
                continue;
            if (glows[i].gameObject.activeSelf != on)
                glows[i].gameObject.SetActive(on);
            if (on)
                glows[i].localScale = new Vector3(pulse, pulse, 1f);
        }
    }

    /// <summary>The reels' symbols for the outcomes (each id's kit symbol and its smeared twin; the default symbol for an id the kit lacks).</summary>
    private void LoadSymbols(IReadOnlyList<string> ids)
    {
        int count = ids != null ? ids.Count : 0;
        _symbols = new Sprite[count];
        _blurred = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            string name = SymbolPrefix + ids[i];
            bool known = Kit(name) != null;
            _symbols[i] = known ? Kit(name) : Kit(SymbolPrefix + "default");
            _blurred[i] = known ? Kit(name + "_blur") : Kit(SymbolPrefix + "default_blur");
        }
    }

    /// <summary>Drops <paramref name="count"/> coins from the chute into the tray, one after another (Reduced Motion: they are simply there).</summary>
    private void DropCoins(int count)
    {
        if (coinsRoot == null || coinTemplate == null)
            return;
        SlotSpinKnobs knobs = UiMotion.Knobs.slots;
        bool still = UiMotion.Amount.Still;
        Vector2 area = coinsRoot.rect.size;                      // the coins sit on the root's centre
        float size = coinTemplate.rect.height;
        for (int i = 0; i < count; i++)
        {
            RectTransform rect = Instantiate(coinTemplate, coinsRoot);
            rect.gameObject.SetActive(true);
            float spread = Mathf.Repeat(i * 0.618034f, 1f) - 0.5f;          // a fixed scatter: the coins are only a show
            var coin = new Coin
            {
                Rect = rect,
                At = new Vector2(spread * 12f, area.y / 2f - size / 2f),
                Speed = new Vector2(spread * 260f, 0f),
                Wait = still ? 0f : i * knobs.coinStaggerSeconds,
                Floor = -area.y / 2f + size / 2f + Mathf.Repeat(i * 0.37f, 1f) * size * 0.5f,
            };
            if (still)
            {
                coin.At = new Vector2(spread * (area.x - size), coin.Floor);
                coin.Resting = true;
            }
            rect.anchoredPosition = coin.At;
            rect.gameObject.SetActive(!(!still && coin.Wait > 0f));
            _coins.Add(coin);
        }
    }

    /// <summary>The coins' fall: gravity, one bounce on the tray's floor, a clink as each lands, kept inside the tray.</summary>
    private void StepCoins(float dt, SlotSpinKnobs knobs)
    {
        if (coinsRoot == null)
            return;
        float half = coinsRoot.rect.width / 2f - (coinTemplate != null ? coinTemplate.rect.width / 2f : 0f);
        for (int i = 0; i < _coins.Count; i++)
        {
            Coin coin = _coins[i];
            if (coin.Resting || coin.Rect == null)
                continue;
            if (coin.Wait > 0f)
            {
                coin.Wait -= dt;
                if (coin.Wait > 0f)
                    continue;
                coin.Rect.gameObject.SetActive(true);
            }
            coin.Speed.y -= knobs.coinGravity * dt;
            coin.At += coin.Speed * dt;
            if (Mathf.Abs(coin.At.x) > half)
            {
                coin.At.x = Mathf.Sign(coin.At.x) * half;
                coin.Speed.x = -coin.Speed.x * 0.5f;
            }
            if (coin.At.y <= coin.Floor)
            {
                coin.At.y = coin.Floor;
                if (coin.Speed.y < -120f)
                {
                    if (i % 3 == 0)
                        Sounds.Play(SoundCues.Coins);
                    coin.Speed = new Vector2(coin.Speed.x * 0.5f, -coin.Speed.y * knobs.coinBounce);
                }
                else
                {
                    coin.Speed = Vector2.zero;
                    coin.Resting = true;
                }
            }
            coin.Rect.anchoredPosition = coin.At;
        }
    }

    /// <summary>Takes the coins out of the tray.</summary>
    private void ClearCoins()
    {
        for (int i = 0; i < _coins.Count; i++)
            if (_coins[i].Rect != null)
                Destroy(_coins[i].Rect.gameObject);
        _coins.Clear();
    }

    /// <summary>The canvas's scale (screen px per reference px), to read a drag in reference px.</summary>
    private float CanvasScale()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        return canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
    }

    private Sprite Kit(string name) => kit != null ? kit.Get(name) : null;
}
