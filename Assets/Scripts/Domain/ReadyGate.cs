using System;

/// <summary>
/// One-shot gate the player releases by tapping the READY sign. GameManager
/// arms it when a case slot starts and shows the case when it is released, so
/// each visitor waits for the player to signal readiness.
/// </summary>
public sealed class ReadyGate
{
    /// <summary>True between Arm() and Release().</summary>
    public bool IsArmed { get; private set; }

    /// <summary>Raised exactly once per Arm(), when Release() is first called.</summary>
    public event Action Released;

    /// <summary>Arms the gate (waiting for the player).</summary>
    public void Arm() => IsArmed = true;

    /// <summary>Releases the gate if armed, firing <see cref="Released"/> once.</summary>
    public void Release()
    {
        if (!IsArmed)
            return;

        IsArmed = false;
        Released?.Invoke();
    }
}
