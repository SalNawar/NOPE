using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A view component on its content root in a pane of the Investigation app
/// (IAppView as a MonoBehaviour, so the builder can wire it and the pane can
/// hold it). A view without items shows or hides its root and reveals only
/// its tab; a view with items overrides the chips, the choice and Reveal.
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
    public event Action Moved;

    /// <inheritdoc />
    public virtual LinkTarget Spot => LinkTarget.ToTab(Tab, Selected);

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

    /// <inheritdoc />
    public virtual void Reveal(LinkTarget target)
    {
        if (target.Item >= 0)
            Select(target.Item);
    }

    /// <summary>Tells the pane the chips changed.</summary>
    protected void RaiseChipsChanged() => ChipsChanged?.Invoke();

    /// <summary>Tells the pane the player moved the view (its Spot changed).</summary>
    protected void RaiseMoved() => Moved?.Invoke();
}
