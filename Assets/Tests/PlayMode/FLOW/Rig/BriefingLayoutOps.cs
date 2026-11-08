using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// The morning briefing's layout op (briefing-memo-overlap, 2026-11-07):
/// <list type="bullet">
/// <item><c>briefingMemoClear</c> (name, optional): asserts
/// <c>briefing.layout.memoClear</c> (with ".&lt;name&gt;" when named): on
/// screen, the Bureau memo's rect (its tilted corners enclosed) and the
/// small story's printed lines (the timeline column's text bounds) do not
/// intersect, so the memo hides no line the player must read. Not
/// applicable on a day with no memo or no story.</item>
/// </list>
/// </summary>
public static class BriefingLayoutOps
{
#if UNITY_EDITOR
    /// <summary>Also known in edit mode, so the scenario tests read the recipes with known ops.</summary>
    [UnityEditor.InitializeOnLoadMethod]
    private static void RegisterInEditor() => Register();
#endif

    /// <summary>Registers the op once (a literal <c>QaRunner.RegisterOp("name"</c> call, read by tools/qa/test_seqlib.py).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        if (!QaRunner.IsKnownOp("briefingMemoClear"))
            QaRunner.RegisterOp("briefingMemoClear", MemoClear);
    }

    private static IEnumerator MemoClear(QaRunner runner, QaStep step)
    {
        yield return QaGame.Frames(2);
        string id = string.IsNullOrEmpty(step.name) ? "briefing.layout.memoClear" : $"briefing.layout.memoClear.{step.name}";
        DayFlowUIController flow = QaGame.Find<DayFlowUIController>();
        var memo = flow != null ? QaGame.Field(flow, "briefingMemo") as GameObject : null;
        var story = flow != null ? QaGame.Field(flow, "briefingStoryText") as TMP_Text : null;
        string at = $"{Screen.width}x{Screen.height}";
        if (memo == null || story == null || !memo.activeInHierarchy || !story.gameObject.activeInHierarchy || story.textInfo.characterCount == 0)
        {
            runner.NotApplicable(id, null, $"day {QaGame.Day()} at {at}: memo {(memo != null && memo.activeInHierarchy ? "shown" : "hidden")}, story {(story != null ? story.textInfo.characterCount : 0)} chars", "no memo or no story on this briefing");
            yield break;
        }
        Rect memoRect = QaGame.ScreenRect(memo) ?? Rect.zero;
        Rect lines = PrintedRect(story);
        float gap = lines.yMin - memoRect.yMax;
        bool clear = !memoRect.Overlaps(lines);
        runner.Assert(id, null, clear,
            $"day {QaGame.Day()} at {at}: story lines y {lines.yMin:0}..{lines.yMax:0}, memo top y {memoRect.yMax:0} (gap {gap:0.0} px; x story {lines.xMin:0}..{lines.xMax:0}, memo {memoRect.xMin:0}..{memoRect.xMax:0}), story {story.textInfo.lineCount} lines at {story.fontSize:0.#}",
            "the memo's enclosed screen rect and the story's printed text bounds do not intersect (y from the bottom)");
    }

    /// <summary>The screen rect (pixels, origin bottom-left) of <paramref name="text"/>'s printed lines.</summary>
    private static Rect PrintedRect(TMP_Text text)
    {
        Canvas canvas = text.GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        Bounds b = text.textBounds;
        float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
        for (int i = 0; i < 4; i++)
        {
            Vector3 local = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, 0f);
            Vector2 s = RectTransformUtility.WorldToScreenPoint(cam, text.transform.TransformPoint(local));
            x0 = Mathf.Min(x0, s.x); y0 = Mathf.Min(y0, s.y);
            x1 = Mathf.Max(x1, s.x); y1 = Mathf.Max(y1, s.y);
        }
        return Rect.MinMaxRect(x0, y0, x1, y1);
    }
}
