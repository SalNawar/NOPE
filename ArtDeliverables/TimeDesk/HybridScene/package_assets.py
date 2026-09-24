"""Package already generated files; no raster edits and no Unity operations."""
from pathlib import Path
from PIL import Image
import json, shutil, html, zipfile

ROOT=Path(__file__).parent
OUT=ROOT/'Generated'
IMAGES=OUT/'Images'
IMAGES.mkdir(exist_ok=True)
sources=json.loads((OUT/'image_sources.json').read_text(encoding='utf-8'))
catalog=json.loads((ROOT/'asset_catalog.json').read_text(encoding='utf-8'))
by_id={x['id']:x for x in sources}
manifest=[]
for item in catalog:
    src=by_id[item['source_id']]
    dest=IMAGES/(item['id']+'.png')
    shutil.copy2(src['source'],dest)
    im=Image.open(dest)
    alpha=im.getchannel('A') if 'A' in im.getbands() else None
    entry={**item,'path':'Images/'+dest.name,'width':im.width,'height':im.height,
           'mode':im.mode,'alpha_extrema':alpha.getextrema() if alpha else None,
           'representation':'2D raster, not a 3D mesh','prompt':src['prompt']}
    manifest.append(entry)
(OUT/'image_manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
models=json.loads((OUT/'mesh_manifest.json').read_text())
esc=html.escape
sections=[]
for group in dict.fromkeys(x['group'] for x in manifest):
    cards=[]
    for x in manifest:
        if x['group']!=group:continue
        cards.append(f'''<article><a class="art" href="{x['path']}"><img loading="lazy" src="{x['path']}" alt="{esc(x['title'])}"></a><div class="caption"><h3>{esc(x['title'])}</h3><p>{esc(x['use'])}</p><small>{x['width']} × {x['height']} · {x['mode']} · 2D raster</small></div></article>''')
    sections.append('<section><h2>'+esc(group)+'</h2><div class="grid">'+''.join(cards)+'</div></section>')
model_rows=''.join(f'<tr><td><a href="Models/{esc(m["file"])}">{esc(m["file"])}</a></td><td>{m["faces"]:,}</td><td>Editable base geometry</td></tr>' for m in models)
page='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Time Sorter — separated asset pack</title><style>
*{box-sizing:border-box}body{margin:0;background:#18232a;color:#eef0e7;font:16px/1.55 system-ui,sans-serif}main{max-width:1440px;margin:auto;padding:38px 28px}a{color:#ffc77b}header{border-bottom:1px solid #53626a;padding-bottom:24px}h1{font-size:38px;line-height:1.15;margin:8px 0 16px}h2{font-size:24px;margin:38px 0 18px}h3{font-size:17px;margin:0 0 6px}p{margin:8px 0}.eyebrow{letter-spacing:.12em;color:#aac7c7;font-size:13px}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(280px,1fr));gap:18px}.art{display:block;background:repeating-conic-gradient(#344048 0% 25%,#3c4851 0% 50%) 50% / 24px 24px;height:300px;padding:12px}img{width:100%;height:100%;object-fit:contain}article{background:#24323a;border:1px solid #41515a;border-radius:8px;overflow:hidden}.caption{padding:16px}small{color:#b4c1c7}.notice{padding:18px;background:#2c3d43;border-left:4px solid #d8a45d;margin:20px 0}table{width:100%;border-collapse:collapse}th,td{text-align:left;padding:10px;border-bottom:1px solid #42525b}code{font-family:monospace}footer{margin:32px 0;color:#b4c1c7}
</style><main><header><div class="eyebrow">TIME SORTER / ASSET AUTHORING PASS</div><h1>Separated environment assets</h1><p>Sky, megacity, traffic, museum exhibits and floor paper are independent files. Hall, portal, booth and functional props have editable 3D base meshes.</p><div class="notice"><strong>Asset pack, not an installed Unity scene.</strong> The models are base geometry with matte palette materials. Exhibit images are 2D cutouts. No Unity testing was performed. Lighting, history variants, interactions and UI bindings still need integration.</div><p><a href="../README.md">Readme</a> · <a href="image_manifest.json">Image inventory and prompts</a> · <a href="scene_layout.json">Scene placement data</a> · <a href="../SCENE_CONTRACT.md">Scene contract</a></p></header>'''
page+=''.join(sections)
page+='''<section><h2>3D base meshes</h2><p>Metres, Y up. Individual OBJ files have named parts, local planar UVs and a shared <a href="Models/palette.mtl">palette.mtl</a>. Keep the material file beside the OBJs. Their current palette is a starting point, not finished material art.</p><p><a href="Models/hover_vehicle_animated.glb">Animated hover vehicle GLB</a> — one 24-second lane traversal. Looping and game-state control are not wired. This mesh is a simple base vehicle; the detailed traffic sprite above is a separate asset.</p><table><thead><tr><th>File</th><th>Faces</th><th>State</th></tr></thead><tbody>'''+model_rows+'</tbody></table></section><footer>Generated raster art with the built-in image tool. Geometry authored offline. Files are kept outside Assets to avoid changing the active Unity scene.</footer></main></html>'
(OUT/'gallery.html').write_text(page,encoding='utf-8')
zip_path=ROOT/'TimeSorter-separated-assets-v1.zip'
with zipfile.ZipFile(zip_path,'w',compression=zipfile.ZIP_DEFLATED) as z:
    for folder in [OUT]:
        for f in folder.rglob('*'):
            if f.is_file():z.write(f,f.relative_to(ROOT))
    for name in ['README.md','SCENE_CONTRACT.md','asset_catalog.json','build_mesh_assets.py','package_assets.py','EXHIBIT_INVENTORY.md']:
        f=ROOT/name
        if f.exists():z.write(f,name)
print(f'Packaged {len(manifest)} PNG images, {len(models)} OBJ meshes, one animated GLB, manifests and gallery.')
print(zip_path)
