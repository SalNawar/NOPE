"""Blender hall, display furniture and painting frames. Fixed composition (art side).

Purpose: the hall: floor (the original floor the user restored), structure, banners,
bench, departures board, portal, service cabinet and paperwork (Hall_* groups, all
live). The gallery rail, plinth, display stand and Art_* painting groups still export
FBX the scene no longer uses since the exhibits were removed (triage B17).
Inputs: artlib.py (this folder); Assets/Art/Office/Hybrid/BlenderOffice/Textures/civic_stone.png
and the painting images in Assets/Art/Office/Hybrid/Images.
Outputs: Assets/Art/Office/Hybrid/BlenderOffice/Models/Hall_*.fbx and Art_*.fbx (+ Exports/
copies), Hall_Collection_manifest.json and Hall_Collection.blend here.
Run: blender --background --python ArtDeliverables/TimeDesk/HybridScene/BlenderOffice/author_hall.py
"""
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from artlib import *
stone_texture=OUT/'Textures/civic_stone.png'
material('Hall_Plaster','CEC7AF',.93)
material('Hall_Stone','D9E0DB',.88,texture=stone_texture)
material('Hall_Floor','FFFFFF',.79,texture=stone_texture)
material('Hall_FloorAlt','E3E8E3',.86,texture=stone_texture)
material('Hall_Grout','808B87',.96)
material('Hall_Teal','315D54',.80)
material('Hall_DarkMetal','2D4246',.75,.18)
material('Hall_Wear','7E887F',.98)
material('Hall_Rust','947256',.98)
material('Hall_LightFace','D5DDD5',.80)
material('Frame_Brass','A29570',.65,.45)
material('Frame_Timber','7F6B53',.94)
material('Frame_Chips','ADA48C',.98)
material('Banner_Cloth','36706C',.97)
material('Portal_Paint','BDC1AE',.66,.10)
material('Portal_Orange','B97846',.79)
material('Portal_Coil','537D82',.56,.30)

group('Hall_Structure')
for x in [-14,14]:
    box('Dado wall',(x,.7,12),(.30,1.4,30),'Hall_Teal',.018)
    box('Deep window sill',(x,1.45,12),(.43,.13,30),'Hall_Stone',.01)
    box('Skirting',(x,.12,12),(.34,.24,30),'Hall_DarkMetal',.008)
    for z in [-3,7,17,27]:
        box('Full height pier',(x,7,z),(.70,14,.72),'Hall_Plaster',.025)
        box('Pier base',(x,.40,z),(.83,.8,.83),'Hall_Stone',.012)
        box('Pier cap',(x,12.9,z),(.9,.35,.92),'Hall_Plaster',.015)
        for y in [1.65,6.8,12.8]:box('Window bracket',(x-.24*(1 if x>0 else -1),y,z),(.08,.2,.20),'Hall_DarkMetal',.004)
    box('Ceiling wall beam',(x,13.6,12),(.95,.7,30),'Hall_Plaster',.023)
    for y in [1.6,6.8,12.8]:box('Glazing transom',(x,y,12),(.095,.068,30),'Hall_DarkMetal',.003)
    for z in [2,12,22]:box('Slender glazing mullion',(x,7.2,z),(.082,11.25,.082),'Hall_DarkMetal',.003)
    xx=x-.23 if x>0 else x+.23
    tube('Wall service conduit',[(xx,1.13,-2.9),(xx,1.13,26.8)],.021,'Hall_Rust')
    for z in [1,10,19]:box('Conduit clip',(xx,1.13,z),(.067,.079,.04),'Hall_DarkMetal',.003)
box('Rear dado',(0,.7,27),(28,1.4,.30),'Hall_Teal',.018)
box('Rear sill',(0,1.45,27),(28,.13,.43),'Hall_Stone',.01)
box('Rear skirting',(0,.12,27),(28,.24,.34),'Hall_DarkMetal',.008)
for x in [-14,-7,0,7,14]:
    box('Rear pier',(x,7,27),(.70,14,.72),'Hall_Plaster',.025)
    box('Rear pier shoe',(x,.4,27),(.83,.8,.83),'Hall_Stone',.012)
for x in [-10.5,-3.5,3.5,10.5]:box('Rear window mullion',(x,7.2,27),(.082,11.25,.095),'Hall_DarkMetal',.003)
for y in [1.6,6.8,12.8]:box('Rear glazing transom',(0,y,27),(28,.068,.095),'Hall_DarkMetal',.003)
box('Ceiling',(0,14.2,12),(28,.3,30),'Hall_Plaster',.025)
for z in [-3,7,17,27]:box('Ceiling beam',(0,13.52,z),(28,.65,.46),'Hall_Stone',.02)
for x in [-9,-3,3,9]:
    for z in [5,12,21]:
        box('Ceiling fixture',(x,13.24,z),(1.85,.14,.60),'Hall_DarkMetal',.007)
        for dx in [-.45,0,.45]:box('Light diffuser',(x+dx,13.16,z),(.41,.015,.47),'Hall_LightFace',.003)
for x,y,z,w,h in [(-6.96,2.5,26.632,.33,.44),(7.13,1.3,26.632,.24,.48),(.16,3.2,26.632,.23,.61),(-10.4,.75,26.843,.9,.12)]:patch('Chipped civic paint',(x,y,z),w,h,'Hall_Wear')
# Recessed flutes, collars and brass fixing plates give the existing civic
# structure a consistent built identity. No room dimensions or window openings change.
for x in [-14,-7,0,7,14]:
    box('Pier inset front channel',(x,7.1,26.625),(.40,10.7,.024),'Hall_DarkMetal',.006)
    for dx in [-.135,0,.135]:box('Pier fluted raised rib',(x+dx,7.1,26.59),(.045,10.65,.062),'Hall_Stone',.005)
    for yy in [1.72,6.8,12.60]:box('Civic pier collar',(x,yy,27),(.82,.14,.82),'Hall_Stone',.018)
    box('Pier base inset field',(x,.43,26.576),(.61,.46,.025),'Hall_Teal',.012)
for x in [-10.5,-3.5,3.5,10.5]:
    box('Glazing junction cover',(x,6.8,26.934),(.23,.20,.029),'Hall_DarkMetal',.009)
    for dx in [-.075,.075]:
        for yy in [6.74,6.86]:cylinder('Glazing plate fixing',(x+dx,yy,26.914),.011,.009,'Frame_Brass','z',12)
for side in [-1,1]:
    for z in [-3,7,17,27]:
        xx=side*13.625
        box('Side pier inset channel',(xx,7.1,z),(.024,10.7,.40),'Hall_DarkMetal',.006)
        for dz in [-.135,0,.135]:box('Side pier fluted rib',(xx-side*.033,7.1,z+dz),(.062,10.65,.045),'Hall_Stone',.005)
        for yy in [1.72,6.8,12.60]:box('Side pier civic collar',(side*14,yy,z),(.82,.14,.82),'Hall_Stone',.018)

group('Hall_Floor')
box('Continuous floor bed',(0,-.085,12),(28,.16,30),'Hall_Grout',0)
for x in range(-14,14,2):
    for z in range(-3,27,2):
        mat='Hall_FloorAlt' if (x*7+z*11)%13<3 else 'Hall_Floor'
        slab=box('Large stone floor slab',(x+1,-.004,z+1),(1.987,.012,1.987),mat,.001)
        for old_uv in list(slab.data.uv_layers):slab.data.uv_layers.remove(old_uv)
        uv=slab.data.uv_layers.new(name='StoneSlabUV')
        quarter=(x*13+z*7)%4
        for loop in slab.data.loops:
            co=slab.data.vertices[loop.vertex_index].co
            u,t=co.x/1.987+.5,co.y/1.987+.5
            for _ in range(quarter):u,t=1-t,u
            uv.data[loop.index].uv=(u,t)
for x in [-6,6]:box('Civic floor border',(x,.004,13),(.075,.002,20),'Hall_Teal',0)
for x,z in [(-7.7,8.4),(8.2,10.1),(-9.1,15.1),(5.5,21.8)]:patch('Scuffed stone',(x,.004,z),.62,.19,'Hall_Wear','top')
# Surface damage follows the loaded edges beside the existing exhibits. These
# shallow chips and repaired joints are geometry, with no baked illumination.
for x,z in [(-3.6,12.2),(3.6,12.2),(-6.95,11.25),(-9.3,16.2),(8.8,16.6)]:
    patch('Worn plinth approach',(x,.005,z),.32,.095,'Hall_Wear','top')
for x,z,sgn in [(-4,11,1),(4,11,-1),(-8,15,1),(8,17,-1)]:
    pts=[(x,.006,z+.08),(x+sgn*.15,.006,z+.27),(x+sgn*.11,.006,z+.47),(x+sgn*.31,.006,z+.66)]
    # Flat hairline silhouette, not an inflated tube or a random pile of debris.
    vv=[]
    for xx,yy,zz in pts:vv.extend([(xx-.006,yy,zz),(xx+.006,yy,zz)])
    mesh('Stone edge hairline',vv,[(i,i+1,i+3,i+2) for i in range(0,6,2)],'Hall_Grout')

group('Hall_Banner')
box('Banner crossbar',(0,-3,0),(1.86,.083,.095),'Hall_DarkMetal',.007)
verts=[];faces=[];nx,ny=12,16
for j in range(ny+1):
    for i in range(nx+1):
        t=j/ny;u=i/nx
        verts.append(((u-.5)*1.65,-3.06-4.4*t+(.10 if i==nx else 0)*t**8,.04*math.sin(u*math.pi*5)*(.25+.75*t)))
for j in range(ny):
    for i in range(nx):
        a=j*(nx+1)+i;faces.append((a,a+1,a+nx+2,a+nx+1))
o=mesh('Blank cloth with weighted folds',verts,faces,'Banner_Cloth',True);m=o.modifiers.new('Fabric thickness','SOLIDIFY');m.thickness=.009
# Turned hems follow the same cloth surface. No logos or permanent slogans.
for edge in [0,nx]:
    tube('Stitched side hem',[verts[j*(nx+1)+edge] for j in range(ny+1)],.007,'Banner_Cloth')
tube('Weighted lower hem',[verts[ny*(nx+1)+i] for i in range(nx+1)],.012,'Banner_Cloth')
for x in [-.65,-.325,0,.325,.65]:
    box('Canvas suspension tab',(x,-3.06,.006),(.052,.16,.019),'Banner_Cloth',.007)
for x in [-.65,.65]:tube('Ceiling suspension',[(x,-2.98,0),(x,1.70,0)],.012,'Hall_DarkMetal')

group('Hall_Departures')
box('Departure housing',(0,0,0),(7,2.2,.3),'Hall_DarkMetal',.035)
box('Inset board surround',(0,0,-.16),(6.83,2.03,.06),'Plastic_WarmGrey',.012)
box('Blank destination face',(0,0,-.195),(6.59,1.8,.022),'CRT_Glass',.008)
for y in [-.60,-.30,0,.30,.60]:box('Destination row separator',(0,y,-.209),(6.5,.011,.004),'Plastic_DeepGrey',.001)
for x in [-2.8,2.8]:tube('Board hanging rod',[(x,1.02,0),(x,4.2,0)],.024,'Hall_DarkMetal')

group('Hall_Bench')
for x in [-1.05,-.35,.35,1.05]:
    box('Pressed seat pan',(x,.49,0),(.64,.07,.56),'Hall_Teal',.018)
    back=box('Pressed seat back',(x,.92,.25),(.64,.77,.058),'Hall_Teal',.022);back.rotation_euler.x=math.radians(-6)
    for z in [-.16,.16]:cylinder('Seat fixing',(x,.529,z),.007,.003,'Case_Steel','y',12)
    for zz in [-.22,.22]:tube('Tubular seat bracket',[(x,.42,zz),(x,.42,.22),(x,.86,.33)],.018,'Hall_DarkMetal')
for x in [-1.08,1.08]:
    box('Bench leg',(x,.23,.04),(.065,.46,.36),'Hall_DarkMetal',.006)
    box('Bench foot',(x,.025,.04),(.22,.05,.61),'Hall_DarkMetal',.008)
box('Seat support rail',(0,.39,.09),(2.94,.09,.085),'Hall_DarkMetal',.007)
patch('Seat paint rubbed',(-.43,.529,-.11),.17,.08,'Hall_Wear','top')

def ring(name,cy,inner,outer,z0,z1,mat,n=96):
    verts=[]
    for z,r in [(z0,inner),(z0,outer),(z1,inner),(z1,outer)]:
        for i in range(n):a=i*math.tau/n;verts.append((math.cos(a)*r,cy+math.sin(a)*r,z))
    faces=[]
    for i in range(n):
        j=(i+1)%n
        faces.extend([(i,j,n+j,n+i),(2*n+i,3*n+i,3*n+j,2*n+j),(i,2*n+i,2*n+j,j),(n+i,n+j,3*n+j,3*n+i)])
    o=mesh(name,verts,faces,mat,True);bevel(o,.012,2);return o

group('Hall_Portal')
ring('Continuous ring chassis',3.05,2.38,2.92,-.23,.30,'Hall_DarkMetal')
ring('Inner emitter collar',3.05,2.31,2.45,-.31,.15,'Portal_Coil')
ring('Outer painted lip',3.05,2.84,2.94,-.30,-.21,'Portal_Paint')
ring('Inner rubber seal',3.05,2.29,2.335,-.337,-.22,'Hall_DarkMetal')
ring('Outer protective rolled bead',3.05,2.91,2.97,-.33,-.19,'Portal_Coil')
for i in range(16):
    a=math.tau*i/16;lo=a+.018;hi=a+math.tau/16-.018
    vv=[]
    for z in [-.33,-.25]:
        for r in [2.48,2.80]:
            for k in range(5):t=lo+(hi-lo)*k/4;vv.append((r*math.cos(t),3.05+r*math.sin(t),z))
    ff=[]
    for k in range(4):ff.extend([(k,k+1,k+6,k+5),(10+k,15+k,16+k,11+k)])
    ff.extend([(0,5,15,10),(4,14,19,9)])
    for k in range(4):ff.extend([(k,10+k,11+k,k+1),(5+k,6+k,16+k,15+k)])
    ob=mesh('Service access segment',vv,ff,'Portal_Orange' if i%4==0 else 'Portal_Paint');bevel(ob,.009,2)
    mid=(lo+hi)/2
    for r in [2.54,2.74]:cylinder('Captive cover bolt',(r*math.cos(mid),3.05+r*math.sin(mid),-.338),.018,.008,'Hall_DarkMetal','z',12)
    if i%2:
        for delta in [-.05,0,.05]:
            t=mid+delta;tube('Cooling aperture',[(2.58*math.cos(t),3.05+2.58*math.sin(t),-.34),(2.69*math.cos(t),3.05+2.69*math.sin(t),-.34)],.008,'Hall_DarkMetal')
    # Physical junction covers and retaining straps give readable depth at
    # the scene camera, instead of a ring made only of flat coloured segments.
    if i%4==0:
        x,y=2.655*math.cos(mid),3.05+2.655*math.sin(mid)
        cap=box('Portal junction housing',(x,y,-.378),(.29,.19,.105),'Portal_Orange',.026)
        cap.rotation_euler.y=-mid
        for sign in [-1,1]:
            bx=x+sign*.10*math.cos(mid);by=y+sign*.10*math.sin(mid)
            cylinder('Junction captive screw',(bx,by,-.438),.015,.009,'Hall_DarkMetal','z',16)
    for delta in [-.072,.072]:
        t=mid+delta
        tube('Emitter rib',[(2.335*math.cos(t),3.05+2.335*math.sin(t),-.355),(2.435*math.cos(t),3.05+2.435*math.sin(t),-.355)],.012,'Portal_Paint')
box('Portal plinth',(0,.10,0),(6.65,.20,1.55),'Hall_Stone',.024)
for x in [-2.15,2.15]:
    box('Ring bearing shoe',(x,.43,.035),(.63,.57,.84),'Hall_DarkMetal',.025)
    box('Bearing front plate',(x,.41,-.406),(.49,.34,.032),'Portal_Paint',.009)
    for dx in [-.175,.175]:
        for yy in [.30,.51]:cylinder('Bearing shoe bolt',(x+dx,yy,-.426),.024,.016,'Hall_DarkMetal','z',20)
tube('Power umbilical',[(2.82,1.68,.25),(3.30,1.05,.32),(3.35,.14,.35),(4.2,.10,.37)],.060,'Cable_Rubber')

group('Hall_ServiceCabinet')
box('Portal cabinet',(0,1.1,0),(1.05,2.2,.65),'Hall_Teal',.017)
box('Hinged cabinet door',(0,1.05,-.342),(.95,1.94,.028),'Portal_Paint',.006)
box('Blank diagnostic recess',(0,1.66,-.363),(.64,.32,.025),'Plastic_DeepGrey',.004)
box('Blank diagnostic glass',(0,1.66,-.379),(.57,.255,.010),'CRT_Glass',.003)
box('Door latch',(.35,1.07,-.384),(.046,.22,.035),'Hall_DarkMetal',.005)
for y in [.30,.40,.50,.60]:box('Cabinet louver',(0,y,-.365),(.66,.026,.012),'Plastic_DeepGrey',.002)
for x,mat in [(-.21,'Indicator_Green'),(0,'Power_Amber'),(.21,'Plastic_DeepGrey')]:cylinder('Service switch',(x,1.34,-.37),.024,.025,mat,'z',24)
patch('Cabinet scuff',(-.32,.24,-.363),.20,.034,'Hall_Wear')

group('Hall_Plinth')
box('Exhibit pedestal',(0,.42,0),(.88,.84,.88),'Hall_Stone',.012)
box('Exhibit top',(0,.865,0),(1,.07,1),'Hall_Plaster',.008)
box('Pedestal upper shadow reveal',(0,.813,0),(.90,.022,.90),'Hall_DarkMetal',.003)
box('Pedestal stepped top moulding',(0,.831,0),(.955,.018,.955),'Hall_Plaster',.004)
box('Pedestal skirting',(0,.045,0),(.96,.09,.96),'Hall_DarkMetal',.007)
box('Blank accession plate',(0,.64,-.444),(.35,.11,.006),'Frame_Brass',.002)
for x in [-.148,.148]:cylinder('Accession plate fixing',(x,.64,-.449),.008,.005,'Hall_DarkMetal','z',12)
patch('Chipped plinth face',(-.32,.29,-.442),.20,.33,'Hall_Wear')

group('Hall_DisplayStand')
box('Display stand base',(0,.035,0),(.7,.07,.48),'Hall_DarkMetal',.008)
box('Telescoping post',(0,.74,0),(.064,1.48,.064),'Hall_DarkMetal',.006)
box('Display vertical bracket',(0,1.67,0),(.09,.75,.07),'Hall_DarkMetal',.005)
for x in [-.21,.21]:box('Picture support',(x,1.4,-.04),(.054,.025,.15),'Hall_DarkMetal',.004)
box('Cross rail',(0,1.44,.01),(.54,.05,.06),'Hall_DarkMetal',.005)

# Two coherent, open display rails replace scattered single-picture posts.
# Their narrow structure preserves the windows and makes the mounting legible.
group('Hall_GalleryRail')
for x in [-2.85,0,2.85]:
    box('Gallery rail foot',(x,.035,.10),(.46,.07,.84),'Hall_DarkMetal',.014)
    box('Gallery upright',(x,1.48,.12),(.070,2.90,.070),'Hall_DarkMetal',.007)
    for z in [-.25,.45]:cylinder('Foot bolt',(x,.076,z),.012,.009,'Frame_Brass','y',16)
for y in [1.24,2.89]:
    box('Continuous picture mounting rail',(0,y,.12),(5.76,.063,.074),'Hall_DarkMetal',.009)
for x in [-2.3,-1.65,-.35,.30,1.65,2.25]:
    tube('Picture hanging wire',[(x,2.9,.10),(x,1.47,.01)],.008,'Hall_DarkMetal')

# Texture is mapped only to the canvas. Existing painted frames/background are
# excluded by UV coordinates, replaced with dimensional mitered wooden frames.
paintings=[('MonaLisa','mona_lisa-flat-v2.png',.99,1.2,[(.192,.84),(.810,.84),(.810,.133),(.192,.133)]),
 ('PearlEarring','pearl_earring.png',.84,1.26,[(.142,.835),(.858,.835),(.858,.166),(.142,.166)]),
 ('StarryNight','starry_night.png',2.37,1.45,[(.119,.795),(.88,.795),(.88,.166),(.119,.166)]),
 ('GreatWave','great_wave.png',2.30,1.45,[(.124,.827),(.879,.827),(.879,.15),(.124,.15)]),
 ('Milkmaid','milkmaid.png',.82,1.04,[(.239,.801),(.780,.833),(.775,.213),(.252,.184)]),
 ('Mondrian','mondrian.png',1.28,1.28,[(.164,.80),(.82,.80),(.834,.181),(.161,.168)])]
for name,file,w,h,uvcorners in paintings:
    group('Art_'+name);mat='Canvas_'+name;material(mat,'FFFFFF',.99,texture=PROJECT/'Assets/Art/Office/Hybrid/Images'/file)
    box('Stretcher back',(0,0,.035),(w,h,.055),'Frame_Timber',.002)
    face=mesh('Painted canvas',[(-w/2,-h/2,0),(w/2,-h/2,0),(w/2,h/2,0),(-w/2,h/2,0)],[(0,1,2,3)],mat)
    uv=face.data.uv_layers.new(name='CanvasUV')
    # Image corners specified with top-origin pixels: bottom left/right then top.
    for loop in face.data.loops:uv.data[loop.index].uv=(uvcorners[loop.vertex_index][0],1-uvcorners[loop.vertex_index][1])
    bw=.07
    if name!='Mondrian':
        # One continuous profile makes true mitred corners and a recessed inner
        # reveal. Broad moulding planes stay legible with the flat painted art.
        vv=[]
        for d,z in [(0,-.012),(.009,-.038),(.015,-.051),(.038,-.053),(.046,-.064),(.061,-.069),(.073,-.048),(.073,.032)]:
            vv.extend([(-w/2-d,-h/2-d,z),(w/2+d,-h/2-d,z),(w/2+d,h/2+d,z),(-w/2-d,h/2+d,z)])
        ff=[(j*4+i,j*4+(i+1)%4,(j+1)*4+(i+1)%4,(j+1)*4+i) for j in range(7) for i in range(4)]
        mould=mesh('Mitred stepped picture moulding',vv,ff,'Frame_Brass' if name!='GreatWave' else 'Frame_Timber')
        bevel(mould,.0018,2)
    else:
        for sx in [-1,1]:
            broken=sx==1
            box('Frame upright',(sx*(w/2+bw/2),.065 if broken else 0,-.015),(bw,h+2*bw-(.13 if broken else 0),.10),'Frame_Brass',.006)
        for sy in [-1,1]:
            broken=sy==-1
            box('Frame crosspiece',(-.055 if broken else 0,sy*(h/2+bw/2),-.015),(w-(.11 if broken else 0),bw,.10),'Frame_Brass',.006)
    if name=='Mondrian':
        fragment=box('Detached frame corner',(w/2+.028,-h/2-.064,-.016),(.15,.073,.093),'Frame_Timber',.003)
        fragment.rotation_euler.y=math.radians(24)
    patch('Frame edge loss',(-w/2-.02,h*.38,-.067),.063,.13,'Frame_Chips')
    box('Blank museum label',(0,-h/2-.035,-.068),(min(.28,w*.4),.035,.003),'Frame_Brass',.002)
    if name in ['StarryNight','Mondrian']:
        patch('Broken frame splinter',(w/2+.03,h/2+.018,-.069),.081,.12,'Frame_Timber')
        tape=box('Old conservation tape',(w/2,h/2-.02,-.073),(.17,.041,.002),'Paper_Label',.0005);tape.rotation_euler.y=math.radians(28)

group('Hall_Paperwork')
for i,(x,z,a) in enumerate([(-.15,0,-.2),(.05,.11,.16),(.15,-.08,-.37)]):
    vv=[(-.15,0,-.21),(.15,.006,-.21),(.15,.013,.19),(-.13,.02,.21)]
    ob=mesh('Discarded sheet',[(xx+x,yy+i*.003,zz+z) for xx,yy,zz in vv],[(0,1,2,3)],'Paper_Label');m=ob.modifiers.new('Paper thickness','SOLIDIFY');m.thickness=.0008

export_library('Hall_Collection')
