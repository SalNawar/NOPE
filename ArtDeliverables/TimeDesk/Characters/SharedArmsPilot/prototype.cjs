/* Asset-preserving feasibility renderer. No raster pixels are edited. */
const fs = require('node:fs');
const path = require('node:path');
const OUT = __dirname;
const ROOT = path.resolve(OUT, '../../../..');
const ART = path.join(ROOT, 'Assets/Art/Characters/Resources/Characters');
const poses = {
  neutral: [[0,0],[0,0]],
  explaining_a: [[-20,-75],[0,0]],
  thinking_a: [[0,0],[22,145]],
  thinking_b: [[-22,-145],[0,0]],
  objecting_a: [[-25,-90],[25,90]],
};
const joints = {
  m: {left:[369,505,354,690],right:[655,505,672,690]},
  f: {left:[393,505,369,691],right:[631,505,655,691]},
};
const assets=new Map();
function uri(key) {
  const file = path.join(ART,key+'.png');
  if (!fs.existsSync(file)) throw new Error('Missing reference '+key);
  if(!assets.has(key)) assets.set(key,'data:image/png;base64,'+fs.readFileSync(file).toString('base64'));
  return '#asset_'+key;
}
function panel(g,pose,skin=1,accessory=true,hair=true) {
  if (!poses[pose]) pose='neutral';
  const id=`${g}_${pose}_${skin}_${accessory}_${hair}`;
  const body=uri(`body_${g}_skin${skin}`);
  const image=(src,attrs='')=>`<use href="${src}" ${attrs}/>`;
  const defs=[];
  const clip=(name,d)=>{defs.push(`<clipPath id="${id}_${name}"><path d="${d}"/></clipPath>`);return `clip-path="url(#${id}_${name})"`;};
  const l=g==='m'?400:414, r=1024-l;
  const torso=clip('torso',`M 440 360 H 584 L ${r+20} 460 L ${r} 550 L ${r-25} 760 L 675 1010 V 1536 H 349 V 1010 L ${l+25} 760 L ${l} 550 L ${l-20} 460 Z`);
  const arms=Object.entries(joints[g]).map(([side,j],index)=>{
    const [sx,sy,ex,ey]=j, [upper,lower]=poses[pose][index];
    const edge=side==='left'?`M 120 450 H ${l-20} L ${l} 550 L ${l+25} 760 L 380 1000 H 120 Z`:`M 904 450 H ${r+20} L ${r} 550 L ${r-25} 760 L 644 1000 H 904 Z`;
    const all=clip(side,edge);
    const top=clip(side+'upper',`M 0 0 H 1024 V ${ey+12} H 0 Z`);
    const bottom=clip(side+'lower',`M 0 ${ey-12} H 1024 V 1050 H 0 Z`);
    const hand=clip(side+'hand','M 0 865 H 1024 V 1050 H 0 Z');
    const t=`rotate(${upper} ${sx} ${sy})`;
    const b=`rotate(${lower} ${ex} ${ey})`;
    return {back:`<g transform="${t}"><g ${all}><g ${top}>${image(body)}</g><g transform="${b}" ${bottom}>${image(body)}</g></g></g>`,front:`<g transform="${t}"><g transform="${b}"><g ${all}><g ${hand}>${image(body)}</g></g></g></g>`};
  });
  const stack = [
    g==='f'&&hair?image(uri('hairback_f_egypt_ancient')):'',
    ...arms.map(a=>a.back),image(body,torso),
    image(uri(`outfit_${g}_egypt_ancient`)),
    image(uri(`head_${g}_skin${skin}_facea`)),
    hair?image(uri(`hair_${g}_egypt_ancient`)):'',
    accessory?image(uri(`accessory_${g}_egypt_ancient`)):'',
    pose.startsWith('thinking')?arms[pose==='thinking_a'?1:0].front:'',
  ].join('');
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1024 1536" role="img" aria-label="${g} ${pose} skin ${skin}"><defs>${defs.join('')}</defs>${stack}</svg>`;
}
function build() {
  const cards=['m','f'].flatMap(g=>Object.keys(poses).map(p=>`<article><h2>${g==='m'?'Male':'Female'} · ${p}</h2>${panel(g,p)}<small>Same torso + outfit · reused body arm regions</small></article>`)).join('');
  const controls='<label>Silhouette <select id="gender"><option value="m">Male</option><option value="f">Female</option></select></label><label>Pose <select id="pose">'+Object.keys(poses).map(p=>`<option>${p}</option>`).join('')+'</select></label><label>Complexion <select id="skin"><option>1</option><option>3</option><option>5</option></select></label><label><input id="hair" type="checkbox" checked> Hair / wig</label><label><input id="accessory" type="checkbox" checked> Accessory</label>';
  const variants={};
  for(const g of ['m','f'])for(const p of Object.keys(poses))for(const s of [1,3,5])for(const a of [false,true])for(const h of [false,true]) variants[`${g}/${p}/${s}/${a}/${h}`]=panel(g,p,s,a,h);
  fs.writeFileSync(path.join(OUT,'comparison.html'),`<!doctype html><meta charset="utf-8"><title>Egypt shared-arm feasibility pilot</title><style>body{margin:0;background:#202328;color:#f5eee0;font:16px system-ui}header{padding:24px 32px}h1{margin:0 0 8px;font-size:28px}p{max-width:1050px;line-height:1.5}main{display:grid;grid-template-columns:repeat(5,1fr);gap:8px;padding:0 24px 24px}article{background:#39414a;border:1px solid #657079;padding:8px;text-align:center}h2{font-size:15px;margin:0}svg{display:block;width:100%;max-height:480px}small{font-size:11px}section{padding:24px 32px}#preview{width:360px;background:#39414a}label{display:inline-block;margin:8px 16px 16px 0}select{margin-left:8px}details{margin:16px 0}</style><header><h1>Egypt / Thebes, c. 1470 BCE — shared-arm feasibility</h1><p>Actual existing layered art, reused through runtime clipping and joint transforms. All five frames reuse each silhouette's torso and linen outfit. Instant swaps; no animation. This is a browser feasibility renderer, not Unity integration or production-ready authored pose art.</p><p><b>QA pending:</b> neutral hand shapes are reused; raised hands and shoulder/elbow joints need visual acceptance. Existing source alpha fringes are retained. Both thinking frames deliberately draw hand pixels above face/hair/accessory.</p></header><main>${cards}</main><section><h2>Instant-swap preview</h2>${controls}<div id="preview"></div><p>Complexion selects the project's baked skin files. Egyptian wigs retain their fixed colour. Facial expression swaps, glasses, moustaches and hats are not claimed as implemented here.</p></section><script>const variants=${JSON.stringify(variants)};function update(){let k=[gender.value,pose.value,skin.value,accessory.checked,hair.checked].join('/');preview.innerHTML=variants[k];}document.querySelectorAll('select,input').forEach(x=>x.addEventListener('change',update));update();</script>`);
  const htmlPath=path.join(OUT,'comparison.html');
  const assetDefs='<svg width="0" height="0" style="position:absolute"><defs>'+Array.from(assets).map(([k,v])=>`<image id="asset_${k}" width="1024" height="1536" href="${v}"/>`).join('')+'</defs></svg>';
  fs.writeFileSync(htmlPath,fs.readFileSync(htmlPath,'utf8').replace('<main>',assetDefs+'<main>'));
  const defs='<defs>'+Array.from(assets).map(([k,v])=>`<image id="asset_${k}" width="1024" height="1536" href="${v}"/>`).join('')+'</defs>';
  const entries=['m','f'].flatMap(g=>Object.keys(poses).map(p=>[g,p]));
  const cells=entries.map(([g,p],i)=>`<g transform="translate(${i%5*360},${Math.floor(i/5)*580+100})"><rect width="352" height="570" fill="#39414a"/><text x="16" y="24" fill="#f5eee0" font-family="sans-serif" font-size="16">${g} / ${p}</text>${panel(g,p).replace('viewBox="0 0 1024 1536"','x="0" y="30" width="352" height="528" viewBox="0 0 1024 1536"')}</g>`).join('');
  fs.writeFileSync(path.join(OUT,'comparison.svg'),`<svg xmlns="http://www.w3.org/2000/svg" width="1800" height="1270" viewBox="0 0 1800 1270"><rect width="1800" height="1270" fill="#202328"/>${defs}<text x="24" y="35" fill="#f5eee0" font-family="sans-serif" font-size="26">Thebes c.1470 BCE — actual reused body regions / fixed torso + clothing</text><text x="24" y="70" fill="#f5eee0" font-family="sans-serif" font-size="18">Feasibility only · visual QA pending · neutral hands reused · existing fringes retained · no Unity integration</text>${cells}</svg>`);
  const crops=entries.map(([g,p],i)=>`<g transform="translate(${i%5*360},${Math.floor(i/5)*450+90})"><rect width="352" height="440" fill="#39414a"/><text x="12" y="24" fill="#f5eee0" font-family="sans-serif" font-size="16">${g} / ${p} shoulder + hand region</text>${panel(g,p).replace('viewBox="0 0 1024 1536"','x="0" y="40" width="352" height="390" viewBox="240 250 544 600"')}</g>`).join('');
  fs.writeFileSync(path.join(OUT,'seams.svg'),`<svg xmlns="http://www.w3.org/2000/svg" width="1800" height="1000"><rect width="1800" height="1000" fill="#202328"/>${defs}<text x="24" y="40" fill="#f5eee0" font-family="sans-serif" font-size="26">Shoulder / elbow / face occlusion crops — QA pending, not accepted production art</text>${crops}</svg>`);
  return Object.keys(variants).length;
}
if(require.main===module) console.log('Built '+build()+' actual layer combinations');
module.exports={poses,joints,panel,build};
