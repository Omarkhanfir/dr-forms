# Download only pinned CC0 source files into an external authoring cache.
import hashlib,json,sys,urllib.request
from pathlib import Path
repo=Path(__file__).resolve().parents[2];out=Path(sys.argv[1]).resolve()
if out==repo or repo in out.parents:raise SystemExit('Source download cache must stay outside the repository')
out.mkdir(parents=True,exist_ok=True)
for item in json.loads((repo/'docs/licenses/asset-downloads.json').read_text()):
 target=out/item['file']
 if not target.exists():target.write_bytes(urllib.request.urlopen(item['source'],timeout=90).read())
 if hashlib.sha256(target.read_bytes()).hexdigest()!=item['sha256']:raise SystemExit('Source hash mismatch: '+str(target))
 print('Verified',item['file'])
