"""Pet animation frames (Saleh 2026-10-17: "just make normal animations"; approved: "Yes, do all moods"): the drawn
frames of the pets' frame-by-frame loops, in every coat.

The drawn frames are made in Canva (generate-image, the mood's white picture as the reference, asked to change one
thing only: the eyes shut, the tail swung, an ear twitched) and exported at 1264 px into
src/frames/raw/pet_<kind>_<mood>_<frame>.png. compose() lays each over the mood's own picture only where it really
differs from it (the changed fur and its rim, feathered by 3 px), so the line art and the still body stay the
picture's own and every frame of a loop lines up exactly: src/frames/pet_<kind>_<mood>_<frame>.png (white, on the
magenta key, like src/). Each coat then paints a frame as make_pets.py paints the picture, with the frame's marks
where a part moved: a rig's "frames" (tools/art/pets/pet_rigs.json) replace the marks of that part, else a mark of
the tail ("part": "tail") moves to the swung tail's tip (its fur farthest from the tail's pivot).

The frames made are the ones the game plays: Assets/Art/UI/Resources/Home/pet_loops.json (each mood's loop and the
reactions; "rest" is the picture itself), for the frames whose drawing exists.
Output: Assets/Art/UI/Resources/Home/pet_<kind>_<coat>_<mood>_<frame>.png (1024x1024, transparent;
ArtSlots.PetFrame), with fixed-GUID metas like make_pets.py's.

Usage: python tools/art/pets/make_pet_frames.py [--only dog_idle] [--preview <folder> [--coats tan,ginger]]
  --preview writes, instead of installing, <kind>_<coat>_<mood>.gif (the loop at its fps on the corner's ink) and
  <kind>_<mood>_strip.png for the given coats.
"""
import json
import os
import sys

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import make_pets as mp  # noqa: E402

INK = (43, 28, 36)
RAW = os.path.join(mp.SRC, "frames", "raw")
FRAMES = os.path.join(mp.SRC, "frames")
LOOPS = os.path.join(mp.OUT, "pet_loops.json")
DIFF = 60  # a channel this far from the picture is a changed pixel
MIN_PATCH = 1500  # changed patches smaller than this (in px, after a 6 px grow) are the generator's noise


def compose(kind, mood, frame):
    """The drawn frame over the mood's picture where it differs from it, and the changed area; saved white on the key."""
    src = np.asarray(Image.open(os.path.join(mp.SRC, f"pet_{kind}_{mood}.png")).convert("RGB")).astype(np.float32)
    raw = Image.open(os.path.join(RAW, f"pet_{kind}_{mood}_{frame}.png")).convert("RGB").resize(src.shape[1::-1], Image.LANCZOS)
    g = np.asarray(raw).astype(np.float32)
    # The generator gives a moved part a soft drop shadow, a darker magenta the key would keep half opaque: inside a
    # magenta-hued area (blue and red well over green), clear of the line art's anti-aliased rim, it is the background.
    hue = (np.minimum(g[..., 0], g[..., 2]) - g[..., 1]) / np.maximum(g.max(-1), 1)
    shadow = ndimage.binary_erosion(hue > 0.55, iterations=2)
    g[shadow] = (255, 0, 255)
    changed = ndimage.binary_opening(np.abs(src - g).max(-1) > DIFF, iterations=1)
    lab, n = ndimage.label(ndimage.binary_dilation(changed, iterations=6))
    sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
    keep = np.isin(lab, 1 + np.flatnonzero(sizes > MIN_PATCH))
    keep = ndimage.binary_fill_holes(ndimage.binary_dilation(keep, iterations=8))
    soft = ndimage.gaussian_filter(keep.astype(np.float32), 3)[..., None]
    out = src * (1 - soft) + g * soft
    os.makedirs(FRAMES, exist_ok=True)
    Image.fromarray(np.clip(out, 0, 255).astype(np.uint8)).save(os.path.join(FRAMES, f"pet_{kind}_{mood}_{frame}.png"))
    return out, keep


def rig_for_frame(rig, frame, white, changed):
    """The rig with the frame's marks in place of the marks of the parts that moved (the rig's own "frames", else the
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


def paint_frame(kind, mood, frame, white, rig, coat, mi, changed=None):
    alpha = mp.key(white)
    rgb = mp.despill(white, alpha)
    seed = 1000 * (kind == "cat") + 10 * mi
    frame_rig = rig if frame == "rest" else rig_for_frame(rig, frame, white, changed)
    out, _ = mp.paint(rgb, frame_rig, mp.COATS[kind][coat], seed, mp.STOPS[kind])
    return np.dstack([np.clip(out, 0, 255), alpha * 255])


def plan():
    """(kind, mood) -> (its loop, the frames to make: every frame its loop and the reactions name that is drawn, fps)."""
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
        out[(kind, mood)] = (loop["frames"], drawn, data["fps"])
    return out


def on_ink(rgba, size):
    img = Image.fromarray(rgba.astype(np.uint8), "RGBA").resize((size, size), Image.LANCZOS)
    bg = Image.new("RGBA", (size, size), INK + (255,))
    bg.alpha_composite(img)
    return bg.convert("RGB")


def main():
    only = sys.argv[sys.argv.index("--only") + 1] if "--only" in sys.argv else None
    preview = sys.argv[sys.argv.index("--preview") + 1] if "--preview" in sys.argv else None
    rigs = {(r["kind"].lower(), r["mood"].lower()): r for r in json.load(open(mp.RIGS, encoding="utf-8"))["rigs"]}
    for (kind, mood), (loop, drawn, fps) in plan().items():
        if only and only != f"{kind}_{mood}":
            continue
        mi = mp.MOODS.index(mood)
        rig = rigs[(kind, mood)]
        white = {"rest": np.asarray(Image.open(os.path.join(mp.SRC, f"pet_{kind}_{mood}.png")).convert("RGB")).astype(np.float32)}
        changed = {}
        for f in drawn:
            white[f], changed[f] = compose(kind, mood, f)
        if preview:
            coats = sys.argv[sys.argv.index("--coats") + 1].split(",") if "--coats" in sys.argv else list(mp.COATS[kind])[:2]
            os.makedirs(preview, exist_ok=True)
            rows = []
            for coat in (c for c in coats if c in mp.COATS[kind]):
                small = {f: on_ink(paint_frame(kind, mood, f, white[f], rig, coat, mi, changed.get(f)), 512) for f in ["rest"] + drawn}
                seq, durs = [], []
                for step in loop:
                    img = small.get(step["frame"], small["rest"])
                    if seq and seq[-1] is img:
                        durs[-1] += step["hold"] * 1000 // fps
                    else:
                        seq.append(img)
                        durs.append(step["hold"] * 1000 // fps)
                seq[0].save(os.path.join(preview, f"{kind}_{coat}_{mood}.gif"), save_all=True, append_images=seq[1:], duration=durs, loop=0)
                rows.append((coat, small))
            cell = 300
            names = ["rest"] + drawn
            sheet = Image.new("RGB", (cell * len(names), (cell + 22) * len(rows)), INK)
            d = ImageDraw.Draw(sheet)
            for r, (coat, small) in enumerate(rows):
                for c, f in enumerate(names):
                    sheet.paste(small[f].resize((cell, cell), Image.LANCZOS), (c * cell, r * (cell + 22) + 22))
                    d.text((c * cell + 4, r * (cell + 22) + 5), f"{kind} {coat} {mood} / {f}", fill=(238, 229, 208))
            sheet.save(os.path.join(preview, f"{kind}_{mood}_strip.png"))
        else:
            for coat in mp.COATS[kind]:
                for f in drawn:
                    rgba = paint_frame(kind, mood, f, white[f], rig, coat, mi, changed[f])
                    mp.save(Image.fromarray(rgba.astype(np.uint8), "RGBA"), f"pet_{kind}_{coat}_{mood}_{f}.png")
        print("frames", kind, mood, drawn, flush=True)


if __name__ == "__main__":
    main()
