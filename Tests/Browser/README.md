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
$env:DRFORMS_FLOW_BUILD_PATH = 'C:\Users\21654\DrFormsGame-WebVerification\DrFormsLabFinalBuild'
node Tests/Browser/reliable-flow.cjs
```

Without a build-path override, the external root must contain
`GamePrototypeBuild/index.html`. Results are
written to `reliable-flow-browser-result.json`; `flow-*.png` screenshots capture
questions and Victory. Failure exits nonzero and preserves session
observations. The suite covers held pointer/key transitions, actual tab focus,
selection versus submission, equal exactly-once rewards, frozen screens,
current-room retry, multiple complete runs/New Run, emulated tablet and
portrait/landscape phone touch controls. Submit saves the answer, grants one
reward, and opens the current exit. Real mouse/touch movement crosses the passage
to start the next room once; crossing the final exit triggers Victory. Repeated
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
$env:DRFORMS_PREVIEW_ROOT = 'C:\Users\21654\DrFormsGame-WebVerification\DrFormsLabFinalBuild'
node Tests/Browser/wifi-preview.cjs
```

This binds to `0.0.0.0:8765` and shares the responsive browser shell with the
regression suite. CSS safe-area insets pad the actual canvas; its rendering
pixel ratio is capped at 1.5. Use `?devDiagnostics=1` only for developer
troubleshooting. Both the panel and Unity reporter require that explicit URL;
normal gameplay has no phone diagnostic overlay. The menu shows a small
`DRFORMS-LAB-1` identifier. Physical notch behavior still requires a device.

Controls: Play starts a run and activates procedural browser audio. Desktop
mouse movement follows the garden floor; click throws a flask at the nearest target.
On touch devices use the left joystick and independent right Attack taps.
Pause opens Resume and Restart / New Run; Restart resets answers/upgrades.
Sound toggles persist in browser local storage (run progress remains in memory).
Defeat Retry preserves earlier answers/upgrades and restarts only the current
room. Victory New Run resets everything. Selecting an answer never submits it;
Submit Answer grants one equal reward and resumes gameplay with an animated exit. A lost-focus
Safari recovery touch is consumed first; release, then use a fresh gesture.
A short visible shield protects Play/Resume/question/focus recovery.

Supplementary actual-browser UI/audio/layout checks:

```powershell
$env:DRFORMS_FLOW_BUILD_PATH = 'C:\Users\21654\DrFormsGame-WebVerification\DrFormsLabFinalBuild'
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


Visual presentation boundary checks:

```powershell
node Tests/Browser/visual-presentation.cjs
```

Use the same external build/artifact/Playwright variables above. This suite uses
loopback 8768 and actual emulated touch movement against all four walls and two
opposite corners in portrait and landscape. It walks around the preserved
practice totem for the north-wall check. It captures combat, touch controls,
impacts and boundary screenshots for manual visual inspection. No gameplay
state is injected; fresh runs use the existing Pause / Restart UI. The full
flow suite projects all four viewport corners onto the ground throughout each
run to check that the camera never reveals the extended floor's edges. Its
mouse projection uses the actual camera field of view from read-only snapshots.

Dungeon route checks use `reliable-flow.cjs` and `polish-ui.cjs`. The flow suite
covers actual locked-door collision, room entry, no advancement while waiting,
exactly-once encounter starts, final-exit Victory, room Retry/New Run, input
ownership, question guards, focus recovery and viewport ground coverage in
portrait and landscape. The former `visual-presentation.cjs` arena boundary
sweep targets the historical single arena and is not a dungeon-route check.


Courtyard art checks use `courtyard-visual.cjs` (loopback 8768). They record actual canvas gameplay as WebM and capture
390 x 844 portrait and 844 x 390 landscape renders, exercise real touch movement
and throws, pause during wind-up, simultaneous joystick/throw touches and release delay. They observe imported skeletal clips, the 1024 shadow budget and
existing effect caps through read-only snapshots. Screenshots and JSON stay
outside Git. See `docs/ART_SOURCES.md` for pinned CC0 sources, licences and
reproducible original environment/material authoring. Later rooms remain simpler.
The flow runner uses visible nearby mouse waypoints with the closer art camera;
guard assertions distinguish genuinely blocked gestures from fresh gestures
delivered after the configured guard has expired.

Lab recordings are `lab-{portrait,landscape}-gameplay.webm` outside Git. Review decoded consecutive frames from these recordings for alternating steps, independent throwing, hand release and follow-through; an advancing clip name alone does not establish visible animation. Release timing uses scaled time; pending releases cancel on gameplay input suspension. Decorative chemical splashes do not query targets or apply area damage.

For **DRFORMS-MANUSCRIPT-2**, use external `ManuscriptReviewBuild` and
`ManuscriptEvidence/Review` for the build/artifact variables. The motion suite also
checks a smaller analog joystick displacement for walking, a stop transition,
and a moving leg clip during the throw overlay. Its pause test starts Attack
and Pause fingers together, then releases Pause, to reduce protocol latency
inside the short release window. An accepted cancelled attack is required;
a pause arriving after release is a test failure, not cancellation evidence.
The full-flow suite records room-two canvas combat as
`manuscript-both-enemies.webm`, using actual controls and read-only observations.
These canvas-stream clips can omit WebGL frames in headless Chrome; delivery
uses browser compositor video instead. Set `DRFORMS_RECORD_VIDEO=1` to capture
`lab-{portrait,landscape}-compositor.webm`. For a short actual-control room-two
demonstration, use `DRFORMS_CAPTURE_COMBAT_ONLY=1` with a separate artifact root
and `DRFORMS_FLOW_PORT=8770`; it kites without attacking until the first ranged
preparation/release/recovery has been observed, then clears the encounter.
`DRFORMS_CAPTURE_PHONE_QUESTION_ONLY=1` captures an ordinary paused question at
390x844 and 844x390 and ends the supplementary run. These modes do not replace
the full-flow suite. Recordings are silent. Long-question fixtures, touch/keyboard selections,
rapid-click input guards, Retry/final victory and simulated Safari focus remain
in the existing full-flow suite. Actual Safari and physical-device results must
be reported separately.

This workstation's external Playwright expects an unavailable encoder revision.
Delivery reused installed FFmpeg 8.1.1 through the external
`ManuscriptEvidence/existing-encoder.cjs` preload (no package/tool upgrades):
`node -r C:/Users/21654/DrFormsGame-WebVerification/ManuscriptEvidence/existing-encoder.cjs Tests/Browser/courtyard-visual.cjs`.
The final 15.64-second H.264 review excerpt is `ManuscriptEvidence/combat-review.mp4`.
The LAN review page supplies normal/quarter-speed playback, the actual panels,
and the redistributed font notice. The preview server supports video MIME types
and byte-range seeking. `GamePrototypeBuild.Build` also packages the OFL font
notice beside future player outputs.

The motion suite uses the default room-one chaser speed (1.8 units/sec) and read-only initial spawn observations to begin moving away before contact. Run it separately from other software-rendered browser suites when verifying the short pending-throw interruption window.
