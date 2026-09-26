"""Measuring the registration features of each kind of source.

* A base figure: silhouette top (scalp), bottom (soles), centre line (the
  skull's mid-line), and the face features (eye centres, nose, mouth, chin).
* A full-body mannequin source: the visible mannequin contour (where magenta
  meets green: the item does not cover it) and, where the face is uncovered,
  the same face features on the magenta face marks.
* A head-only source: the face features only (eyes, nose, mouth, chin).
"""

import numpy as np

from . import imgops as io
from . import landmarks as lm


def darkness(rgb, magenta):
    """Mark strength: 255 - luma for a drawn face; for the magenta mannequin, how much darker than full magenta."""
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    if not magenta:
        return 255.0 - io.lum(rgb)
    fam = (np.minimum(r, b) - g) > 20
    return np.where(fam, 255.0 - np.maximum(r, b), 0.0)


def face_features(rgb, magenta, head_mask, head_top):
    """Eyes, nose, mouth and chin of a front-facing head.

    head_mask: pixels of the bare head/face (skin or magenta) used for the
    centre line. Returns a dict of (x, y) points, or raises when a feature is
    missing.
    """
    d = darkness(rgb, magenta)
    if not magenta:
        # only inside the figure, away from its outline (the background is "dark" too)
        d = d * io.erode(head_mask, 6)
    xm = lm.centre_x(head_mask, head_top + 60, head_top + 100)
    el, er = lm.eyes(d, xm, head_top + 40, head_top + 140, 62)
    ey = (el[1] + er[1]) / 2.0
    face = d[int(ey):int(ey) + 60, int(xm) - 30:int(xm) + 30]
    runs = lm.face_column(d - np.median(face), xm, ey + 12, ey + 150, 25)
    if len(runs) < 2:
        raise RuntimeError("face column: nose / mouth / chin not found")
    # In order down the centre line: the nose, the mouth (the next mark at
    # least 10 px lower) and the chin line (the next at least 12 px lower).
    nose = runs[0]
    rest = [r for r in runs if r[0] >= nose[1] + 10]
    mouth = rest[0] if rest else None
    after = [r for r in runs if mouth and r[0] >= mouth[1] + 12]
    chin_run = after[0] if after else None

    def peak_row(run):
        a, b, _ = run
        col = d[a:b, int(xm) - 3:int(xm) + 4].mean(axis=1)
        return a + int(np.argmax(col)) + 0.5

    out = {"eye_l": el, "eye_r": er, "nose": (xm, (nose[0] + nose[1]) / 2.0)}
    if mouth:
        out["mouth"] = (xm, peak_row(mouth))
    if chin_run:
        out["chin"] = (xm, peak_row(chin_run))
    out["centre_x"] = xm
    return out


def base(rgb, keyed):
    """A base figure's landmarks (source pixels)."""
    fig = keyed["alpha"] > 0.5
    t = lm.top(fig)
    soles = lm.bottom(fig)
    feats = face_features(rgb, False, fig, t)
    return {"top": t, "soles": soles, "centre_x": feats["centre_x"], "face": feats, "figure": fig}


def mannequin_contour(keyed):
    """The visible mannequin contour: edge pixels of the figure (all non-green) next to magenta."""
    fig = ~keyed["green"]
    near_mag = io.dilate(keyed["magenta"], 3)
    return lm.contour(fig, near_mag)


def mannequin_face(rgb, keyed):
    """Face features on a full-body mannequin source (None when the face marks are covered)."""
    mag = keyed["magenta"] & ((np.minimum(rgb[..., 0], rgb[..., 2]) - rgb[..., 1]) > 100)
    t = lm.top(mag)
    try:
        return face_features(rgb, True, mag, t)
    except (RuntimeError, ValueError, IndexError):
        return None


def head_only(rgb, keyed):
    """Face features of a head-only source (the magenta head fitting form)."""
    mag = keyed["magenta"] & ((np.minimum(rgb[..., 0], rgb[..., 2]) - rgb[..., 1]) > 100)
    t = lm.top(mag)
    return face_features(rgb, True, mag, t)
