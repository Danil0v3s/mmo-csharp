# SCRIPT-11 — NPC chat pattern-match auto-talk (defpattern / activatepset / event fire)

> **Epic:** Scripting parity · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** SCRIPT-03 (EventDispatcher to fire the matched label) · **Blocks:** none

## Problem

`npc_chat` lets an NPC watch nearby player chat and auto-fire a scripted event when a message
matches a registered regex pattern (the classic "say the magic word to the gatekeeper" / quiz
NPC). The C# `NpcChatService` is *almost* there — it stores patterns, activates/deactivates sets,
and matches text — but the two pieces that make it actually *do* something are empty:
`DefaultPattern` and `FinalizeEntry` are `{ }`, and `CheckChat` finds matches but **never fires
the event** (the comment says "entry point reserved here"). So a player can type the password and
nothing happens — the NPC never reacts.

## Current state (C#)

- `Map.Server/Scripting/NpcChat/NpcChatService.cs:20-34` — `DefPattern(npc, setId, pattern, event)`
  compiles + stores the regex (REAL).
- `:36-49` — `ActivatePset`/`DeactivatePset`/`DeletePset` toggle the active set (REAL).
- `:51-69` — `CheckChat(npc, speaker, text)` walks active sets, counts matches, but the match body
  only increments a counter — **no event fire** ("The actual event-fire dispatches via the script
  engine; entry point reserved here").
- `:71` — `DefaultPattern(NpcEntity npc) { }` — empty.
- `:78` — `FinalizeEntry(NpcEntity npc, int setId, string pattern) { }` — empty.
- `:72-77` — `Finalize(npc)` clears the NPC's sets (REAL).
- **Caller gap:** find where player chat is received (`Map.Server/Chat/` + the CZ chat handler in
  `Map.Server/Handlers/`). `CheckChat` must be invoked for every NPC in chat range when a player
  speaks. Verify whether any handler calls `INpcChatService.CheckChat` today (likely not).

## rAthena reference (source of truth)

`npc_chat.cpp` (libpcre-backed).

- `npc_chat.cpp:303 npc_chat_def_pattern(nd, setid, pattern, label)` — compile pattern, append a
  `pcrematch_entry { pcre, regex_str, label }` to the set. (≈ C# `DefPattern`.)
- `npc_chat.cpp:102 finalize_pcrematch_entry(e)` — free the compiled regex + strdup'd pattern/label
  for one entry. In C# (GC + compiled `Regex`) this is mostly a no-op, BUT `FinalizeEntry` should
  remove that specific entry from the set (the current empty body leaks the entry). `npc_chat.cpp:322
  npc_chat_finalize(nd)` frees all sets (≈ C# `Finalize`).
- **The match → fire path (the missing piece):** when a player sends a chat message, rAthena
  (`clif_parse_GlobalMessage` → `map_foreachinallrange(npc_chat_sub, ...)`) walks every NPC in
  range; `npc_chat_sub` runs each active set's patterns against the message, and **on the first
  match runs the entry's label as an NPC event with the speaking player attached** (`npc_event`
  / `run_script`), passing the matched text. The NPC reacts (mes, warp, give item, etc.).
- "DefaultPattern" in rAthena terms = the implicit set used when a script registers patterns
  without an explicit set id (set 0) / the parse-time default; in C# this should ensure a default
  set exists for an NPC so `defpattern` without an explicit set still matches.

## Scope — every sub-system that must be touched

- [ ] **`CheckChat` fires the event** — change the match body to invoke `EventDispatcher.FireNamed`
      (SCRIPT-03) with the matched entry's `EventName` (`"NpcName::OnLabel"`) and the **speaking
      player attached**, passing the matched text (and capture groups, if the JS API exposes them).
      Fire on first match per rAthena (or all-active-set matches — match rAthena's `npc_chat_sub`:
      it fires per matching entry; confirm and replicate). Return the match count as today.
- [ ] **`DefaultPattern(npc)`** — ensure a default pattern set (set 0) exists/activates for the NPC
      so `defpattern` calls without a set id land somewhere matchable.
- [ ] **`FinalizeEntry(npc, setId, pattern)`** — remove the matching entry from the set (currently
      leaks); keep `Finalize(npc)` (clears all) as is.
- [ ] **Wire the chat hook** — in the player-chat receive path (CZ global-message handler), after
      normal chat broadcast, call `INpcChatService.CheckChat` for each NPC within chat range of the
      speaker (use the visibility/area query, `map_foreachinallrange` equivalent). Range = the
      standard chat/AREA range.
- [ ] **Script builtins** — confirm `defpattern`/`activatepset`/`deactivatepset`/`deletepset` are
      bound on `ctx.npc` (or a chat surface) and route to `NpcChatService`; wire if missing.

## Done criteria

- An NPC registers `defpattern(0, "password", "Gate::OnCorrect")` + `activatepset(0)`; a nearby
  player typing "the password is correct" fires `Gate::OnCorrect` with that player attached, and
  the NPC reacts (its label script runs).
- A non-matching message fires nothing; a deactivated set never matches; `deletepset` removes it.
- `FinalizeEntry` removes exactly the named entry (no leak); `Finalize` clears all NPC sets.
- Players out of chat range do not trigger the NPC.
- **`DefaultPattern` and `FinalizeEntry` are no longer empty; `CheckChat`'s "entry point reserved"
  comment is gone and it actually fires.**

## Test plan

- `Map.Server.Tests/Scripting/NpcChatTests.cs`: register a pattern set on a fake NPC; call
  `CheckChat(npc, speaker, "...match...")` and assert `EventDispatcher.FireNamed` was invoked with
  the right label + speaker; non-match → no fire; deactivate → no fire; `FinalizeEntry` removes the
  entry (subsequent match count drops).
- Range test: a speaker outside the NPC's area does not trigger CheckChat (assert the chat handler's
  range query excludes it).

## Notes / gotchas

- rAthena uses PCRE; .NET `Regex` syntax differs slightly (named groups, possessive quantifiers).
  Most NPC patterns are simple, but flag a compile failure (already handled in `DefPattern`'s
  try/catch — keep that) so a bad pattern doesn't take down the chat path.
- Depends on SCRIPT-03's `EventDispatcher.FireNamed`; if that isn't landed, this ticket can't fire
  the event honestly — sequence after SCRIPT-03.
- Watch reentrancy: a matched fire happens during chat processing; reuse SCRIPT-03's guard so it
  doesn't clobber a player already in a dialog (defer or skip per rAthena, which would just run the
  event script with the player as rid).
- Match-all-vs-first: verify against `npc_chat_sub` whether multiple active sets each fire or only
  the first match wins, and replicate exactly — quiz NPCs depend on the semantics.
