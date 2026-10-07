/* Rasterize the exact proof sheets; does not alter any source PNG. */
const path=require('node:path');
const search=process.argv[2] ? [path.resolve(process.argv[2])] : [__dirname];
const sharp=require(require.resolve('sharp',{paths:search}));
(async()=>{for(const name of ['comparison','seams']) {
  await sharp(path.join(__dirname,name+'.svg')).png().toFile(path.join(__dirname,name+'.png'));
  console.log('Rendered '+name+'.png');
}})().catch(error=>{console.error(error);process.exit(1)});
