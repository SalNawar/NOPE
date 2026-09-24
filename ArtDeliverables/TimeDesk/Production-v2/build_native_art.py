from pathlib import Path
import json, math, html
from PIL import Image, ImageDraw, ImageFont
ROOT=Path(__file__).resolve().parent
M=json.loads((ROOT/'manifest.json').read_text(encoding='utf-8-sig'))
INK='#304953'; CREAM='#f4eddb'; BLUE='#467791'; GOLD='#c79851'; TEAL='#497e79'; RED='#a55651'
S=3
class Art:
 def __init__(self,w,h,bg=None):
  self.w,self.h=w,h; self.im=Image.new('RGBA',(w*S,h*S),(0,0,0,0));self.d=ImageDraw.Draw(self.im);self.v=[]
  if bg:self.rect(0,0,w,h,bg)
 def rect(self,x,y,w,h,fill,stroke=None,sw=1,r=0):
  box=tuple(int(z*S) for z in (x,y,x+w,y+h))
  if r:self.d.rounded_rectangle(box,radius=int(r*S),fill=fill,outline=stroke,width=max(1,int(sw*S)))
  else:self.d.rectangle(box,fill=fill,outline=stroke,width=max(1,int(sw*S)))
  self.v.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{r}" fill="{fill or "none"}" stroke="{stroke or "none"}" stroke-width="{sw}"/>')
 def line(self,pts,color=INK,sw=2):
  self.d.line([(int(x*S),int(y*S)) for x,y in pts],fill=color,width=max(1,int(sw*S)),joint='curve')
  self.v.append(f'<polyline points="{" ".join(f"{x},{y}" for x,y in pts)}" fill="none" stroke="{color}" stroke-width="{sw}" stroke-linecap="round" stroke-linejoin="round"/>')
 def ellipse(self,x,y,w,h,fill=None,stroke=INK,sw=2):
  self.d.ellipse(tuple(int(z*S) for z in (x,y,x+w,y+h)),fill=fill,outline=stroke,width=max(1,int(sw*S)))
  self.v.append(f'<ellipse cx="{x+w/2}" cy="{y+h/2}" rx="{w/2}" ry="{h/2}" fill="{fill or "none"}" stroke="{stroke or "none"}" stroke-width="{sw}"/>')
 def poly(self,pts,fill,stroke=None,sw=2):
  self.d.polygon([(int(x*S),int(y*S)) for x,y in pts],fill=fill)
  if stroke:self.line(pts+[pts[0]],stroke,sw)
  self.v.append(f'<polygon points="{" ".join(f"{x},{y}" for x,y in pts)}" fill="{fill}" stroke="{stroke or "none"}" stroke-width="{sw}"/>')
 def text(self,x,y,t,size=18,color=INK,bold=False,center=False):
  fp='C:/Windows/Fonts/'+('segoeuib.ttf' if bold else 'segoeui.ttf')
  font=ImageFont.truetype(fp,max(1,int(size*S)))
  if center:x-=self.d.textlength(t,font=font)/(2*S)
  self.d.text((int(x*S),int(y*S)),t,font=font,fill=color)
  self.v.append(f'<text x="{x}" y="{y+size}" font-family="Segoe UI" font-size="{size}" font-weight="{700 if bold else 400}" fill="{color}">{html.escape(t)}</text>')
 def save(self,path):
  p=ROOT/path;p.parent.mkdir(parents=True,exist_ok=True)
  self.im.resize((self.w,self.h),Image.Resampling.LANCZOS).save(p)
  p.with_suffix('.svg').write_text(f'<svg xmlns="http://www.w3.org/2000/svg" width="{self.w}" height="{self.h}" viewBox="0 0 {self.w} {self.h}">' + ''.join(self.v)+'</svg>',encoding='utf-8')
def glyph(a,name,x,y,size,color=INK):
 # All original geometric symbols; no national flags, religious marks or portraits.
 def p(u,v):return (x+u*size,y+v*size)
 def ln(points,sw=.055):a.line([p(*v) for v in points],color,size*sw)
 def re(u,v,w,h,fill=None):a.rect(x+u*size,y+v*size,w*size,h*size,fill,color,size*.035,r=size*.035)
 def el(u,v,w,h,fill=None):a.ellipse(x+u*size,y+v*size,w*size,h*size,fill,color,size*.035)
 n=name.lower()
 if n in ['egypt','water']:
  for j in range(3):ln([(.1,.35+j*.17),(.3,.27+j*.17),(.5,.35+j*.17),(.7,.27+j*.17),(.9,.35+j*.17)])
 elif n in ['greece','democracy']:
  ln([(.12,.25),(.5,.08),(.88,.25),(.12,.25)])
  for u in [.25,.5,.75]:ln([(u,.34),(u,.77)],.07)
  ln([(.12,.85),(.88,.85)])
 elif n in ['china','language','lexicon','dialect']:
  re(.1,.2,.38,.6,CREAM);re(.5,.2,.38,.6,CREAM)
  for v in [.35,.48,.61]:ln([(.19,v),(.38,v)],.025);ln([(.6,v),(.79,v)],.025)
 elif n in ['germany','settings','industry']:
  el(.22,.22,.56,.56);el(.4,.4,.2,.2)
  for i in range(8):
   t=i*math.pi/4;ln([(.5+.31*math.cos(t),.5+.31*math.sin(t)),(.5+.43*math.cos(t),.5+.43*math.sin(t))],.09)
 elif n in ['japan','technology','material','advanced_scanner']:
  re(.25,.25,.5,.5,CREAM);re(.37,.37,.26,.26)
  for t in [.35,.5,.65]:
   ln([(t,.1),(t,.23)]);ln([(t,.77),(t,.9)]);ln([(.1,t),(.23,t)]);ln([(.77,t),(.9,t)])
 elif n in ['britain']:
  ln([(.1,.8),(.9,.8)])
  for u in [.25,.75]:ln([(u,.2),(u,.8)])
  ln([(.1,.4),(.25,.32),(.5,.53),(.75,.32),(.9,.4)])
  for u in [.35,.45,.55,.65]:ln([(u,.5),(u,.8)],.025)
 elif n in ['iraq','notes','cluelog','directives','archive_access']:
  re(.2,.12,.6,.76,CREAM)
  for v in [.3,.45,.6,.75]:ln([(.3,v),(.68,v)],.03)
 elif n=='italy':
  el(.43,.1,.14,.14,CREAM)
  ln([(.48,.24),(.22,.87)],.055);ln([(.52,.24),(.8,.87)],.055)
  ln([(.35,.6),(.67,.6)],.025)
 elif n=='art':
  el(.12,.16,.7,.68,CREAM)
  for u,v in [(.32,.32),(.52,.3),(.68,.42)]:el(u,v,.07,.07,color)
  ln([(.48,.75),(.88,.23)],.075)
 elif n in ['currency','money','credits','small_win','jackpot']:
  el(.12,.28,.55,.55,GOLD);el(.35,.12,.55,.55,'#e9c984');ln([(.63,.24),(.63,.52)],.045)
 elif n in ['internet','future']:
  el(.12,.12,.76,.76);a.ellipse(x+.34*size,y+.12*size,.32*size,.76*size,None,color,size*.025)
  ln([(.12,.5),(.88,.5)],.025);ln([(.22,.28),(.78,.28)],.025);ln([(.22,.72),(.78,.72)],.025)
 elif n in ['scanner','compare']:
  re(.13,.57,.74,.28,CREAM);re(.25,.17,.5,.44,CREAM);ln([(.34,.29),(.66,.29)],.025);ln([(.34,.4),(.61,.4)],.025)
 elif n in ['citizen_records','diplomatic_contacts']:
  re(.12,.2,.76,.6,CREAM);re(.22,.33,.2,.26);ln([(.5,.38),(.77,.38)],.025);ln([(.5,.52),(.77,.52)],.025);ln([(.5,.64),(.69,.64)],.025)
 elif n in ['power']:
  # open ring and vertical power stroke
  pts=[(.5+.31*math.cos(t),.5+.31*math.sin(t)) for t in [(-.95+i*(math.pi*2-1.24)/30) for i in range(31)]]
  ln(pts);ln([(.5,.07),(.5,.47)],.07)
 elif n in ['day','calendar']:
  re(.16,.2,.68,.64,CREAM);ln([(.16,.37),(.84,.37)])
  ln([(.32,.1),(.32,.27)]);ln([(.68,.1),(.68,.27)])
  for u in [.31,.51,.71]:
   for v in [.5,.68]:a.rect(x+(u-.04)*size,y+(v-.04)*size,size*.08,size*.08,color)
 elif n in ['warning','forgery_warning','busted']:
  a.poly([p(.5,.1),p(.94,.86),p(.06,.86)],'#e9c984',color,size*.035)
  ln([(.5,.34),(.5,.58)],.065);el(.47,.68,.06,.06,color)
 elif n in ['stability','science','philosophy']:
  el(.14,.14,.72,.72);ln([(.18,.56),(.34,.56),(.43,.32),(.56,.73),(.66,.48),(.85,.48)],.04)
 elif n in ['ancient']:
  re(.18,.24,.64,.58,CREAM);ln([(.12,.22),(.88,.22)]);ln([(.12,.85),(.88,.85)])
 elif n in ['old']:
  re(.22,.25,.56,.61,CREAM)
  for u in [.22,.43,.65]:re(u,.12,.13,.19,CREAM)
 elif n in ['modern']:
  re(.13,.16,.74,.52,CREAM);ln([(.5,.68),(.5,.82)]);ln([(.29,.84),(.71,.84)])
 elif n in ['militarism']:
  a.poly([p(.2,.16),p(.8,.16),p(.76,.66),p(.5,.9),p(.24,.66)],CREAM,color,size*.04)
  ln([(.36,.36),(.64,.62)]);ln([(.64,.36),(.36,.62)])
 else:
  el(.12,.12,.76,.76);ln([(.2,.65),(.5,.25),(.8,.65)]);ln([(.3,.75),(.7,.75)])
def panel(a,color=CREAM,border=INK,pad=2):
 a.rect(pad,pad,a.w-pad*2-1,a.h-pad*2-1,color,border,1.5,r=min(8,a.h*.15))
 a.line([(pad+3,a.h-pad-4),(pad+3,pad+3),(a.w-pad-4,pad+3)],'#ffffff',1.5)
def paper(a):
 a.rect(0,0,a.w,a.h,'#f6f0df')
 # subtle, reproducible fiber lines instead of noisy unreadable texture
 for j in range(12,a.h,23):a.line([(0,j),(a.w,j)],'#eee7d7',.5)
def seal(a,n,color):
 s=min(a.w,a.h);a.ellipse(s*.07,s*.07,s*.86,s*.86,CREAM,color,s*.025);a.ellipse(s*.12,s*.12,s*.76,s*.76,None,color,s*.008)
 glyph(a,n,s*.24,s*.24,s*.52,color)
COLORS={'Egypt':'#ad7541','Greece':'#477c96','China':'#92713e','Germany':'#526777','Japan':'#9a5f67','Britain':'#506c6c','Iraq':'#547f88','Italy':'#6c8060'}
LABELS={'currency':'Currency','language':'Language','technology':'Technology'}
done=[]
for row in M['assets']:
 path,w,h,alpha,status=row
 name=Path(path).stem
 if path.startswith('Concepts/') or path.startswith('Office/Booth/') and not name.startswith('lighting_overlay'):continue
 if path.startswith('Office/Placeholder/') and '/Posters/' not in path:continue
 if path=='Desktop/xp_bliss.png':continue
 if path.startswith('Home/') and name in ['home_bg','slot_machine','slot_lever']:continue
 if path.startswith('Title/') and (name=='title_bg' or name.startswith('ending_') and name!='ending_panel'):continue
 a=Art(w,h)
 if '/Documents/' in path:
  nation,era=path.split('/')[2:4];color=COLORS[nation]
  if name.endswith('_seal'):seal(a,nation,color)
  else:
   border=name.endswith('_border')
   if not border:
    paper(a)
    a.rect(0,0,w,26,color)
    a.text(64,63,nation.upper(),34,color,True)
    a.text(64,112,era.upper()+' / TEMPORAL TRANSIT',15,color)
    glyph(a,nation,w-150,56,90,color)
    a.line([(64,170),(w-64,170)],color,2)
    # Deliberately blank field area: all identity, dates and legal claims belong to game.
    if 'passport' in name:
     a.rect(64,218,182,228,'#eee7d7',color,1)
     for yy in [262,352,442]:a.line([(282,yy),(w-64,yy)],'#c9c6b8',1)
     for yy in [555,665,775,885]:a.line([(64,yy),(w-64,yy)],'#c9c6b8',1)
    else:
     for yy in [290,425,560,695,830]:a.line([(64,yy),(w-64,yy)],'#c9c6b8',1)
    a.text(64,h-83,'TIME TRANSIT AUTHORITY',12,color,True)
   a.rect(25,25,w-50,h-50,None,color,2)
   a.rect(34,34,w-68,h-68,None,color,.7)
   if era=='Ancient':
    for xx in range(48,w-48,32):
     a.line([(xx,h-37),(xx,h-48),(xx+12,h-48),(xx+12,h-37)],color,1)
   elif era=='Old':
    for xx,yy in [(46,46),(w-46,46),(46,h-46),(w-46,h-46)]:a.ellipse(xx-5,yy-5,10,10,None,color,1)
   elif era=='Modern':
    a.rect(39,h-60,w-78,10,color)
   else:
    for xx in [42,w-110]:
     a.line([(xx,62),(xx,43),(xx+65,43)],color,3)
    for xx in range(55,w-55,11):a.rect(xx,h-54,3,12,color)
 elif '/Posters/' in path:
  paper(a);bits=name.split('_')
  country=bits[2].title() if name.startswith('poster_dom_') else 'Timeline'
  theme=bits[-1];color=COLORS.get(country,BLUE)
  a.rect(13,13,w-26,h-26,None,color,2)
  a.text(w/2,28,'TIME TRANSIT AUTHORITY',10,color,True,True)
  a.ellipse(42,82,w-84,w-84,'#e7ddc6',None)
  glyph(a,theme if theme in ['democracy','science','art'] else 'warning' if theme=='collapse' else 'science' if theme=='boom' else 'art',68,111,w-136,color)
  if country!='Timeline':glyph(a,country.lower(),w-70,270,37,color)
  a.rect(27,315,w-54,3,color)
  a.text(w/2,337,country.upper(),22,color,True,True)
  a.text(w/2,375,theme.replace('_',' ').upper(),13,color,True,True)
 elif name.startswith('lighting_overlay'):
  rgb={'warm':(238,169,78),'cold':(83,150,195),'alarm':(215,63,51)}[name.split('_')[-1]]
  # Pure color artwork, no editing of existing images.
  for yy in range(h):
   op=int(30*(1-yy/h)+5);a.d.line([(0,yy*S),(w*S,yy*S)],fill=(*rgb,op),width=S)
  a.v=[f'<rect width="{w}" height="{h}" fill="rgb{rgb}" opacity=".1"/>']
 elif path.startswith('UI/Icons/') or name.startswith('icon_') or name.startswith('tray_') and name!='tray_bg' or name.startswith('upgrade_') or name.startswith('slot_'):
  key=name
  for prefix in ['icon_','tray_','upgrade_','slot_','attr_','era_']:
   if key.startswith(prefix):key=key[len(prefix):];break
  ss=min(w,h)
  if w>=100 and not path.startswith('UI/Icons/'):a.rect(ss*.09,ss*.12,ss*.78,ss*.75,'#e9e2cd',INK,ss*.018,r=ss*.1)
  glyph(a,key,ss*.12,ss*.12,ss*.76,BLUE if 'attr_' not in name else TEAL)
 elif name.startswith('cursor_'):
  if name.endswith('arrow'):a.poly([(3,2),(3,h-6),(10,h-12),(15,h-2),(20,h-5),(15,h-15),(25,h-15)],CREAM,INK,1.3)
  else:
   a.poly([(11,28),(5,19),(5,15),(9,15),(11,18),(11,3),(15,3),(15,14),(24,14),(27,18),(24,28)],CREAM,INK,1.3)
 elif name.startswith('btn_') or name.startswith('start_button') or name.startswith('intercom_button'):
  pressed='pressed' in name;hover='hover' in name
  color='#cfdfdf' if hover else '#ddd6c3' if pressed else CREAM
  if 'accept' in name:color='#bdd6be' if not pressed else '#83aa89'
  if 'deny' in name:color='#e8c0b8' if not pressed else '#bc8a84'
  panel(a,color)
  if w==24:
   if 'min' in name:a.line([(7,16),(17,16)],INK,2)
   elif 'max' in name:a.rect(6,6,12,11,None,INK,1.5);a.line([(6,9),(18,9)],INK,1)
   else:a.line([(7,7),(17,17)],INK,2);a.line([(17,7),(7,17)],INK,2)
  else:
   label=name.replace('btn_','').replace('start_button','Start').replace('intercom_button','Request')
   for s in ['_normal','_hover','_pressed']:label=label.replace(s,'')
   label={'back_to_office':'< Office'}.get(label,label.replace('_',' ').title())
   a.text(w/2,h*.22+(2 if pressed else 0),label,min(h*.38,28),INK,True,True)
 elif name=='logo_time_sorter':
  a.text(w/2,40,'TIME',130,BLUE,True,True);a.text(w/2,170,'SORTER',140,INK,True,True)
  a.line([(160,345),(1040,345)],GOLD,5)
 elif name.startswith('stamp_'):
  color=TEAL if 'approved' in name else RED
  a.rect(8,8,w-16,h-16,None,color,5,r=8);a.rect(17,17,w-34,h-34,None,color,1.5,r=3)
  a.text(w/2,h*.32,name.replace('stamp_','').upper(),min(w*.095,h*.27),color,True,True)
 elif name.endswith('masthead') or name.endswith('header'):
  title='THE TEMPORAL TIMES' if 'temporal' in name else 'SHIFT LEDGER'
  a.line([(10,h*.14),(w-10,h*.14)],INK,3)
  a.text(w/2,h*.26,title,min(h*.34,w*.055),INK,True,True)
  a.line([(10,h*.84),(w-10,h*.84)],INK,2)
 elif name=='agency_logo':seal(a,'future',BLUE)
 elif name=='scanner_backing':
  a.rect(0,0,w,h,'#233b43')
  for i in range(32,w,64):a.line([(i,0),(i,h)],'#2b454d',1);a.line([(0,i),(w,i)],'#2b454d',1)
  for x,y,sx,sy in [(42,42,1,1),(w-42,42,-1,1),(42,h-42,1,-1),(w-42,h-42,-1,-1)]:a.line([(x+sx*75,y),(x,y),(x,y+sy*75)],'#92b0b4',3)
 elif name in ['page_blank','refbook_page','deviation_report_form','citation_slip','temporal_times_paper','shift_ledger_paper']:
  paper(a)
  if name!='page_blank':
   a.rect(24,24,w-48,h-48,None,'#c8bea6',1)
   if name not in ['refbook_page','temporal_times_paper']:
    for yy in range(150,h-70,85):a.line([(52,yy),(w-52,yy)],'#c9c6b8',1)
 elif name.startswith('refbook_cover'):
  color={'currency':TEAL,'language':BLUE,'technology':'#8d7151'}[name.split('_')[-1]]
  a.rect(4,3,w-8,h-6,color,INK,3,r=8);a.rect(21,19,w-44,h-38,None,GOLD,2,r=3)
  a.line([(38,10),(38,h-10)],'#d0b78a',2)
  glyph(a,name.split('_')[-1],w*.27,h*.19,w*.48,CREAM)
  a.text(w/2,h*.69,name.split('_')[-1].upper(),26,CREAM,True,True)
 elif name.startswith('compare_bar'):
  color={'neutral':'#d8dad4','match':'#c8ddbd','mismatch':'#eed1a2','deviation':'#e7b5af'}[name.split('_')[-1]]
  panel(a,color)
 elif name in ['window_frame','photo_frame','citizen_records_frame','ending_panel']:
  # True transparent interior; only border pixels.
  a.rect(2,2,w-4,h-4,None,INK,2,r=5);a.rect(5,5,w-10,h-10,None,'#e5dcc7',5,r=3)
  a.rect(9,9,w-18,h-18,None,BLUE,1)
 elif name in ['window_titlebar','taskbar','tray_bg']:
  panel(a,BLUE)
  a.line([(4,4),(w-5,4)],'#91afbe',1)
 elif name=='directives_sticky':
  a.poly([(9,8),(w-9,8),(w-9,h-48),(w-48,h-9),(9,h-9)],'#eee2a1',GOLD,1)
  a.poly([(w-48,h-48),(w-9,h-48),(w-48,h-9)],'#d5c77e',GOLD,1)
 elif name in ['panel_expenses','panel_shop','intercom_panel','start_menu_panel','claim_banner']:
  panel(a);a.rect(7,7,w-14,min(38,h*.2),BLUE)
 else:
  raise RuntimeError('Unhandled native asset '+path)
 a.save(path);row[4]='created-native';done.append(path)
(ROOT/'manifest.json').write_text(json.dumps(M,indent=2),encoding='utf-8')
(ROOT/'native-assets.json').write_text(json.dumps(done,indent=2),encoding='utf-8')
print('Created',len(done),'PNG assets and editable SVG sources.')

