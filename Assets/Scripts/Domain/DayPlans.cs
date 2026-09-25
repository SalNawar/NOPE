using System.Collections.Generic;

/// <summary>Which authored day plan a day uses, pure so the fallback is tested headless.</summary>
public static class DayPlans
{
    /// <summary>
    /// Which day plan a day uses: its own, else the latest earlier one. The
    /// index of the first entry equal to <paramref name="day"/>; otherwise of
    /// the largest entry below it (the first such entry on a repeat);
    /// otherwise -1 (also for a null or empty list).
    /// </summary>
    public static int Pick(IReadOnlyList<int> dayNumbers, int day)
    {
        if (dayNumbers == null)
            return -1;

        int best = -1;
        for (int i = 0; i < dayNumbers.Count; i++)
        {
            if (dayNumbers[i] == day)
                return i;
            if (dayNumbers[i] < day && (best < 0 || dayNumbers[i] > dayNumbers[best]))
                best = i;
        }

        return best;
    }
}
