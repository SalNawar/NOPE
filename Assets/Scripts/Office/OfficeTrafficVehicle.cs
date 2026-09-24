using UnityEngine;

/// <summary>Moves one exterior vehicle along its authored lane, independently of the skyline.</summary>
public sealed class OfficeTrafficVehicle : MonoBehaviour
{
    [SerializeField] private Vector3 laneStart = new Vector3(-65f, 6f, 36f);
    [SerializeField] private Vector3 laneEnd = new Vector3(65f, 6f, 36f);
    [SerializeField, Min(1f)] private float traversalSeconds = 30f;
    [SerializeField, Range(0f, 1f)] private float initialPhase;

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

    private void ApplyPosition()
    {
        float phase = Mathf.Repeat(initialPhase + elapsed / Mathf.Max(1f, traversalSeconds), 1f);
        transform.localPosition = Vector3.Lerp(laneStart, laneEnd, phase);
    }
}
