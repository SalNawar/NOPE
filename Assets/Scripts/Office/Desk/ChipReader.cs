using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The chip reader (the desk machines spec §1; Saleh 2026-10-17: "the chip
/// reader should be an actual machine that reads the chip"): an 80s card
/// terminal on the desk. A paper dropped on it (DeskController) is read once
/// the desk has docked it (IPaperDock; Saleh's 1018a playtest: a card slides
/// into the slot at the slot's height and angle, a passport comes down onto
/// the RF pad; a paper with no chip is refused: it bounces back with the
/// FAULT lamp's blink and the fault buzz, Refuse, and never passes through
/// the casing): it goes in with a clunk and the slot flap's flick, the
/// readout scrolls READING with its ticks and the amber READ lamp, then the
/// result shows: CHIP OK with a two-tone beep and a green flash, NO CHIP, or
/// MISMATCH with a low buzz, the red FAULT lamp and a small hit, then on its
/// one-line readout MISMATCH and each differing line in turn ("CITIZEN ID
/// QRT-301", ChipReaderCycle.Lines); the
/// paper comes back out and the desk carries it home, back out of the slot
/// or up off the pad and over the casing. The chip counts as
/// read (DeskReadings.ReadChip): the scanned copy's CHIP tab shows it. The
/// result stays on the readout until the next read; in inspect mode a click
/// on the readout picks its shown line (the first while MISMATCH shows)
/// against the box (DeskHardware.Pick), otherwise it steps to the next line. Its parts are the ChipReader art's
/// (Build Office UI binds them); the office binder lays it on the desk.
/// </summary>
public sealed class ChipReader : MonoBehaviour, IPaperDock
{
    /// <summary>The slot's dust flap (it flicks in as a paper goes in), the pad's top centre (where a passport lies to be read), the slot's mouth (a card's centre as it starts in; it slides along the mouth's forward: the model's CardSlot).</summary>
    [SerializeField] private Transform slotFlap, padCentre, cardSlot;

    /// <summary>How deep the slot runs into the casing from its mouth (the model's metres; the reader's scale applies).</summary>
    [SerializeField] private float slotDepth = 0.05f;

    /// <summary>How long the FAULT lamp blinks when the reader refuses a paper (seconds; two blinks).</summary>
    [SerializeField] private float refuseBlinkSeconds = 0.5f;

    /// <summary>The status lamps: POWER (green), READ (amber), FAULT (red).</summary>
    [SerializeField] private Renderer ledPower, ledRead, ledFault;

    /// <summary>The green readout's live text, over the art's VFD.</summary>
    [SerializeField] private TMP_Text readout;

    /// <summary>The readout's click box (inspect: pick the shown line; otherwise: the next line).</summary>
    [SerializeField] private Clickable readoutClick;

    /// <summary>The cycle's sounds (their placeholders until the bank has them).</summary>
    [SerializeField] private AudioSource sound;

    /// <summary>Where a dropped paper counts as on the reader: its footprint in local XZ, grown by a margin.</summary>
    [SerializeField] private Vector2 dropSize = new Vector2(0.26f, 0.26f);

    /// <summary>A lamp's glow when lit (its colour's multiplier) and dark.</summary>
    [SerializeField] private float lampOn = 2.2f, lampOff = 0.25f;

    private DeskHardware _hardware;
    private ChipReaderCycle _cycle;
    private DeskDocument _paper;
    private Action _done;
    private int _document = -1;
    private bool _hasResult;
    private ChipOutcome _outcome;
    private IReadOnlyList<ChipLine> _lines = Array.Empty<ChipLine>();
    private int _line;
    private float _lineAt, _flap, _okFlash, _refused;
    private int _ticks;
    private string _shown;
    private MaterialPropertyBlock _block;

    /// <summary>True while a paper is in it.</summary>
    public bool Busy => _cycle != null && _cycle.Busy;

    /// <summary>The paper its last result is about (-1: none yet this case).</summary>
    public int Document => _document;

    /// <summary>The last result (valid once a paper has been read this case).</summary>
    public ChipOutcome Outcome => _outcome;

    /// <summary>The readout's text as shown (the probes read it).</summary>
    public string Shown => _shown;

    /// <summary>Wires it to its machines' hub (DeskHardware) and makes its cycle from the desk tuning.</summary>
    public void Bind(DeskHardware hardware)
    {
        _hardware = hardware;
        DeskConfigSO c = hardware != null ? hardware.Config : null;
        _cycle = c != null ? new ChipReaderCycle(c.chipInsertSeconds, c.chipReadSeconds, c.chipResultSeconds, c.chipEjectSeconds) : new ChipReaderCycle(0.35f, 1.1f, 1.2f, 0.35f);
        if (readoutClick != null)
        {
            readoutClick.onClick.RemoveListener(ReadoutClicked);
            readoutClick.onClick.AddListener(ReadoutClicked);
        }
        Lamps();
        Show();
    }

    /// <summary>True when a world point lies over the reader (its drop area).</summary>
    public bool Contains(Vector3 world)
    {
        Vector3 local = transform.InverseTransformPoint(world);
        return Mathf.Abs(local.x) <= dropSize.x / 2f && Mathf.Abs(local.z) <= dropSize.y / 2f;
    }

    /// <summary>Reads <paramref name="paper"/> (its <paramref name="print"/>; its <paramref name="chip"/>, null: none), docked in it by the desk (in the slot or on the pad): the cycle runs; <paramref name="done"/> as it comes back out. False while busy.</summary>
    public bool Read(DeskDocument paper, IReadOnlyList<DocumentField> print, IReadOnlyList<DocumentField> chip, Action done)
    {
        if (_cycle == null || paper == null || !_cycle.Begin(paper.Index, print, chip))
            return false;
        _paper = paper;
        _done = done;
        _hasResult = false;
        _ticks = 0;
        _flap = 1f;
        CueSounds.Play(SoundCues.ChipInsert, sound);
        FeelDirector.Hit(0.15f);
        Lamps();
        Show();
        return true;
    }

    /// <summary>A new case: the readout back to READY, the lamps dark.</summary>
    public void ResetCase()
    {
        Cancel();
        _document = -1;
        _hasResult = false;
        _lines = Array.Empty<ChipLine>();
        Lamps();
        Show();
    }

    /// <summary>Stops at once (the case ended): no paper in it.</summary>
    public void Cancel()
    {
        _cycle?.Cancel();
        _paper = null;
        _done = null;
    }

    private void Update()
    {
        if (_cycle == null)
            return;
        float dt = UiMotion.Delta(Time.unscaledDeltaTime);
        if (_flap > 0f && slotFlap != null)
        {
            _flap = UiMotion.Amount.Still ? 0f : Mathf.Max(0f, _flap - dt * 4f);
            slotFlap.localRotation = Quaternion.Euler(-55f * Mathf.Sin(_flap * Mathf.PI), 0f, 0f);
        }
        if (_okFlash > 0f)
        {
            _okFlash = Mathf.Max(0f, _okFlash - dt);
            Lamps();
        }
        if (_refused > 0f)
        {
            _refused = Mathf.Max(0f, _refused - dt);
            Lamps();
        }

        if (_cycle.Busy)
        {
            ChipReaderStep? entered = _cycle.Tick(dt);
            if (entered == ChipReaderStep.Reading)
                Lamps();
            else if (entered == ChipReaderStep.Result || entered == ChipReaderStep.Eject || entered == ChipReaderStep.Idle)
            {
                if (!_hasResult)
                    Result();
                if (entered != ChipReaderStep.Result)
                    Eject();
                if (entered == ChipReaderStep.Idle)
                    Finish();
            }
            if (_cycle.Step == ChipReaderStep.Reading)
            {
                int ticks = Mathf.FloorToInt(_cycle.Progress * 6f);
                if (ticks > _ticks)
                {
                    _ticks = ticks;
                    CueSounds.Play(SoundCues.ChipRead, sound, 1f + 0.05f * ticks);
                }
            }
        }

        if (_hasResult && _lines.Count > 0)
        {
            _lineAt += dt;
            DeskConfigSO c = _hardware != null ? _hardware.Config : null;
            if (_lineAt >= (c != null ? c.chipLineSeconds : 1.8f))
            {
                _lineAt = 0f;
                _line = (_line + 1) % (_lines.Count + 1);
            }
        }
        Show();
    }

    /// <summary>The result: the chip counts as read, the beep or buzz, the lamp, the lines.</summary>
    private void Result()
    {
        _hasResult = true;
        _document = _cycle.Document >= 0 ? _cycle.Document : _document;
        _outcome = _cycle.Outcome;
        _lines = _cycle.Lines;
        _line = 0;
        _lineAt = 0f;
        if (_hardware != null && _outcome != ChipOutcome.NoChip)
            _hardware.Readings.ReadChip(_document);
        if (_outcome == ChipOutcome.Mismatch)
        {
            CueSounds.Play(SoundCues.ChipFault, sound);
            FeelDirector.Hit(0.3f);
        }
        else
        {
            CueSounds.Play(SoundCues.ChipOk, sound, _outcome == ChipOutcome.NoChip ? 0.8f : 1f);
            _okFlash = _outcome == ChipOutcome.Match ? 0.6f : 0f;
        }
        Lamps();
    }

    /// <summary>The paper comes back out of the slot side of the pad.</summary>
    private void Eject()
    {
        if (_paper == null)
            return;
        CueSounds.Play(SoundCues.ChipEject, sound);
        _flap = 1f;
    }

    /// <summary>The cycle is over: the desk takes the paper back.</summary>
    private void Finish()
    {
        Action done = _done;
        _done = null;
        _paper = null;
        Lamps();
        done?.Invoke();
    }

    /// <summary>The readout clicked: in inspect mode its shown differing line is picked against its box; otherwise the next line shows.</summary>
    private void ReadoutClicked()
    {
        if (!_hasResult || _lines.Count == 0)
        {
            Sounds.Play(SoundCues.UiPress);
            return;
        }
        ChipLine line = _lines[Mathf.Clamp(_line - 1, 0, _lines.Count - 1)];
        if (_hardware != null && _hardware.Inspecting)
        {
            _hardware.Pick(_document, line.Field, line.Chip, ScanLayer.Chip);
            return;
        }
        _line = (_line + 1) % (_lines.Count + 1);
        _lineAt = 0f;
        CueSounds.Play(SoundCues.ChipRead, sound);
        Show();
    }

    /// <summary>The readout's text: READY; READING with its dots; the result (CHIP OK, NO CHIP, MISMATCH with its shown line).</summary>
    private void Show()
    {
        string text;
        if (_cycle != null && _cycle.Busy && !_hasResult)
            text = UiText.Get("machine.chip.reading") + new string('.', 1 + Mathf.FloorToInt(_cycle.Progress * 3f) % 4);
        else if (!_hasResult && _document < 0)
            text = UiText.Get("machine.chip.ready");
        else if (_outcome == ChipOutcome.NoChip)
            text = UiText.Get("machine.chip.none");
        else if (_outcome == ChipOutcome.Match || _lines.Count == 0)
            text = UiText.Get("machine.chip.ok");
        else if (_line == 0)
            text = UiText.Get("machine.chip.mismatch");
        else
        {
            ChipLine line = _lines[Mathf.Clamp(_line - 1, 0, _lines.Count - 1)];
            text = UiText.Format("machine.chip.line", UiText.Category(line.Category).ToUpperInvariant(), line.Chip);
        }
        if (text == _shown)
            return;
        _shown = text;
        if (readout != null)
            readout.text = text;
    }

    /// <summary>The lamps: POWER always lit; READ while reading; FAULT after a mismatch, blinking on a refusal; POWER flashes on an OK.</summary>
    private void Lamps()
    {
        bool reading = _cycle != null && _cycle.Busy && !_hasResult;
        Lamp(ledPower, _okFlash > 0f ? lampOn * 1.8f : lampOn);
        Lamp(ledRead, reading ? lampOn : lampOff);
        // a refusal blinks the FAULT lamp twice (its "no"), over whatever it shows
        bool blink = _refused > 0f && Mathf.Repeat(_refused / Mathf.Max(0.05f, refuseBlinkSeconds) * 2f, 1f) > 0.45f;
        Lamp(ledFault, blink || (_refused <= 0f && _hasResult && _outcome == ChipOutcome.Mismatch) ? lampOn : lampOff);
    }

    /// <inheritdoc />
    public DeskMachine Machine => DeskMachine.ChipReader;

    /// <inheritdoc />
    public bool OnDesk => _hardware != null && _hardware.ReaderToday && isActiveAndEnabled;

    /// <inheritdoc />
    public MachineBox Casing => MachineCasing.Measure(transform, GetComponentsInChildren<Renderer>(false));

    /// <inheritdoc />
    public Vector3 DockPoint(MachineDock dock)
    {
        if (dock == MachineDock.Slot && cardSlot != null)
            return cardSlot.position;
        return padCentre != null ? padCentre.position : transform.position + Vector3.up * 0.07f;
    }

    /// <inheritdoc />
    public Quaternion DockFacing(MachineDock dock) => Quaternion.LookRotation(Slot.way, Vector3.up);

    /// <inheritdoc />
    public (Vector3 way, float depth) Slot
    {
        get
        {
            Transform mouth = cardSlot != null ? cardSlot : transform;
            Vector3 way = Vector3.ProjectOnPlane(mouth.forward, Vector3.up);
            way = way.sqrMagnitude > 1e-8f ? way.normalized : Vector3.forward;
            return (way, cardSlot != null ? slotDepth * Mathf.Abs(transform.lossyScale.z) : 0f);
        }
    }

    /// <inheritdoc />
    public void Refuse()
    {
        _refused = refuseBlinkSeconds;
        CueSounds.Play(SoundCues.ChipFault, sound);
        Lamps();
    }

    private void Lamp(Renderer r, float glow)
    {
        if (r == null || r.sharedMaterial == null)
            return;
        _block ??= new MaterialPropertyBlock();
        r.GetPropertyBlock(_block);
        Color c = r.sharedMaterial.HasProperty("_BaseColor") ? r.sharedMaterial.GetColor("_BaseColor") : Color.white;
        _block.SetColor("_BaseColor", new Color(c.r * glow, c.g * glow, c.b * glow, c.a));
        r.SetPropertyBlock(_block);
    }
}
