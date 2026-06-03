# SCR-CONTROL — Timer / effect / clif / NPC-control builtins + npc_chat

> **Epic:** scripting · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** SCR-EVENTS · **Unlocks:** SCR-BULK

## The deliverable

> The control/effect builtins work: `initnpctimer`/`stopnpctimer`/`settimer`, `specialeffect`/
> `misceffect`, `announce`/`mapannounce`, `enablenpc`/`disablenpc`/`hideonnpc`/`movenpc`,
> `donpcevent`/`cmdothernpc`, and `npc_chat`.

## What this absorbs (archive)

- `_archive/todo/scripting/SCRIPT-08` — timer/effect/clif/NPC-control builtins.
- `_archive/todo/scripting/SCRIPT-11` — NPC-chat (`npc_chat`) completion.

## rAthena reference

- `rathena/src/map/script.cpp` — `buildin_initnpctimer`/`stopnpctimer`, `buildin_specialeffect`/
  `misceffect`, `buildin_announce`/`mapannounce`, `buildin_enablenpc`/`disablenpc`/`movenpc`,
  `buildin_donpcevent`; `rathena/src/map/npc_chat.cpp`.

## Scope

- [ ] Implement each control builtin (NPC timers, effects, announce/broadcast, enable/disable/move,
      donpcevent) + `npc_chat`, each emitting its ZC packet.

## Done criteria

- A test NPC can run a timer, play an effect, announce, hide/show/move itself, and trigger another
  NPC's event; npc_chat speaks; no `ScriptStub` left for these.

## Test plan

- Per-builtin tests + a live control NPC.

## Notes

- Truly last. Builds on SCR-EVENTS (timer/event dispatch).
