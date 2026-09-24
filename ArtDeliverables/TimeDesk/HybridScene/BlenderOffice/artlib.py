"""Editable Blender source for the reference-led PC art pass.

Run with Blender --background --python this_file. No Unity editor/runtime code.
Coordinates passed to helpers are Unity-style: X right, Y up, Z back.
Source objects retain their modeling modifiers in the .blend. Only export copies
are evaluated and grouped by material. Nothing in the game creates this art.
"""
import bpy, math, json, sys, shutil, os
from pathlib import Path
from mathutils import Vector

HERE = Path(__file__).resolve().parent
PROJECT = HERE.parents[3]
OUT = PROJECT / 'Assets/Art/Office/Hybrid/BlenderOffice'
OUT.mkdir(parents=True, exist_ok=True)
(OUT / 'Models').mkdir(exist_ok=True)
(OUT / 'Textures').mkdir(exist_ok=True)
(HERE / 'Exports').mkdir(exist_ok=True)
RENDER = '--render-art' in sys.argv

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for c in list(bpy.data.collections):
    if c.name != 'Collection': bpy.data.collections.remove(c)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
materials = {}
specs = {}
collections = {}
current = None

def linear(c): return c/12.92 if c <= .04045 else ((c+.055)/1.055)**2.4
def material(name, hx, rough=.75, metal=0, texture=None):
    rgb = tuple(int(hx[i:i+2],16)/255 for i in (0,2,4))
    m=bpy.data.materials.new(name); m.diffuse_color=(*[linear(c) for c in rgb],1)
    m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value=m.diffuse_color
    p.inputs['Roughness'].default_value=rough
    p.inputs['Metallic'].default_value=metal
    p.inputs['Specular IOR Level'].default_value=.18 if name!='CRT_Glass' else .3
    if texture and Path(texture).exists():
        t=m.node_tree.nodes.new('ShaderNodeTexImage'); t.image=bpy.data.images.load(str(texture))
        m.node_tree.links.new(t.outputs['Color'],p.inputs['Base Color'])
    materials[name]=m
    specs[name]={'color':rgb,'smoothness':1-rough,'metallic':metal,'texture':str(texture) if texture else None}
    return m

material('CRT_Terracotta','B9B6AD',.81) # Stable material ID; now the neutral face casing.
material('CRT_BackShell','AFADA5',.83)
material('Plastic_Ivory','C7C5BD',.81)
material('Plastic_WarmGrey','999C97',.84)
material('Plastic_DeepGrey','626B69',.87)
material('Recess_Charcoal','2C3334',.91)
material('CRT_Glass','263E42',.28)
material('Case_Steel','ABAFA9',.8,.08)
material('Power_Amber','BA7850',.76)
material('Indicator_Green','88AA76',.63)
material('Wear_Light','C5BFAF',.95)
material('Wear_Dark','8F897C',.95)
material('Paper_Label','C4BFAE',.95)
material('Cable_Rubber','393E36',.92)
material('Desk_Edge','5B4436',.86)
material('Desk_Metal','414D49',.8,.05)
material('Desk_WornEdge','8D6B4C',.94)
wood=PROJECT/'Assets/Art/Office/Hybrid/BlenderPC/Textures/desk_walnut_albedo.png'
material('Desk_Walnut','806247',.86,texture=wood if wood.exists() else None)

def v(p): return (-p[0],-p[2],p[1])
def group(name):
    global current
    current=bpy.data.collections.new(name); scene.collection.children.link(current)
    collections[name]=current
    return current
def move_collection(o):
    for c in list(o.users_collection): c.objects.unlink(o)
    current.objects.link(o)
def finish(o,mat):
    move_collection(o)
    if mat: o.data.materials.append(materials[mat])
    return o
def active(o):
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active=o
def smooth(o,weighted=True):
    for p in o.data.polygons: p.use_smooth=True
    if weighted:
        m=o.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL');m.keep_sharp=True;m.weight=50
    return o
def bevel(o,width=.003,segments=3):
    m=o.modifiers.new('Small manufactured edge','BEVEL');m.width=width;m.segments=segments
    m.limit_method='ANGLE';m.angle_limit=math.radians(32);m.harden_normals=True
    return smooth(o)
def box(name,p,d,mat,b=.003):
    bpy.ops.mesh.primitive_cube_add(size=1,location=v(p));o=bpy.context.object;o.name=name
    o.dimensions=(d[0],d[2],d[1]);active(o);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    finish(o,mat)
    if b:bevel(o,b)
    return o
def mesh(name,verts,faces,mat,shading=False):
    me=bpy.data.meshes.new(name);me.from_pydata([v(p) for p in verts],[],faces);me.update()
    o=bpy.data.objects.new(name,me);current.objects.link(o)
    if mat:me.materials.append(materials[mat])
    active(o);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')
    if shading:smooth(o)
    return o
def roundrect(w,h,r,n=8):
    pts=[]
    for cx,cy,a in [(w/2-r,h/2-r,0),(-w/2+r,h/2-r,90),(-w/2+r,-h/2+r,180),(w/2-r,-h/2+r,270)]:
        for i in range(n+1):
            t=math.radians(a+i*90/n);pts.append((cx+r*math.cos(t),cy+r*math.sin(t)))
    return pts
def loft(name,layers,mat,caps=True):
    # Each layer: depth, width, height, radius, vertical center.
    verts=[]
    for z,w,h,r,y in layers: verts.extend([(x,yy+y,z) for x,yy in roundrect(w,h,r)])
    n=len(verts)//len(layers);faces=[]
    for j in range(len(layers)-1):
        for i in range(n):faces.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
    if caps:faces.extend([tuple(reversed(range(n))),tuple(range((len(layers)-1)*n,len(layers)*n))])
    return mesh(name,verts,faces,mat,True)
def cylinder(name,p,r,depth,mat,axis='y',vertices=32):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=depth,location=v(p))
    o=bpy.context.object;o.name=name
    if axis=='z':o.rotation_euler.x=math.pi/2
    elif axis=='x':o.rotation_euler.y=math.pi/2
    active(o);bpy.ops.object.transform_apply(location=False,rotation=True,scale=True);finish(o,mat);bevel(o,min(.0015,r*.1),2)
    return o
def tube(name,points,r,mat):
    curve=bpy.data.curves.new(name,'CURVE');curve.dimensions='3D';curve.resolution_u=16
    s=curve.splines.new('BEZIER');s.bezier_points.add(len(points)-1)
    for b,p in zip(s.bezier_points,points):b.co=v(p);b.handle_left_type='AUTO';b.handle_right_type='AUTO'
    curve.bevel_depth=r;curve.bevel_resolution=3;curve.resolution_u=12
    o=bpy.data.objects.new(name,curve);current.objects.link(o);curve.materials.append(materials[mat]);return o
def cut(target,cutter):
    active(target);m=target.modifiers.new('Recess '+cutter.name,'BOOLEAN');m.operation='DIFFERENCE';m.solver='EXACT';m.object=cutter
    # Apply only the cut, keep final normals modifier at end of stack.
    bpy.ops.object.modifier_move_up(modifier=m.name)
    bpy.ops.object.modifier_apply(modifier=m.name)
    bpy.data.objects.remove(cutter,do_unlink=True)
def screw(name,p,axis='z',mat='Plastic_DeepGrey'):
    return cylinder(name,p,.0035,.0018,mat,axis,12)
def patch(name,p,w,h,mat='Wear_Light',plane='front'):
    # Authored sparse chips, without painted light/shadow. Irregular silhouettes.
    x,y,z=p;poly=[(-.5,-.26),(-.36,-.5),(.16,-.35),(.5,-.05),(.33,.23),(-.22,.5),(-.46,.2)]
    verts=[(x+a*w,y+b*h,z) if plane=='front' else (x+a*w,y,z+b*h) for a,b in poly]
    return mesh(name,verts,[tuple(range(len(verts)))],mat)

def export_library(filename):
    manifest={'materials':specs,'models':[]}
    for name,col in collections.items():
        exportcol=bpy.data.collections.new('EXPORT_'+name);scene.collection.children.link(exportcol)
        deps=bpy.context.evaluated_depsgraph_get();parts={}
        for source in list(col.objects):
            if source.type not in {'MESH','CURVE'}:continue
            ev=source.evaluated_get(deps)
            me=bpy.data.meshes.new_from_object(ev,preserve_all_data_layers=True,depsgraph=deps)
            ob=bpy.data.objects.new(source.name+'_export',me);ob.matrix_world=source.matrix_world.copy();exportcol.objects.link(ob)
            matname=me.materials[0].name
            if not me.uv_layers:
                uv=me.uv_layers.new(name='SurfaceUV')
                for p in me.polygons:
                    axis=max(range(3),key=lambda i:abs(p.normal[i]))
                    for li in p.loop_indices:
                        co=me.vertices[me.loops[li].vertex_index].co
                        a,b=(co.x,co.y) if axis==2 else ((co.x,co.z) if axis==1 else (co.y,co.z))
                        uv.data[li].uv=(a*.5+.5,b*.5+.5)
            parts.setdefault(matname,[]).append(ob)
        nodes={};counts={}
        for matname,obs in parts.items():
            bpy.ops.object.select_all(action='DESELECT')
            for o in obs:o.select_set(True)
            bpy.context.view_layer.objects.active=obs[0]
            if len(obs)>1:bpy.ops.object.join()
            o=bpy.context.object;o.name=name+'__'+matname
            scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
            bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
            o.data.materials.clear();o.data.materials.append(materials[matname])
            for p in o.data.polygons:p.material_index=0
            nodes[o.name]=matname
            counts[o.name]=sum(len(p.vertices)-2 for p in o.data.polygons)
        bpy.ops.object.select_all(action='DESELECT')
        for o in exportcol.objects:o.select_set(True)
        export_file=HERE/'Exports'/f'{name}.fbx'
        bpy.ops.export_scene.fbx(filepath=str(export_file),use_selection=True,object_types={'MESH'},
            use_mesh_modifiers=True,mesh_smooth_type='OFF',axis_forward='-Z',axis_up='Y',
            apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=True,
            add_leaf_bones=False,bake_anim=False,path_mode='AUTO')
        pending=OUT/'Models'/f'{name}.fbx.pending';shutil.copyfile(export_file,pending);os.replace(pending,OUT/'Models'/f'{name}.fbx')
        manifest['models'].append({'name':name,'assetPath':f'Assets/Art/Office/Hybrid/BlenderOffice/Models/{name}.fbx','nodes':nodes,'triangles':sum(counts.values())})
        for o in list(exportcol.objects):bpy.data.objects.remove(o,do_unlink=True)
        bpy.data.collections.remove(exportcol)
    (HERE/(filename+'_manifest.json')).write_text(json.dumps(manifest,indent=2))
    bpy.ops.wm.save_as_mainfile(filepath=str(HERE/(filename+'.blend')))
    return manifest

def lathe(name,profile,mat,n=40):
    verts=[];faces=[]
    for y,r in profile:
        for i in range(n):
            a=i*math.tau/n;verts.append((r*math.cos(a),y,r*math.sin(a)))
    for j in range(len(profile)-1):
        for i in range(n):faces.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
    faces.extend([tuple(reversed(range(n))),tuple(range((len(profile)-1)*n,len(profile)*n))])
    return mesh(name,verts,faces,mat,True)

