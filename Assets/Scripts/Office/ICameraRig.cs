/// <summary>
/// Abstraction over the camera so view-state logic is testable without
/// Cinemachine. Implemented for real by CinemachineCameraRig.
/// </summary>
public interface ICameraRig
{
    /// <summary>Frames the wide office/booth view.</summary>
    void ShowOffice();

    /// <summary>Frames the close-up monitor view.</summary>
    void ShowMonitor();
}
