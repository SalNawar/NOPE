/// <summary>
/// Abstraction over the camera so view-state logic is decoupled from
/// Cinemachine. Implemented for real by CinemachineCameraRig.
/// </summary>
public interface ICameraRig
{
    /// <summary>Frames the wide office/booth view.</summary>
    void ShowOffice();

    /// <summary>Frames the close-up monitor view.</summary>
    void ShowMonitor();

    /// <summary>True when the rig shows that view's camera and no blend is running.</summary>
    bool IsSettled(OfficeView view);
}
