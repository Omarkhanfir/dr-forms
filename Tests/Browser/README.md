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
questions, rewards and Victory. Failure exits nonzero and preserves session
observations. The suite covers held pointer/key transitions, actual tab focus,
selection versus submission, equal exactly-once rewards, frozen screens,
unfinished-wave retry, multiple complete runs/New Run, emulated tablet and
portrait/landscape phone touch controls. Physical hardware and Safari are not
covered. Chrome and Playwright paths can be adjusted for another workstation.
