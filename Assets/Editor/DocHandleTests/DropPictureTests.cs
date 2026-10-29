using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// FOLD-01 round 2 (the verifier's F1 and F2): the QA rig's measures of a
/// dropped thing's picture (DropPicture). Covered must see a thing lying
/// under something drawn over it (the folder under a tray's rim: the rest's
/// own trace could not, it checks the keep-outs the rest used), and Of must
/// keep the picture of a thing drawn toward the camera (HeldOnTop) where it
/// lies. Makes Unity objects far from anything else in the open scene, so it
/// runs in Unity's EditMode suite only (NeedsEditor).
/// </summary>
public class DropPictureTests
{
    private static readonly Vector3 Far = new Vector3(1000f, 0f, 1000f);

    [Test, Category("NeedsEditor")]
    public void Covered_SeesAThingUnderARim_AndNothingWhenTheRimIsAway()
    {
        GameObject eye = null, thing = null, rim = null;
        Material opaque = null;
        try
        {
            eye = new GameObject("DropPictureTests.Camera");
            Camera camera = eye.AddComponent<Camera>();
            camera.transform.SetPositionAndRotation(Far + new Vector3(0f, 2f, 0f), Quaternion.Euler(90f, 0f, 0f));
            camera.fieldOfView = 30f;
            // The thing: a 20 cm sheet lying face up on the desk 1 m under the camera.
            thing = GameObject.CreatePrimitive(PrimitiveType.Quad);
            thing.name = "DropPictureTests.Sheet";
            thing.transform.SetPositionAndRotation(Far + new Vector3(0f, 1f, 0f), Quaternion.Euler(90f, 0f, 0f));
            thing.transform.localScale = new Vector3(0.2f, 0.2f, 1f);
            // The rim: a bar 2 cm over it across its right half.
            rim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rim.name = "DropPictureTests.Rim";
            rim.transform.position = Far + new Vector3(0.06f, 1.02f, 0f);
            rim.transform.localScale = new Vector3(0.12f, 0.02f, 0.4f);
            // An opaque material of the project's pipeline on both (a primitive's default may be none in a test's scene).
            Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            opaque = new Material(lit) { renderQueue = 2000 };
            thing.GetComponent<MeshRenderer>().sharedMaterial = opaque;
            rim.GetComponent<MeshRenderer>().sharedMaterial = opaque;

            (float? share, int points, string by, int unread) = DropPicture.Covered(thing, camera);
            Assert.IsNotNull(share, $"{by}; material {thing.GetComponent<MeshRenderer>().sharedMaterial}, queue {opaque.renderQueue}, picture {DropPicture.Of(thing, camera)}, camera {camera.pixelWidth}x{camera.pixelHeight}");
            Assert.Greater(points, 100, "points spread over the sheet's face");
            Assert.AreEqual(0, unread, by);
            Assert.That(share.Value, Is.InRange(0.45f, 0.62f), $"about half the sheet lies under the rim ({by})");
            StringAssert.Contains("DropPictureTests.Rim", by, "it names what hides the sheet");

            rim.transform.position = Far + new Vector3(0.5f, 1.02f, 0f);
            (float? clear, _, string none, _) = DropPicture.Covered(thing, camera);
            Assert.AreEqual(0f, clear ?? -1f, 1e-6f, $"nothing over it once the rim is away ({none})");

            // Drawn toward the camera by a ratio about the camera (HeldOnTop), its picture stays where it lies.
            Rect? lying = DropPicture.Of(thing, camera);
            Vector3 at = thing.transform.position, scale = thing.transform.localScale;
            const float ratio = 0.6f;
            thing.transform.position = camera.transform.position + (at - camera.transform.position) * ratio;
            thing.transform.localScale = scale * ratio;
            Rect? held = DropPicture.Of(thing, camera);
            Assert.IsNotNull(lying);
            Assert.IsNotNull(held);
            Assert.AreEqual(lying.Value.center.x, held.Value.center.x, 0.25f, "a quarter pixel: float rounding 1 km from the origin");
            Assert.AreEqual(lying.Value.center.y, held.Value.center.y, 0.25f);
            Assert.AreEqual(lying.Value.width, held.Value.width, 0.25f, "the same picture");
        }
        finally
        {
            foreach (GameObject go in new[] { eye, thing, rim })
                if (go != null)
                    Object.DestroyImmediate(go);
            if (opaque != null)
                Object.DestroyImmediate(opaque);
        }
    }
}
