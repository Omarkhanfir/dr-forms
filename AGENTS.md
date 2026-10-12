# Repository guidance

- Read `docs/PROJECT.md`, `docs/HANDOFF.md`, and relevant files before editing. Inspect repository status and existing implementation first.
- Work on one scoped task at a time. Avoid unrelated changes and keep edits within the requested scope.
- Preserve Unity `.meta` files and asset GUIDs. Move or rename assets together with their `.meta` files; do not regenerate existing metadata.
- Do not edit or commit generated folders or build artifacts, including `Library/`, `Temp/`, `Logs/`, `Obj/`, and `UserSettings/`.
- Never commit secrets, credentials, tokens, or private configuration. Inspect the staged diff before committing.
- Do not upgrade Unity or packages without asking the user first.
- Verify changes with checks appropriate to the task. Report checks and results honestly, including failures and checks that were not run.
- At the end of every task, update `docs/HANDOFF.md` with current progress, checks, and the next step, then give the user a short summary.
- Do not implement gameplay until the user requests it. Follow the product scope in `docs/PROJECT.md`; pricing and hosting remain undecided.
