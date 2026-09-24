from pathlib import Path
import json, shutil, html
from PIL import Image, ImageOps
ROOT=Path(__file__).resolve().parent
M=json.loads((ROOT/'manifest.json').read_text(encoding='utf-8-sig'))
exec((ROOT/'build_native_art.py').read_text(encoding='utf-8-sig').split('done=[]')[0])
a=Art(320,440);paper(a);a.rect(14,14,292,412,None,BLUE,2)
a.text(160,33,'TIME TRANSIT AUTHORITY',10,BLUE,True,True)
glyph(a,'future',70,102,180,BLUE)
a.text(160,318,'KEEP TIME',27,INK,True,True)
a.text(160,358,'IN ORDER',27,INK,True,True)
a.save('Office/Placeholder/poster.png')
for row in M['assets']:
 if row[0]=='Office/Placeholder/poster.png':row[4]='created-native'
(ROOT/'manifest.json').write_text(json.dumps(M,indent=2),encoding='utf-8')

sources=json.loads((ROOT/'generated-sources.json').read_text(encoding='utf-8-sig'))
lookup={x['path']:x for x in sources}
for row in M['assets']:
 path,w,h,alpha,status=row
 if path not in lookup:continue
 src=Path(lookup[path]['source'])
 if not src.exists():continue
 raw=ROOT/'Sources'/path;raw.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(src,raw)
 im=Image.open(src).convert('RGBA')
 if alpha:
  if path!='Office/Booth/desk_deep.png':
   box=im.getchannel('A').getbbox()
   if box:im=im.crop(box)
   fit=ImageOps.contain(im,(max(1,int(w*.96)),max(1,int(h*.96))),Image.Resampling.LANCZOS)
   out=Image.new('RGBA',(w,h),(0,0,0,0));out.alpha_composite(fit,((w-fit.width)//2,(h-fit.height)//2))
  else:out=im.resize((w,h),Image.Resampling.LANCZOS)
 else:
  out=ImageOps.fit(im.convert('RGB'),(w,h),Image.Resampling.LANCZOS)
 dest=ROOT/path;dest.parent.mkdir(parents=True,exist_ok=True);out.save(dest)
 row[4]='created-imagegen'
# The brief explicitly requests a shallow compatibility desk in addition to the newer deep composition.
deep=ROOT/'Office/Booth/desk_deep.png'
if deep.exists():
 im=Image.open(deep).convert('RGBA');box=im.getchannel('A').getbbox()
 im=im.crop(box);im=ImageOps.contain(im,(1600,360),Image.Resampling.LANCZOS)
 out=Image.new('RGBA',(1600,360),(0,0,0,0));out.alpha_composite(im,((1600-im.width)//2,360-im.height))
 out.save(ROOT/'Office/Placeholder/desk.png')
 for row in M['assets']:
  if row[0]=='Office/Placeholder/desk.png':row[4]='created-compatibility'
(ROOT/'manifest.json').write_text(json.dumps(M,indent=2),encoding='utf-8')
report=[];cards=[]
for path,w,h,alpha,status in M['assets']:
 p=ROOT/path
 if not p.exists():report.append({'path':path,'status':'pending'});continue
 im=Image.open(p)
 checks={'size':im.size==(w,h),'alpha':not alpha or im.mode=='RGBA' and im.getchannel('A').getextrema()[0]<255}
 report.append({'path':path,'status':status,'mode':im.mode,'dimensions':list(im.size),'checks':checks})
 cards.append(f'<article><div class="pic"><img loading="lazy" src="{html.escape(path)}"></div><p>{html.escape(path)}</p><small>{w} × {h} · {html.escape(status)}</small></article>')
(ROOT/'validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
(ROOT/'gallery.html').write_text('<!doctype html><meta charset="utf-8"><title>Time Sorter — non-character art</title><style>body{background:#e8e3d7;color:#304953;font:15px Segoe UI;margin:32px}main{display:grid;grid-template-columns:repeat(auto-fill,minmax(240px,1fr));gap:20px}article{background:#f8f3e8;border:1px solid #bac0b8;padding:12px;border-radius:8px}.pic{height:230px;background:repeating-conic-gradient(#e4e4dd 0% 25%,#f4f1e9 0% 50%) 50%/20px 20px;display:flex;align-items:center;justify-content:center}img{max-width:100%;max-height:100%;object-fit:contain}p{overflow-wrap:anywhere}small{color:#62736f}</style><h1>Time Sorter — non-character artwork</h1><p>Eight countries · four eras · no characters or religious content. Separate production assets; not installed into Unity scenes.</p><main>'+''.join(cards)+'</main>',encoding='utf-8')
print(json.dumps({'created':sum(x.get('status')!='pending' for x in report),'pending':sum(x.get('status')=='pending' for x in report),'checkFailures':[x for x in report if 'checks' in x and not all(x['checks'].values())]}))

