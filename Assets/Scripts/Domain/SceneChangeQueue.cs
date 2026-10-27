/// <summary>
/// The scene changes SceneTransit runs, one at a time (pure; owner FLOW;
/// TransitRunner drives the shutter and the loads). A request made while a
/// change runs is never dropped: it waits, a later one replacing it, because
/// the caller (RunManager's day steps and cheats) has already moved the run
/// for it, so the scene asked for last is the one that must end up loaded.
/// FLOW round 4: a second cheat while the first one's office loaded used to be
/// ignored, and the run's world and the loaded office then agreed only by luck.
/// </summary>
public sealed class SceneChangeQueue
{
    /// <summary>True from an accepted request until <see cref="Finish"/>: the whole change, the shutter's rise included.</summary>
    public bool Changing { get; private set; }

    /// <summary>The scene asked for during the change (the latest request), or null when none waits.</summary>
    public string Queued { get; private set; }

    /// <summary>Asks for <paramref name="scene"/>: true when its change starts now; false when a change runs, and the request waits instead (replacing an earlier waiting one).</summary>
    public bool Request(string scene)
    {
        if (Changing)
        {
            Queued = scene;
            return false;
        }
        Changing = true;
        return true;
    }

    /// <summary>Takes the waiting request into <paramref name="scene"/> (the change goes on with it); false, and <paramref name="scene"/> unchanged, when none waits.</summary>
    public bool TakeQueued(ref string scene)
    {
        if (Queued == null)
            return false;
        scene = Queued;
        Queued = null;
        return true;
    }

    /// <summary>The change is over (the shutter is off and <see cref="TakeQueued"/> found nothing waiting): the next request starts a change of its own.</summary>
    public void Finish() => Changing = false;
}
