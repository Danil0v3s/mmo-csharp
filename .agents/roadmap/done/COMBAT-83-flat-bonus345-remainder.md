# COMBAT-83 — Flat bonus3/4/5 remainder (drops, vanish-race/flag, SetDefRace, StateNoRecover, AddEffOnSkill)

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-03) · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-64 · **Blocks:** none
> **Filed by:** COMBAT-64 — the flat bonus3/4/5 forms that each need a subsystem the live
> `ScriptedBonusHost` does not yet model.

## Problem

COMBAT-64 wired the defender `bSubSkill` reduction and rounded out the `bonus4 bAddEff`
(explicit-duration) AddEff family. The live `ScriptedBonusHost` already handles
`bonus3 bAutoSpell{,WhenHit}` (→ autobonus), `bonus3 bAddEff{,2,WhenHit}` (→ AddEff procs),
and `bonus4 bAutoSpellOnSkill`. The remaining flat bonus3/4/5 forms are **silently skipped**
(the host's documented coverage gap) — each needs a subsystem that does not exist yet:

| Form | Needs |
|---|---|
| `bonus3 bAddMonsterDropItem`/`bAddClassDropItem`/`...DropItemGroup` | item-drop bonus tables |
| `bonus3 bSPVanishRaceRate`/`bHPVanishRaceRate` | per-race vanish map (COMBAT-44 did flat vanish) |
| `bonus3 bSPVanishRate`/`bHPVanishRate` (`,bf`) | battle-flag-gated vanish |
| `bonus3 bAddEle`/`bSubEle`/`bSubRace` (`,bf`) | flag-matched cardfix lists → **COMBAT-82** |
| `bonus3 bStateNoRecoverRace` | per-race no-HP/SP-recover debuff-on-hit |
| `bonus4 bSetDefRace`/`bSetMDefRace` | set-DEF-vs-race override |
| `bonus3/4/5 bAddEffOnSkill` | on-skill (not on-hit) status proc |

> NOTE: the regex `BonusScriptExtractor` is retired (CONV-5); all of this belongs in
> `ScriptedBonusHost.bonus3/bonus4/bonus5`, not the regex pass.

## Current state (C#)

- `Map.Server/Inventory/Script/ScriptedBonusHost.cs:bonus3/bonus4/bonus5` — handle autospell +
  AddEff(+WhenHit) + AutoSpellOnSkill; everything else returns silently.
- `Map.Server/Inventory/EquipBonusBundle.cs` — no drop tables, no per-race vanish map, no
  flag-matched lists, no SetDefRace, no StateNoRecover, no AddEffOnSkill list.

## rAthena reference (source of truth)

- `pc.cpp` `pc_bonus3`/`pc_bonus4`/`pc_bonus5` SP_* arms (see the table above for the cases).
- `battle.cpp` consumers (vanish, set-def-race, addeff-on-skill).

## Scope — every sub-system that must be touched

- [x] **Flag-gated flat vanish** (`bonus3 bHPVanishRate/bSPVanishRate, x, n, bf`) — extended
      COMBAT-44's `ApplyVanish`: added `HpVanishFlag`/`SpVanishFlag` to the bundle, parse the bonus3
      forms in `ScriptedBonusHost.bonus3` (BF flag via the COMBAT-82 `BattleFlags`), and gate the
      vanish roll on the attack's BF flag. (The bonus2 no-flag form — COMBAT-44 — is unconstrained.)
- [ ] ➡️ **Per-race vellum vanish** (`bHPVanishRaceRate`/`bSPVanishRaceRate`) → **COMBAT-100** (a
      damage-OVERRIDE mechanic needing an `IsSpDamage` display the C# lacks).
- [ ] ➡️ **Drop-item bonus tables** (`bAddMonsterDropItem`/`bAddClassDropItem`/`...DropItemGroup`) →
      **COMBAT-101** (drop-bonus table + mob-death drop-roll consumer).
- [ ] ➡️ **`bSetDefRace`/`bSetMDefRace`** → **COMBAT-102** (set-DEF-vs-race proc).
- [ ] ➡️ **`bStateNoRecoverRace`** → **COMBAT-103** (on-hit no-recover SC by race).
- [ ] ➡️ **`bAddEffOnSkill`** → **COMBAT-104** (on-skill status proc).
- [x] (Flag-matched `bAddEle`/`bSubEle`/`bSubRace` shipped in **COMBAT-82**.)

## Done criteria

- ✅ the flag-gated flat vanish populates a real consumer (no silent skip) and a numeric test matches
  rAthena (a melee swing vanishes only when its BF flag matches the gate). ➡️ The other five
  subsystems (each its own consumer, per the "split further" note) are **COMBAT-100..104**.

## Test plan

- Per-subsystem numeric tests (vanish-race, drop bonus, set-def-race, addeff-on-skill).

## Notes / gotchas

- Split further if a single subsystem (e.g. drop tables) turns out to be its own large ticket.
- Do not re-add the retired regex extractor path; the host is the live surface.

## History

- 2026-06-03 — This card bundled six independent bonus3/4/5 subsystems; per its "split further" note,
  shipped the one with a clean gap-free consumer — the **flag-gated flat vanish**. Added
  `EquipBonusBundle.HpVanishFlag/SpVanishFlag` (+ Reset), the `bonus3 bHPVanishRate/bSPVanishRate,x,n,bf`
  parse in `ScriptedBonusHost.bonus3` (BF flag via COMBAT-82's `BattleFlags`, with the `pc_bonus`
  defaulting), and the flag gate in `DamageService.ApplyVanish` (BF_WEAPON + melee/ranged from the
  attacker's range; flag 0 = the COMBAT-44 unconstrained form). Combat44BonusTailTests +2 (BF_LONG gate
  blocks a melee swing; BF_SHORT gate fires). Full suite 4158 pass (1 fail = pre-existing INFRA-11 replay
  gate). Decomposed the remaining five subsystems into COMBAT-100 (per-race vellum damage-override),
  COMBAT-101 (drop-item bonus tables), COMBAT-102 (set-def-race), COMBAT-103 (state-no-recover-race),
  COMBAT-104 (add-eff-on-skill) — each its own consumer.
