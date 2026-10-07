const assert=require('node:assert/strict');
const {poses,joints,panel}=require('./prototype.cjs');
assert.deepEqual(Object.keys(poses),['neutral','explaining_a','thinking_a','thinking_b','objecting_a']);
for(const gender of ['m','f']) {
  for(const pose of Object.keys(poses)) {
    const svg=panel(gender,pose);
    assert(svg.includes('viewBox="0 0 1024 1536"'));
    assert(svg.includes(`asset_outfit_${gender}_egypt_ancient`));
    assert(svg.includes(`asset_body_${gender}_skin1`));
    assert(svg.includes(`asset_head_${gender}_skin1_facea`));
    assert(!svg.includes('opacity='));
    if(pose.startsWith('thinking')) assert(svg.lastIndexOf('hand)')>svg.lastIndexOf(`asset_accessory_${gender}_egypt_ancient`),'hand occlusion must render last');
  }
  assert.equal(panel(gender,'missing'),panel(gender,'neutral'),'unknown pose must fall back as one moving set');
  assert(!panel(gender,'neutral',3,false,false).includes('asset_accessory_'));
  assert(!panel(gender,'neutral',3,false,false).includes('asset_hair_'));
  assert(panel(gender,'thinking_a',5).includes(`asset_body_${gender}_skin5`));
  assert.equal(joints[gender].left.length,4);
}
console.log('PASS: 10 pose stacks, canvas, shared references, whole-set fallback, complexion keys, optional layers and thinking hand order. Visual quality is not verified by these tests.');
