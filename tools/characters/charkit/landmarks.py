"""Landmark detection on raw sources: silhouette extents, eyes, chin, contours.

All coordinates are continuous pixel coordinates (a pixel's centre is its
index + 0.5), so they transform exactly under the similarity warp.
"""

import numpy as np

from . import imgops as io


def _rows(mask, min_px=4):
    counts = mask.sum(axis=1)
    return np.nonzero(counts >= min_px)[0]


def top(mask, min_px=4):
    """The top edge of a figure mask (the first row with at least min_px pixels)."""
    return float(_rows(mask, min_px)[0])


def bottom(mask, min_px=4):
    """The bottom edge of a figure mask (just below the last row with at least min_px pixels)."""
    return float(_rows(mask, min_px)[-1] + 1)


def centre_x(mask, y0, y1):
    """The median mid-point of each row's outermost pixels between rows y0 and y1."""
    mids = []
    for y in range(int(y0), int(y1)):
        xs = np.nonzero(mask[y])[0]
        if len(xs) >= 2:
            mids.append((xs[0] + xs[-1] + 1) / 2.0)
    return float(np.median(mids))


def eyes(darkness, x_mid, y0, y1, half_width, open_r=4, rel=0.45):
    """The two eye centres ((xl, yl), (xr, yr)) in a band of rows y0..y1.

    The dark marks (darkness >= rel x its 99th percentile in the band) are
    opened with a disk of radius open_r, which removes thin strokes (brows,
    nose, mouth, outlines) and keeps the eyes (iris and upper lash line); each
    eye is the centroid of the largest remaining blob on its side of x_mid.
    """
    ya, yb = int(y0), int(y1)
    xa, xb = int(x_mid - half_width), int(x_mid + half_width)
    win = darkness[ya:yb, xa:xb]
    marks = win >= rel * np.percentile(win, 99)
    blobs = io.dilate(io.erode(marks, open_r), open_r) & marks
    labels, n = io.label(blobs)
    out = []
    for side in (-1, 1):
        best, best_area = None, 0
        for k in range(1, n + 1):
            ys, xs = np.nonzero(labels == k)
            cx = xs.mean() + xa + 0.5
            if (cx - x_mid) * side <= 4 or len(xs) <= best_area:
                continue
            best, best_area = (cx, ys.mean() + ya + 0.5), len(xs)
        if best is None:
            raise RuntimeError("eye not found")
        out.append(best)
    return out[0], out[1]


def face_column(darkness, x_mid, y_from, y_to, threshold, half_band=3):
    """Dark runs crossing the centre column band between rows y_from and y_to.

    Returns [(start_y, end_y, peak)] top to bottom: below the eyes these are
    the nose, the mouth and the chin (jaw) line.
    """
    band = darkness[int(y_from):int(y_to), int(x_mid) - half_band:int(x_mid) + half_band + 1].mean(axis=1)
    runs, start = [], None
    for i, v in enumerate(band):
        if v >= threshold and start is None:
            start = i
        if (v < threshold or i == len(band) - 1) and start is not None:
            end = i if v < threshold else i + 1
            runs.append((int(y_from) + start, int(y_from) + end, float(band[start:end].max())))
            start = None
    return runs


def contour(mask, inside=None):
    """Boundary pixels of a mask (set pixels with an unset 4-neighbour), optionally only where `inside` holds.

    Returns an (N, 2) array of (x, y) continuous coordinates.
    """
    edge = mask & ~io.erode(mask, 1)
    if inside is not None:
        edge &= inside
    ys, xs = np.nonzero(edge)
    return np.stack([xs + 0.5, ys + 0.5], axis=1).astype(np.float32)
