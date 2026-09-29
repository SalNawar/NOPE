using System;
using System.Collections.Generic;

/// <summary>
/// The balance simulation's bribe-taking clerk (Saleh's Q6 of the Home
/// upgrades spec, 2026-09-29: "balance so you can buy one or two if you take
/// bribes"): at the desk, every dialog offered that has a choice paying the
/// clerk (its effect adds money, the bribe of days 7-15's Rook) is finished
/// on that choice, as a player taking the money would. The careful clerk
/// finishes none. Pure, so the picks are tested headless.
/// </summary>
public static class BribePolicy
{
    /// <summary>
    /// The choices a bribe-taking clerk makes among <paramref name="dialogs"/>:
    /// for each dialog, its first choice (node by node, in authored order)
    /// whose effect <paramref name="payOf"/> says pays more than 0, as the
    /// dialog's id and the effect's asset name; dialogs with no paying choice
    /// are left alone.
    /// </summary>
    public static List<(string dialogId, string effect)> Take(IReadOnlyList<AuthoredDialog> dialogs, Func<string, float> payOf)
    {
        var taken = new List<(string, string)>();
        if (dialogs == null || payOf == null)
            return taken;

        foreach (AuthoredDialog d in dialogs)
        {
            string effect = PayingEffect(d, payOf);
            if (effect != null)
                taken.Add((d.id, effect));
        }
        return taken;
    }

    /// <summary>The dialog's first choice effect that pays, or null.</summary>
    private static string PayingEffect(AuthoredDialog dialog, Func<string, float> payOf)
    {
        if (dialog == null || string.IsNullOrEmpty(dialog.id) || dialog.nodes == null)
            return null;
        foreach (ScriptNode node in dialog.nodes)
            if (node?.choices != null)
                foreach (ScriptChoice choice in node.choices)
                    if (choice != null && !string.IsNullOrEmpty(choice.effect) && payOf(choice.effect) > 0f)
                        return choice.effect;
        return null;
    }
}
