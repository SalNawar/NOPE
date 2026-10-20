using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The office builder's RING SERVICE panel.
/// Part of <see cref="OfficeSceneUIBuilder"/>; carved out of OfficeSceneUIBuilder.Upgrades.cs by P0-A (pure move). Owner: DESK-PLAN (leased to PORTALS after #5).
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    private static RingPanel BuildRingPanel(Transform root)
    {
        Transform host = PropHost(root, "RingPanel", RingPanelModel, PropArt.RingPanel, out Transform m);
        if (host == null)
            return null;
        RingPanel panel = GetOrAdd<RingPanel>(host.gameObject);
        Transform p = m.Find("RingPanel");
        var clicks = new Object[5];
        for (int i = 0; i < 5; i++)
            clicks[i] = PropClick(host, "Breaker" + (i + 1) + "Click", p.Find("Breaker_" + (i + 1)), 0.008f);
        var so = new SerializedObject(panel);
        SetArray(so, "lamps", Enumerable.Range(1, 5).Select(i => (Object)p.Find("Lamp_" + i).GetComponent<Renderer>()).ToArray());
        SetArray(so, "breakers", Enumerable.Range(1, 5).Select(i => (Object)p.Find("Breaker_" + i)).ToArray());
        SetArray(so, "stickers", Enumerable.Range(1, 5).Select(i => (Object)p.Find("Sticker_" + i)).ToArray());
        SetArray(so, "clicks", clicks);
        SetRef(so, "sound", host.GetComponent<AudioSource>());
        so.ApplyModifiedProperties();
        return panel;
    }
}
