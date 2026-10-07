"""Contact sheets for a QA sweep run (tools/audit/unity/_TimeDeskQASweep.cs.txt calls this at the end).

Usage: python qa_contact_sheet.py <run folder>
Reads <run folder>/shots.tsv (area, name, file) and writes <run folder>/contact_<area>.png per area: a grid of thumbnails,
each labelled with its shot's name, so a person can eyeball every state of the area at once. Needs Pillow.
"""
import csv
import os
import sys

from PIL import Image, ImageDraw, ImageFont

THUMB_W, THUMB_H = 480, 270
LABEL_H = 28
COLUMNS = 4
PAD = 8


def font():
    for name in ('arial.ttf', 'DejaVuSans.ttf'):
        try:
            return ImageFont.truetype(name, 16)
        except OSError:
            continue
    return ImageFont.load_default()


def sheet(run, area, rows, face):
    cells = []
    for name, rel in rows:
        path = os.path.join(run, rel)
        if os.path.exists(path):
            cells.append((name, path))
    if not cells:
        return None
    count_rows = (len(cells) + COLUMNS - 1) // COLUMNS
    width = COLUMNS * (THUMB_W + PAD) + PAD
    height = count_rows * (THUMB_H + LABEL_H + PAD) + PAD + 36
    out = Image.new('RGB', (width, height), (36, 30, 40))
    draw = ImageDraw.Draw(out)
    draw.text((PAD, 8), f'{area}: {len(cells)} shots ({os.path.basename(os.path.normpath(run))})', fill=(238, 229, 208), font=face)
    for i, (name, path) in enumerate(cells):
        x = PAD + (i % COLUMNS) * (THUMB_W + PAD)
        y = 36 + PAD + (i // COLUMNS) * (THUMB_H + LABEL_H + PAD)
        with Image.open(path) as im:
            im = im.convert('RGB')
            im.thumbnail((THUMB_W, THUMB_H))
            out.paste(im, (x + (THUMB_W - im.width) // 2, y + (THUMB_H - im.height) // 2))
        draw.text((x, y + THUMB_H + 4), name[:58], fill=(226, 201, 148), font=face)
    target = os.path.join(run, f'contact_{area}.png')
    out.save(target, optimize=True)
    return target


def main():
    run = sys.argv[1]
    by_area = {}
    with open(os.path.join(run, 'shots.tsv'), encoding='utf-8') as f:
        for row in csv.DictReader(f, delimiter='\t'):
            by_area.setdefault(row['area'], []).append((row['name'], row['file']))
    face = font()
    for area, rows in by_area.items():
        made = sheet(run, area, rows, face)
        print(f'{area}: {made or "no shots"}')


if __name__ == '__main__':
    main()
