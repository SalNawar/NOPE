"""
Time Sorter (NOPE) - the UV torch's reveal textures (the desk machines spec §1), drawn with Pillow.

Run with the system Python:   python uv_textures.py
Writes RGBA PNGs into Assets/Art/Office/Gameplay/Textures/: white marks on transparent, tinted violet and
masked to the torch's cone by the TimeDesk/UvReveal shader.
    UvWatermark.png   the Bureau's watermark: its mark over a tiled band of TEMPORAL CUSTOMS (a genuine paper's)
    UvMicroprint.png  a seal's microprint ring: tiny repeated letters round a circle (a genuine seal's)
    UvTick.png        a found mark's tick: a ring with an eye, left on a box once its fault is seen
    UvPool.png        the cone's pool of light on the desk: a soft disc
"""
import math
import os

from PIL import Image, ImageDraw, ImageFont

from machine_textures import KITFONT, tcb_logo

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "Assets", "Art", "Office", "Gameplay", "Textures"))
WHITE = (255, 255, 255, 255)


def font(size):
    return ImageFont.truetype(KITFONT, size) if os.path.exists(KITFONT) else ImageFont.load_default()


def watermark(path, s=1024):
    img = Image.new("RGBA", (s, s), (255, 255, 255, 0))
    band = Image.new("RGBA", (s * 2, s * 2), (255, 255, 255, 0))
    d = ImageDraw.Draw(band)
    f = font(46)
    for row in range(0, s * 2, 110):
        x = -((row // 110) % 2) * 160
        while x < s * 2:
            d.text((x, row), "TEMPORAL CUSTOMS ·", font=f, fill=(255, 255, 255, 120))
            x += 470
    band = band.rotate(28, resample=Image.BICUBIC, center=(s, s))
    img.alpha_composite(band.crop((s // 2, s // 2, s // 2 + s, s // 2 + s)))
    mark = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    m = ImageDraw.Draw(mark)
    c = int(s * 0.36)
    tcb_logo(m, (s - c) // 2, (s - c) // 2 - 30, c, WHITE, (0, 0, 0, 0))
    m.ellipse([s * 0.18, s * 0.18 - 30, s * 0.82, s * 0.82 - 30], outline=WHITE, width=10)
    img.alpha_composite(mark)
    img.save(path)


def microprint(path, s=512):
    img = Image.new("RGBA", (s, s), (255, 255, 255, 0))
    f = font(30)
    text = "TEMPORAL CUSTOMS BUREAU · GENUINE · "
    cx = cy = s / 2
    r = s * 0.42
    n = 64
    for i in range(n):
        ch = text[i % len(text)]
        a = 2 * math.pi * i / n
        glyph = Image.new("RGBA", (40, 40), (0, 0, 0, 0))
        ImageDraw.Draw(glyph).text((10, 0), ch, font=f, fill=WHITE)
        glyph = glyph.rotate(-math.degrees(a) - 90, resample=Image.BICUBIC)
        x = cx + math.cos(a) * r - 20
        y = cy + math.sin(a) * r - 20
        img.alpha_composite(glyph, (int(x), int(y)))
    d = ImageDraw.Draw(img)
    d.ellipse([cx - r * 0.86, cy - r * 0.86, cx + r * 0.86, cy + r * 0.86], outline=(255, 255, 255, 160), width=4)
    img.save(path)


def tick(path, s=128):
    img = Image.new("RGBA", (s, s), (255, 255, 255, 0))
    d = ImageDraw.Draw(img)
    d.ellipse([6, 6, s - 7, s - 7], outline=WHITE, width=10)
    d.ellipse([s * 0.22, s * 0.36, s * 0.78, s * 0.64], outline=WHITE, width=8)
    d.ellipse([s * 0.42, s * 0.42, s * 0.58, s * 0.58], fill=WHITE)
    img.save(path)


def pool(path, s=256):
    """The cone's pool of light on the desk: a soft disc, brightest in the middle, fading to its rim."""
    img = Image.new("RGBA", (s, s), (255, 255, 255, 0))
    px = img.load()
    c = (s - 1) / 2
    for y in range(s):
        for x in range(s):
            r = math.hypot(x - c, y - c) / c
            a = max(0.0, 1.0 - r) ** 0.6
            px[x, y] = (255, 255, 255, int(255 * max(0.0, min(1.0, a))))
    img.save(path)


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, fn in (("UvWatermark.png", watermark), ("UvMicroprint.png", microprint), ("UvTick.png", tick), ("UvPool.png", pool)):
        fn(os.path.join(OUT, name))
        print("texture", name)


if __name__ == "__main__":
    main()
