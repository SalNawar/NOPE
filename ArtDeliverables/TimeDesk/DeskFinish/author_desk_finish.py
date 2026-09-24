"""Editable foreground art with individually mapped painted surfaces."""
import sys,math,json
from pathlib import Path
HERE=Path(__file__).resolve().parent;PROJECT=HERE.parents[2]
sys.path.insert(0,str(HERE.parent/'HybridScene/BlenderOffice'))
import artlib as A
from artlib import *
HERE=Path(__file__).resolve().parent;PROJECT=HERE.parents[2]
A.HERE=HERE;A.PROJECT=PROJECT;A.OUT=PROJECT/'Assets/Art/Office/DeskFinish'
for sub in ['Models','Textures']:(A.OUT/sub).mkdir(parents=True,exist_ok=True)
(HERE/'Exports').mkdir(exist_ok=True)
T=A.OUT/'Textures'
material('Finish_Ivory','FFFFFF',.74,texture=T/'ivory_plastic.png')
material('Finish_WarmGrey','CAC9C3',.80,texture=T/'ivory_plastic.png')
material('Finish_Enamel','C2E1D9',.76,texture=T/'petrol_enamel.png')
material('Finish_EnamelDark','9DAEAA',.80,texture=T/'petrol_enamel.png')
material('Finish_Charcoal','313E40',.88)
material('Finish_KeyGrey','9EAA9F',.80)
material('Finish_KeyCream','DBD1B7',.77)
material('Finish_Coral','B77850',.78)
material('Finish_Brass','AE926A',.70,.12)
material('Finish_BlackRubber','343834',.92)
material('Finish_Glass','243D43',.31)
material('Finish_Paper','D5C7A9',.96)
material('Finish_Ink','3E4944',.97)
material('Finish_Wood','735444',.91)
material('Finish_WoodEdge','987457',.92)
material('Finish_LampInner','D8CDB2',.75)
lamp_emission=(1.0,.72,.42)
lamp_shader=materials['Finish_LampInner'].node_tree.nodes.get('Principled BSDF')
lamp_shader.inputs['Emission Color'].default_value=(*lamp_emission,1)
lamp_shader.inputs['Emission Strength'].default_value=.35
A.specs['Finish_LampInner']['emission']={'color':list(lamp_emission),'intensity':.35}
material('Finish_Mat','A9CAFF',.96,texture=T/'inspection_mat.png')
# A restrained cool tint counteracts the orange bias of the generated veneer.
wood_material=materials['Desk_Walnut']
wood_tint=tuple(int('BEC6CE'[i:i+2],16)/255 for i in (0,2,4))
wood_shader=wood_material.node_tree.nodes.get('Principled BSDF')
wood_shader.inputs['Base Color'].default_value=(*[linear(c) for c in wood_tint],1)
wood_material.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.74
wood_texture=next(node for node in wood_material.node_tree.nodes if node.type=='TEX_IMAGE')
wood_texture.image=bpy.data.images.load(str(T/'walnut_veneer.png'))
wood_multiply=wood_material.node_tree.nodes.new('ShaderNodeMixRGB');wood_multiply.blend_type='MULTIPLY';wood_multiply.inputs[0].default_value=1
wood_multiply.inputs[2].default_value=(*[linear(c) for c in wood_tint],1)
wood_material.node_tree.links.new(wood_texture.outputs['Color'],wood_multiply.inputs[1])
wood_material.node_tree.links.new(wood_multiply.outputs['Color'],wood_shader.inputs['Base Color'])
A.specs['Desk_Walnut']['color']=list(wood_tint)
A.specs['Desk_Walnut']['smoothness']=.26
A.specs['Desk_Walnut']['texture']=str(T/'walnut_veneer.png')

# Keep the desk model; the three hardware assets are authored from local origin.
source=PROJECT/'ArtDeliverables/TimeDesk/HybridScene/BlenderPC/TimeDesk_PC.blend'
with bpy.data.libraries.load(str(source),link=False) as (src,dst):dst.collections=['PC_Desk']
group('Finish_Desk')
pcmap={'Desk_Edge':'Finish_Wood','Desk_Metal':'Finish_EnamelDark','Desk_WornEdge':'Finish_WoodEdge','Recess_Charcoal':'Finish_Charcoal'}
for col in dst.collections:
    for ob in list(col.objects):
        move_collection(ob)
        for slot in ob.material_slots:
            if slot.material:
                n=slot.material.name.split('.')[0]
                if n in pcmap:slot.material=materials[pcmap[n]]
                elif n=='Desk_Walnut':slot.material=materials['Desk_Walnut']
        if ob.type=='MESH' and any(m==materials['Desk_Walnut'] for m in ob.data.materials):
            # Remove the inherited cube UV channel. A single surface map gives
            # the tabletop one continuous grain instead of sampling cube islands.
            for layer in list(ob.data.uv_layers):ob.data.uv_layers.remove(layer)
            uv=ob.data.uv_layers.new(name='WalnutSurfaceUV')
            for face in ob.data.polygons:
                for li in face.loop_indices:
                    co=ob.data.vertices[ob.data.loops[li].vertex_index].co
                    uv.data[li].uv=(co.x/5.8+.5,co.y/2.52+.5 if abs(face.normal.z)>.5 else co.z/.25+.5)
    bpy.data.collections.remove(col)
sys.path.insert(0,str(HERE))
from hardware_models import build_hardware
hardware_anchors=build_hardware(T)
(HERE/'hardware_anchors.json').write_text(json.dumps(hardware_anchors,indent=2))

group('Finish_Tray')
# Continuous drawn-metal pan with a rolled rim, not four separate thin boards.
layers=[(.009,.69,.382,.036),(.027,.716,.407,.041),(.081,.77,.448,.047),(.094,.781,.459,.050),(.101,.777,.455,.05),(.094,.749,.427,.041),(.072,.737,.415,.038),(.030,.684,.371,.028)]
vv=[];ff=[]
for y,w,d,r in layers:vv.extend([(x,y,z) for x,z in roundrect(w,d,r,6)])
n=len(vv)//len(layers)
for j in range(len(layers)-1):
    for i in range(n):ff.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
ff.extend([tuple(reversed(range(n))),tuple(range((len(layers)-1)*n,len(layers)*n))])
mesh('Rolled continuous tray',vv,ff,'Finish_Enamel',True)
box('Soft tray insert',(0,.032,0),(.655,.007,.337),'Finish_EnamelDark',.022)
for x in [-.29,.29]:box('Tray foot',(x,.005,0),(.04,.01,.20),'Finish_BlackRubber',.003)

group('Finish_Intercom')
box('Intercom lower shell seam',(0,.016,0),(.297,.014,.230),'Finish_Charcoal',.011)
for x in [-.110,.110]:
    for z in [-.078,.078]:box('Intercom rubber foot',(x,.007,z),(.039,.014,.035),'Finish_BlackRubber',.005)
vv=[(-.154,.015,-.12),(.154,.015,-.12),(.154,.015,.12),(-.154,.015,.12),(-.147,.105,-.112),(.147,.105,-.112),(.147,.182,.112),(-.147,.182,.112)]
o=mesh('Intercom molded case',vv,[(0,3,2,1),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)],'Finish_Ivory');bevel(o,.011,4)
face=box('Speaker face panel',(-.035,.152,.001),(.188,.009,.182),'Finish_Enamel',.014);face.rotation_euler.x=math.radians(-19)
for i in range(8):
    z=-.06+i*.017;y=.105+(z+.112)*.344+.013
    vent=box('Speaker opening',(-.035,y,z),(.134-abs(i-3.5)*.01,.005,.005),'Finish_Charcoal',.002);vent.rotation_euler.x=math.radians(-19)
cylinder('Push-to-talk rim',(.107,.139,-.029),.021,.008,'Finish_Charcoal','y',32)
cylinder('Push-to-talk cap',(.107,.146,-.029),.017,.011,'Finish_Coral','y',32)
for x in [-.128,.128]:
    # Fixings belong to the lower face, clear of the speaker slots.
    cylinder('Intercom front fixing',(x,.067,-.119),.005,.004,'Hardware_BareMetal','z',20)
    box('Intercom fixing slot',(x,.067,-.122),(.006,.001,.001),'Finish_Charcoal',0)
box('Intercom blank inventory inset',(-.056,.059,-.121),(.082,.021,.002),'Finish_WarmGrey',.002)
tube('Curled intercom lead',[(.10,.06,.12),(.17,.02,.21),(.22,.012,.19),(.24,.012,.27)],.006,'Finish_BlackRubber')

group('Finish_Mouse')
vv=[];ff=[];count=40
for y,w,d in [(.005,.105,.17),(.020,.118,.18),(.043,.109,.166),(.061,.087,.130),(.065,.035,.065)]:
    for i in range(count):a=i*math.tau/count;vv.append((math.cos(a)*w/2,y,math.sin(a)*d/2))
for j in range(4):
    for i in range(count):ff.append((j*count+i,j*count+(i+1)%count,(j+1)*count+(i+1)%count,(j+1)*count+i))
ff.extend([tuple(reversed(range(count))),tuple(range(4*count,5*count))]);mesh('Sculpted two-button mouse',vv,ff,'Finish_Ivory',True)
tube('Mouse button seam',[(0,.060,.063),(0,.065,.028),(0,.065,.004)],.001,'Finish_Charcoal')
tube('Mouse palm seam',[(-.046,.040,.008),(-.024,.060,.007),(0,.065,.007),(.024,.060,.007),(.046,.040,.008)],.001,'Finish_KeyGrey')
tube('Mouse cable',[(0,.025,.089),(.012,.012,.14),(-.09,.009,.22),(-.20,.009,.41),(-.20,.011,.57)],.003,'Finish_BlackRubber')
box('Mouse pad',(0,.002,.015),(.30,.004,.30),'Finish_EnamelDark',.023)

group('Finish_Inkpad')
box('Stamp pad base',(0,.018,0),(.22,.034,.15),'Finish_EnamelDark',.009)
box('Stamp pad lip',(0,.035,0),(.208,.007,.138),'Finish_Enamel',.006)
box('Ink cushion',(0,.040,0),(.185,.009,.113),'Finish_Ink',.009)
lid=box('Open inkpad lid',(0,.066,.095),(.22,.105,.015),'Finish_Enamel',.007);lid.rotation_euler.x=math.radians(12)
for x in [-.069,.069]:
    cylinder('Inkpad rolled hinge',(x,.034,.079),.006,.052,'Finish_Brass','x',24)
    cylinder('Inkpad hinge pin',(x,.034,.079),.0028,.058,'Hardware_BareMetal','x',16)
    box('Inkpad rubber rest',(x,.003,0),(.03,.006,.085),'Finish_BlackRubber',.003)
box('Inkpad lid catch',(0,.028,-.078),(.037,.015,.008),'Finish_Brass',.003)

group('Finish_Lamp')
lathe('Weighted lamp base',[(.003,.113),(.013,.121),(.023,.119),(.031,.095),(.037,.059),(.042,.028)],'Finish_EnamelDark',64)
for x in [-.073,.073]:box('Lamp rubber foot',(x,.004,0),(.035,.008,.08),'Finish_BlackRubber',.003)
points=[(0,.045,0),(-.04,.32,.02),(.105,.62,-.04),(-.30,.80,-.15)]
for i in range(3):
    for off in [-.014,.014]:tube('Articulated lamp strut',[(points[i][0]+off,points[i][1],points[i][2]),(points[i+1][0]+off,points[i+1][1],points[i+1][2])],.008,'Finish_EnamelDark')
for x,y,z in points[1:]:cylinder('Lamp hinge',(x,y,z),.023,.062,'Finish_Brass','x',24)
tube('External lamp flex',[(.02,.03,.05),(.006,.18,.075),(-.02,.34,.06),(.14,.63,-.005),(-.31,.805,-.13)],.003,'Finish_BlackRubber')
# The shade and Unity spotlight share this axis, aimed at the work-mat centre.
start=Vector((-.30,.80,-.15));direction=Vector((-1.43,-.799,-.59)).normalized();right=direction.cross(Vector((0,0,1))).normalized();up=right.cross(direction).normalized()
vv=[];ff=[];nr=48
for dist,r in [(-.018,.038),(0,.048),(.155,.126),(.168,.130),(.172,.121),(.150,.117),(.008,.042)]:
    for i in range(nr):a=i*math.tau/nr;vv.append(tuple(start+direction*dist+(right*math.cos(a)+up*math.sin(a))*r))
for j in range(6):
    for i in range(nr):ff.append((j*nr+i,j*nr+(i+1)%nr,(j+1)*nr+(i+1)%nr,(j+1)*nr+i))
mesh('Spun lamp shade',vv,ff,'Finish_Enamel',True)
# Hollow inner reflector, with normals facing into the shade. The old flat
# disk faced away from the player and made an illuminated lamp look black.
vv=[]
for dist,r in [(.025,.040),(.148,.113),(.165,.120)]:
    for i in range(nr):
        a=i*math.tau/nr;vv.append(tuple(start+direction*dist+(right*math.cos(a)+up*math.sin(a))*r))
reflector=mesh('Cream hollow lamp reflector',vv,[(j*nr+i,j*nr+(i+1)%nr,(j+1)*nr+(i+1)%nr,(j+1)*nr+i) for j in range(2) for i in range(nr)],'Finish_LampInner',True)
axis=Vector(v(direction));origin=Vector(v(start))
for poly in reflector.data.polygons:
    rel=poly.center-origin;radial=rel-axis*rel.dot(axis)
    if poly.normal.dot(radial)>0:poly.flip()
bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=12,radius=.028,location=v(start+direction*.052))
bulb=bpy.context.object;bulb.name='Visible warm lamp bulb';finish(bulb,'Finish_LampInner');smooth(bulb)
cylinder('Lamp switch',(.067,.034,-.02),.009,.015,'Finish_Coral','y',24)

group('Finish_PencilRack')
box('Wooden desk pen rack',(0,.025,0),(.26,.05,.13),'Finish_Wood',.008)
for x,y,mat in [(-.085,.12,'Finish_Coral'),(-.025,.15,'Finish_KeyGrey'),(.04,.13,'Finish_Enamel'),(.095,.10,'Finish_Brass')]:
    cylinder('Pen socket',(x,.052,0),.011,.009,'Finish_Charcoal','y',20)
    cylinder('Office pencil',(x,y,0),.005,(y-.05)*2,mat,'y',6)
    cylinder('Pencil ferrule',(x,y*2-.061,0),.0053,.014,'Finish_Brass','y',12)

group('Finish_Board')
box('Instrument board plywood',(0,0,0),(.76,1.18,.043),'Finish_Wood',.007)
box('Painted notice surface',(0,0,-.025),(.71,1.13,.012),'Finish_Enamel',.008)
for x in [-.365,.365]:box('Notice board wood upright',(x,0,-.018),(.031,1.18,.047),'Finish_WoodEdge',.004)
for y in [-.575,.575]:box('Notice board end rail',(0,y,-.018),(.71,.031,.047),'Finish_WoodEdge',.004)
for x in [-.34,.34]:
    for y in [-.55,.55]:screw('Board fastening',(x,y,-.044),mat='Finish_Brass')
# Clipped, blank memo stock remains replaceable independently of the board.
paper=box('Clipped blank office memo',(.17,-.31,-.039),(.18,.21,.002),'Finish_Paper',.001);paper.rotation_euler.y=math.radians(-5)
box('Memo spring clip',(.17,-.204,-.044),(.057,.017,.012),'Finish_Brass',.003)

group('Finish_Mat')
box('Inspection mat body',(0,.004,0),(2.15,.008,1.04),'Finish_EnamelDark',.005)
o=mesh('Painted inspection surface',[(-1.065,.0085,-.51),(1.065,.0085,-.51),(1.065,.0085,.51),(-1.065,.0085,.51)],[(0,1,2,3)],'Finish_Mat')
uv=o.data.uv_layers.new(name='InspectionUV')
for loop in o.data.loops:
    co=o.data.vertices[loop.vertex_index].co;uv.data[loop.index].uv=(-co.x/2.13+.5,-co.y/1.02+.5)

group('Finish_DeskRail')
box('Rear clerk counter rim',(0,0,0),(1.31,.055,.13),'Finish_WoodEdge',.009)
box('Counter fascia',(0,-.072,.040),(1.28,.10,.052),'Finish_Enamel',.006)
for x in [-.53,.53]:box('Counter bracket',(x,-.125,.02),(.037,.16,.087),'Finish_EnamelDark',.004)

from booth_detail_models import build_desk_stationery, build_panel_ephemera
build_desk_stationery()
build_panel_ephemera(PROJECT)

# UVs are normalized to each manufactured part, not an arbitrary metre-based
# projection. Small controls sample the quiet middle of their material texture.
for col in collections.values():
    for ob in col.objects:
        if ob.type!='MESH':continue
        if any(m and (m.name in ['Finish_Glass','Finish_Mat','Desk_Walnut'] or m.name.startswith('Canvas_')) for m in ob.data.materials):continue
        for layer in list(ob.data.uv_layers):ob.data.uv_layers.remove(layer)
        uv=ob.data.uv_layers.new(name='PaintedSurfaceUV')
        mins=[min(v.co[i] for v in ob.data.vertices) for i in range(3)];maxs=[max(v.co[i] for v in ob.data.vertices) for i in range(3)]
        for p in ob.data.polygons:
            axis=max(range(3),key=lambda i:abs(p.normal[i]));axes=(0,1) if axis==2 else ((0,2) if axis==1 else (1,2))
            for li in p.loop_indices:
                co=ob.data.vertices[ob.data.loops[li].vertex_index].co
                uvp=[(co[i]-mins[i])/max(.00001,maxs[i]-mins[i]) for i in axes]
                if max(maxs[i]-mins[i] for i in axes)<.08:uvp=[.35+a*.3 for a in uvp]
                uv.data[li].uv=uvp

result=export_library('DeskFinish')
for model in result['models']:model['assetPath']='Assets/Art/Office/DeskFinish/Models/'+model['name']+'.fbx'
(HERE/'DeskFinish_manifest.json').write_text(json.dumps(result,indent=2))
