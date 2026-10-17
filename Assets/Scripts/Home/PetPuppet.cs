using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A pet's picture drawn as a puppet (Saleh 2026-10-17: "dog and cat animate
/// by squesh and squeez instead of a real animation"): the sprite on a grid
/// mesh whose every vertex its rig bends into the pose (PetAnimator.Deform),
/// so the head tilts, the ears flick, the tail wags and the chest breathes
/// without a seam. PetStandIn poses it each frame; its blink (the eyes shut)
/// is a second puppet on the same rig and pose. Fitted into its rect keeping
/// the picture's aspect; no raycasts.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class PetPuppet : MaskableGraphic
{
    /// <summary>The mesh's cells a side: fine enough for a smooth bend.</summary>
    private const int Cells = 32;

    /// <summary>The picture.</summary>
    private Sprite _sprite;

    /// <summary>Its rig (null: drawn flat).</summary>
    private PetRig _rig;

    /// <summary>The pose it is drawn in.</summary>
    private PetPose _pose;

    /// <summary>The picture's texture.</summary>
    public override Texture mainTexture => _sprite != null ? _sprite.texture : s_WhiteTexture;

    /// <summary>Draws <paramref name="sprite"/> on <paramref name="rig"/>, at rest.</summary>
    public void Show(Sprite sprite, PetRig rig)
    {
        _sprite = sprite;
        _rig = rig;
        _pose = default;
        raycastTarget = false;
        SetMaterialDirty();
        SetVerticesDirty();
    }

    /// <summary>Bends the picture into <paramref name="pose"/>.</summary>
    public void Pose(PetPose pose)
    {
        _pose = pose;
        SetVerticesDirty();
    }

    /// <summary>The grid over the picture's box, each vertex where the rig puts it in the pose, its texture coordinate where it was.</summary>
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (_sprite == null)
            return;

        Rect r = GetPixelAdjustedRect();
        float aspect = _sprite.rect.width / Mathf.Max(1f, _sprite.rect.height);
        float w = Mathf.Min(r.width, r.height * aspect), h = w / aspect;
        var box = new Rect(r.center.x - w / 2f, r.center.y - h / 2f, w, h);
        Vector4 uv = UnityEngine.Sprites.DataUtility.GetOuterUV(_sprite);
        Color32 tint = color;

        for (int j = 0; j <= Cells; j++)
            for (int i = 0; i <= Cells; i++)
            {
                float u = i / (float)Cells, v = j / (float)Cells;
                PetAnimator.Deform(_rig, _pose, u, 1f - v, out float x, out float y);
                vh.AddVert(new Vector3(box.xMin + x * box.width, box.yMin + (1f - y) * box.height), tint,
                           new Vector4(Mathf.Lerp(uv.x, uv.z, u), Mathf.Lerp(uv.y, uv.w, v)));
            }

        const int row = Cells + 1;
        for (int j = 0; j < Cells; j++)
            for (int i = 0; i < Cells; i++)
            {
                int a = j * row + i;
                vh.AddTriangle(a, a + row, a + row + 1);
                vh.AddTriangle(a, a + row + 1, a + 1);
            }
    }
}
