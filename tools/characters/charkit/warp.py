"""Resampling a keyed layer onto the canvas with a similarity transform.

Colours are premultiplied by alpha before resampling (so transparent pixels
never bleed their colour into edges) and un-premultiplied afterwards. Each
channel is resampled with Pillow's antialiased bicubic resize over an exact
fractional source box, which handles both the small downscales of the
full-body sources and the larger rescales of the head-only sources.
"""

import numpy as np
from PIL import Image

from . import imgops as io

PAD = 512


def _resample(channel, box, size):
    img = Image.fromarray(np.pad(channel.astype(np.float32), PAD, mode="constant"), "F")
    b = (box[0] + PAD, box[1] + PAD, box[2] + PAD, box[3] + PAD)
    return np.asarray(img.resize(size, Image.BICUBIC, box=b), np.float32)


def warp(rgb, alpha, T, size):
    """rgb (H, W, 3) 0..255 and alpha 0..1 in source space -> (rgb, alpha) on a canvas of `size` (w, h).

    T maps source coordinates to canvas coordinates (canvas = s * src + t).
    """
    w, h = size
    box = (-T.tx / T.s, -T.ty / T.s, (w - T.tx) / T.s, (h - T.ty) / T.s)
    a = np.clip(_resample(alpha, box, size), 0.0, 1.0)
    out = np.zeros((h, w, 3), np.float32)
    for c in range(3):
        out[..., c] = _resample(rgb[..., c] * alpha, box, size)
    with np.errstate(invalid="ignore", divide="ignore"):
        col = np.where(a[..., None] > 1e-4, out / np.maximum(a, 1e-4)[..., None], 0.0)
    col = np.clip(col, 0, 255)
    a = np.where(a < 1.0 / 255.0, 0.0, a)
    # Un-premultiplying a faint pixel magnifies the resampling filter's overshoot into
    # stray tints: pixels under 60% coverage take the nearest solid pixel's colour.
    solid = a >= 0.9
    near, reached = io.bleed(np.where(solid[..., None], col, 0), solid, 3)
    faint = (a < 0.6) & reached
    col = np.where(faint[..., None], near, col)
    return col, a


def warp_mask(mask, T, size):
    """A binary or float mask warped the same way (float 0..1)."""
    w, h = size
    box = (-T.tx / T.s, -T.ty / T.s, (w - T.tx) / T.s, (h - T.ty) / T.s)
    return np.clip(_resample(mask.astype(np.float32), box, size), 0.0, 1.0)
