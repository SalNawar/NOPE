using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A view component on its content root in the Investigation app's pane
/// (IAppView as a MonoBehaviour, so the builder can wire it and the pane can
/// hold it). A view without items shows or hides its root; a view with items
/// overrides the chips and the choice.
/// </summary>
public abstract class AppView : MonoBehaviour, IAppView
{
    private static readonly AppChip[] NoChips = Array.Empty<AppChip>();

    /// <inheritdoc />
    public abstract AppTab Tab { get; }

    /// <inheritdoc />
    public virtual IReadOnlyList<AppChip> Chips => NoChips;

    /// <inheritdoc />
    public virtual int Selected => -1;

    /// <inheritdoc />
    public event Action ChipsChanged;

    /// <inheritdoc />
    public virtual void SetVisible(bool visible)
    {
        if (gameObject.activeSelf != visible)
            gameObject.SetActive(visible);
    }

    /// <inheritdoc />
    public virtual void Select(int index)
    {
    }

    /// <summary>Tells the pane the chips changed.</summary>
    protected void RaiseChipsChanged() => ChipsChanged?.Invoke();
}
