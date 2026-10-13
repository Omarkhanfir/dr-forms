# Prototype flow browser regressions

Build the GamePrototype Web development player with `GamePrototypeBuild.Build`.
Keep the output and artifacts outside the repository. The suite serves that
build locally, uses installed Chrome with SwiftShader, and drives actual browser
mouse, keyboard, and CDP multi-touch events. Development snapshots are read-only;
no gameplay mutation, injected damage, teleports, or scripted submission calls
are used.

Use an existing external Playwright installation (no Unity/package changes):

```powershell
$env:DRFORMS_PLAYWRIGHT = 'C:\Users\21654\DrFormsGame-WebVerification\browser-tools\node_modules\playwright'
$env:DRFORMS_FLOW_ARTIFACTS = 'C:\Users\21654\DrFormsGame-WebVerification'
node Tests/Browser/reliable-flow.cjs
```

The external root must contain `GamePrototypeBuild/index.html`. Results are
written to `reliable-flow-browser-result.json`; `flow-*.png` screenshots capture
questions and Victory. Failure exits nonzero and preserves session
observations. The suite covers held pointer/key transitions, actual tab focus,
selection versus submission, equal exactly-once rewards, frozen screens,
unfinished-wave retry, multiple complete runs/New Run, emulated tablet and
portrait/landscape phone touch controls. Submit saves the answer, grants one
reward, and automatically starts the next-wave break or final Victory. Repeated
submission taps/keys must not duplicate progress, fire attacks, or restore an
old movement destination. There is no reward Continue screen.
Physical hardware and Safari are not
covered. Chrome and Playwright paths can be adjusted for another workstation.

The regression server defaults to loopback port 8766 so the Wi-Fi preview on
8765 can stay running. Override `DRFORMS_FLOW_PORT` if necessary and set
`DRFORMS_FLOW_BUILD_PATH` to test another external build directory.

Focus bridge regressions (no browser dependencies):

```powershell
node Tests/Browser/focus-bridge.cjs
```

The full browser suite also reproduces Safari's visible DOM focus 0 / Unity
focus 1 / bridge focus 0 / timeScale 0 / releasewait 1 / held pointers 0.
Only browser focus/visibility observations are overridden; gameplay state is
not injected. Real trusted gestures must recover once, wait for release, and
require fresh input before attacking. Hidden touches must not resume; question,
defeat and Victory pauses must survive recovery, including recovery taps on
Answer/Retry/New Run controls.

For the temporary diagnostic LAN preview:

```powershell
$env:DRFORMS_PREVIEW_ROOT = 'C:\Users\21654\DrFormsGame-WebVerification\SafariFocusBuild'
node Tests/Browser/wifi-preview.cjs
```

This binds to `0.0.0.0:8765`, sets Unity MIME/compression headers and no-store
caching, and injects the pointer-transparent `phone-diagnostics.js` before the
loader. The Unity diagnostic snapshot shows build ID `SAFARI-FOCUS-2`; confirm
that value to distinguish the rebuilt player from a stale page. Set
`DRFORMS_PREVIEW_PORT` to override the port. Allow private networks only if
Windows Firewall prompts. Build outputs/logs/screenshots stay outside Git.
