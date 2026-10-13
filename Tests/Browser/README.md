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

For the Garden Quest LAN preview (diagnostics off by default):

```powershell
$env:DRFORMS_PREVIEW_ROOT = 'C:\Users\21654\DrFormsGame-WebVerification\GardenFinalPolishBuild'
node Tests/Browser/wifi-preview.cjs
```

This binds to `0.0.0.0:8765` and shares the responsive browser shell with the
regression suite. CSS safe-area insets pad the actual canvas; its rendering
pixel ratio is capped at 1.5. Use `?devDiagnostics=1` only for developer
troubleshooting. Both the panel and Unity reporter require that explicit URL;
normal gameplay has no phone diagnostic overlay. The menu shows a small
`GARDEN-QUEST-3` identifier. Physical notch behavior still requires a device.

Controls: Play starts a run and activates procedural browser audio. Desktop
mouse movement follows the garden floor; click casts at the nearest target.
On touch devices use the left joystick and independent right Attack taps.
Pause opens Resume and Restart / New Run; Restart resets answers/upgrades.
Sound toggles persist in browser local storage (run progress remains in memory).
Defeat Retry preserves earlier answers/upgrades and restarts only the unfinished
wave. Victory New Run resets everything. Selecting an answer never submits it;
Submit Answer grants one equal reward and resumes automatically. A lost-focus
Safari recovery touch is consumed first; release, then use a fresh gesture.
A short visible shield protects Play/Resume/question/focus recovery.

Supplementary actual-browser UI/audio/layout checks:

```powershell
$env:DRFORMS_FLOW_BUILD_PATH = 'C:\Users\21654\DrFormsGame-WebVerification\GardenFinalPolishBuild'
node Tests/Browser/polish-ui.cjs
node Tests/Browser/audio-bridge.cjs
```

The UI suite uses loopback 8767, actual tab switching and real Web Audio context
observations, mute/reload persistence, portrait/landscape and explicitly emulated
safe-area padding. Audio-bridge tests use simulated audio nodes, including
Safari's interrupted state; they do not prove audibility on a physical phone.
The complete flow suite uses the development-only `?longQuestions=1` fixture
for its portrait/landscape phone session and observes live ranged telegraphs,
projectile/effect caps and Unity frame counters. Its performance figures are
headless Chrome/SwiftShader measurements with automation overhead, not iPhone
or hardware GPU performance. Build output, screenshots, logs and results remain
outside Git. Do not serve a directory while Unity is rebuilding it.
