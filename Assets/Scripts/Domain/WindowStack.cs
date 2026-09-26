using System;
using System.Collections.Generic;

/// <summary>
/// The desktop's one window stack (the PC redesign WN1): every desktop
/// window's z-order, focus, minimised state and taskbar order, by window id.
/// Open shows and focuses a window (a minimised one restores); Focus raises
/// it; Minimise hides it and keeps its taskbar button, the next visible window
/// taking the focus; Close removes it; a taskbar click minimises the focused
/// window, else restores and focuses it; ClearFocus (a press on the empty
/// desktop) leaves nothing focused. Changed fires once per change and never
/// for a call that changes nothing. Pure; DesktopWindowManager applies it.
/// </summary>
public sealed class WindowStack
{
    private readonly List<string> _z = new List<string>();
    private readonly List<string> _taskbar = new List<string>();
    private readonly HashSet<string> _minimised = new HashSet<string>();

    /// <summary>The visible windows, bottom to top.</summary>
    public IReadOnlyList<string> ZOrder => _z;

    /// <summary>The open windows in open order, minimised ones included (the taskbar's buttons).</summary>
    public IReadOnlyList<string> TaskbarOrder => _taskbar;

    /// <summary>The focused window (always visible), or null.</summary>
    public string Focused { get; private set; }

    /// <summary>Raised once after every change.</summary>
    public event Action Changed;

    /// <summary>True while the window is open (visible or minimised).</summary>
    public bool IsOpen(string id) => id != null && _taskbar.Contains(id);

    /// <summary>True while the window is open and minimised.</summary>
    public bool IsMinimised(string id) => id != null && _minimised.Contains(id);

    /// <summary>Opens the window last on the taskbar (an open one keeps its place), restores it when minimised, raises and focuses it.</summary>
    public void Open(string id)
    {
        if (id == null)
            throw new ArgumentNullException(nameof(id));

        bool changed = false;
        if (!_taskbar.Contains(id))
        {
            _taskbar.Add(id);
            changed = true;
        }
        if (Raise(id) || changed)
            Changed?.Invoke();
    }

    /// <summary>Raises and focuses an open window (restoring it when minimised); an unknown one is ignored.</summary>
    public void Focus(string id)
    {
        if (IsOpen(id) && Raise(id))
            Changed?.Invoke();
    }

    /// <summary>Hides an open, visible window, keeping its taskbar button; when it had the focus, the next visible window (the top) takes it.</summary>
    public void Minimise(string id)
    {
        if (!IsOpen(id) || !_minimised.Add(id))
            return;

        _z.Remove(id);
        if (Focused == id)
            Focused = Top();
        Changed?.Invoke();
    }

    /// <summary>Removes a window; when it had the focus, the next visible window (the top) takes it.</summary>
    public void Close(string id)
    {
        if (id == null || !_taskbar.Remove(id))
            return;

        _minimised.Remove(id);
        _z.Remove(id);
        if (Focused == id)
            Focused = Top();
        Changed?.Invoke();
    }

    /// <summary>A taskbar button's click: minimises the focused window, else restores, raises and focuses it.</summary>
    public void TaskbarClick(string id)
    {
        if (!IsOpen(id))
            return;

        if (Focused == id)
            Minimise(id);
        else
            Focus(id);
    }

    /// <summary>Leaves no window focused (a press on the empty desktop); the z-order stays.</summary>
    public void ClearFocus()
    {
        if (Focused == null)
            return;

        Focused = null;
        Changed?.Invoke();
    }

    /// <summary>Restores, raises and focuses an open window; true when anything changed.</summary>
    private bool Raise(string id)
    {
        bool changed = _minimised.Remove(id);
        if (_z.Count == 0 || _z[_z.Count - 1] != id)
        {
            _z.Remove(id);
            _z.Add(id);
            changed = true;
        }
        if (Focused != id)
        {
            Focused = id;
            changed = true;
        }
        return changed;
    }

    /// <summary>The top visible window, or null.</summary>
    private string Top() => _z.Count > 0 ? _z[_z.Count - 1] : null;
}
