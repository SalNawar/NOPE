"""Figure guide v2: the v1 geometry (make_guide.py, unchanged landmarks = piece-4 LookCanvas)
with the lighting note changed to neutral even light. Review 2: the headroom label no longer invites crowns
(finding 12), and the legs end in simple front-view feet whose soles sit on y=1490 (finding 40), so ChatGPT does not
add feet below the soles line. Writes art/character_guide_v2_1024x1536.png."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

W, H = 1024, 1536
CX = W // 2
Y = dict(head_top=260, chin=424, shoulders=500, waist=760, hips=900, knees=1170, feet=1490)
SAFE_X = (120, W - 120)
CROP = (362, 215, 662, 590)  # passport photo crop, 4:5

img = Image.new("RGB", (W, H), (244, 241, 234))
d = ImageDraw.Draw(img)
font = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 22)
small = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 18)
title = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 24)

def dashed_rect(box, color, dash=14, width=3):
    x0, y0, x1, y1 = box
    for x in range(x0, x1, dash * 2):
        d.line([(x, y0), (min(x + dash, x1), y0)], fill=color, width=width)
        d.line([(x, y1), (min(x + dash, x1), y1)], fill=color, width=width)
    for y in range(y0, y1, dash * 2):
        d.line([(x0, y), (x0, min(y + dash, y1))], fill=color, width=width)
        d.line([(x1, y), (x1, min(y + dash, y1))], fill=color, width=width)

d.rectangle([0, 0, W, Y["head_top"] - 20], fill=(232, 236, 244))
d.text((135, 20), "HEADROOM: tall hats, headdresses and hair buns may extend up here", font=font, fill=(70, 90, 140))

g = (178, 178, 178)
d.ellipse([CX - 62, Y["head_top"], CX + 62, Y["chin"]], fill=g)
d.rectangle([CX - 26, Y["chin"] - 10, CX + 26, Y["shoulders"] + 10], fill=g)
d.polygon([(CX - 170, Y["shoulders"]), (CX + 170, Y["shoulders"]),
           (CX + 120, Y["waist"]), (CX + 140, Y["hips"]),
           (CX - 140, Y["hips"]), (CX - 120, Y["waist"])], fill=g)
d.polygon([(CX - 170, Y["shoulders"]), (CX - 130, Y["shoulders"] + 30),
           (CX - 175, 930), (CX - 215, 925)], fill=g)
d.polygon([(CX + 170, Y["shoulders"]), (CX + 130, Y["shoulders"] + 30),
           (CX + 175, 930), (CX + 215, 925)], fill=g)
d.ellipse([CX - 232, 915, CX - 168, 985], fill=g)
d.ellipse([CX + 168, 915, CX + 232, 985], fill=g)
ANKLE = Y["feet"] - 60
d.polygon([(CX - 140, Y["hips"]), (CX - 8, Y["hips"]), (CX - 40, ANKLE), (CX - 100, ANKLE)], fill=g)
d.polygon([(CX + 8, Y["hips"]), (CX + 140, Y["hips"]), (CX + 100, ANKLE), (CX + 40, ANKLE)], fill=g)
# Front-view feet: ankle to toes, the soles flat on the SOLES line (y=1490), toes turned slightly out.
d.polygon([(CX - 100, ANKLE), (CX - 40, ANKLE), (CX - 30, Y["feet"] - 18), (CX - 26, Y["feet"]), (CX - 124, Y["feet"]), (CX - 118, Y["feet"] - 20)], fill=g)
d.polygon([(CX + 40, ANKLE), (CX + 100, ANKLE), (CX + 118, Y["feet"] - 20), (CX + 124, Y["feet"]), (CX + 26, Y["feet"]), (CX + 30, Y["feet"] - 18)], fill=g)

for y in range(0, H, 24):
    d.line([(CX, y), (CX, y + 12)], fill=(120, 120, 120), width=1)

labels = dict(head_top="TOP OF HEAD", chin="CHIN", shoulders="SHOULDERS", waist="WAIST",
              hips="HIPS", knees="KNEES", feet="SOLES OF FEET (floor)")
for key, y in Y.items():
    d.line([(0, y), (W, y)], fill=(200, 60, 60), width=2)
    d.text((W - 300, y - 26), f"{labels[key]}  y={y}", font=small, fill=(160, 40, 40))

dashed_rect((SAFE_X[0], 10, SAFE_X[1], Y["feet"]), (60, 110, 200))
d.text((SAFE_X[0] + 15, 60), "SAFE AREA: nothing may cross the blue dashed box", font=small, fill=(60, 110, 200))
dashed_rect(CROP, (200, 120, 0), width=4)
d.text((CROP[0] + 6, CROP[1] + 6), "PASSPORT PHOTO CROP", font=small, fill=(170, 100, 0))

d.text((20, H - 34), "Time Sorter character guide v2  |  1024 x 1536  |  front view, neutral even light", font=title, fill=(40, 40, 40))

out = Path(__file__).resolve().parent.parent / "character_guide_v2_1024x1536.png"
img.save(out)
print("saved", out, img.size)
