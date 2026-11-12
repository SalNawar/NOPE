using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The paper's grab pad (DeskDocument's counter half): a paper on the counter
/// shows tiny, so a second box on its sheet takes the press out to a least size
/// on the screen (DeskConfigSO.counterGrabPx; GrabPad.Half stops it half way to a
/// neighbour). It lies in the sheet's plane, so a press on it meets the same
/// plane as one on the sheet and the paper keeps the point pressed under the pointer.
/// </summary>
public sealed partial class DeskDocument
{
    /// <summary>The paper's face mesh as drawn (the guide's rim is drawn round it): a shut booklet's cover (one page high), else the face (D66: the rim of the open booklet framed empty mat over the shut passport).</summary>
    public Renderer PaperFace => cover != null && cover.ShutFace != null ? cover.ShutFace : paperQuad;

    /// <summary>The extra box (made on first use) on the sheet.</summary>
    private BoxCollider _pad;

    /// <summary>True when the paper lies on the counter and takes a press: not held, not carried, not in a machine.</summary>
    public bool WantsGrabPad => Zone == DeskZone.Counter && !_docked && !_carry.Running && (drag == null || !drag.IsDragging);

    /// <summary>The sheet's middle on the screen (px), or false when it is not in front of <paramref name="eye"/>.</summary>
    public bool TryScreenCentre(Camera eye, out (float x, float y) centre)
    {
        centre = default;
        if (eye == null || sheet == null || !sheet.TryGetComponent(out BoxCollider box))
            return false;
        Vector3 s = eye.WorldToScreenPoint(box.bounds.center);
        centre = (s.x, s.y);
        return s.z > 0f;
    }

    /// <summary>Fits the pad to at least <paramref name="minPx"/> by <paramref name="minPx"/> on the screen of <paramref name="eye"/> (never less than the sheet, and <paramref name="marginPx"/> past its edge), stopping half way to each of <paramref name="others"/> (the other counter papers' centres, px). Off when the paper does not lie on the counter or its own box is off.</summary>
    public void FitGrabPad(Camera eye, float minPx, float marginPx, IReadOnlyList<(float x, float y)> others)
    {
        bool want = WantsGrabPad && eye != null && sheet != null && sheet.TryGetComponent(out BoxCollider box) && box.enabled;
        if (!want)
        {
            if (_pad != null)
                _pad.enabled = false;
            return;
        }
        box = sheet.GetComponent<BoxCollider>();
        Vector3 scale = sheet.lossyScale;
        Vector3 middle = box.bounds.center;
        Vector3 at = eye.WorldToScreenPoint(middle);
        float px = PxPerMetre(eye, middle, at, sheet.right);
        float py = PxPerMetre(eye, middle, at, sheet.up);
        if (at.z <= 0f || px < 1f || py < 1f || scale.x <= 1e-5f || scale.y <= 1e-5f)
        {
            if (_pad != null)
                _pad.enabled = false;
            return;
        }
        (float hx, float hy) = GrabPad.Half(minPx / 2f, marginPx, box.size.x * scale.x * px / 2f, box.size.y * scale.y * py / 2f, at.x, at.y, others);
        if (_pad == null)
            _pad = sheet.gameObject.AddComponent<BoxCollider>();
        _pad.center = box.center;
        _pad.size = new Vector3(2f * hx / px / scale.x, 2f * hy / py / scale.y, box.size.z);
        _pad.enabled = true;
    }

    /// <summary>How many screen px one metre along <paramref name="axis"/> (world) covers at <paramref name="middle"/>.</summary>
    private static float PxPerMetre(Camera eye, Vector3 middle, Vector3 at, Vector3 axis)
    {
        Vector3 s = eye.WorldToScreenPoint(middle + axis * 0.01f);
        return new Vector2(s.x - at.x, s.y - at.y).magnitude / 0.01f;
    }
}
