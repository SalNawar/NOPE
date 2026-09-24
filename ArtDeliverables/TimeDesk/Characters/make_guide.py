"""Builds the shared character figure guide (1024x1536) that every ChatGPT
character request attaches, so all layers are drawn at the same size/place."""
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
title = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 26)

def dashed_rect(box, color, dash=14, width=3):
    x0, y0, x1, y1 = box
    for x in range(x0, x1, dash * 2):
        d.line([(x, y0), (min(x + dash, x1), y0)], fill=color, width=width)
        d.line([(x, y1), (min(x + dash, x1), y1)], fill=color, width=width)
    for y in range(y0, y1, dash * 2):
        d.line([(x0, y), (x0, min(y + dash, y1))], fill=color, width=width)
        d.line([(x1, y), (x1, min(y + dash, y1))], fill=color, width=width)

# Headroom band.
d.rectangle([0, 0, W, Y["head_top"] - 20], fill=(232, 236, 244))
d.text((135, 20), "HEADROOM: tall hats, crowns, hair buns may extend up here", font=font, fill=(70, 90, 140))

# Mannequin (neutral grey), front-facing, arms relaxed slightly away from body.
g = (178, 178, 178)
d.ellipse([CX - 62, Y["head_top"], CX + 62, Y["chin"]], fill=g)                      # head
d.rectangle([CX - 26, Y["chin"] - 10, CX + 26, Y["shoulders"] + 10], fill=g)         # neck
d.polygon([(CX - 170, Y["shoulders"]), (CX + 170, Y["shoulders"]),
           (CX + 120, Y["waist"]), (CX + 140, Y["hips"]),
           (CX - 140, Y["hips"]), (CX - 120, Y["waist"])], fill=g)                   # torso
d.polygon([(CX - 170, Y["shoulders"]), (CX - 130, Y["shoulders"] + 30),
           (CX - 175, 930), (CX - 215, 925)], fill=g)                                # left arm
d.polygon([(CX + 170, Y["shoulders"]), (CX + 130, Y["shoulders"] + 30),
           (CX + 175, 930), (CX + 215, 925)], fill=g)                                # right arm
d.ellipse([CX - 232, 915, CX - 168, 985], fill=g)                                    # left hand
d.ellipse([CX + 168, 915, CX + 232, 985], fill=g)                                    # right hand
d.polygon([(CX - 140, Y["hips"]), (CX - 8, Y["hips"]), (CX - 30, Y["feet"]), (CX - 110, Y["feet"])], fill=g)  # left leg
d.polygon([(CX + 8, Y["hips"]), (CX + 140, Y["hips"]), (CX + 110, Y["feet"]), (CX + 30, Y["feet"])], fill=g)    # right leg

# Centre line.
for y in range(0, H, 24):
    d.line([(CX, y), (CX, y + 12)], fill=(120, 120, 120), width=1)

# Landmark lines.
labels = dict(head_top="TOP OF HEAD", chin="CHIN", shoulders="SHOULDERS", waist="WAIST",
              hips="HIPS", knees="KNEES", feet="SOLES OF FEET (floor)")
for key, y in Y.items():
    d.line([(0, y), (W, y)], fill=(200, 60, 60), width=2)
    d.text((W - 300, y - 26), f"{labels[key]}  y={y}", font=small, fill=(160, 40, 40))

# Safe area and passport crop.
dashed_rect((SAFE_X[0], 10, SAFE_X[1], Y["feet"]), (60, 110, 200))
d.text((SAFE_X[0] + 15, 60), "SAFE AREA: nothing may cross the blue dashed box", font=small, fill=(60, 110, 200))
dashed_rect(CROP, (200, 120, 0), width=4)
d.text((CROP[0] + 6, CROP[1] + 6), "PASSPORT PHOTO CROP", font=small, fill=(170, 100, 0))

d.text((20, H - 34), "Time Sorter character guide  |  1024 x 1536  |  front view, light from top-left", font=title, fill=(40, 40, 40))

out = r"E:\unity\NOPE\ArtDeliverables\TimeDesk\Characters\character_guide_1024x1536.png"
img.save(out)
print("saved", out, img.size)
