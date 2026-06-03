# INF-BONUSHOST — ScriptedBonusHost residual builtins

> **Epic:** infra · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** none

## The deliverable

> The remaining `ScriptedBonusHost` item-script host functions resolve real values
> (`getskilllv`/`eaclass`/`countitem`/`Class`/`Zeny`/`bonus5` forms) so item scripts that branch on
> them work.

## What this absorbs (archive)

- `_archive/todo/infra/INFRA-09` — `ScriptedBonusHost` residual host stubs (getskilllv/eaclass/countitem/Class/Zeny/bonus5).

## rAthena reference

- `rathena/src/map/script.cpp` — `buildin_getskilllv`/`eaclass`/`countitem`/`readparam`(Class/Zeny);
  the `bonus5` script forms in item scripts.

## Scope

- [ ] Implement each residual host function against the real player state (skill level, job class,
      item count, zeny) + the `bonus5` parse forms.

## Done criteria

- An item script that reads `getskilllv`/`countitem`/`Class`/`Zeny` and applies a `bonus5` resolves
  the real values; no residual stub left in the host.

## Test plan

- Host-function tests (each → real player state).

## Notes

- Parallel. Small. Completes the V8 item-script bonus host.
