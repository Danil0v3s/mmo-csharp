# INFRA-09 — ScriptedBonusHost residual host-functions (conditional combo correctness)

> **Epic:** Infra parity · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none

## Problem

`ScriptedBonusHost` is the V8-callable surface for translated rAthena item / combo
scripts. A cluster of its host functions return a constant `0`/`1` instead of reading
live state, so any **conditional** combo that gates on them silently mis-fires. The bias
is "miss, don't lie" (a combo that *should* fire is skipped rather than firing wrongly) —
correctness-safe but it means real set bonuses **under-apply**. Examples: a card combo
that adds +20 ATK only `if getskilllv("..") >= 5` never fires; a combo gated on
`countitem(X)`, `isequipped(Y)`, `eaclass() & EAJL_THIRD`, `getParam(Class) == ...`, or
`readparam(bDex)` is dead. `bonus5` also drops its val2/val3.

## Current state (C#)

- `Map.Server/Inventory/Script/ScriptedBonusHost.cs` — ctor (`:48-72`) already injects
  `_pc`, `_bundle`, `_equipped`, `_catalog`, `_bonusSvc`, `_entities`, `_skillSvc`
  (`IPlayerSkillService`), `_optionSvc`, `_visibility`, `_sc`. The data is mostly already
  in scope.
- `getskilllv(params object[] _) => 0` (`:589`) — **despite `_skillSvc` being injected**.
- `eaclass(params object[] _) => 0` (`:748`).
- `getiteminfo => 0` (`:753`), `getitemcount => 0` (`:755`), `checkoption => 0` (`:757`),
  `checkmount => 0` (`:759`), `countitem => 0` (`:761`), `isequipped => 0` (`:763`),
  `isequippedcnt => 0` (`:765`), `checkfalcon => 0` (`:769`), `checkriding => 0` (`:771`),
  `checkcart => 0` (`:773`), `checkidle => 0` (`:775`). (`basicskillcheck => 1` at `:767`
  is a deliberate "assume basics learned" — verify before changing.)
- `getParam` switch (`:905-922`): primary + trait stats read live from `_pc.Stats`, but
  `"Class" => 0`, `"Sex" => 0`, `"Zeny" => 0` (`:918-920`) — the comment notes these
  "live on MapSessionData / CharEntity, not PlayerEntity".
- `bonus5(params object[] args)` (`:216-225`) — folds the 5-arg form onto the 3-arg
  `ApplyIndexedBonus` using only `args[2]`, **dropping `args[3]` (val2) and `args[4]`
  (val3)**.
- Working reference points already implemented in the same file: `getequipid` (`:594`),
  `getequiprefinerycnt`/`getrefine` (`:584-587`) read `_equipped` + `_catalog`;
  `getequipweaponlv` (`:601`) reads `ItemEntity.WeaponLevel`. Use these patterns.

## rAthena reference (source of truth)

Canonical source is `script.cpp` BUILDIN functions (the script DSL), backed by `pc.cpp` /
`status.cpp` readers.

- `getskilllv("SkillName")` → `pc_checkskill(sd, skill_id)` — the PC's learned level of
  that skill (0 if unlearned).
- `countitem(itemId)` / `getitemcount` → sum of `amount` over inventory rows matching the
  id (identified). `countitem2` variants add card/refine filters.
- `isequipped(id, ...)` → 1 if all listed ids are currently equipped (cards count when
  slotted into an equipped item); `isequippedcnt` → count of how many are equipped.
- `eaclass()` → `pc_mapid2jobid`-style expanded-class bitmask (EAJL_2_1/2_2/UPPER/BABY/
  THIRD ...) derived from the PC's job id.
- `getParam(SP_CLASS/SP_SEX/SP_ZENY)` → `sd->status.class_` / `sd->status.sex` /
  `sd->status.zeny`. `readparam` is the same dispatch.
- `getiteminfo(itemId, type)` → a field of the item's `item_data` (type selects
  buy/sell/weight/atk/def/range/slot/weapon-level/...). See `script.cpp`
  BUILDIN(getiteminfo) for the `type` → field table.
- `checkoption(opt)` → 1 if `sd->sc.option & opt`. `checkmount`/`checkfalcon`/
  `checkriding`/`checkcart` → the corresponding OPTION_* / mount-state checks.
- `checkidle()` → idle-time check against `battle_config.idletime_*`.
- `bonus5 bKey, idx, val2, val3, val` (or the per-key arg order) — five-arg indexed bonus;
  the extra scalars are real (e.g. `bSubDefEle` family, certain autospell variants). The
  per-key arg meaning must match `pc_bonus5` in `pc.cpp`.

## Scope — every sub-system that must be touched

- [ ] **`getskilllv`** → `_skillSvc?.GetLevel(_pc, resolveSkillId(args[0]))`. Resolve the
      skill name/constant to an id (a skill-name→id map; the skill DB already has names).
      Return 0 when unlearned or `_skillSvc` null.
- [ ] **`countitem` / `getitemcount`** → sum `amount` over the PC's inventory for the id.
      The host needs the inventory list — plumb the session inventory in (the equip
      recalc that builds this host has the session; pass `IReadOnlyList<InventoryItem>
      bag` into the ctor alongside `_equipped`, or expose it via an injected accessor).
- [ ] **`isequipped` / `isequippedcnt`** → scan `_equipped` (already injected) for the
      listed ids, counting cards slotted into equipped items (`Card0..3`). isequipped
      returns 1 only if **all** listed ids are present.
- [ ] **`eaclass`** → resolve the PC's job id to the expanded-class bitmask. Add/locate a
      job-class resolver (the project likely has a `JobInfo`/`MapId` mapping; if not, port
      the `pc_jobid2mapid` table) and return the EAJL_* mask. Conditional combos gating on
      `& EAJL_THIRD` etc. then fire correctly.
- [ ] **`getParam("Class"/"Sex"/"Zeny")`** → plumb `Class`/`Sex`/`Zeny` from
      `MapSessionData`/`CharEntity` into the host (constructor param or accessor). Wire the
      three switch arms to the real values.
- [ ] **`getiteminfo(itemId, type)`** → read the `_catalog` item row field selected by
      `type` (buy/sell/weight/atk/def/range/slots/weaponlevel/...). Implement the `type`
      table per `script.cpp` BUILDIN(getiteminfo).
- [ ] **`checkoption`** → `_optionSvc` / `_pc` option bits. `checkmount`/`checkfalcon`/
      `checkriding`/`checkcart` → the corresponding option/state reads (use `_optionSvc`
      if it exposes them; otherwise read the PC's option/mount fields).
- [ ] **`checkidle`** → idle-time read if the PC tracks last-action time; otherwise return
      a documented conservative value (and note it).
- [ ] **`bonus5`** (`:216-225`) → stop dropping val2/val3. Route to a `pc_bonus5`-equivalent
      that carries all three scalars. If `BonusScriptExtractor` has no 5-arg path, add
      `ApplyIndexedBonus5(bundle, key, idx, val2, val3, val)` (or extend the bundle's
      slot-list as the docstring hints) so the few keys that use it (per `pc.cpp`
      `pc_bonus5`) apply fully.
- [ ] **Ctor / DI**: thread the new inputs (inventory bag, Class/Sex/Zeny) from the equip
      recalc call site (`EquipService.TryRecalcStats` / wherever the host is constructed).

This is **runtime-only** — no EF, no migration, no packets.

## Done criteria

- A combo gated on `getskilllv("X") >= 5` fires exactly when the PC has X at level ≥ 5.
- `countitem(redpotion)` returns the real bag count; `isequipped(card)` returns 1 only
  when slotted into an equipped item.
- `eaclass() & EAJL_THIRD` is true for a 3rd-job PC and false otherwise; class/sex-gated
  combos fire correctly.
- `getParam("Class"/"Sex"/"Zeny")` return live values; `bonus5` applies val2 and val3.
- None of the listed functions return a hardcoded constant where live state is available.

## Test plan

- `Map.Server.Tests/Inventory/Script/ScriptedBonusHostTests`:
  - `getskilllv` returns the injected skill level; 0 when unlearned.
  - `countitem`/`getitemcount` over a seeded bag; `isequipped`/`isequippedcnt` over seeded
    equipped items incl. cards.
  - `eaclass` for a 1st/2nd/transcendent/3rd/baby job → correct mask bits.
  - `getParam` Class/Sex/Zeny return plumbed values.
  - `bonus5` applies all three scalars (assert via the bundle / extractor output).
- A combo-script regression: pick one stock conditional combo and assert the bonus now
  lands when its gate is satisfied and not otherwise.

## Notes / gotchas

- `_skillSvc` is **already injected** — `getskilllv` returning 0 is the most embarrassing
  gap; the only missing piece is the name→id resolution.
- The "miss not lie" bias means these are **correctness-safe but under-applying** — fixing
  them can only *add* bonuses that should already be there, so regression risk is low, but
  pin the changed combos with tests so a future edit can't silently re-zero them.
- `basicskillcheck => 1` and the petinfo path (`:777`+) are deliberate — verify intent
  before touching; this ticket targets the constant-0 readers, not those.
- `getiteminfo`'s `type` → field mapping is the fiddliest part; copy the exact table from
  `script.cpp` BUILDIN(getiteminfo) rather than guessing field order.
- Class/Sex/Zeny living on `MapSessionData`/`CharEntity` is the same plumbing INFRA-01
  needs for the refine job bonus — consider a shared accessor.
