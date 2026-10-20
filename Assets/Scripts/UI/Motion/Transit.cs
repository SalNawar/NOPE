using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The screen transitions (P0-A seam; owner FLOW): a fade, a slide and a wait,
/// each calling its done callback when it ends. The P0 stub cuts instantly
/// (the end state at once, then done), as every screen change does today.
/// </summary>
public static class Transit
{
    /// <summary>Fades <paramref name="group"/> to <paramref name="alpha"/> over <paramref name="seconds"/>, then <paramref name="done"/>. The P0 stub: at once.</summary>
    public static void Fade(CanvasGroup group, float alpha, float seconds, Action done = null)
    {
        if (group != null)
            group.alpha = alpha;
        done?.Invoke();
    }

    /// <summary>Slides <paramref name="rect"/> to <paramref name="to"/> (its anchored position) over <paramref name="seconds"/>, then <paramref name="done"/>. The P0 stub: at once.</summary>
    public static void Slide(RectTransform rect, Vector2 to, float seconds, Action done = null)
    {
        if (rect != null)
            rect.anchoredPosition = to;
        done?.Invoke();
    }

    /// <summary>Waits <paramref name="seconds"/>, then <paramref name="done"/>. The P0 stub: at once.</summary>
    public static void Wait(float seconds, Action done) => done?.Invoke();
}

/// <summary>
/// The scene change with its transition (P0-A seam; owner FLOW; RUN-SAFE
/// consumes it): <see cref="Load"/>. The P0 stub is today's synchronous
/// SceneManager.LoadScene.
/// </summary>
public static class SceneTransit
{
    /// <summary>Loads <paramref name="scene"/> (by name). The P0 stub: SceneManager.LoadScene, as RunManager does today.</summary>
    public static void Load(string scene) => SceneManager.LoadScene(scene);
}
