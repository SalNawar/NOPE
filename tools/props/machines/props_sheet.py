"""
Time Sorter (NOPE) - contact sheets for the upgrade desk props, composed with Pillow (system Python).

    python props_sheet.py <render_dir> <prefix> [<prefix> ...]

For each prefix it reads <prefix>_tt00..03.png (the turntable) and <prefix>_use.png (the in-use shot)
from <render_dir> and writes <prefix>_sheet.png there: the in-use shot large on the left, the four
turntable frames in a 2 x 2 grid on the right, a caption strip on top. upgrade_props_model.py calls it.
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

INK, CARD, SLATE = (43, 28, 36), (250, 242, 225), (74, 88, 114)


def font(size):
    for p in (r"C:\Windows\Fonts\bahnschrift.ttf", r"C:\Windows\Fonts\arialbd.ttf"):
        if os.path.exists(p):
            return ImageFont.truetype(p, size)
    return ImageFont.load_default()


def sheet(render_dir, prefix):
    frames = [os.path.join(render_dir, f"{prefix}_tt{i:02d}.png") for i in range(4)]
    use = os.path.join(render_dir, f"{prefix}_use.png")
    missing = [p for p in frames + [use] if not os.path.exists(p)]
    if missing:
        print("sheet skipped, missing:", missing)
        return None
    cell_w, cell_h = 420, 336
    use_h = cell_h * 2
    u = Image.open(use).convert("RGB")
    u = u.resize((int(u.width * use_h / u.height), use_h), Image.LANCZOS)
    cap = 44
    out = Image.new("RGB", (u.width + cell_w * 2 + 12, use_h + cap + 8), CARD)
    d = ImageDraw.Draw(out)
    d.rectangle([0, 0, out.width, cap], fill=INK)
    d.text((14, 8), f"{prefix}   in use  |  turntable: front, right side, back, left side", font=font(24), fill=CARD)
    out.paste(u, (4, cap + 4))
    labels = ("front (+Y)", "viewer's right side (-X)", "back", "viewer's left side (+X)")
    for i, p in enumerate(frames):
        im = Image.open(p).convert("RGB").resize((cell_w - 4, cell_h - 4), Image.LANCZOS)
        x = u.width + 8 + (i % 2) * cell_w
        y = cap + 4 + (i // 2) * cell_h
        out.paste(im, (x, y))
        d.text((x + 8, y + 6), labels[i], font=font(18), fill=SLATE)
    path = os.path.join(render_dir, f"{prefix}_sheet.png")
    out.save(path)
    print("sheet", path)
    return path


if __name__ == "__main__":
    rd = sys.argv[1]
    for pre in sys.argv[2:]:
        sheet(rd, pre)
