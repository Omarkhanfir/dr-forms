// Temporary LAN diagnostics. Observe events only: never preventDefault, focus,
// forward touches, or alter Unity gameplay/input state.
(() => {
  const d = window.DrFormsDiagnostics = { version: 'PHONE-DIAG-2', unity: null,
    unityAt: 0, browserFrames: 0, errors: [], errorCount: 0, touches: 0,
    touchStarts: 0, touchMoves: 0, touchEnds: 0, touchCancels: 0, pointers: 0,
    pointerDowns: 0, pointerMoves: 0, pointerUps: 0, pointerCancels: 0,
    lastTarget: 'none', lastTouch: 'none', started: performance.now() };
  d.receiveUnity = state => { d.unity = state; d.unityAt = performance.now(); };
  const describe = value => {
    try { return value instanceof Error ? value.stack || value.message : typeof value === 'string' ? value : JSON.stringify(value); }
    catch { return String(value); }
  };
  d.captureError = (source, value) => {
    d.errorCount++; d.errors.push(source + ': ' + describe(value));
    if (d.errors.length > 3) d.errors.shift();
  };
  window.addEventListener('error', e => {
    const target = e.target;
    d.captureError('browser', e.message || ('resource failed: ' + (target && (target.src || target.href) || 'unknown')));
  }, true);
  window.addEventListener('unhandledrejection', e => d.captureError('promise', e.reason));
  const originalError = console.error;
  console.error = function (...args) {
    d.captureError('console', args.map(describe).join(' '));
    return originalError.apply(console, args);
  };
  const originalAlert = window.alert;
  window.alert = function (message) { d.captureError('alert', message); return originalAlert.call(window, message); };
  for (const [type, counter] of [['touchstart','touchStarts'],['touchmove','touchMoves'],['touchend','touchEnds'],['touchcancel','touchCancels']]) {
    document.addEventListener(type, e => {
      d[counter]++; d.touches = e.touches.length;
      d.lastTarget = e.target.id || e.target.tagName || 'unknown';
      const touch = e.changedTouches[0];
      d.lastTouch = type + (touch ? ' @' + Math.round(touch.clientX) + ',' + Math.round(touch.clientY) : '');
    }, { capture: true, passive: true });
  }
  const activePointers = new Set();
  for (const [type,counter] of [['pointerdown','pointerDowns'],['pointermove','pointerMoves'],['pointerup','pointerUps'],['pointercancel','pointerCancels']]) {
    document.addEventListener(type, e => {
      d[counter]++;
      if(type==='pointerdown') activePointers.add(e.pointerId);
      if(type==='pointerup'||type==='pointercancel') activePointers.delete(e.pointerId);
      d.pointers=activePointers.size;
    }, {capture:true,passive:true});
  }
  const flag = value => value ? '1' : '0';
  const number = value => Number(value || 0).toFixed(2);
  const render = () => {
    let panel = document.getElementById('phone-diagnostics');
    if (!panel && document.body) {
      panel = document.createElement('pre'); panel.id = 'phone-diagnostics';
      panel.setAttribute('aria-label', 'Temporary phone diagnostics');
      panel.style.cssText = 'position:fixed;top:env(safe-area-inset-top,0px);left:0;z-index:2147483647;margin:0;padding:5px;box-sizing:border-box;max-width:100vw;width:390px;max-height:65vh;overflow:hidden;white-space:pre-wrap;overflow-wrap:anywhere;background:rgba(0,0,0,.88);color:#b8ffc4;font:10px/1.25 monospace;pointer-events:none;';
      document.body.appendChild(panel);
    }
    if (!panel) return;
    const s=d.unity;
    const lines=[d.version+' Build '+(s?s.build:'waiting')+' / TEMP',
      'Browser beat '+d.browserFrames+' | Unity '+(s?s.frame:'WAITING')+' age '+(s?number((performance.now()-d.unityAt)/1000):'--')+'s',
      'Errors browser/Unity '+d.errorCount+'/'+(s?s.unityErrorCount:'--')];
    const errors=[...d.errors,...(s?s.unityErrors:[])];
    if(errors.length)lines.push(errors.slice(-2).map(e=>e.slice(0,220)).join('\n'));
    lines.push(
      'DOM focus '+flag(document.hasFocus())+' hidden '+flag(document.hidden)+' touchCap '+navigator.maxTouchPoints,
      'DOM touch active '+d.touches+' S/M/E/C '+[d.touchStarts,d.touchMoves,d.touchEnds,d.touchCancels].join('/'),
      'DOM ptr active '+d.pointers+' D/M/U/C '+[d.pointerDowns,d.pointerMoves,d.pointerUps,d.pointerCancels].join('/'),
      'Last '+d.lastTouch+' target '+d.lastTarget);
    if(s) lines.push(
      'State '+s.state+' wave '+s.wave+' HP '+s.health+' scale '+number(s.timeScale)+' time '+number(s.gameTime),
      'Focus Unity/bridge/gate '+[s.applicationFocus,s.bridgeFocus,s.focus].map(flag).join('/')+' epoch '+s.epoch,
      'Play '+flag(s.canPlay)+' enabled '+flag(s.gameplayEnabled)+' ctrl '+flag(s.controlsEnabled)+' releaseWait '+flag(s.waitingForRelease),
      'Overlays question/end '+flag(s.questionOverlay)+'/'+flag(s.endOverlay)+' selected '+s.selectedUI,
      'Unity touch dev/mode '+flag(s.unityTouchDevice)+'/'+flag(s.touchMode)+' active '+s.unityTouches+' starts '+s.unityTouchStarts+' peak '+s.peakUnityTouches+' held '+flag(s.pointerPressed),
      'UI events '+flag(s.eventSystem)+' '+s.uiModule+' hit '+s.lastTouchHit,
      'Joy '+number(s.joystick.x)+','+number(s.joystick.y)+' shots '+s.shots+' pos '+number(s.position.x)+','+number(s.position.z));
    if(s&&!s.bridgeFocus&&!document.hidden)lines.push('Tap game to recover; release, then use fresh input.');
    panel.textContent=lines.join('\n');
  };
  const beat=()=>{d.browserFrames++;requestAnimationFrame(beat);};
  requestAnimationFrame(beat);setInterval(render,250);render();
})();
