mergeInto(LibraryManager.library, {
  PrototypeAudio_Init: function() {
    if (window.drFormsAudio) return;
    var a = window.drFormsAudio = {context:null, muted:false, voices:0};
    try { a.muted = localStorage.getItem('GardenQuestMute') === '1'; } catch (_) {}
    function unlock(e) {
      if (!e.isTrusted || document.hidden || !Module.canvas.contains(e.target)) return;
      try {
        if (!a.context) a.context = new (window.AudioContext || window.webkitAudioContext)();
        if (a.context.state === 'suspended') a.context.resume().catch(function(){});
      } catch (_) {}
    }
    document.addEventListener('pointerdown', unlock, true);
    document.addEventListener('touchstart', unlock, {capture:true, passive:true});
  },
  PrototypeAudio_Muted: function() { return window.drFormsAudio && window.drFormsAudio.muted ? 1 : 0; },
  PrototypeAudio_Toggle: function() {
    var a = window.drFormsAudio; if (!a) return;
    a.muted = !a.muted;
    try { localStorage.setItem('GardenQuestMute', a.muted ? '1' : '0'); } catch (_) {}
  },
  PrototypeAudio_Play: function(kind) {
    var a = window.drFormsAudio;
    if (!a || a.muted || !a.context || a.context.state !== 'running' || a.voices >= 8) return;
    var c = a.context, now = c.currentTime;
    var notes = kind === 3 ? [523,659,784,1047] : kind === 2 ? [523,784] : kind === 1 ? [220] : [660];
    notes.forEach(function(f, i) {
      var o = c.createOscillator(), g = c.createGain(), start = now + i * .09;
      o.type = kind === 1 ? 'triangle' : 'sine'; o.frequency.setValueAtTime(f, start);
      o.frequency.exponentialRampToValueAtTime(kind === 0 ? f * 1.7 : f * .95, start + .12);
      g.gain.setValueAtTime(0, start); g.gain.linearRampToValueAtTime(.035, start + .008);
      g.gain.exponentialRampToValueAtTime(.0001, start + .16);
      o.connect(g); g.connect(c.destination); a.voices++;
      o.onended = function() { o.disconnect(); g.disconnect(); a.voices--; };
      o.start(start); o.stop(start + .18);
    });
  }
});
