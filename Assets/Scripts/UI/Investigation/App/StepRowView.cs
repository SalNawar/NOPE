using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One row of the Investigation app's steps checklist (StepsPanel clones the
/// builder's inactive row per step): a tick box the player can tick or untick
/// by hand, its tick, and the step's label, which jumps to where the step is
/// done. The references live on the row, so a clone keeps its own.
/// </summary>
public sealed class StepRowView : MonoBehaviour
{
    /// <summary>The tick box: a click ticks or unticks the step by hand (it holds for the case).</summary>
    [SerializeField] private Button box;

    /// <summary>The tick, shown while the step is done.</summary>
    [SerializeField] private GameObject check;

    /// <summary>The label: a click jumps to where the step is done (or shows its hint).</summary>
    [SerializeField] private Button label;

    /// <summary>The label's text: the step's name and, with parts, its progress.</summary>
    [SerializeField] private TMP_Text text;

    /// <summary>The tick box.</summary>
    public Button Box => box;

    /// <summary>The label's button.</summary>
    public Button Label => label;

    /// <summary>Shows the step: its tick and its text.</summary>
    public void Show(bool done, string line)
    {
        if (check != null && check.activeSelf != done)
            check.SetActive(done);
        if (text != null)
            text.text = line;
    }
}
