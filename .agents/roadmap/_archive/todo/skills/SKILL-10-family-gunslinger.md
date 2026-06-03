# SKILL-10 — Family: Gunslinger — ChainAction multi-hit + coin / ammo branches

> **Epic:** Skills · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** SKILL-05 (single ratio path) · **Blocks:** none

## Problem

The Gunslinger family is largely bodied, but the one remaining shell —
**`ChainAction` (GS_CHAINACTION)** — is the load-bearing one, and it's a no-op
`WeaponSkillImpl`. In rAthena `GS_CHAINACTION` is *two* mechanisms:

1. **A castable damage skill** (`skill.cpp:5330`) — a direct gun hit.
2. **An auto-attack multi-hit modifier** (`battle.cpp:4459`) — when a Revolver user
   has learned Chain Action, normal attacks gain `div_ = skill_get_num(GS_CHAINACTION,
   lv)` extra bullets (the signature "two shots per click"). This is the whole point
   of the skill and it's the half that's missing.

The C# plugin's own docstring concedes it: *"Multi-hit cosmetic — the actual chain
follow-up is driven by the auto-attack hook. We leave Hits at the SkillDef default."*
There is no auto-attack hook that reads Chain Action, so Revolver users never get the
extra shot. Additionally the Gunslinger coin/ammo branches (Glittering coin gain,
Desperado / Rapid Shower ammo-per-hit consumption, the coin-spend skills) need
verification that they consume coins/ammo and branch on weapon type (Revolver vs
Rifle vs Gatling vs Shotgun vs Grenade) — several bodied plugins may skip the
ammo-type gate.

## Current state (C#)

- `Map.Server/Skills/Behaviors/Gunslinger/ChainAction.cs` — `class ChainAction : WeaponSkillImpl { ctor }`, no override. Docstring: *"Multi-hit cosmetic — the actual chain follow-up is driven by the auto-attack hook. We leave Hits at the SkillDef default."* — i.e. neither the cast hit nor the auto-attack multi-hit is wired.
- 43 Gunslinger plugins total; ChainAction is the only no-override shell, but the coin/ammo gates across the others are unverified (Glittering, Desperado, RapidShower, RichsCoin, TripleAction, FullBuster).
- Auto-attack pipeline: `IBattleCalculator.CalcWeaponAttack` produces `div_`/hits; there is no read of `GS_CHAINACTION` to bump `div_` on a Revolver auto-attack. Confirm where the auto-attack div_ is set (battle calc) and whether a learned-skill hook exists.
- Coin counter: `ctx.Orbs` (`IPlayerOrbService`) — confirm it has a Gunslinger-coin counter (`pc->spiritball` is reused for coins in rAthena); add/read if missing.
- Ammo type: `ctx.Equip` / `ctx.Catalog` — weapon-type + ammo-type reads for the per-weapon branch.

## rAthena reference (source of truth)

- `rathena/src/map/battle.cpp:4459` — `if (wd->div_ == 1 && sd->weapontype1 == W_REVOLVER && (skill_lv = pc_checkskill(sd, GS_CHAINACTION)) > 0) wd->div_ = skill_get_num(GS_CHAINACTION, skill_lv);` — the auto-attack multi-hit on Revolver. `battle.cpp:2966` — the crit interaction.
- `rathena/src/map/battle.cpp:7458` `case GS_CHAINACTION:` — the skill's `battle_calc_attack_skill_ratio` arm (cast-hit damage).
- `rathena/src/map/skill.cpp:5330` `case GS_CHAINACTION:` — castend damage arm.
- `rathena/src/map/skill.cpp:1804` — `SC_FLING` uses `sd->spiritball_old` (the coin count) — confirms coins ride the spiritball counter for Gunslinger.
- Gunslinger ammo: the per-weapon ammo-type gate (`skill_get_ammotype` / `W_REVOLVER`/`W_RIFLE`/`W_GATLING`/`W_SHOTGUN`/`W_GRENADE`) decides which skills are usable + how much ammo each consumes (`skill_get_ammo_qty`).
- Monolithic-switch caveat: canonical source is `battle.cpp` (auto-attack `div_` + ratio) and `skill.cpp` (castend + coin/ammo cost); split-file `rathena-fork/src/map/skills/gunslinger/chainaction.cpp` does NOT exist here.

## Scope — every sub-system that must be touched

- [ ] **ChainAction cast hit** — give `ChainAction` a `CalculateSkillRatio` override matching `battle.cpp:7458`, so the *castable* form deals correct damage (routes through the single ratio path from SKILL-05).
- [ ] **ChainAction auto-attack multi-hit** — add the auto-attack `div_` bump in the battle calc: when the attacker is a Revolver user with learned `GS_CHAINACTION` and the swing `div_ == 1`, set `div_ = skill_get_num(GS_CHAINACTION, lv)`. This is the missing "auto-attack hook." Reads learned skill via `pc_checkskill` + weapon type via the equip read. Cite `battle.cpp:4459`.
- [ ] **Coin economy** — confirm `IPlayerOrbService` tracks Gunslinger coins (spiritball reuse); wire `Glittering`/`RichsCoin` to add coins and the coin-spend skills (Desperado, etc.) to consume them. Add the counter if absent.
- [ ] **Ammo-type branches** — audit the bodied Gunslinger plugins (Desperado, RapidShower, FullBuster, TripleAction, SpreadAttack, etc.) for the per-weapon ammo-type gate + `skill_get_ammo_qty` consumption; add the gate where missing so a Rifle skill can't fire from a Shotgun, and ammo is consumed per hit.
- [ ] **DI** — ChainAction stays registered.
- [ ] **No new packets** beyond the existing hit/coin broadcasts.

## Done criteria

- A Revolver user with learned Chain Action gets `skill_get_num(GS_CHAINACTION, lv)` shots per auto-attack (test: `div_` bumped on Revolver, unchanged on Rifle / when unlearned).
- The castable GS_CHAINACTION deals damage per the `battle.cpp:7458` ratio (test).
- Glittering/RichsCoin add coins; coin-spend skills consume them and fail when broke (test).
- Ammo-type-gated skills reject the wrong weapon and consume the right ammo qty per hit (test on 2 weapon types).
- No "Multi-hit cosmetic … we leave Hits at the SkillDef default" comment remains.

## Test plan

- `ChainActionTests.RevolverAutoAttackMultiHit` — Revolver + learned lv → `div_ == GetNum(GS_CHAINACTION, lv)`; Rifle or unlearned → `div_ == 1`.
- `ChainActionTests.CastHitRatio` — castable form ratio matches `battle.cpp:7458`.
- `GunslingerCoinTests` — Glittering adds, spend consumes, broke → fail.
- `GunslingerAmmoTests` — wrong-weapon reject + correct ammo qty consumed.
- DI audit green.

## The two-mechanism split, concretely

rAthena `GS_CHAINACTION` is referenced in three places, and the C# shell wires none:
- `battle.cpp:4459` — auto-attack `div_` bump on Revolver (the passive "two shots").
- `battle.cpp:2966` — the crit interaction (`!skill_get_nk(GS_CHAINACTION, NK_CRITICAL)`).
- `battle.cpp:7458` / `skill.cpp:5330` — the *castable* skill's ratio + castend.

The plugin (`ChainAction.cs`) can only own the third. The first two must live in the
battle calculator's auto-attack path, reading `pc_checkskill(GS_CHAINACTION)` + the
equipped weapon type. This is the crux: a fix that only touches `ChainAction.cs`
leaves the signature passive broken. Confirm where `IBattleCalculator.CalcWeaponAttack`
sets `div_` and add the learned-skill hook there.

## Notes / gotchas

- Chain Action's multi-hit is in the **battle calc**, not the skill plugin — the plugin only owns the castable form. Putting the `div_` bump in the plugin would miss every normal auto-attack. Wire it where the auto-attack `div_` is computed.
- The `div_ == 1` guard in `battle.cpp:4459` matters: Chain Action only doubles a *single*-hit swing, not an already-multi-hit one. Preserve the guard or you stack multipliers.
- Gunslinger coins reuse the spiritball counter in rAthena (`spiritball`/`spiritball_old`). Don't add a separate persisted field if the orb service already covers it.
- SKILL-05 should land first so the castable ChainAction ratio flows through the single ratio path (otherwise the cast-hit and any splash dispatch could diverge).
