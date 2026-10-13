const http=require('http'),fs=require('fs'),path=require('path'),assert=require('assert');
const {chromium}=require(process.env.DRFORMS_PLAYWRIGHT||'playwright');
const {playerShell}=require('./player-shell.cjs');
const artifacts=process.env.DRFORMS_FLOW_ARTIFACTS,root=process.env.DRFORMS_FLOW_BUILD_PATH;
assert(path.isAbsolute(artifacts||'')&&path.isAbsolute(root||''));
const result={checks:[],sessions:[]},port=8767;
const server=http.createServer((req,res)=>{
 const name=new URL(req.url,'http://localhost').pathname,file=path.resolve(root,'.'+(name==='/'?'/index.html':name));
 if(!file.startsWith(root+path.sep)||!fs.existsSync(file)){res.writeHead(404);return res.end();}
 const mime={'.html':'text/html','.js':'application/javascript','.wasm':'application/wasm','.css':'text/css','.png':'image/png'};
 res.setHeader('Content-Type',mime[path.extname(file)]||'application/octet-stream');
 if(name==='/'){return res.end(playerShell(fs.readFileSync(file,'utf8')).replace('.then((unityInstance) => {','.then((unityInstance) => {window.prototypeInstance=unityInstance;'));}
 fs.createReadStream(file).pipe(res);
});
const pass=(name,data)=>{result.checks.push({name,data});console.log('PASS: '+name);};
(async()=>{let browser;try{
 await new Promise(r=>server.listen(port,'127.0.0.1',r));
 browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-unsafe-swiftshader']});
 result.browser=browser.version();result.renderer='headless Chrome / SwiftShader on Windows; emulated touch, no iPhone hardware';
 for(const phone of [false,true]){
  const context=await browser.newContext(phone?{viewport:{width:390,height:844},hasTouch:true,isMobile:true,deviceScaleFactor:1,userAgent:'Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 Version/18.0 Mobile/15E148 Safari/604.1'}:{viewport:{width:1280,height:900}});
  const page=await context.newPage(),states=[],events=[];result.sessions.push({phone,states,events});
  page.on('console',m=>{if(m.text().startsWith('PROTOTYPE_STATE:'))states.push(JSON.parse(m.text().slice(16)));else if(m.type()==='error')events.push(m.text());});
  page.on('pageerror',e=>events.push(e.message));
  async function load(){await page.goto(`http://127.0.0.1:${port}/`);await page.waitForFunction(()=>!!window.prototypeInstance,null,{timeout:180000});await page.waitForTimeout(5500);}
  async function state(){let n=states.length;await page.evaluate(()=>window.prototypeInstance.SendMessage('Prototype Player','LogVerificationState'));await page.waitForFunction(()=>true);for(let i=0;i<200&&states.length===n;i++)await page.waitForTimeout(20);assert(states.length>n);return states.at(-1);}
  async function click(s,p){const box=await page.locator('#unity-canvas').boundingBox(),x=box.x+p.x/s.screenWidth*box.width,y=box.y+(1-p.y/s.screenHeight)*box.height;if(phone)await page.touchscreen.tap(x,y);else await page.mouse.click(x,y,{delay:80});await page.waitForTimeout(180);return state();}
  await load();let s=await state();assert(!s.started&&s.timeScale===0);
  assert(await page.locator('[id*="phone-diag"]').count()===0);
  const layout=await page.locator('#unity-canvas').boundingBox();assert(layout.width=== (phone?390:1280)&&layout.height===(phone?844:900));
  await page.screenshot({path:path.join(artifacts,`polish-${phone?'phone':'desktop'}-start.png`)});
  pass('Default menu has no diagnostic panel and the canvas fills its viewport',{phone,layout});
  s=await click(s,s.playCenter);assert(s.started&&s.shotsFired===0);
  assert(await page.evaluate(()=>window.drFormsAudio.context.state==='running'));
  pass('Trusted Play unlocks real Web Audio without attacking',{phone});
  await page.waitForTimeout(600);s=await state();await page.screenshot({path:path.join(artifacts,`polish-${phone?'phone':'desktop'}-combat.png`)});
  s=await click(s,s.pauseCenter);assert(s.manualPaused&&s.timeScale===0);
  const t=s.time,answers=s.answers,shots=s.shotsFired;
  const cdp=await context.newCDPSession(page);await cdp.send('Emulation.setFocusEmulationEnabled',{enabled:false});
  const other=await context.newPage();await other.goto('about:blank');await other.bringToFront();await page.waitForTimeout(300);
  let hidden=await state();assert(!hidden.focused&&hidden.timeScale===0&&hidden.manualPaused);
  await other.close();await page.bringToFront();await page.waitForTimeout(250);s=await state();
  assert(s.manualPaused&&s.timeScale===0&&s.time===t);assert.deepStrictEqual(s.answers,answers);
  s=await click(s,s.resumeCenter);assert(!s.manualPaused&&s.timeScale===1&&s.grace&&s.shotsFired===shots);
  assert(await page.evaluate(()=>window.drFormsAudio.context.state==='running'));
  pass('Real tab switch retains manual pause; Resume consumes input, grants grace and restores audio',{phone});
  // A genuine mute gesture, followed by a real reload, checks persistence.
  s=await click(s,s.muteCenter);assert(s.muted);
  assert(await page.evaluate(()=>localStorage.getItem('GardenQuestMute')==='1'));
  await page.reload();await page.waitForFunction(()=>!!window.prototypeInstance,null,{timeout:180000});await page.waitForTimeout(5500);s=await state();assert(s.muted&&!s.started&&s.answers.length===0);
  pass('Mute survives browser reload while run progress resets',{phone});
  if(phone){
   await page.setViewportSize({width:844,height:390});await page.waitForTimeout(500);s=await state();assert(!s.started&&s.muted);
   await page.screenshot({path:path.join(artifacts,'polish-phone-landscape-menu.png')});
   // CSS padding stands in for notch insets; this is explicitly emulation.
   await page.addStyleTag({content:'#unity-container{padding:19px 47px 21px 47px!important}'});await page.waitForTimeout(350);
   const box=await page.locator('#unity-canvas').boundingBox();assert(box.x>=47&&box.y>=19&&box.x+box.width<=797&&box.y+box.height<=369);
   s=await state();await page.screenshot({path:path.join(artifacts,'polish-phone-inset-menu.png')});
   pass('Landscape resize and emulated safe-area padding retain menu/mute state',{box});
  }
  s=await state();s=await click(s,s.playCenter);assert(s.started&&s.muted);
  await page.waitForTimeout(350);s=await state();const before=s.shotsFired;
  s=await click(s,phone?s.attackCenter:s.playerFloorScreen);
  assert(s.shotsFired===before+1);
  await page.waitForTimeout(550);assert(await page.evaluate(()=>window.drFormsAudio.tones===0));
  pass('Muted real attack emits no oscillator tones',{phone});
  s=await state();s=await click(s,s.pauseCenter);assert(s.manualPaused);
  s=await click(s,s.menuRestartCenter);await page.waitForTimeout(400);s=await state();
  assert(s.started&&!s.manualPaused&&s.playerHealth===100&&s.answers.length===0&&s.damage===25&&s.shotsFired===0&&s.muted);
  pass('Pause-menu Restart reloads a fresh run and keeps mute preference',{phone});
  assert(events.length===0,events.join('\n'));await context.close();
 }
 pass('No browser or Unity errors in supplementary UI checks');
}catch(e){result.error=e.stack;console.error(e);process.exitCode=1;}finally{fs.writeFileSync(path.join(artifacts,'polish-ui-result.json'),JSON.stringify(result,null,2));if(browser)await browser.close();server.close();}})();
