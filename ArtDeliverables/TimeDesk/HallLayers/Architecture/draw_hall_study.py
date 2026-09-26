"""Create original vector architectural study drawings; no scene or image edits."""
from pathlib import Path
import json, math, html, itertools

OUT = Path(__file__).resolve().parent
# HallLayers/Architecture -> TimeDesk
SOURCE = OUT.parent.parent / 'PaletteExploration/Applied/LayoutWear/after.json'
INK='#302D33'; MUTED='#726B70'; PAPER='#FAF7F0'; LINE='#D8D0C4'
OCHRE='#B87927'; PLUM='#75506F'; CORAL='#B75348'; BLUE='#577285'

class Drawing:
    def __init__(self,w=1800,h=1240):
        self.w=w; self.h=h
        self.parts=[f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}">',
        '<defs><marker id="ochre" markerWidth="9" markerHeight="9" refX="7" refY="4" orient="auto"><path d="M0 0 L8 4 L0 8" fill="none" stroke="#B87927" stroke-width="1.6"/></marker><marker id="plum" markerWidth="9" markerHeight="9" refX="7" refY="4" orient="auto"><path d="M0 0 L8 4 L0 8" fill="none" stroke="#75506F" stroke-width="1.6"/></marker><marker id="coral" markerWidth="9" markerHeight="9" refX="7" refY="4" orient="auto"><path d="M0 0 L8 4 L0 8" fill="none" stroke="#B75348" stroke-width="1.6"/></marker></defs>',
        f'<rect width="{w}" height="{h}" fill="{PAPER}"/>']
    def rect(self,x,y,w,h,fill='none',stroke=INK,sw=1.4,extra=''):
        self.parts.append(f'<rect x="{x:.2f}" y="{y:.2f}" width="{w:.2f}" height="{h:.2f}" fill="{fill}" stroke="{stroke}" stroke-width="{sw}" {extra}/>')
    def line(self,x1,y1,x2,y2,color=INK,sw=1.4,dash=''):
        self.parts.append(f'<line x1="{x1:.2f}" y1="{y1:.2f}" x2="{x2:.2f}" y2="{y2:.2f}" stroke="{color}" stroke-width="{sw}"'+(f' stroke-dasharray="{dash}"' if dash else '')+'/>')
    def path(self,points,color=INK,sw=2,dash='',arrow=None,fill='none'):
        points=list(points); d='M '+' L '.join(f'{x:.2f} {y:.2f}' for x,y in points)
        self.parts.append(f'<path d="{d}" fill="{fill}" stroke="{color}" stroke-width="{sw}" stroke-linecap="round" stroke-linejoin="round"'+(f' stroke-dasharray="{dash}"' if dash else '')+(f' marker-end="url(#{arrow})"' if arrow else '')+'/>')
    def circle(self,x,y,r,fill=PAPER,stroke=INK,sw=1.4):
        self.parts.append(f'<circle cx="{x:.2f}" cy="{y:.2f}" r="{r:.2f}" fill="{fill}" stroke="{stroke}" stroke-width="{sw}"/>')
    def text(self,x,y,text,size=17,color=INK,weight='400',anchor='start',line=1.35):
        lines=text.split('\n')
        self.parts.append(f'<text x="{x:.2f}" y="{y:.2f}" font-family="Arial, sans-serif" font-size="{size}" fill="{color}" font-weight="{weight}" text-anchor="{anchor}">')
        for i,t in enumerate(lines): self.parts.append(f'<tspan x="{x:.2f}" dy="{0 if i==0 else size*line}">{html.escape(t)}</tspan>')
        self.parts.append('</text>')
    def tag(self,x,y,label,color=INK):
        self.circle(x,y,15,PAPER,color,2); self.text(x,y+5,label,14,color,'700','middle')
    def dimh(self,x1,x2,y,label):
        self.line(x1,y,x2,y,MUTED,.9)
        for x in [x1,x2]: self.line(x-4,y+5,x+4,y-5,MUTED,1)
        self.text((x1+x2)/2,y-9,label,15,MUTED,anchor='middle')
    def dimv(self,x,y1,y2,label):
        self.line(x,y1,x,y2,MUTED,.9)
        for y in [y1,y2]: self.line(x-5,y+4,x+5,y-4,MUTED,1)
        self.parts.append(f'<text x="{x-10}" y="{(y1+y2)/2}" transform="rotate(-90 {x-10} {(y1+y2)/2})" font-family="Arial" font-size="15" text-anchor="middle" fill="{MUTED}">{html.escape(label)}</text>')
    def header(self,num,title,sub):
        self.text(55,47,'NOPE  /  HALL SPATIAL STUDY',15,MUTED,'700')
        self.text(55,96,title,34,INK,'700')
        self.text(57,131,sub,18,MUTED)
        self.text(1735,49,num,24,INK,'700','end')
        self.line(55,154,1745,154,LINE,1.5)
    def footer(self,desc):
        self.line(55,self.h-68,1745,self.h-68,LINE,1.5)
        self.text(55,self.h-40,desc,14,MUTED)
        self.text(1745,self.h-40,'26 SEP 2026  /  REV A  /  PROPOSAL',14,MUTED,anchor='end')
    def save(self,name):
        (OUT/name).write_text('\n'.join(self.parts+['</svg>']),encoding='utf-8')

# Source values are measured render bounds from the saved approved scene audit.
raw=json.loads(SOURCE.read_text(encoding='utf-8-sig'))
def bounds(prefix):
    rr=[r for r in raw['renderers'] if r['path'].startswith(prefix)]
    return {'min':{k:min(r['min'][k] for r in rr) for k in 'xyz'},'max':{k:max(r['max'][k] for r in rr) for k in 'xyz'}}
measured={
    'source':str(SOURCE.relative_to(OUT.parents[3])).replace('\\','/'),
    'floor':bounds('HybridOffice/Hall/Blender_HallFloor/'),
    'desk':bounds('HybridOffice/Booth/Finish_Desk/'),
    'portal_ring':bounds('HybridOffice/Hall/Blender_PortalRing/Hall_Portal__Portal_Coil'),
    'camera_position':raw['cameraPosition'], 'camera_rotation':raw['cameraRotation'],
    'camera_vertical_fov_degrees':55,
}
(OUT/'measured_scene.json').write_text(json.dumps(measured,indent=2),encoding='utf-8')

# Plan in Unity's horizontal axes: x across the room, z toward the portal.
S=24; X=lambda x:650+S*x; Z=lambda z:902-S*z
plan=Drawing()
plan.header('A01','A hall with a route through it','Top view  /  existing 28 × 30 m hall retained  /  proposed adjacent building connections')
def pr(x,z,w,d,fill='none',stroke=INK,sw=1.4,extra=''):
    plan.rect(X(x),Z(z+d),w*S,d*S,fill,stroke,sw,extra)
def pl(x1,z1,x2,z2,color=INK,sw=2,dash=''): plan.line(X(x1),Z(z1),X(x2),Z(z2),color,sw,dash)
def pt(x,z,t,size=15,color=INK,weight='400',anchor='middle'): plan.text(X(x),Z(z),t,size,color,weight,anchor)
def pp(points,color,arrow=None,dash=''): plan.path([(X(x),Z(z)) for x,z in points],color,3,dash,arrow)

# Public concourse and connecting cores: extensions are schematic, hall is measured.
pr(-14,-3,28,30,'#F0EBE1','none')
pr(-22,19,8,10,'#EEE8DC',LINE)
pr(7,17,7,8,'#EEE8DC',LINE)
pr(14,20,6,5,'#EEE8DC',LINE)
pr(14,-5.5,3,25.5,'#E2DDD5',LINE)
pr(19,8,3,18,'#EEE8DC',LINE)
pr(22,19.8,3.5,5.2,'#EEE8DC',LINE)
pr(22,8.1,3.5,4.9,'#EEE8DC',LINE)
pr(19,25.8,6.5,2.7,'#EEE8DC',LINE)
pr(-14,-5.5,28,2.5,'#E2DDD5',LINE)
pt(0,-4.4,'RESTRICTED STAFF GALLERY  •  CONTINUES BEHIND ALL THREE DESKS',14)

# A narrow central strip is intentionally empty; this is not a floor repaint.
pr(-2.4,2.1,4.8,15.7,'#E7DEE7','none')
pt(0,13.2,'CLEAR\nDEPARTURE\nAPPROACH',15,PLUM,'700')
pt(0,9.7,'4.8 m planning reserve',12,MUTED)

# Walls, with deliberate large openings to cores; rear stays glazed.
pl(-14,-3,-14,21,INK,6); pl(-14,25,-14,27,INK,6)
pl(14,-3,14,20,INK,6); pl(14,25,14,27,INK,6)
for a,b in [(-14,-6.3),(-4.5,-.9),(.9,4.5),(6.3,14)]: pl(a,-3,b,-3,INK,5)
for a,b in [(-14,-7),(-3.8,-1.6),(1.6,3.8),(7,14)]: pl(a,.55,b,.55,INK,3)
pl(17,-5.5,17,20,INK,3)
pl(14,-5.5,17,-5.5,INK,3)
pl(14,-5.5,14,-4.9,INK,3); pl(14,-3.5,14,-3,INK,3)
pl(14,-4.9,15.4,-4.9,INK,1)
pt(15.5,-6.1,'STAFF ACCESS',11,MUTED)
pl(-14,27,14,27,INK,3); pl(-14,26.8,14,26.8,BLUE,2)
pt(0,26,'REAR GLAZING / CITY BEYOND',13,BLUE,'700')
# Small side windows in thick side walls.
for side in [-14,14]:
    for z in [7.5,11]:
        pl(side,z,side,z+1.4,PAPER,7); pl(side,z,side,z+1.4,BLUE,2)

# Arrival core: lift and genuine stair flight; public doorway and depth.
pr(-21,25.1,3.2,3.2,'#DDD5C8',INK)
pt(-19.4,26.5,'LIFT',13)
pl(-19.9,25.1,-18.9,25.1,PAPER,4)
pr(-21,19.7,3.2,4,'none',INK)
for z in [19.9+i*.36 for i in range(11)]: pl(-21,z,-17.8,z,INK,.8)
pp([(-19.4,20),(-19.4,23.3)],BLUE)
pt(-19.4,19.1,'STAIR FROM LOBBY',11)
pt(-18,29.8,'PUBLIC ARRIVAL CORE',15,INK,'700')
pt(-16.3,24.2,'ENTRY',12,OCHRE,'700')
pl(-14,21,-15.3,21,INK,3); pl(-14,25,-15.3,25,INK,3)
plan.dimv(X(-15.3),Z(21),Z(25),'4 m threshold')

# Luggage lockers: 0.8 deep, mixed compartments detailed on A02.
pr(-11.8,17.4,5.6,.8,'#DDC7B9',INK)
for x in [-11.8+i*.56 for i in range(11)]: pl(x,17.4,x,18.2,INK,.6)
pr(-11.8,15.6,5.6,1.8,'none',MUTED,.9,'stroke-dasharray="4 4"')
pt(-9,19.1,'LUGGAGE STORAGE',13,INK,'700')
pt(-9,15.05,'1.8 m access apron',12,MUTED)
plan.tag(X(-12.7),Z(17.8),'L',OCHRE)

# Waiting seats, storage and displays remain outside travel aisles.
for x,z,w,d in [(-9.3,9.8,4.2,.7),(-9.3,11.8,4.2,.7),(7.1,10.7,4.8,.7),(7.1,12.7,4.8,.7)]:
    pr(x,z,w,d,'#D3C1D0',INK)
    for xx in [x+i*.7 for i in range(1,int(w/.7))]: pl(xx,z,xx,z+d,INK,.7)
pt(-7.2,13.3,'PRE-CHECK WAITING',12)
pt(9.5,14.4,'WAIT / REFERRAL',12)

# Queue zone and counter row, facing into the hall (up the drawing).
pr(-9,4,5.8,4.8,'none',MUTED,1,'stroke-dasharray="5 4"')
pt(-6.1,7.5,'CALL / QUEUE',13)
pt(-6.1,6.5,'Waiting stays left\nof the portal axis',11,MUTED)
for x in [-5.4,0,5.4]:
    pr(x-2.4,-3,4.8,2.45,'#E8E2D9',LINE)
    pr(x-1.6,-.55,3.2,1.1,'#D7B790',INK,1.7)
    plan.circle(X(x),Z(-1.7),7,PAPER,INK)
    pt(x,-.10,('01' if x<0 else '02 / PLAYER' if x==0 else '03'),12,INK,'700')
    # A standing visitor's plan footprint.
    plan.circle(X(x),Z(1.15),6,PAPER,INK)
    pl(x-1.9,-3,x-1.9,.6,INK,2)
pt(0,-6.35,'THREE CHECK DESKS ON ONE LINE  /  OTHER TWO OUTSIDE THE MAIN CAMERA',14,INK,'700')

# Current central desk footprint stays as a visible audit comparison.
pr(-2.9,-1.2695,5.8,2.5295,'none',CORAL,1.7,'stroke-dasharray="6 4"')

# Existing portal footprint and back-of-machine maintenance access.
pr(-3.325,19.225,6.65,1.55,'#CAD4DA',INK,2)
pl(-2.97,20.03,2.97,20.03,BLUE,8)
pt(0,22,'PORTAL / DEPARTURE',15,INK,'700')
pt(0,21.1,'6.65 m existing base',12,MUTED)
pr(3.575,19.675,1.05,.65,'#DDD4C6',INK)
pt(0,23.55,'6.2 m rear service space retained',12,MUTED)

# One framed picture + two individual institutional exhibit positions.
pl(-13.8,25.3,-13.8,26.6,PLUM,5)
plan.tag(X(-13.1),Z(26),'P',PLUM)
for x,tag in [(-10,'A'),(6,'B')]:
    pr(x-.6,24,.95,1.0,'#D8D1C6',INK)
    plan.tag(X(x-.13),Z(24.5),tag)
pt(-9.8,22.8,'CERAMIC',11,MUTED)
pt(6,22.8,'STONE RELIEF',11,MUTED)

# The right wall is a deep cross-corridor, with rooms beyond the visible opening.
pl(7,17,7,25,INK,5); pl(7,25,19,25,INK,5)
pl(7,17,8,17,INK,5); pl(12,17,14,17,INK,5)
pl(17,20,19,20,INK,5)
pl(14,20,17,20,MUTED,1,'3 3')
pl(19,8,19,20,INK,4); pl(19,25,19,28.5,INK,4)
pl(22,8,22,9.5,INK,3); pl(22,11.2,22,21.5,INK,3); pl(22,23.2,22,28.5,INK,3)
for z0 in [9.5,21.5]:
    pl(22,z0,23.6,z0,INK,1.1)
    plan.parts.append(f'<path d="M {X(22)} {Z(z0+1.6)} A {S*1.6} {S*1.6} 0 0 1 {X(23.6)} {Z(z0)}" fill="none" stroke="{MUTED}" stroke-width=".9"/>')
pt(23.75,23.5,'MEDBAY',12,INK,'700')
pt(23.75,11.6,'JAIL',12,INK,'700')
pt(22.25,27.5,'C-SUITES',12,INK,'700')
pt(22.25,26.45,'secure lift / stair',10,MUTED)
pt(11,26.0,'RECESSED CORRIDOR',12,INK,'700')
pt(10,18.2,'4 m opening',11,MUTED)
plan.tag(X(7.2),Z(17),'S',CORAL)

arrival=[(-16.6,23),(-12.6,23),(-12.6,16.4),(-9.5,16.4),(-10.2,14.5),(-10.2,8.4),(-9.6,3.3),(-2.1,3.3),(0,1.15)]
approved=[(0,1.75),(0,8),(0,12),(0,18.1)]
referral=[(.8,1.7),(3.4,3.4),(10.2,3.4),(12.9,7),(12.9,14.5),(10,17),(10,22),(20.4,22)]
pp(arrival,OCHRE,'ochre')
pp(approved,PLUM,'plum')
pp(referral,CORAL,'coral','7 5')

# Exact source camera cone, flattened to plan at camera eye height.
camera=(0,-2.62)
half=math.atan(math.tan(math.radians(55/2))*16/9)
for sign in [-1,1]:
    endx=sign*14; endz=camera[1]+14/math.tan(half)
    pl(camera[0],camera[1],endx,endz,BLUE,1,'5 6')
plan.circle(X(0),Z(-2.62),4,BLUE,BLUE)
pt(3.6,-2.6,'existing camera',10,BLUE)
pl(0,-3.1,0,27,MUTED,.8,'12 5 2 5')
plan.tag(X(0),Z(28),'A',MUTED); plan.tag(X(0),Z(-7.8),'A',MUTED)

plan.dimh(X(-14),X(14),Z(28.8),'28.0 m existing floor width')
plan.dimv(90,Z(-3),Z(27),'30.0 m existing floor depth')
plan.text(1265,223,'HOW THE BUILDING WORKS',21,INK,'700')
notes=[
('01  ARRIVE FROM THE BUILDING','Level 02 is the proposed departures floor.\nA public lift and stair connect to the\nstreet-level admissions lobby below.'),
('02  STORE, WAIT, CHECK','Train-station luggage compartments.\nOne common waiting zone serves three\ndesks. Only the middle desk is the player’s.'),
('03  DEPART FOR THE PAST','After the check, turn back into the hall\nand walk along the clear central route\nto the existing portal.'),
('04  THE RIGHT SIDE CONTINUES','A forward-facing mouth is visible past\nthe corkboard. Walk into the recess,\nthen turn right toward the departments.'),
('05  DISPLAY, DO NOT SCATTER','P = one painting. A = ceramic object.\nB = stone relief. Each has an intentional\ndisplay anchor; final artifacts are TBD.'),
]
y=265
for title,body in notes:
    plan.text(1265,y,title,17,INK,'700'); plan.text(1265,y+29,body,16,MUTED); y+=140
plan.text(1265,992,'SOURCE / PROPOSAL',17,INK,'700')
plan.text(1265,1022,'Solid hall perimeter + portal: source dimensions.\nNew bays, cores and furniture: design proposal.\nRed dashed desk: current 5.8 × 2.53 m footprint.\nBlue rays: approximate existing horizontal view.',15,MUTED)

# Scale bar and route legend.
for i in range(5): plan.rect(130+i*24,1110,24,8,INK if i%2==0 else PAPER,INK,.7)
plan.text(130,1142,'0',12,MUTED); plan.text(250,1142,'5 m',12,MUTED,anchor='end')
for x,c,t,dash in [(340,OCHRE,'BEFORE CHECK',''),(600,PLUM,'APPROVED DEPARTURE',''),(935,CORAL,'REFERRAL ROUTE','7 5')]:
    plan.line(x,1115,x+50,1115,c,3,dash); plan.text(x+65,1121,t,14,c,'700')
plan.footer('Scale bar governs printed scale. Source camera and floor unchanged in Unity. Proposed layout only.')
plan.save('A01_top_plan.svg')

# Longitudinal section uses the original hall envelope. Side features are projected.
sec=Drawing()
sec.header('A02','Check the human scale before dressing the room','Section A–A  /  portal, desk and camera heights  /  proposed storage detail')
Q=28; SX=lambda z:180+(z+5.5)*Q; SY=lambda y:670-y*Q
def sr(z,y,d,h,fill='none',stroke=INK,sw=1.4,extra=''):
    sec.rect(SX(z),SY(y+h),d*Q,h*Q,fill,stroke,sw,extra)
def sl(z1,y1,z2,y2,color=INK,sw=1.5,dash=''): sec.line(SX(z1),SY(y1),SX(z2),SY(y2),color,sw,dash)
def st(z,y,t,size=15,color=INK,weight='400',anchor='middle'): sec.text(SX(z),SY(y),t,size,color,weight,anchor)
def human(z,h=1.75,color=INK):
    # Orthographic human-height marker, deliberately not character art.
    cx=SX(z); fy=SY(0); scale=Q
    sec.circle(cx,fy-(h-.13)*scale,.12*scale,PAPER,color,1.5)
    sec.line(cx,fy-(h-.27)*scale,cx,fy-.72*scale,color,2)
    sec.path([(cx-.25*scale,fy-1.02*scale),(cx,fy-1.42*scale),(cx+.25*scale,fy-1.02*scale)],color,1.5)
    sec.path([(cx-.18*scale,fy),(cx,fy-.72*scale),(cx+.18*scale,fy)],color,1.5)

sr(-5.5,-.35,35.5,.35,'#AFA89D',INK)
sr(-5.5,-6.2,35.5,.35,'#D8D0C4',INK)
sl(-3,0,-3,14.35,INK,4); sl(27,0,27,14.35,INK,4)
sl(-3,14.35,27,14.35,MUTED,1.3,'7 5')
st(12,14.7,'EXISTING STRUCTURAL ENVELOPE ≈ 14.4 m — NOT A NEW CEILING',13,MUTED)
sl(27,1.6,27,12.8,BLUE,5)
st(24.1,10,'REAR\nGLAZING',14,BLUE)
sl(24.8,9.6,27,9.6,BLUE,1)
sec.dimv(1160,SY(0),SY(14.35),'14.35 m source envelope')

# Projected corridor profile and reduced side windows show actual occupied scale.
sr(17,0,8,3.6,'none',MUTED,1,'stroke-dasharray="5 5"')
st(23.2,3.0,'SIDE PASSAGE\n(projected)',10,MUTED)
for z in [7.5,11]: sr(z,2.4,1.4,2.1,'#E0E7E9',BLUE,1)
st(9,5.3,'SMALLER SIDE WINDOWS\n2.4 m sill / 4.5 m head, proposed',12,BLUE)

# Current desk is ghosted, proposed compact footprint drawn solid.
sr(-1.2695,.90,2.5295,.16,'none',CORAL,1.8,'stroke-dasharray="5 4"')
sr(-.55,.93,1.1,.12,'#D7B790',INK,2)
sl(-.44,0,-.44,.93,INK,3); sl(.44,0,.44,.93,INK,3)
human(-1.2); human(1.15)
st(-.2,3.5,'CHECK DESK',14,INK,'700')
st(-.2,2.65,'1.05 m counter\n1.10 m target depth',12,MUTED)
sec.dimh(SX(-.55),SX(.55),SY(-.9),'1.10 m')

# Eye/camera study: current high camera versus human reference, no runtime decision.
sec.circle(SX(-2.62),SY(2.16),4,CORAL,CORAL)
sl(-2.62,2.16,4,2.16-math.tan(math.radians(10))*(4+2.62),CORAL,1.2,'5 5')
sec.text(65,520,'SOURCE CAMERA\n2.16 m high\n10° downward',13,CORAL,'700')
sec.path([(166,552),(SX(-2.62),SY(2.16))],CORAL,1)
sec.circle(SX(-1.2),SY(1.62),2.5,BLUE,BLUE)

# Portal profile (diameter seen edge-on in this section), plus rear service space.
sr(19.225,0,1.55,.2,'#CAD4DA',INK)
sr(19.67,.08,.90,5.94,'#CAD4DA',INK,2)
sl(20.03,.71,20.03,5.39,BLUE,4)
st(20,7.4,'EXISTING PORTAL',15,INK,'700')
st(20,6.5,'6.0 m high',13,MUTED)
human(17.8)
st(14.1,1.1,'1.75 m\nhuman datum',12,MUTED)
sec.dimh(SX(.55),SX(19.225),SY(-1.55),'18.68 m desk-front to portal base')
sec.dimh(SX(20.775),SX(27),SY(-1.55),'6.23 m')
st(12.3,-4.2,'PROPOSED LOBBY / ADMISSIONS FLOOR BELOW  •  +0.00 BUILDING DATUM',15,MUTED)
st(10.5,-2.9,'HALL FLOOR = LEVEL 02  /  +6.00 m proposed  /  Unity local Y = 0',13,INK,'700')

sec.line(1230,193,1230,830,LINE,1.5)
sec.text(1270,218,'SANITY CHECK',22,INK,'700')
audit=[
('KEEP','28 × 30 m works as a shared concourse.\nIt needs a public core, a staff side and\nthree counter bays to explain its size.'),
('REWORK BEFORE FINAL ART','Current desk: 5.8 × 2.53 m.\nStudy target: 3.2 × 1.10 m per station.\nDo not auto-scale the PC or its content.'),
('CAMERA DECISION STILL OPEN','The 2.16 m source camera helps expose\nthe tabletop, but is above a 1.62 m\nstanding-eye reference. Blockout first.'),
('BUILDING LEVEL IS A PROPOSAL','One floor above street admissions.\nPublic lift/stair on the left; secure\ndepartment circulation on the right.'),
('NO SCENE MUTATION','Original floor, portal and gameplay\nremain untouched. Desk resizing is\nonly an envelope study on these sheets.'),
]
y=256
for title,body in audit:
    sec.text(1270,y,title,16,INK,'700'); sec.text(1270,y+27,body,16,MUTED); y+=114

# Luggage locker front elevation: mixed size baggage compartments, not wardrobes.
sec.line(55,870,1745,870,LINE,1.5)
sec.text(65,908,'L  /  LUGGAGE LOCKER BANK',21,INK,'700')
sec.text(65,939,'Front elevation  •  mixed bag / suitcase sizes',15,MUTED)
LX=95; LY=1125; LS=76
sec.rect(LX,LY-1.95*LS,2.8*LS,1.95*LS,'#DDC7B9',INK,1.8)
for i in range(5):
    xx=LX+i*.56*LS
    sec.line(xx,LY-1.95*LS,xx,LY,INK,1)
    cuts=[.95,1.45] if i%2==0 else [.65,1.30]
    for h in cuts: sec.line(xx,LY-h*LS,xx+.56*LS,LY-h*LS,INK,1)
    for j,h in enumerate([.30,1.12,1.7]):
        sec.circle(xx+.46*LS,LY-h*LS,2,INK,INK,.5)
        sec.text(xx+6,LY-h*LS,str(101+i*3+j),9,MUTED)
sec.dimh(LX,LX+2.8*LS,LY+21,'2.8 m example module')
sec.dimv(LX-20,LY,LY-1.95*LS,'1.95 m')
sec.text(355,989,'Numbered key / coin locks.\n0.8 m cabinet depth.\n1.8 m clear standing apron.\nTwo to three modules form the left bank.',16,MUTED)
sec.text(800,908,'P + A + B  /  A SMALL INSTITUTIONAL COLLECTION',21,INK,'700')
sec.text(800,947,'One framed painting, one ceramic vessel and one stone relief.\nUse fixed display positions with plinths, cases or wall mounts.\nNeglect belongs to the display: faded labels, dust, failed lights, worn frames.\nThese are layout markers; the final artifacts have not been selected.',17,MUTED)
sec.text(800,1070,'NEXT VISUAL: an untextured blockout from this plan,\nchecked through the actual game camera before finished sprites.',18,INK,'700')
sec.footer('Orthographic section at equal horizontal / vertical scale. Side windows and passage shown projected, dashed where appropriate.')
sec.save('A02_side_section.svg')

# Basic, meaningful spatial checks on the proposal; not a Unity or code test.
obstacles=[
    ('left lockers',(-11.8,17.4,-6.2,18.2)),
    ('left bench 1',(-9.3,9.8,-5.1,10.5)),('left bench 2',(-9.3,11.8,-5.1,12.5)),
    ('right bench 1',(7.1,10.7,11.9,11.4)),('right bench 2',(7.1,12.7,11.9,13.4)),
    ('left display',(-10.6,24,-9.65,25)),('right display',(5.4,24,6.35,25)),
]+[(f'counter {i+1}',(x-1.6,-.55,x+1.6,.55)) for i,x in enumerate([-5.4,0,5.4])]
def path_hits(path,radius=.3):
    hits=set()
    for a,b in zip(path,path[1:]):
        length=math.dist(a,b); steps=max(1,math.ceil(length/.05))
        for i in range(steps+1):
            t=i/steps; x=a[0]+(b[0]-a[0])*t; z=a[1]+(b[1]-a[1])*t
            for name,(x0,z0,x1,z1) in obstacles:
                dx=max(x0-x,0,x-x1); dz=max(z0-z,0,z-z1)
                if math.hypot(dx,dz)<radius: hits.add(name)
    return sorted(hits)
def path_length(path): return round(sum(math.dist(a,b) for a,b in zip(path,path[1:])),2)

# Conservative projected bounds catch obvious corkboard occlusion before a blockout.
# This is not full visibility: AABBs and a few target centers cannot prove whole-object visibility.
cp=raw['cameraPosition']; pitch=math.radians(raw['cameraRotation']['x'])
tanv=math.tan(math.radians(55/2)); tanh=tanv*16/9
def project(point):
    x,y,z=point; dy=y-cp['y']; dz=z-cp['z']
    up=math.cos(pitch)*dy+math.sin(pitch)*dz
    forward=math.cos(pitch)*dz-math.sin(pitch)*dy
    return ((x-cp['x'])/(forward*tanh),up/(forward*tanv))
board_rects={}
for label,root in [('left','Finish_Board'),('right','Finish_RightBoard')]:
    bb=bounds('HybridOffice/Booth/'+root+'/')
    projected=[project(p) for p in itertools.product(*[(bb['min'][k],bb['max'][k]) for k in 'xyz'])]
    board_rects[label]=[min(p[0] for p in projected),min(p[1] for p in projected),max(p[0] for p in projected),max(p[1] for p in projected)]
def visibility(point):
    x,y=project(point)
    hits=[side for side,(x0,y0,x1,y1) in board_rects.items() if x0<=x<=x1 and y0<=y<=y1]
    return {'ndc':[round(x,4),round(y,4)],'inside_frame':abs(x)<=1 and abs(y)<=1,'corkboard_overlap':hits}
targets={'left_entry_center':(-14,1.8,23),'right_corridor_mouth_center':(10,1.8,17),'locker_bank_center':(-9,1,17.8),'one_painting_center':(-13.8,2.2,26),'portal_center':(0,3.05,20.03)}
checks={
 'scope':'2D design-space checks only; no Unity scene checks or changes',
 'confirmed_flow':'building -> desk check -> portal departure',
 'proposal_level':'Level 02, one floor above street admissions; not established canon',
 'metres_assumption':'Unity units treated as metres for this study',
 'hall_footprint_metres':[28,30], 'counter_count':3,
 'counter_target_metres':[3.2,1.1,1.05],
 'arrival_route_obstacle_hits_0_6m_person':path_hits(arrival),
 'departure_route_obstacle_hits_0_6m_person':path_hits(approved),
 'referral_route_obstacle_hits_0_6m_person':path_hits(referral),
 'arrival_route_length_metres':path_length(arrival),
 'departure_route_length_metres':path_length(approved),
 'central_aisle_width_metres':4.8, 'left_entry_opening_metres':4,
 'right_forward_facing_opening_metres':4, 'right_outer_cross_corridor_metres':5, 'locker_clear_apron_metres':1.8,
 'source_camera_horizontal_fov_degrees':round(math.degrees(half)*2,2),
 'portal_base_to_rear_wall_metres':round(27-20.775,3),
 'target_center_visibility_checks':{name:visibility(point) for name,point in targets.items()},
 'rejected_positions_due_to_board_occlusion':{'right_sidewall_at_z16_5':visibility((14,1.8,16.5)),'left_wall_lockers_at_z13_3':visibility((-12.7,1,13.3))},
 'deferred':['Actual camera perspective/occlusion blockout','Desk prop and interaction layout if envelope shrinks','Room heights and building floor designation approval','Three sprite layers derived from one shared camera','Department hooks remain gameplay-owned'],
}
(OUT/'sanity_check.json').write_text(json.dumps(checks,indent=2),encoding='utf-8')
print(json.dumps(checks,indent=2))
