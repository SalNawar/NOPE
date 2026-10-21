// Exports the r17 live city (E:/unity/NOPE-tools/city/live/r17.html) for the Unity port (CITY-R17).
//
// It loads the page in headless Chrome with its frame loop held (requestAnimationFrame never fires), so the page
// boots, installs every r17 part and prefills the traffic exactly as it does in a browser, then reads the data the
// page actually draws from (after the r17 load-time lane edits, the K skyway, the soft clouds, the r17 ads and
// train, the r16 fire and smoke) and writes three JSON files plus a golden traffic record for the C# tests:
//   city_r17_scene.json     camera, planes, clouds, train, ads (tiles, sequences, holds), fire, smoke, noon lights and
//                           the draw order the page runs (every drawer as [system, plane, order])
//   city_r17_traffic.json   the lanes as the page runs them, the slot rules, types, liveries, sim constants
//   city_r17_vehicles.json  the four toon meshes (taxi, bus, truck, coupe), base64 typed arrays as data/r17veh.js
//   city_r17_golden.json    every vehicle after the prefill and after 48 frames at 1/24 s (the C# sim must match),
//                           the clouds' placement at a few frames and the train's pass on every frame of its cycle
//
// usage: NODE_PATH=<dir with puppeteer-core> node export_r17.js --live E:/unity/NOPE-tools/city/live
//        --out <Assets/Data/City/R17> --golden <Assets/Tests/EditMode/City/R17/Data>
const path = require('path'), fs = require('fs');
const args = process.argv.slice(2), opt = {};
for (let i = 0; i < args.length; i++) if (args[i].startsWith('--')) opt[args[i].slice(2)] = args[i + 1] && !args[i + 1].startsWith('--') ? args[++i] : true;
const LIVE = path.resolve(opt.live || 'E:/unity/NOPE-tools/city/live');
const OUT = path.resolve(opt.out || 'Assets/Data/City/R17');
const GOLD = path.resolve(opt.golden || 'Assets/Tests/EditMode/City/R17/Data');
const CHROME = process.env.CHROME || 'C:/Program Files/Google/Chrome/Application/chrome.exe';
const puppeteer = require('puppeteer-core');

// The r17 traffic's load-time rules (js/r17traffic.js: LANE_SLOT, LANE_CAP, SLOT_FIX, MODEL_OF, HEAVY, LIV, LIV_OF,
// ACURVE, T0, the sim seed, the sun, the haze and the ink). They are closure constants in the page, so they are
// restated here as data; check_rules() below fails the export if the page's file no longer carries them.
const RULES = {
  laneSlot: { 'H1-E1': 0, 'H1-E2': 0, 'H1-W1': 0, 'H1-W2': 0 },
  laneCap: { 'D-T1': 5, 'D-T2': 5, 'D-A1': 5, 'D-A2': 5 },
  slotFix: { 'G-RISE': [[72, 87.5, 8]] },
  modelOf: { taxi: 'taxi', compact: 'coupe', coupe: 'coupe', bike: 'coupe', police: 'coupe', limo: 'coupe',
             ambulance: 'truck', van: 'coupe', panel: 'truck', bus: 'bus', dbus: 'bus',
             hauler: 'truck', tanker: 'truck', flatbed: 'truck', lifter: 'truck' },
  heavy: ['bus', 'dbus', 'hauler', 'tanker', 'flatbed', 'lifter', 'panel'],
  liveries: {
    car: [['#3E7C82', '#DCCB9A'], ['#A3523A', '#2F3F4E'], ['#D8CDAE', '#5B6E7C'], ['#4D6A92', '#D7D2C2'], ['#7C8A5A', '#3A4636'],
          ['#8C3F3F', '#D9C9A3'], ['#C08A3E', '#3B4A55'], ['#9DB7BF', '#46606C'], ['#5C6F8A', '#C9B78E'], ['#B9A27A', '#5A4636']],
    police: [['#26364F', '#D8D2BE']], limo: [['#1F2A36', '#8A7A5A'], ['#E0DCCF', '#5C5148']],
    ambulance: [['#DCD3BC', '#A84236']],
    van: [['#7A8C8A', '#D2C7A8'], ['#B5683A', '#DCCB9A'], ['#4A6A96', '#DCD3BC'], ['#5E7F62', '#CFC3A0'], ['#C9BFA4', '#8A4B36']],
    bus: [['#3E7C82', '#DCCB9A'], ['#B5683A', '#E2D3AC'], ['#4A6A96', '#DCD3BC'], ['#7E3B3B', '#DCCB9A']],
    truck: [['#A14E36', '#6E7C78'], ['#3F6E73', '#C7B998'], ['#C99A45', '#4B5E73'], ['#5D6E7D', '#8A5A3C'], ['#7E3B3B', '#9AA4A6']],
    tanker: [['#3F6E73', '#C8C2B0'], ['#A14E36', '#B9BDB4']], flatbed: [['#8A5A3C', '#4C5A5C']], lifter: [['#D0A040', '#3D4A52'], ['#C99A45', '#5D6E7D']]
  },
  liveryOf: { compact: 'car', coupe: 'car', bike: 'car', police: 'police', limo: 'limo', ambulance: 'ambulance', van: 'van',
              panel: 'van', bus: 'bus', dbus: 'bus', hauler: 'truck', tanker: 'tanker', flatbed: 'flatbed', lifter: 'lifter' },
  sim: { seed: 20261021, t0: 640, acurve: 12.0, substep: 1 / 120, settle30: 720, settle120: 240, minZ: 15, cullMargin: 120 },
  look: { sun: [-0.45, 0.85, -0.25], haze: [0.52, 0.66, 0.78], ink: [0x12 / 255, 0x1A / 255, 0x22 / 255], glint: 1 }
};
function check_rules() {
  const src = fs.readFileSync(path.join(LIVE, 'js', 'r17traffic.js'), 'utf8');
  const need = ["var LANE_SLOT = { 'H1-E1': 0, 'H1-E2': 0, 'H1-W1': 0, 'H1-W2': 0 };", "var LANE_CAP = { 'D-T1': 5, 'D-T2': 5, 'D-A1': 5, 'D-A2': 5 };",
    "var SLOT_FIX = { 'G-RISE': [[72, 87.5, 8]] };", 'var ACURVE = 12.0;', 'T0 = 640', 'App.mulberry32(20261021)',
    "var HAZE = [0.52, 0.66, 0.78], INK = [0x12 / 255, 0x1A / 255, 0x22 / 255];", 'var x = -0.45, y = 0.85, z = -0.25'];
  const miss = need.filter(s => src.indexOf(s) < 0);
  if (miss.length) throw new Error('js/r17traffic.js changed; update RULES in export_r17.js: ' + miss.join(' | '));
}

(async () => {
  check_rules();
  fs.mkdirSync(OUT, { recursive: true }); fs.mkdirSync(GOLD, { recursive: true });
  const browser = await puppeteer.launch({ executablePath: CHROME, headless: 'new', protocolTimeout: 600000,
    args: ['--allow-file-access-from-files', '--disable-web-security', '--ignore-gpu-blocklist', '--force-device-scale-factor=1'] });
  const page = await browser.newPage();
  page.on('pageerror', e => console.log('[pageerror]', e.message));
  // hold the page's own frame loop: the state is then exactly the prefill, and every later step is ours
  await page.evaluateOnNewDocument(() => { window.requestAnimationFrame = function () { return 0; }; });
  await page.setViewport({ width: 1920, height: 1080, deviceScaleFactor: 1 });
  await page.goto('file:///' + path.join(LIVE, 'r17.html').replace(/\\/g, '/'), { waitUntil: 'load' });
  await page.waitForFunction('window.App && App.ready && App.r17AllInstalled && App.r17 && App.r17.lanes', { timeout: 180000 });

  const scene = await page.evaluate(() => {
    const S = window.SCENE, RT = window.ROUTES14, CL = window.R17CLOUDS, SOFT = window.R17CLOUDS_SOFT, TR = window.R17TRAIN,
      FX = window.R16FX, AD = window.R17ADS, LG = window.SCENE_LIGHTS || {};
    const base = s => s ? s.split('/').pop() : null;
    return {
      version: 'r17',
      plate: S.plate,
      camera: { cx: S.camera.cx, cy: S.camera.cy, F: S.camera.F, Z1: S.camera.Z1 },
      focus: [1500, 760],
      look: { parallaxPx: App.knobs.parallaxPx, parallaxY: App.knobs.parallaxY, ease: App.knobs.ease, fps: 24 },
      hazeColor: [122 / 255, 178 / 255, 196 / 255],
      depth: { PZ: RT.PZ, PF: RT.PF },
      planes: App.layers.map(L => ({ id: L.def.id, name: L.def.name, parallax: L.def.parallax, s: L.def.s, haze: L.def.haze || 0,
        frame: L.def.rect || [0, 0, S.plate.w, S.plate.h], file: base(L.def.file) })),
      clouds: { scRef: CL.sc_ref, pad: SOFT.pad, slot: 0, list: CL.clouds.map(c => {
        const sf = SOFT.clouds[c.id];
        return { id: c.id, band: c.band, file: base(sf.img), w: sf.w, h: sf.h, n: c.n, cols: c.cols, top: c.top, A: c.A, L: c.L, k: c.k,
          x0: c.x0, par: c.par, haze: c.haze, step: c.step, mph: c.mph };
      }) },
      train: {
        cars: TR.cars.map(c => ({ name: c.name, file: base(c.img), glow: base(c.glow), ox: c.ox, w: c.w, h: c.h })),
        gangways: TR.gangways, gangRows: TR.gang_rows, length: TR.L, skirtRow: TR.skirt_row, pad: TR.pad, deckLine: TR.deck_line,
        skirtAbove: TR.skirt_above, leftMatte: TR.left_matte, leftMattePlane: TR.left_matte_plane, clipX: TR.clip_x, clipPlane: TR.clip_plane,
        margin: TR.margin, speed: TR.speed, cycle: TR.cycle, passes: TR.passes, bob: TR.bob, glowAlpha: TR.glow_alpha,
        gangColor: TR.tones.GANG.map(v => v / 255), plane: 7,
        parapet: { file: base(FX.train.parapet), at: FX.train.parapet_at, bookP09: FX.train.book_p09 },
        paint: 'viaduct_paint.png'
      },
      ads: { fps: AD.fps, n: AD.n, plane: 5, sheets: AD.sheets.map(base), tiles: AD.tiles,
        screens: AD.screens.map(s => ({ id: s.id, rect: s.rect, seq: s.seq })) },
      fire: { n: FX.fire.n, plane: 9, rect: FX.fire.rect, sheets: FX.fire.sheets.map(base), seq: FX.fire.seq,
        spill: { base: base(FX.fire.spill.base), baseRect: FX.fire.spill.base_rect, spill: base(FX.fire.spill.spill), spillRect: FX.fire.spill.spill_rect, gain: FX.fire.spill.gain },
        paint: 'furnace_paint.png' },
      smoke: { n: FX.smoke.n, plane: 9, stacks: ['L', 'R'].map(k => { const d = FX.smoke.stacks[k];
        return { id: k, rect: d.rect, sheets: d.sheets.map(base), seq: d.seq, mouthRect: d.mrect, mouthSheets: d.msheets.map(base), mouthSeq: d.mseq }; }) },
      lights: {
        hallSeam: LG.hallSeam || [], hallBand: LG.hallBand || [], ringLamps: LG.ringLamps || [], exTop: LG.exTop || [],
        warn: (LG.warn || []).filter(a => a[1] === Math.min.apply(null, LG.warn.map(b => b[1])))
      },
      // the draw order the page actually runs: every installed drawer as [system, plane index, order], read from
      // App.drawers (the slot after plane i is tier 'tr' + i; the r11 tier names resolve through App.tierSlot)
      order: App.drawers.map(d => {
        const name = d.name, sys = /^r17-traffic-tr\d+$/.test(name) ? 'traffic' : /^r16-lights-P\d+$/.test(name) ? 'lights'
          : ({ 'r17-clouds': 'clouds', 'r17-train': 'train', 'r17-ads': 'ads', 'r16-smoke': 'smoke', 'r16-furnace-fire': 'fire',
               'r16-smoke-mouth': 'smokeMouth', 'hive': 'laundry' })[name] || name;
        const m = /^tr(\d+)$/.exec(d.tier), plane = m ? +m[1] : (App.tierSlot[d.tier] != null ? App.tierSlot[d.tier] : -1);
        return [sys, plane, d.order];
      }).sort((a, b) => a[1] - b[1] || a[2] - b[2])
    };
  });

  // the ad schedule's holds (which ad each screen shows from which frame, and how it changes), for the duplicate check
  const sch = JSON.parse(fs.readFileSync(path.join(LIVE, 'loops', 'ads17', 'work', 'schedule17.json'), 'utf8'));
  scene.ads.holds = sch.holds; scene.ads.transitionFrames = { roll: 8, wipe: 7, tv: 6 };

  const traffic = await page.evaluate((RULES) => {
    const RT = window.ROUTES14;
    const lanes = App.r17.lanes.map(l => ({ id: l.id, corridor: l.corridor, dir: l.dir, speed: l.d.speed, headway: l.d.headway,
      phase: l.d.phase, k0: l.d.k0 || 0, tab: l.d.tab, parent: l.d.parent || null, branch: l.d.branch || null,
      pts: Array.from(l.d.pts) }));
    return Object.assign({ version: 'r17', cam: RT.cam, PZ: RT.PZ, PF: RT.PF, types: RT.types, dims: RT.veh, lanes: lanes }, RULES);
  }, RULES);

  const vehicles = await page.evaluate(() => window.R17VEH);

  // golden: every vehicle (ghosts once) after the prefill, then after 48 page frames of 1/24 s
  const snap = () => page.evaluate(() => {
    const seen = {}, out = [];
    App.r17.lanes.forEach(l => l.vs.forEach(q => { if (seen[q.id]) return; seen[q.id] = 1;
      out.push([q.id, q.lane.id, q.type, +q.s.toFixed(6), +q.v.toFixed(6), q.lc ? q.lc.from.id : null]); }));
    out.sort((a, b) => a[0] - b[0]);
    const vis = App.r17.vehicles().map(q => [q.id, +q.x.toFixed(4), +q.y.toFixed(4), +q.Z.toFixed(4), q.slot, +q.roll.toFixed(6), +q.pitch.toFixed(6)]);
    vis.sort((a, b) => a[0] - b[0]);
    return { simT: App.r17.simT(), count: App.r17.all(), all: out, visible: vis };
  });
  const golden = { note: 'r17.html, frame loop held: [0] after the prefill, [1] after 48 frames of 1/24 s (5 substeps each)', frames: 48, dt: 1 / 24, states: [] };
  golden.states.push(await snap());
  // the loops' own placement, from the page's functions: clouds at a few frames at the review scale (sc_ref), and the
  // train's active pass on every frame of its 48 s cycle at the page's device scale
  golden.clouds = await page.evaluate(() => {
    const a = window.R17CLOUDS.sc_ref, out = [];
    [0, 1, 2, 3, 47, 100, 999, 2304, 5000, 12345, 86400].forEach(f => App.r17Clouds(f, a).forEach(c => out.push([f, c.id, c.x, c.top, c.j, c.wgt])));
    return { sc: a, rows: out };
  });
  golden.train = await page.evaluate(() => {
    const out = [];
    for (let f = 0; f < window.R17TRAIN.cycle; f++) { const p = App.r17TrainPass(f); out.push(p ? [p.dir, p.k, p.i] : null); }
    return { sc: App.view.sc, passes: out };
  });
  await page.evaluate(() => { window.__advance(48, 1 / 24); });
  golden.states.push(await snap());
  await browser.close();

  const w = (dir, name, obj) => { const p = path.join(dir, name); fs.writeFileSync(p, JSON.stringify(obj) + '\n'); console.log('wrote', p, fs.statSync(p).size, 'bytes'); };
  w(OUT, 'city_r17_scene.json', scene);
  w(OUT, 'city_r17_traffic.json', traffic);
  w(OUT, 'city_r17_vehicles.json', vehicles);
  w(GOLD, 'city_r17_golden.json', golden);
  console.log('lanes', traffic.lanes.length, 'vehicles after prefill', golden.states[0].count, 'visible', golden.states[0].visible.length);
})().catch(e => { console.error(e); process.exit(1); });
