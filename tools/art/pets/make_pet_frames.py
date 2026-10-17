"""Pet animation frames (Saleh 2026-10-17: "just make normal animations"): classic frame-by-frame loops, a few drawn
frames per mood played at 10 fps, each differing from the mood's picture only where something really moves.

From a coat's picture (make_pets.py) it draws, per mood:
  rest     the picture as it is;
  breath   the chest and back risen a few pixels (a breath in), the head and the ground still;
  tail_a,  the tail drawn swung one way and the other (a wag), bending from its root;
  tail_b
  ear      one ear twitched;
  blink    the eyes shut (the coat's _blink picture laid over the picture), for the moods whose eyes are open.
Every frame keeps the picture's own line art and colours: a frame moves the drawn pixels of one part a little and
holds the rest of the body still (no scaling, squash or tween; the frames are held, never in-betweened). The parts are
the rigs' regions in Assets/Art/UI/Resources/Home/pet_rigs.json; each mood's loop (which frame, held how many frames)
and its part motions are LOOPS below.

Preview: python tools/art/pets/make_pet_frames.py --preview <folder> [--coats dog_tan,cat_ginger]
  writes <kind>_<coat>_<mood>.gif (the loop at 10 fps on the corner's ink), <kind>_<coat>_strip.png (each mood's
  drawn frames side by side, labelled) and <kind>_<coat>_<mood>_<frame>.png.
"""
import json
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import make_pets as mp  # noqa: E402

FPS = 10
INK = (43, 28, 36)

# Per mood: the motions (degrees for the tail and ear, picture heights for the breath) and the loop: (frame, held
# frames at 10 fps). The rest frame carries most of the time; motions are brief and small.
LOOPS = {
    "idle": dict(breath=0.0045, tail=7, ear=10, loop=[
        ("rest", 8), ("breath", 8), ("rest", 6), ("blink", 1), ("rest", 3), ("breath", 8), ("rest", 4), ("tail_a", 3),
        ("rest", 2), ("tail_b", 3), ("rest", 4), ("breath", 8), ("rest", 3), ("ear", 2), ("rest", 5)]),
    "happy": dict(breath=0.006, tail=12, ear=10, loop=[
        ("tail_a", 2), ("tail_b", 2), ("tail_a", 2), ("tail_b", 2), ("breath", 2), ("tail_b", 2), ("tail_a", 2),
        ("tail_b", 2), ("tail_a", 2), ("rest", 2), ("ear", 2), ("tail_a", 2), ("tail_b", 2), ("tail_a", 2), ("tail_b", 2)]),
    "sad": dict(breath=0.004, tail=4, ear=6, loop=[
        ("rest", 12), ("breath", 12), ("rest", 8), ("blink", 3), ("rest", 6), ("breath", 12), ("rest", 10), ("tail_a", 4),
        ("rest", 6)]),
    "sick": dict(breath=0.007, tail=3, ear=0, loop=[
        ("rest", 7), ("breath", 7), ("rest", 7), ("breath", 7), ("blink", 4), ("rest", 5), ("breath", 7), ("rest", 7),
        ("breath", 7), ("rest", 4)]),
}

# A cat's tail lies against its body (wrapped round the paws, or up its side): it moves half as far as a dog's.
TAIL_SCALE = {"dog": 1.0, "cat": 0.5}

SIZE = mp.SIZE
Y, X = np.mgrid[0:SIZE, 0:SIZE].astype(np.float32) / SIZE


def weight(bone, softness=0.6):
    """A bone's coverage: 1 inside softness of its ellipse, easing to 0 at its edge."""
    if not bone or not bone.get("radius"):
        return np.zeros((SIZE, SIZE), np.float32)
    d = np.sqrt(((X - bone["center"][0]) / bone["radius"][0]) ** 2 + ((Y - bone["center"][1]) / bone["radius"][1]) ** 2)
    return 1 - mp.smoothstep(softness, 1.0, d)


def warp(rgba: np.ndarray, sx: np.ndarray, sy: np.ndarray) -> np.ndarray:
    """The picture drawn with each pixel taken from (sx, sy) (0..1 coordinates), premultiplied so edges stay clean."""
    a = rgba[..., 3:4] / 255.0
    pre = np.concatenate([rgba[..., :3] * a, a * 255.0], axis=-1)
    coords = [sy * SIZE - 0.5, sx * SIZE - 0.5]
    out = np.stack([ndimage.map_coordinates(pre[..., c], coords, order=1, mode="constant") for c in range(4)], -1)
    alpha = out[..., 3:4] / 255.0
    rgb = np.where(alpha > 1e-4, out[..., :3] / np.maximum(alpha, 1e-4), 0)
    return np.concatenate([np.clip(rgb, 0, 255), out[..., 3:4]], -1)


def turned(rgba, bone, degrees, bend=True):
    """The part under <bone> drawn turned by <degrees> about its pivot (bending more towards its tip when <bend>)."""
    if not bone or not bone.get("radius") or degrees == 0:
        return rgba
    w = weight(bone)
    if bend and bone.get("tip"):
        length = math.dist(bone["pivot"], bone["tip"])
        w = w * np.clip(np.sqrt((X - bone["pivot"][0]) ** 2 + (Y - bone["pivot"][1]) ** 2) / length, 0, 1)
    a = -np.radians(degrees) * w  # where each drawn pixel came from: turned back
    cx, cy = bone["pivot"]
    dx, dy = X - cx, Y - cy
    return warp(rgba, cx + dx * np.cos(a) - dy * np.sin(a), cy + dx * np.sin(a) + dy * np.cos(a))


def breathed(rgba, rig, amount):
    """The chest and back risen by up to <amount> (a breath in): the body's fur moves up the more the higher it sits
    above the ground, the head and the ground still."""
    body = rig["body"]
    ground = body["pivot"][1]
    top = body["center"][1] - body["radius"][1]
    rise = np.clip((ground - Y) / max(1e-3, ground - top), 0, 1)
    w = weight(body, 0.5) * rise * (1 - weight(rig["head"], 0.5))
    return warp(rgba, X, Y + amount * w)


def frames_for(kind, coat, mood, rig, painted_rgba, blink_rgba):
    """The mood's drawn frames: name -> RGBA float picture."""
    m = LOOPS[mood]
    out = {"rest": painted_rgba}
    out["breath"] = breathed(painted_rgba, rig, m["breath"])
    if rig.get("tail") and m["tail"]:
        swing = m["tail"] * TAIL_SCALE[kind]
        out["tail_a"] = turned(painted_rgba, rig["tail"], swing)
        out["tail_b"] = turned(painted_rgba, rig["tail"], -swing)
    if rig.get("ears") and m["ear"]:
        out["ear"] = turned(painted_rgba, rig["ears"][0], m["ear"], bend=False)
    if blink_rgba is not None:
        base = painted_rgba.copy()
        a = blink_rgba[..., 3:4] / 255.0
        base[..., :3] = base[..., :3] * (1 - a) + blink_rgba[..., :3] * a
        out["blink"] = base
    return out


def coat_picture(kind, coat, mood, rig, colours, mi):
    src = np.asarray(Image.open(os.path.join(mp.SRC, f"pet_{kind}_{mood}.png")).convert("RGB")).astype(np.float32)
    alpha = mp.key(src)
    rgb = mp.despill(src, alpha)
    seed = 1000 * (kind == "cat") + 10 * mi
    painted, fur = mp.paint(rgb, rig, colours, seed, mp.STOPS[kind])
    rgba = np.dstack([np.clip(painted, 0, 255), alpha * 255]).astype(np.float32)
    blink = None
    if rig["eyes"]:
        b = mp.blink(painted, fur, rig, np.array(mp.INK, np.float32) * mp.line_darkening(colours))
        b[..., 3] *= alpha * 255
        blink = b
    return rgba, blink


def on_ink(rgba, size):
    img = Image.fromarray(np.clip(rgba, 0, 255).astype(np.uint8), "RGBA").resize((size, size), Image.LANCZOS)
    bg = Image.new("RGBA", (size, size), INK + (255,))
    bg.alpha_composite(img)
    return bg.convert("RGB")


def preview(folder, coats):
    os.makedirs(folder, exist_ok=True)
    rigs = {(r["kind"].lower(), r["mood"].lower()): r for r in json.load(open(mp.RIGS, encoding="utf-8"))["rigs"]}
    for kc in coats:
        kind, coat = kc.split("_")
        colours = mp.COATS[kind][coat]
        strip_rows = []
        for mi, mood in enumerate(mp.MOODS):
            rig = rigs[(kind, mood)]
            rgba, blink = coat_picture(kind, coat, mood, rig, colours, mi)
            drawn = frames_for(kind, coat, mood, rig, rgba, blink)
            small = {name: on_ink(f, 384) for name, f in drawn.items()}
            for name, img in small.items():
                img.save(os.path.join(folder, f"{kind}_{coat}_{mood}_{name}.png"))
            seq, durs = [], []
            for name, hold in LOOPS[mood]["loop"]:
                if name not in small:
                    name = "rest"
                if seq and seq[-1] is small[name]:
                    durs[-1] += hold * 1000 // FPS
                else:
                    seq.append(small[name])
                    durs.append(hold * 1000 // FPS)
            seq[0].save(os.path.join(folder, f"{kind}_{coat}_{mood}.gif"), save_all=True, append_images=seq[1:], duration=durs, loop=0)
            strip_rows.append((mood, small))
            print("frames", kind, coat, mood, sorted(small), flush=True)
        cell = 256
        cols = max(len(s) for _, s in strip_rows)
        sheet = Image.new("RGB", (cell * cols, (cell + 20) * len(strip_rows)), INK)
        d = ImageDraw.Draw(sheet)
        order = ["rest", "breath", "tail_a", "tail_b", "ear", "blink"]
        for r, (mood, small) in enumerate(strip_rows):
            for c, name in enumerate(n for n in order if n in small):
                sheet.paste(small[name].resize((cell, cell), Image.LANCZOS), (c * cell, r * (cell + 20) + 20))
                d.text((c * cell + 4, r * (cell + 20) + 4), f"{mood} / {name}", fill=(238, 229, 208))
        sheet.save(os.path.join(folder, f"{kind}_{coat}_strip.png"))


if __name__ == "__main__":
    if "--preview" in sys.argv:
        coats = sys.argv[sys.argv.index("--coats") + 1].split(",") if "--coats" in sys.argv else ["dog_tan", "cat_ginger"]
        preview(sys.argv[sys.argv.index("--preview") + 1], coats)
