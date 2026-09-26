"""Historic machinery, meaningful desk keepsakes and animated-vehicle meshes (art side).

Purpose: the three flying cars (City_AirTaxi, City_AirSedan, City_AirVan: the live traffic
moved by OfficeTrafficVehicle), the board bracket and pin, and the removed exhibits
(printing press, Wright Flyer, rocket, Voyager cover), whose FBX the scene no longer
uses (triage B17).
Inputs: artlib.py (this folder); the exhibit OBJs in Assets/Art/Office/Hybrid/Models
(exhibit_printing_press, exhibit_wright_flyer, exhibit_rocket; keep them while this
script builds the exhibits); Assets/Art/Office/Hybrid/Images/voyager_record.png.
Outputs: Assets/Art/Office/Hybrid/BlenderOffice/Models/{City_Air*,Office_*,Art_*}.fbx
(+ Exports/ copies), Details_Collection_manifest.json and Details_Collection.blend here.
Run: blender --background --python ArtDeliverables/TimeDesk/HybridScene/BlenderOffice/author_details.py
"""
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from artlib import *
material('Historic_Timber','79624C',.93)
material('Historic_WornTimber','A08766',.96)
material('Historic_Iron','485759',.87,.12)
material('Historic_Canvas','C8C3AD',.97)
material('Historic_CanvasPatch','9D9E8B',.98)
material('Historic_Brass','9E9268',.84,.16)
material('Rocket_Ochre','B6A15D',.93)
material('Historic_Rust','876450',.99)
material('Vehicle_Ochre','B5985F',.86)
material('Vehicle_Teal','678581',.86)
material('Vehicle_Red','9D6F62',.86)
material('Vehicle_Glass','3F5961',.80)
material('Vehicle_Light','C9D6C9',.81)
material('Voyager_Gold','B8A46A',.90,.10)
material('Voyager_Diagram','FFFFFF',.99,texture=PROJECT/'Assets/Art/Office/Hybrid/Images/voyager_record.png')

# Preserve established historical geometry; weld face fragments and author
# normals/edge treatments in Blender rather than carrying faceted OBJ shading.
matmap={'timber':'Historic_Timber','timber_wear':'Historic_WornTimber','canvas':'Historic_Canvas','canvas_patch':'Historic_CanvasPatch','metal':'Historic_Iron','ochre':'Rocket_Ochre','brass':'Historic_Brass','rubber':'Recess_Charcoal','ivory_dark':'Plastic_WarmGrey','stone':'Plastic_WarmGrey','rust':'Historic_Rust','paper':'Paper_Label'}
for model,filename in [('Art_PrintingPress','exhibit_printing_press'),('Art_WrightFlyer','exhibit_wright_flyer'),('Art_Rocket','exhibit_rocket')]:
    group(model);verts=[];groups={};part='';mat=''
    for line in (PROJECT/'Assets/Art/Office/Hybrid/Models'/(filename+'.obj')).read_text().splitlines():
        a=line.split()
        if not a:continue
        if a[0]=='v':verts.append(tuple(map(float,a[1:4])))
        elif a[0]=='g':part=a[1]
        elif a[0]=='usemtl':mat=a[1]
        elif a[0]=='f':groups.setdefault((part,mat),[]).append([int(t.split('/')[0])-1 for t in a[1:]])
    for (part,mat),faces in groups.items():
        selected=sorted({i for f in faces for i in f});remap={old:new for new,old in enumerate(selected)}
        o=mesh(part,[verts[i] for i in selected],[[remap[i] for i in f] for f in faces],matmap[mat])
        active(o);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.remove_doubles(threshold=.00004);bpy.ops.mesh.dissolve_limited(angle_limit=.001);bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')
        bevel(o,.0015,2)
    if model=='Art_PrintingPress':
        for x in [-.61,.61]:
            for y in [.36,1.96]:box('Mortise end grain',(x,y,-.032),(.082,.051,.01),'Historic_WornTimber',.001)
        for x in [-.31,.31]:box('Sliding carriage handle',(x,.785,-.91),(.045,.065,.19),'Historic_Timber',.008)
    if model=='Art_Rocket':
        for z in [-.68,.65]:
            for i in range(12):
                a=i*math.tau/12;cylinder('Boiler band rivet',(.414*math.sin(a),.97+.414*math.cos(a),z-.026),.007,.009,'Historic_Iron','z',12)
    if model=='Art_WrightFlyer':
        for x in [-1.45,.92]:
            patch('Muslin repair patch',(x,.886,.12),.15,.14,'Historic_CanvasPatch','top')

group('Art_VoyagerCover')
cylinder('Golden cover rim',(0,0,0),.21,.008,'Voyager_Gold','z',96)
verts=[(0,0,-.005)]+[(.206*math.cos(i*math.tau/96),.206*math.sin(i*math.tau/96),-.005) for i in range(96)]
o=mesh('Stylized engraved face',verts,[(0,i+1,(i+1)%96+1) for i in range(96)],'Voyager_Diagram')
uv=o.data.uv_layers.new(name='RecordUV')
for loop in o.data.loops:
    p=o.data.vertices[loop.vertex_index].co
    uv.data[loop.index].uv=(.509-p.x/.412*.782,.50+p.z/.412*.936)
for x in [-.213,.213]:box('Cover mounting lug',(x,0,.001),(.035,.034,.009),'Voyager_Gold',.002)

group('Office_BoardBracket')
box('Bracket arm',(0,0,0),(.62,.036,.045),'Desk_Metal',.004)
for x in [-.28,.28]:box('Bracket mounting plate',(x,0,.026),(.075,.16,.014),'Desk_Metal',.003)
for x in [-.28,.28]:
    for y in [-.05,.05]:cylinder('Bracket screw',(x,y,.014),.005,.005,'Case_Steel','z',16)
group('Office_Pin')
cylinder('Metal pin',(0,0,0),.001,.021,'Case_Steel','z',12)
cylinder('Push pin head',(0,0,-.011),.005,.006,'Power_Amber','z',20)

def vehicle(name,paint,van=False):
    group(name)
    loft('Long chamfered body',[(-1.9,1.28,.21,.05,.33),(-1.60,1.72,.42,.09,.40),(1.32,1.72,.42,.09,.40),(1.83,1.45,.27,.07,.37)],paint)
    box('Dark sill',(0,.23,0),(1.56,.10,3.24),'Plastic_DeepGrey',.024)
    # Wedge-shaped cabin with a broad windshield and separate side glazing.
    y0=.56;top=1.16 if van else 1.05;rear=1.24 if van else .80
    vv=[(-.68,y0,-.75),(.68,y0,-.75),(.68,y0,rear),(-.68,y0,rear),(-.56,top,-.40),(.56,top,-.40),(.57,top,rear-.18),(-.57,top,rear-.18)]
    o=mesh('Cabin shell',vv,[(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)],paint);bevel(o,.018,3)
    mesh('Sloped windscreen',[(-.597,.625,-.710),(.597,.625,-.710),(.507,top-.046,-.440),(-.507,top-.046,-.440)],[(0,1,2,3)],'Vehicle_Glass')
    for s in [-1,1]:
        for z0,z1 in [(-.31,.18),(.24,rear-.26)]:
            mesh('Side window',[(s*.674,.65,z0),(s*.674,.65,z1),(s*.579,top-.07,z1),(s*.579,top-.07,z0)],[(0,1,2,3) if s<0 else (3,2,1,0)],'Vehicle_Glass')
        for z in [-.95,.98]:
            pod=loft('Lift nacelle',[(-.40,.25,.17,.05,.20),(-.3,.32,.26,.08,.20),(.3,.32,.26,.08,.20),(.40,.25,.17,.05,.20)],'Plastic_DeepGrey');pod.location=v((s*.85,0,z))
            cylinder('Downward lift grille',(s*.85,.091,z),.092,.012,'Historic_Iron','y',32)
        box('Door pull',(s*.689,.59,.1),(.012,.028,.16),'Plastic_DeepGrey',.004)
        for z in [-.38,.66]:tube('Door seam',[(s*.83,.33,z),(s*.83,.55,z)],.003,'Plastic_DeepGrey')
    box('Front bumper',(0,.30,-1.92),(1.38,.078,.08),'Plastic_DeepGrey',.015)
    box('Front grille',(0,.412,-1.871),(.58,.09,.008),'Plastic_DeepGrey',.006)
    for x in [-.52,.52]:
        box('Headlight',(x,.43,-1.863),(.27,.10,.018),'Vehicle_Light',.009)
        box('Rear lamp',(x,.44,1.826),(.25,.075,.018),'Power_Amber',.007)
    if not van:
        box('Blank taxi roof marker',(0,top+.06,.08),(.35,.11,.18),'Plastic_Ivory',.009)
    patch('Broad door scuff',(-.52,.505,-1.731),.21,.02,'Wear_Light')

vehicle('City_AirTaxi','Vehicle_Ochre')
vehicle('City_AirSedan','Vehicle_Teal')
vehicle('City_AirVan','Vehicle_Red',True)
export_library('Details_Collection')
