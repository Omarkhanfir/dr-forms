const fs=require('fs'),vm=require('vm'),assert=require('assert');
let api,created=0,resumes=0,nodes=0;const handlers={},store=new Map();
class Context {
 constructor(){this.state='suspended';this.currentTime=0;this.destination={};created++;}
 resume(){this.state='running';resumes++;return Promise.resolve();}
 createOscillator(){nodes++;return {frequency:{setValueAtTime(){},exponentialRampToValueAtTime(){}},connect(){},disconnect(){},start(){},stop(){}};}
 createGain(){return {gain:{setValueAtTime(){},linearRampToValueAtTime(){},exponentialRampToValueAtTime(){}},connect(){},disconnect(){}};}
}
const sandbox={window:{AudioContext:Context},Module:{canvas:{contains:t=>t==='canvas'}},document:{hidden:false,addEventListener:(name,fn)=>handlers[name]=fn},localStorage:{getItem:k=>store.get(k),setItem:(k,v)=>store.set(k,v)},LibraryManager:{library:{}},mergeInto:(_,value)=>api=value};
vm.runInNewContext(fs.readFileSync('Assets/Plugins/WebGL/PrototypeAudio.jslib','utf8'),sandbox);
let passes=0;const pass=name=>{console.log('PASS: '+name);passes++;};
api.PrototypeAudio_Init();api.PrototypeAudio_Play(0);assert(nodes===0&&created===0);pass('No audio context or sounds before a trusted gesture');
handlers.pointerdown({isTrusted:false,target:'canvas'});handlers.pointerdown({isTrusted:true,target:'elsewhere'});assert(created===0);pass('Synthetic and off-canvas gestures do not unlock');
sandbox.document.hidden=true;handlers.touchstart({isTrusted:true,target:'canvas'});assert(created===0);sandbox.document.hidden=false;pass('Hidden-page touches do not activate audio');
handlers.pointerdown({isTrusted:true,target:'canvas'});api.PrototypeAudio_Play(0);assert(created===1&&resumes===1&&nodes===1);pass('Trusted visible gesture activates procedural sound');
api.PrototypeAudio_Toggle();api.PrototypeAudio_Play(3);assert(api.PrototypeAudio_Muted()===1&&nodes===1&&store.get('GardenQuestMute')==='1');pass('Muted sound creates no tones and persists preference');
api.PrototypeAudio_Toggle();for(let i=0;i<12;i++)api.PrototypeAudio_Play(3);assert(sandbox.window.drFormsAudio.voices===8);pass('Overlapping chords remain bounded at eight voices');
sandbox.window.drFormsAudio.context.state='interrupted';handlers.touchstart({isTrusted:true,target:'canvas'});assert(resumes===2);pass('Safari interrupted context resumes on a fresh trusted gesture');
sandbox.window.drFormsAudio.context.state='closed';handlers.pointerdown({isTrusted:true,target:'canvas'});assert(created===2&&resumes===3);pass('Closed context can be recreated safely');
console.log(`${passes} audio bridge checks passed`);
