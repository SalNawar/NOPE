using System;
using System.Collections.Generic;

/// <summary>
/// A back/forward stack (P spec AP9, IN1): the browser's pages today, the
/// Investigation app's panes later. Go adds a new current entry and clears
/// everything after it; Back and Forward move through the entries. At most
/// the cap's number of entries are kept (the oldest drops first); going to
/// the current entry again changes nothing.
/// </summary>
public sealed class NavHistory<T>
{
    private readonly List<T> _entries = new List<T>();
    private readonly int _cap;
    private int _index = -1;

    /// <summary>A history keeping at most <paramref name="cap"/> entries (below 1 counts as 1).</summary>
    public NavHistory(int cap)
    {
        _cap = Math.Max(1, cap);
    }

    /// <summary>True once something was visited.</summary>
    public bool HasCurrent => _index >= 0;

    /// <summary>The entry being shown (default before the first Go).</summary>
    public T Current => _index >= 0 ? _entries[_index] : default;

    /// <summary>True when Back has somewhere to go.</summary>
    public bool CanBack => _index > 0;

    /// <summary>True when Forward has somewhere to go.</summary>
    public bool CanForward => _index >= 0 && _index < _entries.Count - 1;

    /// <summary>Visits <paramref name="at"/>: it becomes the current entry and the forward entries are dropped (nothing changes when it is already current).</summary>
    public void Go(T at)
    {
        if (_index >= 0 && EqualityComparer<T>.Default.Equals(_entries[_index], at))
            return;

        if (_index < _entries.Count - 1)
            _entries.RemoveRange(_index + 1, _entries.Count - _index - 1);
        _entries.Add(at);
        _index = _entries.Count - 1;

        while (_entries.Count > _cap)
        {
            _entries.RemoveAt(0);
            _index--;
        }
    }

    /// <summary>Steps back: true with the entry before the current one, false (and default) when there is none.</summary>
    public bool Back(out T at)
    {
        if (!CanBack)
        {
            at = default;
            return false;
        }
        at = _entries[--_index];
        return true;
    }

    /// <summary>Steps forward: true with the entry after the current one, false (and default) when there is none.</summary>
    public bool Forward(out T at)
    {
        if (!CanForward)
        {
            at = default;
            return false;
        }
        at = _entries[++_index];
        return true;
    }
}
