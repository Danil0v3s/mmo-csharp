# SCRIPT-01 — Dialog primitives (close2 / input / prompt / clear / cutin / progressbar / sleep)

> **Epic:** Scripting parity · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Blocks:** SCRIPT-10 (town NPCs need input/close2)

## Problem

The dialog runtime works end-to-end for `mes`/`next`/`select`/`menu`/`close`
(real suspend+resume over `TaskCompletionSource`), but the *other* primitives a
real NPC needs are all no-op `ScriptStub.Call(...)` that only log. A script that
calls `await ctx.input()` (numeric/string entry — the backbone of every "type
the amount" / "enter the password" NPC), `ctx.close2()` (the very common
"keep the window, return control without ending the script" idiom), `ctx.prompt()`
(menu that also returns on Escape/256), `ctx.clear()`, `ctx.cutin()` (illustration
overlay), `ctx.progressbar()` (timed gauge that blocks input), or `ctx.sleep()/sleep2()`
gets a logged stub and a placeholder return. The client either hangs (no packet
ever sent so the player is stuck), or the script proceeds with a bogus `0`.

## Current state (C#)

- `Map.Server/Scripting/Dialog/DialogContext.cs:77-124` — `mes`/`next`/`select`/`menu`/`close`
  are REAL (enqueue `ZC_SAY_DIALOG`/`ZC_WAIT_DIALOG`/`ZC_MENU_LIST`/`ZC_CLOSE_DIALOG`,
  set `_dialog.Pending`, return the TCS task). This is the pattern to extend.
- `Map.Server/Scripting/Dialog/DialogContext.AdditionalFlowUtilities.cs` — `close2`,
  `prompt`, `clear`, `cutin`, `progressbar`, `sleep`, `sleep2` are `ScriptStub.CallAsync(...)`
  no-ops (verify exact method list in file; these are the in-scope stub sites).
- `Map.Server/Scripting/Dialog/DialogSession.cs:41-51` — `PendingWait` union has only
  `Next` / `Menu` / `Close`. No `Input` / `InputStr` / `Progressbar` variants.
- `Map.Server/Scripting/Dialog/DialogDispatcher.cs:95-119` — `ResumeNext`/`ResumeMenu`/
  `ResumeClose` are the only resume entry points. No resume for input or progressbar.
- `Map.Server/Handlers/` — `ContactNpcHandler`, `ChooseMenuHandler`, `CloseDialogHandler`,
  `ReqNextScriptHandler` exist. **No handler for the input-edit packets.**
- `Core.Server/Packets/In/CZ/` — has `CZ_CHOOSE_MENU`, `CZ_CLOSE_DIALOG`, `CZ_REQ_NEXT_SCRIPT`.
  **No `CZ_INPUT_EDITDLG` (0x0143) nor `CZ_INPUT_EDITDLGSTR` (0x01d5).**
- `Core.Server/Packets/Out/ZC/` — no `ZC_OPEN_EDITDLG` (0x0142), no `ZC_OPEN_EDITDLGSTR`
  (0x01d4), no `ZC_SHOWILLUST` (cutin, 0x01b3), no progress-bar packets (`ZC_PROGRESS`
  0x02f0 / `ZC_PROGRESS_CANCEL` 0x02f2; the CZ ack is `CZ_PROGRESS` 0x02f1).

## rAthena reference (source of truth)

Canonical source is `script.cpp` BUILDIN arms.

- `script.cpp:5032 BUILDIN(close2)` — `clif_scriptclose(sd, npc)`; sets `st->mes_active=0`
  but **does NOT terminate the script** (unlike `close`). Returns control; the dialog
  window stays up until the player clicks Close, which fires the close-ack and the
  script continues from the statement after `close2`. So `close2` is a *suspend that
  resolves on the client's close-ack*, then the script keeps running.
- `script.cpp:6143 BUILDIN(input)` — opens an edit dialog. Numeric form sends
  `ZC_OPEN_EDITDLG` (0x0142); string form sends `ZC_OPEN_EDITDLGSTR` (0x01d4). The
  reply `CZ_INPUT_EDITDLG`/`CZ_INPUT_EDITDLGSTR` carries the value. Numeric input is
  clamped to `[min,max]` (default min `input_min_value`, max `input_max_value` from
  `battle_config`, typically 0 .. INT_MAX); the builtin returns `-1` if below min,
  `1` if above max, `0` if in range (this return code is what scripts test). The value
  is written into the script variable argument; for the C# JS API we instead *return*
  the value (and expose the range-status separately if needed).
- `script.cpp:5307 BUILDIN(prompt)` — like `menu` but Escape returns `0xff` → the
  script gets `255` (constant `MAX_MENU_OPTIONS`+something); rAthena: prompt sets
  `@menu`/return value and on cancel returns 255 rather than ending the script.
- `script.cpp:6917 BUILDIN(cutin)` — `clif_cutin(sd, image, type)` → `ZC_SHOWILLUST`
  (0x01b3): string filename + byte position (0=bottom,1=top? — see clif_cutin; 255 =
  remove). Non-blocking.
- `script.cpp:22420 BUILDIN(progressbar)` — `clif_progressbar(sd, color, seconds)` →
  `ZC_PROGRESS` (0x02f0): color (hex string → uint) + duration. Blocks until the bar
  finishes OR the player moves/acts (which cancels — `CZ_PROGRESS` 0x02f1 ack). The
  script resumes when the timer elapses or the cancel arrives.
- `script.cpp:20261 BUILDIN(sleep)` / `:20294 BUILDIN(sleep2)` — `sleep` detaches the
  player (`st->rid=0`) for ms then resumes the script with no attached player; `sleep2`
  keeps the player attached and aborts if the player logged out. Both yield the game
  loop for N ms. (`clear` is `clif_scriptclear` 0x0152 — wipe the dialog text region.)

## Scope — every sub-system that must be touched

- [ ] **New outgoing packets** in `Core.Server/Packets/Out/ZC/`:
      `ZC_OPEN_EDITDLG` (0x0142, fixed, body = npc id u32),
      `ZC_OPEN_EDITDLGSTR` (0x01d4, fixed, npc id u32),
      `ZC_SHOWILLUST` (0x01b3, fixed: image[64] + byte type),
      `ZC_PROGRESS` (0x02f0: color u32 + seconds u32),
      `ZC_PROGRESS_CANCEL` (0x02f2),
      `ZC_SCRIPTCLEAR` (0x0152, npc id u32) for `clear`.
- [ ] **New incoming packets** in `Core.Server/Packets/In/CZ/`:
      `CZ_INPUT_EDITDLG` (0x0142, npc id u32 + value i32),
      `CZ_INPUT_EDITDLGSTR` (0x01d5, var-length: npc id u32 + string),
      `CZ_PROGRESS` (0x02f1, npc id u32). Register var-length sizes in
      `Core.Server/Packets/appsettings.packets.json` where applicable.
- [ ] **`PendingWait` variants** in `DialogSession.cs`: `InputNum(TaskCompletionSource<int>, int Min, int Max)`,
      `InputStr(TaskCompletionSource<string>)`, `Progressbar(TaskCompletionSource)`.
      Progressbar also needs a game-loop timer token so it auto-resolves on timeout.
- [ ] **`DialogContext.AdditionalFlowUtilities.cs`** — replace the stubs:
      `input(min,max)` → flush, send `ZC_OPEN_EDITDLG`, set `PendingWait.InputNum`, await TCS,
      clamp on resume, return value; `input(string)` overload → `ZC_OPEN_EDITDLGSTR` + `InputStr`;
      `close2()` → send `ZC_CLOSE_DIALOG`, set a `Close`-like pending that **resolves and lets
      the script continue** (do NOT null out `session.Dialog` like `ResumeClose` does — close2
      must keep the session alive); `prompt(options)` → like `select` but map Escape (255) to a
      sentinel the JS sees (return 255); `clear()` → send `ZC_SCRIPTCLEAR`, no suspend;
      `cutin(image,type)` → send `ZC_SHOWILLUST`, no suspend; `progressbar(color,seconds)` →
      send `ZC_PROGRESS`, register a loop timer for `seconds`, set `PendingWait.Progressbar`,
      resolve on timer OR on `CZ_PROGRESS` cancel; `sleep/sleep2(ms)` → register a loop timer,
      suspend, resume on elapse (sleep2 aborts if player gone).
- [ ] **`DialogDispatcher.cs`** — add `ResumeInputNum(session,npcId,value)`,
      `ResumeInputStr(session,npcId,text)`, `ResumeProgressCancel(session,npcId)`, and a
      `ResumeClose2`-style path (or parameterize `ResumeClose` to not end the session for the
      close2 case). Reuse `ValidateResume<T>`.
- [ ] **New handlers** in `Map.Server/Handlers/`: `InputEditDlgHandler` (CZ_INPUT_EDITDLG),
      `InputEditDlgStrHandler` (CZ_INPUT_EDITDLGSTR), `ProgressCancelHandler` (CZ_PROGRESS).
      Each tagged `[PacketHandler(...)]` and dispatching into `IDialogDispatcher`.
- [ ] **Game-loop timer hook** for `progressbar`/`sleep` — schedule a one-shot on the 60fps
      tick (reuse the per-NPC timer plumbing from SCRIPT-03 if it lands first; otherwise a
      minimal `DialogTimerService` driven from `MapServerImpl`'s tick).
- [ ] **`scripts/types/api.d.ts`** — fix the `input`/`prompt`/`progressbar`/`cutin`/`sleep`
      signatures so authors get correct return types (`input(): Promise<string>` vs
      `input(min,max): Promise<number>`).

## Done criteria

- `await ctx.input(1, 100)` opens the numeric edit box; typing `50` returns `50`; typing
  `200` clamps/returns per rAthena (`> max` → returns max value & status; pin the exact
  clamp+return in a test). String `await ctx.input()` returns the typed string.
- `await ctx.close2()` keeps the window, and code after it runs once the client clicks Close
  (the dialog session is NOT torn down before the continuation runs).
- `ctx.cutin("kafra_01", 2)` sends `ZC_SHOWILLUST`; `ctx.cutin("", 255)` clears it.
- `await ctx.progressbar("0xFFFFFF", 5)` blocks ~5 s then resumes; moving cancels and resumes early.
- `await ctx.sleep(1000)` yields ~1 s and resumes.
- **No `ScriptStub.Call` remains in `DialogContext.AdditionalFlowUtilities.cs`.**

## Test plan

- `Map.Server.Tests/Scripting/DialogPrimitivesTests.cs`: drive a fake `MapSessionData`,
  invoke a TS hook that calls `ctx.input(1,100)`, assert `ZC_OPEN_EDITDLG` was enqueued and
  `_dialog.Pending` is `InputNum(1,100)`; feed `dispatcher.ResumeInputNum(s, npcId, 50)`,
  assert the script continuation observed `50`; feed `200`, assert clamp/return matches rAthena.
- Pin `close2` continuation: after `ResumeClose2`, the next `ctx.mes` still sends (session alive).
- Pin progressbar timeout via an injectable clock/timer (resolve on simulated elapse).
- Reuse the `ScriptHostTests.cs` harness for engine setup.

## Notes / gotchas

- `EnableTaskPromiseConversion` is already on — return `Task`/`Task<T>` and ClearScript marshals.
- Order matters: every suspending method must call `FlushPlayerDirty()` first (see `DialogContext.cs`).
- `close2` is the #1 footgun: it must not reuse `ResumeClose` (which sets `session.Dialog=null`)
  or the script's post-close2 statements run against a dead session.
- Numeric input default bounds come from `battle_config.input_min_value` / `input_max_value`;
  surface sane defaults (0 .. 2147483647) when the script omits them.
