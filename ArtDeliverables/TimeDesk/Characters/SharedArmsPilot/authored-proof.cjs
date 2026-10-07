/* Actual authored-arm assembly. All PNG references preserved byte-for-byte. */
const fs=require('node:fs'),path=require('node:path');
const root=path.resolve(__dirname,'../../../..');
const art=path.join(root,'Assets/Art/Characters/Resources/Characters');
const source=path.join(__dirname,'sources/arm_m_skin1__thinking_a.png');
const files=new Map();
function ref(key){if(!files.has(key))files.set(key,fs.readFileSync(key==='authored_arm'?source:path.join(art,key+'.png')).toString('base64'));return '#raw_'+key;}
function img(key,extra=''){return `<use href="${ref(key)}" ${extra}/>`;}
function authored(){return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1024 1536"><defs><clipPath id="keep_static"><path d="M0 0 H600 V450 L597 450 L632 480 L624 550 L618 600 L617 650 L621 680 L624 730 L632 760 L644 800 L658 840 L658 1000 H1024 V1536 H0 Z"/></clipPath><clipPath id="arm_front"><path d="M420 415 H537 L617 655 L730 775 L692 860 H631 L500 625 Z"/></clipPath><clipPath id="hand_front"><rect x="420" y="415" width="125" height="140"/></clipPath></defs>${img('authored_arm')}${img('body_m_skin1','clip-path="url(#keep_static)"')}${img('outfit_m_egypt_ancient')}${img('authored_arm','clip-path="url(#arm_front)"')}${img('head_m_skin1_facea')}${img('hair_m_egypt_ancient')}${img('accessory_m_egypt_ancient')}${img('authored_arm','clip-path="url(#hand_front)"')}</svg>`;}
function neutral(){return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1024 1536">${['body_m_skin1','outfit_m_egypt_ancient','head_m_skin1_facea','hair_m_egypt_ancient','accessory_m_egypt_ancient'].map(k=>img(k)).join('')}</svg>`;}
const views=[neutral(),authored()];
const defs='<defs>'+Array.from(files).map(([k,v])=>`<image id="raw_${k}" width="1024" height="1536" href="data:image/png;base64,${v}"/>`).join('')+'</defs>';
const titles=['Existing neutral layers','Authored thinking arm / direct registration'];
const panels=views.map((v,i)=>`<g transform="translate(${i*512} 110)"><rect width="502" height="1330" fill="#39414a"/><text x="14" y="24" fill="#fff" font-size="18">${titles[i]}</text>${v.replace('viewBox="0 0 1024 1536"','x="0" y="40" width="502" height="753" viewBox="0 0 1024 1536"')}${v.replace('id="','id="crop_').replace('viewBox="0 0 1024 1536"','x="0" y="815" width="502" height="500" viewBox="300 270 440 440"')}</g>`).join('');
fs.writeFileSync(path.join(__dirname,'authored-comparison.svg'),`<svg xmlns="http://www.w3.org/2000/svg" width="1024" height="1460"><rect width="1024" height="1460" fill="#202328"/>${defs}<g font-family="sans-serif"><text x="20" y="36" fill="#fff" font-size="24">One authored thinking-arm test / Egyptian pilot</text><text x="20" y="75" fill="#fff" font-size="16">Actual reused layers / generated arm directly placed / registration FAIL</text>${panels}</g></svg>`);
console.log('Wrote authored-comparison.svg');

