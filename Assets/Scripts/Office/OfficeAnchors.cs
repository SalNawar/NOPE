using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>One anchor as the art office resolved it.</summary>
public sealed class ResolvedAnchor
{
    /// <summary>Which place this is.</summary>
    public OfficeAnchorId Id;

    /// <summary>Where it came from (an explicit anchor, a fallback object, the default pose, or nothing).</summary>
    public AnchorSource Source;

    /// <summary>The object found (null for the default pose or a missing anchor).</summary>
    public Transform Transform;

    /// <summary>The path or name that found it ("" when nothing did).</summary>
    public string Path = string.Empty;

    /// <summary>The object's position, or the default position.</summary>
    public Vector3 Position;

    /// <summary>The object's rotation, or the default heading.</summary>
    public Quaternion Rotation = Quaternion.identity;

    /// <summary>True when the object has active renderers (a real prop, not an empty marker).</summary>
    public bool HasBounds;

    /// <summary>The world bounds of the object's active renderers (valid when <see cref="HasBounds"/>).</summary>
    public Bounds Bounds;

    /// <summary>True when an object or the default pose stands for it.</summary>
    public bool Found => Source != AnchorSource.Missing;
}

/// <summary>
/// Resolves the office scene contract against the loaded art office: for each
/// anchor, an object named Anchor_{id} anywhere in the scene, else the first
/// active fallback object, else the default pose (OfficeContract's order).
/// Shared by the gameplay layer's binder (at runtime) and the editor's
/// contract check, so both report the same thing.
/// </summary>
public static class OfficeAnchors
{
    /// <summary>Resolves every anchor of <paramref name="contract"/> in <paramref name="scene"/>.</summary>
    public static Dictionary<OfficeAnchorId, ResolvedAnchor> Resolve(Scene scene, OfficeSceneContractSO contract)
    {
        var result = new Dictionary<OfficeAnchorId, ResolvedAnchor>();
        foreach (OfficeAnchorId id in System.Enum.GetValues(typeof(OfficeAnchorId)))
            result[id] = Resolve(scene, contract != null ? contract.Spec(id) : null, id);
        return result;
    }

    /// <summary>Resolves one anchor (a null spec means no fallbacks and no default).</summary>
    public static ResolvedAnchor Resolve(Scene scene, OfficeSceneContractSO.AnchorSpec spec, OfficeAnchorId id)
    {
        List<string> candidates = OfficeContract.Candidates(id, spec != null ? spec.fallbacks : null);
        for (int i = 0; i < candidates.Count; i++)
        {
            Transform found = scene.IsValid() ? Find(scene, candidates[i], includeInactive: false) : null;
            if (found == null)
                continue;

            var anchor = new ResolvedAnchor
            {
                Id = id,
                Source = OfficeContract.SourceOf(i, spec != null && spec.hasDefault),
                Transform = found,
                Path = candidates[i],
                Position = found.position,
                Rotation = found.rotation,
            };
            anchor.HasBounds = TryBounds(found, out anchor.Bounds);
            return anchor;
        }

        bool hasDefault = spec != null && spec.hasDefault;
        return new ResolvedAnchor
        {
            Id = id,
            Source = OfficeContract.SourceOf(-1, hasDefault),
            Position = hasDefault ? spec.defaultPosition : Vector3.zero,
            Rotation = hasDefault ? Quaternion.Euler(0f, spec.defaultYaw, 0f) : Quaternion.identity,
        };
    }

    /// <summary>
    /// The object a contract path names in a scene: a root path
    /// ("HybridOffice/Booth/Blender_Next") from its root object, or a bare name
    /// matched anywhere (the first in hierarchy order). Inactive objects count
    /// only when <paramref name="includeInactive"/>.
    /// </summary>
    public static Transform Find(Scene scene, string path, bool includeInactive)
    {
        if (!scene.IsValid() || string.IsNullOrWhiteSpace(path))
            return null;

        bool bare = OfficeContract.IsBareName(path);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform hit = null;
            if (!bare)
            {
                int slash = path.IndexOf('/');
                if (root.name == path.Substring(0, slash))
                    hit = root.transform.Find(path.Substring(slash + 1));
            }
            else
            {
                hit = FindByName(root.transform, path, includeInactive);
            }

            if (hit != null && (includeInactive || hit.gameObject.activeInHierarchy))
                return hit;
        }

        return null;
    }

    /// <summary>The first object named <paramref name="name"/> at or under <paramref name="t"/>, depth first (inactive ones skipped unless asked for).</summary>
    private static Transform FindByName(Transform t, string name, bool includeInactive)
    {
        if (!includeInactive && !t.gameObject.activeInHierarchy)
            return null;
        if (t.name == name)
            return t;
        for (int i = 0; i < t.childCount; i++)
        {
            Transform hit = FindByName(t.GetChild(i), name, includeInactive);
            if (hit != null)
                return hit;
        }
        return null;
    }

    /// <summary>The world bounds of the active renderers under <paramref name="t"/>; false when it has none.</summary>
    public static bool TryBounds(Transform t, out Bounds bounds)
    {
        bounds = default;
        bool any = false;
        foreach (Renderer r in t.GetComponentsInChildren<Renderer>(false))
        {
            if (!r.enabled || r is ParticleSystemRenderer)
                continue;
            if (!any)
            {
                bounds = r.bounds;
                any = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }
        return any;
    }

    /// <summary>
    /// The screen's renderer and submesh under the PC: an active renderer named
    /// like a screen (OfficeContract.IsScreenName) and its screen-named
    /// material's submesh (or its first); else the submesh of the first
    /// screen-named material on any renderer.
    /// </summary>
    public static (Renderer renderer, int submesh) FindGlass(Transform pc)
    {
        Renderer[] renderers = pc.GetComponentsInChildren<Renderer>(false);
        foreach (Renderer r in renderers)
            if (r.enabled && OfficeContract.IsScreenName(r.name))
                return (r, Mathf.Max(0, ScreenMaterialIndex(r)));
        foreach (Renderer r in renderers)
        {
            int index = ScreenMaterialIndex(r);
            if (r.enabled && index >= 0)
                return (r, index);
        }
        return (null, 0);
    }

    private static int ScreenMaterialIndex(Renderer r)
    {
        Material[] materials = r.sharedMaterials;
        for (int i = 0; i < materials.Length; i++)
            if (materials[i] != null && OfficeContract.IsScreenName(materials[i].name))
                return i;
        return -1;
    }
}
