"""Small numpy image helpers: loading, shifts, morphology, distance bands, colour bleeding.

Only numpy and Pillow: the tool must run anywhere the art pipeline runs.
"""

import numpy as np
from PIL import Image


def load_rgb(path):
    """An image as float32 RGB 0..255 (H, W, 3)."""
    return np.asarray(Image.open(path).convert("RGB"), dtype=np.float32)


def save_rgba(path, rgb, alpha):
    """Writes straight (not premultiplied) RGBA; rgb 0..255, alpha 0..1."""
    out = np.dstack([np.clip(rgb, 0, 255), np.clip(alpha, 0, 1) * 255.0])
    Image.fromarray(np.round(out).astype(np.uint8), "RGBA").save(path, optimize=True)


def shift(a, dy, dx, fill=0):
    """a shifted by (dy, dx) (content moves down/right for positive values), edges filled."""
    out = np.full_like(a, fill)
    h, w = a.shape[:2]
    ys, yd = (slice(0, h - dy), slice(dy, h)) if dy >= 0 else (slice(-dy, h), slice(0, h + dy))
    xs, xd = (slice(0, w - dx), slice(dx, w)) if dx >= 0 else (slice(-dx, w), slice(0, w + dx))
    out[yd, xd] = a[ys, xs]
    return out


def _disk(r):
    return [(dy, dx) for dy in range(-r, r + 1) for dx in range(-r, r + 1) if dy * dy + dx * dx <= r * r + r]


def dilate(mask, r):
    """Binary dilation by a disk of radius r."""
    if r <= 0:
        return mask.copy()
    out = np.zeros_like(mask)
    for dy, dx in _disk(r):
        out |= shift(mask, dy, dx, False)
    return out


def erode(mask, r):
    """Binary erosion by a disk of radius r (outside the image counts as set)."""
    if r <= 0:
        return mask.copy()
    out = np.ones_like(mask)
    for dy, dx in _disk(r):
        out &= shift(mask, dy, dx, True)
    return out


def local_max(values, valid, r):
    """Per pixel, the max of `values` over valid pixels within radius r (-inf where none)."""
    src = np.where(valid, values, -np.inf).astype(np.float32)
    out = np.full(values.shape, -np.inf, np.float32)
    for dy, dx in _disk(r):
        np.maximum(out, shift(src, dy, dx, -np.inf), out=out)
    return out


def local_min(values, valid, r):
    """Per pixel, the min of `values` over valid pixels within radius r (+inf where none)."""
    return -local_max(-values, valid, r)


def distance_bands(mask, max_r):
    """Approximate distance (in px, 0 inside) to `mask`, capped at max_r + 1."""
    dist = np.full(mask.shape, max_r + 1, np.int16)
    grown = mask.copy()
    dist[grown] = 0
    for k in range(1, max_r + 1):
        grown = dilate(grown, 1)
        newly = grown & (dist > k)
        dist[newly] = k
    return dist


def bleed(rgb, valid, iterations):
    """Fills invalid pixels with the mean colour of valid 8-neighbours, growing `iterations` px."""
    rgb = rgb.copy()
    valid = valid.copy()
    nbrs = [(dy, dx) for dy in (-1, 0, 1) for dx in (-1, 0, 1) if dy or dx]
    for _ in range(iterations):
        acc = np.zeros_like(rgb)
        cnt = np.zeros(valid.shape, np.float32)
        for dy, dx in nbrs:
            v = shift(valid, dy, dx, False)
            acc += shift(rgb, dy, dx, 0) * v[..., None]
            cnt += v
        grow = (~valid) & (cnt > 0)
        if not grow.any():
            break
        rgb[grow] = acc[grow] / cnt[grow][:, None]
        valid |= grow
    return rgb, valid


def label(mask):
    """4-connected components: (labels int32, count). Pure numpy union-find over runs."""
    h, w = mask.shape
    labels = np.zeros((h, w), np.int32)
    parent = [0]

    def find(a):
        while parent[a] != a:
            parent[a] = parent[parent[a]]
            a = parent[a]
        return a

    prev_runs = []
    for y in range(h):
        row = mask[y]
        if not row.any():
            prev_runs = []
            continue
        d = np.diff(np.concatenate(([0], row.view(np.int8), [0])))
        starts = np.nonzero(d == 1)[0]
        ends = np.nonzero(d == -1)[0]
        runs = []
        j = 0
        for s, e in zip(starts, ends):
            lab = 0
            while j < len(prev_runs) and prev_runs[j][1] <= s:
                j += 1
            k = j
            while k < len(prev_runs) and prev_runs[k][0] < e:
                pl = find(prev_runs[k][2])
                if lab == 0:
                    lab = pl
                elif pl != lab:
                    parent[max(pl, lab)] = min(pl, lab)
                    lab = min(pl, lab)
                k += 1
            if lab == 0:
                lab = len(parent)
                parent.append(lab)
            labels[y, s:e] = lab
            runs.append((s, e, lab))
        prev_runs = runs
    roots = np.array([find(i) for i in range(len(parent))], np.int32)
    uniq, compact = np.unique(roots, return_inverse=True)
    labels = compact[labels].astype(np.int32)
    return labels, len(uniq) - 1


def lum(rgb):
    """Rec. 601 luma of 0..255 RGB."""
    return rgb[..., 0] * 0.299 + rgb[..., 1] * 0.587 + rgb[..., 2] * 0.114
