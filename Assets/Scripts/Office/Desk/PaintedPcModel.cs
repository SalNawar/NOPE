using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The desk PC's model (Saleh 2026-11-07: "Not modern retro sci-fi 80 anime
/// pc", "With cyberpunk twist"): the Blender build PcRetro.fbx
/// (tools/art/blender/pc_retro.py; DeskConfigSO.pcModel) stands where the
/// painted desk's PC_Crt and PC_Keyboard stood, and their painted meshes hide.
/// The model's root goes on PC_Crt's pivot, square to the desk (the script
/// builds it in the bake's frame from that pivot), under PC_Crt's mesh, so
/// the PC's hover outline (DeskPainted.RenderersOf) takes it in. Only the
/// painted renderers on PC_Crt itself and under PC_Keyboard hide: the CRT's
/// screen quad, its dome and glass (PaintedCrt.Fit), the CRT's click shape
/// (its mesh) and the measured homes stay as they are. The CRT's recess plate
/// (PaintedCrt.Fit, sized for the painted casing's opening) hides too
/// (HideRecess): it is wider than the model's face and showed round it as a
/// flat tan card. The power knob's click goes on the model's drawn knob
/// (KnobFromGlass).
/// </summary>
public static class PaintedPcModel
{
    /// <summary>The model's object name under PC_Crt (a second call finds it).</summary>
    public const string Name = "PcRetro";

    /// <summary>The model's brightness knob from the glass's centre, in metres right and up (pc_retro.py: disc_y at x 0.165, z 0.182; the screen centre at z 0.355).</summary>
    public static readonly Vector2 KnobFromGlass = new Vector2(0.165f, -0.173f);

    /// <summary>The CRT's recess plate (PaintedCrt.Fit).</summary>
    private const string Recess = "CrtRecess";

    /// <summary>The painted desk's CRT and keyboard (DeskPaintedMap.Objects).</summary>
    private const string Crt = "PC_Crt", Keyboard = "PC_Keyboard";

    /// <summary>
    /// Puts <paramref name="model"/> on <paramref name="desk"/>'s PC and hides
    /// the painted CRT and keyboard. Idempotent. Returns the model's instance,
    /// or null (no model, or no PC_Crt in the bake: the painted PC stays).
    /// Logs the model's top-down size against the painted PC's (the contract:
    /// within 5 %).
    /// </summary>
    public static GameObject Wear(DeskPainted desk, GameObject model)
    {
        Transform crt = desk != null ? desk.Mesh(Crt) : null;
        if (model == null || crt == null)
            return null;
        Transform had = crt.Find(Name);
        if (had != null)
            return had.gameObject;

        Bounds before = Footprint(crt, false);
        Transform keyboard = desk.Mesh(Keyboard);
        if (keyboard != null)
            before.Encapsulate(Footprint(keyboard, true));

        GameObject worn = Object.Instantiate(model, crt, false);
        worn.name = Name;
        Transform t = worn.transform;
        t.SetPositionAndRotation(crt.position, desk.transform.rotation);
        Vector3 scale = crt.lossyScale;
        t.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
        foreach (Renderer r in worn.GetComponentsInChildren<Renderer>(true))
        {
            r.gameObject.layer = crt.gameObject.layer;
            r.shadowCastingMode = ShadowCastingMode.Off;
        }

        if (crt.TryGetComponent(out Renderer painted))
            painted.enabled = false;
        if (keyboard != null)
            foreach (Renderer r in keyboard.GetComponentsInChildren<Renderer>(true))
                r.enabled = false;

        Bounds after = Footprint(t, true);
        Debug.Log($"[PaintedPcModel] {model.name} on the desk: {after.size.x:0.000} x {after.size.z:0.000} m from above, {after.size.y:0.000} m high; the painted PC was {before.size.x:0.000} x {before.size.z:0.000} m, {before.size.y:0.000} m high; centre moved {Vector3.Distance(after.center, before.center) * 1000f:0} mm.");
        return worn;
    }

    /// <summary>
    /// Hides the recess plate PaintedCrt.Fit made under <paramref name="screen"/>
    /// (after Fit, when the model is worn): its 0.49 x 0.42 m quad is bigger
    /// than the model's 0.43 x 0.38 m face and showed round it. Idempotent.
    /// </summary>
    public static void HideRecess(Renderer screen)
    {
        Transform recess = screen != null ? screen.transform.Find(Recess) : null;
        if (recess != null && recess.TryGetComponent(out Renderer r))
            r.enabled = false;
    }

    /// <summary>The world bounds of <paramref name="root"/>'s renderers (its own only, or with its children's).</summary>
    private static Bounds Footprint(Transform root, bool children)
    {
        Renderer[] renderers = children ? root.GetComponentsInChildren<Renderer>(true) : root.GetComponents<Renderer>();
        var bounds = new Bounds(root.position, Vector3.zero);
        bool any = false;
        foreach (Renderer r in renderers)
        {
            if (!any)
                bounds = r.bounds;
            else
                bounds.Encapsulate(r.bounds);
            any = true;
        }
        return bounds;
    }
}
