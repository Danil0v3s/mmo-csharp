# SCR-VARS — Variable/register system + mapreg SQL

> **Epic:** scripting · **Status:** ❌ Not started · **Size:** M · **Player-visible:** no
> **Depends on:** none · **Unlocks:** SCR-BULK

## The deliverable

> The full script variable/register system works + persists: scoped vars (`@`/`$`/`#`/`'`/`.`),
> arrays, `getd`/`setd`, and `$globalvar`/`$@` mapreg SQL persistence across restart.

## What this absorbs (archive)

- `_archive/todo/scripting/SCRIPT-07` — variable/register system (mapreg SQL, arrays, getd/setd, consolidation).
- `_archive/todo/infra/INFRA-07` — MapReg `$globalvar` SQL persistence.

## rAthena reference

- `rathena/src/map/script.cpp` — the var scope prefixes, `buildin_getd`/`setd`, array builtins;
  `mapreg_setreg`/`mapreg_load`/`mapreg_save` (the `mapreg` SQL table).

## Scope

- [ ] Implement the var-scope resolution (account/char/perm/temp/npc/global) + arrays + getd/setd.
- [ ] Persist `$`/`$@` permanent mapregs to the `mapreg` SQL table; load on boot; save on change.

## Done criteria

- A script sets a `$globalvar`, the server restarts, and the value is intact; arrays + getd/setd
  resolve correctly; char/account-scoped vars round-trip via the reg pipeline.

## Test plan

- Var-scope tests + a mapreg persistence round-trip (set → restart → read).

## Notes

- Truly last. The perm-var-reg pipeline (char_reg_num) already exists (archive COMBAT-52 used it).
