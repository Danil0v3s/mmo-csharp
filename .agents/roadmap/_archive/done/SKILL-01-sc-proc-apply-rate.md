# SKILL-01 — Status-change procs must run the apply-rate / sc_def mechanism

> **Epic:** Skills · **Status:** ✅ Done (2026-06-01) · **Size:** L · **Player-visible:** yes
> **Depends on:** none · **Blocks:** SKILL-07, SKILL-08, SKILL-09, SKILL-12

## Problem

Every skill that procs a status change (Bash stun, Frost Diver freeze, Meteor
Storm stun, all the debuff skills) rolls the proc with `Random.Shared.Next(100) < chance`
directly inside the plugin, then calls `ctx.Sc.Start(...)` unconditionally. The
`chance` is the *raw* skill chance from the rAthena switch arm. This bypasses the
entire rAthena `status_get_sc_def` pipeline: the target's STR/AGI/VIT/INT/DEX/LUK
resistances, level-difference scaling, the `SC_*` resist items/cards, the duration
reduction (separate from the apply-rate reduction), and the per-tick minimum-rate
floor. A 5 % stun on a 99 VIT MVP currently lands at the same rate as on a 1 VIT
novice; freeze on a high-MDEF target lands full-rate; debuffs ignore boss immunity
except where a plugin happens to special-case it.

166 plugin files reference `Random.Shared`; the overwhelming majority of those are
SC proc rolls. The `IStatusChangeService.Start` signature has **no** rate / resist /
flag parameter at all — so even if a plugin wanted to defer to the engine, there is
no entry point.

## Current state (C#)

- `Map.Server/Status/IStatusChangeService.cs:28` — `Start(target, type, val1..val4, durationMs, source, nowTick)`. No `rate` argument, no `flag` argument, no resist computation. Caller decides whether the SC lands.
- `Map.Server/Skills/Behaviors/Mage/MeteorStorm.cs` `ApplyAdditionalEffects` — `if (_rng.Next(100) < 3 * skillLevel) ctx.Sc?.Start(target, StatusType.Stun, ...)`. Representative of the whole tail.
- `Map.Server/Skills/Behaviors/Acolyte/Adoramus.cs:35`, `DragonCombo.cs:25`, `Windmill.cs:44` — same raw-roll pattern; Windmill even rolls the *duration* with `Random.Shared`.
- ~166 files under `Map.Server/Skills/Behaviors/` use `Random.Shared`. Many inject a `Random? rng` ctor param for testability (e.g. `LaudaAgnus`, `Oratio`, `DecreaseAgi`) — that injection seam is reusable.
- No `status_get_sc_def` equivalent exists anywhere (`grep` for `GetScDef`/`sc_def`/`ApplyRate` in `Map.Server/Status` + `Map.Server/Skills` returns nothing).

## rAthena reference (source of truth)

- `rathena/src/map/status.cpp:9350` — `t_tick status_get_sc_def(struct block_list *src, struct block_list *bl, enum sc_type type, int32 rate, t_tick tick, unsigned char flag)`. Returns the **reduced duration** and mutates the rate via the `sc_def`/`sc_def_rate`/`tick_def` tables keyed off the target's stats. Boss / undead / `SC_NORATE` flags short-circuit.
- `rathena/src/map/status.cpp:status_change_start` (≈9851) — the canonical apply path. Computes `rate`, calls `status_get_sc_def` to get the resisted `rate` + reduced `tick`, then rolls `rnd()%10000 < rate` (rate is in 1/100-% units). `flag` (`SCSTART_NORATEDEF`, `SCSTART_NOTICKDEF`, `SCSTART_NOAVOID`, `SCSTART_LOADED`) gates which reductions apply.
- Stat-def table summary: stun/sleep/stone resisted by VIT; freeze/curse by MDEF+LUK; blind by VIT+INT; poison/silence/etc. by VIT; level-diff `(status_get_lv(src) - status_get_lv(bl))` adds to rate. Each `sc_def` entry has a min-rate floor (default 100 = 1 %) so nothing is fully immune unless boss/flagged.
- Monolithic-switch caveat: the canonical source is `status.cpp`. The rate is *passed in* by each `skill_additional_effect` (`skill.cpp` ≈ the per-skill arm) as a raw percent; the engine resists it. The C# split must mirror that division of labor — plugin supplies raw rate, engine resists.

## Scope — every sub-system that must be touched

- [x] **`IStatusChangeService.Start` overload** — ✅ added `Start(target, type, int rate, val1..4, durationMs, source, ScStartFlag flag, nowTick)` (rate in 1/100-% units). The legacy no-rate `Start` is now a thin wrapper calling it with `rate=10000, flag=NoRateDef|NoTickDef|NoAvoid` (guaranteed, no resist) so every existing self-buff/scripted caller is unaffected.
- [x] **`ScStartFlag` enum** — ✅ `None/NoRateDef/NoTickDef/NoAvoid/Loaded` (`Map.Server/Status/ScStartFlag.cs`).
- [x] **`IStatusChangeService.GetScDef`** — ✅ ported renewal `status_get_sc_def`: reads the target's battle status + level, applies the per-SC def table + level-diff (`(max(0,lvSrc−lvTgt))²/5·100`), Curse LUK-0 immunity, and the boss/MVP short-circuit. Returns `(resistedRate, reducedDuration)`; the roll lives in `Start` so the math is unit-testable.
- [x] **SC-def table** — ✅ `Map.Server/Status/ScDefTable.cs` — renewal rows for the standard CC set (Poison/Stun/Silence/Bleeding/Sleep/StoneWait/Freeze/Curse/Blind/Confusion) + the composite Fear/Burning. *(Bespoke-formula arms + per-SC `min_rate`/`min_duration` + SCRESIST/Siegfried/item-reseff adds ➡️ **SKILL-15**.)*
- [x] **`StatusChangeService` impl** — ✅ `GetScDef` + the resist+roll in the new `Start` (seedable injected `Random`).
- [ ] **Plugin migration** — ➡️ **Moved to SKILL-14.** Migrated the 3 representative plugins (MeteorStorm/Adoramus/Bash); the remaining ~163 `Random`-gated procs are a mechanical sweep tracked there.
- [x] **Boss / immune gating** — ✅ `MobEntity.Stats.Mode & MobMode.StatusImmune/Mvp` gated in `GetScDef` for the BossResist/MvpResist-flagged SCs (bypassed by `NoAvoid`).
- [x] **No new packets / IPC / DB** — ✅ purely server-side combat math.

## Done criteria

- ✅ `Start` with `rate` rolls through `GetScDef`; a stun on VIT 99 lands far less often than on VIT 1 (test pins resisted rate 30 vs 2970).
- ✅ Boss mobs are immune to the flagged CC SCs (test pins boss immunity vs a normal mob landing).
- ✅ Level-difference scaling matches rAthena sign + magnitude (test: lv99-vs-lv1 levelAdv 192000 → resist clamps to 0).
- ➡️ **Moved to SKILL-14:** No plugin calls `Random.Shared.Next` purely to gate an SC apply (only 3 of ~166 migrated here; the grep-guard test lands with the bulk sweep).
- ✅ No `// TODO` / log-only no-op in the touched plugins or `StatusChangeService`.

## Test plan

- `Map.Server.Tests` (or the map test project): `ScDefTests` — pin resisted rate for (stun, VIT 1) vs (stun, VIT 99); (freeze, MDEF/LUK low) vs high; level-diff +/-; boss immunity for stun/freeze/sleep/stone/curse/blind/silence.
- `StatusChangeServiceProcTests` — with a seeded `Random`, assert apply happens iff `roll < resistedRate`; assert reduced duration is applied (not the raw duration).
- Regression: pick 3 migrated plugins (MeteorStorm, Adoramus, a Bash-stun) and assert they call `Start` with `rate = chance*100` and no pre-roll.
- Grep guard test or CI check: no `Random.Shared.Next` / injected-`rng.Next` immediately preceding a `ctx.Sc.Start` in `Behaviors/`.

## Migration mechanics (per-plugin pattern)

The 166 `Random.Shared` files fall into three buckets — handle each differently:

1. **Pure SC-proc roll** (the majority — `MeteorStorm` stun, Bash stun, FrostDiver
   freeze): delete the `if (rng.Next(100) < chance)` guard; call
   `ctx.Sc?.Start(target, type, rate: chance * 100, …)` and let the engine roll.
   If `rng` was injected *only* for this roll, drop the ctor param.
2. **SC-proc with a non-SC side-roll** (e.g. `Windmill` rolls *both* the proc and a
   random duration): move the proc to the engine; keep `rng` for the duration roll —
   but prefer reading the duration from `skill_db` (SKILL-04) so even that becomes
   deterministic where rAthena uses a table.
3. **Non-SC randomness** (`MeteorStorm` cell offset, `Abracadabra` spell pick,
   `RichsCoin`): leave entirely alone — not in scope.

Annotate each migrated call site with the rAthena rate expression it came from
(e.g. `// skill.cpp WZ_METEOR arm: 3*lv % stun → rate 3*lv*100`).

## Notes / gotchas

- rate units: rAthena `status_change_start` rate is 1/100 % (rolls `rnd()%10000`). Many skill arms pass `rate` already in those units; the per-plugin chances here are whole percents, so multiply by 100 at the call site. Get this wrong and every proc is 100× too rare or too common.
- Duration reduction (`tick_def`) is *separate* from rate reduction — some SCs resist duration but not landing chance, and vice-versa. Don't fold them.
- The self-buff path (Increase Agi, Blessing on an ally) must NOT route through resist — those are guaranteed. The wrapper-with-`NoRateDef|NoTickDef|NoAvoid` keeps them intact; verify no buff skill regresses to "sometimes fails."
- `GetScDef` reads the target's *battle* status (final STR/AGI/VIT/INT/DEX/LUK after cards + SCs), not the base stat. Pull from the same status snapshot the damage path uses, or the resist will disagree with what the player sees.
- A handful of SCs are flagged `SCSTART_NOAVOID` in rAthena (cannot be resisted regardless of stats) — honor the `flag` short-circuit so those still land at the passed rate.
- This is the prerequisite for the family tickets (SKILL-07/08/09/11/12) — do not migrate per-family procs until this entry point exists, or you'll write the call sites twice.

## History

- 2026-06-01 · Built the SC resist pipeline (engine slice). `ScStartFlag` enum; renewal
  `status_get_sc_def` ported as `StatusChangeService.GetScDef` (stat resist via `ScDefTable`,
  `levelAdv = (max(0,lvSrc−lvTgt))²/5·100`, Curse LUK-0 immunity, MD_STATUSIMMUNE/MD_MVP boss
  gate, Aegis rate rounding, separate rate vs duration reduction); new rate-aware
  `Start(rate,…,flag)` overload that resists + rolls (seedable `Random`); legacy no-rate
  `Start` is now a guaranteed wrapper (`NoRateDef|NoTickDef|NoAvoid`). Two new interface
  members got default impls so existing test doubles compile unchanged. Migrated 3
  representative procs (MeteorStorm/Adoramus/Bash) to `rate = chance*100`. `Skill01ScDefTests`
  (12). Suite 3668 green. Follow-ups: SKILL-14 (bulk-migrate the remaining ~163 plugin proc
  rolls + grep-guard), SKILL-15 (ScDefTable depth — bespoke arms + min_rate/min_duration +
  SCRESIST/Siegfried/item-reseff).
