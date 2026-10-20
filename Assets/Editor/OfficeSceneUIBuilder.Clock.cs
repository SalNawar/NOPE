using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// The office builder's CRT clock on the desk.
/// Part of <see cref="OfficeSceneUIBuilder"/>; carved out of OfficeSceneUIBuilder.Desk.cs by P0-A (pure move). Owner: UI-PC.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>A primitive part of a stand-in prop (no collider: the prop's click box takes the clicks).</summary>
    /// <summary>
    /// The desk CRT's clock plate (CrtClock; Saleh 2026-10-18), rebuilt each
    /// run under the Office root, inactive until the screen shows the desktop
    /// (PcScreenClone lays it on the glass at bind): a dark unlit quad, the
    /// taskbar tray's colour, with the time over the date in the tray clock's
    /// face and ink (world texts on the gameplay's sorting layer).
    /// </summary>
    private static CrtClock BuildCrtClock(Transform office, DeskConfigSO config, TMP_Text trayClock)
    {
        DestroyChildIfPresent(office, "CrtClock");
        Transform host = EnsureChild(office, "CrtClock");
        Material plateMaterial = EnsureMaterial("CrtClock_Plate", "Universal Render Pipeline/Unlit", m =>
        {
            m.SetColor("_BaseColor", CrtClockPlate);
            BaseShaderGUI.SetMaterialKeywords(m);
        });
        GameObject plate = PrimitivePart(host, "Plate", PrimitiveType.Quad, Vector3.zero, Vector3.one, plateMaterial);
        plate.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

        TextMeshPro Line(string name, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshPro));
            go.transform.SetParent(host, false);
            TextMeshPro tmp = go.GetComponent<TextMeshPro>();
            tmp.text = string.Empty;
            if (trayClock != null && trayClock.font != null)
            {
                tmp.font = trayClock.font;
                tmp.fontSharedMaterial = trayClock.font.material;
            }
            tmp.color = trayClock != null ? trayClock.color : Color.white;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.enableAutoSizing = true;
            tmp.sortingLayerID = GameplaySortingLayerId();
            return tmp;
        }
        TextMeshPro time = Line("Time", FontStyles.Bold);
        TextMeshPro date = Line("Date", FontStyles.Normal);

        CrtClock clock = host.gameObject.AddComponent<CrtClock>();
        var so = new SerializedObject(clock);
        SetRef(so, "config", config);
        SetRef(so, "plate", plate.transform);
        SetRef(so, "timeText", time);
        SetRef(so, "dateText", date);
        so.ApplyModifiedProperties();
        host.gameObject.SetActive(false);
        return clock;
    }

    /// <summary>The CRT clock plate's colour: the taskbar tray's dark glass.</summary>
    private static readonly Color CrtClockPlate = new Color(0.07f, 0.1f, 0.09f, 1f);
}
