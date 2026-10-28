using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// FOLD-01 (Saleh's playtest of 2026-10-28, point 4: "You move the folder and
/// it snaps somewhere else"): DeskController judges every rest in the desk's
/// own frame (DeskLayout.Frame, the office camera's level axes, the frame the
/// reading view is posed on), not along the office camera's line to the
/// scanner (SetScannerView). On the painted desk that line is 27 degrees off
/// the desk's axes: each footprint grew to the box round its turned shape,
/// and the reading view's corners left a band 10 cm deep, so every rest's
/// depth went to the band's middle. A rectangle on the desk's axes keeps its
/// own size in the rest's frame, whatever the scanner's line. Makes Unity
/// objects, so it runs in Unity's EditMode suite only (NeedsEditor).
/// </summary>
public class RestFrameTests
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    [Test, Category("NeedsEditor")]
    public void ARest_IsJudgedInTheDesksFrame_NotAlongTheOfficeCamerasLineToTheScanner()
    {
        var settings = new DeskLayoutSettings();
        var frame = new DeskFrame(new Vector3(0f, 1f, 0f), Vector3.forward);
        GameObject deskObject = null, surfaceObject = null;
        try
        {
            DeskLayout.Bind(frame, settings.mat.Rect, settings);
            surfaceObject = new GameObject("Surface");
            surfaceObject.transform.position = new Vector3(0.27f, 1f, 0f);
            DeskSurface surface = surfaceObject.AddComponent<DeskSurface>();
            deskObject = new GameObject("Desk");
            var desk = deskObject.AddComponent<DeskController>();
            typeof(DeskController).GetField("surface", Private).SetValue(desk, surface);
            // The office camera's line to the scanner on the painted desk's right rail (OfficeSceneBinder: SetScannerView(toScanner)).
            desk.SetScannerView(Quaternion.Euler(0f, 27f, 0f) * Vector3.forward, 0f);

            MethodInfo inViewFrame = typeof(DeskController).GetMethod("InViewFrame", Private);
            var corners = new[] { new Vector3(0.1f, 1f, 0.2f), new Vector3(0.3f, 1f, 0.2f), new Vector3(0.1f, 1f, 0.5f), new Vector3(0.3f, 1f, 0.5f) };
            var rect = (DeskRect)inViewFrame.Invoke(desk, new object[] { corners });
            Assert.AreEqual(0.2f, rect.Width, 1e-4f, "a 20 cm wide footprint on the desk's axes stays 20 cm wide (turned 27 degrees, it grew to 31 cm)");
            Assert.AreEqual(0.3f, rect.Height, 1e-4f, "and 30 cm deep (turned, 36 cm)");
            Assert.AreEqual(0.2f - 0.27f, rect.CentreX, 1e-4f, "measured from the desk surface's centre, along the desk's right");
            Assert.AreEqual(0.35f, rect.CentreY, 1e-4f, "and along the desk's forward");
        }
        finally
        {
            DeskLayout.Unbind();
            if (deskObject != null)
                Object.DestroyImmediate(deskObject);
            if (surfaceObject != null)
                Object.DestroyImmediate(surfaceObject);
        }
    }
}
