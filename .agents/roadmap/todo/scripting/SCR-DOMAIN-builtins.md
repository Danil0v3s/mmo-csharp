# SCR-DOMAIN — Quest/party/guild/instance/companion script builtins

> **Epic:** scripting · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** GP-QUEST, GP-PARTY, GP-GUILD, GP-INSTANCE (the systems must exist) · **Unlocks:** SCR-BULK

## The deliverable

> The domain script builtins work, bridging scripts to the gameplay systems: quest
> (`setquest`/`completequest`/`checkquest`), party/guild/clan queries, instance
> (`instance_create`/`instance_enter`/`instance_attachmap`/`instance_destroy`), and
> pet/homun/merc/mail/auction/channel/BG builtins.

## What this absorbs (archive)

- `_archive/todo/scripting/SCRIPT-04` — quest & achievement script builtins.
- `_archive/todo/scripting/SCRIPT-05` — party / guild / clan script builtins.
- `_archive/todo/scripting/SCRIPT-06` — instance scripting builtins.
- `_archive/todo/scripting/SCRIPT-09` — pet/homun/merc/mail/auction/channel/BG script builtins.

## rAthena reference

- `rathena/src/map/script.cpp` — `buildin_setquest`/`completequest`/`checkquest`,
  `buildin_getpartyleader`/guild queries, `buildin_instance_*`, `buildin_makepet`/`getmercinfo`/etc.

## Scope

- [ ] Implement each domain builtin calling the corresponding gameplay service (quest/party/guild/
      instance/companion/mail/auction).

## Done criteria

- A test NPC can give a quest, query party/guild, create + enter an instance, and make a pet via
  script; each routes to the real system; no `ScriptStub` left for these.

## Test plan

- Per-domain builtin tests + a live quest-giver / instance-entrance NPC.

## Notes

- Truly last AND gated on the gameplay systems existing (GP-QUEST/PARTY/GUILD/INSTANCE) — the
  builtins are thin bridges over those services.
