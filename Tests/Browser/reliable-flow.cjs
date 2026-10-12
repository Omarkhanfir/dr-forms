const http = require('http'), fs = require('fs'), path = require('path'), assert = require('assert');
const { chromium } = require(process.env.DRFORMS_PLAYWRIGHT || 'playwright');
const artifacts = process.env.DRFORMS_FLOW_ARTIFACTS;
assert(artifacts && path.isAbsolute(artifacts), 'Set DRFORMS_FLOW_ARTIFACTS to an absolute external build/artifact directory');
const root = path.join(artifacts, 'GamePrototypeBuild');
const result = { sessions: [], checks: [] };
const dist = (a, b) => Math.hypot(a.x - b.x, a.z - b.z);
const lag = s => Math.hypot(s.camera.x - s.position.x - 8, s.camera.y - s.position.y - 14, s.camera.z - s.position.z + 10);
const server = http.createServer((req, res) => {
  const name = decodeURIComponent(new URL(req.url, 'http://localhost').pathname);
  const file = path.resolve(root, '.' + (name === '/' ? '/index.html' : name));
  if (!file.startsWith(root + path.sep)) { res.writeHead(403); return res.end(); }
  if (!fs.existsSync(file) || !fs.statSync(file).isFile()) { res.writeHead(404); return res.end(); }
  const plain = file.replace(/\.(gz|br)$/, '');
  const mime = { '.html': 'text/html', '.js': 'application/javascript', '.wasm': 'application/wasm', '.css': 'text/css', '.png': 'image/png' };
  res.setHeader('Content-Type', mime[path.extname(plain)] || 'application/octet-stream');
  if (file.endsWith('.gz')) res.setHeader('Content-Encoding', 'gzip');
  if (file.endsWith('.br')) res.setHeader('Content-Encoding', 'br');
  if (file.endsWith('index.html')) {
    const html = fs.readFileSync(file, 'utf8').replace('.then((unityInstance) => {', '.then((unityInstance) => { window.prototypeInstance = unityInstance;');
    res.setHeader('Content-Length', Buffer.byteLength(html)); return res.end(html);
  }
  res.setHeader('Content-Length', fs.statSync(file).size); fs.createReadStream(file).pipe(res);
});
function passed(name, details) { result.checks.push({ name, details }); console.log('PASS: ' + name); }
(async () => {
  let browser;
  try {
    await new Promise(resolve => server.listen(8765, '127.0.0.1', resolve));
    browser = await chromium.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true, args: ['--enable-unsafe-swiftshader'] });
    async function open(touch, phone = false) {
      const target = {touch,phone}; result.sessions.push(target);
      target.events = []; target.states = [];
      const context = await browser.newContext(touch ? {
        viewport: phone ? { width: 390, height: 844 } : { width: 1024, height: 768 }, hasTouch: true, isMobile: true, deviceScaleFactor: 1,
        userAgent: 'Mozilla/5.0 (Linux; Android 13; Tablet) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36'
      } : { viewport: { width: 1280, height: 900 } });
      const page = await context.newPage();
      await page.addInitScript(() => {
        window.touchAudit = [];
        for (const type of ['touchstart', 'touchend', 'touchcancel'])
          document.addEventListener(type, event => window.touchAudit.push({ type,
            active: Array.from(event.touches, t => t.identifier), changed: Array.from(event.changedTouches, t => t.identifier) }), true);
      });
      page.on('console', message => {
        const text = message.text();
        if (text.startsWith('PROTOTYPE_STATE:')) target.states.push(JSON.parse(text.slice(16).trim()));
        else target.events.push({ type: message.type(), text });
      });
      page.on('pageerror', error => target.events.push({ type: 'pageerror', text: error.message }));
      page.on('requestfailed', req => target.events.push({ type: 'requestfailed', url: req.url() }));
      page.on('response', res => { if (res.status() >= 400) target.events.push({ type: 'http-error', url: res.url(), status: res.status() }); });
      await page.goto('http://127.0.0.1:8765', { waitUntil: 'domcontentloaded' });
      await page.waitForFunction(() => !!window.prototypeInstance, null, { timeout: 180000 });
      await page.waitForTimeout(1500);
      const snapshot = async () => {
        const before = target.states.length;
        await page.evaluate(() => window.prototypeInstance.SendMessage('Prototype Player', 'LogVerificationState'));
        const deadline = Date.now() + 8000;
        while (target.states.length === before && Date.now() < deadline) await page.waitForTimeout(20);
        assert(target.states.length > before, 'No state received');
        const s = target.states.at(-1);
        assert(Math.abs(s.position.x) < 9.7 && Math.abs(s.position.z) < 9.7, 'Escaped arena');
        assert(Math.abs(s.position.y - 1) < 0.15, 'Player left floor');
        if(s.enemyAlive) { assert(Math.abs(s.enemyPosition.x)<9.6 && Math.abs(s.enemyPosition.z)<9.6, 'Enemy escaped arena'); assert(Math.abs(s.enemyPosition.y-1)<0.15, 'Enemy left floor'); }
        return s;
      };
      const client = async (s, point) => {
        const box = await page.locator('#unity-canvas').boundingBox();
        return { x: box.x + point.x / s.screenWidth * box.width,
          y: box.y + (1 - point.y / s.screenHeight) * box.height };
      };
      let s = await snapshot();
      const playableDeadline = Date.now() + 20000;
      while (s.time <= 0 && Date.now() < playableDeadline) {
        await page.waitForTimeout(500);
        s = await snapshot();
      }
      assert(s.time > 0, 'Unity game loop did not start');
      assert(s.touchMode === touch, 'Wrong control mode: ' + JSON.stringify(s));
      target.initial = s;
      return { page, context, snapshot, client, target };
    }




    const poll=async(t,condition,timeout=15000)=>{
      let s; const end=Date.now()+timeout;
      do { s=await t.snapshot();if(condition(s)) return s;await t.page.waitForTimeout(45); }while(Date.now()<end);
      throw Error('State condition timed out: '+JSON.stringify(s));
    };
    const floorPoint=(s,world)=>{
      const f=s.forward,norm=Math.hypot(f.x,f.z),r={x:f.z/norm,y:0,z:-f.x/norm};
      const u={x:f.y*r.z,y:f.z*r.x-f.x*r.z,z:-f.y*r.x};
      const v={x:world.x-s.camera.x,y:-s.camera.y,z:world.z-s.camera.z};
      const dot=a=>a.x*v.x+a.y*v.y+a.z*v.z,focal=s.screenHeight/2/Math.tan(Math.PI/6),depth=dot(f);
      return {x:s.screenWidth/2+dot(r)*focal/depth,y:s.screenHeight/2+dot(u)*focal/depth};
    };
    const option=async(t,s,index)=>{
      for(let i=0;i<6;i++){
        const p=s.answerCenters[index];
        if(p.y>s.screenHeight*.27 && p.y<s.screenHeight*.92) return s;
        const start=await t.client(s,{x:s.screenWidth*.75,y:s.screenHeight*.45});
        if(t.target.touch){
          const end=await t.client(s,{x:s.screenWidth*.75,y:s.screenHeight*(p.y<s.screenHeight*.27?.75:.3)});
          await t.cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{...start,id:8}]});
          for(let step=1;step<=6;step++){await t.cdp.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{x:start.x+(end.x-start.x)*step/6,y:start.y+(end.y-start.y)*step/6,id:8}]});await t.page.waitForTimeout(30);}
          await t.cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});
        }else{await t.page.mouse.move(start.x,start.y);await t.page.mouse.wheel(0,p.y<s.screenHeight*.27?220:-220);}
        await t.page.waitForTimeout(200);s=await t.snapshot();
      }
      throw Error('Option cannot be scrolled into view: '+JSON.stringify(s));
    };
    let combatFocusChecked=false;
    const click=async(t,s,point,count=1)=>{
      const p=await t.client(s,point);
      if(t.target.touch) await t.page.touchscreen.tap(p.x,p.y);
      else await t.page.mouse.click(p.x,p.y,{clickCount:count});
      await t.page.waitForTimeout(90);
      return t.snapshot();
    };
    const freeze=async(t,s)=>{
      await t.page.waitForTimeout(650);const q=await t.snapshot();
      assert(q.time===s.time && dist(q.position,s.position)<.001 && q.playerHealth===s.playerHealth);
      assert(q.shotsFired===s.shotsFired && q.hitsApplied===s.hitsApplied && q.wave===s.wave);
      assert(q.cooldownRemaining===s.cooldownRemaining && q.breakRemaining===s.breakRemaining);
      assert.deepStrictEqual(q.projectilePositions,s.projectilePositions);
      return q;
    };
    // All actions below are browser mouse/keyboard/CDP touch events. SendMessage is
    // used only by snapshot() for the development build's read-only observation.
    async function clearWave(t,holdOnFinal=false){
      let s=await t.snapshot(),angle=0,lastClick=-1,held=false,heldAt=0;
      const end=Date.now()+100000;
      while(!s.questionPaused && !s.defeated && Date.now()<end){
        let goal={x:6*Math.cos(angle),z:6*Math.sin(angle)};
        if(dist(s.position,goal)<1.2){angle+=Math.PI/6;goal={x:6*Math.cos(angle),z:6*Math.sin(angle)};}
        if(t.target.touch){
          const norm=Math.hypot(s.forward.x,s.forward.z),fx=s.forward.x/norm,fz=s.forward.z/norm;
          const dx=goal.x-s.position.x,dz=goal.z-s.position.z,len=Math.hypot(dx,dz);
          const j=await t.client(s,{x:s.joystickCenter.x+(dx*fz-dz*fx)/len*s.joystickRadius*.85,
            y:s.joystickCenter.y+(dx*fx+dz*fz)/len*s.joystickRadius*.85});
          const a=await t.client(s,s.attackCenter);
          if(!t.joystickStarted){const center=await t.client(s,s.joystickCenter);await t.cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{...center,id:1}]});t.joystickStarted=true;}
          await t.cdp.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{...j,id:1}]});
          if(s.time-lastClick>.28){
            await t.cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{...j,id:1},{...a,id:2}]});
            await t.page.waitForTimeout(25);
            // CDP touchEnd names the finger ending, rather than remaining fingers.
            await t.cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[{...a,id:2}]});lastClick=s.time;
          }
        }else{
          const p=await t.client(s,floorPoint(s,goal));await t.page.mouse.move(p.x,p.y);
          if(!held && s.time-lastClick>.26){await t.page.mouse.click(p.x,p.y);lastClick=s.time;}
          // A shot is in flight at the last enemy; hold a second attack gesture
          // across the actual combat->question transition.
          if(holdOnFinal && s.enemyCount===1 && s.enemyHealth<=s.damage*2 && s.projectiles>0){
            await t.page.mouse.down();await t.page.keyboard.down('Enter');held=true;heldAt=Date.now();holdOnFinal=false;
          }
        }
        await t.page.waitForTimeout(45);s=await t.snapshot();
        if(held && !s.questionPaused && Date.now()-heldAt>1200){
          // The nearest target may have been the practice target. Keep kiting;
          // release and try again rather than assuming that shot clears a wave.
          await t.page.mouse.up();await t.page.keyboard.up('Enter');held=false;holdOnFinal=true;
        }
      }
      assert(!s.defeated,'Defeated while clearing wave');assert(s.questionPaused && !s.rewardVisible,'No question after clear');
      assert(s.answers.length===s.wave-1 && !s.victory && !s.onBreak);
      if(t.target.touch){ // joystick touch survives into UI until explicitly released
        await t.page.waitForTimeout(650);s=await t.snapshot();assert(!s.questionInputReady && s.selectedAnswer===-1);
        const p=await t.client(s,s.answerCenters[0]);
        await t.cdp.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{...p,id:1}]});
        await t.cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});t.joystickStarted=false;
        await t.page.waitForTimeout(80);s=await t.snapshot();assert(s.selectedAnswer===-1 && s.answers.length===s.wave-1);
        passed('Existing touch cannot select or submit newly opened question', {wave:s.wave,phone:t.target.phone});
      }
      if(held){
        await t.page.waitForTimeout(650);s=await t.snapshot();assert(!s.questionInputReady && s.selectedAnswer===-1);
        const p=await t.client(s,s.answerCenters[0]);await t.page.mouse.move(p.x,p.y);await t.page.mouse.up();
        await t.page.waitForTimeout(80);s=await t.snapshot();assert(s.selectedAnswer===-1 && s.answers.length===s.wave-1);
        await t.page.waitForTimeout(600);s=await t.snapshot();assert(!s.questionInputReady && s.selectedAnswer===-1);
        await t.page.keyboard.up('Enter');
        passed('Held attack and keyboard Submit across final kill cannot select or submit an answer');
      }
      // Rapid tapping before the guard expires must not select an option.
      for(let i=0;i<2;i++) s=await click(t,s,s.answerCenters[0]);
      // On ordinary (unheld) clears we may already be beyond the guard due to
      // low frame rates; held-transition checks guarantee this early boundary.
      if(held || t.target.touch) assert(s.selectedAnswer===-1);
      return poll(t,x=>x.questionInputReady);
    }
    async function answer(t,s,keyboard=false){
      const n=s.answers.length,damage=s.damage;
      s=await freeze(t,s);assert(!s.victory && s.answers.length===n);
      // Submit while unselected must do nothing.
      if(s.selectedAnswer<0){s=await click(t,s,s.submitCenter);assert(s.answers.length===n && !s.rewardVisible);}
      s=await option(t,s,0);s=await click(t,s,s.answerCenters[0]);assert(s.selectedAnswer===0 && s.answers.length===n && s.damage===damage && !s.rewardVisible);
      s=await option(t,s,1);s=await click(t,s,s.answerCenters[1]);assert(s.selectedAnswer===1 && s.answers.length===n);
      await t.page.screenshot({path:path.join(artifacts,`flow-${t.target.touch?(t.target.phone?'phone':'tablet'):'desktop'}-question-${s.wave}.png`)});
      passed('Selection can change without submitting or granting rewards', {wave:s.wave,touch:t.target.touch});
      if(keyboard){
        await t.page.keyboard.press('ArrowDown',{delay:80});await t.page.waitForTimeout(550);
        await t.page.keyboard.press('ArrowDown',{delay:80});await t.page.waitForTimeout(550);
        await t.page.keyboard.press('Enter',{delay:80});await t.page.waitForTimeout(100);s=await t.snapshot();
        assert(s.rewardVisible,'Keyboard focus/navigation failed to submit');
      }else s=await click(t,s,s.submitCenter);
      assert(s.rewardVisible && s.answers.length===n+1 && s.damage===damage+10 && !s.victory);
      // Repeat gesture / keyboard submit spam cannot duplicate or continue.
      s=await click(t,s,s.submitCenter,2);await t.page.keyboard.press('Enter');s=await t.snapshot();
      assert(s.rewardVisible && s.answers.length===n+1 && s.damage===damage+10 && !s.victory);
      s=await freeze(t,s);
      await t.page.screenshot({path:path.join(artifacts,`flow-${t.target.touch?(t.target.phone?'phone':'tablet'):'desktop'}-reward-${s.wave}.png`)});
      passed('Submission grants one equal reward; reward screen freezes and requires separate Continue', {wave:s.wave,touch:t.target.touch});
      s=await poll(t,x=>x.questionInputReady);
      s=await click(t,s,s.continueCenter);
      if(n===2){s=await poll(t,x=>x.victory && x.victoryVisible);assert(s.answers.length===3 && s.wave===3 && s.enemyCount===0);
        await t.page.screenshot({path:path.join(artifacts,`flow-${t.target.touch?(t.target.phone?'phone':'tablet'):'desktop'}-victory.png`)});
        passed('Victory requires all waves, final submission and reward Continue');
      }else {
        if(!t.target.touch&&!combatFocusChecked){
          const cdp=await t.context.newCDPSession(t.page);await cdp.send('Emulation.setFocusEmulationEnabled',{enabled:false});
          const other=await t.context.newPage();await other.goto('about:blank');await other.bringToFront();await t.page.waitForTimeout(350);
          const hidden=await t.snapshot();assert(!hidden.focused&&hidden.onBreak);await t.page.waitForTimeout(1000);const later=await t.snapshot();
          assert(later.time===hidden.time&&later.breakRemaining===hidden.breakRemaining&&later.wave===hidden.wave);
          await other.close();await t.page.bringToFront();await t.page.waitForTimeout(150);const returned=await t.snapshot();
          assert(returned.focused&&!returned.hasDestination&&returned.wave===hidden.wave);
          combatFocusChecked=true;passed('Focus loss freezes combat clock and wave-break countdown without stale movement');
        }
        s=await poll(t,x=>x.wave===n+2 && !x.questionPaused);
      }
      return s;
    }
    const d=await open(false);let s=await poll(d,x=>x.wave===1);
    // First run: guard regressions + keyboard + preserved retry mid-run.
    s=await clearWave(d,true);
    const focusCdp=await d.context.newCDPSession(d.page);
    await focusCdp.send('Emulation.setFocusEmulationEnabled',{enabled:false});
    const other=await d.context.newPage();await other.goto('about:blank');await other.bringToFront();
    await d.page.waitForTimeout(350);const unfocused=await d.snapshot();assert(!unfocused.focused);
    await d.page.waitForTimeout(500);const still=await d.snapshot();assert(still.time===unfocused.time && still.answers.length===0);
    await other.close();await d.page.bringToFront();
    s=await poll(d,x=>x.focused&&x.questionInputReady);assert(s.selectedAnswer===-1&&s.answers.length===0);
    passed('Actual tab focus loss/regain keeps question paused and cannot advance');
    s=await answer(d,s,true);
    assert(s.wave===2 && s.answers.length===1 && s.damage===35);
    s=await poll(d,x=>x.defeated&&x.defeatVisible,45000);
    const prior=s.answers;await d.page.waitForTimeout(500);s=await click(d,s,s.restartCenter);
    s=await poll(d,x=>x.wave===2&&!x.defeated);
    assert.deepStrictEqual(s.answers,prior);assert(s.damage===35 && s.playerHealth===100 && s.enemyCount===3);
    assert(s.projectiles===0 && s.effects===0 && s.shotsFired===0 && !s.questionPaused);
    passed('Retry restores only unfinished wave, health/transients, retaining prior answer and reward');
    s=await poll(d,x=>x.defeated&&x.defeatVisible,45000);s=await click(d,s,s.restartCenter);
    s=await poll(d,x=>x.wave===2&&!x.defeated);assert(s.answers.length===1&&s.damage===35&&s.playerHealth===100&&s.enemyCount===3);
    passed('Repeated defeat/retry preserves progress without duplicating rewards');
    for(let i=0;i<2;i++) {s=await clearWave(d,i===0);s=await answer(d,s);}
    const win=s;s=await freeze(d,s);assert(s.answers.length===3);
    s=await click(d,s,s.restartCenter);s=await poll(d,x=>x.wave===1&&!x.victory);
    assert(s.answers.length===0 && s.damage===25 && s.playerHealth===100 && s.enemyCount===2 && s.shotsFired===0);
    passed('New Run clears every answer, reward and wave progress');
    // Complete another run in the same browser to catch static-session leakage.
    for(let i=0;i<3;i++){s=await clearWave(d,i===0);s=await answer(d,s);}
    await d.context.close();
    for(const phone of [false,true]){
      const t=await open(true,phone);t.cdp=await t.context.newCDPSession(t.page);
      s=await poll(t,x=>x.wave===1);s=await clearWave(t);s=await answer(t,s);
      // Rotate an emulated portrait phone into landscape during Question 2.
      s=await clearWave(t);
      if(phone){await t.page.setViewportSize({width:844,height:390});await t.page.waitForTimeout(500);s=await t.snapshot();}
      s=await answer(t,s);s=await clearWave(t);s=await answer(t,s);
      passed('Complete emulated touch run and responsive question/reward controls', {phone});
      await t.context.close();
    }
    for(const target of result.sessions)assert(!target.events.some(e=>['error','pageerror','requestfailed','http-error'].includes(e.type)),JSON.stringify(target.events));
    passed('No browser errors, page exceptions, or failed requests');
  }catch(error){result.error=error.stack;console.error(error);process.exitCode=1;}
  finally{fs.writeFileSync(path.join(artifacts,'reliable-flow-browser-result.json'),JSON.stringify(result,null,2));if(browser)await browser.close();server.close();}
})();
