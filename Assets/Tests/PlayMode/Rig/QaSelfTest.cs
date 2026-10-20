#if UNITY_EDITOR || TIMEDESK_QA
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// A full-screen UI target for the rig's self-test: records what the event
/// system delivered from the virtual mouse (hover, press, drag, release and
/// their screen positions), so the test measures the real pointer events, not
/// the rig's own numbers.
/// </summary>
public sealed class QaDragProbe : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    /// <summary>True once the pointer entered (a hover was delivered).</summary>
    public bool Hovered { get; private set; }

    /// <summary>Where the press was delivered (screen pixels).</summary>
    public Vector2? Down { get; private set; }

    /// <summary>Where the release was delivered (screen pixels).</summary>
    public Vector2? Up { get; private set; }

    /// <summary>Drag events delivered.</summary>
    public int Drags { get; private set; }

    /// <summary>Forgets everything recorded.</summary>
    public void Clear()
    {
        Hovered = false;
        Down = Up = null;
        Drags = 0;
    }

    /// <summary>Records the hover.</summary>
    public void OnPointerEnter(PointerEventData eventData) => Hovered = true;

    /// <summary>Records the press position.</summary>
    public void OnPointerDown(PointerEventData eventData) => Down = eventData.position;

    /// <summary>Counts a drag event.</summary>
    public void OnDrag(PointerEventData eventData) => Drags++;

    /// <summary>Records the release position.</summary>
    public void OnPointerUp(PointerEventData eventData) => Up = eventData.position;
}

/// <summary>
/// The rig's self-test (P0-B "harness self-tests"): on an overlay of its own,
/// every P-* path that ends on the screen is dragged with the virtual mouse,
/// and the press and release the event system delivered must land within
/// <see cref="TolerancePx"/> of the scripted start and end, with a hover
/// before the press and drag events between. P-OFF must deliver its release
/// too (the pointer left the window while held).
/// </summary>
public static class QaSelfTest
{
    /// <summary>The largest allowed distance between a scripted and a delivered pointer position, px.</summary>
    public const float TolerancePx = 1f;

    /// <summary>Runs the self-test with <paramref name="input"/>, recording one assertion per path through <paramref name="assert"/> (id, pass, measured, detail).</summary>
    public static IEnumerator Run(QaInput input, Action<string, bool, string, string> assert)
    {
        GameObject eventSystem = null;
        if (EventSystem.current == null)
        {
            eventSystem = new GameObject("QaSelfTestEvents", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        var canvasGo = new GameObject("QaSelfTestCanvas", typeof(Canvas), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        var targetGo = new GameObject("QaSelfTestTarget", typeof(RectTransform), typeof(Image), typeof(QaDragProbe));
        targetGo.transform.SetParent(canvasGo.transform, false);
        var rt = (RectTransform)targetGo.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        targetGo.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.12f, 1f);
        QaDragProbe probe = targetGo.GetComponent<QaDragProbe>();
        try
        {
            yield return input.MoveTo(new Vector2(4f, 4f), 3);
            var screen = new Vector2(Screen.width, Screen.height);
            var cases = new List<(QaPathKind kind, Vector2 from, Vector2 to)>
            {
                (QaPathKind.Plain, Frac(screen, 0.3f, 0.4f), Frac(screen, 0.6f, 0.55f)),
                (QaPathKind.Fast, Frac(screen, 0.1f, 0.5f), Frac(screen, 0.9f, 0.5f)),
                (QaPathKind.Slow, Frac(screen, 0.4f, 0.4f), Frac(screen, 0.45f, 0.45f)),
                (QaPathKind.Wobble, Frac(screen, 0.2f, 0.3f), Frac(screen, 0.7f, 0.6f)),
                (QaPathKind.Diag, Frac(screen, 0.3f, 0.3f), Frac(screen, 0.6f, 0.5f)),
                (QaPathKind.Flick, Frac(screen, 0.2f, 0.7f), Frac(screen, 0.7f, 0.4f)),
                (QaPathKind.Edge, Frac(screen, 0.5f, 0.5f), Frac(screen, 0.97f, 0.5f)),
                (QaPathKind.Onto, Frac(screen, 0.25f, 0.25f), Frac(screen, 0.75f, 0.75f)),
                (QaPathKind.Off, Frac(screen, 0.5f, 0.5f), Frac(screen, 0.5f, 0.98f)),
            };
            foreach ((QaPathKind kind, Vector2 from, Vector2 to) in cases)
            {
                probe.Clear();
                QaPath path = QaPaths.Drag(kind, from, to, screen);
                yield return input.MoveTo(path.Samples[0].Position, 4);
                yield return input.Play(path);
                yield return null;
                float downErr = probe.Down.HasValue ? (probe.Down.Value - path.Start).magnitude : float.PositiveInfinity;
                float upErr = probe.Up.HasValue ? (probe.Up.Value - path.End).magnitude : float.PositiveInfinity;
                bool pass = probe.Hovered && probe.Drags > 0 && downErr <= TolerancePx && (kind == QaPathKind.Off ? probe.Up.HasValue : upErr <= TolerancePx);
                assert($"selftest.{kind}", pass, $"press error {Px(downErr)} px, release error {Px(upErr)} px, drags {probe.Drags}, hover {probe.Hovered}",
                    $"{kind}: scripted {path.Start} -> {path.End}; delivered {probe.Down} -> {probe.Up} (tolerance {TolerancePx} px{(kind == QaPathKind.Off ? "; P-OFF needs only the release delivered" : string.Empty)})");
            }
        }
        finally
        {
            UnityEngine.Object.Destroy(canvasGo);
            if (eventSystem != null)
                UnityEngine.Object.Destroy(eventSystem);
        }
    }

    private static Vector2 Frac(Vector2 screen, float x, float y) => new Vector2(screen.x * x, screen.y * y);

    private static string Px(float v) => float.IsInfinity(v) ? "none" : v.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
}
#endif
