// Static preview handler adapted from Tests/Browser/reliable-flow.cjs.
// Temporary phone diagnostic script is injected before Unity's loader.
const http = require('http'), fs = require('fs'), path = require('path');
if (!process.env.DRFORMS_PREVIEW_ROOT || !path.isAbsolute(process.env.DRFORMS_PREVIEW_ROOT))
  throw new Error('Set DRFORMS_PREVIEW_ROOT to an absolute external Web build directory');
const root = path.resolve(process.env.DRFORMS_PREVIEW_ROOT);
const port = Number(process.env.DRFORMS_PREVIEW_PORT || 8765);
const mime = { '.html': 'text/html; charset=utf-8', '.js': 'application/javascript',
  '.wasm': 'application/wasm', '.data': 'application/octet-stream',
  '.json': 'application/json', '.css': 'text/css', '.png': 'image/png',
  '.svg': 'image/svg+xml', '.ico': 'image/x-icon' };
const server = http.createServer((req, res) => {
  if (req.method !== 'GET' && req.method !== 'HEAD') { res.writeHead(405); return res.end(); }
  let name;
  try { name = decodeURIComponent(new URL(req.url, 'http://localhost').pathname); }
  catch { res.writeHead(400); return res.end(); }
  res.setHeader('Cache-Control', 'no-store');
  if (name === '/phone-diagnostics.js') {
    const script = fs.readFileSync(path.join(__dirname, 'phone-diagnostics.js'));
    res.setHeader('Content-Type', 'application/javascript');
    res.setHeader('Content-Length', script.length);
    return res.end(req.method === 'HEAD' ? undefined : script);
  }
  const file = path.resolve(root, '.' + (name === '/' ? '/index.html' : name));
  if (!file.startsWith(root + path.sep)) { res.writeHead(403); return res.end(); }
  let stat;
  try { stat = fs.statSync(file); } catch { res.writeHead(404); return res.end(); }
  if (!stat.isFile()) { res.writeHead(404); return res.end(); }
  if (file === path.join(root, 'index.html')) {
    const html = fs.readFileSync(file, 'utf8').replace('<head>', '<head><script src="/phone-diagnostics.js?v=PHONE-DIAG-2"></script>');
    res.setHeader('Content-Type', 'text/html; charset=utf-8');
    res.setHeader('Content-Length', Buffer.byteLength(html));
    return res.end(req.method === 'HEAD' ? undefined : html);
  }
  // .gz/.br use HTTP decompression. Unity .unityweb fallback uses its loader's
  // own decompressor, so it must not receive a Content-Encoding header.
  const plain = file.replace(/\.(gz|br|unityweb)$/, '');
  res.setHeader('Content-Type', mime[path.extname(plain)] || 'application/octet-stream');
  if (file.endsWith('.gz')) res.setHeader('Content-Encoding', 'gzip');
  if (file.endsWith('.br')) res.setHeader('Content-Encoding', 'br');
  res.setHeader('Content-Length', stat.size);
  res.setHeader('Cache-Control', 'no-store');
  if (req.method === 'HEAD') return res.end();
  if (req.method !== 'GET') { res.writeHead(405); return res.end(); }
  const stream = fs.createReadStream(file);
  stream.on('error', () => res.destroy());
  stream.pipe(res);
});
server.on('error', error => { console.error(error); process.exitCode = 1; });
server.listen(port, '0.0.0.0', () => console.log(`Serving ${root} on 0.0.0.0:${port}`));
