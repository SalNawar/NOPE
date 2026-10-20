#if UNITY_EDITOR || TIMEDESK_QA
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;

/// <summary>
/// DOC-LOOK's rig op (docs/pending/QA-RIG.md "Tracks' own ops"; FIX_PLAN
/// DOC-LOOK, BUG-9): <c>capHeight</c> measures the cap height on the screen
/// of every text printed on every desk paper shown now, through the camera the
/// pointer looks through, and asserts <c>capHeight.&lt;name&gt;</c>: each
/// paper's smallest data (a Value or a Hand, DeskDocument.Print names each
/// text by its role) at least CapHeight.DataFloorA1 pixels at a screen 1080
/// lines tall or more and CapHeight.DataFloorA2 under it, its smallest label
/// at least CapHeight.LabelFloorA2 at 720 lines and under. The measure is
/// the font's cap line at the text's size, from the text's place, projected
/// up its own page: what a reader sees, the reading view's tilt and the
/// paper's world size included.
/// </summary>
public static class DocLookOps
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#endif
    private static void Register() => QaRunner.RegisterOp("capHeight", CapHeightOp);

    private static IEnumerator CapHeightOp(QaRunner runner, QaStep step)
    {
        yield return null;
        Camera camera = QaGame.PointerCamera();
        string name = string.IsNullOrEmpty(step.name) ? "papers" : step.name;
        if (camera == null)
        {
            runner.Assert("capHeight." + name, "DOC-LOOK", false, "no camera", "No pointer camera to measure through.");
            yield break;
        }
        bool a1 = Screen.height >= 1000;
        float dataFloor = a1 ? CapHeight.DataFloorA1 : CapHeight.DataFloorA2;
        float labelFloor = a1 ? 0f : CapHeight.LabelFloorA2;
        float leastData = float.MaxValue, leastLabel = float.MaxValue;
        string leastDataAt = string.Empty, leastLabelAt = string.Empty;
        int papers = 0, texts = 0;
        foreach (DeskDocument paper in Object.FindObjectsByType<DeskDocument>(FindObjectsInactive.Exclude))
        {
            if (paper.Sheet == null)
                continue;
            papers++;
            foreach (TMP_Text text in paper.Sheet.GetComponentsInChildren<TMP_Text>(false))
            {
                if (string.IsNullOrWhiteSpace(text.text) || !(text.GetComponent<Renderer>() is Renderer r) || !r.enabled || r.forceRenderingOff || text.font == null)
                    continue;
                bool data = text.name == nameof(FormTextRole.Value) || text.name == nameof(FormTextRole.Hand);
                bool label = text.name == nameof(FormTextRole.Label);
                if (!data && !label)
                    continue;
                float px = CapPixels(text, camera);
                if (px <= 0f)
                    continue;
                texts++;
                string at = $"{paper.name}:{text.text.Replace('\n', ' ')}";
                if (data && px < leastData)
                {
                    leastData = px;
                    leastDataAt = at;
                }
                if (label && px < leastLabel)
                {
                    leastLabel = px;
                    leastLabelAt = at;
                }
            }
        }
        bool dataOk = papers > 0 && leastData != float.MaxValue && leastData + 1e-3f >= dataFloor;
        bool labelOk = leastLabel == float.MaxValue || leastLabel + 1e-3f >= labelFloor;
        string measured = string.Format(CultureInfo.InvariantCulture, "{0}x{1}: data {2:0.0} px (floor {3:0}), label {4:0.0} px (floor {5:0}), {6} texts on {7} papers",
                                        Screen.width, Screen.height, leastData == float.MaxValue ? 0f : leastData, dataFloor,
                                        leastLabel == float.MaxValue ? 0f : leastLabel, labelFloor, texts, papers);
        runner.Assert("capHeight." + name, "DOC-LOOK", dataOk && labelOk, measured, $"smallest data '{leastDataAt}', smallest label '{leastLabelAt}'");
    }

    /// <summary>The on-screen pixels of <paramref name="text"/>'s capitals: its font's cap line at its size (a world-space em is TmpFormText.WorldEmPerFontSize per size unit), up its own page, through <paramref name="camera"/>.</summary>
    private static float CapPixels(TMP_Text text, Camera camera)
    {
        FaceInfo face = text.font.faceInfo;
        if (face.pointSize <= 0f)
            return 0f;
        float capLocal = face.capLine / face.pointSize * text.fontSize * TmpFormText.WorldEmPerFontSize;
        Transform t = text.transform;
        Vector3 baseline = t.position;
        Vector3 cap = t.TransformPoint(new Vector3(0f, capLocal, 0f));
        Vector3 a = camera.WorldToScreenPoint(baseline), b = camera.WorldToScreenPoint(cap);
        if (a.z <= 0f || b.z <= 0f)
            return 0f;
        return Vector2.Distance(a, b);
    }
}
#endif
