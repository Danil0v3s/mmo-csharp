# SCR-DIALOG — Dialog primitives complete

> **Epic:** scripting · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** SCR-BULK

## The deliverable

> Every NPC dialog primitive works end-to-end against the client: `mes/next/menu/close` (already
> work) plus `close2/input/prompt/clear/cutin/progressbar/sleep/select`.

## What this absorbs (archive)

- `_archive/todo/scripting/SCRIPT-01` — complete dialog primitives (close2/input/prompt/clear/cutin/progressbar/sleep).

## rAthena reference

- `rathena/src/map/script.cpp` builtins: `buildin_close2`, `buildin_input`, `buildin_prompt`,
  `buildin_clear`, `buildin_cutin`, `buildin_progressbar`, `buildin_sleep`/`sleep2`.
- `rathena/src/map/clif.cpp` — the matching ZC dialog packets + the CZ response parse.

## Scope

- [ ] Implement each remaining dialog builtin in the V8 host (`ctx.*`) + its ZC packet + the CZ
      response handler (input value, progressbar wait, cutin show/hide).

## Done criteria

- A test NPC using each primitive drives the client correctly (input returns the typed value;
  progressbar blocks then resumes; cutin shows the image); no `ScriptStub` left for these.

## Test plan

- Dialog runtime tests per primitive + a live NPC walkthrough.

## Notes

- Truly last (scripting). Dialog `mes/next/menu/close` already work (README ground truth).
