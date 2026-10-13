// Exercise the production jslib's lifecycle listeners without Unity/browser
// dependencies. Browser integration tests separately verify clock/input gates.
const assert = require('assert'), fs = require('fs'), path = require('path'), vm = require('vm');
const source = fs.readFileSync(path.resolve(__dirname, '../../Assets/Plugins/WebGL/PrototypeControls.jslib'), 'utf8');
function fixture(focused=false,hidden=false) {
  const target=()=>({listeners:{},addEventListener(type,listener){(this.listeners[type]||=[]).push(listener);},
    send(type,event={}){for(const listener of this.listeners[type]||[])listener(event);}});
  const canvas=Object.assign(target(),{style:{},matches:()=>false}),document=Object.assign(target(),{hidden,hasFocus:()=>focused});
  const window=Object.assign(target(),{matchMedia:()=>({matches:true})}),library={};
  const context={Module:{canvas},document,window,navigator:{maxTouchPoints:5},LibraryManager:{library},mergeInto:Object.assign};
  vm.createContext(context);vm.runInContext(source,context);
  context.PrototypeCanvasInput={...library.$PrototypeCanvasInput};library.PrototypeControls_Init();
  return {canvas,document,window,state:context.PrototypeCanvasInput};
}
let checks=0;const test=(name,fn)=>{fn();checks++;console.log('PASS: '+name);};
test('Visible Safari DOM focus false seeds a paused bridge',()=>{const f=fixture();assert.strictEqual(f.state.focused,false);});
test('Trusted visible canvas touch restores bridge and invalidates the recovery gesture',()=>{const f=fixture();f.canvas.send('touchstart',{isTrusted:true});assert(f.state.focused);assert.strictEqual(f.state.epoch,1);});
test('Synthetic touch/pointer cannot recover focus',()=>{const f=fixture();f.canvas.send('touchstart',{isTrusted:false});f.canvas.send('pointerdown',{isTrusted:false,pointerType:'touch',button:0});assert(!f.state.focused);assert.strictEqual(f.state.epoch,0);});
test('Movement, pointer enter and release cannot recover focus',()=>{const f=fixture();for(const type of ['pointermove','pointerenter','touchmove','touchend'])f.canvas.send(type,{isTrusted:true});assert(!f.state.focused);});
test('A touch outside the game cannot recover the bridge',()=>{const f=fixture();f.document.send('touchstart',{isTrusted:true});assert(!f.state.focused);});
test('Pointer/touch down pair increments epoch once, later touches do not repeatedly cancel',()=>{const f=fixture();f.canvas.send('pointerdown',{isTrusted:true,pointerType:'touch',button:0});f.canvas.send('touchstart',{isTrusted:true});assert(f.state.focused);assert.strictEqual(f.state.epoch,1);for(let i=0;i<35;i++)f.canvas.send('touchstart',{isTrusted:true});assert.strictEqual(f.state.epoch,1);});
test('Hidden touches and focus events cannot resume background gameplay',()=>{const f=fixture(false,true);f.canvas.send('touchstart',{isTrusted:true});f.canvas.send('pointerdown',{isTrusted:true,pointerType:'touch',button:0});f.window.send('focus');assert(!f.state.focused);assert.strictEqual(f.state.epoch,0);});
test('Hiding a recovered game revokes focus; visibility alone with DOM focus false does not recover',()=>{const f=fixture();f.canvas.send('touchstart',{isTrusted:true});f.document.hidden=true;f.document.send('visibilitychange');assert(!f.state.focused);const epoch=f.state.epoch;f.document.hidden=false;f.document.send('visibilitychange');assert(!f.state.focused);f.canvas.send('touchstart',{isTrusted:true});assert(f.state.focused);assert(f.state.epoch>epoch);});
test('Blur cancels recovered input; a new visible touch can recover again',()=>{const f=fixture();f.canvas.send('touchstart',{isTrusted:true});f.window.send('blur');assert(!f.state.focused);assert.strictEqual(f.state.epoch,2);f.canvas.send('touchstart',{isTrusted:true});assert(f.state.focused);assert.strictEqual(f.state.epoch,3);});
test('A right mouse button cannot recover, but a trusted left down can',()=>{const f=fixture();f.canvas.send('pointerdown',{isTrusted:true,pointerType:'mouse',button:2});assert(!f.state.focused);f.canvas.send('pointerdown',{isTrusted:true,pointerType:'mouse',button:0});assert(f.state.focused);});
console.log(`${checks} focus bridge checks passed`);
