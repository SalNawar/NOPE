using TMPro;
using UnityEngine;

/// <summary>
/// A slip printing out of its slot line by line (Saleh 2026-10-07: the
/// citation slip prints out line by line, then its stamp slams with a small
/// shake): Print shows <c>text</c>'s lines one after another
/// (MotionKnobs.printLinesPerSecond) with the printer's cue, and when the last
/// line is out plays the landing cue with a small camera bump (the stamp
/// slam's: CameraFeel). The text itself is whole from the start (only how
/// many lines show changes), so whatever reads it reads all of it. Reduced
/// Motion and Motion intensity 0 show it at once.
/// </summary>
[DisallowMultipleComponent]
public sealed class UiPrint : MonoBehaviour, IMotionTick
{
    private TMP_Text _text;
    private string _land;
    private float _shown;
    private int _lines;

    /// <summary>Prints <paramref name="text"/> line by line with <paramref name="printCue"/>, then <paramref name="landCue"/> and a small bump.</summary>
    public static void Print(TMP_Text text, string printCue, string landCue)
    {
        if (text == null)
            return;
        Sounds.Play(printCue);
        if (!text.TryGetComponent(out UiPrint print))
            print = text.gameObject.AddComponent<UiPrint>();
        print.Begin(text, landCue);
    }

    private void Begin(TMP_Text text, string landCue)
    {
        _text = text;
        _land = landCue;
        _text.ForceMeshUpdate();
        _lines = Mathf.Max(1, _text.textInfo.lineCount);
        if (UiMotion.Amount.Still)
        {
            Finish();
            return;
        }
        _shown = 0f;
        _text.maxVisibleLines = 1;
        UiMotion.Run(this);
    }

    /// <summary>Shows one more line as its time comes; false once all are out.</summary>
    public bool TickMotion(float dt)
    {
        if (this == null || _text == null)
            return false;
        _shown += dt * Mathf.Max(0.1f, UiMotion.Knobs.printLinesPerSecond);
        if (_shown + 1f < _lines)
        {
            _text.maxVisibleLines = 1 + (int)_shown;
            return true;
        }
        Finish();
        MotionKnobs knobs = UiMotion.Knobs;
        CameraFeel.Shake(knobs.shakeStamp, knobs.shakeStampSeconds, false);
        return false;
    }

    /// <summary>Hidden mid-print: whole again for the next time.</summary>
    private void OnDisable()
    {
        if (_text != null)
            _text.maxVisibleLines = 99999;
    }

    /// <summary>Every line shows; the landing cue.</summary>
    private void Finish()
    {
        _text.maxVisibleLines = 99999;
        Sounds.Play(_land);
    }
}
