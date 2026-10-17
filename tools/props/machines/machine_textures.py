"""
Time Sorter (NOPE) - the desk machines' decal textures (chip reader, UV torch), drawn with Pillow.

Run with the system Python (not Blender's):   python machine_textures.py
Writes PNGs into Assets/Art/Office/Gameplay/Machines/ (next to the FBXs, which reference them by
relative path). The models' scripts call this themselves when a texture is missing.

The decals carry only the machines' own printing (brand plate, model, the pad's pictogram, the VFD's
idle frame): diegetic hardware markings, not UI labels. Palette: the run-7 UI kit (oxblood, slate,
signal red, brass, manila, bone, lavender, ink, card). Labels in BigShoulders (the kit's condensed
face) with a small katakana line, the kit's habit.
"""
import math
import os

from PIL import Image, ImageDraw, ImageFilter, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
OUT = os.path.join(REPO, "Assets", "Art", "Office", "Gameplay", "Machines")
KITFONT = os.path.join(REPO, "Assets", "Fonts", "Kit", "BigShoulders-Bold.ttf")

OXBLOOD, SLATE, SIGNAL, BRASS = (138, 47, 59), (74, 88, 114), (194, 58, 46), (212, 160, 85)
MANILA, BONE, LAVENDER, INK, CARD = (226, 201, 148), (238, 229, 208), (152, 134, 168), (43, 28, 36), (250, 242, 225)
GUNMETAL = (70, 76, 86)


def font(size, kit=True):
    for p in ([KITFONT] if kit else []) + [r"C:\Windows\Fonts\bahnschrift.ttf", r"C:\Windows\Fonts\arialbd.ttf"]:
        if os.path.exists(p):
            return ImageFont.truetype(p, size)
    return ImageFont.load_default()


def kana(size):
    for p in (r"C:\Windows\Fonts\YuGothB.ttc", r"C:\Windows\Fonts\msgothic.ttc"):
        if os.path.exists(p):
            return ImageFont.truetype(p, size)
    return None


def mono(size):
    for p in (r"C:\Windows\Fonts\OCRAEXT.TTF", r"C:\Windows\Fonts\consolab.ttf"):
        if os.path.exists(p):
            return ImageFont.truetype(p, size)
    return ImageFont.load_default()


def centred(d, box, text, f, fill):
    l, t, r, b = d.textbbox((0, 0), text, font=f)
    x0, y0, x1, y1 = box
    d.text(((x0 + x1 - (r - l)) / 2 - l, (y0 + y1 - (b - t)) / 2 - t), text, font=f, fill=fill)


def tcb_logo(d, x, y, s, fg, bg):
    """The Bureau mark: a rounded square with a time-gate arch cut in it and 'TCB' under it."""
    d.rounded_rectangle([x, y, x + s, y + s], radius=s // 7, fill=fg)
    a = s // 5
    d.pieslice([x + a, y + a, x + s - a, y + s - a // 2 + s // 3], 180, 360, fill=bg)
    d.rectangle([x + a, y + s // 2, x + s - a, y + s - a // 2], fill=bg)
    d.rectangle([x + s // 2 - s // 12, y + s // 2 - s // 10, x + s // 2 + s // 12, y + s - a // 2], fill=fg)


def vfd(path, lines, w=640, h=128, fg=(108, 255, 154), bg=(6, 26, 18)):
    """A green vacuum-fluorescent readout: dot-matrix glyphs, a soft bloom, faint scan grid."""
    base = Image.new("RGB", (w, h), bg)
    glow = Image.new("RGB", (w, h), (0, 0, 0))
    d = ImageDraw.Draw(glow)
    f = mono(int(h * 0.30))
    y = int(h * 0.10)
    for text in lines:
        d.text((int(w * 0.04), y), text, font=f, fill=fg)
        y += int(h * 0.42)
    # dot-matrix: keep only a grid of dots of the glyphs
    dots = Image.new("L", (w, h), 0)
    dd = ImageDraw.Draw(dots)
    step = 4
    for yy in range(0, h, step):
        for xx in range(0, w, step):
            dd.rectangle([xx, yy, xx + step - 2, yy + step - 2], fill=255)
    glyph = Image.composite(glow, Image.new("RGB", (w, h)), dots)
    soft = glow.filter(ImageFilter.GaussianBlur(4))
    out = Image.new("RGB", (w, h))
    pb, pg, ps, po = base.load(), glyph.load(), soft.load(), out.load()
    for yy in range(h):
        for xx in range(w):
            b, g, s = pb[xx, yy], pg[xx, yy], ps[xx, yy]
            po[xx, yy] = tuple(min(255, int(b[k] + g[k] + 0.55 * s[k])) for k in range(3))
    out.save(path)


def chip_badge(path, w=720, h=144):
    img = Image.new("RGB", (w, h), OXBLOOD)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([3, 3, w - 4, h - 4], radius=14, outline=BRASS, width=5)
    tcb_logo(d, 22, 20, 104, BONE, OXBLOOD)
    d.text((146, 10), "TCB  CHIP READER", font=font(64), fill=BONE)
    d.text((148, 84), "MODEL CR-11 · TEMPORAL CUSTOMS", font=font(30), fill=MANILA)
    k = kana(24)
    if k:
        d.text((572, 34), "チップリーダー", font=k, fill=MANILA)
    img.save(path)


def chip_pad(path, s=512):
    """The RF pad on the reader's top: a chip pictogram, three waves, and the instruction band."""
    img = Image.new("RGB", (s, s), (40, 36, 44))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([10, 10, s - 11, s - 11], radius=36, outline=LAVENDER, width=6)
    # the chip
    cx, cy, c = s // 2, int(s * 0.42), int(s * 0.17)
    d.rounded_rectangle([cx - c, cy - c, cx + c, cy + c], radius=18, fill=BRASS, outline=INK, width=5)
    for k in (-1, 0, 1):
        d.line([cx - c, cy + k * c // 2, cx + c, cy + k * c // 2], fill=INK, width=4)
    d.line([cx, cy - c, cx, cy + c], fill=INK, width=4)
    d.rounded_rectangle([cx - c // 3, cy - c // 3, cx + c // 3, cy + c // 3], radius=6, fill=MANILA, outline=INK, width=4)
    # the waves
    for i, r in enumerate((c + 34, c + 64, c + 94)):
        d.arc([cx - r, cy - r, cx + r, cy + r], 300, 60, fill=LAVENDER, width=8 - i)
        d.arc([cx - r, cy - r, cx + r, cy + r], 120, 240, fill=LAVENDER, width=8 - i)
    d.rounded_rectangle([40, int(s * 0.78), s - 40, int(s * 0.92)], radius=12, fill=SLATE)
    centred(d, (40, int(s * 0.78), s - 40, int(s * 0.92)), "PLACE PASSPORT", font(44), BONE)
    img.save(path)


def chip_slot_label(path, w=512, h=64):
    img = Image.new("RGB", (w, h), GUNMETAL)
    d = ImageDraw.Draw(img)
    # an arrow into the slot, the words beside it
    d.polygon([(26, 14), (58, 14), (42, 48)], fill=BRASS)
    d.text((76, 6), "INSERT CARD  \u00b7  CHIP UP", font=font(44), fill=BONE)
    img.save(path)


def uv_label(path, w=1024, h=512):
    """The torch's sticker (one on each side of the barrel, 2:1 like its arc): lavender, ink type running
    along the torch toward the lens, signal hazard ends."""
    img = Image.new("RGB", (w, h), LAVENDER)
    d = ImageDraw.Draw(img)
    stripe, edge = 34, 110
    for x0 in (0, w - edge):
        d.rectangle([x0, 0, x0 + edge, h], fill=INK)
        for i in range(-18, 12):
            x = x0 + i * stripe
            d.polygon([(x, h), (x + stripe // 2, h), (x + stripe // 2 + h // 2, 0), (x + h // 2, 0)], fill=SIGNAL)
    d.rectangle([edge, 0, w - edge, h], fill=LAVENDER)
    d.rectangle([edge, 0, w - edge, h - 1], outline=INK, width=6)
    tcb_logo(d, 146, 150, 180, INK, LAVENDER)
    d.text((360, 40), "UV-7", font=font(170), fill=INK)
    d.text((362, 238), "BLACKLIGHT", font=font(104), fill=INK)
    d.text((364, 372), "TCB · INSPECTION", font=font(48), fill=(64, 44, 70))
    k = kana(34)
    if k:
        d.text((366, 434), "ブラックライト", font=k, fill=INK)
    img.save(path)


def uv_cradle(path, w=512, h=128):
    img = Image.new("RGB", (w, h), SLATE)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([4, 4, w - 5, h - 5], radius=12, outline=BONE, width=4)
    centred(d, (0, 4, w, h * 0.66), "UV TORCH", font(64), BONE)
    centred(d, (0, h * 0.58, w, h - 8), "RETURN AFTER USE", font(30), MANILA)
    img.save(path)


def uv_lens(path, s=256):
    """The violet lens face: a reflector's rings round a bright LED cluster."""
    img = Image.new("RGB", (s, s), (60, 18, 120))
    d = ImageDraw.Draw(img)
    c = s // 2
    for i, r in enumerate(range(c, 8, -14)):
        v = 0.55 + 0.45 * (i % 2)
        d.ellipse([c - r, c - r, c + r, c + r], fill=(int(120 * v), int(60 * v), int(235 * v)))
    for a in range(6):
        x = c + int(math.cos(a * math.pi / 3) * s * 0.18)
        y = c + int(math.sin(a * math.pi / 3) * s * 0.18)
        d.ellipse([x - 13, y - 13, x + 13, y + 13], fill=(232, 210, 255))
    d.ellipse([c - 15, c - 15, c + 15, c + 15], fill=(250, 240, 255))
    img.filter(ImageFilter.GaussianBlur(1)).save(path)


TEXTURES = {
    "ChipReader_VFD.png": lambda p: vfd(p, ["TCB CR-11  READY", "INSERT CARD >"]),
    "ChipReader_Badge.png": chip_badge,
    "ChipReader_Pad.png": chip_pad,
    "ChipReader_SlotLabel.png": chip_slot_label,
    "UvTorch_Label.png": uv_label,
    "UvTorch_Cradle.png": uv_cradle,
    "UvTorch_Lens.png": uv_lens,
}


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, fn in TEXTURES.items():
        fn(os.path.join(OUT, name))
        print("texture", name)


if __name__ == "__main__":
    main()
