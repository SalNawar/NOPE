"""The game's character-art contract, read from the game's own sources.

Nothing here is a second copy of a game number: the canvas landmarks come
from LookCanvas.cs, the skin swatches and hair colours from
PlaceholderPalette.cs, the hair-colour tokens from LookKeys.cs, and the
wardrobe flags (wig, back, covers, artNation) from world_source.json. The
key names are built with the LookKeys grammar and then checked against
coverage.json (the list the brief generated from LookKeys), so a name the
game would not load fails loudly.
"""

import json
import os
import re

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))

LOOK_CANVAS = "Assets/Scripts/Visuals/LookCanvas.cs"
PALETTE = "Assets/Scripts/Visuals/PlaceholderPalette.cs"
LOOK_KEYS = "Assets/Scripts/Domain/LookKeys.cs"
CHARACTER_ART = "Assets/Scripts/Characters/CharacterArt.cs"
WORLD_SOURCE = "Assets/Data/World/world_source.json"
COVERAGE = "ArtDeliverables/TimeDesk/Characters/Production/coverage.json"


def _read(rel):
    with open(os.path.join(ROOT, rel), encoding="utf-8-sig") as f:
        return f.read()


def canvas():
    """LookCanvas's integer constants (Width, Height, CenterX, HeadTop, Chin, ... PhotoBottom)."""
    src = _read(LOOK_CANVAS)
    values = {m.group(1): int(m.group(2)) for m in re.finditer(r"public const int (\w+) = (\d+);", src)}
    for name in ("Width", "Height", "CenterX", "HeadTop", "Chin", "Shoulders", "Waist", "Hips", "Knees", "Feet",
                 "PhotoLeft", "PhotoTop", "PhotoRight", "PhotoBottom"):
        if name not in values:
            raise RuntimeError(f"LookCanvas.{name} not found in {LOOK_CANVAS}")
    return values


def skin_swatches():
    """The five skin swatches (tone 1..5) as RGB tuples, from PlaceholderPalette.SkinSwatches."""
    src = _read(PALETTE)
    block = re.search(r"SkinSwatches\s*=\s*\{(.*?)\};", src, re.S)
    if not block:
        raise RuntimeError(f"SkinSwatches not found in {PALETTE}")
    tones = [tuple(int(v, 16) for v in t) for t in re.findall(r"\(0x(\w\w), 0x(\w\w), 0x(\w\w)\)", block.group(1))]
    if len(tones) != 5:
        raise RuntimeError(f"expected 5 skin swatches in {PALETTE}, found {len(tones)}")
    return tones


def hair_colours():
    """Every hair colour token in LookKeys.HairColours order, with its RGB from PlaceholderPalette.Hair."""
    keys_src = _read(LOOK_KEYS)
    m = re.search(r"HairColours = new\[\] \{ (.*?) \};", keys_src)
    if not m:
        raise RuntimeError(f"HairColours not found in {LOOK_KEYS}")
    consts = dict(re.findall(r'public const string (\w+) = "(\w+)";', keys_src))
    tokens = []
    for part in (p.strip() for p in m.group(1).split(",")):
        tokens.append(part.strip('"') if part.startswith('"') else consts[part])

    pal_src = _read(PALETTE)
    rgb = {name: (int(r, 16), int(g, 16), int(b, 16))
           for name, r, g, b in re.findall(r'case "(\w+)": return \(0x(\w\w), 0x(\w\w), 0x(\w\w)\);', pal_src)}
    missing = [t for t in tokens if t not in rgb]
    if missing:
        raise RuntimeError(f"PlaceholderPalette.Hair has no colour for {missing}")
    return [(t, rgb[t]) for t in tokens]


def resources_folder():
    """CharacterArt.AssetFolder: where final art goes (Assets/Art/Characters/Resources/Characters)."""
    src = _read(CHARACTER_ART)
    res = re.search(r'ResourcesFolder = "(\w+)";', src).group(1)
    folder = re.search(r'AssetFolder = "([^"]+)" \+ ResourcesFolder;', src).group(1)
    return folder + res


def wardrobe(nation, era):
    """The world_source.json wardrobe of a place: {'m': {...}, 'f': {...}}."""
    world = json.loads(_read(WORLD_SOURCE))
    for place in world["places"]:
        if place["country"] == nation and place["era"] == era:
            return place["wardrobe"]
    raise KeyError(f"no place {nation}_{era} in {WORLD_SOURCE}")


def required_keys():
    """coverage.json requiredFlat: every key the game needs (the brief's LookKeys-derived list)."""
    return set(json.loads(_read(COVERAGE))["requiredFlat"])


# --- The LookKeys grammar (LookKeys.Body / Head / Garment) ---------------------------------

LAYER_TOKEN = {"hairback": "hairback", "outfit": "outfit", "facialhair": "facialhair",
               "hair": "hair", "headwear": "headwear", "accessory": "accessory"}


def body_key(g, skin):
    return f"body_{g}_skin{skin}"


def head_key(g, skin, face):
    return f"head_{g}_skin{skin}_face{face}"


def garment_key(layer, g, nation, era, colour=None):
    name = f"{LAYER_TOKEN[layer]}_{g}_{nation}_{era}"
    return name + "_" + colour if colour else name


def check_keys(names):
    """Raises unless every name is one the game needs (coverage.json)."""
    known = required_keys()
    unknown = sorted(n for n in names if n not in known)
    if unknown:
        raise RuntimeError(f"not game keys (coverage.json): {unknown}")
