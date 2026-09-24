"""Offline asset authoring only. Writes OBJ/MTL and animated GLB; never touches Unity.

Units: metres; Y up; booth camera looks toward +Z. Meshes are editable base meshes,
not final sculpted art. Screen groups are deliberately separate and unlettered.
"""
from pathlib import Path
import math, json, struct
from collections import defaultdict

ROOT = Path(__file__).parent / 'Generated'
MODELS = ROOT / 'Models'
MODELS.mkdir(parents=True, exist_ok=True)
COLORS = {
 'cream':(.64,.59,.46), 'cream_light':(.79,.74,.61), 'teal':(.12,.29,.30),
 'metal':(.22,.26,.27), 'dark':(.07,.11,.14), 'screen':(.09,.17,.22),
 'wood':(.43,.24,.12), 'ochre':(.61,.39,.12), 'blue':(.14,.27,.34),
 'paper':(.76,.72,.61), 'floor':(.53,.53,.46), 'floor_teal':(.35,.44,.42),
 'floor_rust':(.56,.36,.21), 'concrete':(.49,.51,.49), 'rust':(.49,.25,.17),
 'glass':(.17,.26,.29), 'key':(.64,.63,.56), 'green':(.23,.42,.31),
}

def rot(p,a):
 a=math.radians(a);x,y,z=p
 return (x*math.cos(a)+z*math.sin(a),y,-x*math.sin(a)+z*math.cos(a))

class Mesh:
 def __init__(self,name):self.name=name;self.parts=[]
 def face(self,name,mat,points): self.parts.append((name,mat,list(points)))
 def box(self,name,c,s,mat,a=0):
  x,y,z=c;w,h,d=[q/2 for q in s]
  v=[(-w,-h,-d),(w,-h,-d),(w,h,-d),(-w,h,-d),(-w,-h,d),(w,-h,d),(w,h,d),(-w,h,d)]
  v=[rot(p,a) for p in v];v=[(p[0]+x,p[1]+y,p[2]+z) for p in v]
  for f in [(0,3,2,1),(4,5,6,7),(0,4,7,3),(1,2,6,5),(0,1,5,4),(3,7,6,2)]:
   self.face(name,mat,[v[i] for i in f])
 def cylinder(self,name,c,r,h,mat,n=16):
  x,y,z=c
  lo=[(x+r*math.cos(i*2*math.pi/n),y-h/2,z+r*math.sin(i*2*math.pi/n)) for i in range(n)]
  hi=[(p[0],y+h/2,p[2]) for p in lo]
  self.face(name,mat,lo);self.face(name,mat,list(reversed(hi)))
  for i in range(n):j=(i+1)%n;self.face(name,mat,[lo[i],hi[i],hi[j],lo[j]])
 def panel(self,name,c,w,h,mat):
  x,y,z=c;self.face(name,mat,[(x-w/2,y-h/2,z),(x-w/2,y+h/2,z),(x+w/2,y+h/2,z),(x+w/2,y-h/2,z)])
 def frustum(self,name,c,w,h,d,rw,rh,mat):
  x,y,z=c
  v=[(x-w/2,y-h/2,z-d/2),(x+w/2,y-h/2,z-d/2),(x+w/2,y+h/2,z-d/2),(x-w/2,y+h/2,z-d/2),
     (x-rw/2,y-rh/2,z+d/2),(x+rw/2,y-rh/2,z+d/2),(x+rw/2,y+rh/2,z+d/2),(x-rw/2,y+rh/2,z+d/2)]
  for f in [(0,3,2,1),(4,5,6,7),(0,4,7,3),(1,2,6,5),(0,1,5,4),(3,7,6,2)]: self.face(name,mat,[v[i] for i in f])
 def add(self,other,pos=(0,0,0),yaw=0,scale=1,prefix=''):
  for name,mat,pts in other.parts:
   vs=[rot(tuple(v*scale for v in p),yaw) for p in pts]
   self.face(prefix+name,mat,[(p[0]+pos[0],p[1]+pos[1],p[2]+pos[2]) for p in vs])
 def write(self):
  out=['# Time Sorter editable base mesh; metres, Y-up','mtllib palette.mtl'];idx=1
  last=None
  for name,mat,pts in self.parts:
   if last!=(name,mat):out += ['g '+name,'usemtl '+mat];last=(name,mat)
   out+=['v %.6f %.6f %.6f'%p for p in pts]
   # Local planar UVs let blank screens and picture planes accept runtime content.
   spans=[max(p[a] for p in pts)-min(p[a] for p in pts) for a in range(3)]
   axes=sorted(range(3),key=lambda a:spans[a],reverse=True)[:2]
   if 0 in axes and 1 in axes:axes=[0,1]
   elif 0 in axes and 2 in axes:axes=[0,2]
   else:axes=[2,1]
   lows=[min(p[a] for p in pts) for a in axes]
   for p in pts:out.append('vt %.6f %.6f'%tuple((p[a]-lows[j])/(spans[a] or 1) for j,a in enumerate(axes)))
   out.append('f '+' '.join('%d/%d'%(idx+i,idx+i) for i in range(len(pts))));idx+=len(pts)
  (MODELS/(self.name+'.obj')).write_text('\n'.join(out)+'\n')
  return {'file':self.name+'.obj','faces':len(self.parts),'vertices':idx-1,'parts':sorted(set(p[0] for p in self.parts))}

models={}
def model(name):m=Mesh(name);models[name]=m;return m

# Hall surfaces, structural members and open window bays are genuinely separate.
m=model('hall_floor')
for x in range(-14,14):
 for z in range(-3,27):
  mat='floor_teal' if (x+z)%7==0 else 'floor_rust' if (x*13+z*7)%41==0 else 'floor'
  m.box('floor_tiles',(x+.5,-.075,z+.5),(.996,.15,.996),mat)
m=model('hall_structure')
for x in [-14,14]:
 m.box('side_dado',(x,1,12),(0.28,2,30),'teal')
 for z in [-3,4.5,12,19.5,27]:
  m.box('side_piers',(x,7,z),(.75,14,.75),'cream_light')
 m.box('side_header',(x,13.65,12),(.9,.7,30),'cream')
 for y in [2.15,6,10,13.1]:m.box('side_window_rails',(x,y,12),(.16,.12,30),'metal')
 for z in [i*2.5-3 for i in range(13)]:m.box('side_window_mullions',(x,7.6,z),(.12,10.9,.12),'metal')
m.box('rear_dado',(0,1,27),(28,2,.28),'teal')
for x in [-14,-7,0,7,14]:m.box('rear_piers',(x,7,27),(.75,14,.75),'cream_light')
for x in range(-14,15,2):m.box('rear_mullions',(x,7.6,27),(.12,10.9,.12),'metal')
for y in [2.15,6,10,13.1]:m.box('rear_rails',(0,y,27),(28,.12,.16),'metal')
m.box('ceiling',(0,14.2,12),(28,.3,30),'cream')
for z in [-3,4.5,12,19.5,27]:m.box('ceiling_beams',(0,13.5,z),(28,.65,.45),'concrete')
for x in [-9,-3,3,9]:
 for z in [5,12,19,25]:m.box('ceiling_light_faces',(x,13.22,z),(1.9,.06,.65),'cream_light')

# One reusable banner, instantiated exactly four times in the composition.
m=model('banner')
m.box('top_crossbar',(0,0,0),(1.85,.12,.12),'metal')
m.box('cloth_blank',(0,-2.3,0),(1.65,4.5,.025),'teal')
m.box('bottom_hem',(0,-4.56,0),(1.72,.07,.07),'metal')
for x in [-.65,.65]:m.cylinder('ceiling_suspension',(x,.9,0),.018,1.8,'metal',6)
m=model('departure_board')
m.box('housing',(0,0,0),(7,2.2,.3),'metal');m.panel('runtime_departures',(0,0,-.157),6.6,1.8,'screen')
for x in [-2.8,2.8]:m.cylinder('ceiling_rods',(x,2,0),.035,4,'metal',8)

# Portal: ring and opening exported separately for activation and material animation.
m=model('portal_ring')
for i in range(12):
 a0=i*2*math.pi/12+.012;a1=(i+1)*2*math.pi/12-.012
 ro=2.9;ri=2.35;cy=3.05
 v=[(r*math.cos(a),cy+r*math.sin(a),z) for z in [-.32,.32] for r,a in [(ri,a0),(ro,a0),(ro,a1),(ri,a1)]]
 mat='ochre' if i%3==0 else 'cream'
 for f in [(0,3,2,1),(4,5,6,7),(0,1,5,4),(3,7,6,2),(1,2,6,5),(0,4,7,3)]:m.face('ring_segment_%02d'%i,mat,[v[j] for j in f])
m.box('platform',(0,.1,0),(6.1,.2,1.6),'metal')
for x in [-2.1,2.1]:m.box('ring_support',(x,.35,.1),(.7,.7,1.2),'metal')
m=model('portal_surface');n=48
m.face('animated_opening','screen',[(2.34*math.cos(i*2*math.pi/n),3.05+2.34*math.sin(i*2*math.pi/n),0) for i in reversed(range(n))])
m=model('portal_cabinet')
m.box('cabinet',(0,1.1,0),(1.05,2.2,.65),'metal');m.panel('blank_diagnostic',(0,1.65,-.331),.65,.3,'screen')
for y in [.25,.4,.55,.7]:m.box('vent',(0,y,-.335),(.7,.045,.025),'dark')

m=model('public_bench')
for x in [-1.05,-.35,.35,1.05]:
 m.box('seat',(x,.5,0),(.63,.1,.57),'blue');m.box('seat_back',(x,.95,.25),(.63,.83,.1),'blue')
for x in [-1.1,1.1]:m.box('leg',(x,.24,0),(.09,.48,.5),'metal')
m.box('rail',(0,.38,0),(2.9,.09,.1),'metal')
m=model('exhibit_plinth');m.box('base',(0,.45,0),(1,.9,1),'concrete');m.panel('blank_caption',(0,.6,-.506),.46,.16,'ochre')
m=model('frame')
for x in [-.51,.51]:m.box('frame_side',(x,.7,0),(.06,1.46,.05),'ochre')
for y in [-.015,1.415]:m.box('frame_end',(0,y,0),(1.08,.06,.05),'ochre')
m.panel('picture_surface',(0,.7,-.028),.98,1.37,'paper')

# Desk and short panels. No camera-dependent painted geometry.
m=model('desk')
m.box('top',(0,1,0),(5.8,.12,1.8),'wood');m.box('front_edge',(0,.88,-.84),(5.8,.14,.12),'wood')
for x in [-2.65,2.65]:m.box('leg',(x,.45,.15),(.17,.9,1.25),'metal')
m=model('partition')
m.box('solid_panel',(0,1.18,0),(.08,2.36,2.05),'teal')
for z in [-1.06,1.06]:m.box('terminal_post',(0,1.2,z),(.14,2.4,.14),'metal')
m.box('cap',(0,2.4,0),(.15,.1,2.25),'metal')

m=model('crt_monitor')
m.box('swivel_base',(0,.055,.02),(.72,.11,.5),'cream')
m.cylinder('swivel',(0,.14,.06),.17,.12,'metal')
m.frustum('tube_casing',(0,.60,.04),.9,.75,.7,.55,.53,'cream_light')
m.box('front_bezel',(0,.6,-.328),(.91,.76,.075),'cream')
m.panel('crt_screen_4_3',(0,.62,-.37),.68,.51,'screen')
m.box('lower_controls',(.30,.30,-.37),(.08,.025,.016),'metal')
for z in [.03,.08,.13,.18,.23]:m.box('tube_vent',(.323,.65,z),(.013,.24,.021),'dark')
m=model('keyboard')
m.box('keyboard_body',(0,.027,0),(.75,.054,.26),'cream')
for row in range(5):
 for col in range(16):
  if row==0 and 4<=col<=10:continue
  m.box('keys',((col-7.5)*.043,.065,(row-2)*.041),(.038,.027,.035),'key')
m.box('spacebar',(0,.065,-.082),(.29,.027,.035),'key')

m=model('credits_till')
m.box('drawer',(0,.06,0),(.60,.12,.48),'cream')
m.frustum('till_body',(0,.20,.035),.60,.26,.43,.58,.22,'cream_light')
for row in range(4):
 for col in range(5):m.box('till_keys',((col-2)*.065,.342,(row-1.5)*.058-.09),(.052,.025,.046),'key' if col<4 else 'teal')
m.box('readout_housing',(0,.46,.15),(.38,.18,.12),'metal');m.panel('runtime_credits',(0,.46,.083),.32,.12,'screen')
m=model('next_sign')
m.box('sign_base',(0,.025,0),(.65,.05,.22),'ochre');m.box('sign_casing',(0,.14,.02),(.60,.23,.075),'ochre')
m.panel('runtime_next',(0,.14,-.021),.54,.17,'screen')
m=model('digital_clock')
m.box('clock_casing',(0,.11,0),(.40,.22,.10),'cream');m.panel('runtime_clock',(0,.11,-.052),.33,.14,'screen')
m=model('stability_device')
m.box('casing',(0,.19,0),(.40,.38,.11),'cream');m.panel('runtime_stability',(0,.23,-.057),.29,.19,'screen')
for x in [-.09,.09]:m.box('indicator',(x,.07,-.059),(.04,.02,.01),'metal')
m=model('calendar')
m.box('calendar_backing',(0,.20,0),(.25,.40,.025),'cream');m.panel('runtime_calendar',(0,.20,-.014),.23,.37,'paper')

m=model('folded_newspaper')
for i in range(4):m.box('folded_pages',(i*.002,.009+i*.004,0),(.23,.005,.32),'paper')
# Untextured sheet is intentional; article/obscure-photo artwork is a separate surface.
m=model('hover_vehicle')
m.frustum('body',(0,.35,0),1.8,.42,3.8,1.55,.35,'ochre')
m.frustum('cabin',(0,.77,.1),1.5,.45,1.8,1.25,.34,'ochre')
m.panel('front_glass',(0,.79,-.811),1.15,.23,'glass')
for x in [-.65,.65]:
 for z in [-1.3,1.3]:m.cylinder('lift_pod',(x,.05,z),.24,.22,'metal',8)

m=model('near_megablock')
for y,w,d,h in [(8,22,20,16),(24,17,17,16),(36,26,21,8),(48,16,15,16),(59,21,18,6),(68,12,13,12)]:
 m.box('megablock_mass',(0,y,0),(w,h,d),'concrete')
 for yy in [y-h*.25,y+h*.25]:
  m.box('front_window_band',(0,yy,-d/2-.02),(w-.6,.60,.10),'dark')
  m.box('rear_window_band',(0,yy,d/2+.02),(w-.6,.60,.10),'dark')
for x in [-7,7]:m.frustum('buttress',(x,12,-9.5),4,24,5,2.5,24,'cream')
m.box('accent_band',(0,35.2,-10.56),(25,.85,.09),'teal')
m.panel('runtime_billboard',(0,40,-10.61),9,5,'cream')

# Palette: no baked lights, textures or shadows.
(MODELS/'palette.mtl').write_text('\n'.join('newmtl %s\nKd %.4f %.4f %.4f\nKa 0 0 0\nKs 0 0 0\nNs 1\nd 1\nillum 2\n'%(k,*v) for k,v in COLORS.items()))
manifest=[m.write() for m in models.values()]

# Editable placement map and an assembled base-mesh scene for DCC review.
placements=[]
def place(model_name,name,pos=(0,0,0),yaw=0,scale=1):
 placements.append(dict(model=model_name,name=name,position=pos,yaw_degrees=yaw,scale=scale))
place('hall_floor','HallFloor');place('hall_structure','HallStructure')
for x,z in [(-8,13),(-4.5,18),(4.5,18),(8,13)]:place('banner','Banner_'+str(x),(x,11.8,z))
place('departure_board','Departures',(0,9.1,19))
place('portal_ring','PortalRing',(0,0,20));place('portal_surface','PortalOpening',(0,0,20.03));place('portal_cabinet','PortalCabinet',(4.1,0,20))
for x in [-11.5,11.5]:
 for z in [9,16]:place('public_bench','Bench_'+str(x)+'_'+str(z),(x,0,z),90 if x<0 else -90)
place('desk','Desk');place('partition','LeftPartition',(-2.95,0,.15));place('partition','RightPartition',(2.95,0,.15))
place('crt_monitor','CRT',(1.65,1.06,.13),-16);place('keyboard','Keyboard',(1.6,1.06,-.55),-8)
place('credits_till','Till',(-1.75,1.06,.1),10);place('next_sign','Next',(0,1.06,.23))
place('digital_clock','Clock',(-2.86,2.05,.1),90);place('stability_device','Stability',(2.86,1.95,.18),-90)
place('calendar','DayCalendar',(-2.86,1.45,.4),90);place('folded_newspaper','Newspaper',(-2.1,1.06,-.53),-8)
for x,z,scale in [(-35,50,.8),(-10,95,1),(25,80,1.1),(45,30,.8),(-50,5,.65)]:place('near_megablock','Megablock_'+str(x),(x,-15,z),0,scale)
for i,(x,z) in enumerate([(-10,8),(-9,15),(-7,22),(10,9),(9,16),(7,23)]):place('exhibit_plinth','ExhibitAnchor_'+str(i),(x,0,z))
assembly=Mesh('assembled_base_geometry')
for p in placements:assembly.add(models[p['model']],p['position'],p['yaw_degrees'],p['scale'],p['name']+'__')
manifest.append(assembly.write())
scene={'status':'Base meshes only; no Unity scene changed or tested','units':'metres','axis':'Y up, view +Z',
 'camera_proposal':{'position':[0,1.8,-3.0],'look_at':[0,3.5,20],'vertical_fov_degrees':58,'aspect':16/9},
 'placements':placements,'traffic_routes':[{'start':[-65,12,50],'end':[65,12,50],'seconds':24},{'start':[65,18,75],'end':[-65,18,75],'seconds':34}],
 'dynamic_surfaces':['runtime_clock','runtime_stability','runtime_credits','runtime_next','runtime_departures','runtime_billboard','runtime_calendar','crt_screen_4_3','animated_opening'],
 'not_implemented':['Unity import/wiring','history-driven variants','shift-clock lighting','sculpted exhibit meshes','runtime animation controller']}
(ROOT/'scene_layout.json').write_text(json.dumps(scene,indent=2))
(ROOT/'mesh_manifest.json').write_text(json.dumps(manifest,indent=2))

# GLB vehicle animation demonstration: independent motion, not painted into skyline.
def write_traffic_glb(mesh,path):
 data=bytearray();views=[];accessors=[]
 def acc(vals,typ,components,component_type=5126):
  while len(data)%4:data.append(0)
  start=len(data);flat=[x for row in vals for x in (row if isinstance(row,(tuple,list)) else [row])]
  data.extend(struct.pack('<'+'f'*len(flat),*flat))
  views.append({'buffer':0,'byteOffset':start,'byteLength':len(data)-start})
  a={'bufferView':len(views)-1,'componentType':component_type,'count':len(vals),'type':typ}
  if typ=='VEC3':a['min']=[min(v[i] for v in vals) for i in range(3)];a['max']=[max(v[i] for v in vals) for i in range(3)]
  if typ=='SCALAR':a['min']=[min(vals)];a['max']=[max(vals)]
  accessors.append(a);return len(accessors)-1
 mats=list(COLORS);prims=[]
 for mat in mats:
  pts=[];norms=[]
  for _,mt,face in mesh.parts:
   if mt!=mat:continue
   for i in range(1,len(face)-1):
    tri=[face[0],face[i],face[i+1]];a=[tri[1][k]-tri[0][k] for k in range(3)];b=[tri[2][k]-tri[0][k] for k in range(3)]
    n=(a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]);l=math.sqrt(sum(q*q for q in n)) or 1
    pts+=tri;norms += [tuple(q/l for q in n)]*3
  if pts:prims.append({'attributes':{'POSITION':acc(pts,'VEC3',3),'NORMAL':acc(norms,'VEC3',3)},'material':mats.index(mat)})
 times=acc([0.,12.,24.],'SCALAR',1);motion=acc([(-25,0,0),(0,0,0),(25,0,0)],'VEC3',3)
 doc={'asset':{'version':'2.0','generator':'Time Sorter offline asset authoring'},'scene':0,'scenes':[{'nodes':[0]}],
 'nodes':[{'name':'HoverVehicle_traffic_demo','mesh':0}],'meshes':[{'primitives':prims}],
 'materials':[{'name':k,'pbrMetallicRoughness':{'baseColorFactor':[*COLORS[k],1],'metallicFactor':0,'roughnessFactor':1}} for k in mats],
 'buffers':[{'byteLength':len(data)}],'bufferViews':views,'accessors':accessors,
 'animations':[{'name':'LaneTraverse_24s','samplers':[{'input':times,'output':motion,'interpolation':'LINEAR'}],'channels':[{'sampler':0,'target':{'node':0,'path':'translation'}}]}]}
 j=json.dumps(doc,separators=(',',':')).encode();j+=b' '*((-len(j))%4);data+=b'\0'*((-len(data))%4)
 path.write_bytes(struct.pack('<4sII',b'glTF',2,12+8+len(j)+8+len(data))+struct.pack('<I4s',len(j),b'JSON')+j+struct.pack('<I4s',len(data),b'BIN\0')+data)
write_traffic_glb(models['hover_vehicle'],MODELS/'hover_vehicle_animated.glb')
print('Authored',len(manifest),'OBJ files and one animated vehicle GLB in',MODELS)
