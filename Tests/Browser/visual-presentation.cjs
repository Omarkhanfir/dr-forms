// Real touch boundary/rotation captures; snapshots observe state without modifying gameplay.
const http=require('http'),fs=require('fs'),path=require('path'),assert=require('assert');
const {chromium}=require(process.env.DRFORMS_PLAYWRIGHT||'playwright');
const {playerShell}=require('./player-shell.cjs');
const root=path.resolve(process.env.DRFORMS_FLOW_BUILD_PATH),artifacts=process.env.DRFORMS_FLOW_ARTIFACTS;
const result={checks:[],captures:[],errors:[]},port=8768;
const server=http.createServer((req,res)=>{
 const name=new URL(req.url,'http://localhost').pathname,file=path.resolve(root,'.'+(name==='/'?'/index.html':name));
 if(!file.startsWith(root+path.sep)||!fs.existsSync(file)){res.writeHead(404);return res.end();}
 const mime={'.html':'text/html','.js':'application/javascript','.wasm':'application/wasm','.png':'image/png','.css':'text/css'};
 res.setHeader('Content-Type',mime[path.extname(file)]||'application/octet-stream');
 if(name==='/')return res.end(playerShell(fs.readFileSync(file,'utf8')).replace('.then((unityInstance) => {','.then((unityInstance) => {window.prototypeInstance=unityInstance;'));
 fs.createReadStream(file).pipe(res);
});
(async()=>{let browser;try{
 await new Promise(r=>server.listen(port,'127.0.0.1',r));
 browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-unsafe-swiftshader']});
 const context=await browser.newContext({viewport:{width:390,height:844},isMobile:true,hasTouch:true,deviceScaleFactor:1});
 const page=await context.newPage(),states=[];
 page.on('console',m=>{if(m.text().startsWith('PROTOTYPE_STATE:'))states.push(JSON.parse(m.text().slice(16)));else if(m.type()==='error')result.errors.push(m.text());});
 page.on('pageerror',e=>result.errors.push(e.message));page.on('requestfailed',r=>result.errors.push(r.url()));
 const cdp=await context.newCDPSession(page);
 async function state(){const n=states.length;await page.evaluate(()=>window.prototypeInstance.SendMessage('Prototype Player','LogVerificationState'));for(let i=0;i<200&&states.length===n;i++)await page.waitForTimeout(20);assert(states.length>n);return states.at(-1);}
 async function client(s,p){const b=await page.locator('#unity-canvas').boundingBox();return {x:b.x+p.x/s.screenWidth*b.width,y:b.y+(1-p.y/s.screenHeight)*b.height};}
 async function tap(s,p){const c=await client(s,p);await page.touchscreen.tap(c.x,c.y);await page.waitForTimeout(200);return state();}
 async function capture(name,s){const file=path.join(artifacts,name+'.png');await page.screenshot({path:file});result.captures.push({file,state:s});}
 async function restart(s){s=await tap(s,s.pauseCenter);assert(s.manualPaused);s=await tap(s,s.menuRestartCenter);await page.waitForTimeout(650);s=await state();assert(s.started&&s.playerHealth===100&&s.answers.length===0);return s;}
 await page.goto(`http://127.0.0.1:${port}/`);await page.waitForFunction(()=>!!window.prototypeInstance,null,{timeout:180000});await page.waitForTimeout(5500);
 let s=await state();s=await tap(s,s.playCenter);await page.waitForTimeout(600);s=await state();
 await capture('visual-after-portrait',s);
 s=await tap(s,s.attackCenter);await page.waitForTimeout(340);s=await state();await capture('visual-after-portrait-impact',s);
 for(const landscape of [false,true]){
  await page.setViewportSize(landscape?{width:844,height:390}:{width:390,height:844});await page.waitForTimeout(400);s=await state();
  s=await restart(s);await capture('visual-after-'+(landscape?'landscape':'portrait')+'-touch',s);
  for(const [name,dx,dz] of [['east',1,0],['west',-1,0],['north',0,1],['south',0,-1],['northeast',1,1],['southwest',-1,-1]]){
   s=await restart(s);
   // The preserved practice totem blocks the straight north route; walk around it.
   if(name==='north') {
    const start=await client(s,s.joystickCenter),side=await client(s,{x:s.joystickCenter.x+s.joystickRadius,y:s.joystickCenter.y});
    await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{...start,id:1}]});
    await cdp.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{...side,id:1}]});
    await page.waitForTimeout(700);await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});await page.waitForTimeout(120);s=await state();
   }
   const magnitude=Math.hypot(dx,dz),center=await client(s,s.joystickCenter);
   const end=await client(s,{x:s.joystickCenter.x+dx/magnitude*s.joystickRadius,y:s.joystickCenter.y+dz/magnitude*s.joystickRadius});
   await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{...center,id:1}]});
   await cdp.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{...end,id:1}]});
   await page.waitForTimeout(magnitude>1?3500:2600);
   await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});await page.waitForTimeout(450);s=await state();result.latest=s;
   assert(!s.defeated&&!s.questionPaused&&s.canPlay,'Boundary movement unexpectedly ended combat');
   assert(Math.abs(s.position.x)<9.7&&Math.abs(s.position.z)<9.7,'Collision escaped');
   if(dx)assert(s.position.x*dx>9,'Did not reach x boundary');if(dz)assert(s.position.z*dz>9,'Did not reach z boundary');
   assert(Math.hypot(s.camera.x-s.position.x,s.camera.y-s.position.y-17,s.camera.z-s.position.z+6)<.2,'Camera failed to settle');
   await capture(`visual-${landscape?'landscape':'portrait'}-boundary-${name}`,s);
   result.checks.push({name:`${landscape?'landscape':'portrait'} ${name} wall collision and smooth following`,position:s.position});console.log('PASS: '+result.checks.at(-1).name);
  }
 }
 assert(result.errors.length===0,result.errors.join('\n'));result.checks.push({name:'No browser/Unity errors across visual boundary sweep'});
 result.environment={browser:browser.version(),renderer:'headless Chrome / SwiftShader, emulated touch; no physical iPhone'};
}catch(e){result.error=e.stack;console.error(e);process.exitCode=1;}finally{fs.writeFileSync(path.join(artifacts,'visual-presentation-result.json'),JSON.stringify(result,null,2));if(browser)await browser.close();server.close();}})();
