using System;
using System.Collections.Generic;

/// <summary>
/// The Esc key's "let go first" steps (FLOW, D20: "if you press escape you
/// should auto open the pc setting menu"; FIX_PLAN_v2's FLOW proposal, so
/// Phase B tracks need no lease on FLOW's Esc handler). Esc lets go of one
/// thing the player is holding or has open, if anything, and otherwise opens
/// Settings (PauseMenu). Each step answers "did I let go of something?":
/// the desk's hold (cancelled first: <see cref="HoldOrder"/>), the PC's own
/// chain (a menu, a field), the office's own back-out on this press (a mode
/// left), then what a track registers (DIALOGUE an open ring, PORTALS a held
/// cable). Steps run lowest order first, ties in the order they were
/// registered; the first that lets go ends the press. Pure.
/// </summary>
public sealed class EscapeChain
{
    /// <summary>The desk's hold (HoldRegistry): cancelled before anything else (DOC-HANDLE's rule, v2).</summary>
    public const int HoldOrder = 0;

    /// <summary>The PC's own Escape chain (a context menu, the card, the search, a field: DesktopKeyboard).</summary>
    public const int DesktopOrder = 100;

    /// <summary>The office's own back-out on this press (OfficeControls left a mode: the wheel, the PC, the reading view, a drag).</summary>
    public const int OfficeOrder = 150;

    /// <summary>The order a Phase B track's step takes by default (after the hold and the PC).</summary>
    public const int TrackOrder = 200;

    /// <summary>The game's one chain (PauseMenu runs it on every Esc).</summary>
    public static EscapeChain Shared { get; } = new EscapeChain();

    /// <summary>The registered steps, in registration order.</summary>
    private readonly List<Step> _steps = new List<Step>();

    /// <summary>How many steps are registered.</summary>
    public int Count => _steps.Count;

    /// <summary>
    /// Registers <paramref name="letGo"/> as the step <paramref name="name"/>
    /// at <paramref name="order"/> (a step of that name is replaced): it lets
    /// go of something and returns true, or returns false when it holds nothing.
    /// </summary>
    /// <exception cref="ArgumentException">The step has no name.</exception>
    /// <exception cref="ArgumentNullException">The step has no body.</exception>
    public void Register(string name, int order, Func<bool> letGo)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("An Esc step needs a name.", nameof(name));
        if (letGo == null)
            throw new ArgumentNullException(nameof(letGo));
        Unregister(name);
        _steps.Add(new Step(name, order, letGo, _next++));
    }

    /// <summary>Removes the step <paramref name="name"/> (nothing when none is registered).</summary>
    public void Unregister(string name) => _steps.RemoveAll(s => s.Name == name);

    /// <summary>Removes every step (a new play session).</summary>
    public void Clear() => _steps.Clear();

    /// <summary>True when a step named <paramref name="name"/> is registered.</summary>
    public bool Has(string name) => _steps.Exists(s => s.Name == name);

    /// <summary>One Esc press: runs the steps in order until one lets go; returns its name, or null when none held anything (Settings opens).</summary>
    public string LetGo()
    {
        if (_steps.Count == 0)
            return null;
        var ordered = new List<Step>(_steps);
        ordered.Sort((a, b) => a.Order != b.Order ? a.Order.CompareTo(b.Order) : a.Sequence.CompareTo(b.Sequence));
        foreach (Step step in ordered)
            if (step.LetGo())
                return step.Name;
        return null;
    }

    /// <summary>The registration count (ties keep their registration order).</summary>
    private int _next;

    /// <summary>One registered step.</summary>
    private readonly struct Step
    {
        public readonly string Name;
        public readonly int Order;
        public readonly Func<bool> LetGo;
        public readonly int Sequence;

        public Step(string name, int order, Func<bool> letGo, int sequence)
        {
            Name = name;
            Order = order;
            LetGo = letGo;
            Sequence = sequence;
        }
    }
}
