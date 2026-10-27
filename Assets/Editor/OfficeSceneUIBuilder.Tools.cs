using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The office builder's desk tools: the UV torch (its pool, collider and
/// materials). The magnifiers (the lens kit) were cut by P0-C (docs/pending/CUTS.md).
/// Part of <see cref="OfficeSceneUIBuilder"/>; carved out of OfficeSceneUIBuilder.Machines.cs and .Upgrades.cs by P0-A (pure move). Owner: TOOLS.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The thumb slide's travel when on, in the torch's space: toward the lens (uv_torch_model.py's SWITCH_TRAVEL, 10 mm) and pressed 1 mm down (GAME_FEEL_BIBLE C3: "The thumb switch clicks down 1 mm").</summary>
    private static readonly Vector3 UvTorchSwitchTravel = new Vector3(0f, -0.001f, -0.010f);

    /// <summary>The click box's margin round the torch's own meshes (metres, all round): it hugs the torch (BUG-4b).</summary>
    private const float UvTorchClickMargin = 0.005f;

    /// <summary>The UV torch (DeskHardware/UvTorch): its model, its click box over the torch at rest (its own meshes only: the beam's cone, BeamFX, never widens it, BUG-4b), the violet spot light, the hum's loop and the switch's sources; null without its art.</summary>
    private static UvTorch BuildUvTorch(Transform root)
    {
        Transform host = EnsureChild(root, "UvTorch");
        UvTorch torch = GetOrAdd<UvTorch>(host.gameObject);
        GameObject machine = MachineArt(host, UvTorchModel, PropArt.UvTorch, UvTorchMaterial);
        if (machine == null)
        {
            Object.DestroyImmediate(host.gameObject);
            return null;
        }
        Transform m = machine.transform;
        Transform body = m.Find("UvTorch");
        host.gameObject.layer = OfficeLayers.InteractableLayer;
        BoxCollider box = GetOrAdd<BoxCollider>(host.gameObject);
        Bounds b = TorchBounds(body, host);
        box.center = b.center;
        box.size = b.size + Vector3.one * (2f * UvTorchClickMargin);
        Clickable click = GetOrAdd<Clickable>(host.gameObject);
        click.SetOutline(body.GetComponentsInChildren<Renderer>(true).Where(r => r.name != "BeamFX").ToArray());

        DestroyChildIfPresent(host, "UvSpot");
        var spotGo = new GameObject("UvSpot", typeof(Light));
        spotGo.transform.SetParent(host, false);
        Light spot = spotGo.GetComponent<Light>();
        spot.type = LightType.Spot;
        spot.color = UvLightColour;
        spot.intensity = 12f;
        spot.range = 0.5f;
        spot.spotAngle = 40f;
        spot.innerSpotAngle = 20f;
        spot.shadows = LightShadows.None;
        spot.enabled = false;

        DestroyChildIfPresent(host, "UvPool");
        GameObject poolGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        poolGo.name = "UvPool";
        Object.DestroyImmediate(poolGo.GetComponent<Collider>());
        poolGo.transform.SetParent(host, false);
        MeshRenderer poolRenderer = poolGo.GetComponent<MeshRenderer>();
        poolRenderer.sharedMaterial = EnsureMaterial("UvPool", "Universal Render Pipeline/Unlit", x =>
        {
            x.SetColor("_BaseColor", new Color(0.42f, 0.2f, 0.95f, 0.42f));
            x.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(UvTextures + "/UvPool.png"));
            x.SetFloat("_Surface", 1f);
            x.SetFloat("_Blend", 0f); // a violet wash over the paper; the marks glow over it (their queues come after)
            UnityEditor.BaseShaderGUI.SetMaterialKeywords(x);
            x.renderQueue = 2984;
        });
        poolRenderer.shadowCastingMode = ShadowCastingMode.Off;
        poolRenderer.receiveShadows = false;
        poolGo.SetActive(false);

        AudioSource sound = GetOrAdd<AudioSource>(host.gameObject);
        sound.playOnAwake = false;
        sound.spatialBlend = 0f;
        Transform humHost = EnsureChild(host, "Hum");
        AudioSource hum = GetOrAdd<AudioSource>(humHost.gameObject);
        hum.playOnAwake = false;
        hum.loop = true;
        hum.spatialBlend = 0f;
        hum.volume = 0.6f;

        Transform beam = body.Find("BeamFX");
        beam.gameObject.SetActive(false);
        var so = new SerializedObject(torch);
        SetRef(so, "torch", body);
        SetRef(so, "switchKnob", body.Find("Switch"));
        SetRef(so, "lens", body.Find("Lens").GetComponent<Renderer>());
        SetRef(so, "beam", beam.gameObject);
        SetRef(so, "spot", spot);
        SetRef(so, "pool", poolGo.transform);
        SetRef(so, "hum", hum);
        SetRef(so, "sound", sound);
        so.FindProperty("lensLocal").vector3Value = body.InverseTransformPoint(body.Find("Lens").position);
        so.FindProperty("switchTravel").vector3Value = UvTorchSwitchTravel;
        so.ApplyModifiedProperties();
        return torch;
    }

    /// <summary>
    /// The bounds of the torch's own meshes under <paramref name="body"/>, in
    /// <paramref name="space"/>'s local space: the beam's cone (BeamFX, 32 cm
    /// long) left out. 1019a's box took it in (LocalBounds), 2.6 times the
    /// torch, and swallowed clicks on the chip reader beside it (BUG-4b).
    /// </summary>
    private static Bounds TorchBounds(Transform body, Transform space)
    {
        Transform beam = body.Find("BeamFX");
        Bounds b = default;
        bool any = false;
        foreach (MeshFilter f in body.GetComponentsInChildren<MeshFilter>(true))
        {
            if (f.sharedMesh == null || (beam != null && f.transform.IsChildOf(beam)))
                continue;
            foreach (Vector3 corner in Corners(f.sharedMesh.bounds))
            {
                Vector3 p = space.InverseTransformPoint(f.transform.TransformPoint(corner));
                if (any)
                    b.Encapsulate(p);
                else
                    b = new Bounds(p, Vector3.zero);
                any = true;
            }
        }
        return b;
    }

    /// <summary>The desk material for a slot of the UV torch's model (uv_torch_model.py's table): solids in NOPE/Desk Anime, the lens URP Unlit with its face, the beam an additive Unlit violet.</summary>
    private static Material UvTorchMaterial(string slot)
    {
        switch (slot)
        {
            case "Torch_Rubber": return AnimeMaterial("TorchArt_Rubber", Hex("#2B1C24"));
            case "Torch_Chrome": return AnimeMaterial("TorchArt_Chrome", Hex("#C9CCD4"));
            case "Torch_Brass": return AnimeMaterial("TorchArt_Brass", Hex("#D4A055"));
            case "Torch_Switch": return AnimeMaterial("TorchArt_Switch", Hex("#C23A2E"));
            case "Torch_Label": return DecalMaterial("TorchArt_Label", "UvTorch_Label.png", Hex("#9886A8"));
            case "Cradle_Body": return AnimeMaterial("TorchArt_Cradle", Hex("#8A2F3B"));
            case "Cradle_Label": return DecalMaterial("TorchArt_CradleLabel", "UvTorch_Cradle.png", Hex("#4A5872"));
            case "Torch_Lens":
                return EnsureMaterial("TorchArt_Lens", "Universal Render Pipeline/Unlit", x =>
                {
                    x.SetColor("_BaseColor", new Color(0.75f, 0.62f, 1f));
                    x.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(MachinesFolder + "/UvTorch_Lens.png"));
                });
            case "Torch_Beam":
                return EnsureMaterial("TorchArt_Beam", "Universal Render Pipeline/Unlit", x =>
                {
                    x.SetColor("_BaseColor", new Color(0.55f, 0.36f, 1f, 0.3f));
                    x.SetFloat("_Surface", 1f);
                    x.SetFloat("_Blend", 2f); // additive: light, not paint
                    x.SetFloat("_Cull", 0f);
                    UnityEditor.BaseShaderGUI.SetMaterialKeywords(x);
                });
            default: return AnimeMaterial("TorchArt_Body", Hex("#4A5872"));
        }
    }
}
