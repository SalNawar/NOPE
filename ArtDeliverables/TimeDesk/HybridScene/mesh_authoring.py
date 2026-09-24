"""Shared offline OBJ authoring primitives. Never loaded by Unity."""
from pathlib import Path
import math, json, shutil

HERE=Path(__file__).resolve().parent
ASSETS=HERE.parents[2]/'Assets/Art/Office/Hybrid'
OUT=ASSETS/'Models'
BACKUP=HERE/'BeforeSurfaceRefinement/Models'
BACKUP.mkdir(parents=True,exist_ok=True)
COLORS={
 'ivory':(.68,.60,.44),'ivory_edge':(.83,.75,.57),'ivory_dark':(.43,.37,.27),
 'teal':(.13,.25,.24),'teal_edge':(.25,.39,.34),'metal':(.20,.24,.22),
 'rubber':(.08,.105,.10),'screen':(.085,.145,.15),'glass_edge':(.13,.24,.25),
 'wood':(.52,.29,.13),'wood_edge':(.65,.40,.20),'brass':(.59,.40,.16),
 'key':(.74,.68,.53),'key_dark':(.48,.47,.37),'paper':(.79,.75,.61),
 'green':(.42,.74,.47),'red':(.64,.26,.16),'rust':(.38,.20,.12),
}

def rr(w,h,r,n=5):
    r=max(.0001,min(r,w*.49,h*.49))
    pts=[]
    for cx,cy,base in [(w/2-r,h/2-r,0),(-w/2+r,h/2-r,90),(-w/2+r,-h/2+r,180),(w/2-r,-h/2+r,270)]:
        for i in range(n+1):
            a=math.radians(base+90*i/n);pts.append((cx+r*math.cos(a),cy+r*math.sin(a)))
    return pts

class Mesh:
    def __init__(self,name): self.name=name;self.faces=[];self.materials={}
    def face(self,name,mat,pts):
        self.faces.append((name,mat,pts));self.materials[name]=mat
    def profile(self,name,mat,c,profiles,axis='z',cap=True):
        # Each profile: (depth, width, height, corner radius).
        def xyz(a,b,t):return (a+c[0],b+c[1],t+c[2]) if axis=='z' else (a+c[0],t+c[1],-b+c[2])
        rings=[[xyz(a,b,t) for a,b in rr(w,h,r)] for t,w,h,r in profiles]
        if cap:self.face(name,mat,list(reversed(rings[0])));self.face(name,mat,rings[-1])
        for a,b in zip(rings,rings[1:]):
            for i in range(len(a)):
                j=(i+1)%len(a);self.face(name,mat,[a[i],a[j],b[j],b[i]])
    def box(self,name,c,s,mat,bevel=.015,axis='z'):
        w,h,d=s if axis=='z' else (s[0],s[2],s[1])
        b=min(bevel,w*.2,h*.2,d*.35)
        self.profile(name,mat,c,[(-d/2,w-2*b,h-2*b,max(b,.003)),(-d/2+b,w,h,2*b),(d/2-b,w,h,2*b),(d/2,w-2*b,h-2*b,max(b,.003))],axis)
    def panel(self,name,c,w,h,mat,r=.015):
        self.face(name,mat,[(x+c[0],y+c[1],c[2]) for x,y in reversed(rr(w,h,r))])
    def ring(self,name,c,outer,inner,z0,z1,mat):
        a=rr(*outer);b=rr(*inner)
        for i in range(len(a)):
            j=(i+1)%len(a)
            self.face(name,mat,[(a[j][0]+c[0],a[j][1]+c[1],z0+c[2]),(a[i][0]+c[0],a[i][1]+c[1],z0+c[2]),(b[i][0]+c[0],b[i][1]+c[1],z1+c[2]),(b[j][0]+c[0],b[j][1]+c[1],z1+c[2])])
    def cyl(self,name,c,r,h,mat,axis='y',n=24):
        def pos(a,t):
            u=r*math.cos(a);v=r*math.sin(a)
            return (u+c[0],t+c[1],-v+c[2]) if axis=='y' else (u+c[0],v+c[1],t+c[2])
        a=[pos(i*2*math.pi/n,-h/2) for i in range(n)];b=[pos(i*2*math.pi/n,h/2) for i in range(n)]
        self.face(name,mat,list(reversed(a)));self.face(name,mat,b)
        for i in range(n):j=(i+1)%n;self.face(name,mat,[a[i],a[j],b[j],b[i]])
    def tube(self,name,points,r,mat,n=8):
        # Parallel-transport-like frames suffice for these short, smooth cables.
        rings=[]
        for i,p in enumerate(points):
            a=points[max(0,i-1)];b=points[min(len(points)-1,i+1)]
            d=[b[k]-a[k] for k in range(3)];L=math.sqrt(sum(v*v for v in d));d=[v/L for v in d]
            up=[0,1,0] if abs(d[1])<.9 else [0,0,1]
            u=[d[1]*up[2]-d[2]*up[1],d[2]*up[0]-d[0]*up[2],d[0]*up[1]-d[1]*up[0]];L=math.sqrt(sum(v*v for v in u));u=[v/L for v in u]
            v=[d[1]*u[2]-d[2]*u[1],d[2]*u[0]-d[0]*u[2],d[0]*u[1]-d[1]*u[0]]
            rings.append([tuple(p[k]+r*(math.cos(j*2*math.pi/n)*u[k]+math.sin(j*2*math.pi/n)*v[k]) for k in range(3)) for j in range(n)])
        for a,b in zip(rings,rings[1:]):
            for i in range(n):j=(i+1)%n;self.face(name,mat,[a[i],b[i],b[j],a[j]])
    def write(self):
        p=OUT/(self.name+'.obj')
        if p.exists() and not (BACKUP/p.name).exists():shutil.copy2(p,BACKUP/p.name)
        text=['# Rounded authored office prop; metres, Y-up','mtllib refined_palette.mtl','s 1'];idx=1;last=None
        for name,mat,pts in self.faces:
            if last!=(name,mat):text+=['g '+name,'usemtl '+mat];last=(name,mat)
            text+=['v %.6f %.6f %.6f'%p for p in pts]
            spans=[max(p[a] for p in pts)-min(p[a] for p in pts) for a in range(3)]
            axes=sorted(range(3),key=lambda a:spans[a],reverse=True)[:2]
            if set(axes)=={0,1}:axes=[0,1]
            elif set(axes)=={0,2}:axes=[0,2]
            else:axes=[2,1]
            low=[min(p[a] for p in pts) for a in axes]
            for p0 in pts:text.append('vt %.6f %.6f'%tuple((p0[a]-low[k])/(spans[a] or 1) for k,a in enumerate(axes)))
            text.append('f '+' '.join('%d/%d'%(idx+j,idx+j) for j in range(len(pts))));idx+=len(pts)
        p.write_text('\n'.join(text)+'\n')
        return dict(model=self.name,groups=self.materials,faces=len(self.faces))

