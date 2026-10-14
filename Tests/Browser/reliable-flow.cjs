const http = require('http'), fs = require('fs'), path = require('path'), assert = require('assert');
const { chromium } = require(process.env.DRFORMS_PLAYWRIGHT || 'playwright');
const artifacts = process.env.DRFORMS_FLOW_ARTIFACTS;
assert(artifacts && path.isAbsolute(artifacts), 'Set DRFORMS_FLOW_ARTIFACTS to an absolute external build/artifact directory');
const root = process.env.DRFORMS_FLOW_BUILD_PATH || path.join(artifacts, 'GamePrototypeBuild');
const port = Number(process.env.DRFORMS_FLOW_PORT || 8766);
const result = { sessions: [], checks: [] };
const dist = (a, b) => Math.hypot(a.x - b.x, a.z - b.z);
// Check all four viewport rays, including the ground behind touch controls.
const groundCoverage = s => {
  const f=s.forward,n=Math.hypot(f.x,f.z),r={x:f.z/n,y:0,z:-f.x/n};
  const u={x:f.y*r.z,y:f.z*r.x-f.x*r.z,z:-f.y*r.x};
  const tan=Math.tan(s.fieldOfView*Math.PI/360),aspect=s.screenWidth/s.screenHeight;
  for(const x of [-1,1])for(const y of [-1,1]) {
    const d={x:f.x+r.x*x*tan*aspect+u.x*y*tan,y:f.y+u.y*y*tan,z:f.z+r.z*x*tan*aspect+u.z*y*tan};
    assert(d.y<0,'Camera reveals sky');
    const t=(.006-s.camera.y)/d.y;
    assert(t>0&&Math.abs(s.camera.x+d.x*t)<79&&Math.abs(s.camera.z+d.z*t)<79,'Camera reveals scenery edge');
  }
};
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
    const html = require('./player-shell.cjs').playerShell(fs.readFileSync(file, 'utf8')).replace('.then((unityInstance) => {', '.then((unityInstance) => { window.prototypeInstance = unityInstance;');
    res.setHeader('Content-Length', Buffer.byteLength(html)); return res.end(html);
  }
  res.setHeader('Content-Length', fs.statSync(file).size); fs.createReadStream(file).pipe(res);
});
function passed(name, details) { result.checks.push({ name, details }); console.log('PASS: ' + name); }
(async () => {
  let browser;
  try {
    await new Promise(resolve => server.listen(port, '127.0.0.1', resolve));
    browser = await chromium.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true, args: ['--enable-unsafe-swiftshader'] });
    async function open(touch, phone = false) {
      const target = {touch,phone}; result.sessions.push(target);
      target.events = []; target.states = [];
      const context = await browser.newContext({...(touch ? {
        viewport: phone ? { width: 390, height: 844 } : { width: 1024, height: 768 }, hasTouch: true, isMobile: true, deviceScaleFactor: 1,
        userAgent: 'Mozilla/5.0 (Linux; Android 13; Tablet) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36'
      } : { viewport: { width: 1280, height: 900 } }),...(process.env.DRFORMS_RECORD_VIDEO==='1'?{recordVideo:{dir:path.join(artifacts,'video'),size:{width:1280,height:900}}}:{})});
      const page = await context.newPage();
      if(phone) await page.addInitScript(()=>{document.hasFocus=()=>false;});
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
      await page.goto(`http://127.0.0.1:${port}${phone ? "/?longQuestions=1" : "/"}`, { waitUntil: 'domcontentloaded' });
      await page.waitForFunction(() => !!window.prototypeInstance, null, { timeout: 180000 });
      await page.waitForTimeout(5500);
      const snapshot = async () => {
        const before = target.states.length;
        await page.evaluate(() => window.prototypeInstance.SendMessage('Prototype Player', 'LogVerificationState'));
        const deadline = Date.now() + 8000;
        while (target.states.length === before && Date.now() < deadline) await page.waitForTimeout(20);
        assert(target.states.length > before, 'No state received');
        const s = target.states.at(-1);
        groundCoverage(s);
        assert(s.playerFloorScreen.x>0&&s.playerFloorScreen.x<s.screenWidth&&s.playerFloorScreen.y>0&&s.playerFloorScreen.y<s.screenHeight,'Camera lost player framing');
        assert(Math.abs(s.position.x) < 9.7 && s.position.z > -9.7 && s.position.z < 62, 'Escaped arena');
        assert(Math.abs(s.position.y - 1) < 0.15, 'Player left floor');
        if(s.enemyAlive) { assert(Math.abs(s.enemyPosition.x)<9.6 && s.enemyPosition.z > -9.6 && s.enemyPosition.z < 58, 'Enemy escaped arena'); assert(Math.abs(s.enemyPosition.y-1)<0.15, 'Enemy left floor'); }
        return s;
      };
      const client = async (s, point) => {
        const box = await page.locator('#unity-canvas').boundingBox();
        return { x: box.x + point.x / s.screenWidth * box.width,
          y: box.y + (1 - point.y / s.screenHeight) * box.height };
      };
      let s = await snapshot();
      if(phone){
        await page.waitForTimeout(500);s=await snapshot();target.safariStartup=s;
        assert(s.applicationFocus&&!s.bridgeFocus&&!s.focused&&s.timeScale===0&&s.waitingForRelease&&!s.pointerPressed);
        passed('Safari startup with DOM focus false reproduces the exact paused/input-wait state before any interaction');
        const p=await client(s,s.attackCenter);await page.touchscreen.tap(p.x,p.y);
      }
      // Fresh Play is deliberate; a Safari focus-recovery gesture is consumed first.
      await page.waitForTimeout(250); s = await snapshot();
      assert(!s.started && s.timeScale === 0 && s.wave === 0 && s.shotsFired === 0);
      await page.screenshot({path:path.join(artifacts, `polish-${touch ? phone ? 'phone' : 'tablet' : 'desktop'}-start.png`)});
      const play = await client(s, s.playCenter);
      if (touch) await page.touchscreen.tap(play.x, play.y); else await page.mouse.click(play.x, play.y);
      await page.waitForTimeout(300); s = await snapshot();
      assert(s.started && s.shotsFired === 0 && !s.manualPaused);
      passed('Fresh Play starts combat and consumes its gesture without attacking');
      const playableDeadline = Date.now() + 20000;
      while (s.time <= 0 && Date.now() < playableDeadline) {
        await page.waitForTimeout(500);
        s = await snapshot();
      }
      assert(s.time > 0, 'Unity game loop did not start');
      assert(s.touchMode === touch, 'Wrong control mode: ' + JSON.stringify(s));
      if(phone){
        assert(s.focused&&s.bridgeFocus&&s.applicationFocus&&s.timeScale===1&&s.shotsFired===0);
        assert(await page.evaluate(()=>!document.hasFocus()));
        await page.evaluate(()=>{delete document.hasFocus;});
        passed('Trusted visible startup touch restores the saved clock while DOM focus stays false and fires no attack');
      }
      // Exercise menu ownership and persistent sound with genuine clicks/taps.
      const act = async point => { const p = await client(s, point); if(touch) await page.touchscreen.tap(p.x,p.y); else await page.mouse.click(p.x,p.y); await page.waitForTimeout(150); s=await snapshot(); };
      await act(s.pauseCenter); assert(s.manualPaused && s.timeScale===0 && !s.canPlay);
      const pausedTime=s.time, shots=s.shotsFired;
      await page.waitForTimeout(300); s=await snapshot(); assert(s.time===pausedTime);
      // Sound stays outside the paused menu; test it after resume.
      await act(s.resumeCenter); assert(!s.manualPaused && s.timeScale===1 && s.shotsFired===shots && s.grace);
      const muted=s.muted; await act(s.muteCenter); assert(s.muted!==muted);
      await page.waitForTimeout(600); await act(s.muteCenter); assert(s.muted===muted);
      passed('Manual Pause freezes its own clock; Resume consumes input and adds grace; mute toggles');
      assert(await page.evaluate(()=>window.drFormsAudio.context.state==='running'));
      passed('Real browser Web Audio remains running after Play and mute toggles');
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
      const dot=a=>a.x*v.x+a.y*v.y+a.z*v.z,focal=s.screenHeight/2/Math.tan(s.fieldOfView*Math.PI/360),depth=dot(f);
      return {x:s.screenWidth/2+dot(r)*focal/depth,y:s.screenHeight/2+dot(u)*focal/depth};
    };
    const option=async(t,s,index)=>{
      for(let i=0;i<18;i++){
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
      else if(count===2) await t.page.mouse.dblclick(p.x,p.y,{delay:35});
      else await t.page.mouse.click(p.x,p.y);
      // Scene reload and its first physics step can briefly settle the controller above ground.
      const reload=(s.defeated||s.victory)&&point.x===s.restartCenter.x&&point.y===s.restartCenter.y;
      await t.page.waitForTimeout(reload?650:90);
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
    // Fault injection is restricted to browser focus/visibility observations.
    // Unity gameplay state is never set: reproduce Safari's DOM focus false
    // while Unity focus remains true, then recover using trusted real input.
    const safariPause=async(t)=>{
      await t.page.evaluate(()=>{
        document.hasFocus=()=>false;
        document.dispatchEvent(new Event('visibilitychange'));
      });
      const s=await poll(t,x=>x.applicationFocus&&!x.bridgeFocus&&!x.focused
        &&x.timeScale===0&&x.waitingForRelease&&!x.pointerPressed);
      assert(await t.page.evaluate(()=>!document.hidden&&!document.hasFocus()));
      return freeze(t,s);
    };
    const clearSafariObservation=async(t)=>t.page.evaluate(()=>{delete document.hasFocus;delete document.hidden;});
    const recoverSafari=async(t,s,point,scale)=>{
      const p=await t.client(s,point),epoch=s.browserEpoch,shots=s.shotsFired,answers=s.answers;
      if(t.target.touch)await t.cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{...p,id:9}]});
      else {await t.page.mouse.move(p.x,p.y);await t.page.mouse.down();}
      await t.page.waitForTimeout(150);s=await t.snapshot();
      assert(s.applicationFocus&&s.bridgeFocus&&s.focused&&s.browserEpoch>epoch&&s.waitingForRelease&&!s.canPlay);
      assert(s.shotsFired===shots);assert.deepStrictEqual(s.answers,answers);
      assert(await t.page.evaluate(()=>!document.hasFocus()),'Recovery incorrectly relied on DOM focus becoming true');
      if(t.target.touch)await t.cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});
      else await t.page.mouse.up();
      s=await poll(t,x=>x.inputReady&&!x.waitingForRelease&&!x.pointerPressed&&x.timeScale===scale);
      assert(s.shotsFired===shots);assert.deepStrictEqual(s.answers,answers);
      return s;
    };
    // All actions below are browser mouse/keyboard/CDP touch events. SendMessage is
    // used only by snapshot() for the development build's read-only observation.
    async function traverse(t,s) {
      const room=s.room, starts=s.encounterStarts, shots=s.shotsFired;
      await t.page.waitForTimeout(1000);s=await t.snapshot();
      assert(s.room===room&&!s.victory&&s.doorOpening===1&&s.encounterStarts===starts);
      passed('Door animates open; waiting after Submit never starts another encounter or Victory',{room});
      const end=Date.now()+20000;
      while(s.room===room&&!s.victory&&Date.now()<end) {
        const goal={x:0,z:room*24+(room===2?12.5:15)};
        if(t.target.touch){
          const dx=goal.x-s.position.x,dz=goal.z-s.position.z,len=Math.hypot(dx,dz);
          const center=await t.client(s,s.joystickCenter);
          const j=await t.client(s,{x:s.joystickCenter.x+dx/len*s.joystickRadius*.95,y:s.joystickCenter.y+dz/len*s.joystickRadius*.95});
          if(!t.joystickStarted){await t.cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{...center,id:1}]});t.joystickStarted=true;}
          await t.cdp.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{...j,id:1}]});
        }else{
          // Use a nearby visible waypoint; only real pointer movement resamples world ground.
          const step=Math.min(5,Math.hypot(goal.x-s.position.x,goal.z-s.position.z));
          const len=Math.hypot(goal.x-s.position.x,goal.z-s.position.z);
          const p=await t.client(s,floorPoint(s,{x:s.position.x+(goal.x-s.position.x)/len*step,z:s.position.z+(goal.z-s.position.z)/len*step}));
          await t.page.mouse.move(p.x+(Date.now()%2?2:-2),p.y);
        }
        await t.page.waitForTimeout(80);s=await t.snapshot();
        if(s.position.z>room*24+8&&s.position.z<room*24+13&&!t.target['routeCapture'+room]) {
          t.target['routeCapture'+room]=true;
          await t.page.screenshot({path:path.join(artifacts,`route-${t.target.phone?'phone':t.target.touch?'tablet':'desktop'}-passage-${room+1}.png`)});
        }
      }
      if(t.target.touch){await t.cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});t.joystickStarted=false;}
      assert(room===2?s.victory:s.room===room+1&&s.encounterStarts===starts+1,'Physical doorway crossing did not advance once');
      passed('Physical passage crossing advances exactly once',{room,position:s.position});
      if(room<2) {
        await t.page.waitForTimeout(350);const later=await t.snapshot();
        assert(later.room===room+1&&later.encounterStarts===starts+1&&later.enemyCount===room+3&&!later.exitUnlocked);
        passed('Entry boundary does not duplicate encounter and new exit remains locked',{room:later.room});
        s=later;
        if(!t.target.touch) {
          await t.page.waitForTimeout(900);const settled=await t.snapshot();
          await t.page.waitForTimeout(400);const still=await t.snapshot();
          assert(dist(settled.position,still.position)<.015&&dist(settled.destination,still.destination)<.001);
          passed('Stationary desktop pointer retains its world destination as route camera settles');
          s=still;
        }
        if(t.target.touch&&!t.target.phone&&room===0) {
          // Backtrack into the cleared room using actual joystick events.
          const move=async direction=>{
            const center=await t.client(s,s.joystickCenter);
            const thumb=await t.client(s,{x:s.joystickCenter.x,y:s.joystickCenter.y+direction*s.joystickRadius*.95});
            await t.cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{...center,id:1}]});
            await t.cdp.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{...thumb,id:1}]});
          };
          await move(-1);await t.page.waitForTimeout(1400);
          await t.cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});s=await t.snapshot();
          assert(s.position.z<14&&s.room===1);
          const health=s.playerHealth,enemy=s.enemyPosition,ranged=s.rangedShots;
          await t.page.waitForTimeout(1200);s=await t.snapshot();
          assert(s.playerHealth===health&&dist(s.enemyPosition,enemy)<.001&&s.enemyBolts===0);
          assert.deepStrictEqual(s.rangedShots,ranged);
          passed('Enemies stop moving and attacking while player backtracks into inactive cleared room');
          await move(1);await t.page.waitForTimeout(1700);
          await t.cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});s=await t.snapshot();
          assert(s.room===1&&s.encounterStarts===starts+1&&s.enemyCount===3);
          passed('Re-entering current room does not spawn its encounter twice');
        }
      }
      return s;
    }
    async function clearWave(t,holdOnFinal=false,retries=0){
      let s=await t.snapshot(),lastClick=-1,held=false,heldAt=0,lastAlive=s;
      const demonstrateCast=process.env.DRFORMS_CAPTURE_COMBAT_ONLY==='1'&&s.wave===2;
      let demonstrateAfter=Infinity;
      const recordMotion=s.wave===2&&!t.target.touch&&!t.target.motionRecorded;
      if(recordMotion){
        t.target.motionRecorded=true;
        await t.page.evaluate(()=>{
          window.combatChunks=[];
          const source=document.querySelector('#unity-canvas'),capture=document.createElement('canvas');capture.width=source.width;capture.height=source.height;
          const paint=capture.getContext('2d');window.combatCapturing=true;
          function copy(){if(!window.combatCapturing)return;paint.drawImage(source,0,0);requestAnimationFrame(copy);}requestAnimationFrame(copy);
          window.combatRecorder=new MediaRecorder(capture.captureStream(30),{mimeType:'video/webm;codecs=vp9',videoBitsPerSecond:2500000});
          window.combatRecorder.ondataavailable=e=>{if(e.data.size)window.combatChunks.push(e.data)};window.combatRecorder.start();
        });
      }
      const initialShots=s.shotsFired,initialMoving=s.movingSeconds;
      assert(s.rangedCount === (s.wave>=2 ? 1 : 0), 'Introduce one mage only after wave one');
      let observedTelegraph=false, observedRangedShot=false;
      // Continue around the perimeter after answering instead of resetting the
      // goal across the arena and running through a random chaser cluster.
      let angle=Math.hypot(s.position.x,s.position.z)>1?Math.atan2(s.position.z-s.room*24,s.position.x)+Math.PI/6:0;
      const end=Date.now()+100000;
      while(!s.questionPaused && !s.defeated && Date.now()<end){
        const framing=s.screenWidth>s.screenHeight?'landscape':'portrait';
        if(!t.target['capture'+framing]) {
          t.target['capture'+framing]=true;
          await t.page.screenshot({path:path.join(artifacts,`visual-after-${t.target.phone?'phone':t.target.touch?'tablet':'desktop'}-${framing}.png`)});
        }
        observedTelegraph ||= s.telegraphCount>0;
        if(demonstrateCast&&s.telegraphCount>0&&demonstrateAfter===Infinity)demonstrateAfter=s.time+1.9;
        if(s.telegraphCount>0 && !t.target.telegraphScreenshot) {
          t.target.telegraphScreenshot=true;
          await t.page.screenshot({path:path.join(artifacts, `polish-${t.target.touch?t.target.phone?'phone':'tablet':'desktop'}-ranged-telegraph.png`)});
        }
        observedRangedShot ||= s.rangedShots.some(n=>n>0);
        assert(s.projectiles<=8 && s.projectilePool<=8 && s.effects<=24 && s.enemyBolts<=4);
        let goal={x:6*Math.cos(angle),z:s.room*24+6*Math.sin(angle)};
        if(dist(s.position,goal)<1.2){angle+=Math.PI/6;goal={x:6*Math.cos(angle),z:s.room*24+6*Math.sin(angle)};}
        if(t.target.touch){
          const norm=Math.hypot(s.forward.x,s.forward.z),fx=s.forward.x/norm,fz=s.forward.z/norm;
          const dx=goal.x-s.position.x,dz=goal.z-s.position.z,len=Math.hypot(dx,dz);
          const j=await t.client(s,{x:s.joystickCenter.x+(dx*fz-dz*fx)/len*s.joystickRadius*.85,
            y:s.joystickCenter.y+(dx*fx+dz*fz)/len*s.joystickRadius*.85});
          const a=await t.client(s,s.attackCenter);
          if(!t.joystickStarted){const center=await t.client(s,s.joystickCenter);await t.cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{...center,id:1}]});t.joystickStarted=true;}
          await t.cdp.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{...j,id:1}]});
          if(s.time-lastClick>.28&&(!demonstrateCast||s.time>=demonstrateAfter)){
            await t.cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{...j,id:1},{...a,id:2}]});
            await t.page.waitForTimeout(25);
            // CDP touchEnd names the finger ending, rather than remaining fingers.
            await t.cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[{...a,id:2}]});lastClick=s.time;
          }
        }else{
          // A closer art camera cannot show every point on the old six-unit kiting circle.
          // Walk toward it using nearby visible floor waypoints and genuine fresh mouse moves.
          const dx=goal.x-s.position.x,dz=goal.z-s.position.z,len=Math.hypot(dx,dz),step=Math.min(2.4,len);
          const waypoint={x:s.position.x+dx/Math.max(.001,len)*step,z:s.position.z+dz/Math.max(.001,len)*step};
          const p=await t.client(s,floorPoint(s,waypoint));p.x+=t.target.states.length%2?4:-4;await t.page.mouse.move(p.x,p.y);
          if(!held && s.time-lastClick>.26&&(!demonstrateCast||s.time>=demonstrateAfter)){await t.page.mouse.click(p.x,p.y);lastClick=s.time;}
          // A shot is in flight at the last enemy; hold a second attack gesture
          // across the actual combat->question transition.
          if(holdOnFinal && s.enemyCount===1 && s.enemyHealth<=s.damage*2 && s.projectiles>0){
            await t.page.mouse.down();await t.page.keyboard.down('Enter');held=true;heldAt=Date.now();holdOnFinal=false;
          }
        }
        lastAlive=s;await t.page.waitForTimeout(45);s=await t.snapshot();
        if(held && !s.questionPaused && Date.now()-heldAt>1200){
          // The nearest target may have been the practice target. Keep kiting;
          // release and try again rather than assuming that shot clears a wave.
          await t.page.mouse.up();await t.page.keyboard.up('Enter');held=false;holdOnFinal=true;
        }
      }
      if(s.defeated&&t.target.touch&&retries<2){
        // Random chasers can legitimately kill a moving test player. Do not
        // hide an input freeze: recovery is allowed only after observed active
        // movement, shots, focus and a playable frame immediately before death.
        assert(s.focused&&lastAlive.canPlay&&s.shotsFired>initialShots&&s.movingSeconds>initialMoving);
        const prior=s.answers,damage=s.damage,wave=s.wave;
        await t.cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});t.joystickStarted=false;
        s=await poll(t,x=>x.defeated&&x.defeatVisible&&x.inputReady&&!x.pointerPressed);
        if(t.target.phone){
          s=await safariPause(t);s=await recoverSafari(t,s,s.restartCenter,0);
          assert(s.defeated&&s.playerHealth===0&&!s.canPlay);await clearSafariObservation(t);
          passed('Phone touch recovery on Retry consumes the gesture and keeps the defeat pause');
        }
        await t.page.waitForTimeout(550);s=await click(t,s,s.restartCenter);
        s=await poll(t,x=>x.wave===wave&&!x.defeated&&x.canPlay);
        assert.deepStrictEqual(s.answers,prior);assert(s.damage===damage&&s.playerHealth===100&&s.enemyCount===wave+1);
        passed('Ordinary touch Retry after active-combat defeat preserves answers/rewards and restores only the unfinished wave', {phone:t.target.phone,wave});
        return clearWave(t,holdOnFinal,retries+1);
      }
      if(recordMotion){
        const recording=await t.page.evaluate(async()=>{
          await new Promise(r=>{window.combatRecorder.onstop=r;window.combatRecorder.stop()});
          window.combatCapturing=false;
          return new Promise(r=>{const f=new FileReader();f.onload=()=>r(f.result.split(',')[1]);f.readAsDataURL(new Blob(window.combatChunks,{type:'video/webm'}));});
        });
        fs.writeFileSync(path.join(artifacts,'manuscript-both-enemies.webm'),Buffer.from(recording,'base64'));
      }
      if(observedTelegraph && observedRangedShot) passed('Bloom specimen windup and slow ranged shot observed during actual combat', {wave:s.wave,phone:t.target.phone});
      assert(!s.defeated,'Defeated while clearing wave');assert(s.questionPaused,'No question after clear');
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
      for(let i=0;i<2;i++) {
        const guarded=!s.questionInputReady;s=await click(t,s,s.answerCenters[0]);
        if(guarded&&!s.questionInputReady)assert(s.selectedAnswer===-1);
        assert(s.answers.length===s.wave-1);
      }
      // On ordinary (unheld) clears we may already be beyond the guard due to
      // low frame rates; held-transition checks guarantee this early boundary.
      // A fresh tap delivered after the observed guard expires may legitimately select.
      return poll(t,x=>x.questionInputReady);
    }
    async function answer(t,s,keyboard=false){
      const n=s.answers.length,damage=s.damage;
      s=await freeze(t,s);assert(!s.victory && s.answers.length===n);
      // Submit while unselected must do nothing.
      if(s.selectedAnswer<0){s=await click(t,s,s.submitCenter);assert(s.answers.length===n && s.questionPaused);}
      s=await option(t,s,0);s=await click(t,s,s.answerCenters[0]);assert(s.selectedAnswer===0 && s.answers.length===n && s.damage===damage && s.questionPaused);
      s=await option(t,s,1);s=await click(t,s,s.answerCenters[1]);assert(s.selectedAnswer===1 && s.answers.length===n);
      await t.page.screenshot({path:path.join(artifacts,`flow-${t.target.touch?(t.target.phone?'phone':'tablet'):'desktop'}-question-${s.wave}.png`)});
      passed('Selection can change without submitting or granting rewards', {wave:s.wave,touch:t.target.touch});
      const submitPoint=s.submitCenter,shots=s.shotsFired,hits=s.hitsApplied,position=s.position,chosen=s.selectedAnswer;
      if(keyboard){
        await t.page.keyboard.press('ArrowDown',{delay:80});await t.page.waitForTimeout(550);
        await t.page.keyboard.press('ArrowDown',{delay:80});await t.page.waitForTimeout(550);
        await t.page.keyboard.press('Enter',{delay:80});await t.page.waitForTimeout(100);s=await t.snapshot();
        assert(!s.questionPaused && s.answers.length===n+1,'Keyboard focus/navigation failed to submit');
      }else s=await click(t,s,s.submitCenter,t.target.touch?1:2);
      assert(!s.questionPaused && s.answers.length===n+1 && s.damage===damage+10);
      assert(s.answers[n]===chosen,'Submit saved a different answer from the selected option');
      assert(s.shotsFired===shots && s.hitsApplied===hits && dist(s.position,position)<.001 && !s.hasDestination,
        'Submission leaked into gameplay input');
      assert(!s.victory && s.exitUnlocked && !s.onBreak,'Submit must unlock exit and return to gameplay');
      // Repeat taps at the removed Submit screen and keyboard spam cannot
      // duplicate answers/rewards, fire attacks, or restore an old destination.
      // Keyboard submission leaves the mouse over an option. Moving it to the
      // former Submit location would be fresh gameplay movement, not a stale gesture.
      if(!keyboard) s=await click(t,s,submitPoint,2);
      await t.page.keyboard.press('Enter');await t.page.keyboard.press('Enter');s=await t.snapshot();
      assert(!s.questionPaused && s.answers.length===n+1 && s.damage===damage+10);
      assert(s.shotsFired===shots && s.hitsApplied===hits && dist(s.position,position)<.001 && !s.hasDestination);
      await t.page.waitForTimeout(650);s=await t.snapshot();
      assert(s.shotsFired===shots && dist(s.position,position)<.001 && !s.hasDestination);
      passed('Submit grants exactly one reward and resumes automatically without attack or stale movement', {wave:s.wave,touch:t.target.touch});
      s=await traverse(t,s);
      if(n===2){s=await poll(t,x=>x.victory && x.victoryVisible);assert(s.answers.length===3 && s.wave===3 && s.enemyCount===0);
        await t.page.screenshot({path:path.join(artifacts,`flow-${t.target.touch?(t.target.phone?'phone':'tablet'):'desktop'}-victory.png`)});
        passed('Crossing final exit shows Victory with all three answers and rewards');
      }else {
        if(!t.target.touch&&!combatFocusChecked){
          const cdp=await t.context.newCDPSession(t.page);await cdp.send('Emulation.setFocusEmulationEnabled',{enabled:false});
          const other=await t.context.newPage();await other.goto('about:blank');await other.bringToFront();await t.page.waitForTimeout(350);
          const hidden=await t.snapshot();assert(!hidden.focused);await t.page.waitForTimeout(1000);const later=await t.snapshot();
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
    if(process.env.DRFORMS_CAPTURE_PHONE_QUESTION_ONLY==='1'){
      for(const [label,width,height] of [['portrait',390,844],['landscape',844,390]]){
        await d.page.setViewportSize({width,height});await d.page.waitForTimeout(350);s=await d.snapshot();
        assert(s.questionPaused&&s.answers.length===0&&s.selectedAnswer===-1);
        await d.page.screenshot({path:path.join(artifacts,`manuscript-question-${label}.png`)});
      }
      passed('Actual normal question panel captured at both phone resolutions without changing its transaction');
      await d.context.close();return;
    }
    if(process.env.DRFORMS_CAPTURE_COMBAT_ONLY==='1'){
      s=await answer(d,s);s=await clearWave(d);
      assert(s.questionPaused&&s.wave===2);const video=d.page.video();await d.context.close();if(video)await video.saveAs(path.join(artifacts,'combat-compositor.webm'));return;
    }
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
    const prior=s.answers;
    s=await safariPause(d);s=await recoverSafari(d,s,s.restartCenter,0);
    assert(s.defeated&&s.defeatVisible&&s.playerHealth===0&&!s.canPlay);
    await clearSafariObservation(d);passed('Safari focus recovery on Retry preserves defeat pause and cannot restart with the recovery gesture');
    await d.page.waitForTimeout(550);s=await click(d,s,s.restartCenter);
    s=await poll(d,x=>x.wave===2&&!x.defeated);
    assert.deepStrictEqual(s.answers,prior);assert(s.damage===35 && s.playerHealth===100 && s.enemyCount===3);
    assert(s.projectiles===0 && s.effects<=24 && s.shotsFired===0 && !s.questionPaused && !s.hasDestination);
    assert(s.room===1&&Math.abs(s.position.z-17)<.15&&Math.abs(s.position.x)<.15);
    passed('Retry restores only unfinished wave, health/transients, retaining prior answer and reward');
    s=await poll(d,x=>x.defeated&&x.defeatVisible,45000);s=await click(d,s,s.restartCenter);
    s=await poll(d,x=>x.wave===2&&!x.defeated);assert(s.answers.length===1&&s.damage===35&&s.playerHealth===100&&s.enemyCount===3);
    passed('Repeated defeat/retry preserves progress without duplicating rewards');
    for(let i=0;i<2;i++) {s=await clearWave(d,i===0);s=await answer(d,s);}
    const win=s;s=await freeze(d,s);assert(s.answers.length===3);
    s=await safariPause(d);s=await recoverSafari(d,s,s.restartCenter,0);
    assert(s.victory&&s.victoryVisible&&!s.canPlay);
    await clearSafariObservation(d);passed('Safari focus recovery on New Run preserves Victory pause and cannot reset progress');
    await d.page.waitForTimeout(550);s=await click(d,s,s.restartCenter);s=await poll(d,x=>x.wave===1&&!x.victory);
    assert(s.answers.length===0 && s.damage===25 && s.playerHealth===100 && s.enemyCount===2 && s.shotsFired===0);
    passed('New Run clears every answer, reward and wave progress');
    // Complete another run in the same browser to catch static-session leakage.
    for(let i=0;i<3;i++){s=await clearWave(d,i===0);s=await answer(d,s);}
    await d.context.close();
    for(const phone of [false,true]){
      const t=await open(true,phone);t.cdp=await t.context.newCDPSession(t.page);
      s=await poll(t,x=>x.wave===1);
      if(!phone){
        const center=await t.client(s,s.joystickCenter);
        const up=await t.client(s,{x:s.joystickCenter.x,y:s.joystickCenter.y+s.joystickRadius*.95});
        await t.cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{...center,id:1}]});
        await t.cdp.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{...up,id:1}]});
        await t.page.waitForTimeout(2800);s=await t.snapshot();
        await t.cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});
        assert(s.room===0 && s.wave===1 && !s.exitUnlocked && s.doorOpening===0 && s.position.z<9.4 && s.position.z>8);
        assert(s.encounterStarts===1 && s.answers.length===0);
        await t.page.screenshot({path:path.join(artifacts,'route-locked-door.png')});
        passed('Physical movement into locked exit is blocked; next encounter remains inactive');
      }
      if(phone){
        s=await safariPause(t);
        passed('Exact Safari deadlock: visible DOM focus 0, Unity/bridge/gate 1/0/0, scale 0, releasewait 1 with no held touches');
        const before=s.shotsFired;
        await t.page.evaluate(()=>{Object.defineProperty(document,'hidden',{configurable:true,get:()=>true});document.dispatchEvent(new Event('visibilitychange'));});
        s=await click(t,s,s.attackCenter);assert(!s.bridgeFocus&&!s.focused&&s.timeScale===0&&s.waitingForRelease&&s.shotsFired===before);
        await t.page.evaluate(()=>{delete document.hidden;document.dispatchEvent(new Event('visibilitychange'));});
        s=await poll(t,x=>!x.bridgeFocus&&x.timeScale===0&&!x.pointerPressed);
        passed('Background touches cannot recover focus or fire and becoming visible with DOM focus false does not resume');
        s=await recoverSafari(t,s,s.attackCenter,1);assert(s.canPlay&&!s.hasDestination);
        passed('Trusted visible Attack recovery is consumed until release; saved combat clock resumes despite DOM focus staying false');
        await clearSafariObservation(t);
        // Random spawns may still be outside flask range at recovery; an out-of-range
        // gesture is correctly rejected. Wait for actual pursuit before this attack proof.
        s=await poll(t,x=>x.canPlay&&x.enemyAlive&&dist(x.enemyPosition,x.position)<x.range-.1);
        s=await click(t,s,s.attackCenter);s=await poll(t,x=>x.shotsFired===before+1&&!x.throwPending);assert(s.shotsFired===before+1);
        passed('A fresh Attack after recovery fires normally exactly once');
      }
      s=await clearWave(t);
      if(phone){
        s=await safariPause(t);s=await recoverSafari(t,s,s.answerCenters[0],0);
        assert(s.questionPaused&&!s.canPlay&&s.selectedAnswer===-1&&s.answers.length===0);
        await clearSafariObservation(t);s=await poll(t,x=>x.questionInputReady);
        s=await freeze(t,s);passed('Safari question recovery preserves paused clock and rejects the recovery selection; deliberate guarded answers still work');
      }
      s=await answer(t,s);
      // Rotate an emulated portrait phone into landscape during Question 2.
      s=await clearWave(t);
      if(phone){await t.page.setViewportSize({width:844,height:390});await t.page.waitForTimeout(500);s=await t.snapshot();}
      s=await answer(t,s);s=await clearWave(t);s=await answer(t,s);
      passed('Complete emulated touch run and responsive guarded Submit controls', {phone});
      await t.context.close();
    }
    // Measured Unity frames across consecutive active-combat snapshots. Software
    // rendering + automation overhead; never an iPhone performance claim.
    result.performance = result.sessions.map(target=>{
      const windows=[];
      for(let i=1;i<target.states.length;i++) {
        const a=target.states[i-1],b=target.states[i],dt=b.realtime-a.realtime;
        if(a.canPlay&&b.canPlay&&a.wave===b.wave&&!a.onBreak&&!b.onBreak&&dt>=.08&&dt<=1.5&&b.frame>a.frame)
          windows.push((b.frame-a.frame)/dt);
      }
      windows.sort((a,b)=>a-b);
      return {touch:target.touch,phone:target.phone,samples:windows.length,
        medianUnityFps:windows[Math.floor(windows.length/2)],p10UnityFps:windows[Math.floor(windows.length*.1)]};
    });
    assert(result.sessions.some(t=>t.states.some(s=>s.telegraphCount>0 && s.rangedCount===1)),'No live mage telegraph observed');
    assert(result.sessions.some(t=>t.states.some(s=>s.rangedShots.some(n=>n>0))),'No live ranged attack observed');
    passed('Gradual single ranged enemy, real windup and ranged attacks observed across completed runs');
    result.environment = {browser:browser.version(),renderer:'Windows installed Chrome headless / SwiftShader; deviceScaleFactor 1; automated mouse and emulated touch'};
    for(const target of result.sessions)assert(!target.events.some(e=>['error','pageerror','requestfailed','http-error'].includes(e.type)),JSON.stringify(target.events));
    passed('Full viewport ground coverage holds for every portrait, landscape and camera-follow snapshot');
    passed('No browser errors, page exceptions, or failed requests');
  }catch(error){result.error=error.stack;console.error(error);process.exitCode=1;}
  finally{fs.writeFileSync(path.join(artifacts,'reliable-flow-browser-result.json'),JSON.stringify(result,null,2));if(browser)await browser.close();server.close();}
})();
