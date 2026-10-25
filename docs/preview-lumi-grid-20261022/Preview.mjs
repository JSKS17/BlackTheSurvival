// Read-only localhost preview of the existing subject 80px grid pipeline.
import http from 'node:http';
import { createReadStream } from 'node:fs';
import { stat } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
const directory=path.dirname(fileURLToPath(import.meta.url));
const workspace=path.resolve(directory,'../..');
const routes=new Map([
  ['/',path.join(directory,'index.html')],
  ['/grid.js',path.join(directory,'grid.js')],
  ['/normal.png',path.join(workspace,'Assets/Resources/Lumia/Portraits/lumi.png')],
  ['/virtual.png',path.join(workspace,'Assets/Resources/Lumia/PortraitsRare/lumi.png')],
  ['/jackie.png',path.join(workspace,'Assets/Resources/Lumia/PortraitsReference/jackie.png')],
  ['/nia.png',path.join(workspace,'Assets/Resources/Lumia/PortraitsReference/nia.png')],
  ['/font.ttf',path.join(workspace,'Assets/Resources/Lumia/Galmuri11.ttf')],
]);
const types={'.html':'text/html; charset=utf-8','.js':'text/javascript; charset=utf-8','.png':'image/png','.ttf':'font/ttf'};
http.createServer(async(req,res)=>{
  if(!['GET','HEAD'].includes(req.method)){res.writeHead(405);res.end();return;}
  const pathname=new URL(req.url,'http://127.0.0.1:8242').pathname;
  const file=routes.get(pathname);
  if(!file){res.writeHead(404);res.end();return;}
  try{const info=await stat(file);res.writeHead(200,{'Content-Type':types[path.extname(file)],'Content-Length':info.size,'Cache-Control':'no-store','X-Content-Type-Options':'nosniff'});if(req.method==='HEAD')res.end();else createReadStream(file).pipe(res);}
  catch{res.writeHead(404);res.end();}
}).listen(8242,'127.0.0.1',()=>console.log(JSON.stringify({ready:true,url:'http://127.0.0.1:8242/',sourceAssetsModified:false})));
