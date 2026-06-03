# COMBAT-93 — Reconcile SkillFailCause with rAthena e_useskill_fail_cause wire values

> **Epic:** combat · **Status:** ✅ Done (2026-06-03) · **Size:** S · **Player-visible:** yes
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

- [x] Renumbered every `SkillFailCause` member to its exact `e_useskill_fail_cause` value
      (clif.hpp:402), keeping the C# names (zero call-site churn — the 6 emitted members keep their
      names): `SkillFail=0`, `SpInsufficient=1`, `HpInsufficient=2`, `Stuff=3` (was 15), `Delay=4`,
      `ZenyInsufficient=5`, `WrongWeapon=6`, `NoRedJewel=7`, `NoBlueJewel=8`, `Weight=9`,
      `NoEnemy=11` (was 12, TOTARGET), `Skill=16` (was 17), `NeedHelpers=17` (was 20),
      `SummonNone=20` (was 26), `NeedEquipmentKunai=34`, `State=57` (CART, was 16), `NeedItem`/`Item=71`,
      `NeedEquipment=72` (was 21), `NoCombo=73` (COMBOSKILL, was 10), `NoSpiritualSphere=74` (SPIRITS,
      was 13), `NeedMoreBullet=84`, `Coin=85` (COINS, was 23). The 6 C#-invented causes with no
      rAthena equivalent (`NoMemo`/`StealCoin`/`UndeadId`/`InvokerNotConfirm`/`Amount`/`Sight`) fall
      back to the generic `USESKILL_FAIL_LEVEL=0` ("skill failed") — the correct client behavior for
      an unmapped cause (all 6 are unused; kept for source compat).
- [x] Added `Combat93FailCauseWireTests` (24) pinning the rAthena wire values + the 6 emitted causes.

## Done criteria

- ✅ Every emitted `SkillFailCause` byte equals rAthena's `e_useskill_fail_cause` value (the 6
  actually-sent causes — SkillFail 0, Skill 16, NeedHelpers 17, SummonNone 20, NeedEquipmentKunai 34,
  NeedMoreBullet 84 — are all exact, fixing the 3 that were wrong), so the client shows the correct
  fail message. The full enum is reconciled.

## Test plan

- ✅ `Combat93FailCauseWireTests`: a 22-row Theory pinning SP/HP/STUFF/WEIGHT/SUMMON_NONE/BULLET/
  KUNAI/COMBOSKILL/SPIRITS/COINS/… to the rAthena numbers + facts for the emitted set and the
  generic-fallback set.

## Notes / gotchas

- Pure value renumbering — verified (build + 4235/167/111 tests) that no call site relied on a
  specific (wrong) numeric value rather than the name. Only 6 members are emitted; the rest were
  defined-but-unused.
- The 6 orphan causes mapping to 0 is the faithful treatment, not a caveat: rAthena has no
  corresponding `e_useskill_fail_cause`, so the generic "skill failed" string is the correct render.

## History

- 2026-06-03 — Reconciled `SkillFailCause` to rAthena `e_useskill_fail_cause` (clif.hpp:402): every
  member now carries its exact wire value (keeping the C# names → zero call-site churn). Fixed the 3
  wrong emitted causes (Skill 17→16, NeedHelpers 20→17, SummonNone 26→20) + ~14 unused ones; the 6
  C#-invented causes with no rAthena equivalent fall back to the generic LEVEL=0. Added
  `Combat93FailCauseWireTests` (24). Build clean; Core.Server.Tests 111 + Map.Server.Tests 4235 (1
  fail = pre-existing INFRA-11 replay gate) + Char.Server.Tests 167 green. No follow-ups.
