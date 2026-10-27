using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The booth's roll shutter over the glass (FLOW; the game feel bible's G3
/// and G4): shut at the day's start, it rolls up as the shift opens (the
/// first AVAILABLE press: 1.2 s, OutCubic, booth_shutter_open) and rolls down
/// once the last traveller's payoff has played, before the report (1.2 s,
/// InCubic, ending on booth_shutter_close's clang); ShutterRoll times it,
/// MotionTuningSO.shutterSeconds tunes it. The shutter is the office's object
/// named <see cref="ObjectName"/> (the art office's or the gameplay layer's),
/// placed shut; it rolls along its parent's up by its own height. An office
/// without one (the desk set's shutter is an art request: docs/pending/FLOW.md)
/// rolls nothing and sounds nothing, and the report follows the payoff at
/// once. GameManager attaches it and drives it.
/// </summary>
public sealed class BoothShutter : MonoBehaviour
{
    /// <summary>The shutter object's name in the office's scenes.</summary>
    public const string ObjectName = "BoothShutter";

    /// <summary>The shutter (null: the office has none).</summary>
    private Transform _curtain;

    /// <summary>Its shut pose (local) and its travel up to open (local, its height along its parent's up).</summary>
    private Vector3 _shut, _travel;

    /// <summary>How open it is (0 shut, 1 open).</summary>
    private float _open;

    /// <summary>The roll under way, if any.</summary>
    private Coroutine _roll;

    /// <summary>Adds the shutter's driver to <paramref name="game"/> (once) and finds the office's shutter.</summary>
    public static BoothShutter Attach(GameManager game)
    {
        if (game == null)
            return null;
        if (!game.TryGetComponent(out BoothShutter shutter))
            shutter = game.gameObject.AddComponent<BoothShutter>();
        shutter.Find(game.gameObject.scene);
        return shutter;
    }

    /// <summary>The shift opens: the shutter rolls up (nothing without one, or when it is open already).</summary>
    public void Open()
    {
        if (_curtain == null || _open >= 1f)
            return;
        Sounds.Play(ShutterRoll.OpenCue);
        Roll(true, null);
    }

    /// <summary>The day is over (after the last payoff): the shutter rolls down and clangs shut, then <paramref name="done"/> (at once without a shutter, or when it is shut already).</summary>
    public void Close(Action done)
    {
        if (_curtain == null || _open <= 0f)
        {
            done?.Invoke();
            return;
        }
        Roll(false, () =>
        {
            Sounds.Play(ShutterRoll.CloseCue);
            done?.Invoke();
        });
    }

    /// <summary>Rolls toward open or shut over the tuned length (real time: hit-stop never stretches it), then <paramref name="done"/>; a new roll replaces the running one.</summary>
    private void Roll(bool opening, Action done)
    {
        if (_roll != null)
            StopCoroutine(_roll);
        _roll = StartCoroutine(Rolling(opening, done));
    }

    private IEnumerator Rolling(bool opening, Action done)
    {
        float seconds = UiMotion.Tuning != null ? UiMotion.Tuning.shutterSeconds : ShutterRoll.DefaultSeconds;
        float from = _open, elapsed = 0f;
        while (!ShutterRoll.Done(elapsed, seconds))
        {
            yield return null;
            if (_curtain == null)
                yield break;
            elapsed += UiMotion.Delta(Time.unscaledDeltaTime);
            Place(ShutterRoll.At(from, opening, elapsed, seconds));
        }
        Place(opening ? 1f : 0f);
        _roll = null;
        done?.Invoke();
    }

    /// <summary>Poses the shutter <paramref name="open"/> of the way up.</summary>
    private void Place(float open)
    {
        _open = open;
        if (_curtain != null)
            _curtain.localPosition = _shut + _travel * open;
    }

    /// <summary>Finds the shutter in <paramref name="gameplay"/> or the art office (its shut pose and its height), or none.</summary>
    private void Find(Scene gameplay)
    {
        _curtain = FindIn(gameplay) ?? FindIn(OfficeScenes.ArtScene);
        _open = 0f;
        if (_curtain == null)
            return;
        _shut = _curtain.localPosition;
        float height = 0f;
        foreach (Renderer r in _curtain.GetComponentsInChildren<Renderer>())
            height = Mathf.Max(height, r.bounds.size.y);
        _travel = _curtain.parent != null ? _curtain.parent.InverseTransformVector(Vector3.up * height) : Vector3.up * height;
    }

    /// <summary>The active object named <see cref="ObjectName"/> in <paramref name="scene"/>, or null.</summary>
    private static Transform FindIn(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>())
                if (t.name == ObjectName)
                    return t;
        return null;
    }
}
