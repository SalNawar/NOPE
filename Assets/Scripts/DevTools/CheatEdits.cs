using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The cheat changes made to the run's world since it was last saved
/// (RUN-SAFE, BUG-28a): a skip or a jump from Home starts its night from the
/// end-of-shift save (so Home's arrival, charged in memory, is never charged
/// twice), and these changes are replayed on that save first, so a stability,
/// money, flag, item, unlock or leader cheat made at Home, or while the
/// end-of-shift report shows, is kept. DevCheats notes each change it makes
/// with the edit that makes it (Note); RunManager clears the list at every
/// save and every new or continued run (Clear) and replays it on the save
/// (ReplayOn, logged by RunManager). Pure over WorldState, so it is tested
/// headless.
/// </summary>
public sealed class CheatEdits
{
    /// <summary>The changes in the order they were made: each one's label (for the log) and its edit.</summary>
    private readonly List<(string label, Action<WorldState> edit)> _edits = new List<(string, Action<WorldState>)>();

    /// <summary>How many changes are kept.</summary>
    public int Count => _edits.Count;

    /// <summary>The changes' labels, in the order they were made.</summary>
    public IEnumerable<string> Labels => _edits.Select(e => e.label);

    /// <summary>Keeps <paramref name="edit"/> (a change a cheat just made to the world, as an edit of any world) under <paramref name="label"/>; nothing for a null edit.</summary>
    public void Note(string label, Action<WorldState> edit)
    {
        if (edit != null)
            _edits.Add((label ?? string.Empty, edit));
    }

    /// <summary>Forgets every change (the world was saved with them, or replaced).</summary>
    public void Clear() => _edits.Clear();

    /// <summary>Replays every change, in order, on <paramref name="world"/> (null: nothing) and returns it.</summary>
    public WorldState ReplayOn(WorldState world)
    {
        if (world == null)
            return world;
        foreach ((string _, Action<WorldState> edit) in _edits)
            edit(world);
        return world;
    }
}
