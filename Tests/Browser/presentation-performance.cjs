// Repeatable presentation observation. Browser software rendering is not device FPS.
const http=require('http'),fs=require('fs'),path=require('path'),assert=require('assert');
const {chromium}=require(process.env.DRFORMS_PLAYWRIGHT||'playwright');
const {playerShell}=require('./player-shell.cjs');
const root=process.env.DRFORMS_FLOW_BUILD_PATH,artifacts=process.env.DRFORMS_FLOW_ARTIFACTS;
assert(path.isAbsolute(root||'')&&path.isAbsolute(artifacts||''));
const port=8774,result={sessions:[],environment:'Windows Chrome / SwiftShader; Unity frame counters and browser RAF, not hardware GPU or physical iPhone'};
const server=http.createServer((req,res)=>{
 const name=new URL(req.url,'http://localhost').pathname,file=path.resolve(root,'.'+(name==='/'?'/index.html':name));
 if(!file.startsWith(root+path.sep)||!fs.existsSync(file)){res.writeHead(404);return res.end();}
 res.setHeader('Cache-Control','no-store');
 res.setHeader('Content-Type',({'.html':'text/html','.js':'application/javascript','.wasm':'application/wasm','.png':'image/png'})[path.extname(file)]||'application/octet-stream');
 if(name==='/')return res.end(playerShell(fs.readFileSync(file,'utf8')).replace('.then((unityInstance) => {','.then((unityInstance) => {window.prototypeInstance=unityInstance;'));
 fs.createReadStream(file).pipe(res);
});
(async()=>{let browser;try{
 await new Promise(r=>server.listen(port,'127.0.0.1',r));
 browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-unsafe-swiftshader']});
 result.browser=browser.version();
 for(const [name,width,height,touch] of [['desktop',1440,900,false],['tablet',1024,768,true],['phone',390,844,true]]){
  const context=await browser.newContext({viewport:{width,height},hasTouch:touch,isMobile:touch,deviceScaleFactor:1,
   ...(touch?{userAgent:'Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 Version/18.0 Mobile/15E148 Safari/604.1'}:{})});
  const page=await context.newPage(),states=[],errors=[],session={name,states,errors,windows:[]};result.sessions.push(session);
  page.on('console',m=>{if(m.text().startsWith('PROTOTYPE_STATE:'))states.push(JSON.parse(m.text().slice(16)));else if(m.type()==='error')errors.push(m.text());});
  page.on('pageerror',e=>errors.push(e.message));page.on('requestfailed',r=>errors.push(r.url()));
  await page.goto(`http://127.0.0.1:${port}/`);await page.waitForFunction(()=>!!window.prototypeInstance,null,{timeout:180000});await page.waitForTimeout(5500);
  const state=async()=>{const n=states.length;await page.evaluate(()=>window.prototypeInstance.SendMessage('Prototype Player','LogVerificationState'));for(let i=0;i<200&&states.length===n;i++)await page.waitForTimeout(20);assert(states.length>n);return states.at(-1);};
  let s=await state();const box=await page.locator('#unity-canvas').boundingBox(),p={x:box.x+s.playCenter.x/s.screenWidth*box.width,y:box.y+(1-s.playCenter.y/s.screenHeight)*box.height};
  if(touch)await page.touchscreen.tap(p.x,p.y);else await page.mouse.click(p.x,p.y);
  await page.waitForTimeout(600);s=await state();assert(s.canPlay&&s.wave===1);
  await page.screenshot({path:path.join(artifacts,`composition-${name}.png`)});
  await page.evaluate(()=>{window.reviewRaf=0;window.reviewRafRunning=true;function frame(){if(!window.reviewRafRunning)return;window.reviewRaf++;requestAnimationFrame(frame);}requestAnimationFrame(frame);});
  let raf=await page.evaluate(()=>window.reviewRaf),clock=Date.now();
  for(let i=0;i<12;i++){
   await page.waitForTimeout(1000);const b=await state(),nextRaf=await page.evaluate(()=>window.reviewRaf),nextClock=Date.now(),dt=b.realtime-s.realtime;
   assert(b.canPlay&&!b.defeated&&b.answers.length===0,'Measurement must remain in real active combat');
   session.windows.push({unityFps:(b.frame-s.frame)/dt,browserRaf:(nextRaf-raf)/((nextClock-clock)/1000)});s=b;raf=nextRaf;clock=nextClock;
  }
  await page.evaluate(()=>window.reviewRafRunning=false);
  for(const key of ['unityFps','browserRaf']){const sorted=session.windows.map(w=>w[key]).sort((a,b)=>a-b);session[key]={median:sorted[6],p10:sorted[1],samples:sorted.length};}
  assert(errors.length===0,errors.join('\n'));await context.close();console.log('MEASURED '+name,JSON.stringify({unity:session.unityFps,raf:session.browserRaf}));
 }
}catch(e){result.error=e.stack;process.exitCode=1;console.error(e);}finally{fs.writeFileSync(path.join(artifacts,'presentation-performance-result.json'),JSON.stringify(result,null,2));if(browser)await browser.close();server.close();}})();
