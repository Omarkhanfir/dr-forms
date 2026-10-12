mergeInto(LibraryManager.library, {
  $PrototypeCanvasInput: { canvas: null, inside: false, focused: true, epoch: 0 },
  PrototypeControls_Init__deps: ['$PrototypeCanvasInput'],
  PrototypeControls_Init: function () {
    var state = PrototypeCanvasInput;
    if (state.canvas) return;
    var canvas = Module['canvas'];
    state.canvas = canvas;
    state.focused = document.hasFocus() && !document.hidden;
    state.inside = canvas.matches(':hover');
    var cancel = function () { state.inside = false; state.epoch++; };
    canvas.style.touchAction = 'none';
    canvas.addEventListener('pointerenter', function () { state.inside = true; });
    canvas.addEventListener('pointermove', function () { state.inside = true; });
    canvas.addEventListener('pointerleave', function (event) {
      if (event.pointerType !== 'touch') cancel();
    });
    canvas.addEventListener('pointercancel', cancel);
    window.addEventListener('blur', function () { state.focused = false; cancel(); });
    window.addEventListener('focus', function () { state.focused = !document.hidden; });
    document.addEventListener('visibilitychange', function () {
      state.focused = !document.hidden && document.hasFocus();
      if (!state.focused) cancel();
    });
  },
  PrototypeControls_Focused__deps: ['$PrototypeCanvasInput'],
  PrototypeControls_Focused: function () { return PrototypeCanvasInput.focused ? 1 : 0; },
  PrototypeControls_Inside__deps: ['$PrototypeCanvasInput'],
  PrototypeControls_Inside: function () { return PrototypeCanvasInput.inside ? 1 : 0; },
  PrototypeControls_Epoch__deps: ['$PrototypeCanvasInput'],
  PrototypeControls_Epoch: function () { return PrototypeCanvasInput.epoch; },
  PrototypeControls_Touch: function () {
    return navigator.maxTouchPoints > 0 && window.matchMedia('(pointer: coarse)').matches ? 1 : 0;
  }
});
