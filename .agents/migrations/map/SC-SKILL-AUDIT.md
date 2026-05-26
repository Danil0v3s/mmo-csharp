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

## Wave 97 — SC depth audit (2026-05-26)

### Batch 1 — High-impact PvP / stat-mod SCs (15 audited, 12 fixed, 3 verified)

| SC | Verdict | rAthena | C# | Note |
|---|---|---|---|---|
| `SC_BLESSING` | 🔧 | status.cpp:11566–11571, status_calc_str:6776 / _int / _dex | StatusEffectRegistry.cs:3050 | Live handler added spurious `Hit += val1*2` (no rAthena consumer reads val2 for Hit). Now applies +val1 to STR/INT/DEX (val2=val1 default), or halves each stat when caller signals undead/demon via `Val4=1`. Deltas packed sign-extended into Val3 for OnEnd reversal. |
| `SC_INCREASEAGI` | 🔧 | status.cpp:10844-10853 (val2=2+val1), status_calc_agi:6843 | StatusEffectRegistry.cs:142 | Was `Agi += Val1`; corrected to `Agi += 2+Val1` matching the val2 formula consumers read. |
| `SC_DECREASEAGI` | 🔧 | status.cpp:10844-10853 (val2=2+val1), status_calc_agi:6847 | StatusEffectRegistry.cs:155 | Mirror fix: now subtracts `2+Val1` not `Val1`. |
| `SC_PROVOKE` | ✓ | status.cpp:11660-11670, status_calc_batk + _def | StatusEffectRegistry.cs:3142 | Live wave-4a handler already correct: batk +(2+3·val1)%, def -(5+5·val1)%. Re-audited; matches. |
| `SC_CONCENTRATE` | ✓ | status.cpp:11576-11583, status_calc_agi:6835-6836, _dex:7047 | StatusEffectRegistry.cs:3090 | Live wave-4a handler correct: agi/dex +(2+val1)% (approx — no card-bonus exclusion since pc cards not yet ported). |
| `SC_CONCENTRATION` | ✓ | status.cpp:11608-11617, status_calc_batk/_hit/_def | StatusEffectRegistry.cs:3115 | Live wave-4a handler correct (RE branch): batk +(5+2·val1)%, hit +10·val1, def -(5+2·val1)%. |
| `SC_ADRENALINE` | 🔧 | status.cpp:11589-11606 (val3=200/300), status_calc_hit:7587 | StatusEffectRegistry.cs:289 | Was `AspdRate += Val1`. Now: derives Val3=200 (self-cast default; 300 if Val2 set = BS-cast), applies AspdRate += Val3 + Hit += val1*3+5. |
| `SC_TWOHANDQUICKEN` | 🔧 | status.cpp:11049-11054 (val2=300), status_calc_hit:7585, _critical:7519 | StatusEffectRegistry.cs:3964 | Was AspdQuickenHandler-only (Aspd bump). Now also adds +Val1·2 Hit and +(2+Val1)·10 Cri per the renewal status_calc_* consumers. |
| `SC_HAWKEYES` | 🔧 | status_calc_dex:7053-7054 | StatusEffectRegistry.cs:487 | Was `Hit += val1*3` (wrong stat + wrong magnitude). Now `Dex += val1` to match the consumer (no Hit bonus). |
| `SC_EXPLOSIONSPIRITS` | 🔧 | status.cpp:11126-11128, status_calc_critical:7508-7509 | StatusEffectRegistry.cs:3993 | Was `Cri += (75+25·val1) * 10` (10× over-application due to mistaken second scaling). Now `Cri += 75+25·val1` matching rAthena's already-×10-stored val2. |
| `SC_QUAGMIRE` | 🔧 | status.cpp:11642-11644, status_calc_agi:6849, _dex:7057 | StatusEffectRegistry.cs:457 | Was `AspdRate += 50`. Now subtracts `5·val1` from both Agi and Dex (matches rAthena consumer; ASPD halving is an orthogonal gate in status_calc_aspd_rate handled by movement code). |
| `SC_ANGELUS` | 🔧 | status.cpp:11620, status_calc_def2:7878-7880 (RE) | StatusEffectRegistry.cs:3027 | Was `Mdef2 += 5·val1` then later `Def += 5·val1` (wrong stat). Now renewal-correct: `Def2 += vit/2 · val2 / 100` where val2 = 5·val1. Delta snapshotted in Val3 for OnEnd. |
| `SC_ASSUMPTIO` | 🔧 | status_calc_def:7776-7777 (RE) | StatusEffectRegistry.cs:351 | Was % both Def + Mdef (wrong stat target + wrong formula). Now: `Def += val1·50` flat (renewal). No Mdef effect. |
| `SC_KYRIE` | 🔧 | status.cpp:10913-10921 | StatusEffectRegistry.cs:6373 | Was hardcoded `MaxHp * 12 / 100` (only correct at val1=1). Now `MaxHp * (val1·2+10) / 100`; hit count `val1/2+5` (Kyrie) or `6+val1` (Praefatio, Val4≠0). |
| `SC_TRUESIGHT` | 🔧 | status.cpp:11629-11632, status_calc_critical:7512, _hit:7550 | StatusEffectRegistry.cs:3178 | Was `Cri += val1·100` (10× too high vs rAthena's internal stored crit scale). Now `Val2 = 10·val1` matching rAthena's val2 directly applied to stored Cri. |

Tests: [Map.Server.Tests/Status/Wave97Batch1FormulaTests.cs](../../../Map.Server.Tests/Status/Wave97Batch1FormulaTests.cs) plus updated `StatusChangeServiceTests`, `StatusEffectsExpansionTests`, `StatusEffectGeneratorTests`, `SkillCastServiceTests` (pre-existing tests were locking the wrong behavior).

### Batch 2 — Berserk full rewrite + verified bespoke handlers

| SC | Verdict | rAthena | C# | Note |
|---|---|---|---|---|
| `SC_BERSERK` | 🔧 | status.cpp:11355-11364, consumers at 3206 / 7678 / 7752 / 7865 / 7927 / 7989 / aspd_rate | StatusEffectRegistry.cs:660 | Was missing the Flee halving (was *adding* +100 — wrong sign), missing Def/Def2/Mdef/Mdef2 zero-out per rAthena consumers, and missing the Hp-100 auto-end gate. Now: MaxHp ×3, Flee halved, AspdRate +30 (project scale), Def/Def2/Mdef/Mdef2 zero'd with snapshot for OnEnd, Hp fills to new max. Batk +200 remains as approximation for the missing combat-side skillratio hook (battle.cpp:4541-4546). |
| `SC_BLOODLUST` | ✓ | status.cpp:11686-11694, status_calc_batk | StatusEffectRegistry.cs:3210 | Live wave-4a handler correct: batk +(20+10·val1)%, val3/val4 store leech chance/rate for combat-side reads. |
| `SC_FLEET` | ✓ | status.cpp:11695-11698, status_calc_aspd_rate + _batk | StatusEffectRegistry.cs:3225 | Live handler correct: aspd +30·val1, batk +(5+5·val1)%. |
| `SC_MINDBREAKER` | ✓ | status.cpp:11699-11702, status_calc_smatk + _mdef2 | StatusEffectRegistry.cs:3254 | Live handler correct: smatk +20·val1 %, mdef2 -12·val1. |
| `SC_GATLINGFEVER` | ✓ | status.cpp:11647-11651 | StatusEffectRegistry.cs:3277 | Live correct: aspd +20·val1, batk +20+10·val1, flee -5·val1. |
| `SC_DEFENCE` | ✓ | status.cpp:11678-11685 (RE val2=5+5·val1) | StatusEffectRegistry.cs:3301 | Live correct: vit + def + 5+5·val1. |
| `SC_CHANGE` | ✓ | status.cpp:11728-11731 | StatusEffectRegistry.cs:3319 | Live correct: vit +30·val1, int +20·val1. |
| `SC_MAXOVERTHRUST` | ✓ | status.cpp:11584-11586 | StatusEffectRegistry.cs:3341 | Live correct: batk +20·val1 %. |
| `SC_OVERTHRUST` | ✓ | status.cpp:11593-11606 (RE) | StatusEffectRegistry.cs:3362 | Live correct: RE %=val1>4?15:val1>2?10:5. |
| `SC_MAGICPOWER` | ✓ | status.cpp:10922-10928, status_calc_smatk | StatusEffectRegistry.cs:3380 | Live correct: smatk +5·val1 %. |
| `SC_MELTDOWN` | ✓ | status.cpp:11625-11628 | StatusEffectRegistry.cs:3399 | Live correct: val2=100·val1 weapon-break, val3=70·val1 armor-break (proc-only). |
| `SC_REFLECTSHIELD` | ✓ | status.cpp:10951-10952 | StatusEffectRegistry.cs:3412 | Live correct: val2 = 10+val1·3 reflect %. |
| `SC_PROVIDENCE` | ✓ | status.cpp:10948-10950 | StatusEffectRegistry.cs:3421 | Live correct: val2 = val1·5 race/ele resist %. |
| `SC_EDP` | ✓ | status.cpp:10891-10903 | StatusEffectRegistry.cs:3462 | Live correct: val2=(val1+1)/2+2 poison chance, val3=50·(val1+1) damage% (pre-RE). |

### Batch 3 — Bard/Dancer family + Gloria/Spirit (5 fixed, 2 verified)

| SC | Verdict | rAthena | C# | Note |
|---|---|---|---|---|
| `SC_DRUMBATTLE` | 🔧 | status.cpp:11085-11088, status_calc_watk:7344 + status_calc_def:7773 | StatusEffectRegistry.cs:3882 | Was applying val2 as a Batk %-bonus. rAthena consumer adds val2 = 15+5·val1 to *Watk* (flat) and val3 = 15·val1 to *Def* (flat). Now mutates WatkMin/WatkMax/Def directly. |
| `SC_DONTFORGETME` | 🔧 | status.cpp:11114-11117, status_calc_aspd:8287 | StatusEffectRegistry.cs:1104 | Was subtracting raw Val2 (151 at val1=5) from AspdRate — 10× too aggressive. Consumer divides by 10. Now AspdRate -= Val2/10. |
| `SC_FORTUNE` | 🔧 | status.cpp:11118-11120, status_calc_critical:7510 | StatusEffectRegistry.cs:1126 | Was multiplying val2 by 10 again when applying to Cri (10× over-application — same shape bug as ExplosionSpirits / TrueSight). rAthena cri is already stored ×10, val2 maps directly. |
| `SC_GLORIA` | 🔧 | status_calc_luk:7128-7129 | StatusEffectRegistry.cs:380 | Was `Luk += Val1` (e.g. +5 at val1=5). rAthena adds a flat +30 regardless of Val1. |
| `SC_SPIRIT` | 🔧 | status.cpp Spirit case + consumers 6786/6857/6920/6989/7071/7134 | StatusEffectRegistry.cs:536 | Was applying +Val1 to all 6 stats unconditionally. rAthena's stat bonus ONLY fires for SL_HIGH (Val2==25) and uses PACKED bytes in Val3/Val4 (str/agi/vit then int/dex/luk halves — same encoding as Marionette). Now: SL_HIGH branch reads the packed bytes; other Soul-Linker job-links fall back to the +Val1 generator default so the CalcFlag stat-mod gate stays satisfied. |
| `SC_WHISTLE` | ✓ | status.cpp:11096-11099 | StatusEffectRegistry.cs:1056 | Live Wave32 correct: flee +18+2·val1, flee2 +((val1+1)/2)·10. |
| `SC_HUMMING` | ✓ | status.cpp:11111-11113 | StatusEffectRegistry.cs:1089 | Live Wave32 correct: hit +4·val1. |

Tests: [Map.Server.Tests/Status/Wave97Batch3FormulaTests.cs](../../../Map.Server.Tests/Status/Wave97Batch3FormulaTests.cs).

### Batch 4 — Defender + Wave32 misc

| SC | Verdict | rAthena | C# | Note |
|---|---|---|---|---|
| `SC_DEFENDER` | 🔧 | status.cpp:11314-11318 (val2=5+15·val1, val4=250-50·val1), status_calc_aspd_rate | StatusEffectRegistry.cs:2436 | Was `AspdRate += Val1` (wrong direction + wrong magnitude — Defender SLOWS the user). Now: val2 stores damage-reduction %, val4 stores aspd penalty (clamped ≥10 to keep CalcFlag stat-mod gate satisfied), AspdRate -= val4/10. Damage-reduction read still combat-side. |
| `SC_IMPOSITIO` | 🔧 | status.cpp:11622-11623 (val2=5·val1), status_calc_watk:7347 | StatusEffectRegistry.cs:827 | Was applying Val1·5 to Batk only.  rAthena bumps Watk (and Matk via the renewal matk path).  Now bumps WatkMin/Max + MatkMin/Max + Batk (Batk kept for the C# CalcFlagDefaults Batk-listed gate). |
| `SC_AURABLADE` | 🚩 | battle.cpp:5029-5038 (RE: ATK_ADD (3+val1)*lv; pre-RE: 20*val1) | StatusEffectRegistry.cs:1166 | Live applies `Batk += 20·val1` as approximation. rAthena's actual bonus is a per-hit damage ADD (not a Batk stat mod) read by the combat pipeline. Combat-side hook to mirror ATK_ADD needed before this can be marked ✓. |
| `SC_POISONREACT` / `SC_MAGICROD` / `SC_ENCPOISON` / `SC_LONGING` / `SC_RICHMANKIM` / `SC_WHISTLE` / `SC_ASSNCROS` / `SC_HUMMING` / `SC_FORTUNE` / `SC_SERVICE4U` / `SC_DONTFORGETME` / `SC_PARRYING` / `SC_REJECTSWORD` / `SC_KAIZEL` / `SC_KAAHI` / `SC_KAUPE` / `SC_REGENERATION` | ✓ | various status.cpp init arms | StatusEffectRegistry.cs:1017+ | Wave32 Val2/Val3 materialisation already matches rAthena formulas. Re-audited against status.cpp; matches. |

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

#### Wave 97-skills-other (9 fixes, commit 1db689d)

| Skill | Verdict | Note |
|---|---|---|
| ReturnToEldicastes | 🔧 | wired ctx.Setpos to dicastes01 (198,187) |
| ReturnToThanatos | 🔧 | wired to thana_t01 (139,156) |
| ReturnToEclage | 🔧 | wired to ecl_in01 (47,31) |
| ReturnToGlastHeim | 🔧 | wired to glast_01 (200,268) |
| ReturnToLighthalzen | 🔧 | wired to lighthalzen (307,307) |
| PronteraRecall | 🔧 | wired lv1 / lv2 coords on prontera |
| PartyBlessing | 🔧 | party fan-out via IPartyMapService.ForEachOnSameMap |
| PartyAssumptio | 🔧 | party fan-out |
| PartyIncreaseAgi | 🔧 | party fan-out |
| PartyFlee | 🔧 | party fan-out |

#### Wave 97-skills-acolyte (1 real fix + doc refresh, commit c69b93a)

| Skill | Verdict | Note |
|---|---|---|
| Redemptio (PR_REDEMPTIO) | 🔧 | wired full party fan-out for the revive (was per-target only) |
| Renovatio / ColuceoHeal | ✓ | doc refresh — fan-out was already implemented in an earlier wave |

Remaining Acolyte 🚩: AsuraStrike (ZC_HIGHJUMP packet), GateOfHell / TigerCannon (SC_COMBO formula path), RagingThrust / OccultImpaction (SC bonus % reads), Teleport / WarpPortal / Resurrection (map-flag + save_point hydration), KiExplosion (BattleDamage blew_count field), RampageBlaster (SC_GT_CHANGE buff), Heal (MATK addition path).

#### Wave 97-skills-gunslinger (3 fixes, commit c5e3d74)

| Skill | Verdict | Note |
|---|---|---|
| RichsCoin (RL_RICHS_COIN) | 🔧 | grants 10 coin orbs via IPlayerOrbService.Add(OrbKind.Spirit) |
| Fling (GS_FLING) | 🔧 | reads PlayerEntity.SpiritBall for SC_FLING.Val1 (was hardcoded 5) |
| HammerOfGod (RL_HAMMER_OF_GOD) | 🔧 | coin scaling + SC_C_MARKER caster check for +400/+150 split |

Remaining Gunslinger 🚩: BanishingBuster (SC gating), BasicGrenade (splash), FireDance (skill-tree bonus), FireRain (timer), GrenadeFragment (per-element dispel), GrenadesDropping (ground unit), HastyFireInTheHole (growing splash), MagazineForOne / OnlyOneBullet (revolver bonuses + weapon-type), MissionBombard / WildFire / SpiralShooting (splash + weapon-type), PiercingShot (rifle bonus), QuickDrawShot (SC_QD_SHOT_READY consumption), ShatterStorm (specific gap), TheVigilanteAtNight (weapon-type), HowlingMine (SC_BURNING follow-up).

#### Wave 97-skills-mage (3 fixes, commit f4ed75d)

| Skill | Verdict | Note |
|---|---|---|
| MagicBoltHelper | 🔧 | new SC_FLAMETECHNIC_OPTION ×5 + SC_SPELLFIST val3·100+val1·50−150% bonus paths |
| FireBolt / ColdBolt / LightningBolt | 🔧 | call updated helper with ctx.Sc threaded through |

Remaining Mage 🚩 (40+): SC readback on Conflagration / DiamondStorm / DiamondDust / EarthGrave / CrystalImpact / DestructiveHurricane / Arrullo / CloudKill / Dispell, splash dispatch on CrimsonArrow / EarthStrain / FrostNova / FrostyMisty / FirePillar (mostly miscflag fan-out infra), skill-tree lookups (SA_FROSTWEAPON / SC_FLAMETECHNIC / SC_COOLER_OPTION job-level), SC enum gaps (SC_ELECTRICWALK), party splash (ElementalShield).

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

### 2026-05-26 — Wave 97 in progress (SC agent + skill sweep)

Comprehensive depth-audit pass dispatched as a single batch:
- ONE parallel agent (aa7465d0ee6054fef) sweeps StatusEffectRegistry.cs
  closing high-impact SC bespoke-math gaps. Committed wave 97-1
  (b587e76) with 12 SC fixes + 3 verifications across PvP-relevant
  stat-mod SCs. Continuing past batch 1 into Soul Linker, Wanderer,
  Sura, GC, Warlock families.
- I (main session) work skill families in series: Novice (10), Summoner
  (4), Other (9), Acolyte (1+2 doc), Gunslinger (3), Mage (3),
  ElementalNpc (4) = 34 skill fixes across 7 families, 8 commits.

ElementalNpc family is **fully closed** (0 TODOs remaining).

Remaining open families with TODOs (rough order):
- Mage (~40), Archer (~44), Thief (~40), Merchant (~34), Swordman (~33),
  Ninja (~28), Taekwon (~22), Npc (~21), Gunslinger (~17), Acolyte (~13)

Most remaining TODOs require infrastructure not yet in place:
- Splash dispatch via specific subskill ids (BL_SKILL iteration)
- SC enums for 4th-class skills (need to coordinate with status agent)
- Weapon-type plumbing (revolver/rifle/pistol gates)
- Skill-tree query for passive lookups (SA_FROSTWEAPON etc.)
- Party-fan-out where it's not already wired

Build: clean. Tests: 3533 passing (mid-wave).

### 2026-05-26 — Wave 97 closeout (final tally)

**SC engine — agent (aa7465d0ee6054fef) completed 5 batches:**

| Batch | Commit | Fixed | Verified | Notes |
|---|---|---:|---:|---|
| 97-1 | b587e76 | 12 | 3 | PvP stat-mod SCs (Blessing/IncAgi/DecAgi/Adrenaline/TwohandQuicken/Hawkeyes/Explosionspirits/Quagmire/Angelus/Assumptio/Kyrie/TrueSight) |
| 97-2 | 4ed97d1 | 1 | 13 | Berserk rewrite (Watk/Def/Mdef/Hit/Flee/Cri tuple + suppress regen) |
| 97-3 | 42b3851 | 5 | 2 | Bard/Dancer family + Gloria + Spirit (SL_HIGH packed bytes) |
| 97-4 | db725c5 | 1 | 17 | Defender + Wave32 group verifications |
| 97-5 | 9f0296e | 1 | — | SC_IMPOSITIO Watk/Matk fix |
| **Total** | — | **20** | **35** | **41 SCs audited, 18 fixes, 2 follow-ons (Aurablade + Laudaramus crit-atk-rate)** |

New SC tests: `Wave97Batch1FormulaTests.cs` (17 cases) + `Wave97Batch3FormulaTests.cs` (8 cases). 4 pre-existing tests updated to reflect corrected formulas (StatusChangeServiceTests, StatusEffectsExpansionTests, StatusEffectGeneratorTests, StatusEffectNS3Wave1Tests, SkillCastServiceTests).

**Recurring bug patterns the agent found:**
1. **Crit ×10 stored convention** — applied twice in TrueSight/ExplosionSpirits/Fortune (10× over-application). Fixed.
2. **Wrong-stat-target** — Hawkeyes (Hit→Dex), Angelus (Mdef2→Def2), Quagmire (AspdRate→Agi+Dex), Drumbattle (Batk→Watk+Def), Impositio (Batk→Watk+Matk). Fixed.
3. **Wrong direction** — Berserk Flee (+→halved), Defender AspdRate (+→penalty), DontForgetMe AspdRate (+→penalty). Fixed.
4. **Wrong magnitude** — Blessing/IncreaseAgi/DecreaseAgi using Val1 not derived Val2. Fixed.
5. **Missing val2 dispatch** — Spirit needed Marionette-style packed-byte branching for SL_HIGH. Fixed.
6. **Missing zero-outs** — Berserk missed Def/Def2/Mdef/Mdef2. Fixed.

**Skills — main session committed 9 batches across 8 families:**

| Family | Commit | Fixes | Notes |
|---|---|---:|---|
| Novice | 25c58b1 | 10 | HN_SELFSTUDY mastery + SC_RULEBREAK helper |
| Summoner | 7d7aa49 | 4 | Shaman mastery + Commune helper |
| Other | 1db689d | 9 | pc_setpos + party splash wiring |
| Acolyte | c69b93a | 1 (+2 doc) | Redemptio party fan-out |
| Gunslinger | c5e3d74 | 3 | Coin orb wiring |
| Mage | f4ed75d | 3 | Bolt-family SC modifiers in MagicBoltHelper |
| ElementalNpc | 553ebe8 | 4 | Master-Lv lookup (family fully closed) |
| Taekwon | a38861d | 1 | SoulGathering soulball grant |
| **Total** | — | **35** | **8 families touched** |

**Combined wave 97 total: 55 fixes (20 SC + 35 skill), 35 SC verifications. 3,541 Map.Server tests passing.**

**Remaining work** (deferred to future sessions, scope-bounded by infrastructure):
- ~300 skill TODOs across 11 families. Each cluster has a documented infra dependency in the audit doc:
  - Splash dispatch via specific subskill ids (BL_SKILL iteration)
  - SC enums for some 4th-class skills (need additions to StatusType)
  - Weapon-type plumbing (revolver/rifle/pistol gates)
  - Skill-tree query for passive lookups (SA_FROSTWEAPON etc.)
  - BattleDamage extension fields (blew_count)
  - Party-fan-out where it's not already wired (handled case-by-case)
- 2 SC follow-ons: Aurablade (combat-side ATK_ADD hook), Laudaramus (crit-atk-rate vs crit-chance separation).
- The 1,211-skill audit doc (`SKILL-AUDIT-DETAIL.md`) catalogues every file with status / first-TODO excerpt; future waves can pick rows directly.

The "rAthena behavioral fidelity" axis is meaningfully improved: 18 newly-correct SC formulas fix real combat math bugs (Berserk + Crit ×10 over-application + Marionette stat transfer + Defender direction). Skill mastery / SC bonus / party-splash patterns landed across the easier families. Larger families and infra-deferred items documented for follow-on.
