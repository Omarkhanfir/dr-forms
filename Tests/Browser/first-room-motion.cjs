// Actual inputs and read-only snapshots; no gameplay/health/spawn manipulation.
const fs=require('fs'),path=require('path'),http=require('http'),assert=require('assert');
const {chromium}=require(process.env.DRFORMS_PLAYWRIGHT||'playwright');
const {playerShell}=require('./player-shell.cjs');
const root=process.env.DRFORMS_FLOW_BUILD_PATH,out=process.env.DRFORMS_FLOW_ARTIFACTS,port=8772;
assert(path.isAbsolute(root||'')&&path.isAbsolute(out||''));
const result={checks:[],sessions:[]};const pass=(name,data)=>{result.checks.push({name,data});console.log('PASS: '+name);};
const server=http.createServer((req,res)=>{
 const name=new URL(req.url,'http://localhost').pathname,file=path.resolve(root,'.'+(name==='/'?'/index.html':name));
 if(!file.startsWith(root+path.sep)||!fs.existsSync(file)){res.writeHead(404);return res.end();}
 res.setHeader('Cache-Control','no-store');res.setHeader('Content-Type',({'.html':'text/html','.js':'application/javascript','.wasm':'application/wasm','.png':'image/png'})[path.extname(file)]||'application/octet-stream');
 if(name==='/')return res.end(playerShell(fs.readFileSync(file,'utf8')).replace('.then((unityInstance) => {','.then((unityInstance) => {window.prototypeInstance=unityInstance;'));
 fs.createReadStream(file).pipe(res);
});
(async()=>{let browser;try{
 await new Promise(r=>server.listen(port,'127.0.0.1',r));browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-unsafe-swiftshader']});
 for(const touch of [false,true]){
  const label=touch?'phone':'desktop',viewport=touch?{width:390,height:844}:{width:1440,height:900};
  const context=await browser.newContext({viewport,hasTouch:touch,isMobile:touch,deviceScaleFactor:1,recordVideo:{dir:path.join(out,'raw-video'),size:viewport},...(touch?{userAgent:'Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 Version/18.0 Mobile/15E148 Safari/604.1'}:{})});
  const page=await context.newPage(),cdp=await context.newCDPSession(page),states=[],errors=[],session={label,states,errors,events:[]};result.sessions.push(session);
  page.on('console',m=>{if(m.text().startsWith('PROTOTYPE_STATE:'))states.push(JSON.parse(m.text().slice(16)));else if(m.type()==='error')errors.push(m.text());});page.on('pageerror',e=>errors.push(e.message));
  await page.goto(`http://127.0.0.1:${port}/`);await page.waitForFunction(()=>!!window.prototypeInstance,null,{timeout:180000});await page.waitForTimeout(5500);
  const snapshot=async()=>{const n=states.length;await page.evaluate(()=>window.prototypeInstance.SendMessage('Prototype Player','LogVerificationState'));for(let i=0;i<300&&states.length===n;i++)await page.waitForTimeout(15);assert(states.length>n);return states.at(-1);};
  const client=async(s,p)=>{const b=await page.locator('#unity-canvas').boundingBox();return{x:b.x+p.x/s.screenWidth*b.width,y:b.y+(1-p.y/s.screenHeight)*b.height};};
  const click=async(s,p)=>{const c=await client(s,p);if(touch)await page.touchscreen.tap(c.x,c.y);else await page.mouse.click(c.x,c.y,{delay:50});};
  const floor=(s,p)=>{const f=s.forward,n=Math.hypot(f.x,f.z),r={x:f.z/n,y:0,z:-f.x/n},u={x:f.y*r.z,y:f.z*r.x-f.x*r.z,z:-f.y*r.x};const v={x:p.x-s.camera.x,y:-s.camera.y,z:p.z-s.camera.z},dot=a=>a.x*v.x+a.y*v.y+a.z*v.z,k=s.screenHeight/2/Math.tan(s.fieldOfView*Math.PI/360),d=dot(f);return{x:s.screenWidth/2+dot(r)*k/d,y:s.screenHeight/2+dot(u)*k/d};};
  let s=await snapshot();await click(s,s.playCenter);await page.waitForTimeout(650);s=await snapshot();assert(s.canPlay&&s.wave===1);
  session.initialCamera={camera:s.camera,forward:s.forward,fieldOfView:s.fieldOfView,position:s.position,viewport};
  await page.screenshot({path:path.join(out,`matching-${label}.png`)});
  let thumb=null;
  async function stop(){if(touch&&thumb){await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});thumb=null;}else if(!touch){s=await snapshot();const p=await client(s,floor(s,s.position));await page.mouse.move(p.x,p.y);}await page.waitForTimeout(350);s=await snapshot();}
  async function attack(){
   if(touch){const a=await client(s,s.attackCenter);await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[...(thumb?[{...thumb,id:1}]:[]),{...a,id:2}]});await page.waitForTimeout(65);await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:thumb?[{...a,id:2}]:[]});}
   else{await page.mouse.down();await page.waitForTimeout(40);await page.mouse.up();}
  }
  async function move(direction,strength,milliseconds,throwing=false){
   const length=Math.hypot(direction.x,direction.z);direction={x:direction.x/length,z:direction.z/length};let nextShot=0;const end=Date.now()+milliseconds;
   if(touch&&!thumb){const c=await client(s,s.joystickCenter);await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{...c,id:1}]});}
   while(Date.now()<end&&s.canPlay&&!s.defeated){
    if(touch){thumb=await client(s,{x:s.joystickCenter.x+direction.x*s.joystickRadius*strength,y:s.joystickCenter.y+direction.z*s.joystickRadius*strength});await cdp.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{...thumb,id:1}]});}
    else{const distance=strength<.5?.38:2.3,p=await client(s,floor(s,{x:s.position.x+direction.x*distance,z:s.position.z+direction.z*distance}));await page.mouse.move(p.x,p.y);}
    if(throwing&&Date.now()>nextShot){await attack();nextShot=Date.now()+350;}
    await page.waitForTimeout(85);s=await snapshot();
   }
  }
  // Establish space without changing enemies. Run away from the nearer initial spawn,
  // then sustain a slower gait. The camera and collision remain the production ones.
  const near=s.spawns.reduce((a,b)=>Math.hypot(a.x,a.z)<Math.hypot(b.x,b.z)?a:b),away={x:-near.x,z:-near.z};
  await move(away,.8,650);await move(away,.34,2100);await stop();
  assert(states.some(x=>x.artAnimation==='Walking_A')&&states.some(x=>x.artAnimation==='Running_A'));
  pass('Normal camera records articulated walk, run and stopping through real controls',{label});
  const walk=states.filter(x=>x.locomotionAnimation==='Walking_A'&&x.joints?.authoredUnitScale>0);
  if(walk.length>=3){
   const range=(key)=>{const values=walk.map(x=>x.joints[key].x);let max=0;for(const a of values)for(const b of values)max=Math.max(max,Math.min(Math.abs(a-b),360-Math.abs(a-b)));return max;};
   const ranges=Object.fromEntries(['leftHip','rightHip','leftKnee','rightKnee','leftFoot','rightFoot','leftArm','rightArm'].map(k=>[k,range(k)]));
   assert(ranges.leftHip>8&&ranges.rightHip>8&&ranges.leftKnee>15&&ranges.rightKnee>15&&ranges.leftFoot>8&&ranges.rightFoot>8,JSON.stringify(ranges));
   pass('Actual hip/knee/foot transforms change during continuous walking',{label,ranges});
  }
  // Move tangentially back toward the center so there is room to throw and turn.
  const home={x:-s.position.x,z:-s.position.z};const shots=s.shotsFired;
  await move(home,.65,1350,true);await stop();
  // Allow the easy chaser to enter range naturally if the initial spawns were far away.
  const wait=Date.now()+6000;while(Date.now()<wait&&s.shotsFired===shots&&s.canPlay){await attack();await page.waitForTimeout(310);s=await snapshot();}
  assert(s.shotsFired>shots);
  assert(states.some(x=>x.artAnimation==='Spellcast_Shoot'&&['Walking_A','Running_A'].includes(x.locomotionAnimation)),'No sampled moving throw');
  pass('Moving throws and hand release recorded at the unchanged camera',{label});
  const contact=s.playerDamageApplications,end=Date.now()+6500;
  while(Date.now()<end&&s.canPlay&&s.playerDamageApplications===contact){await page.waitForTimeout(90);s=await snapshot();}
  assert(s.playerDamageApplications>contact,'Contact attack was not observed');
  pass('Live monster approaches and anticipates its unchanged contact attack',{label,damageSeen:true});
  const limit=Date.now()+16000;
  while(Date.now()<limit&&s.canPlay&&!s.questionPaused&&!s.defeated){await attack();await page.waitForTimeout(310);s=await snapshot();}
  assert(s.questionPaused&&!s.defeated);pass('Actual flask hits and monster defeat clear the existing first encounter',{label});
  assert(errors.length===0,errors.join('\n'));const video=page.video();await context.close();await video.saveAs(path.join(out,`motion-${label}.webm`));
 }
 result.environment='Windows Chrome/SwiftShader, actual input and phone touch emulation; silent compositor recordings';
}catch(e){result.error=e.stack;console.error(e);process.exitCode=1;}finally{fs.writeFileSync(path.join(out,'first-room-motion-result.json'),JSON.stringify(result,null,2));if(browser)await browser.close();server.close();}})();
