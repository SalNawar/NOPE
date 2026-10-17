using UnityEngine;

/// <summary>
/// Small helpers the upgrades' desk objects share (ScannerAddOns, the
/// objects under DeskUpgrades): a lamp's glow set per renderer (its Unlit
/// base colour, through a property block, so the shared material stays as
/// built), a spring aimed (a snap under Reduced Motion) and stepped.
/// </summary>
public static class DeskProps
{
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor"), Color = Shader.PropertyToID("_Color");

    /// <summary>Sets <paramref name="r"/>'s base colour to <paramref name="c"/> (its own, not its material's).</summary>
    public static void Tint(Renderer r, UnityEngine.Color c, MaterialPropertyBlock block)
    {
        if (r == null || block == null)
            return;
        r.GetPropertyBlock(block);
        block.SetColor(BaseColor, c);
        block.SetColor(Color, c);
        r.SetPropertyBlock(block);
    }

    /// <summary>Aims <paramref name="spring"/> at <paramref name="target"/> (snapped there under Reduced Motion).</summary>
    public static void Aim(ref Spring spring, float target)
    {
        spring.Target = target;
        if (UiMotion.Amount.Still)
            spring.Snap(target);
    }

    /// <summary>Steps <paramref name="spring"/> by <paramref name="dt"/> in <paramref name="feel"/>; true while it moved.</summary>
    public static bool Step(ref Spring spring, float dt, MotionFeel feel)
    {
        if (spring.AtRest)
            return false;
        spring.Step(dt, UiMotion.Knobs.Get(feel), 1e-4f, 1e-3f);
        return true;
    }
}
