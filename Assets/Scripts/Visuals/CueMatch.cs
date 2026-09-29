using System;
using System.Collections.Generic;

/// <summary>
/// A cue-reactive prop's pick (TimelineReactiveSprite): of its cue-id
/// mappings, the first whose cue is among the active cues wins, in mapping
/// order; a blank cue id never matches. Pure, so the rule is tested headless.
/// </summary>
public static class CueMatch
{
    /// <summary>
    /// The index of the first of <paramref name="mappingCount"/> mappings whose
    /// cue id (<paramref name="cueIdAt"/>) is among <paramref name="activeCues"/>;
    /// -1 when none is.
    /// </summary>
    public static int First(int mappingCount, Func<int, string> cueIdAt, IReadOnlyList<string> activeCues)
    {
        if (cueIdAt == null || activeCues == null)
            return -1;

        for (int m = 0; m < mappingCount; m++)
        {
            string cueId = cueIdAt(m);
            if (string.IsNullOrEmpty(cueId))
                continue;
            for (int i = 0; i < activeCues.Count; i++)
                if (activeCues[i] == cueId)
                    return m;
        }
        return -1;
    }
}
