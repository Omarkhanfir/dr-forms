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
  const label=landscape?'landscape':'portrait';const context=await browser.newContext({viewport:landscape?{width:844,height:390}:{width:390,height:844},hasTouch:true,isMobile:true,deviceScaleFactor:1,userAgent:'Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 Version/18.0 Mobile/15E148 Safari/604.1'});
  const page=await context.newPage(),cdp=await context.newCDPSession(page),states=[],errors=[];result.sessions.push({label,states,errors});
  page.on('console',m=>{if(m.text().startsWith('PROTOTYPE_STATE:'))states.push(JSON.parse(m.text().slice(16)));else if(m.type()==='error')errors.push(m.text());});page.on('pageerror',e=>errors.push(e.message));page.on('requestfailed',r=>errors.push('Failed '+r.url()));
  await page.goto(`http://127.0.0.1:${port}/`);await page.waitForFunction(()=>!!window.prototypeInstance,null,{timeout:180000});await page.waitForTimeout(5500);
  const state=async()=>{const n=states.length;await page.evaluate(()=>window.prototypeInstance.SendMessage('Prototype Player','LogVerificationState'));for(let i=0;i<200&&states.length===n;i++)await page.waitForTimeout(20);assert(states.length>n);return states.at(-1);};
  const client=async(s,p)=>{const b=await page.locator('#unity-canvas').boundingBox();return{x:b.x+p.x/s.screenWidth*b.width,y:b.y+(1-p.y/s.screenHeight)*b.height};};
  let s=await state();const play=await client(s,s.playCenter);await page.touchscreen.tap(play.x,play.y);await page.waitForTimeout(550);s=await state();
  assert(s.started&&s.artSkinnedParts>0&&s.artAnimation==='Idle'&&s.artShadowResolution===1024);const idle=s.artClipTime;await page.waitForTimeout(250);s=await state();assert(s.artClipTime!==idle);pass('Rigged hero has advancing skeletal idle clip',{label,parts:s.artSkinnedParts});
  await page.screenshot({path:path.join(artifacts,`art-${label}-room.png`)});
  const center=await client(s,s.joystickCenter),thumb=await client(s,{x:s.joystickCenter.x+s.joystickRadius*.6,y:s.joystickCenter.y+s.joystickRadius*.5});
  await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{...center,id:1}]});await cdp.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{...thumb,id:1}]});let capturedRun=false;
  for(let i=0;i<8;i++){await page.waitForTimeout(80);s=await state();if(s.artAnimation==='Running_A'&&!capturedRun){capturedRun=true;await page.screenshot({path:path.join(artifacts,`art-${label}-run.png`)});}}
  assert(capturedRun&&Math.hypot(s.position.x,s.position.z)>1);pass('Actual joystick movement plays skeletal run without moving the controller through animation',{label});
  await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});await page.waitForTimeout(100);s=await state();
  const end=Date.now()+15000;while(Math.hypot(s.enemyPosition.x-s.position.x,s.enemyPosition.z-s.position.z)>5&&Date.now()<end){await page.waitForTimeout(200);s=await state();}
  const before=s.shotsFired;
  for(let i=0;i<2;i++){
   const a=await client(s,s.attackCenter);await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{...a,id:2}]});await page.waitForTimeout(65);s=await state();await page.screenshot({path:path.join(artifacts,`art-${label}-cast-${i}.png`)});await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});await page.waitForTimeout(300);s=await state();
  }
  assert(s.shotsFired>before&&states.some(x=>x.artAnimation==='Spellcast_Shoot'));pass('Real Attack plays skeletal cast and launches the existing bounded projectile',{label,shots:s.shotsFired});
  assert(states.every(x=>x.projectilePool<=8&&x.effects<=24&&x.enemyBolts<=4));pass('New presentation retains effect/projectile caps',{label});
  assert(errors.length===0,errors.join('\n'));pass('Phone-resolution render has no browser or Unity errors',{label});await context.close();
 }
 result.environment={browser:browser.version(),renderer:'Headless Chrome/SwiftShader; phone viewport and touch emulation, not physical Safari'};
}catch(e){result.error=e.stack;console.error(e);process.exitCode=1;}finally{fs.writeFileSync(path.join(artifacts,'courtyard-visual-result.json'),JSON.stringify(result,null,2));if(browser)await browser.close();server.close();}})();
