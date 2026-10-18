using TMPro;
using UnityEngine;

/// <summary>
/// The desk CRT's clock (Saleh 2026-10-18: "the pc should have time and date
/// too"): from the office view the desktop on the CRT is too small to read its
/// taskbar, so a phosphor plate in the glass's bottom-right corner, over the
/// taskbar's tray, shows the shift clock (HH:MM, written by
/// ShiftClockReadouts with the tray's) over today's date in the agency's
/// calendar ("14 MAR 2150", written by OfficeUIController with the
/// taskbar's), sized as a share of the desktop as the glass shows it
/// (DeskConfigSO.crtClockSize) so it reads at 1280x720. It lies on the glass,
/// facing the chair, a hair in front of it, and shows only while the screen
/// shows the desktop (PcScreenClone: bound and on).
/// </summary>
public sealed class CrtClock : MonoBehaviour
{
    /// <summary>The desk tuning (the plate's share of the desktop).</summary>
    [SerializeField] private DeskConfigSO config;

    /// <summary>The plate behind the text (a unit quad, scaled to the plate).</summary>
    [SerializeField] private Transform plate;

    /// <summary>The clock ("09:07"), the plate's upper part.</summary>
    [SerializeField] private TMP_Text timeText;

    /// <summary>Today's date ("14 MAR 2150"), the plate's lower part.</summary>
    [SerializeField] private TMP_Text dateText;

    /// <summary>How far in front of the glass the plate lies, as a share of the glass's half height.</summary>
    private const float Lift = 0.06f;

    /// <summary>The share of the plate's height the clock takes (the date takes the rest).</summary>
    private const float TimeShare = 0.58f;

    /// <summary>
    /// Lays the plate on the glass <paramref name="glass"/> (the desktop filling
    /// <paramref name="fitU"/> by <paramref name="fitV"/> of its half extents,
    /// from its centre): in the desktop's bottom-right corner, facing the viewer.
    /// </summary>
    public void Place(GlassFrame glass, float fitU, float fitV)
    {
        Vector2 share = config != null ? config.crtClockSize : new Vector2(0.5f, 0.3f);
        Vector3 right = glass.Right * fitU, up = glass.Up * fitV;
        float width = right.magnitude * 2f * share.x, height = up.magnitude * 2f * share.y;
        if (width <= 0f || height <= 0f)
            return;
        Vector3 toViewer = Vector3.Cross(glass.Up, glass.Right).normalized;
        Vector3 centre = glass.Centre + right * (1f - share.x) - up * (1f - share.y) + toViewer * (glass.Up.magnitude * Lift);
        transform.SetPositionAndRotation(centre, Quaternion.LookRotation(-toViewer, glass.Up.normalized));
        transform.localScale = Vector3.one;
        if (plate != null)
            plate.localScale = new Vector3(width, height, 1f);
        Lay(timeText, width, height * TimeShare, height * (1f - TimeShare) / 2f);
        Lay(dateText, width, height * (1f - TimeShare), -height * TimeShare / 2f);
    }

    /// <summary>Shows the plate (the screen shows the desktop) or hides it.</summary>
    public void SetShown(bool shown)
    {
        if (gameObject.activeSelf != shown)
            gameObject.SetActive(shown);
    }

    private static void Lay(TMP_Text text, float width, float height, float y)
    {
        if (text == null)
            return;
        var rect = (RectTransform)text.transform;
        rect.localPosition = new Vector3(0f, y, -0.0005f);
        rect.localRotation = Quaternion.identity;
        rect.sizeDelta = new Vector2(width * 0.92f, height * 0.9f);
        text.enableAutoSizing = true;
        text.fontSizeMax = height * 10f; // a world text's size is ten to the unit
        text.fontSizeMin = height;
    }
}
