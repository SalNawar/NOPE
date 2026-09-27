"""Registered visible-pixel extraction. Run from repository root.

The source is never resampled. Masks are authored against its 2048-wide review
coordinates and rasterized at source resolution. Every source pixel has exactly
one owner; the reconstruction assertion is mandatory before writing a manifest.
Hidden surfaces and true glass transmission are not inferred by this script.
"""
from pathlib import Path
import hashlib
import json
import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[4]
SOURCE = ROOT / "ArtDeliverables/TimeDesk/HallLayers/Master/TwoPanel/hall-anime-platform-v8.png"
OUT = ROOT / "Assets/Art/Office/AnimeHallLayers"
REPORT = Path(__file__).resolve().parent
rgb = np.asarray(Image.open(SOURCE).convert("RGB"))
H, W = rgb.shape[:2]
assert (W, H) == (2172, 724), "Mask registration requires the v8 master."
S = W / 2048
labels = np.zeros((H, W), np.uint16)
entries = []

def shape(points=None, rect=None, ellipse=None, lines=None, width=1):
    im = Image.new("L", (W, H))
    d = ImageDraw.Draw(im)
    coords = lambda pts: [(round(x*S), round(y*S)) for x,y in pts]
    if points: d.polygon(coords(points), fill=255)
    if rect: d.rectangle(tuple(round(v*S) for v in rect), fill=255)
    if ellipse: d.ellipse(tuple(round(v*S) for v in ellipse), fill=255)
    if lines:
        for line in lines: d.line(coords(line), fill=255, width=max(1,round(width*S)), joint="curve")
    return np.asarray(im) > 0

def layer(name, mask, normal=(0,0,-1)):
    index = len(entries)
    entries.append(dict(id=f"{index:02d} {name}", file=f"{index:02d}-{name.lower().replace(' ','-')}.png",
                        order=index, normal=list(normal)))
    labels[mask] = index
    return index

layer("Structural masonry", np.ones((H,W),bool))
ceiling = shape(points=[(510,0),(2048,0),(2048,147),(1390,126),(1126,194),(813,194),(654,127)])
layer("Ceiling and overhead services",ceiling,(0,-1,-.35))
left_window = shape(points=[(0,0),(522,0),(522,391),(0,542)])
far_windows = (shape(points=[(656,124),(1270,128),(1270,252),(656,253)]) |
               shape(points=[(681,287),(1260,287),(1260,368),(682,379)]))
r,g,b = [rgb[:,:,i].astype(np.int16) for i in range(3)]
blue = (b-r > 14) & (g-r > 5) & (r > 85) & (b > 140)
layer("Window frames",left_window | far_windows)
exterior = (left_window | far_windows) & blue
layer("Exterior placeholder single layer", exterior)

layer("Upper right service walls",shape(points=[(1390,91),(2048,82),(2048,255),(1390,249)]),(-1,0,-.15))
layer("Lower right department walls",shape(points=[(1264,284),(2048,316),(2048,682),(1731,642),(1538,571),(1264,436)]),(-1,0,-.3))
floor = shape(points=[(684,476),(684,345),(787,317),(1124,317),(1263,356),(1264,436),
                      (1546,574),(1623,607),(1885,646),(2048,683),(0,683),(0,655),(398,556),(528,579)])
layer("Concourse terracotta floor",floor,(0,1,-.08))
layer("Upper gallery floor",shape(points=[(1290,247),(2048,247),(2048,283),(1290,275)]),(0,1,0))
layer("Bridge fascia",shape(points=[(503,249),(1284,249),(1284,290),(660,287),(660,276),(503,276)]))
layer("Left elevator pier",shape(points=[(519,0),(653,0),(653,393),(611,398),(611,295),(521,282)]))
layer("Right load bearing pier",shape(points=[(1272,0),(1390,0),(1390,320),(1280,296),(1280,444),(1257,430),(1257,292),(1272,292)]))
layer("Left stair structure",shape(points=[(388,572),(471,412),(612,381),(648,391),(538,579),(521,581)]),(0,.65,-.65))
layer("Foreground platform",shape(points=[(0,655),(1469,637),(1560,682),(2048,683),(0,683)]),(0,1,0))

# Flags, signage, doors and fitted cabinets are independent color/control surfaces.
layer("Flag left cloth",shape(points=[(681,0),(738,0),(738,186),(683,186)]))
layer("Flag right cloth",shape(points=[(1225,0),(1277,0),(1277,179),(1225,185)]))
layer("Departure board frame",shape(rect=(794,60,1085,172)) |
      shape(lines=[[(845,0),(842,63)],[(1042,0),(1042,64)]],width=9))
layer("Departure board blank display",shape(rect=(804,70,1076,162)))
for name,points in [
    ("Detention door",[(1312,376),(1369,387),(1369,486),(1312,454)]),
    ("Medbay door",[(1397,414),(1468,428),(1468,531),(1397,492)]),
    ("Executive suites door",[(1504,457),(1586,474),(1586,571),(1504,530)]),
    ("Detention pictogram",[(1308,331),(1374,344),(1374,389),(1308,376)]),
    ("Medbay pictogram",[(1394,353),(1471,369),(1471,429),(1394,414)]),
    ("Executive suites pictogram",[(1501,379),(1588,395),(1588,474),(1501,457)]),
    ("Lower elevator doors",[(546,286),(609,287),(610,385),(546,402)]),
    ("Upper side elevator door",[(633,148),(657,161),(657,250),(633,249)]),
    ("Left parcel lockers",[(0,543),(409,429),(447,418),(404,488),(0,647)]),
]:
    layer(name,shape(points=points))

# Individually authored silhouettes, not rectangular image cards.
props = [
 ("Rear baggage scanner",[(681,347),(703,338),(703,309),(714,300),(757,296),(772,303),(773,343),(760,356),(727,361),(709,376),(687,379)]),
 ("Rear waiting seat",[(897,323),(904,313),(912,309),(971,309),(985,314),(995,324),(987,332),(962,339),(916,334),(898,330)]),
 ("Rear water dispenser",[(997,299),(1005,298),(1010,304),(1010,332),(997,332)]),
 ("Rear mixed parcel cabinets",[(1145,292),(1206,290),(1206,347),(1145,331)]),
 ("Rear maintenance rover",[(1203,339),(1207,323),(1217,323),(1214,313),(1221,294),(1231,292),(1237,300),(1223,314),(1228,325),(1240,329),(1254,342),(1251,356),(1239,359),(1226,356),(1214,359),(1202,351)]),
 ("Right vending machine",[(1618,443),(1679,451),(1708,464),(1706,591),(1663,596),(1618,572)]),
 ("Right waiting seats",[(1669,549),(1693,514),(1734,513),(1741,516),(1760,515),(1803,539),(1831,558),(1838,594),(1818,598),(1816,633),(1799,635),(1779,625),(1741,607),(1712,587),(1682,585)]),
 ("Right waste bin",[(1812,562),(1841,553),(1853,566),(1856,636),(1829,639),(1811,621)]),
 ("Left waste bin",[(558,523),(562,517),(582,515),(596,523),(596,571),(582,578),(559,575)]),
 ("Framed historical artwork",[(1758,390),(1820,400),(1821,491),(1757,478)]),
 ("Armillary display",[(1861,437),(1866,429),(1874,435),(1878,440),(1896,455),(1895,482),(1878,489),(1878,502),(1850,493),(1852,484),(1844,476),(1842,459),(1850,443)]),
 ("Stone bust display",[(1950,436),(1965,431),(1979,435),(1988,447),(1987,463),(1981,475),(1998,492),(2002,522),(1940,510),(1944,485),(1959,475),(1950,460)]),
]
for name,points in props: layer(name,shape(points=points))

# Portal bays are separated first, then their metal ring and glass/gate faces.
floor_color = (r-g > 23) & (r-b > 28) & (g > 45)
portals = [
 ("01 front",[(754,493),(800,448),(1119,448),(1167,487),(1166,574),(1131,612),(803,613),(754,581)],(884,382,1052,552),(913,412,1023,512)),
 ("02 rear left",[(755,353),(787,341),(904,344),(928,356),(927,397),(907,405),(775,404),(754,393)],(814,309,891,383),(830,323,875,367)),
 ("03 rear right",[(1004,348),(1140,342),(1171,354),(1170,397),(1148,407),(1006,399)],(1035,310,1113,382),(1050,325,1098,368)),
 ("04 upper left",[(1475,191),(1654,190),(1686,210),(1685,251),(1474,251)],(1553,144,1653,248),(1571,164,1633,226)),
 ("05 upper right",[(1695,190),(1931,190),(1962,207),(1962,253),(1696,253)],(1770,127,1906,258),(1791,151,1883,230)),
]
for name,points,outer,inner in portals:
    bay = shape(points=points)
    # Warm floor seen through unglazed openings stays with the floor.
    layer("Portal "+name+" secure bay",bay & ~floor_color)
    ring = shape(ellipse=outer) & ~shape(ellipse=inner)
    layer("Portal "+name+" metal ring",ring & ~floor_color)

# Rails have explicit narrow stroke masks rather than a rectangular crop of floor.
rail_lines = [[(0,524),(1536,524)],[(0,607),(1491,607)]]
for x in [31,155,275,394,505,612,721,830,965,1087,1217,1347,1489,1537]:
    rail_lines.append([(x,521),(x,650 if x<1500 else 662)])
front_rail = shape(lines=rail_lines,width=11)
for x in [31,155,275,394,505,612,721,830,965,1087,1217,1347,1489,1537]:
    front_rail |= shape(ellipse=(x-9,642 if x<1500 else 653,x+10,659 if x<1500 else 670))
front_rail |= shape(lines=[[(1346,644),(1486,505),(1538,523)],[(1352,658),(1490,527)],
                          [(1437,557),(1437,638)],[(1399,599),(1399,638)],
                          [(1888,509),(2048,550)],[(1888,589),(2048,632)],
                          [(1888,508),(1888,653)],[(1994,537),(1994,682)]],width=11)
layer("Foreground tall platform railing",front_rail)
bridge_lines = [[(500,231),(1289,240),(2048,230)],[(500,249),(1289,258),(2048,256)]]
for x in range(511,1280,53): bridge_lines.append([(x,231),(x,254)])
for x in [1293,1372,1455,1538,1622,1705,1793,1884,1973,2040]: bridge_lines.append([(x,232),(x,265)])
layer("Bridge and upper gallery railing",shape(lines=bridge_lines,width=6))
left_lines=[[(0,522),(511,350)],[(0,541),(459,401)],[(398,549),(477,380),(511,351)],
            [(527,555),(610,391),(642,380)],[(414,558),(493,401)],[(541,559),(623,399)]]
for x,y in [(32,512),(117,483),(202,455),(283,429),(357,404),(425,379),(480,362)]:
    left_lines.append([(x,y),(x,y+29)])
for x,y in [(417,513),(441,462),(465,411),(551,509),(577,457),(601,410)]:
    left_lines.append([(x,y),(x,y+28)])
layer("Left observation and stair railing",shape(lines=left_lines,width=6))

# Independent fixture faces can later receive emission without baking it into walls.
lamps = shape(rect=(841,0,905,12)) | shape(rect=(960,0,1060,18))
for rect in [(738,75,777,85),(824,121,853,130),(898,151,922,158),
             (1777,104,1800,154),(1662,49,1687,95),(1855,89,1880,130)]:
    lamps |= shape(rect=rect)
layer("Fixture diffusers",lamps)
fire = shape(rect=(511,337,534,380)) | shape(rect=(1370,174,1393,214)) | shape(rect=(1252,374,1278,415))
layer("Fire safety cabinets",fire)

# Separate the visible painted glass from each secured bay. This does not invent
# transmission or hidden scenery; the glass still contains the source's painted view.
for name,_,_,_ in portals:
    owner = next(i for i,e in enumerate(entries) if e["id"].endswith("Portal "+name+" secure bay"))
    glass = (labels == owner) & (b-r > 18) & (g-r > 10) & (r > 90)
    layer("Portal "+name+" painted glass",glass)

OUT.joinpath("Textures").mkdir(parents=True,exist_ok=True)
reconstructed = np.zeros_like(rgb)
coverage = np.zeros((H,W),np.uint8)
manifest_entries = []
for index,entry in enumerate(entries):
    mask = labels == index
    if not mask.any(): raise RuntimeError("Empty layer: "+entry["id"])
    rgba = np.zeros((H,W,4),np.uint8)
    rgba[:,:,:3][mask] = rgb[mask]
    rgba[:,:,3][mask] = 255
    Image.fromarray(rgba).save(OUT/"Textures"/entry["file"],optimize=True)
    reconstructed[mask] = rgb[mask]
    coverage += mask
    yy,xx = np.nonzero(mask)
    entry["visiblePixels"] = int(mask.sum())
    entry["visibleBounds"] = [int(xx.min()),int(yy.min()),int(xx.max()+1),int(yy.max()+1)]
    entry["suggestedSurfaceNormal"] = entry["normal"]
    # A common lighting plane avoids pretending these cutouts are reconstructed 3D
    # surfaces. Individual material normals remain editable for a later normal pass.
    entry["normal"] = [0,0,-1]
    manifest_entries.append(entry)
assert np.all(coverage == 1), "Missing or multiply-owned source pixels"
assert np.array_equal(reconstructed,rgb), "Source colors/registration changed"
manifest = dict(width=W,height=H,source=SOURCE.name,
                sourceSha256=hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
                method="Mutually exclusive visible-pixel masks; no image resampling",
                limitations="Fixed-view visible surfaces; security glass includes painted transmission; no hidden-surface completion in extracted PNGs",
                layers=manifest_entries)
OUT.joinpath("layers.json").write_text(json.dumps(manifest,indent=2)+"\n",encoding="utf-8")
Image.fromarray(reconstructed).save(REPORT/"reconstructed.png")
validation = dict(layerCount=len(entries),width=W,height=H,missingPixels=int((coverage==0).sum()),
                  overlapPixels=int((coverage>1).sum()),changedSourcePixels=int(np.any(reconstructed!=rgb,axis=2).sum()))
REPORT.joinpath("extraction-validation.json").write_text(json.dumps(validation,indent=2)+"\n")

# Inspection sheet with source-preserving pixels on a neutral checker.
thumb_w,thumb_h = 434,145
sheet = Image.new("RGB",(thumb_w*4,175*((len(entries)+3)//4)),(32,34,39))
draw = ImageDraw.Draw(sheet)
for index,entry in enumerate(entries):
    tile = Image.open(OUT/"Textures"/entry["file"]).convert("RGBA")
    tile.thumbnail((thumb_w,thumb_h))
    x,y = (index%4)*thumb_w,(index//4)*175
    sheet.paste(tile,(x,y+23),tile)
    draw.text((x+4,y+4),entry["id"],fill=(235,235,235))
sheet.save(REPORT/"layer-contact-sheet.png")
print(json.dumps(validation))
