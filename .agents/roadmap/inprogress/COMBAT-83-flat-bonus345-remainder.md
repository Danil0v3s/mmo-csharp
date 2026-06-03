# COMBAT-83 — Flat bonus3/4/5 remainder (drops, vanish-race/flag, SetDefRace, StateNoRecover, AddEffOnSkill)

> **Epic:** Combat parity · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** yes
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

- [ ] Per-race vanish map + the flag-gated vanish variant (extends COMBAT-44).
- [ ] Drop-item bonus tables + the drop roll consumer.
- [ ] `bStateNoRecoverRace` (on-hit no-recover by race) + `bSetDefRace`/`bSetMDefRace`.
- [ ] `bAddEffOnSkill` (on-skill proc list) + its consumer.
- [ ] Wire each into `ScriptedBonusHost.bonus3/4/5` (NOT the retired regex extractor).
- [ ] (Flag-matched `bAddEle`/`bSubEle`/`bSubRace` are COMBAT-82 — coordinate, don't duplicate.)

## Done criteria

- ➡️ from COMBAT-64: each listed flat bonus3/4/5 form populates a real consumer (no silent skip);
  a representative numeric test per subsystem matches rAthena.

## Test plan

- Per-subsystem numeric tests (vanish-race, drop bonus, set-def-race, addeff-on-skill).

## Notes / gotchas

- Split further if a single subsystem (e.g. drop tables) turns out to be its own large ticket.
- Do not re-add the retired regex extractor path; the host is the live surface.
