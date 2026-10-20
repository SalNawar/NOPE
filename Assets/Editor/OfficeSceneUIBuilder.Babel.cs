using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The office builder's BABEL rack.
/// Part of <see cref="OfficeSceneUIBuilder"/>; carved out of OfficeSceneUIBuilder.Upgrades.cs by P0-A (pure move). Owner: none in Phase A (reserved for BABEL-TUNER).
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    private static BabelRack BuildBabelRack(Transform root)
    {
        Transform host = PropHost(root, "BabelRack", BabelRackModel, PropArt.BabelRack, out Transform m);
        if (host == null)
            return null;
        BabelRack rack = GetOrAdd<BabelRack>(host.gameObject);
        Transform b = m.Find("BabelRack");
        var so = new SerializedObject(rack);
        SetArray(so, "cartridges", Enumerable.Range(0, 4).Select(i => (Object)b.Find("Cartridge_" + i)).ToArray());
        SetArray(so, "lamps", Enumerable.Range(0, 4).Select(i => (Object)b.Find($"Cartridge_{i}/CartridgeLED_{i}").GetComponent<Renderer>()).ToArray());
        SetRef(so, "needle", b.Find("Needle"));
        SetRef(so, "click", PropClick(host, "Click", b));
        SetRef(so, "sound", host.GetComponent<AudioSource>());
        so.ApplyModifiedProperties();
        return rack;
    }
}
