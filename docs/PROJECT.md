# Dr.forms

Dr.forms is a SaaS platform for creating and completing forms, quizzes, homework, and tests. Participants can answer directly or choose an optional Unity 3D action RPG experience.

## Assessment experiences

- **Direct answering:** Participants complete questions through a conventional form or assessment interface.
- **Optional game:** Participants complete the assessment through a Unity 3D action RPG, with questions presented at intervals configured by the creator.

Creators configure question intervals and the rules governing upgrades in the game experience.

The current local prototype uses this pacing: Wave 1 → Question 1 → Reward →
Wave 2 → Question 2 → Reward → Wave 3 → Question 3 → Reward → Victory.
Each cleared wave waits for its answer and a separate reward Continue; no timed
mid-combat question trigger runs. This is a prototype pacing choice, not a
restriction on future creator-configured intervals. The prototype validates
exactly three waves and three questions, each with three options. Question
content, wave enemy counts, and reward settings remain independent.

Questions freeze gameplay and require released pointers/submit keys followed by
a configurable input guard (default 0.5 seconds), a fresh option selection, and
a separate Submit Answer gesture. The guard prevents accidental input; it does
not establish that a participant read the question. Selection can change before
submission. Each answer grants one equal damage upgrade by default, with
correctness feedback off. A reward summary requires its own Continue gesture.
Victory requires all three cleared waves and submitted answers.

Defeat Retry restores health and restarts only the unfinished wave, preserving
completed answers and earned upgrades. Victory New Run resets progress and base
stats. Progress exists only in memory; browser refresh/close or application
restart does not preserve it. No backend, persistence, hosting, or pricing
decision is implied.

Prototype verification (2026-10-12): the Web development build succeeded, and
45 actual Chrome mouse/keyboard/emulated-touch browser checks passed across
four complete runs and two defeat/retry cycles. Question/reward/Victory
screenshots, build logs and results are outside Git; see [HANDOFF.md](HANDOFF.md)
and [browser regression instructions](../Tests/Browser/README.md). Physical
devices, Safari, release/native players, Editor runtime, arbitrary long-content
fixtures and alternative Inspector configurations remain unverified. Next
manual check: rapidly attack through a final kill, release, change a selection,
and verify that only Submit Answer records it and Continue advances the wave.

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
