using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The shut rulebook folder's seal (night build stills, 2026-10-28): on day 1
/// the shut folder read as a plain manila square with a brown strip on its
/// spine. DeskRulebook.Show shuts the folder (FolderCover.ShutNow) at a
/// day's start, which can come before the cover's Awake while the rulebook's
/// object is inactive. The seal's ride (PaperCover.Ride) then used a rest
/// that was never read and put the seal on the hinge, half under the board.
/// The cover is built here as the builder builds it (BuildFolderCover's
/// places and sizes), in edit mode, where its Awake does not run, as on the
/// inactive rulebook. The folder's title on the board (its cover words)
/// rides the same way, so it is checked with the seal. Needs Unity (the
/// EditMode suite).
/// </summary>
public class FolderCoverSealTests
{
    /// <summary>The builder's places (OfficeSceneUIBuilder.BuildFolderCover at RulebookFolderSize 0.46 x 0.25875 m): the hinge on the spine, the board's centre and size, the seal's side.</summary>
    private static readonly Vector3 HingePlace = new Vector3(0.00506f, 0f, -FolderCover.Lift);
    private static readonly Vector2 BoardCentre = new Vector2(0.111895f, -0.007115625f), BoardSize = new Vector2(0.22379f, 0.2372737f);
    private const float SealSide = 0.0939918f, Gap = 0.00005f;

    [Test, Category("NeedsEditor")]
    public void ShutNow_BeforeAwake_KeepsTheSealOnTheBoardWhereItWasBuilt()
    {
        var root = new GameObject("Cover");
        try
        {
            Transform hinge = new GameObject("Hinge").transform;
            hinge.SetParent(root.transform, false);
            hinge.localPosition = HingePlace;
            Renderer front = Quad("Front", hinge, new Vector3(BoardCentre.x, BoardCentre.y, -Gap), BoardSize);
            Renderer back = Quad("Back", hinge, new Vector3(BoardCentre.x, BoardCentre.y, Gap), BoardSize);
            back.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Renderer seal = Quad("Seal", hinge, new Vector3(BoardCentre.x, BoardCentre.y, -2f * Gap), new Vector2(SealSide, SealSide));
            Vector3 built = seal.transform.localPosition;
            var titleObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshPro));
            titleObject.transform.SetParent(hinge, false);
            titleObject.transform.localPosition = new Vector3(BoardCentre.x, BoardCentre.y + 0.083f, -2f * Gap);
            ((RectTransform)titleObject.transform).sizeDelta = new Vector2(0.188f, 0.057f);
            TextMeshPro title = titleObject.GetComponent<TextMeshPro>();
            title.text = "TEMPORAL CUSTOMS BUREAU";
            Vector3 titleBuilt = titleObject.transform.localPosition;

            // edit mode: AddComponent runs no Awake, as a day's start on the inactive rulebook does not
            FolderCover cover = root.AddComponent<FolderCover>();
            var so = new SerializedObject(cover);
            so.FindProperty("hinge").objectReferenceValue = hinge;
            so.FindProperty("front").objectReferenceValue = front;
            so.FindProperty("back").objectReferenceValue = back;
            so.FindProperty("seal").objectReferenceValue = seal.transform;
            SerializedProperty words = so.FindProperty("words");
            words.arraySize = 1;
            words.GetArrayElementAtIndex(0).objectReferenceValue = title;
            so.ApplyModifiedPropertiesWithoutUndo();

            cover.ShutNow();

            Vector3 now = seal.transform.localPosition;
            Assert.That(Vector3.Distance(now, built), Is.LessThan(1e-6f),
                $"the shut folder's seal moved from its built place {built:F5} to {now:F5} (on the hinge: half under the board, a brown strip on the spine)");
            Assert.That(now.z, Is.LessThan(front.transform.localPosition.z), "the seal lies over the board (toward the viewer), not under it");
            Assert.That(Mathf.Abs(now.x - front.transform.localPosition.x), Is.LessThan(1e-6f), "the seal is centred across the board");
            Assert.That(Vector3.Distance(titleObject.transform.localPosition, titleBuilt), Is.LessThan(1e-6f), "the folder's title stays where it was built on the board");
            Assert.That(cover.Shut, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    /// <summary>A unit quad under <paramref name="parent"/> at <paramref name="place"/>, <paramref name="size"/> metres.</summary>
    private static Renderer Quad(string name, Transform parent, Vector3 place, Vector2 size)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        quad.transform.SetParent(parent, false);
        quad.transform.localPosition = place;
        quad.transform.localScale = new Vector3(size.x, size.y, 1f);
        return quad.GetComponent<Renderer>();
    }
}
