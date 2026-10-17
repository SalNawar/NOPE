"""Pet animation frames, v3 (Saleh 2026-10-17: "just make normal animations"; v2's moved pixels read as "warped
cut-outs"): frame-by-frame loops of properly drawn frames, played at 10 fps, each held, never in-betweened.

The drawn frames are made in Canva (generate-image, the mood's white picture as the reference, asked to change one
thing only: the eyes shut, the tail swung up, the tail swept low ...) and exported at 1264 px into
src/frames/raw/pet_<kind>_<mood>_<frame>.png. compose() lays each one over the mood's own picture only where it
really differs from it (the changed fur and its rim, feathered by 3 px), so the line art and the still body stay the
picture's own and every frame of a loop lines up exactly; the result is src/frames/pet_<kind>_<mood>_<frame>.png
(white, on the magenta key, like src/). Each coat then paints a frame as it paints the picture (make_pets.paint),
with the frame's own marks where a part moved (pet_rigs.json: a rig's "frames" replace the marks of that part, e.g.
the tail tip's pale fur follows the tail).

LOOPS holds each mood's loop: (frame, held frames at 10 fps); "rest" is the mood's picture.

Preview: python tools/art/pets/make_pet_frames.py --preview <folder> [--only dog_idle] [--coats cream,tan]
  composes the frames, then writes <kind>_<coat>_<mood>.gif and <kind>_<mood>_strip.png (each coat's frames).
"""
import json
import os
import sys

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import make_pets as mp  # noqa: E402

FPS = 10
INK = (43, 28, 36)
RAW = os.path.join(mp.SRC, "frames", "raw")
FRAMES = os.path.join(mp.SRC, "frames")
DIFF = 60  # a channel this far from the picture is a changed pixel
MIN_PATCH = 1500  # changed patches smaller than this (in px, after a 6 px grow) are the generator's noise

# The loops, (frame, held frames at 10 fps): the rest frame carries most of the time; a wag is up, rest, low, rest.
LOOPS = {
    ("dog", "idle"): [
        ("rest", 10), ("tail_up", 2), ("rest", 1), ("tail_low", 2), ("rest", 1), ("tail_up", 2), ("rest", 1),
        ("tail_low", 2), ("rest", 8), ("blink", 1), ("rest", 12), ("tail_up", 2), ("rest", 1), ("tail_low", 2),
        ("rest", 6)],
}


def compose(kind, mood, frame):
    """The drawn frame over the mood's picture where it differs from it; saved white on the key, and returned."""
    src = np.asarray(Image.open(os.path.join(mp.SRC, f"pet_{kind}_{mood}.png")).convert("RGB")).astype(np.float32)
    raw = Image.open(os.path.join(RAW, f"pet_{kind}_{mood}_{frame}.png")).convert("RGB").resize(src.shape[1::-1], Image.LANCZOS)
    g = np.asarray(raw).astype(np.float32)
    changed = ndimage.binary_opening(np.abs(src - g).max(-1) > DIFF, iterations=1)
    lab, n = ndimage.label(ndimage.binary_dilation(changed, iterations=6))
    sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
    keep = np.isin(lab, 1 + np.flatnonzero(sizes > MIN_PATCH))
    keep = ndimage.binary_fill_holes(ndimage.binary_dilation(keep, iterations=8))
    soft = ndimage.gaussian_filter(keep.astype(np.float32), 3)[..., None]
    out = src * (1 - soft) + g * soft
    os.makedirs(FRAMES, exist_ok=True)
    Image.fromarray(np.clip(out, 0, 255).astype(np.uint8)).save(os.path.join(FRAMES, f"pet_{kind}_{mood}_{frame}.png"))
    return out


def rig_for_frame(rig, frame):
    """The rig with the frame's marks in place of the marks of the parts that moved."""
    override = next((f for f in rig.get("frames", []) if f["frame"] == frame), None)
    if not override:
        return rig
    parts = {m.get("part") for m in override["marks"]}
    marks = [m for m in rig["marks"] if m.get("part") not in parts] + override["marks"]
    return dict(rig, marks=marks)


def painted(kind, mood, frame, white_rgb, rig, coat, mi):
    alpha = mp.key(white_rgb)
    rgb = mp.despill(white_rgb, alpha)
    seed = 1000 * (kind == "cat") + 10 * mi
    out, _ = mp.paint(rgb, rig_for_frame(rig, frame), mp.COATS[kind][coat], seed, mp.STOPS[kind])
    return np.dstack([np.clip(out, 0, 255), alpha * 255])


def on_ink(rgba, size):
    img = Image.fromarray(rgba.astype(np.uint8), "RGBA").resize((size, size), Image.LANCZOS)
    bg = Image.new("RGBA", (size, size), INK + (255,))
    bg.alpha_composite(img)
    return bg.convert("RGB")


def preview(folder, only, coats):
    os.makedirs(folder, exist_ok=True)
    rigs = {(r["kind"].lower(), r["mood"].lower()): r for r in json.load(open(mp.RIGS, encoding="utf-8"))["rigs"]}
    for (kind, mood), loop in LOOPS.items():
        if only and only != f"{kind}_{mood}":
            continue
        mi = mp.MOODS.index(mood)
        names = ["rest"] + sorted({f for f, _ in loop if f != "rest"})
        white = {"rest": np.asarray(Image.open(os.path.join(mp.SRC, f"pet_{kind}_{mood}.png")).convert("RGB")).astype(np.float32)}
        for f in names[1:]:
            white[f] = compose(kind, mood, f)
        rows = []
        for coat in coats:
            small = {f: on_ink(painted(kind, mood, f, white[f], rigs[(kind, mood)], coat, mi), 512) for f in names}
            seq, durs = [], []
            for f, hold in loop:
                if seq and seq[-1] is small[f]:
                    durs[-1] += hold * 1000 // FPS
                else:
                    seq.append(small[f])
                    durs.append(hold * 1000 // FPS)
            seq[0].save(os.path.join(folder, f"{kind}_{coat}_{mood}.gif"), save_all=True, append_images=seq[1:], duration=durs, loop=0)
            rows.append((coat, small))
            print("loop", kind, coat, mood, names, flush=True)
        cell = 320
        sheet = Image.new("RGB", (cell * len(names), (cell + 22) * len(rows)), INK)
        d = ImageDraw.Draw(sheet)
        for r, (coat, small) in enumerate(rows):
            for c, f in enumerate(names):
                sheet.paste(small[f].resize((cell, cell), Image.LANCZOS), (c * cell, r * (cell + 22) + 22))
                d.text((c * cell + 4, r * (cell + 22) + 5), f"{kind} {coat} {mood} / {f}", fill=(238, 229, 208))
        sheet.save(os.path.join(folder, f"{kind}_{mood}_strip.png"))


if __name__ == "__main__":
    if "--preview" in sys.argv:
        only = sys.argv[sys.argv.index("--only") + 1] if "--only" in sys.argv else None
        coats = sys.argv[sys.argv.index("--coats") + 1].split(",") if "--coats" in sys.argv else ["cream", "tan"]
        preview(sys.argv[sys.argv.index("--preview") + 1], only, coats)
