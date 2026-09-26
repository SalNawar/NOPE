using UnityEngine;

/// <summary>
/// Art side (OfficeScene, on each HybridOffice/Traffic car): moves one exterior
/// vehicle along its authored lane, independently of the skyline. The car slides
/// in local space from <see cref="laneStart"/> to <see cref="laneEnd"/> over
/// <see cref="traversalSeconds"/> of scaled time, then wraps to the start.
/// Presentation only.
/// </summary>
public sealed class OfficeTrafficVehicle : MonoBehaviour
{
    /// <summary>Where the lane begins, in the parent's local space.</summary>
    [SerializeField] private Vector3 laneStart = new Vector3(-65f, 6f, 36f);

    /// <summary>Where the lane ends, in the parent's local space.</summary>
    [SerializeField] private Vector3 laneEnd = new Vector3(65f, 6f, 36f);

    /// <summary>Seconds for one pass from start to end (at least 1).</summary>
    [SerializeField, Min(1f)] private float traversalSeconds = 30f;

    /// <summary>Where along the lane the car is when it is enabled (0 start, 1 end): staggers the cars sharing a lane.</summary>
    [SerializeField, Range(0f, 1f)] private float initialPhase;

    /// <summary>Scaled seconds since the car was enabled.</summary>
    private float elapsed;

    private void OnEnable()
    {
        elapsed = 0f;
        ApplyPosition();
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        ApplyPosition();
    }

    /// <summary>Places the car at its phase along the lane (the Max guards a value set below 1 from code).</summary>
    private void ApplyPosition()
    {
        float phase = Mathf.Repeat(initialPhase + elapsed / Mathf.Max(1f, traversalSeconds), 1f);
        transform.localPosition = Vector3.Lerp(laneStart, laneEnd, phase);
    }
}
