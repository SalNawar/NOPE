"""Revision B: original architectural diagrams, no Unity or raster image edits."""
from pathlib import Path
import math, json

HERE=Path(__file__).resolve().parent
helper=HERE.parent/'draw_hall_study.py'
env={'__file__':str(helper)}
exec(compile(helper.read_text(encoding='utf-8').split('# Source values')[0],str(helper),'exec'),env)
env['OUT']=HERE
D=env['Drawing']; INK=env['INK']; PAPER=env['PAPER']; MUTED=env['MUTED']; LINE=env['LINE']
BLUE=env['BLUE']; PLUM=env['PLUM']; OCHRE=env['OCHRE']; CORAL=env['CORAL']

def save(d,name):
    d.save(name)
    p=HERE/name
    p.write_text(p.read_text(encoding='utf-8').replace('REV A','REV B'),encoding='utf-8')

p=D(); p.header('B01','The crown of a government megatower','Top view  /  monumental hall + a window-side player station  /  architectural proposal')
S=10; X=lambda x:515+x*S; Z=lambda z:1035-z*S
def r(x,z,w,d,fill='none',stroke=INK,sw=1.5,extra=''): p.rect(X(x),Z(z+d),w*S,d*S,fill,stroke,sw,extra)
def l(x,z,xx,zz,c=INK,sw=2,dash=''):p.line(X(x),Z(z),X(xx),Z(zz),c,sw,dash)
def t(x,z,s,size=14,c=INK,weight='400',anchor='middle'):p.text(X(x),Z(z),s,size,c,weight,anchor)
def route(points,c,arrow=None,dash=''):p.path([(X(x),Z(z)) for x,z in points],c,3,dash,arrow)

# Hall dimensions and core are proposals; source foreground remains separate.
r(-30,-5,60,80,'#F0EBE1','none')
r(-30,-10,60,5,'#E3DDD3',LINE)
r(30,10,15,30,'#E8E0D4',LINE)
l(-30,-5,-30,75,INK,5); l(-30,75,30,75,INK,5)
l(30,-5,30,10,INK,5); l(30,40,30,75,INK,5)
wall_start=-30
for cx in [-22,-11,0,11,22]:
    l(wall_start,-5,cx-.9,-5,INK,3);wall_start=cx+.9
l(wall_start,-5,30,-5,INK,3)
# City-facing curtain wall: west and north elevations.
l(-30,-5,-30,75,BLUE,3); l(-30,75,30,75,BLUE,3)
for z in [0,12,24,36,48,60,72]:
    r(-30,z,1.6,1.6,INK,INK)
for x in [-18,-6,6,18]: r(x,73.4,1.6,1.6,INK,INK)
# Grand interior piers carry the high civic volume without turning it into a booth room.
for x in [-19,19]:
    for z in [30,48,66]:r(x-.7,z-.7,1.4,1.4,'#BDB3A5',INK)
t(0,77.1,'FICTIONAL MEGACITY / REAR PANORAMA',14,BLUE,'700')
p.parts.append(f'<text x="{X(-34)}" y="{Z(40)}" transform="rotate(-90 {X(-34)} {Z(40)})" font-family="Arial" font-size="15" font-weight="700" text-anchor="middle" fill="{BLUE}">CONTINUOUS CITY-FACING WEST GLAZING</text>')
t(0,68,'MONUMENTAL PUBLIC VOLUME',16,INK,'700')
t(0,64.5,'Repeated structural bays; long views; sparse furnishings',12,MUTED)

# Portal on the civic axis: size retained in this study, location provisional.
r(-3.325,34.225,6.65,1.55,'#CBD6DB',INK,2)
l(-2.97,35,2.97,35,BLUE,6)
t(0,40,'PORTAL',16,INK,'700')
t(0,37.8,'Existing ~6 m scale',12,MUTED)
r(-4,26,8,8,'#E7DEE7','none')
t(0,28,'CLEAR APPROACH',12,PLUM,'700')

# Five stations on the front service line; the player occupies the window end.
desks=[(-22,5),(-11,7),(0,8),(11,7),(22,5)]
for i,(x,z) in enumerate(desks):
    yaw=math.atan2(-x,35-z)
    # Width perpendicular to sightline; full source footprint as a placeholder.
    pts=[]
    for a,b in [(-2.9,-1.265),(2.9,-1.265),(2.9,1.265),(-2.9,1.265),(-2.9,-1.265)]:
        pts.append((X(x+a*math.cos(yaw)+b*math.sin(yaw)),Z(z-a*math.sin(yaw)+b*math.cos(yaw))))
    p.path(pts,INK,1.8,fill='#D7B790')
    t(x,z+.0,'PLAYER' if i==0 else f'{i+1:02}',10,INK,'700')
    p.circle(X(x-1.8*math.sin(yaw)),Z(z-1.8*math.cos(yaw)),3,PAPER,INK)
t(5,-2.5,'FIVE STATIONS / SAME FRONT SERVICE LINE',12,INK,'700')
t(0,-8,'STAFF GALLERY  →  SECURED CONNECTION TO THE CORE',12,MUTED)

# Window-side sightlines, not runtime Cinemachine poses.
eye=(-23.06,3.55)
portal=(0,35)
main_yaw=math.atan2(portal[0]-eye[0],portal[1]-eye[1])
fov=math.atan(math.tan(math.radians(55/2))*16/9)
for angle in [main_yaw-fov,main_yaw+fov]:
    end=(eye[0]+36*math.sin(angle),eye[1]+36*math.cos(angle))
    l(*eye,*end,PLUM,1,'5 5')
route([eye,portal],PLUM,None,'5 5')
window_target=(-30,20)
window_yaw=math.atan2(window_target[0]-eye[0],window_target[1]-eye[1])
route([eye,window_target],BLUE)
p.circle(X(eye[0]),Z(eye[1]),4,PLUM,PLUM)
for angle in [window_yaw-fov,window_yaw+fov]:
    end=(eye[0]+22*math.sin(angle),eye[1]+22*math.cos(angle))
    l(*eye,*end,BLUE,1,'4 5')
p.circle(X(eye[0]),Z(eye[1]),4,PLUM,PLUM)
t(-31.5,17,'LEFT LOOK',12,BLUE,'700')
t(-25,-13.2,'Player sits at the window end; other stations extend to the right.',13,INK,'700','start')

# Station lockers remain on the left, away from the local pan window.
r(-26,22,11.2,.8,'#DDC7B9',INK)
for x in [-26+i*.56 for i in range(21)]:l(x,22,x,22.8,INK,.55)
r(-26,20.2,11.2,1.8,'none',MUTED,.8,'stroke-dasharray="4 4"')
t(-20.4,25,'LUGGAGE LOCKERS',12,INK,'700')
for x,z in [(-12,16),(-12,19),(5,16),(5,19)]:
    r(x,z,7,.8,'#D3C1D0',INK)
    for xx in [x+i for i in range(1,7)]:l(xx,z,xx,z+.8,INK,.6)
t(0,22.5,'COMMON WAITING / CALL AREA',12,MUTED)

# Right-hand tower core: public lifts, enclosed stairs and an onward corridor.
for x in [32,37]:
    r(x,32,3.5,4,'#D7CDBD',INK);t(x+1.75,33.8,'LIFT',9)
r(40.5,12,3.5,7,'none',INK)
for z in [12+i*.65 for i in range(11)]:l(40.5,z,44,z,INK,.7)
t(42.2,10.5,'STAIR',10)
route([(37,29),(30,29),(25,24),(20,12),(0,12)],OCHRE,'ochre')
route([(0,12),(-18,12),(-21,8)],OCHRE,'ochre')
route([(-19,8),(-15,10),(-3,12),(-3,24),(0,32)],PLUM,'plum')
t(37.5,42.5,'TOWER CORE',16,INK,'700')
t(37.5,39.5,'Public arrival / secure staff routes',11,MUTED)
t(37.4,27,'DEPARTMENT\nCORRIDOR',13,INK,'700')
t(37.4,22,'MEDBAY\nJAIL\nC-SUITES',12,MUTED)
# Secure staff route joins the rear counter gallery, without crossing the public floor.
r(30,-10,5,20,'#E3DDD3',LINE)
t(34,-6,'STAFF',10,MUTED)

# One painting and two deliberately mounted artifacts, clear of the city's left view.
for x,z,label in [(27,54,'P'),(24,46,'A'),(-24,46,'B')]:
    r(x-1,z-1,2,2,'#D7CDBD',INK);p.tag(X(x),Z(z),label)
t(0,53,'ONE PAINTING + TWO ARTIFACT DISPLAY BAYS',12,MUTED)

p.dimh(X(-30),X(30),Z(82),'60 m proposed hall width')
p.dimv(140,Z(-5),Z(75),'80 m proposed hall depth')
p.text(1080,227,'CONFIRMED DIRECTION',21,INK,'700')
p.text(1080,262,'A massive, grand government hall at the top\nof a megatower overlooking a fictional city.\nThe player can turn left to look outside.',19,MUTED)
notes=[
('01  MONUMENTAL STRUCTURE / HUMAN SCALE','Study a 60 × 80 m hall, about 24 m high.\nKeep furniture, luggage and people believable.\nGrandeur comes from the structural span.'),
('02  PUT THE PLAYER NEAR THE GLASS','The player is at the window end of the line.\nFive stations are a proposal, not a fixed count.\nNo neighbouring desk stands between player and city.'),
('03  TWO DELIBERATE VIEWS','Forward: traveller, portal and the depth of the hall.\nAbout 60° left: oblique window view and city below.\nCamera cones here are illustrative, not control settings.'),
('04  MOVE THE BUILDING CORE TO THE RIGHT','Public lifts arrive from lower floors.\nA recessed corridor continues toward departments.\nThe left facade remains available for the panorama.'),
('05  GRANDEUR WITH INSTITUTIONAL NEGLECT','Maintain the monumental structure and fine materials.\nWear appears at handles, seats, labels and display cases.\nUse one painting and a few world artifacts, properly held.'),
]
y=377
for title,body in notes:
    p.text(1080,y,title,17,INK,'700');p.text(1080,y+29,body,17,MUTED);y+=139
p.text(1080,1096,'Unchanged in Unity: floor, PC, desk, portal, camera and gameplay.',15,INK,'700')
p.footer('Revision B replaces the low-floor / 28 × 30 m proposal. Dimensions, station count and poses remain blockout candidates.')
save(p,'B01_tower_crown_plan.svg')

q=D();q.header('B02','Grand hall, human-scale workstation, city below','Section + local window view  /  tower height deliberately left open  /  no camera implementation')
# Longitudinal section at 10 px/m. Monumental height compared with unchanged people.
SX=lambda z:130+10*(z+5); SY=lambda y:538-10*y
def sr(z,y,w,h,fill='none',stroke=INK,sw=1.5):q.rect(SX(z),SY(y+h),10*w,10*h,fill,stroke,sw)
def sl(z,y,zz,yy,c=INK,sw=1.5,dash=''):q.line(SX(z),SY(y),SX(zz),SY(yy),c,sw,dash)
def st(z,y,text,size=14,c=INK,weight='400'):q.text(SX(z),SY(y),text,size,c,weight,'middle')
sr(-5,-1,80,1,'#BDB4A5',INK)
sl(-5,0,-5,24,INK,4);sl(75,0,75,24,INK,4)
sl(-5,24,75,24,MUTED,1.5,'6 5')
sl(75,1.2,75,22,BLUE,4)
st(35,27,'60 × 80 m FOOTPRINT  /  24 m HIGH VOLUME, PROPOSED',16,INK,'700')
q.dimv(990,SY(0),SY(24),'24 m proposed clear height')
sr(5,1,2.53,.12,'#D7B790',INK)
sl(5.2,0,5.2,1,INK,2);sl(7.3,0,7.3,1,INK,2)
for z in [3.5,9.0,31]:
    q.circle(SX(z),SY(1.62),1.3,INK,INK)
    sl(z,1.48,z,.65,INK,1.6);sl(z,.65,z-.2,0,INK,1.1);sl(z,.65,z+.2,0,INK,1.1)
sr(34.5,0,1,6,'#CAD4DA',INK)
st(35,9,'PORTAL\n6 m source scale',13,INK,'700')
st(9,-4.3,'HUMANS ≈ 1.75 m\nFURNITURE NOT ENLARGED',12,MUTED)
st(54,-4.3,'SOURCE PORTAL SIZE RETAINED IN STUDY\nFraming needs a perspective check',12,MUTED)
st(34,18,'The room dwarfs its occupants.',22,INK,'700')
st(34,14,'Tall piers, long bays, deep reveals and uninterrupted glazing.',15,MUTED)
q.text(1100,238,'THE UPPER CROWN OF A GOVERNMENT TOWER',20,INK,'700')
q.text(1100,274,'The hall occupies the upper civic crown.\nPublic lifts travel up from admissions far below.\nThe tower’s exact height and floor number remain open.\nC-SUITES branches into the controlled upper core.',18,MUTED)
q.text(1100,407,'THE CITY VIEW NEEDS VERTICAL DEPTH',18,INK,'700')
q.text(1100,440,'Show roofs and occupied levels below, distant towers,\nand several layers of city massing. It should feel like\nlooking out from the institution above the city.',18,MUTED)

# Local transverse cut: the same window-end station, west glass about 7 m from study eye.
q.line(55,660,1745,660,LINE,1.5)
q.text(65,703,'LEFT VIEW  /  LOCAL TRANSVERSE SECTION',23,INK,'700')
q.text(65,733,'Diagrammatic: the tower / city is context, not a final skyline design.',15,MUTED)
LX=lambda x:120+(x+38)*30; LY=lambda y:994-y*30
# Outside city silhouettes below the upper hall level.
for x,y,w,h in [(-40,-4,2,2.8),(-37.2,-3,2,1.5),(-34.5,-4.6,2,3)]:
    q.rect(LX(x),LY(y+h),w*30,h*30,'#D7DDE0',LINE,1)
q.rect(LX(-30),LY(0),17*30,18,'#BDB4A5',INK)
q.line(LX(-30),LY(0),LX(-30),LY(7.5),INK,6)
q.line(LX(-30),LY(1.1),LX(-30),LY(7.5),BLUE,3)
q.line(LX(-29.8),LY(1.1),LX(-29.8),LY(7.5),BLUE,1)
q.text(LX(-30),LY(8),'GLASS CONTINUES UP TO THE HIGH HALL',12,BLUE,'700','middle')
# Human and gaze in section.
q.circle(LX(-23.06),LY(1.62),4,PAPER,INK)
q.line(LX(-23.06),LY(1.48),LX(-23.06),LY(.65),INK,2)
q.line(LX(-23.06),LY(.65),LX(-23.3),LY(0),INK,2)
q.line(LX(-23.06),LY(.65),LX(-22.85),LY(0),INK,2)
q.path([(LX(-23.06),LY(1.62)),(LX(-30),LY(1.4)),(LX(-35),LY(-.2))],BLUE,2)
q.dimh(LX(-30),LX(-23.06),1041,'~7 m shortest distance to glazing')
q.text(LX(-20.5),LY(3),'PLAYER / WINDOW-END STATION',13,INK,'700','middle')
q.text(410,1094,'Pan ray projected into this section; actual view is oblique.',13,MUTED)
q.text(65,824,'CITY FAR BELOW\nBuilding height not fixed',14,MUTED)
q.text(1010,727,'WHAT THE PAN CHANGES',21,INK,'700')
q.text(1010,766,'Use real depth for window reveals, piers, desk and nearby props.\nA flat front-facing hall painting would break when viewed sideways.\nDistant city layers, mounted pictures and distant crowds can still\nuse sprites where their angle and depth remain believable.',18,MUTED)
q.text(1010,907,'NEXT SANITY CHECK',19,INK,'700')
q.text(1010,943,'Build one plain spatial model and inspect both camera views.\nTest the corkboards and partitions across the entire pan.\nThe portal gets smaller at this greater viewing distance.\nKeep the actual pan controls and interaction rules with Claude.',18,MUTED)
q.footer('Perspective framing, interaction preservation and the pan range are unresolved until the shared-camera blockout.')
save(q,'B02_tower_crown_section.svg')

distance=math.dist(eye,portal)
data={
 'scope':'Architectural proposal only; no Unity changes',
 'user_confirmed':['Massive grand hall','At top of mega government tower','Fictional city overlooking view','Player can pan left to window','Building entry -> desk check -> portal departure'],
 'proposed_hall_metres':[60,80,24],
 'proposed_counter_count':5,
 'source_floor_area_square_metres':28*30,
 'proposed_floor_area_square_metres':60*80,
 'floor_area_ratio':round(60*80/(28*30),2),
 'study_eye_plan_xz':eye,
 'window_wall_x':-30,
 'study_eye_to_window_metres':round(eye[0]+30,2),
 'study_eye_to_portal_plan_distance_metres':round(distance,2),
 'source_portal_height_metres':6,
 'approximate_portal_vertical_angular_size_degrees':round(math.degrees(2*math.atan(3/distance)),2),
 'study_main_heading_degrees':round(math.degrees(main_yaw),2),
 'study_left_window_target_xz':window_target,
 'study_left_window_heading_degrees':round(math.degrees(window_yaw),2),
 'study_eye_to_window_target_metres':round(math.dist(eye,window_target),2),
 'heading_change_degrees':round(math.degrees(main_yaw-window_yaw),2),
 'important_limitations':['The initial straight-left window look required about 126 degrees of turn. The revised oblique look requires about 59 degrees; this is not a committed pan range.','Tower absolute height / floor number unspecified.','Original floor appearance and near-field module must be preserved; expanded floor must not be made by stretching the source.','Desk footprint preserved as source-size placeholders in B01, not automatically reduced as in Rev A.','This is an adjacency / massing study, not fully checked furniture navigation.','Runtime camera / traveller anchors / PC focus require Claude-owned integration.']
}
(HERE/'study_checks.json').write_text(json.dumps(data,indent=2),encoding='utf-8')
print(json.dumps(data,indent=2))
