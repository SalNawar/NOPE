"""Paints the desk documents' edge atlas (cel style, no Canva: see the report) into
Assets/Art/Office/Gameplay/Documents/docs_edges.png.

Atlas bands (v up, as UVs read it; PIL rows run down):
  R0 v 0.75..1.00  a page block's edge: page lines, cream and warm grey, an ink line at its foot
  R1 v 0.50..0.75  a manila board's cut edge: a lit lip, the board's fibre, an ink foot
  R2 v 0.25..0.50  a single sheet's or a card's edge: cream, an ink foot
  R3 v 0.00..0.25  solids by u: thread (0.125), plastic (0.375), brass (0.625), ink (0.875)
Each tiles along u (the perimeter) except R3."""
import random
import sys
from PIL import Image, ImageDraw, ImageFilter

OUT = sys.argv[1]
S = 512
img = Image.new('RGB', (S, S))
d = ImageDraw.Draw(img)
rnd = random.Random(7)
INK = (58, 38, 48)


def band(v0, v1):
    """PIL rows (top, bottom) of a v range."""
    return int((1 - v1) * S), int((1 - v0) * S)


# R0: page lines (12 pages top to bottom), alternate cream and warm grey, thin dark seams, ink foot.
t, b = band(0.75, 1.0)
h = b - t
foot = int(h * 0.14)
pages = 12
ph = (h - foot) / pages
for i in range(pages):
    y0 = t + int(i * ph)
    y1 = t + int((i + 1) * ph)
    col = (246, 238, 220) if i % 2 == 0 else (226, 214, 196)
    d.rectangle((0, y0, S, y1), fill=col)
    d.line((0, y1 - 1, S, y1 - 1), fill=(196, 180, 160))
d.rectangle((0, b - foot, S, b), fill=INK)

# R1: manila board edge: a lit lip (top 22%), the board (tan with fibre streaks), an ink foot (18%).
t, b = band(0.5, 0.75)
h = b - t
lip = int(h * 0.22)
foot = int(h * 0.18)
d.rectangle((0, t, S, t + lip), fill=(236, 210, 152))
d.rectangle((0, t + lip, S, b - foot), fill=(170, 124, 76))
for _ in range(220):
    x = rnd.randrange(S)
    y = rnd.randrange(t + lip, b - foot)
    ln = rnd.randrange(6, 28)
    c = rnd.choice([(150, 106, 62), (186, 140, 90), (160, 116, 70)])
    d.line((x, y, x + ln, y), fill=c)
d.rectangle((0, b - foot, S, b), fill=INK)

# R2: a sheet's or a card's edge: cream (a touch of lavender in the shade side) and an ink foot (30%).
t, b = band(0.25, 0.5)
h = b - t
foot = int(h * 0.3)
d.rectangle((0, t, S, b - foot), fill=(232, 222, 206))
d.rectangle((0, t, S, t + max(2, int(h * 0.12))), fill=(250, 244, 232))
d.rectangle((0, b - foot, S, b), fill=INK)

# R3: solids.
t, b = band(0.0, 0.25)
for i, col in enumerate([(150, 48, 50), (238, 236, 230), (212, 160, 85), INK]):
    d.rectangle((i * S // 4, t, (i + 1) * S // 4, b), fill=col)
# the brass strip gets a highlight line (it is sampled at its centre)
d.rectangle((S // 2 + 40, t, S // 2 + 88, b), fill=(240, 202, 128))

img.save(OUT)
print('wrote', OUT)
