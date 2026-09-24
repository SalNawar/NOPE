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
material('City_Concrete','A3B5C0',.96)
material('City_ConcreteLight','BCCACF',.96)
material('City_ConcreteDark','8199A7',.96)
material('City_Window','5F7D8C',.87)
material('City_WindowLight','9DB4B9',.90)
material('City_Service','536970',.92)
material('City_Rust','9A7863',.97)
material('City_Sign','607E82',.94)
material('City_Weather','728385',.98)

massing=[[(0,9,0,26,18,21),(0,27,0,22,18,19),(1,40,0,29,8,23),(-2,52,1,18,16,17),(-2,66,1,23,10,20),(-4,78,2,14,14,13)],
 [(0,10,0,28,20,24),(0,26,0,26,12,22),(-6,45,1,14,26,18),(7,48,1,14,32,18),(0,66,1,30,8,20),(4,75,2,16,10,13)],
 [(0,10,0,29,20,24),(0,23,0,34,6,27),(0,38,0,24,24,20),(0,53,0,32,6,25),(2,66,1,21,20,18),(2,80,2,26,8,20)]]
for variant,name in enumerate(['City_CivicMegablock','City_TwinMegablock','City_TerracedMegablock']):
    group(name)
    for level,(cx,cy,cz,w,h,d) in enumerate(massing[variant]):
        box('Concrete structural tier',(cx,cy,cz),(w,h,d),'City_ConcreteLight' if level%3==0 else 'City_Concrete',.075)
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
            for col in range(scols):
                z=cz+(col-(scols-1)/2)*2.9
                for side in [-1,1]:
                    xx=cx+side*(w/2+.008)
                    mesh('Side window pair',[(xx,y-.53,z-.87),(xx,y-.53,z+.87),(xx,y+.53,z+.87),(xx,y+.53,z-.87)],[(0,1,2,3) if side>0 else (3,2,1,0)],'City_Window')
        for xx in [cx-w*.39,cx+w*.39]:
            box('Vertical service spine',(xx,cy,cz-d/2-.24),(.40,h,.5),'City_Service',.032)
            for y in [cy-h/2+1,cy+h/2-1]:box('Service strap',(xx,y,cz-d/2-.52),(.57,.18,.12),'City_Rust',.013)
        if level in [1,3]:
            box('External plant housing',(cx+w/2-.9,cy,cz-d/2-.57),(1.4,2.3,1.1),'City_Service',.07)
            for i in range(4):box('Plant louver',(cx+w/2-.9,cy-.65+i*.42,cz-d/2-1.13),(1.05,.13,.05),'City_ConcreteDark',.008)
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
