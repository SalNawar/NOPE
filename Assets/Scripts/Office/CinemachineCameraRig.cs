using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Real <see cref="ICameraRig"/> backed by two Cinemachine cameras. Raises the
/// active camera's priority so the CinemachineBrain blends the push-in.
/// </summary>
public sealed class CinemachineCameraRig : MonoBehaviour, ICameraRig
{
    /// <summary>Wide booth camera.</summary>
    [SerializeField] private CinemachineCamera officeCam;

    /// <summary>Close-up monitor camera.</summary>
    [SerializeField] private CinemachineCamera monitorCam;

    private const int Active = 20;
    private const int Idle = 10;

    /// <summary>Raises the office camera above the monitor camera.</summary>
    public void ShowOffice()
    {
        if (officeCam != null) officeCam.Priority = Active;
        if (monitorCam != null) monitorCam.Priority = Idle;
    }

    /// <summary>Raises the monitor camera above the office camera.</summary>
    public void ShowMonitor()
    {
        if (officeCam != null) officeCam.Priority = Idle;
        if (monitorCam != null) monitorCam.Priority = Active;
    }
}
