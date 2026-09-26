"""Registration: similarity transforms (uniform scale + translation, no rotation).

A transform maps source coordinates to target coordinates: q = s * p + t.
Front-view figures are upright, so rotation is not estimated (a rotated
source would show up as large residuals and be sent back for regeneration).
"""

import numpy as np

from . import imgops as io


class Similarity:
    """q = s * p + (tx, ty)."""

    def __init__(self, s=1.0, tx=0.0, ty=0.0):
        self.s, self.tx, self.ty = float(s), float(tx), float(ty)

    def apply(self, pts):
        pts = np.asarray(pts, np.float64)
        return pts * self.s + np.array([self.tx, self.ty])

    def then(self, other):
        """This transform followed by `other`."""
        return Similarity(self.s * other.s, other.s * self.tx + other.tx, other.s * self.ty + other.ty)

    def as_dict(self):
        return {"scale": round(self.s, 5), "tx": round(self.tx, 3), "ty": round(self.ty, 3)}


def fit_points(src, dst, weights=None):
    """Least-squares similarity mapping src points onto dst points."""
    p = np.asarray(src, np.float64)
    q = np.asarray(dst, np.float64)
    w = np.ones(len(p)) if weights is None else np.asarray(weights, np.float64)
    w = w / w.sum()
    pm = (p * w[:, None]).sum(0)
    qm = (q * w[:, None]).sum(0)
    pc, qc = p - pm, q - qm
    s = (w[:, None] * pc * qc).sum() / (w[:, None] * pc * pc).sum()
    t = qm - s * pm
    return Similarity(s, t[0], t[1])


def _nearest(src, dst, chunk=2048):
    """For each src point, the index of and distance to its nearest dst point."""
    idx = np.empty(len(src), np.int64)
    dist = np.empty(len(src), np.float64)
    for i in range(0, len(src), chunk):
        d2 = ((src[i:i + chunk, None, :] - dst[None, :, :]) ** 2).sum(-1)
        j = d2.argmin(1)
        idx[i:i + chunk] = j
        dist[i:i + chunk] = np.sqrt(d2[np.arange(len(j)), j])
    return idx, dist


def chamfer_fit(src_pts, dst_mask_edge, dst_pts, init, search_px=12, search_scale=0.04, max_pts=2500,
                marks_src=None, marks_dst=None):
    """Registers source contour points onto a destination contour.

    A coarse grid search over scale and translation (around `init`) on a
    truncated distance map of the destination contour, then ICP refinement
    with exact nearest neighbours (points farther than 6 px ignored).
    Optional matched mark points (marks_src -> marks_dst, e.g. the eyes and
    the nose) join both stages with the same total weight as the contour.
    Returns (Similarity, stats) with stats: mean / p95 distance (px) of the
    inlier points after the fit, and the inlier share.
    """
    ms = None if marks_src is None else np.asarray(marks_src, np.float64)
    md = None if marks_dst is None else np.asarray(marks_dst, np.float64)
    rng = np.random.default_rng(0)
    src = np.asarray(src_pts, np.float64)
    if len(src) > max_pts:
        src = src[rng.choice(len(src), max_pts, replace=False)]
    dst = np.asarray(dst_pts, np.float64)
    if len(dst) > 4 * max_pts:
        dst = dst[rng.choice(len(dst), 4 * max_pts, replace=False)]

    dmap = io.distance_bands(dst_mask_edge, 20).astype(np.float32)
    h, w = dmap.shape
    cx, cy = src[:, 0].mean(), src[:, 1].mean()

    def cost(s, tx, ty):
        q = (src - [cx, cy]) * s + [cx * init.s + init.tx, cy * init.s + init.ty] + [tx, ty]
        xi = np.clip(q[:, 0].astype(int), 0, w - 1)
        yi = np.clip(q[:, 1].astype(int), 0, h - 1)
        c = np.minimum(dmap[yi, xi], 8).mean()
        if ms is not None:
            qm = (ms - [cx, cy]) * s + [cx * init.s + init.tx, cy * init.s + init.ty] + [tx, ty]
            c += np.minimum(np.hypot(*(qm - md).T), 8).mean()
        return c

    best = (np.inf, init.s, 0, 0)
    for s in np.linspace(init.s * (1 - search_scale), init.s * (1 + search_scale), 21):
        for ty in range(-search_px, search_px + 1, 2):
            for tx in range(-search_px, search_px + 1, 2):
                c = cost(s, tx, ty)
                if c < best[0]:
                    best = (c, s, tx, ty)
    _, s, tx, ty = best
    for step in (1.0, 0.5):
        improved = True
        while improved:
            improved = False
            for ds, dx, dy in ((0.002, 0, 0), (-0.002, 0, 0), (0, step, 0), (0, -step, 0), (0, 0, step), (0, 0, -step)):
                c = cost(s + ds, tx + dx, ty + dy)
                if c < best[0] - 1e-6:
                    best = (c, s + ds, tx + dx, ty + dy)
                    _, s, tx, ty = best
                    improved = True
    # centre-relative form -> absolute similarity
    T = Similarity(s, cx * init.s + init.tx + tx - s * cx, cy * init.s + init.ty + ty - s * cy)

    for _ in range(15):
        q = T.apply(src)
        j, d = _nearest(q, dst)
        keep = d < 6.0
        if ms is not None:
            wc = np.full(keep.sum(), 1.0 / max(keep.sum(), 1))
            wm = np.full(len(ms), 1.0 / len(ms))
            T2 = fit_points(np.vstack([src[keep], ms]), np.vstack([dst[j[keep]], md]), np.concatenate([wc, wm]))
        else:
            T2 = fit_points(src[keep], dst[j[keep]])
        if abs(T2.s - T.s) < 1e-6 and abs(T2.tx - T.tx) < 1e-3 and abs(T2.ty - T.ty) < 1e-3:
            T = T2
            break
        T = T2
    q = T.apply(src)
    _, d = _nearest(q, dst)
    inl = d < 6.0
    stats = {"contour_mean_px": round(float(d[inl].mean()), 2), "contour_p95_px": round(float(np.percentile(d[inl], 95)), 2),
             "contour_inliers": round(float(inl.mean()), 3), "contour_points": int(len(src))}
    return T, stats
