"""Multilevel terminal concept drawings. No changes to Unity or prior drawings."""
from pathlib import Path
import math,json
HERE=Path(__file__).resolve().parent
helper=HERE.parent/'draw_hall_study.py'
e={'__file__':str(helper)}
exec(compile(helper.read_text(encoding='utf-8').split('# Source values')[0],str(helper),'exec'),e)
e['OUT']=HERE
D=e['Drawing'];INK=e['INK'];PAPER=e['PAPER'];MUTED=e['MUTED'];LINE=e['LINE'];BLUE=e['BLUE'];PLUM=e['PLUM'];OCHRE=e['OCHRE'];CORAL=e['CORAL']
def save(d,name):
 d.save(name);p=HERE/name;p.write_text(p.read_text(encoding='utf-8').replace('REV A','REV C'),encoding='utf-8')

p=D();p.header('C01','A grand terminal for departures through time','Inspection concourse plan  /  lower and upper platforms shown in context  /  proposal')
S=10;X=lambda x:505+x*S;Z=lambda z:1030-z*S
def r(x,z,w,d,f='none',c=INK,sw=1.3,extra=''):p.rect(X(x),Z(z+d),w*S,d*S,f,c,sw,extra)
def l(x,z,xx,zz,c=INK,sw=2,dash=''):p.line(X(x),Z(z),X(xx),Z(zz),c,sw,dash)
def t(x,z,s,size=13,c=INK,b='400',anchor='middle'):p.text(X(x),Z(z),s,size,c,b,anchor)
def route(points,c,arrow=None,dash=''):p.path([(X(x),Z(z)) for x,z in points],c,2.8,dash,arrow)

# Levels: existing inspection floor 0; lower -6; upper +6; high roof +24.
r(-30,-6,60,81,'#EBE4D8','none')
r(-25,4.5,45,60.5,'#DDD3C3',OCHRE,1,'stroke-dasharray="5 4"')
t(-4,68,'GRAND CENTRAL VOLUME / CITY AT THE TOWER CROWN',13,INK,'700')
t(-4,64,'OPEN TO LOWER DEPARTURE FLOOR  −6 m',13,OCHRE,'700')
l(-30,-6,-30,75,INK,5);l(-30,75,30,75,INK,5)
l(-30,-6,-30,75,BLUE,3);l(-30,75,30,75,BLUE,3)
l(30,-6,30,6,INK,5);l(30,20,30,75,INK,5)
for z in [-5,7,19,31,43,55,67]:r(-30,z,1.2,1.2,INK,INK)
for x in [-18,-6,6,18]:r(x,73.8,1.2,1.2,INK,INK)
p.parts.append(f'<text x="{X(-34)}" y="{Z(39)}" transform="rotate(-90 {X(-34)} {Z(39)})" font-family="Arial" font-size="14" font-weight="700" text-anchor="middle" fill="{BLUE}">WEST WINDOW / CITY FAR BELOW</text>')

# Upper gallery and platforms run along the right; legs / undercroft are explicit in section.
r(17,28,13,43,'#DBD0DF',PLUM,2)
t(23.5,73,'UPPER GALLERY  +6 m',13,PLUM,'700')
# The bridge is above the inspection level; no opaque block is placed in front of the desk.
r(-25,57,42,4,'#E6DDE7',PLUM,1.2,'stroke-dasharray="6 3"')
t(-4,59,'HIGH CROSSING / +6 m',11,PLUM,'700')
r(-5.5,7,35.5,4,'none',PLUM,1.1,'stroke-dasharray="6 3"')
r(25,11,5,17,'none',PLUM,1.1,'stroke-dasharray="6 3"')
t(14,9.1,'UPPER LINK TO PLATFORM LIFT',8,PLUM)
# Upper bridge support lines in plan.
for x,z in [(-24,58),(16,58),(18,35),(18,66)]:r(x-.6,z-.6,1.2,1.2,'#AFA596',INK)

# Each numbered platform is a gated holding area with its own portal.
platforms=[('01',-18,28,-6),('02',3,47,-6),('03',21,48,6),('04',21,65,6)]
for label,x,z,level in platforms:
 color=OCHRE if level<0 else PLUM
 r(x-4.5,z-8,9,11,'#E8DECE' if level<0 else '#DED1E0',color,1.5)
 r(x-3.325,z-.775,6.65,1.55,'#CBD6DB',INK,1.5)
 l(x-2.97,z,x+2.97,z,BLUE,5)
 p.tag(X(x),Z(z-4.2),label,color)
 t(x,z+4,f'PLATFORM {label}',12,color,'700')
 t(x,z-7,'BOARDING GATE',9,MUTED)
t(-14,37,'LOWER PLATFORMS / −6 m',13,OCHRE,'700')
r(-7,30,8,.5,'none',MUTED,1,'stroke-dasharray="3 2"')
t(-3,32.3,'DEPARTURE BOARD',10,MUTED)

# Front edge of concourse: transparent guard is essential to the downward view.
l(-25,4.5,-12,4.5,BLUE,2);l(-2,4.5,20,4.5,BLUE,2)
t(5,6.9,'CLEAR GLASS GUARD / VIEW DOWN',10,BLUE)
# Controlled platform stair and lift accessible after desk clearance.
r(-12,5,5.5,16,'#D3C7B6',INK)
for z in [5+i*.35 for i in range(19)]+[12.8+i*.35 for i in range(19)]:l(-12,z,-6.5,z,INK,.7)
route([(-9.3,6),(-9.3,19)],OCHRE,'ochre')
t(-9.3,23,'DOWN TO 01 / 02',10,OCHRE,'700')
r(-5.5,7,3.5,4,'#D3C7B6',INK)
t(-3.75,8.7,'LIFT',8)
t(1,12,'PLATFORM LIFT\n−6 / 0 / +6 m',10,MUTED)

# Right arrival core is public, with a real continuing corridor to departments.
r(30,6,13,14,'#E3DACD',INK)
for x in [32,37]:r(x,15,3.5,3.5,'#D3C7B6',INK);t(x+1.75,16.5,'LIFT',8)
t(36.5,22.3,'ARRIVALS FROM TOWER',13,INK,'700')
t(36.5,10.8,'MEDBAY / JAIL\nC-SUITES →',11,MUTED)
r(30,-6,6,12,'#DCD5CB',LINE)

# Private inspection booth: desk normal stays parallel to hall axes.
r(-25,-6,10,9,'#EADFD0',INK,1.3)
l(-15,-6,-15,3,INK,5) # opaque right return
l(-25,-6,-25,-3,INK,4)
l(-25,-3,-25,3,BLUE,2) # clear left bay to city
l(-25,-6,-21,-6,INK,4);l(-19,-6,-15,-6,INK,4) # staff door behind
l(-25,3,-23.2,3,INK,4);l(-16.8,3,-15,3,INK,4)
r(-22.9,-1.265,5.8,2.53,'#D7B790',INK,1.8)
t(-20,-.1,'PLAYER',10,INK,'700')
t(-20,-9,'PRIVATE BOOTH / STRAIGHT DESK',13,INK,'700')
t(-19,8,'ONE VISITOR',10,INK,'700')
p.circle(X(-20),Z(2.1),3,PAPER,INK)

# Work accommodation sits behind the opaque return, outside all intended player views.
r(-14,-6,44,7,'#D8D2C9',INK,1.3)
t(8,-2.2,'ENCLOSED STAFF / SERVICE ACCOMMODATION',11,MUTED)
t(8,-4.5,'NO VISIBLE DESKS OR COWORKERS',11,INK,'700')

# Luggage / waiting belong to the public concourse, not the booth or platform edge.
r(-29,13,3.2,10,'#DDC7B9',INK)
for z in [13+i for i in range(11)]:l(-29,z,-25.8,z,INK,.6)
t(-27.4,25.2,'LUGGAGE',10,INK,'700')
t(-27.4,23.8,'LOCKERS',10,INK,'700')
for z in [26,33,40]:r(23,z,5,.8,'#CEBECE',INK)
t(24,24,'WAIT / CALL',11,MUTED)

# One wall picture and two artifact anchors on the concourse perimeter.
for x,z,label in [(-28,46,'P'),(-28,54,'A'),(27,54,'B')]:p.tag(X(x),Z(z),label)

# Distinct flows are schematic on their levels; these do not prescribe gameplay.
route([(35,13),(26,13),(22,4),(22,2.3),(-13,2.3),(-14,3.8),(-20,3.8),(-20,2.1)],OCHRE,'ochre')
route([(-19,2.4),(-16,3.8),(-9.3,3.8),(-9.3,6)],PLUM,'plum')
route([(-9.3,21),(-12,23),(-18,23)],OCHRE,'ochre','5 4')

# Camera headings: straight forward and 60 degrees left; no tilted station.
eye=(-20,-2.62)
window=(-30,eye[1]+10/math.tan(math.radians(60)))
for yaw,col,length in [(0,PLUM,32),(-60,BLUE,13)]:
 end=(eye[0]+length*math.sin(math.radians(yaw)),eye[1]+length*math.cos(math.radians(yaw)))
 route([eye,end],col,None,'5 4')
p.circle(X(eye[0]),Z(eye[1]),4,PLUM,PLUM)
t(-32,4,'LEFT LOOK',11,BLUE,'700')
p.dimh(X(-30),X(30),Z(82),'60 m study width')
p.dimv(142,Z(-6),Z(75),'81 m study depth')
p.text(1050,223,'TERMINAL FIRST. OFFICE SECOND.',23,INK,'700')
notes=[
('01  THE PLAYER HAS A PRIVATE BOOTH','One straight-facing desk. The right return and\nrear service zone conceal all neighbouring work.\nThere is no visible row of clerks.'),
('02  PEOPLE BOARD PLATFORMS','Four numbered departure platforms in this study.\nEach has a holding area, boarding gate and teleporter.\nThe platform count is a proposal, not fixed canon.'),
('03  THREE LEGIBLE LEVELS','Inspection concourse: 0 m, existing desk floor datum.\nLower platforms: −6 m. Upper platforms: +6 m.\nA high crossing and supported galleries reveal depth.'),
('04  THE BUILDING HAS A ROUTE','Tower lifts → luggage / waiting → desk clearance.\nThen the controlled platform stair or lift.\nDepartment routes continue through the right core.'),
('05  THE LEFT VIEW IS THE CITY','A clear side bay faces the tower’s west glazing.\nPan left from the same booth; no coworkers enter view.\nThe whole pan still needs a 3D occlusion check.'),
('06  DISPLAYS ARE SPARSE AND DELIBERATE','P = one painting. A / B = world artifacts.\nKeep the terminal’s scale and circulation dominant.\nNeglect belongs to fixtures, displays and repairs.'),
]
y=275
for a,b in notes:
 p.text(1050,y,a,17,INK,'700');p.text(1050,y+29,b,17,MUTED);y+=138
p.footer('Levels and routes are architectural proposals. Current Unity scene, PC, original floor assets and gameplay remain untouched.')
save(p,'C01_terminal_plan.svg')

q=D();q.header('C02','A private booth overlooking a multilevel terminal','Longitudinal section / side platforms projected  /  inspector floor kept at the existing zero datum')
SX=lambda z:110+10*(z+6);SY=lambda y:608-y*10
def sr(z,y,w,h,f='none',c=INK,sw=1.4):q.rect(SX(z),SY(y+h),w*10,h*10,f,c,sw)
def sl(z,y,zz,yy,c=INK,sw=1.5,dash=''):q.line(SX(z),SY(y),SX(zz),SY(yy),c,sw,dash)
def st(z,y,s,size=13,c=INK,b='400'):q.text(SX(z),SY(y),s,size,c,b,'middle')
sr(-6,-6.4,81,.4,'#BCAE9A',INK)
sr(-6,-.35,10.5,.35,'#BCAE9A',INK)
sl(4.5,0,4.5,1.1,BLUE,2)
sl(-6,-6,-6,24,INK,4);sl(75,-6,75,24,INK,4)
sl(-6,24,75,24,MUTED,1.3,'6 4')
sl(75,1,75,22.5,BLUE,3)
st(35,27,'MONUMENTAL TOWER-CROWN VOLUME / 30 m TOTAL HEIGHT',16,INK,'700')
q.dimv(960,SY(-6),SY(24),'30 m study volume')
# Upper platforms / crossing, shown behind the main cut.
sr(40,5.65,30,.35,'#CFC0D3',PLUM)
for z in [41,56,69]:sl(z,-6,z,5.65,MUTED,3)
sr(57,6,4,.25,'#CFC0D3',PLUM)
st(58,4,'SUPPORTED GALLERY / PASSAGES BELOW',11,MUTED)
for label,z,y in [('01',28,-6),('02',47,-6),('03',48,6),('04',65,6)]:
 sr(z-.5,y,1,6,'#CAD4DA',INK,1.4)
 q.tag(SX(z),SY(y+7.8),label,OCHRE if y<0 else PLUM)
st(31,-9.2,'LOWER DEPARTURE PLATFORMS  −6 m',14,OCHRE,'700')
st(56,16,'UPPER DEPARTURE PLATFORMS  +6 m',14,PLUM,'700')
sr(-1.265,.94,2.53,.12,'#D7B790',INK)
sl(-1.1,0,-1.1,.94,INK,2);sl(1.1,0,1.1,.94,INK,2)
st(1,8,'PRIVATE\nINSPECTION BOOTH',12,INK,'700')
st(0,-2.6,'CONCOURSE  0 m',11,INK,'700')
# Actual source camera height over the unchanged booth floor.
q.circle(SX(-2.62),SY(2.16),3,PLUM,PLUM)
sl(-2.62,2.16,28,-6,PLUM,1,'5 4')
sl(-2.62,2.16,48,9,PLUM,1,'5 4')
st(17,1.2,'CLEAR GLASS GUARD',10,BLUE)
# Stair / landing are diagrammatic, projected to this section.
pts=[(SX(5),SY(0))]
stair_z=5
for i in range(36):
 if i==18:stair_z+=1.5;pts.append((SX(stair_z),SY(-3)))
 stair_z+=.35
 pts.extend([(SX(stair_z),SY(-i/6)),(SX(stair_z),SY(-(i+1)/6))])
q.path(pts,MUTED,1.2)
st(13,-8,'CONTROLLED STAIR',10,MUTED)
q.text(1050,234,'WHY THIS FITS THE VISION',22,INK,'700')
q.text(1050,274,'The booth is a small administrative threshold\ninside a vast transport system. Beyond it,\npeople are sorted onto numbered platforms\nand disappear into departures for the past.',20,MUTED)
q.text(1050,410,'VISIBILITY CHECKS',19,INK,'700')
q.text(1050,447,'Straight desk and forward viewing axis.\nThe inspection floor stays at the current datum.\nAn early deck edge allows a view down to Platform 01.\nThe guard must be transparent, not an opaque wall.\nUpper portals sit beyond the low booth screen.',17,MUTED)
q.text(1050,598,'NOT YET IMPLEMENTED',18,INK,'700')
q.text(1050,632,'Camera controls, visitor anchors and interaction\nremain gameplay-owned. These are spatial drawings;\nfull perspective and pan checks come in the blockout.',17,MUTED)

# Booth close-up makes the privacy and left window relationship explicit.
q.line(55,757,1745,757,LINE,1.4)
q.text(65,799,'BOOTH DETAIL / PLAN',22,INK,'700')
BX=lambda x:140+(x+26)*23;BZ=lambda z:980-z*23
def bl(x,z,xx,zz,c=INK,sw=3):q.line(BX(x),BZ(z),BX(xx),BZ(zz),c,sw)
q.rect(BX(-25),BZ(3),10*23,9*23,'#EADFD0',LINE)
bl(-15,-6,-15,3,INK,5);bl(-25,-6,-21,-6,INK,5);bl(-19,-6,-15,-6,INK,5)
bl(-25,-6,-25,-3,INK,5);bl(-25,-3,-25,3,BLUE,3)
q.rect(BX(-22.9),BZ(1.265),5.8*23,2.53*23,'#D7B790',INK)
q.text(BX(-20),BZ(0)+4,'STRAIGHT DESK',12,INK,'700','middle')
q.circle(BX(-20),BZ(-2.62),4,PLUM,PLUM)
q.path([(BX(-20),BZ(-2.62)),(BX(-20),BZ(5.5))],PLUM,2,'5 4')
q.path([(BX(-20),BZ(-2.62)),(BX(-26),BZ(.845))],BLUE,2,'5 4')
q.text(470,850,'LEFT',15,BLUE,'700');q.text(470,878,'Clear side bay → city glazing.\nNo neighbouring workstation on this side.',16,MUTED)
q.text(470,966,'RIGHT + REAR',15,INK,'700');q.text(470,994,'Opaque return and enclosed staff rooms.\nHidden work areas stay outside the public view.',16,MUTED)
q.text(1030,803,'THE ARCHITECTURAL CHARACTER',20,INK,'700')
q.text(1030,841,'A grand train terminal translated into time travel:\nplatform numbers, departure boards, galleries,\ncontrolled boarding, luggage storage and long views.\n\nState grandeur above; ordinary lives processed below.\nOne painting and a few neglected artifacts are details\nwithin that system, rather than the subject of the hall.',18,MUTED)
q.footer('Section is schematic: side galleries are projected, not all on one cut line. No furniture or game scene is changed.')
save(q,'C02_terminal_section.svg')

# Narrow analytic sanity checks; no claim of full scene/navigation validation.
cam=(-20,2.16,-2.62)
edge_z=4.5
low_foot_y=cam[1]+(-6-cam[1])*((edge_z-cam[2])/(28-cam[2]))
data={
 'confirmed':['Grand train-terminal structure at tower crown','Teleporters serve numbered platforms','Multiple platforms / floors','Straight private desk; no visible other desks or coworkers','Left window pan','Building entry -> desk check -> platform departure'],
 'proposal':{'width':60,'depth':81,'roof_relative_to_inspection':24,'lower_platform_level':-6,'inspection_level':0,'upper_platform_level':6,'platform_count':4},
 'desk_yaw_degrees':0,
 'study_left_pan_degrees':60,
 'source_camera_height_over_inspection_floor':2.16,
 'portal01_foot_ray_height_at_concourse_edge_metres':round(low_foot_y,3),
 'portal01_foot_ray_clears_floor_edge':low_foot_y>0,
 'opaque_guard_would_hide_lower_portal_foot':low_foot_y<1.1,
 'left_pan_ray_hits_booth_window_at_z':round(cam[2]+5/math.tan(math.radians(60)),3),
 'left_booth_clear_window_z_range':[-3,3],
 'deferred':['Full scene occlusion through the pan, including existing PC/corkboards','Detailed stairs/lift/boarding gate geometry and accessible circulation','Crowd placement after platform blockout','Camera and interaction hooks remain with Claude','Original floor near the desk retained; no current floor mesh is edited'],
}
(HERE/'checks.json').write_text(json.dumps(data,indent=2),encoding='utf-8')
print(json.dumps(data,indent=2))
