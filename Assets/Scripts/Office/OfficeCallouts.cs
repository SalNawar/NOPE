using System;
using UnityEngine;

/// <summary>
/// The office's callouts over the hall (the traveller's speech bubble and the
/// departure board's tooltip) belong to the office view: they fade out
/// (DeskConfigSO.cityDeskControlsFadeSeconds; a cut under Reduced Motion) and
/// take no clicks while the reading view is on (Saleh 2026-10-18: "dialogue
/// bubles stay visible when you click on the table") or the view looks at the
/// city (leftover 2026-10-08: the bubble drew over the city), and fade back in
/// as the office view returns, a line still being said showing again where it
/// was. The bubble's line is never lost: the PC's transcript keeps every line
/// said. Whether each callout is up stays its owner's (the wheel, the board).
/// </summary>
public sealed class OfficeCallouts : MonoBehaviour
{
    /// <summary>The desk tuning (the fade's seconds).</summary>
    [SerializeField] private DeskConfigSO config;

    /// <summary>The reading view (optional).</summary>
    [SerializeField] private DeskView deskView;

    /// <summary>The city view (optional).</summary>
    [SerializeField] private CityView cityView;

    /// <summary>The callouts' CanvasGroups (the speech bubble's host, the board tooltip's host).</summary>
    [SerializeField] private CanvasGroup[] callouts = Array.Empty<CanvasGroup>();

    private float _shown = 1f;

    /// <summary>True while the office view shows (neither the reading view nor the city).</summary>
    private bool OfficeShows => (deskView == null || !deskView.IsOn) && (cityView == null || !cityView.IsOn);

    private void Update()
    {
        bool shows = OfficeShows;
        float seconds = MotionPreference.Reduced || config == null ? 0f : config.cityDeskControlsFadeSeconds;
        _shown = CanvasGroupFade.Step(callouts, _shown, shows, seconds, Time.unscaledDeltaTime);
    }
}

/// <summary>Fades a set of CanvasGroups in or out together (the city view's desk controls, the office's callouts): they take clicks only while shown.</summary>
public static class CanvasGroupFade
{
    /// <summary>Moves <paramref name="alpha"/> toward 1 (<paramref name="shown"/>) or 0 over <paramref name="seconds"/> (0: a cut) by <paramref name="deltaTime"/>, applies it to every group (their raycasts and interactivity follow <paramref name="shown"/>), and returns it; nothing is written once it has arrived.</summary>
    public static float Step(CanvasGroup[] groups, float alpha, bool shown, float seconds, float deltaTime)
    {
        float target = shown ? 1f : 0f;
        if (alpha == target)
            return alpha;
        alpha = seconds > 0f ? Mathf.MoveTowards(alpha, target, deltaTime / seconds) : target;
        if (groups != null)
            foreach (CanvasGroup group in groups)
                if (group != null)
                {
                    group.alpha = alpha;
                    group.blocksRaycasts = group.interactable = shown;
                }
        return alpha;
    }
}
