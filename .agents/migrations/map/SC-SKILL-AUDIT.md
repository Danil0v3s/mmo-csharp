# SC / skill bespoke-math verification log

Per-item parity audit against rAthena. Started 2026-05-26 after every
`*-parity.md` doc reached 0/0 structurally (43/43 docs, every public
function has a canonical C# entry point). This doc tracks the **depth**
pass — does each entry point actually do the rAthena math?

## Method

For every row:

1. Read rAthena's source body. SCs: `status.cpp`'s `status_change_start`
   switch arm + `status_calc_*` consumer reads. Skills: family `.cpp` in
   `src/map/skills/`.
2. Read our C# implementation. SCs: `Map.Server/Status/StatusEffectRegistry.cs`.
   Skills: `Map.Server/Skills/Behaviors/<Family>/*.cs`.
3. Compare. If formulas match, mark ✓. If divergent, fix + add a unit
   test pinning the rAthena formula, mark 🔧.
4. If complex enough to need a follow-on, mark 🚩 with a one-line note
   on what to investigate.

## Status legend

| Mark | Meaning |
|---|---|
| ✓ | verified-match — C# port matches rAthena byte-for-byte |
| 🔧 | divergent at audit, fixed in this pass (commit cited) |
| 🚩 | divergent at audit, follow-on needed (note inline) |
| ⏳ | queued for audit (not yet started) |

## Wave 96 — Initial high-impact SC batch (2026-05-26)

### Status changes

| SC | Verdict | rAthena | C# | Note |
|---|---|---|---|---|
| `SC_MARIONETTE` / `SC_MARIONETTE2` | 🔧 | status.cpp:11376–11414, status_calc_str:6782 | StatusEffectRegistry.cs:2346, MarionetteControl.cs | Source +Val1 on every stat (wrong). Fix: pack source.stat/2 into Val3 (str<<16 \| agi<<8 \| vit) / Val4 (int<<16 \| dex<<8 \| luk). Source-side SC subtracts deltas, target-side SC adds deltas (capped at max_parameter - target.stat). Wave 96 commit. |

### Skills

#### Wave 97-skills-novice (9 fixes, commit 25c58b1)

| Skill | Verdict | Note |
|---|---|---|
| HellsDrive (HN_HELLS_DRIVE) | 🔧 | added HN_SELFSTUDY_SOCERY +4·lv mastery + 70% RULEBREAK boost |
| GroundGravitation (HN_GROUND_GRAVITATION) | 🔧 | mastery +2·lv + 50% RULEBREAK |
| JackFrostNova (HN_JACK_FROST_NOVA) | 🔧 | mastery +3·lv + 70% RULEBREAK |
| JupitelThunderstorm (HN_JUPITEL_THUNDER_STORM) | 🔧 | mastery +3·lv + 70% RULEBREAK |
| MeteorStormBuster (HN_METEOR_STORM_BUSTER) | 🔧 | mastery +5·lv + 50% RULEBREAK |
| NapalmVulcanStrike (HN_NAPALM_VULCAN_STRIKE) | 🔧 | mastery +4·lv (amp×2) + 40% RULEBREAK |
| DoubleBowlingBash (HN_DOUBLEBOWLINGBASH) | 🔧 | HN_SELFSTUDY_TATICS +3·lv (TATICS has no RULEBREAK path) |
| MegaSonicBlow (HN_MEGA_SONIC_BLOW) | 🔧 | TATICS +5·lv (existing HP<50% ×2 retained) |
| ShieldChainRush (HN_SHIELD_CHAIN_RUSH) | 🔧 | TATICS +3·lv |
| SpiralPierceMax (HN_SPIRAL_PIERCE_MAX) | 🔧 | TATICS +3·lv + size multiplier ×1.5/1.3/1.2 |

Remaining Novice 🚩 (FirstAid is the only ✓; HelpAngel still needs party splash).

#### Wave 97-skills-summoner (4 fixes, commit 7d7aa49)

| Skill | Verdict | Note |
|---|---|---|
| HyunrokCannon (SH_HYUN_ROK_CANNON) | 🔧 | mastery ×50 + Hyun Rok Commune (+400·lv + 25·mastery) |
| HyunrokBreeze (SH_HYUN_ROKS_BREEZE) | 🔧 | mastery ×20 + Commune (+100 + 200·lv + 20·mastery) |
| ChulhoSonicClaw (SH_CHUL_HO_SONIC_CLAW) | 🔧 | mastery ×50 + Chul Ho Commune (+400·lv + 50·mastery) |
| HogogongStrike (SH_HOGOGONG_STRIKE) | 🔧 | mastery ×10 + Commune (+70 + 150·lv + 10·mastery) |

Remaining Summoner 🚩: SU_SPIRITOFLIFE/LAND multipliers (need skill-tree query + SC integration), KisulWaterSpraying / KisulRampage (heal formula + AP enum gating), party splash on Hiss / MeowMeow / Purring / festival skills (party fan-out service shape).

## Queue

Highest-impact SCs / skills to audit next, in rough priority:

1. `SC_PROVIDENCE` (Crusader race-resist) — verify formula in DamageService matches battle.cpp Providence branch.
2. `SC_BLESSING` — stat-up + caster's blessed-undead damage flip.
3. `SC_INCREASEAGI` / `SC_DECREASEAGI` — exact Agi/Aspd deltas.
4. `SC_ANGELUS` — VIT * Val1 % MaxHp bonus.
5. `SC_KYRIE` / `SC_HIGH_KYRIE` — barrier HP + max-hits calculation.
6. `SC_ASSUMPTIO` / `SC_ASSUMPTIO5` — flat damage % reduction.
7. `SC_MAGNIFICAT` — SP regen multiplier.
8. `SC_GLORIA` — +30 LUK timing.
9. `SC_QUAGMIRE` — Agi/Dex/Aspd debuff %.
10. `SC_SLOW_GRACE` — Aspd / Speed % from BD_LULLABY family.

After SCs, queue first skill batch:

1. `AL_HEAL` — Acolyte Heal magnitude formula.
2. `AL_BLESSING` — vit/dex/agi gain per level.
3. `AL_INCAGI` — agi gain per level.
4. `BS_HAMMERFALL` — stun rate, damage formula.
5. `SM_BASH` — % damage per level, hit count.
6. `MG_FIREBOLT` / `_COLDBOLT` / `_LIGHTNINGBOLT` — Matk multiplier per level.
7. `AC_DOUBLE` — damage % per level (Archer).
8. `KN_BOWLINGBASH` — multi-hit ratio + stun chance.
9. `TF_STEAL` — base success rate formula vs target dex/lv.
10. `WZ_METEOR` — meteor count + damage per level.

## History

### 2026-05-26 — Wave 96 starting

Verification pass kickoff. Sets the pattern: one SC / one skill at a
time, citation to rAthena line, fix + test if divergent, log here.
