"""Pet animation frames (Saleh 2026-10-17: "just make normal animations"; approved: "Yes, do all moods"): the drawn
frames of the pets' frame-by-frame loops, in every coat.

The drawn frames are made in Canva (generate-image, the mood's white picture as the reference, asked to change one
thing only: the eyes shut, the tail swung, an ear twitched) and exported at their native 1264 px into
src/frames/raw/pet_<kind>_<mood>_<frame>.png. Since 2026-10-18 (Saleh on demo 1018a: "Cat tail animation is wrong") a
cat's tail frames swing the tail from its base: the base stays where it leaves the haunch, the tail keeps its length
and only its tip end moves (idle: the tip lifts and curls; sad: a small tired flick of the tip; happy: the raised tail
sways about its base, then stands taller).

compose() lays each drawing over the mood's own white picture only where it really differs from it (the changed fur
and its rim, feathered), so the line art and the still body stay the picture's own and every frame of a loop lines up
exactly: src/frames/pet_<kind>_<mood>_<frame>.png (white, on the magenta key, like src/). The changed area is the
union of where the part was and where it is, so nothing of the part is left behind.

Each coat then makes the frame over its own picture (make_pets.py), changing that same area only:
- a dog coat paints the white frame as make_pets.py paints the picture, with the frame's marks where a part moved: a
  rig's "frames" (tools/art/pets/pet_rigs.json) replace the marks of that part, else a mark of the tail
  ("part": "tail") moves to the swung tail's tip (its fur farthest from the tail's pivot);
- a drawn cat coat takes its own Canva drawing of the frame where there is one (src/coats/frames/
  pet_cat_<coat>_<mood>_<frame>.png, made from the white frame alone with the coat's description, as its picture was:
  the tail frames, so a tabby's tail keeps its rings), moved onto its picture (the shift that lines up their still
  bodies) and its colours matched to the picture's fur around the area (per channel, by quantiles); otherwise (a blink,
  an ear) the white frame's fur takes the coat's colour for its tone, read off the picture's fur around the area;
- the white cat is the white frame.

The frames made are the ones the game plays: Assets/Art/UI/Resources/Home/pet_loops.json (each mood's loop and the
reactions; "rest" is the picture itself), for the frames whose drawing exists.
Output: Assets/Art/UI/Resources/Home/pet_<kind>_<coat>_<mood>_<frame>.png (1024x1024, transparent;
ArtSlots.PetFrame), with fixed-GUID metas like make_pets.py's.

Usage: python tools/art/pets/make_pet_frames.py [--only cat_idle] [--coats ginger,black]
"""
import json
import os
import sys

import cv2
import numpy as np
from PIL import Image
from scipy import ndimage

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import make_pets as mp  # noqa: E402

RAW = os.path.join(mp.SRC, "frames", "raw")
FRAMES = os.path.join(mp.SRC, "frames")
LOOPS = os.path.join(mp.OUT, "pet_loops.json")
DIFF = 60  # a channel this far from the picture is a changed pixel
MIN_PATCH = int(1500 * mp.SCALE * mp.SCALE)  # changed patches smaller than this (after a grow) are the generator's noise
GROW = round(6 * mp.SCALE)  # the grow that joins a changed part's pieces
RIM = round(8 * mp.SCALE)  # the changed area's rim past the part
FEATHER = 3 * mp.SCALE  # the changed area's soft edge (a gaussian's sigma)
AROUND = round(90 * mp.SCALE)  # how far around the changed area a coat's fur is read for its colours


def compose(kind, mood, frame):
    """The drawn frame over the mood's white picture where it differs from it: (the frame, the changed area, its soft
    edge); the frame is saved white on the key."""
    src = mp.load(mp.white_path(kind, mood))
    g = mp.load(os.path.join(RAW, f"pet_{kind}_{mood}_{frame}.png"))
    # The generator gives a moved part a soft drop shadow, a darker magenta the key would keep half opaque: inside a
    # magenta-hued area (blue and red well over green), clear of the line art's anti-aliased rim, it is the background.
    hue = (np.minimum(g[..., 0], g[..., 2]) - g[..., 1]) / np.maximum(g.max(-1), 1)
    g[ndimage.binary_erosion(hue > 0.55, iterations=2)] = (255, 0, 255)
    changed = ndimage.binary_opening(np.abs(src - g).max(-1) > DIFF, iterations=1)
    lab, n = ndimage.label(ndimage.binary_dilation(changed, iterations=GROW))
    sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
    keep = np.isin(lab, 1 + np.flatnonzero(sizes > MIN_PATCH))
    keep = ndimage.binary_fill_holes(ndimage.binary_dilation(keep, iterations=RIM))
    soft = ndimage.gaussian_filter(keep.astype(np.float32), FEATHER)[..., None]
    out = src * (1 - soft) + g * soft
    os.makedirs(FRAMES, exist_ok=True)
    Image.fromarray(np.clip(out, 0, 255).astype(np.uint8)).save(os.path.join(FRAMES, f"pet_{kind}_{mood}_{frame}.png"))
    return out, keep, soft


def rig_for_frame(rig, frame, white, changed):
    """A dog rig with the frame's marks in place of the marks of the parts that moved (the rig's own "frames", else the
    tail's marks moved to the swung tail's tip)."""
    override = next((f for f in rig.get("frames", []) if f["frame"] == frame), None)
    if override:
        parts = {m.get("part") for m in override["marks"]}
        return dict(rig, marks=[m for m in rig["marks"] if m.get("part") not in parts] + override["marks"])
    tail_marks = [m for m in rig["marks"] if m.get("part") == "tail"]
    if not frame.startswith("tail") or not tail_marks or not rig.get("tail"):
        return rig
    fur = mp.key(white) > 0.5
    ys, xs = np.nonzero(changed & fur)
    if len(xs) == 0:
        return rig
    px, py = rig["tail"]["pivot"]
    d = (xs / mp.SIZE - px) ** 2 + (ys / mp.SIZE - py) ** 2
    far = d >= np.percentile(d, 97)
    tip = [float(xs[far].mean() / mp.SIZE), float(ys[far].mean() / mp.SIZE)]
    moved = [dict(m, center=tip) for m in tail_marks]
    return dict(rig, marks=[m for m in rig["marks"] if m.get("part") != "tail"] + moved)


def around(keep, alpha):
    """The opaque pixels around the changed area (within AROUND of it, off it): where a coat's colours are read."""
    near = cv2.dilate(keep.astype(np.uint8), cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (2 * AROUND + 1,) * 2)) > 0
    off = cv2.dilate(keep.astype(np.uint8), np.ones((9, 9), np.uint8)) == 0
    return near & off & (alpha > 0.95)


def aligned(drawing, picture, keep):
    """The coat's frame drawing moved onto its picture: the sub-pixel shift that lines up their still bodies (the
    changed area masked out of both)."""
    a = picture @ mp.LUMA
    b = drawing @ mp.LUMA
    b = np.where(ndimage.binary_dilation(keep, iterations=RIM), a, b)
    window = cv2.createHanningWindow(a.shape[::-1], cv2.CV_32F)
    (dx, dy), _ = cv2.phaseCorrelate(a.astype(np.float32), b.astype(np.float32), window)
    m = np.float32([[1, 0, -dx], [0, 1, -dy]])
    return cv2.warpAffine(drawing, m, a.shape[::-1], flags=cv2.INTER_CUBIC, borderMode=cv2.BORDER_REPLICATE)


def matched(rgb, rgb_alpha, picture, alpha, keep):
    """The drawing's colours matched to the picture's around the changed area, channel by channel by quantiles (the
    two drawings' tones agree; their stripes need not lie in the same places)."""
    ring = around(keep, alpha) & (rgb_alpha > 0.95)
    out = rgb.copy()
    q = np.linspace(0, 100, 101)
    for c in range(3):
        src_q = np.percentile(rgb[..., c][ring], q)
        dst_q = np.percentile(picture[..., c][ring], q)
        out[..., c] = np.interp(rgb[..., c], src_q, dst_q)
    return out


def recoloured(white_frame, white_rest, picture, alpha, keep, eyes=True):
    """The white frame's fur in the coat's colours: each tone of the white fur takes the coat's median colour for that
    tone around the changed area (its lines, eyes and inner ears as drawn; without eyes, a small pale island is fur
    too, not an eye's highlight)."""
    rest_fur, rest_lum = mp.fur(white_rest)
    sel = around(keep, alpha) & (rest_fur > 0.9)
    edges = np.linspace(0.4, 1.0, 25)
    centres = (edges[:-1] + edges[1:]) / 2
    which = np.digitize(rest_lum[sel], edges) - 1
    colours = picture[sel]
    table, have = [], []
    for i in range(len(centres)):
        pick = which == i
        if pick.sum() >= 20:
            table.append(np.median(colours[pick], axis=0))
            have.append(centres[i])
    table = np.array(table)
    frame_fur, frame_lum = mp.fur(white_frame) if eyes else mp.coat_mask(white_frame)
    tone = np.stack([np.interp(frame_lum, have, table[:, c]) for c in range(3)], axis=-1)
    return white_frame * (1 - frame_fur[..., None]) + tone * frame_fur[..., None]


def coat_frame(kind, coat, mood, frame, white, keep, soft, rig):
    """The coat's frame at SIZE: (RGB, alpha)."""
    white_rgb, white_alpha = mp.keyed(white.copy())
    if kind == "dog":
        return mp.paint(white_rgb, rig_for_frame(rig, frame, white, keep), mp.COATS[kind][coat], mp.seed(kind, mood)), white_alpha
    if not mp.is_drawn(kind, coat):
        return white_rgb, white_alpha
    picture, alpha = mp.picture(kind, coat, mood, None)
    white_rest, _ = mp.keyed(mp.load(mp.white_path(kind, mood)))
    path = mp.drawn_path(coat, mood, frame)
    if os.path.exists(path):
        raw = mp.load(mp.drawn_path(coat, mood))
        drawing, drawing_alpha = mp.keyed(aligned(mp.load(path), raw, keep))
        content, content_alpha = matched(drawing, drawing_alpha, picture, alpha, keep), drawing_alpha
        # A drawing can leave a scrap of the white fur unpainted (a toe peeking under the swung tail): on a coat far
        # from white, white fur left in the drawing takes the coat's tone for its own (a tail frame has no eye).
        if np.median(picture[around(keep, alpha)] @ mp.LUMA) < 0.7 * 255:
            missed = keep & (mp.coat_mask(content)[0] > 0.5)
            m = ndimage.gaussian_filter(missed.astype(np.float32), 1.0)[..., None]
            content = content * (1 - m) + recoloured(content, white_rest, picture, alpha, keep, eyes=False) * m
    else:
        content, content_alpha = recoloured(white_rgb, white_rest, picture, alpha, keep), white_alpha
    s = soft[..., 0]
    return picture * (1 - soft) + content * soft, alpha * (1 - s) + content_alpha * s


def plan():
    """(kind, mood) -> (its loop, the frames to make: every frame its loop and the reactions name that is drawn)."""
    data = json.load(open(LOOPS, encoding="utf-8"))
    reacted = {f["frame"] for r in data["reactions"] for f in r["frames"]}
    out = {}
    for loop in data["loops"]:
        kind, mood = loop["kind"].lower(), loop["mood"].lower()
        names = {f["frame"] for f in loop["frames"]} | reacted
        drawn = sorted(n for n in names if n != "rest" and os.path.exists(os.path.join(RAW, f"pet_{kind}_{mood}_{n}.png")))
        missing = sorted(n for n in {f["frame"] for f in loop["frames"]} if n != "rest" and n not in drawn)
        if missing:
            raise SystemExit(f"{kind} {mood}: the loop names frames with no drawing: {missing}")
        out[(kind, mood)] = drawn
    return out


def main():
    only = sys.argv[sys.argv.index("--only") + 1] if "--only" in sys.argv else None
    rigs = mp.load_rigs()
    for (kind, mood), drawn in plan().items():
        if only and only != f"{kind}_{mood}":
            continue
        coats = list(mp.COATS[kind])
        if "--coats" in sys.argv:
            coats = [c for c in sys.argv[sys.argv.index("--coats") + 1].split(",") if c in mp.COATS[kind]]
        for f in drawn:
            white, keep, soft = compose(kind, mood, f)
            for coat in coats:
                rgb, alpha = coat_frame(kind, coat, mood, f, white, keep, soft, rigs.get((kind, mood)))
                mp.save(mp.finish(rgb, alpha), f"pet_{kind}_{coat}_{mood}_{f}.png")
        print("frames", kind, mood, drawn, flush=True)


if __name__ == "__main__":
    main()
