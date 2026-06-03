# COMBAT-93 — Reconcile SkillFailCause with rAthena e_useskill_fail_cause wire values

> **Epic:** combat · **Status:** 🚧 In progress · **Size:** S · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none
> **Filed by:** COMBAT-76 — it needed the correct wire values for NEED_MORE_BULLET /
> NEED_EQUIPMENT_KUNAI and found the rest of the enum is renumbered wrong.

## Problem

`SkillFailCause` (Core.Server) is sent **raw** on the wire by `clif_skill_fail`
(`BroadcastSkillFail` → `ZC_ACK_TOUSESKILL.Cause = (byte)cause`), so each value MUST equal
rAthena's `e_useskill_fail_cause` (clif.hpp:402) for the client to render the right localized
string. Most C# values do **not** match — e.g. `Stuff = 15` (rAthena `STUFF_INSUFFICIENT = 3`),
`Delay = 4` vs rAthena `SKILLINTERVAL = 4` (coincidental), `WrongWeapon = 6` (matches),
`NoRedJewel = 7` (rAthena `REDJAMSTONE = 7`, matches), but `Weight = 9` vs rAthena
`WEIGHTOVER = 9` (matches), `NoCombo = 10` vs rAthena `USESKILL_FAIL = 10` (mismatch), etc. The
enum is a legacy partial renumbering. COMBAT-76 corrected only the two ammo causes it emits
(`NeedMoreBullet = 84`, `NeedEquipmentKunai = 34`); the rest still render wrong strings on a
real client.

## Current state (C#)

- `Core.Server/Packets/Out/ZC/ZC_ACK_TOUSESKILL.cs` — `enum SkillFailCause : byte` with ~30
  values, most not matching `e_useskill_fail_cause`. Two (Kunai/Bullet) are correct (COMBAT-76).
- `Map.Server/Skills/SkillClientService.cs:BroadcastSkillFail` — sends `(byte)cause` unmapped.

## rAthena reference (source of truth)

- `src/map/clif.hpp:402` `enum e_useskill_fail_cause` — the authoritative byte→string map.

## Scope — every sub-system that must be touched

- [ ] Renumber every `SkillFailCause` member to its exact `e_useskill_fail_cause` value (or add the
      missing ones), keeping the C# names. Update all `BroadcastSkillFail(...)` call sites that used a
      now-renumbered name (names stay the same, so most are source-compatible).
- [ ] Add a regression test pinning a representative subset to the rAthena numbers.

## Done criteria

- Every emitted `SkillFailCause` byte equals rAthena's `e_useskill_fail_cause` value, so the client
  shows the correct fail message.

## Test plan

- `Combat93FailCauseWireTests`: assert the byte values for SP/HP/STUFF/WEIGHT/BULLET/KUNAI/etc.

## Notes / gotchas

- Pure value renumbering — verify no call site relied on a specific (wrong) numeric value rather
  than the name (none should; they all use the named members).
