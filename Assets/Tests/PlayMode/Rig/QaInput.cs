#if UNITY_EDITOR || TIMEDESK_QA
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>
/// The QA rig's virtual mouse and keyboard (BUG-53: "a virtual mouse that
/// still differs from real input" is the risk, so input goes through Input
/// System devices with QueueStateEvent, never by calling handlers). The game
/// reads them as it reads a real mouse: the UI input module raycasts, hovers,
/// presses and drags with them, with the real grab offset, because the
/// pointer is pressed where the object is and moved from there. While it is
/// on, every other pointer and keyboard is disabled so a hand on the real
/// mouse cannot disturb a run, and they come back on <see cref="Dispose"/>.
/// One per run (<see cref="Current"/>).
/// </summary>
public sealed class QaInput : IDisposable
{
    /// <summary>The live virtual input, or null outside a QA run.</summary>
    public static QaInput Current { get; private set; }

    /// <summary>Raised on the frame the game first sees a press: the channel ("mouse" or "key") and the button or key.</summary>
    public event Action<string, string> Pressed;

    /// <summary>The virtual mouse.</summary>
    public Mouse Mouse { get; }

    /// <summary>The virtual keyboard.</summary>
    public Keyboard Keyboard { get; }

    /// <summary>Where the virtual pointer is (screen pixels, origin bottom-left).</summary>
    public Vector2 Position { get; private set; }

    /// <summary>True while the virtual left button is held.</summary>
    public bool Down { get; private set; }

    private readonly List<InputDevice> _disabled = new List<InputDevice>();
    private readonly InputSettings.BackgroundBehavior _background;
    private Key _held = Key.None;
    private bool _disposed;

    /// <summary>Adds the virtual devices, makes them current and disables every other pointer and keyboard.</summary>
    public QaInput()
    {
        Current?.Dispose();
        foreach (InputDevice device in InputSystem.devices)
            if ((device is Pointer || device is Keyboard) && device.enabled)
                _disabled.Add(device);
        foreach (InputDevice device in _disabled)
            InputSystem.DisableDevice(device);
        Mouse = InputSystem.AddDevice<Mouse>("QaMouse");
        Keyboard = InputSystem.AddDevice<Keyboard>("QaKeyboard");
        Mouse.MakeCurrent();
        Keyboard.MakeCurrent();
        _background = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        Current = this;
        Set(new Vector2(Screen.width * 0.5f, 20f), false);
    }

    /// <summary>Puts the pointer at <paramref name="position"/> with the left button <paramref name="down"/> (seen by the game next frame).</summary>
    public void Set(Vector2 position, bool down)
    {
        var state = new MouseState { position = position, delta = position - Position };
        if (down)
            state = state.WithButton(MouseButton.Left);
        InputSystem.QueueStateEvent(Mouse, state);
        Position = position;
        Down = down;
    }

    /// <summary>Plays <paramref name="path"/>, one sample per frame; <paramref name="onSample"/> gets each sample's index before its frame.</summary>
    public IEnumerator Play(QaPath path, Action<int> onSample = null)
    {
        for (int i = 0; i < path.Samples.Count; i++)
        {
            onSample?.Invoke(i);
            Set(path.Samples[i].Position, path.Samples[i].Down);
            yield return null;
        }
    }

    /// <summary>Moves the pointer (button as it is) to <paramref name="to"/> over <paramref name="frames"/> frames, eased.</summary>
    public IEnumerator MoveTo(Vector2 to, int frames)
    {
        foreach (Vector2 p in QaPaths.Move(Position, to, frames))
        {
            Set(p, Down);
            yield return null;
        }
    }

    /// <summary>Presses <paramref name="key"/>, holds it <paramref name="holdFrames"/> frames and releases it.</summary>
    public IEnumerator Tap(Key key, int holdFrames = 2)
    {
        _held = key;
        InputSystem.QueueStateEvent(Keyboard, new KeyboardState(key));
        for (int i = 0; i < Math.Max(1, holdFrames); i++)
            yield return null;
        _held = Key.None;
        InputSystem.QueueStateEvent(Keyboard, new KeyboardState());
        yield return null;
    }

    /// <summary>Called once per frame early (QaRunner): raises <see cref="Pressed"/> on the frame the game first sees a press.</summary>
    public void Poll()
    {
        if (Mouse.leftButton.wasPressedThisFrame)
            Pressed?.Invoke("mouse", "left");
        if (_held != Key.None && Keyboard[_held].wasPressedThisFrame)
            Pressed?.Invoke("key", _held.ToString());
    }

    /// <summary>Removes the virtual devices, re-enables the devices it disabled and restores the background behaviour (once; later calls do nothing).</summary>
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        if (Mouse != null && Mouse.added)
            InputSystem.RemoveDevice(Mouse);
        if (Keyboard != null && Keyboard.added)
            InputSystem.RemoveDevice(Keyboard);
        foreach (InputDevice device in _disabled)
            if (device.added)
                InputSystem.EnableDevice(device);
        _disabled.Clear();
        InputSystem.settings.backgroundBehavior = _background;
        if (Current == this)
            Current = null;
    }

    /// <summary>
    /// Undoes what a run that never reached <see cref="Dispose"/> left behind
    /// (a crash, a domain reload, a run stopped from outside; the editor
    /// harness calls it on every restore): disposes <see cref="Current"/>;
    /// then, when a QaMouse or QaKeyboard device is still added (the input
    /// system keeps devices across the editor's domain reloads, so one left
    /// behind marks a run that never gave the input back), removes it and
    /// enables every disabled pointer and keyboard, which that run disabled.
    /// Returns how many devices it changed (0 when nothing was left behind).
    /// </summary>
    public static int ReleaseStray()
    {
        Current?.Dispose();
        var stray = new List<InputDevice>();
        foreach (InputDevice device in InputSystem.devices)
            if (device.name.StartsWith("QaMouse", StringComparison.Ordinal) || device.name.StartsWith("QaKeyboard", StringComparison.Ordinal))
                stray.Add(device);
        if (stray.Count == 0)
            return 0;
        foreach (InputDevice device in stray)
            InputSystem.RemoveDevice(device);
        int changed = stray.Count;
        var disabled = new List<InputDevice>();
        foreach (InputDevice device in InputSystem.devices)
            if ((device is Pointer || device is Keyboard) && !device.enabled)
                disabled.Add(device);
        foreach (InputDevice device in disabled)
            InputSystem.EnableDevice(device);
        return changed + disabled.Count;
    }
}
#endif
