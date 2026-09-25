"""Three distinct megablock silhouettes, keeping the established exterior scale."""
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from artlib import *

# Direct data construction avoids thousands of scene-wide operator updates.
def mesh(name,verts,faces,mat,shading=False):
    me=bpy.data.meshes.new(name);me.from_pydata([v(p) for p in verts],[],faces);me.update()
    ob=bpy.data.objects.new(name,me)
    # Current collection is owned by artlib's group() function.
    import artlib
    artlib.current.objects.link(ob);me.materials.append(materials[mat])
    return ob

def box(name,p,d,mat,b=0):
    x,y,z=p;w,h,t=[a/2 for a in d]
    vv=[(x-w,y-h,z-t),(x+w,y-h,z-t),(x+w,y+h,z-t),(x-w,y+h,z-t),(x-w,y-h,z+t),(x+w,y-h,z+t),(x+w,y+h,z+t),(x-w,y+h,z+t)]
    ff=[(0,3,2,1),(4,5,6,7),(0,4,7,3),(1,2,6,5),(0,1,5,4),(3,7,6,2)]
    ob=mesh(name,vv,[tuple(reversed(f)) for f in ff],mat)
    if b:bevel(ob,b,2)
    return ob
material('City_Concrete','B1B0A1',.96)
material('City_ConcreteLight','D0C5A9',.96)
material('City_ConcreteDark','687E86',.96)
material('City_Window','334D5A',.70)
material('City_WindowLight','D9BB82',.76)
material('City_Service','3D525B',.83,.14)
material('City_Rust','AA704E',.97)
material('City_Sign','4F7979',.84)
material('City_Weather','728385',.98)
window_emission=(.72,.40,.14)
window_shader=materials['City_WindowLight'].node_tree.nodes.get('Principled BSDF')
window_shader.inputs['Emission Color'].default_value=(*window_emission,1)
window_shader.inputs['Emission Strength'].default_value=1.4
specs['City_WindowLight']['emission']={'color':list(window_emission),'intensity':1.4}

massing=[[(0,9,0,26,18,21),(0,27,0,22,18,19),(1,40,0,29,8,23),(-2,52,1,18,16,17),(-2,66,1,23,10,20),(-4,78,2,14,14,13)],
 [(0,10,0,28,20,24),(0,26,0,26,12,22),(-6,45,1,14,26,18),(7,48,1,14,32,18),(0,66,1,30,8,20),(4,75,2,16,10,13)],
 [(0,10,0,29,20,24),(0,23,0,34,6,27),(0,38,0,24,24,20),(0,53,0,32,6,25),(2,66,1,21,20,18),(2,80,2,26,8,20)]]
for variant,name in enumerate(['City_CivicMegablock','City_TwinMegablock','City_TerracedMegablock']):
    group(name)
    for level,(cx,cy,cz,w,h,d) in enumerate(massing[variant]):
        finish='City_ConcreteLight' if level%3==0 else 'City_Concrete'
        if variant==1 and level in [1,3]:finish='City_Sign'
        if variant==2 and level in [1,3]:finish='City_Rust'
        box('Concrete structural tier',(cx,cy,cz),(w,h,d),finish,.075)
        box('Projecting floor cornice',(cx,cy-h/2+.27,cz),(w+.30,.38,d+.30),'City_ConcreteDark',.04)
        rows=max(1,int((h-2.8)/3));cols=max(1,int((w-2.1)/2.9));scols=max(1,int((d-2.1)/2.9))
        # Distinct recessed window bays instead of unbroken black horizontal stripes.
        for row in range(rows):
            y=cy-h/2+2.2+row*3
            for col in range(cols):
                x=cx+(col-(cols-1)/2)*2.9
                wm='City_WindowLight' if (col*7+row*3+level)%11==2 else 'City_Window'
                for side in [-1,1]:
                    zz=cz+side*(d/2+.008)
                    mesh('Window pair',[(x-.87,y-.53,zz),(x+.87,y-.53,zz),(x+.87,y+.53,zz),(x-.87,y+.53,zz)],[(0,1,2,3) if side<0 else (3,2,1,0)],wm)
                    box('Window centre bar',(x,y,zz+side*.01),(.075,1.06,.033),'City_ConcreteDark',0)
                    # A modeled reveal and sill create depth under live light;
                    # the window no longer reads as a dark rectangle painted on concrete.
                    vv=[]
                    for w0,h0,z0 in [(.98,.64,zz+side*.17),(.87,.53,zz)]:
                        vv.extend([(x-w0,y-h0,z0),(x+w0,y-h0,z0),(x+w0,y+h0,z0),(x-w0,y+h0,z0)])
                    ff=[(k,(k+1)%4,(k+1)%4+4,k+4) for k in range(4)]
                    if side>0:ff=[tuple(reversed(f)) for f in ff]
                    mesh('Chamfered window reveal',vv,ff,'City_ConcreteDark')
                    box('Projecting window sill',(x,y-.64,zz+side*.10),(2.06,.105,.25),'City_ConcreteLight',.018)
                    if (row*3+col+level*7+variant)%17==2:
                        # An occasional partly closed blind breaks mechanical
                        # repetition, while leaving the underlying window intact.
                        box('Faded partial window blind',(x,y+.28,zz+side*.026),(1.65,.43,.017),'City_Sign',0)
                        for jy in range(3):box('Blind slat',(x,y+.13+jy*.13,zz+side*.042),(1.66,.023,.021),'City_ConcreteDark',0)
                    if variant==0:
                        box('Civic window brow',(x,y+.71,zz+side*.22),(2.15,.12,.48),'City_ConcreteLight',.022)
            for col in range(scols):
                z=cz+(col-(scols-1)/2)*2.9
                for side in [-1,1]:
                    xx=cx+side*(w/2+.008)
                    mesh('Side window pair',[(xx,y-.53,z-.87),(xx,y-.53,z+.87),(xx,y+.53,z+.87),(xx,y+.53,z-.87)],[(0,1,2,3) if side>0 else (3,2,1,0)],'City_Window')
                    box('Side window mullion',(xx+side*.025,y,z),(.06,1.06,.075),'City_ConcreteDark',0)
                    box('Side window sill',(xx+side*.12,y-.64,z),(.28,.105,2.06),'City_ConcreteLight',.018)
        # Each design has its own structural rhythm. Deep jambs occupy the
        # spaces BETWEEN window bays rather than covering openings arbitrarily.
        if variant==0:
            for k in range(0,cols-1,2):
                x=cx+(k-(cols-1)/2)*2.9+1.45
                box('Civic vertical facade fin',(x,cy,cz-d/2-.30),(.31,h-.8,.61),'City_ConcreteLight',.04)
                box('Fin recessed line',(x,cy,cz-d/2-.61),(.073,h-.95,.022),'City_ConcreteDark',.004)
        elif variant==1:
            for side in [-1,1]:
                box('Twin tower corner spine',(cx+side*(w/2-.31),cy,cz-d/2-.20),(.62,h,.43),'City_ConcreteLight',.04)
            for row in range(rows):
                yy=cy-h/2+1.37+row*3
                box('Twin tower ribbon spandrel',(cx,yy,cz-d/2-.025),(w-.9,.41,.042),'City_Service',.008)
        else:
            for k in range(0,cols,3):
                x=cx+(k-(cols-1)/2)*2.9
                box('Industrial pier facing',(x-1.22,cy,cz-d/2-.21),(.34,h-.40,.44),'City_Rust',.025)
                for yy in [cy-h/2+.8,cy+h/2-.8]:
                    box('Industrial pier coupling',(x-1.22,yy,cz-d/2-.45),(.49,.27,.09),'City_Service',.009)
        for xx in [cx-w*.39,cx+w*.39]:
            box('Vertical service spine',(xx,cy,cz-d/2-.24),(.40,h,.5),'City_Service',.032)
            for y in [cy-h/2+1,cy+h/2-1]:box('Service strap',(xx,y,cz-d/2-.52),(.57,.18,.12),'City_Rust',.013)
        if level in [1,3]:
            box('External plant housing',(cx+w/2-.9,cy,cz-d/2-.57),(1.4,2.3,1.1),'City_Service',.07)
            for i in range(4):box('Plant louver',(cx+w/2-.9,cy-.65+i*.42,cz-d/2-1.13),(1.05,.13,.05),'City_ConcreteDark',.008)
            box('Plant service support',(cx+w/2-.9,cy-1.24,cz-d/2-.65),(1.72,.15,1.34),'City_Service',.025)
            for xx in [cx+w/2-1.43,cx+w/2-.37]:
                box('Plant wall mounting bracket',(xx,cy-1.53,cz-d/2-.30),(.12,.55,.58),'City_ConcreteDark',.015)
        if level in [0,2,4]:
            patch('Broad weather streak',(cx-w*.23,cy-h*.17,cz-d/2-.015),.8,h*.33,'City_Weather')
    # Blank signage allows timeline-driven branding; never a baked slogan.
    eligible=[cz-d/2 for cx,cy,cz,w,h,d in massing[variant] if cy-h/2<=33<=cy+h/2]
    zz=min(eligible)-.22
    box('Blank civic sign housing',(0,33,zz),(8.2,4.2,.35),'City_Service',.08)
    box('Blank civic sign face',(0,33,zz-.185),(7.7,3.7,.024),'City_Sign',.018)
    cx,cy,cz,w,h,d=massing[variant][-1]
    for x in [cx-w*.25,cx+w*.25]:
        box('Rooftop air handler',(x,cy+h/2+1.2,cz),(2.5,2.4,3),'City_Service',.07)
        cylinder('Vent stack',(x,cy+h/2+3.1,cz),.32,1.7,'City_ConcreteDark','y',16)
    # Grounded heavy lower buttresses remain behind the office glazing.
    for x in [-8,8]:box('Podium buttress',(x,8,-11.5),(3.2,16,3.5),'City_ConcreteLight',.10)
export_library('City_Collection')
