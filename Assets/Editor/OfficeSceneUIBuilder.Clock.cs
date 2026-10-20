using TMPro;
using UnityEngine;

/// <summary>
/// The office builder's retired CRT clock (BUG-20a).
/// Part of <see cref="OfficeSceneUIBuilder"/>; carved out of OfficeSceneUIBuilder.Desk.cs by P0-A (pure move). Owner: UI-PC.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>
    /// The desk CRT's clock plate is retired (BUG-20a, Saleh's note 10: "not
    /// a massive box"; the PC's clock is the taskbar's tray clock): rebuilt
    /// each run as an empty, inactive CrtClock host under the Office root with
    /// two empty texts ("Time", "Date") and no plate, because DESK-PLAN's
    /// BuildOffice and the root's wiring still look them up; nothing shows
    /// it. INTEGRATION deletes the host with those lines
    /// (docs/pending/UI-PC.md). <paramref name="config"/> and
    /// <paramref name="trayClock"/> are no longer read (the signature is the
    /// caller's).
    /// </summary>
    private static CrtClock BuildCrtClock(Transform office, DeskConfigSO config, TMP_Text trayClock)
    {
        DestroyChildIfPresent(office, "CrtClock");
        Transform host = EnsureChild(office, "CrtClock");
        foreach (string line in new[] { "Time", "Date" })
        {
            var go = new GameObject(line, typeof(RectTransform), typeof(TextMeshPro));
            go.transform.SetParent(host, false);
            go.GetComponent<TextMeshPro>().text = string.Empty;
        }
        CrtClock clock = host.gameObject.AddComponent<CrtClock>();
        host.gameObject.SetActive(false);
        return clock;
    }
}
