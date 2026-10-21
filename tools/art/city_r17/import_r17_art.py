"""Imports the r17 look-dev's art into Unity for the CITY-R17 port.

Reads E:/unity/NOPE-tools/city/live (the r17 page's own files) and writes Assets/Art/City/R17:
  Planes/  P00..P10, each cropped to its painted pixels (+2 px), from assets/layers (2859 x 1152 on the plate frame
           [-85.333, 0, 2858.667, 1152]; P08 and P10 carry the r9 rim fixes)
  Clouds/  the 8 soft cloud sheets (assets/r17/clouds_soft)
  Ads/     the 4 ad sheets (assets/r17/ads)
  Fire/    the fire sheet (assets/r15/fire), the furnace base and spill (assets/r16/furnace)
  Smoke/   the two stacks' body and mouth sheets (assets/r16/smoke)
  Train/   the three cars and their glows (assets/r17/train), the r9 parapet (assets/r15/train) and the r12 viaduct
           paint (assets/r12/viaduct_paint.png), cropped to its painted pixels
  Hive/    the laundry sway's clean patch and cloth (assets/hive_clean.png, hive_cloth.png), cropped to their pixels
Pixels are never resampled. Every texture is padded with transparent pixels on the right and bottom to a multiple of 4
(BC7 needs it); the data's pixel rects still point at the same pixels, so uv = px / the padded size.
Writes Assets/Data/City/R17/city_r17_art.json (each file's padded size, and where each cropped image sits on the
plate) and a .meta for every new file (the import settings CityR17Import enforces: no sRGB decode, because the city
composites in the look-dev's gamma space; alpha is transparency; no mips; clamp; bilinear; BC7). An existing meta is kept
as it is. Never run it while a Unity editor has the project open (it rewrites the PNGs under the importer).

usage: python -I tools/art/city_r17/import_r17_art.py [--live E:/unity/NOPE-tools/city/live] [--repo .]
"""
import argparse
import json
import uuid
from pathlib import Path

import numpy as np
from PIL import Image

PLATE_FRAME = (-85.333, 0.0, 2858.667, 1152.0)   # where the 2859 x 1152 plane images sit on the plate (data/layers.js rect)

SOURCES = {
    'Planes': [('assets/layers/P%02d.png' % i, 'P%02d.png' % i, 'plane') for i in range(11)],
    'Clouds': [('assets/r17/clouds_soft/%s.png' % c, '%s.png' % c, 'sheet') for c in ('N1', 'N2', 'M1', 'M2', 'M3', 'F1', 'F2', 'F3')],
    'Ads': [('assets/r17/ads/ads_%d.png' % i, 'ads_%d.png' % i, 'sheet') for i in range(4)],
    'Fire': [('assets/r15/fire/fire_0.png', 'fire_0.png', 'sheet'), ('assets/r16/furnace/base.png', 'base.png', 'sheet'),
             ('assets/r16/furnace/spill.png', 'spill.png', 'sheet')],
    'Smoke': [('assets/r16/smoke/%s.png' % s, '%s.png' % s, 'sheet') for s in ('smokeL_0', 'smokeLm_0', 'smokeR_0', 'smokeRm_0')],
    'Train': [('assets/r17/train/%s.png' % s, '%s.png' % s, 'sheet') for s in ('car_lead', 'car_mid', 'car_tail', 'glow_lead', 'glow_mid', 'glow_tail')]
             + [('assets/r15/train/viaduct_parapet.png', 'viaduct_parapet.png', 'sheet'),
                ('assets/r12/viaduct_paint.png', 'viaduct_paint.png', 'paint')],
    'Hive': [('assets/hive_clean.png', 'hive_clean.png', 'paint'), ('assets/hive_cloth.png', 'hive_cloth.png', 'paint')],
}

FOLDER_META = "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"


def texture_meta(guid, max_size):
    return f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 0
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 1
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: {max_size}
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 100
  spriteMode: 0
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 0
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: {max_size}
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 2
    compressionQuality: 100
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Standalone
    maxTextureSize: {max_size}
    resizeAlgorithm: 0
    textureFormat: 25
    textureCompression: 2
    compressionQuality: 100
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 1
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID:
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def write_meta(path, body_fn):
    meta = Path(str(path) + '.meta')
    if meta.exists():
        return   # keep it (and its GUID): Unity may have normalised it, and CityR17Import enforces the settings anyway
    meta.write_text(body_fn(uuid.uuid4().hex), encoding='utf-8', newline='\n')


def folder_meta(path):
    meta = Path(str(path) + '.meta')
    if not meta.exists():
        meta.write_text(f"fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\n{FOLDER_META}", encoding='utf-8', newline='\n')


def up4(n):
    return (n + 3) // 4 * 4


def alpha_box(a, margin):
    """The painted pixels' box [x0, y0, x1, y1) (alpha > 0), grown by margin and kept inside the image."""
    al = a[:, :, 3]
    ys, xs = np.nonzero(al > 0)
    if len(xs) == 0:
        return 0, 0, a.shape[1], a.shape[0]
    h, w = al.shape
    return max(0, xs.min() - margin), max(0, ys.min() - margin), min(w, xs.max() + 1 + margin), min(h, ys.max() + 1 + margin)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--live', default='E:/unity/NOPE-tools/city/live')
    ap.add_argument('--repo', default='.')
    args = ap.parse_args()
    live, repo = Path(args.live), Path(args.repo)
    art = repo / 'Assets' / 'Art' / 'City' / 'R17'
    data = repo / 'Assets' / 'Data' / 'City' / 'R17'
    for p in (repo / 'Assets' / 'Art' / 'City', art, repo / 'Assets' / 'Data' / 'City', data):
        p.mkdir(parents=True, exist_ok=True)
        folder_meta(p)
    out = {'note': 'written by tools/art/city_r17/import_r17_art.py; size = the padded texture (px); '
                   'plate = where the image sits on the plate (plate px) for the cropped planes and paint', 'files': {}}
    total = 0
    for folder, items in SOURCES.items():
        dst_dir = art / folder
        dst_dir.mkdir(exist_ok=True)
        folder_meta(dst_dir)
        for src_rel, name, kind in items:
            im = Image.open(live / src_rel).convert('RGBA')
            a = np.array(im)
            entry = {}
            if kind == 'plane':
                sx = PLATE_FRAME[2] / a.shape[1]
                sy = PLATE_FRAME[3] / a.shape[0]
                x0, y0, x1, y1 = alpha_box(a, 2)
                a = a[y0:y1, x0:x1]
                entry['crop'] = [int(x0), int(y0), int(x1 - x0), int(y1 - y0)]
                entry['plate'] = [PLATE_FRAME[0] + x0 * sx, PLATE_FRAME[1] + y0 * sy, (x1 - x0) * sx, (y1 - y0) * sy]
            elif kind == 'paint':
                x0, y0, x1, y1 = alpha_box(a, 2)
                a = a[y0:y1, x0:x1]
                entry['crop'] = [int(x0), int(y0), int(x1 - x0), int(y1 - y0)]
                entry['plate'] = [float(x0), float(y0), float(x1 - x0), float(y1 - y0)]
            h, w = a.shape[:2]
            pw, ph = up4(w), up4(h)
            if (pw, ph) != (w, h):
                padded = np.zeros((ph, pw, 4), dtype=np.uint8)
                padded[:h, :w] = a
                a = padded
            entry['size'] = [pw, ph]
            entry['used'] = [w, h]
            dst = dst_dir / name
            Image.fromarray(a, 'RGBA').save(dst, optimize=True)
            write_meta(dst, lambda g, m=max(pw, ph): texture_meta(g, 4096 if m <= 4096 else 8192))
            out['files'][folder + '/' + name] = entry
            total += pw * ph
            print(f'{folder}/{name}: {pw}x{ph}' + (f" crop {entry['crop']}" if 'crop' in entry else ''))
    (data / 'city_r17_art.json').write_text(json.dumps(out, indent=1) + '\n', encoding='utf-8', newline='\n')
    print(f'texels {total / 1e6:.1f} M (about {total / 1e6:.0f} MB at BC7, no mips)')


if __name__ == '__main__':
    main()
