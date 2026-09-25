/// <summary>
/// One document's written reveal (piece 10 X18), shared by its desk paper and
/// its scanned copy so a document reveals once, whichever is seen first: NaN
/// before the first sighting (piece 9's "not yet revealed"), then the seconds
/// since it (the flip's clock); +infinity once finished (a row click: English
/// at once, never replayed). DocumentReveal.Begin decides when it starts.
/// </summary>
public sealed class RevealClock
{
    private float _startedAt = float.NaN;
    private bool _finished;

    /// <summary>True once started.</summary>
    public bool Started => !float.IsNaN(_startedAt);

    /// <summary>Starts the reveal at <paramref name="now"/>; a later call does nothing (the first sighting wins).</summary>
    public void Start(float now)
    {
        if (!Started)
            _startedAt = now;
    }

    /// <summary>Finishes a started reveal (its elapsed time is +infinity from now on); before a start it does nothing.</summary>
    public void Finish()
    {
        if (Started)
            _finished = true;
    }

    /// <summary>Seconds since the start at <paramref name="now"/>: NaN before a start, +infinity once finished.</summary>
    public float Elapsed(float now)
    {
        if (!Started)
            return float.NaN;
        return _finished ? float.PositiveInfinity : now - _startedAt;
    }
}
