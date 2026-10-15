const http=require('http'),fs=require('fs'),path=require('path'),assert=require('assert');
const {chromium}=require(process.env.DRFORMS_PLAYWRIGHT||'playwright');
const {playerShell}=require('./player-shell.cjs');
const root=process.env.DRFORMS_FLOW_BUILD_PATH,artifacts=process.env.DRFORMS_FLOW_ARTIFACTS,port=8768;
assert(path.isAbsolute(root||'')&&path.isAbsolute(artifacts||''));
const result={checks:[],sessions:[]};const pass=(name,data)=>{result.checks.push({name,data});console.log('PASS: '+name);};
const server=http.createServer((req,res)=>{
 const file=path.resolve(root,'.'+(new URL(req.url,'http://localhost').pathname==='/'?'/index.html':new URL(req.url,'http://localhost').pathname));
 if(!file.startsWith(root+path.sep)||!fs.existsSync(file)){res.writeHead(404);return res.end();}
 res.setHeader('Cache-Control','no-store');res.setHeader('Content-Type',({'.html':'text/html','.js':'application/javascript','.wasm':'application/wasm','.png':'image/png'})[path.extname(file)]||'application/octet-stream');
 if(file.endsWith('index.html'))return res.end(playerShell(fs.readFileSync(file,'utf8')).replace('.then((unityInstance) => {','.then((unityInstance) => {window.prototypeInstance=unityInstance;'));
 fs.createReadStream(file).pipe(res);
});
(async()=>{let browser;try{
 await new Promise(r=>server.listen(port,'127.0.0.1',r));browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-unsafe-swiftshader']});
 for(const landscape of [false,true]){
  const label=landscape?'landscape':'portrait',viewport=landscape?{width:844,height:390}:{width:390,height:844};const context=await browser.newContext({viewport,hasTouch:true,isMobile:true,deviceScaleFactor:1,userAgent:'Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 Version/18.0 Mobile/15E148 Safari/604.1',...(process.env.DRFORMS_RECORD_VIDEO==='1'?{recordVideo:{dir:path.join(artifacts,'video'),size:viewport}}:{})});
  const page=await context.newPage(),cdp=await context.newCDPSession(page),states=[],errors=[];result.sessions.push({label,states,errors});
  page.on('console',m=>{if(m.text().startsWith('PROTOTYPE_STATE:'))states.push(JSON.parse(m.text().slice(16)));else if(m.type()==='error')errors.push(m.text());});page.on('pageerror',e=>errors.push(e.message));page.on('requestfailed',r=>errors.push('Failed '+r.url()));
  await page.goto(`http://127.0.0.1:${port}/`);await page.waitForFunction(()=>!!window.prototypeInstance,null,{timeout:180000});await page.waitForTimeout(5500);
  const state=async()=>{const n=states.length;await page.evaluate(()=>window.prototypeInstance.SendMessage('Prototype Player','LogVerificationState'));for(let i=0;i<200&&states.length===n;i++)await page.waitForTimeout(20);assert(states.length>n);return states.at(-1);};
  const client=async(s,p)=>{const b=await page.locator('#unity-canvas').boundingBox();return{x:b.x+p.x/s.screenWidth*b.width,y:b.y+(1-p.y/s.screenHeight)*b.height};};
  let s=await state();const play=await client(s,s.playCenter);await page.touchscreen.tap(play.x,play.y);await page.waitForTimeout(550);s=await state();
  await page.evaluate(()=>{
   const source=document.querySelector('#unity-canvas'),capture=document.createElement('canvas');capture.width=source.width;capture.height=source.height;
   const paint=capture.getContext('2d');window.labCapturing=true;
   function copy(){if(!window.labCapturing)return;paint.drawImage(source,0,0);requestAnimationFrame(copy);}requestAnimationFrame(copy);
   window.labChunks=[];window.labRecorder=new MediaRecorder(capture.captureStream(30),{mimeType:'video/webm;codecs=vp9',videoBitsPerSecond:2500000});
   window.labRecorder.ondataavailable=e=>{if(e.data.size)window.labChunks.push(e.data)};window.labRecorder.start();
  });
  assert(s.started&&s.artSkinnedParts>0&&s.artAnimation==='Idle'&&s.artShadowResolution===1024);const idle=s.artClipTime;await page.waitForTimeout(250);s=await state();assert(s.artClipTime!==idle);pass('Rigged hero has advancing skeletal idle clip',{label,parts:s.artSkinnedParts});
  await page.screenshot({path:path.join(artifacts,`art-${label}-room.png`)});
  // A smaller real joystick displacement must produce a different authored gait.
  const walkCenter=await client(s,s.joystickCenter),walkThumb=await client(s,{x:s.joystickCenter.x+s.joystickRadius*.34,y:s.joystickCenter.y});
  await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{...walkCenter,id:1}]});
  await cdp.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{...walkThumb,id:1}]});
  await page.waitForTimeout(200);s=await state();assert(s.artAnimation==='Walking_A');
  await page.screenshot({path:path.join(artifacts,`art-${label}-walk.png`)});
  await page.waitForTimeout(450);await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});
  s=await state();assert(['Stopping','Idle'].includes(s.artAnimation));
  pass('Analog walking uses its own gait and released movement enters a stop pose',{label});
  await page.waitForTimeout(350);s=await state();
  // Start away from the nearest initial spawn while both chasers are still outside contact range.
  // Default room-one chasers move at 1.8 units/sec toward the stationary player; no state is injected.
  const near=s.spawns.reduce((best,p)=>Math.hypot(p.x,p.z)<Math.hypot(best.x,best.z)?p:best),initialDistance=Math.hypot(near.x,near.z);
  const end=Date.now()+15000;while(initialDistance-1.8*s.time>4.5&&Date.now()<end){await page.waitForTimeout(100);s=await state();}
  const length=Math.hypot(near.x,near.z),center=await client(s,s.joystickCenter),thumb=await client(s,{x:s.joystickCenter.x-near.x/length*s.joystickRadius*.65,y:s.joystickCenter.y-near.z/length*s.joystickRadius*.65});
  const start=s.position,shots=s.shotsFired,attack=await client(s,s.attackCenter);
  await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{...center,id:1}]});await cdp.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{...thumb,id:1}]});
  let capturedRun=false;for(let i=0;i<4&&!capturedRun;i++){await page.waitForTimeout(60);s=await state();if(s.artAnimation==='Running_A'){capturedRun=true;await page.screenshot({path:path.join(artifacts,`art-${label}-run.png`)});}}
  for(let i=0;i<2;i++){await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{...thumb,id:1},{...attack,id:2}]});await page.waitForTimeout(150);s=await state();await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[{...attack,id:2}]});await page.waitForTimeout(150);}
  await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});s=await state();
  assert(capturedRun&&Math.hypot(s.position.x-start.x,s.position.z-start.z)>1);pass('Actual joystick movement plays skeletal run without moving the controller through animation',{label});
  assert(s.shotsFired>shots);pass('Simultaneous actual touch movement and throwing',{label});
  assert(states.some(x=>x.artAnimation==='Spellcast_Shoot'&&['Walking_A','Running_A'].includes(x.locomotionAnimation)));
  pass('Throw overlay retains the moving leg animation',{label});
  const pause=await client(s,s.pauseCenter),attackForPause=await client(s,s.attackCenter);
  await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{...attackForPause,id:2},{...pause,id:3}]});
  await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[{...attackForPause,id:2}]});
  await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});await page.waitForTimeout(180);s=await state();
  assert(s.manualPaused&&!s.throwPending&&s.attackCount>s.shotsFired);const pausedShots=s.shotsFired;await page.waitForTimeout(160);s=await state();assert(s.shotsFired===pausedShots);
  const resume=await client(s,s.resumeCenter);await page.touchscreen.tap(resume.x,resume.y);await page.waitForTimeout(200);s=await state();assert(!s.manualPaused&&!s.throwPending&&s.shotsFired===pausedShots);pass('Pause cancels pending release and Resume launches no delayed attack',{label});
  const before=s.shotsFired;
  for(let i=0;i<2;i++){
   const a=await client(s,s.attackCenter);await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{...a,id:2}]});await page.waitForTimeout(65);s=await state();await page.screenshot({path:path.join(artifacts,`art-${label}-cast-${i}.png`)});await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});await page.waitForTimeout(300);s=await state();
  }
  assert(s.releaseDelay>=.099&&s.releaseDelay<.20);pass('Flask release follows the authored wind-up timing',{label,delay:s.releaseDelay});
  assert(s.shotsFired>before&&states.some(x=>x.artAnimation==='Spellcast_Shoot'));pass('Real Attack plays skeletal cast and launches the existing bounded projectile',{label,shots:s.shotsFired});
  assert(states.every(x=>x.projectilePool<=8&&x.effects<=24&&x.enemyBolts<=4));pass('New presentation retains effect/projectile caps',{label});
  const recording=await page.evaluate(async()=>{await new Promise(r=>{window.labRecorder.onstop=r;window.labRecorder.stop()});window.labCapturing=false;return await new Promise(r=>{const f=new FileReader();f.onload=()=>r(f.result.split(',')[1]);f.readAsDataURL(new Blob(window.labChunks,{type:'video/webm'}))})});
  fs.writeFileSync(path.join(artifacts,`lab-${label}-gameplay.webm`),Buffer.from(recording,'base64'));
  assert(errors.length===0,errors.join('\n'));pass('Phone-resolution render has no browser or Unity errors',{label});const video=page.video();await context.close();if(video)await video.saveAs(path.join(artifacts,`lab-${label}-compositor.webm`));
 }
 result.environment={browser:browser.version(),renderer:'Headless Chrome/SwiftShader; phone viewport and touch emulation, not physical Safari'};
}catch(e){result.error=e.stack;console.error(e);process.exitCode=1;}finally{fs.writeFileSync(path.join(artifacts,'courtyard-visual-result.json'),JSON.stringify(result,null,2));if(browser)await browser.close();server.close();}})();
