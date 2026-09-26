"""Revision 2: editable clean, soft CRT study. Geometry and renders, no image filters (art side).

Purpose: the Rebuilt CRT, the live office PC (the user protects it: rerun only on request).
Inputs: CRT_StyleStudy.blend here (the earlier ivory study, loaded for the before/after
comparison; keep that .blend).
Outputs: Assets/Art/Office/ImportedOffice/Models/CRT_Rebuilt.fbx (its CRT2_Glass object is
the glass the gameplay layer finds, scene contract PCScreen: keep that name),
CRT_Rebuilt_Study.blend and revision2.json here, and with --compare the
revision2_before/after.png renders. Then Tools > Office Art > Apply Rebuilt CRT Study in Unity.
Paths resolve from this file (any checkout or worktree).
Run: blender --background --python ArtDeliverables/TimeDesk/ImportedOffice/BlenderCRT/rebuild_crt.py [-- --compare]
(written for Blender 5.2).
"""
import bpy, math, json, sys
from pathlib import Path
from mathutils import Vector
HERE=Path(__file__).resolve().parent
P=HERE.parents[3]  # the repo root
OUT=P/'Assets/Art/Office/ImportedOffice'
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene
materials={}
def linear(v): return v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4
def material(name,hexcolor,rough):
    rgb=[int(hexcolor[i:i+2],16)/255 for i in (0,2,4)]
    m=bpy.data.materials.new(name);m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value=(*map(linear,rgb),1)
    p.inputs['Roughness'].default_value=rough
    p.inputs['Specular IOR Level'].default_value=.24 if name!='Glass' else .15
    materials[name]=m
    return m
for args in [('Shell','ADA18A',.76),('Bezel','C5B89E',.73),('Case','A39883',.78),('Trim','655F53',.81),('Dark','343C39',.84),('Glass','273333',.47),('Orange','B87D45',.74),('Teal','557D73',.76),('Ink','686C61',.85)]:material(*args)
after=bpy.data.collections.new('02 Rebuilt CRT - editable parts');scene.collection.children.link(after)
def collect(o,mat):
    for c in list(o.users_collection):c.objects.unlink(o)
    after.objects.link(o);o.data.materials.append(materials[mat]);return o
def finish(o,bevel=0):
    bpy.context.view_layer.objects.active=o
    if bevel:
        m=o.modifiers.new('Soft manufactured edge','BEVEL');m.width=bevel;m.segments=4
        m.harden_normals=True;m.use_clamp_overlap=True
    for f in o.data.polygons:f.use_smooth=True
    m=o.modifiers.new('Stable planar normals','WEIGHTED_NORMAL');m.keep_sharp=True;m.weight=50
    return o
def box(name,loc,size,mat,bevel=.003):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    collect(o,mat);return finish(o,bevel)
def mesh(name,verts,faces,mat,bevel=0):
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update()
    o=bpy.data.objects.new(name,me);after.objects.link(o);o.data.materials.append(materials[mat])
    bpy.context.view_layer.objects.active=o;o.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')
    o.select_set(False);return finish(o,bevel)
def outline(w,h,r,y,z):
    pts=[]
    for cx,cz,a in [(w/2-r,h/2-r,0),(-w/2+r,h/2-r,90),(-w/2+r,-h/2+r,180),(w/2-r,-h/2+r,270)]:
        for j in range(12):
            angle=math.radians(a+j*90/11)
            pts.append((cx+r*math.cos(angle),y,z+cz+r*math.sin(angle)))
    return pts
def loft(name,profiles,mat,cap=True,bevel=.002):
    verts=sum([outline(*p) for p in profiles],[]);n=48;faces=[]
    for k in range(len(profiles)-1):
        for i in range(n):faces.append((k*n+i,k*n+(i+1)%n,(k+1)*n+(i+1)%n,(k+1)*n+i))
    if cap:faces.extend([tuple(reversed(range(n))),tuple(range((len(profiles)-1)*n,len(profiles)*n))])
    return mesh(name,verts,faces,mat,bevel)
def cylinder(name,loc,radius,depth,mat,front=False,scale=(1,1,1)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=64,radius=radius,depth=depth,location=loc)
    o=bpy.context.object;o.name=name;o.scale=scale
    if front:o.rotation_euler.x=math.pi/2
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    collect(o,mat);return finish(o,min(.0015,depth*.2))
def label(name,body,loc,size,mat):
    cu=bpy.data.curves.new(name,'FONT');cu.body=body;cu.size=size;cu.extrude=0;cu.space_character=1.15
    o=bpy.data.objects.new(name,cu);after.objects.link(o);o.location=loc;o.rotation_euler=(math.pi/2,0,0);cu.materials.append(materials[mat]);return o
# Low horizontal system unit. Feet and a narrow assembly joint establish construction.
box('System shell',(0,-.01,.062),(.56,.47,.100),'Case',.009)
box('System front fascia',(0,-.250,.060),(.544,.018,.084),'Bezel',.006)
for x in [-.222,.222]:
    for y in [-.19,.18]:box('Rubber foot',(x,y,.009),(.065,.050,.018),'Dark',.004)
box('Case seam',(0,-.260,.098),(.508,.001,.0015),'Trim',.0005)
# Two disk bays, small ejects and status lenses; calm left panel with functional vent group.
for z in [.045,.077]:
    box('Drive surround',(.151,-.261,z),(.166,.005,.026),'Trim',.002)
    box('Drive face',(.151,-.265,z),(.159,.006,.023),'Case',.0015)
    box('Disk opening',(.147,-.269,z+.002),(.120,.002,.0025),'Dark',.0007)
    box('Disk eject',(.213,-.270,z-.007),(.014,.005,.005),'Bezel',.0012)
    box('Drive lamp',(.083,-.269,z-.006),(.003,.001,.002),'Teal',.0005)
for i in range(13):box('Front vent',(-.215+i*.010,-.261,.045),(.004,.002,.020),'Trim',.0015)
box('Nameplate',(-.175,-.261,.080),(.085,.002,.014),'Case',.0015)
label('System legend','OFFICE / 90',(-.212,-.263,.077),.006,'Ink')
cylinder('System power',(.251,-.263,.073),.005,.003,'Orange',True)
# Visible swivel pedestal, with a low oval foot and a softened neck.
cylinder('Swivel foot',(0,-.035,.122),.122,.020,'Shell',scale=(1,.80,1))
cylinder('Swivel seat',(0,-.024,.139),.068,.019,'Trim',scale=(1,.85,1))
box('Tilt neck',(0,-.019,.158),(.094,.080,.045),'Shell',.011)
# Deep tapered back, slimmer front and a real recessed lip. Frame is a closed ring.
zc=.336
loft('Tapered rear housing',[(.426,.329,.015,-.181,zc),(.430,.333,.016,-.156,zc),(.285,.260,.019,.190,zc+.002)],'Shell')
profiles=[(.442,.347,.018,-.213,zc),(.436,.341,.016,-.184,zc),(.392,.292,.013,-.184,zc+.007),(.389,.289,.014,-.229,zc+.007),(.442,.347,.018,-.213,zc)]
loft('Rounded monitor bezel',profiles,'Bezel',False,.002)
loft('Narrow glass gasket',[(.389,.289,.014,-.229,zc+.007),(.382,.282,.015,-.236,zc+.007)],'Trim',False,.0008)
# Gently convex, blank glass. Ring tessellation avoids a planar black card.
verts=[(0,-.246,zc+.007)];faces=[]
for s in [.20,.45,.70,.90,1.0]:verts+=outline(.382*s,.282*s,.015*s,-.246+.012*s*s,zc+.007)
for i in range(48):faces.append((0,1+i,1+(i+1)%48))
for k in range(4):
    for i in range(48):faces.append((1+k*48+i,1+(k+1)*48+i,1+(k+1)*48+(i+1)%48,1+k*48+(i+1)%48))
screen=mesh('Blank curved CRT glass',verts,faces,'Glass')
screen.modifiers.clear()
for p in screen.data.polygons:p.use_smooth=True
# Modest monitor controls; no television tuning knob.
cylinder('Monitor power',(.166,-.226,.183),.006,.004,'Orange',True)
for x in [.123,.142]:box('Monitor adjustment',(x,-.227,.182),(.010,.004,.004),'Shell',.0015)
cylinder('Monitor status',(.185,-.226,.183),.0017,.002,'Teal',True)
label('Monitor legend','OFFICE 14',(-.164,-.227,.182),.005,'Ink')
# Side ventilation follows the sloping side plane, all geometry and clean colours.
for side in [-1,1]:
    for i in range(10):
        y=-.083+i*.020
        x=side*(.215-(y+.156)/.346*.0725+.0008)
        o=box('Rear side vent',(x,y,.355),(.0015,.007,.083),'Trim',.002)
        o.rotation_euler.z=side*.205

# Keep editable pieces in the blend; make a separate export collection grouped by material.
export=bpy.data.collections.new('Export meshes');scene.collection.children.link(export)
for key,mat in materials.items():
    clones=[]
    for original in list(after.objects):
        if not original.data.materials or original.data.materials[0]!=mat:continue
        copy=original.copy();copy.data=original.data.copy();export.objects.link(copy);clones.append(copy)
    if not clones:continue
    bpy.ops.object.select_all(action='DESELECT')
    for o in clones:o.select_set(True)
    bpy.context.view_layer.objects.active=clones[0];bpy.ops.object.convert(target='MESH');bpy.ops.object.join()
    o=bpy.context.object;o.name='CRT2_'+key
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.15192,island_margin=.008);bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.object.select_all(action='DESELECT')
for o in export.objects:o.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(OUT/'Models/CRT_Rebuilt.fbx'),use_selection=True,object_types={'MESH'},use_mesh_modifiers=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=True,add_leaf_bones=False,bake_anim=False)
export.hide_render=True;export.hide_viewport=True
# Previous real Blender study, under identical new studio conditions for comparison.
with bpy.data.libraries.load(str(HERE/'CRT_StyleStudy.blend'),link=False) as (src,dst):dst.objects=['CRT_Painted']
before=bpy.data.collections.new('01 Before - previous ivory study');scene.collection.children.link(before)
old=dst.objects[0];before.objects.link(old)
scene.render.engine='CYCLES';scene.cycles.samples=40;scene.cycles.use_denoising=True
scene.render.resolution_x=1100;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast'
scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.28,.31,.35,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.35
groundmat=material('Studio','514D45',.88)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.001));bpy.context.object.data.materials.append(groundmat)
for name,loc,power,size,color in [('Key',(-1.6,-2,2.7),180,2.4,(1,.88,.72)),('Fill',(1.8,-1.2,1.4),55,2,(.75,.86,1)),('Rim',(0,1.4,2),110,1.6,(1,.92,.8))]:
    d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size;d.color=color
    o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,.25))-o.location).to_track_quat('-Z','Y').to_euler()
d=bpy.data.cameras.new('Same comparison camera');cam=bpy.data.objects.new('Same comparison camera',d);scene.collection.objects.link(cam);scene.camera=cam
cam.location=(-1.15,-2.1,1.07);cam.rotation_euler=(Vector((0,-.005,.27))-cam.location).to_track_quat('-Z','Y').to_euler();d.type='ORTHO';d.ortho_scale=.95
after.hide_render=True
if '--compare' in sys.argv:
    scene.render.filepath=str(HERE/'revision2_before.png');bpy.ops.render.render(write_still=True)
after.hide_render=False;before.hide_render=True;before.hide_viewport=True
if '--compare' in sys.argv:
    scene.render.filepath=str(HERE/'revision2_after.png');bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'CRT_Rebuilt_Study.blend'))
report={'revision':2,'method':'Reconstructed CRT assembly, prior vendor-derived study retained as comparison','main_corner_radius_m':.018,'same_camera_lighting':True,'materials':{name:{'color':list(m.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value),'roughness':m.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value} for name,m in materials.items()},'export':'Assets/Art/Office/ImportedOffice/Models/CRT_Rebuilt.fbx','screen_center_blender':[0,-.240,.343],'triangles':sum(len(p.vertices)-2 for o in export.objects for p in o.data.polygons)}
(HERE/'revision2.json').write_text(json.dumps(report,indent=2))
