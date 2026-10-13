# Dr.forms

Dr.forms is a SaaS platform for creating and completing forms, quizzes, homework, and tests. Participants can answer directly or choose an optional Unity 3D action RPG experience.

## Assessment experiences

- **Direct answering:** Participants complete questions through a conventional form or assessment interface.
- **Optional game:** Participants complete the assessment through a Unity 3D action RPG, with questions presented at intervals configured by the creator.

Creators configure question intervals and the rules governing upgrades in the game experience.

The current local prototype has three connected dungeon rooms along a forward
route. Each room runs one easy encounter (2/3/4 enemies), then presents its
matching question. Submit saves the answer, grants its reward once, and returns
directly to gameplay while the room's exit animates open. The player must move
through the doorway and passage to start the next room's encounter. The final
answer opens the final exit; crossing it triggers Victory. Locked exits block
movement, and future rooms have no active enemies. There is no automatic wave
break, Continue button, reward screen or timed mid-combat question trigger.
This prototype pacing does not restrict future creator-configured intervals.
The prototype validates three encounters and three questions, each with three
options. Question content, enemy counts and rewards remain independent.

Questions freeze gameplay and require released pointers/submit keys followed by
a configurable input guard (default 0.5 seconds), a fresh option selection, and
a separate Submit Answer gesture. The guard prevents accidental input; it does
not establish that a participant read the question. Selection can change before
submission. Each answer grants one equal damage upgrade by default, with
correctness feedback off. The progress HUD shows the damage reward after Submit.
Resuming clears input and requires neutral pointers and fresh desktop movement,
so the submission gesture cannot fire an attack or restore an old destination.
Victory requires all three cleared rooms and submitted answers plus crossing the final exit.

Defeat Retry restores health and restarts only the current room, preserving
completed answers and earned upgrades. Victory New Run resets progress and base
stats. Progress exists only in memory; browser refresh/close or application
restart does not preserve it. No backend, persistence, hosting, or pricing
decision is implied.

Earlier single-arena verification (2026-10-13): the Web development build succeeded, and
54 actual Chrome mouse/keyboard/emulated-touch browser checks passed across
four complete runs and two defeat/retry cycles. Question/Victory
screenshots, build logs and results are outside Git; see [HANDOFF.md](HANDOFF.md)
and [browser regression instructions](../Tests/Browser/README.md). Physical
devices, Safari, release/native players, Editor runtime, arbitrary long-content
fixtures and alternative Inspector configurations remain unverified. Next
manual check: rapidly attack through a final kill, release, change a selection,
and verify that only Submit Answer records it, awards once, and resumes without
firing an attack.

iPhone 12 Safari testing subsequently confirmed a focus-bridge deadlock: visible
canvas touches reached Unity while DOM/browser focus remained false and paused
the clock. A trusted touch on the visible game now restores bridge focus and
is consumed until release; subsequent fresh input can play. Question, ending,
and background pauses remain protected. The diagnostic `SAFARI-FOCUS-2` player
was confirmed by the user on a physical iPhone 12: movement and attacks work. Preserve this recovery bridge; see the latest handoff.

For timed tests, the timer counts only time spent answering questions. Gameplay and other time outside answering do not consume the test's answering-time allowance.

## Multiplayer class battle

A separate multiplayer class battle takes place after assessments. This battle contains no questions and is separate from the assessment and its answering-time timer.

## Responses and datasets

Responses support private exports for creators. Creators may also opt to contribute responses to public datasets, subject to review before publication. Public dataset participation is optional; collecting responses does not automatically make them public.

## Game scope and extensibility

Start with one game: the Unity 3D action RPG. Design the platform to support additional games later, keeping assessment content, response handling, timing rules, and creator configuration separate from game-specific implementation.

## Open decisions

- **Pricing:** Undecided.
- **Hosting:** Undecided.

## Historical Garden Quest local polish

An earlier prototype used an original procedural garden fantasy style, an adventurer
with a growing crystal wand, mushroom chasers, and one slower telegraphed flower
mage in waves two and three. Counts remain 2/3/4, with three equal-reward questions
and Submit-unlocked room exits. Damage protection follows Play, Resume,
question submission and focus recovery. Contact damage is modest and spaced.

Play activates procedural browser audio. Pause/Resume and Restart/New Run are
separate from question, background and ending pauses. Sound preference persists
in browser local storage; assessment/run progress is still in memory. Retry
preserves submitted answers/upgrades. The default preview hides developer
diagnostics; use `?devDiagnostics=1` explicitly for troubleshooting. The menu
shows `DRFORMS-LAB-1`. Phone layouts use a safe-area-aware canvas and browser
insets. Physical iPhone verification of this new polish is pending; the user
confirmed movement and attack in the preceding SAFARI-FOCUS-2 build.


Garden Quest's presentation now uses a closer overhead camera, continuous tiled
grass beyond the unchanged collision boundaries, outer paths, beveled border
stones and procedural cypress/fern scenery. Shared soft mesh contacts ground
characters without real-time shadow maps. Articulated arms/boots, facing/cast/hit/
defeat motion, bounded movement dust/impact sparks and up to twelve short damage
numbers remain original procedural art, with no purchased/downloaded assets.
This improves the prototype; it does not provide commercial character art.


## Representative room art

The first room is an overgrown experimental laboratory courtyard. Original
rigged Dr.forms scientist and experimental creatures replace the medieval
characters, with goggles, wild hair, an ivory coat, teal gloves/boots and a
colourful flask satchel. Blended locomotion and an independent throwing arm
support moving flask attacks. A short hand release, flight arc, bounded chemical
splashes and glass-like synthesized sound replace the wand presentation; damage
remains single-target. Sources and rights are in [ART_SOURCES.md](ART_SOURCES.md).

The warm-ivory/deep-teal palette with restrained green/violet accents extends to
HUD, controls, specimen equipment and locked/open exit indicators. The existing
1024 shadow budget, three encounters, question/reward transactions, progression,
controls and Safari recovery remain. Later rooms retain simpler environments.
This is a local review milestone; hosting and pricing remain undecided.
