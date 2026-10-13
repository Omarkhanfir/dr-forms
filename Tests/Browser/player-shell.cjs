// Shared player shell used by both the LAN preview and browser regressions.
function playerShell(html) {
  html = html.replace("<head>", '<head><meta name="viewport" content="width=device-width, initial-scale=1.0, viewport-fit=cover">');
  html = html.replace(/meta.content = '[^']*';/, "meta.content = 'width=device-width, initial-scale=1.0, viewport-fit=cover';");
  html = html.replace('arguments: [],', 'arguments: [], devicePixelRatio: Math.min(window.devicePixelRatio || 1, 1.5),');
  html = html.replace(/<meta name="viewport"[^>]*>/, '<meta name="viewport" content="width=device-width, initial-scale=1.0, viewport-fit=cover">');
  return html.replace('</head>', `<style>
html,body{margin:0;width:100%;height:100%;overflow:hidden;background:#102e32}
#unity-container{position:fixed!important;inset:0!important;width:100%!important;height:100%!important;transform:none!important;box-sizing:border-box;padding:env(safe-area-inset-top) env(safe-area-inset-right) env(safe-area-inset-bottom) env(safe-area-inset-left)}
#unity-fullscreen-container{width:100%!important;height:100%!important}
#unity-canvas{width:100%!important;height:100%!important;display:block;touch-action:none}
#unity-footer{display:none}
</style></head>`);
}
module.exports = {playerShell};
