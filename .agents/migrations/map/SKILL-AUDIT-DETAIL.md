# Per-skill audit — generated 2026-05-26

Method: for each C# SkillImpl, compare against the matching rAthena .cpp by name.

Status legend:

- ✓ rAthena .cpp matched, no TODO/pending markers in C#
- 🚩 C# contains TODO/pending/deferred marker — explicit known gap
- ⚠️ C# missing CalculateSkillRatio while rAthena has skillratio formula
- ❓ no matching rAthena .cpp file (likely C#-only NPC or auto-skill)

## Novice family

| Skill | Status | Note |
|---|---|---|
| DoubleBowlingBash | 🚩 | known gap:  HN_SELFSTUDY_TATICS bonus are TODO. |
| FirstAid | ✓ | matched |
| GroundGravitation | 🚩 | known gap:  amp + SC_RULEBREAK boost are TODO. |
| HellsDrive | 🚩 | known gap:  amplifier + SC_RULEBREAK boost are TODO. |
| HelpAngel | 🚩 | known gap:  members when the caster is in a party). Party splash is TODO. |
| JackFrostNova | 🚩 | known gap:  default. HN_SELFSTUDY_SOCERY amp + SC_RULEBREAK boost are TODO. |
| JupitelThunderstorm | 🚩 | known gap:  SC_RULEBREAK boost are TODO. |
| MegaSonicBlow | 🚩 | known gap:  bonus is TODO. |
| MeteorStormBuster | 🚩 | known gap:  SC_RULEBREAK boost are TODO. |
| NapalmVulcanStrike | 🚩 | known gap:  hit. HN_SELFSTUDY_SOCERY amp + SC_RULEBREAK boost are TODO. |
| ShieldChainRush | 🚩 | known gap:  SC_HNNOWEAPON to the caster. HN_SELFSTUDY_TATICS bonus is TODO. |
| SpiralPierceMax | 🚩 | known gap:  (Small 1.5×, Medium 1.3×, Large 1.2×) is TODO once Size is plumbed. |

## Summoner family

| Skill | Status | Note |
|---|---|---|
| Bite | ✓ | matched |
| BlessingofMysticalCreatures | ✓ | matched |
| BunchofShrimp | ✓ | matched |
| CatnipMeteor | 🚩 | known gap:  are TODO. |
| CatnipPowdering | 🚩 | known gap:  Drops the catnip-cell unit; SU_SPIRITOFLAND flee2 buff is TODO. |
| Chattering | ✓ | matched |
| ChulhoSonicClaw | 🚩 | known gap:  Mastery / Chul Ho Communion bonuses are TODO. |
| ColorsofHyunrok | ✓ | matched |
| Grooming | ✓ | matched |
| Hiss | 🚩 | known gap:  party splash is TODO. |
| HogogongStrike | 🚩 | known gap:  Mastery + Chul Ho Communion bonuses are TODO; on-hit damage only |
| HowlingofChulho | ✓ | matched |
| HyunrokBreeze | 🚩 | known gap:  Mastery + Hyun Rok Communion bonuses are TODO. |
| HyunrokCannon | 🚩 | known gap:  Mastery + Hyun Rok Communion bonuses are TODO. |
| KisulRampage | 🚩 | known gap:  range and heals AP to party-mates. AP heal + SC enum are TODO; we |
| KisulWaterSpraying | 🚩 | known gap:  HP on the target (no party splash here). Mastery / CRT plumbing TODO. |
| Lope | ✓ | matched |
| LunaticCarrotBeat | ✓ | matched |
| MarineFestivalofKisul | 🚩 | known gap:  Solo cast applies the festival buff SC (TODO — enum missing); party |
| MeowMeow | 🚩 | known gap:  party splash; SC enum is missing — TODO. |
| NyangGrass | 🚩 | known gap:  Drops the unit at the cast XY. SU_SPIRITOFLAND MATK buff is TODO. |
| PickyPeck | 🚩 | known gap:  SU_SPIRITOFLIFE adds HP-ratio multiplier (TODO — skilltree query). |
| PowerofFlock | ✓ | matched |
| Purring | 🚩 | known gap:  party splash are TODO. |
| SandyFestivalofKisul | 🚩 | known gap:  Solo cast applies the festival buff SC (TODO — enum missing); party |
| ScarofTarou | 🚩 | known gap:  SC_BITESCAR on hit. SU_SPIRITOFLIFE HP-ratio bonus is TODO. |
| Scratch | ✓ | matched |
| SilvervineRootTwist | 🚩 | known gap:  damage tick via SU_SV_ROOTTWIST_ATK is TODO. |
| SilvervineStemSpear | 🚩 | known gap:  is TODO. |
| SpiritofSavage | 🚩 | known gap:  HP-ratio multiplier are TODO. |
| TastyShrimpParty | ✓ | matched |
| TunaBelly | ✓ | matched |
| TunaParty | ✓ | matched |

## MercenaryNpc family

| Skill | Status | Note |
|---|---|---|
| MercenaryArrowRepel | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenaryarrowrepel.cpp) |
| MercenaryArrowShower | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenaryarrowshower.cpp) |
| MercenaryBash | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenarybash.cpp) |
| MercenaryBenediction | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenarybenediction.cpp) |
| MercenaryBlessing | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenaryblessing.cpp) |
| MercenaryBowlingBash | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenarybowlingbash.cpp) |
| MercenaryBrandishSpear | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenarybrandishspear.cpp) |
| MercenaryCompress | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenarycompress.cpp) |
| MercenaryCrash | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenarycrash.cpp) |
| MercenaryDecreaseAgi | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenarydecreaseagi.cpp) |
| MercenaryDoubleStrafe | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenarydoublestrafe.cpp) |
| MercenaryFocusedArrowStrike | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenaryfocusedarrowstrike.cpp) |
| MercenaryFreezingTrap | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenaryfreezingtrap.cpp) |
| MercenaryIncreaseAgility | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenaryincreaseagility.cpp) |
| MercenaryKyrieEleison | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenarykyrieeleison.cpp) |
| MercenaryLandMine | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenarylandmine.cpp) |
| MercenaryLexDivina | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenarylexdivina.cpp) |
| MercenaryMagnificat | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenarymagnificat.cpp) |
| MercenaryMagnumBreak | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenarymagnumbreak.cpp) |
| MercenaryMentalCure | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenarymentalcure.cpp) |
| MercenaryMindBlaster | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenarymindblaster.cpp) |
| MercenaryPierce | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenarypierce.cpp) |
| MercenaryProvoke | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenaryprovoke.cpp) |
| MercenaryRecuperate | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenaryrecuperate.cpp) |
| MercenaryRegain | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenaryregain.cpp) |
| MercenaryRemoveTrap | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenaryremovetrap.cpp) |
| MercenarySacrifice | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenarysacrifice.cpp) |
| MercenarySandman | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenarysandman.cpp) |
| MercenaryScapegoat | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenaryscapegoat.cpp) |
| MercenarySense | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenarysense.cpp) |
| MercenaryShieldReflect | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenaryshieldreflect.cpp) |
| MercenarySight | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenarysight.cpp) |
| MercenarySkidTrap | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenaryskidtrap.cpp) |
| MercenarySpiralPierce | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenaryspiralpierce.cpp) |
| MercenaryTender | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mercenary/mercenarytender.cpp) |

## Other family

| Skill | Status | Note |
|---|---|---|
| Baby | 🚩 | known gap:  mother. Adoption family lookup is TODO; we land the animation. |
| BattleBuster | ✓ | matched |
| CallAllFamily | ✓ | matched |
| CallBaby | ✓ | matched |
| CallParent | ✓ | matched |
| CatCry | ✓ | matched |
| CheerUp | 🚩 | known gap:  + splash are TODO; we apply to the named target. |
| ChristmasCarol | ✓ | matched |
| DualCannonFire | ✓ | matched |
| EquipSwitch | ✓ | matched |
| GmSandman | ✓ | matched |
| GuardiansRecall | ✓ | matched |
| ILookUpToYou | 🚩 | known gap:  MaxSP. SP cost lookup pipeline is TODO; we land the animation. |
| IMissYou | ✓ | matched |
| IWillProtectYou | 🚩 | known gap:  MaxHP. HP cost lookup pipeline is TODO; we land the animation. |
| InfinityBuster | ✓ | matched |
| NetRepair | 🚩 | known gap:  Splash heal of 10% MaxHP to allies. Splash dispatch is TODO; we |
| NetSupport | 🚩 | known gap:  Splash heal of 3% MaxSP to allies. Splash dispatch is TODO; we |
| NiflheimRecall | ✓ | matched |
| OdinsRecall | ✓ | matched |
| OneForever | ✓ | matched |
| OpenBuyingStore | ✓ | matched |
| PartyAssumptio | 🚩 | known gap:  Applies SC_ASSUMPTIO; party splash is TODO. |
| PartyBlessing | 🚩 | known gap:  Applies SC_BLESSING; party splash is TODO. |
| PartyFlee | 🚩 | known gap:  Applies SC_PARTYFLEE; party splash is TODO. |
| PartyIncreaseAgi | 🚩 | known gap:  Applies SC_INCREASEAGI; party splash is TODO. |
| PeonyMamy | ✓ | matched |
| PronteraRecall | 🚩 | known gap:  is TODO. |
| RayOfProtection | 🚩 | known gap:  Buff SC; enum not yet in StatusType — TODO. Animation lands. |
| ReturnToEclage | 🚩 | known gap:  Teleports the caster to eclage_in (47, 31). pc_setpos pipeline is TODO. |
| ReturnToEldicastes | 🚩 | known gap:  Teleports to dicastes (198, 187). pc_setpos is TODO. |
| ReturnToGlastHeim | 🚩 | known gap:  Teleports to glast_01 (200, 268). pc_setpos is TODO. |
| ReturnToLighthalzen | 🚩 | known gap:  Teleports to lighthalzen (307, 307). pc_setpos is TODO. |
| ReturnToThanatos | 🚩 | known gap:  Teleports to thana_t01 (139, 156). pc_setpos is TODO. |
| Ro20thAnniversaryFirecracker | ✓ | matched |
| Sadagui | ✓ | matched |
| SequoiaDust | ✓ | matched |
| SnowFlip | ✓ | matched |
| SummerNightDream | ✓ | matched |
| WeaponEnchantment | 🚩 | known gap:  lookup is TODO; we land the animation. |

## Gunslinger family

| Skill | Status | Note |
|---|---|---|
| AntiMaterialBlast | ✓ | matched |
| BanishingBuster | 🚩 | known gap:  gating is TODO. |
| BasicGrenade | 🚩 | known gap:  bonus is TODO. Splash dispatch is TODO; we land on the target. |
| BindTrap | ✓ | matched |
| Bullseye | ✓ | matched |
| ChainAction | ✓ | matched |
| Cracker | ✓ | matched |
| CrimsonMarker | ✓ | matched |
| Desperado | ✓ | matched |
| Disarm | ✓ | matched |
| DragonTail | ✓ | matched |
| Dust | ✓ | matched |
| FallenAngel | ✓ | matched |
| FireDance | 🚩 | known gap:  Ratio <c>+(100 + 100*lv) + 20*Desperado_lv</c>. Skill-tree bonus is TODO. |
| FireRain | 🚩 | known gap:  + 80 ms-per-wave timers are TODO. |
| Flicker | ✓ | matched |
| Fling | ✓ | matched |
| FullBuster | ✓ | matched |
| Gatlingfever | ✓ | matched |
| Glittering | ✓ | matched |
| GrenadeFragment | 🚩 | known gap:  dispels them all. Per-element SC enums + dispel logic are TODO. |
| GrenadesDropping | 🚩 | known gap:  is TODO; we place a single unit at the cast cell. |
| GroundDrift | ✓ | matched |
| HammerOfGod | 🚩 | known gap:  SC_C_MARKER from caster). Coin tracking is TODO; we use the |
| HastyFireInTheHole | 🚩 | known gap:  splash with growing radius is TODO. |
| HowlingMine | 🚩 | known gap:  splash + SC_BURNING follow-up (TODO). |
| IntensiveAim | ✓ | matched |
| MagazineForOne | 🚩 | known gap:  and revolver weapon bonuses are TODO. |
| MassSpiral | ✓ | matched |
| MissionBombard | 🚩 | known gap:  Splash dispatch + ground unit are TODO. |
| OnlyOneBullet | 🚩 | known gap:  revolver bonus are TODO. |
| PiercingShot | 🚩 | known gap:  Renewal ratio <c>+(100 + 20*lv)</c>; rifle bonus +150+30*lv (TODO). |
| QuickDrawShot | 🚩 | known gap:  dispatch + SC_QD_SHOT_READY consumption are TODO; we run a single |
| RapidShower | ✓ | matched |
| RichsCoin | 🚩 | known gap:  Grants 10 coin orbs to the caster. Coin orb system is TODO. |
| RoundTrip | ✓ | matched |
| ShatterStorm | 🚩 | known gap:  is TODO. |
| SlugShot | ✓ | matched |
| SpiralShooting | 🚩 | known gap:  bonuses are TODO; splash dispatch is TODO. |
| SpreadAttack | ✓ | matched |
| TheVigilanteAtNight | 🚩 | known gap:  uses a different ratio (TODO — needs weapon-type plumbing). |
| Tracking | ✓ | matched |
| TripleAction | ✓ | matched |
| WildFire | 🚩 | known gap:  bonuses + splash dispatch are TODO. |

## Homunculus family

| Skill | Status | Note |
|---|---|---|
| AbsoluteZephyr | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/absolutezephyr.cpp) |
| Avoid | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/avoid.cpp) |
| BenedictionOfChaos | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/benedictionofchaos.cpp) |
| BioExplosion | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/bioexplosion.cpp) |
| BlastForge | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/blastforge.cpp) |
| BlazingAndFurious | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/blazingandfurious.cpp) |
| Caprice | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/caprice.cpp) |
| Castling | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/castling.cpp) |
| Change | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/change.cpp) |
| ContinualBreakCombo | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/continualbreakcombo.cpp) |
| Defense | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/defense.cpp) |
| EraserCutter | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/erasercutter.cpp) |
| EternalQuickCombo | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/eternalquickcombo.cpp) |
| GlanzenSpies | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/glanzenspies.cpp) |
| GoldeneTone | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/goldenetone.cpp) |
| GraniticArmor | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/graniticarmor.cpp) |
| HealingTouch | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/healingtouch.cpp) |
| HeiligePferd | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/heiligepferd.cpp) |
| HolyPole | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/holypole.cpp) |
| LavaSlide | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/lavaslide.cpp) |
| LightOfRegene | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/lightofregene.cpp) |
| MagmaFlow | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/magmaflow.cpp) |
| MidnightFrenzy | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/midnightfrenzy.cpp) |
| Moonlight | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/moonlight.cpp) |
| NeedleOfParalyze | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/needleofparalyze.cpp) |
| NeedleStinger | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/needlestinger.cpp) |
| OveredBoost | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/overedboost.cpp) |
| PainKiller | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/painkiller.cpp) |
| PoisonMist | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/poisonmist.cpp) |
| Pyroclastic | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/pyroclastic.cpp) |
| SBR44 | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/sbr44.cpp) |
| SilentBreeze | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/silentbreeze.cpp) |
| SilverVeinRush | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/silverveinrush.cpp) |
| SonicClaw | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/sonicclaw.cpp) |
| SteelHorn | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/steelhorn.cpp) |
| StoneWall | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/stonewall.cpp) |
| StyleChange | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/stylechange.cpp) |
| SummonLegion | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/summonlegion.cpp) |
| Tempering | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/tempering.cpp) |
| TheOneFighterRises | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/theonefighterrises.cpp) |
| TinderBreaker | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/tinderbreaker.cpp) |
| ToxinOfMandara | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/toxinofmandara.cpp) |
| TwisterCutter | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/twistercutter.cpp) |
| VolcanicAsh | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/volcanicash.cpp) |
| XenoSlasher | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/homunculus/xenoslasher.cpp) |

## ElementalNpc family

| Skill | Status | Note |
|---|---|---|
| AgeOfIce | 🚩 | known gap:  the elemental's master is known. Master-Lv lookup is TODO; we use |
| AquaPlay | ✓ | matched |
| Avalanche | ✓ | matched |
| Blast | ✓ | matched |
| CircleOfFire | ✓ | matched |
| ColdForce | ✓ | matched |
| CoolAir | ✓ | matched |
| Cooler | ✓ | matched |
| CrystalArmor | ✓ | matched |
| CursedSoil | ✓ | matched |
| DeadlyPoison | 🚩 | known gap:  lookup is TODO; using the caster's Lv as a stand-in. |
| DeepPoisoning | ✓ | matched |
| EarthCare | ✓ | matched |
| EyesOfStorm | ✓ | matched |
| FireArrow | ✓ | matched |
| FireBomb | ✓ | matched |
| FireCloak | ✓ | matched |
| FireMantle | ✓ | matched |
| FireWave | ✓ | matched |
| FlameArmor | ✓ | matched |
| FlameRock | 🚩 | known gap:  lookup is TODO; using the caster's Lv as a stand-in. |
| FlameTechnic | ✓ | matched |
| GraceBreeze | ✓ | matched |
| Gust | ✓ | matched |
| Heater | ✓ | matched |
| HurricaneRage | ✓ | matched |
| IceNeedle | ✓ | matched |
| Petrology | ✓ | matched |
| PoisonShield | ✓ | matched |
| PowerOfGaia | ✓ | matched |
| Pyrotechnic | ✓ | matched |
| RockLauncher | ✓ | matched |
| SolidSkin | ✓ | matched |
| StoneHammer | ✓ | matched |
| StoneRain | ✓ | matched |
| StoneShield | ✓ | matched |
| StormWind | 🚩 | known gap:  lookup is TODO; using the caster's Lv as a stand-in. |
| StrongProtection | ✓ | matched |
| TidalWeapon | ✓ | matched |
| Tropic | ✓ | matched |
| TyphoonMissile | ✓ | matched |
| Upheaval | ✓ | matched |
| WaterBarrier | ✓ | matched |
| WaterDrop | ✓ | matched |
| WaterScreen | ✓ | matched |
| WaterScrew | ✓ | matched |
| WildStorm | ✓ | matched |
| WindCurtain | ✓ | matched |
| WindSlasher | ✓ | matched |
| WindStep | ✓ | matched |
| Zephyr | ✓ | matched |

## Ninja family

| Skill | Status | Note |
|---|---|---|
| CastNinjaSpell | ✓ | matched |
| ColdBloodedCannon | ✓ | matched |
| CrimsonFireFormation | 🚩 | known gap:  + 20*charm when fire charms are held (charm bonus TODO). |
| CrimsonFirePetal | ✓ | matched |
| DarkDragonNightmare | ✓ | matched |
| DarkeningCannon | ✓ | matched |
| DistortedCrescent | ✓ | matched |
| EarthCharm | ✓ | matched |
| EmptyShadow | ✓ | matched |
| FinalStrike | ✓ | matched |
| FireCharm | ✓ | matched |
| GoldenDragonCannon | 🚩 | known gap:  SS_ANTENPOU bonus and SC_GROUND_CHARM_POWER +5500 are TODO. |
| HiddenWater | ✓ | matched |
| HuumaShurikenConstruct | ✓ | matched |
| HuumaShurikenGrasp | 🚩 | known gap:  SS_FUUMAKOUCHIKU partner bonus is TODO. |
| IceCharm | ✓ | matched |
| IceMeteor | ✓ | matched |
| IllusionBewitch | 🚩 | known gap:  and swaps positions. Position swap is TODO (skill_check_unit_movepos). |
| IllusionDeath | ✓ | matched |
| IllusionShadow | ✓ | matched |
| IllusionShock | ✓ | matched |
| ImprovisedDefense | 🚩 | known gap: 2*(10*lv) ratio (caster-centred unit + self SC). Self SC is TODO. |
| Infiltrate | 🚩 | known gap:  relocation logic is TODO; this only resolves the hit + self SC. |
| Kamaitachi | 🚩 | known gap:  AoE iteration is TODO. |
| KoCrossSlash | 🚩 | known gap:  SC_JYUMONJIKIRI. Position-shift + double-hit logic is TODO. |
| KunaiDistortion | 🚩 | known gap:  are TODO. |
| KunaiExplosion | 🚩 | known gap:  + Kagemusya scaling are TODO. |
| KunaiNightmare | ✓ | matched |
| KunaiRefraction | 🚩 | known gap:  bonus + detonate iteration is TODO. |
| KunaiRotation | 🚩 | known gap:  Self-SC + paired SS_KUNAIWAIKYOKU placement TODO. |
| KunaiSplash | ✓ | matched |
| LightningStrikeOfDestruction | 🚩 | known gap: +100*lv ratio (charm bonus TODO). POS2 unit placement. |
| Makibishi | ✓ | matched |
| MeltAway | 🚩 | known gap:  <c>+(-100 + 700*lv) + 5*con</c>. Self-SC + blown self are TODO. |
| Mirage | ✓ | matched |
| MirrorImage | ✓ | matched |
| MoonlightFantasy | ✓ | matched |
| NightmareErasion | ✓ | matched |
| OminousMoonlight | ✓ | matched |
| RagingFireDragon | 🚩 | known gap:  (charm bonus TODO). |
| RapidThrow | ✓ | matched |
| RedFlameCannon | 🚩 | known gap:  SS_ANTENPOU partner bonus + SC_FIRE_CHARM_POWER +8500 are TODO. |
| ReleaseNinjaSpell | 🚩 | known gap:  present. Charm consumption is TODO — needs IPlayerOrbsService. |
| ShadowDance | 🚩 | known gap:  SS_KAGEGARI partner bonus + mirage cast + alt-flag scaling TODO. |
| ShadowFlash | 🚩 | known gap:  SS_KAGENOMAI partner bonus + alt-flag scaling are TODO. |
| ShadowHiding | ✓ | matched |
| ShadowHunting | 🚩 | known gap:  SS_KAGEGISSEN partner bonus is TODO. |
| ShadowLeap | ✓ | matched |
| ShadowNightmare | ✓ | matched |
| ShadowSlash | 🚩 | known gap:  Caster slide is TODO. |
| ShadowTrampling | 🚩 | known gap:  SC_KG_KAGEHUMI. Splash iteration is TODO. |
| ShadowWarrior | ✓ | matched |
| SoulCutter | 🚩 | known gap:  SC_SOULFAIRY; ends those SCs on hit (TODO). |
| SpearOfIce | 🚩 | known gap:  We don't have ctx in this hook, so the SC_SUITON test is deferred — TODO. |
| SwirlingPetal | 🚩 | known gap:  and Kagemusya scaling are TODO. |
| ThrowHuumaShuriken | ✓ | matched |
| ThrowKunai | ✓ | matched |
| ThrowShuriken | ✓ | matched |
| ThrowZeny | ✓ | matched |
| ThunderingCannon | 🚩 | known gap:  SS_ANTENPOU partner bonus + SC_WIND_CHARM_POWER +8500 are TODO. |
| VanishingSlash | 🚩 | known gap:  (handled via existing additional-effect chain TODO). |
| WindBlade | 🚩 | known gap: +50 ratio (wind-charm bonus TODO). Magic single hit. |
| WindCharm | ✓ | matched |

## Swordman family

| Skill | Status | Note |
|---|---|---|
| Abundance | 🚩 | known gap:  Requires RK_RUNEMASTERY ≥ 6 (skill-tree check is TODO). Applies |
| AutoBerserk | ✓ | matched |
| Banding | ✓ | matched |
| BanishingPoint | ✓ | matched |
| Bash | ✓ | matched |
| BattleChant | ✓ | matched |
| BowlingBash | ✓ | matched |
| BrandishSpear | 🚩 | known gap:  directional cone splash (map_foreachindir) is TODO — for now we |
| CannonSpear | ✓ | matched |
| ChargeAttack | ✓ | matched |
| CounterAttack | ✓ | matched |
| CrossRain | 🚩 | known gap:  + 7*SPL formula. Holy-S bonus + IG_SPEAR_SWORD_M skill-tree lookup are TODO. |
| CrushStrike | 🚩 | known gap:  Requires RK_RUNEMASTERY ≥ 7 (TODO). Applies SC_CRUSHSTRIKE. |
| DragonBreath | ✓ | matched |
| DragonHowling | 🚩 | known gap:  Splash dispatch is TODO; for now we apply to the named target. |
| DragonicAura | ✓ | matched |
| DragonicBreath | ✓ | matched |
| EarthDrive | 🚩 | known gap:  scales with IG_SHIELD_MASTERY level — TODO. Splash cell wipe is TODO. |
| EnchantBlade | ✓ | matched |
| Endure | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/swordman/endure.cpp) |
| FightingSpirit | ✓ | matched |
| ForceOfVanguard | ✓ | matched |
| GiantGrowth | ✓ | matched |
| GloriaDomini | ✓ | matched |
| GrandCross | ✓ | matched |
| GrandJudgement | 🚩 | known gap:  against Plant / Insect. Imperial-guard checkskill bonus is TODO. |
| GuardianShield | ✓ | matched |
| HackAndSlasher | ✓ | matched |
| HesperusLit | ✓ | matched |
| HolyCross | 🚩 | known gap:  3*lv % chance to apply SC_BLIND. Weapon-type query is TODO; we use |
| HundredSpear | 🚩 | known gap:  val1 ≥ 10, the ratio doubles. SC plumbing into ratio is TODO. |
| IgnitionBreak | 🚩 | known gap:  Ratio <c>+(-100 + 450*lv)</c>. Splash dispatch is TODO; for now we |
| ImperialCross | 🚩 | known gap:  Skill-tree + SC bonuses are TODO. |
| JudgementCross | 🚩 | known gap:  Imperial-guard skilltree bonus is TODO. |
| KingsGrace | ✓ | matched |
| LuxAnima | ✓ | matched |
| MadnessCrusher | 🚩 | known gap:  weapon level bonus and ChargingPierceCount-x2 are TODO (need |
| MagnumBreak | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/swordman/magnumbreak.cpp) |
| MartyrsReckoning | ✓ | matched |
| MilleniumShield | 🚩 | known gap:  Requires RK_RUNEMASTERY ≥ 9 (TODO). Applies SC_MILLENNIUMSHIELD. |
| MoonSlasher | ✓ | matched |
| OverBrand | 🚩 | known gap:  plumbing into ratio are TODO. |
| OverSlash | 🚩 | known gap:  Skill-tree + miscflag-based divisor are TODO. |
| PhantomThrust | ✓ | matched |
| Pierce | 🚩 | known gap:  (SC plumb TODO). Hit chance bonus <c>+5*lv %</c>. Multi-hit count |
| Piety | ✓ | matched |
| PinpointAttack | 🚩 | known gap:  pipeline isn't ported yet so those are TODO. |
| Provoke | ✓ | matched |
| ProvokeSelf | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/swordman/provokeself.cpp) |
| RadiantSpear | 🚩 | known gap:  into the ratio are TODO. |
| RageBurst | 🚩 | known gap:  missing-HP delta; spirit-ball plumbing is TODO. |
| RayOfGenesis | ✓ | matched |
| Refresh | ✓ | matched |
| Relax | 🚩 | known gap:  tick interval). Time2 lookup is TODO. |
| ResistantSouls | 🚩 | known gap:  Applies SC_PROVIDENCE; refuses to target other crusaders (TODO — |
| Sacrifice | 🚩 | known gap:  are TODO; we apply SC_DEVOTION with val1 = caster id. |
| ServantWeapon | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| ServantWeaponDemolition | 🚩 | known gap:  caster's available servant balls — TODO. |
| ServantWeaponPhantom | ✓ | matched |
| ServantWeaponSign | 🚩 | known gap:  Multi-target slot management (MAX_SERVANT_SIGN) is TODO. |
| ShieldBoomerang | ✓ | matched |
| ShieldChain | 🚩 | known gap:  and SC_SHIELD_POWER +50 % multiplier are TODO (need equip access). |
| ShieldPress | 🚩 | known gap:  bonus are TODO. |
| ShieldReflect | ✓ | matched |
| ShieldShooting | 🚩 | known gap:  refine / Shield Mastery bonuses are TODO. |
| ShieldSpell | ✓ | matched |
| Smite | ✓ | matched |
| SonicWave | ✓ | matched |
| SpearBoomerang | ✓ | matched |
| SpearStab | ✓ | matched |
| SpiralPierce | 🚩 | known gap:  is TODO. On hit, 100% SC_ANKLE on non-status-immune targets. |
| StoneHardSkin | ✓ | matched |
| StormBlast | ✓ | matched |
| StormSlash | 🚩 | known gap:  SC_GIANTGROWTH. SC plumb into ratio is TODO. |
| Trample | ✓ | matched |
| TraumaticBlow | ✓ | matched |
| UltimateSacrifice | ✓ | matched |
| VitalStrike | ✓ | matched |
| VitalityActivation | 🚩 | known gap:  Requires RK_RUNEMASTERY ≥ 2 (TODO). Applies SC_VITALITYACTIVATION. |
| WindCutter | 🚩 | known gap:  Weapon-type query is TODO; we use the default 300*lv ratio. |

## Thief family

| Skill | Status | Note |
|---|---|---|
| AbyssDagger | 🚩 | known gap:  SC start happens via castendNoDamage hook (TODO). |
| AbyssSquare | 🚩 | known gap:  ABC_MAGIC_SWORD_M partner bonus is TODO. |
| Antidote | ✓ | matched |
| AutoShadowSpell | ✓ | matched |
| BackSlide | 🚩 | known gap:  200ms unstoppable. Endure-tick grant is TODO; this stub does the |
| BackStab | 🚩 | known gap:  Behind-target slide is TODO. |
| BloodyLust | ✓ | matched |
| BodyPainting | 🚩 | known gap:  <c>53 + 2*lv</c>% to all enemies in range. Dispel + splash are TODO. |
| ChainReactionShot | 🚩 | known gap:  Follow-up ABC_CHAIN_REACTION_SHOT_ATK detonation is TODO. |
| ChaosPanic | ✓ | matched |
| Cloaking | ✓ | matched |
| CloakingExceed | ✓ | matched |
| CloseConfine | ✓ | matched |
| CounterInstinct | ✓ | matched |
| CounterSlash | 🚩 | known gap:  4th-class change_level_4th override is TODO. |
| CreateDeadlyPoison | ✓ | matched |
| CreateNewPoison | ✓ | matched |
| CrossImpact | 🚩 | known gap:  (TODO — skill_check_unit_movepos). |
| CrossRipperSlasher | 🚩 | known gap:  +val1*200 ratio per cutter spin. Cutter prerequisite check is TODO. |
| CrossSlash | 🚩 | known gap:  SC_SHADOW_EXCEED bonus (+60*lv +2*pow) is TODO. |
| DancingKnife | ✓ | matched |
| DarkClaw | 🚩 | known gap:  (handled in additional-effect chain — TODO). |
| DarkIllusion | 🚩 | known gap:  GC_CROSSIMPACT. Teleport + chain are TODO. |
| DeftStab | ✓ | matched |
| Detoxify | ✓ | matched |
| DimensionDoor | ✓ | matched |
| DivestAll | 🚩 | known gap:  service + FCP-failure prompt are TODO. |
| DivestArmor | 🚩 | known gap:  Strips the target's armor. Strip service is TODO. |
| DivestHelm | 🚩 | known gap:  Strips the target's helm. Strip service is TODO. |
| DivestShield | 🚩 | known gap:  Strips the target's shield. Strip service is TODO. |
| DivestWeapon | 🚩 | known gap:  service is TODO — animation only. |
| DoubleAttack | ✓ | matched |
| EmergencyEscape | 🚩 | known gap:  Self-knockback is TODO. |
| EnchantDeadlyPoison | 🚩 | known gap:  status-side renewal effect (TODO). |
| EnchantPoison | ✓ | matched |
| Envenom | ✓ | matched |
| EternalSlash | 🚩 | known gap:  SC_SHADOW_EXCEED. Hit count comes from SC_E_SLASH_COUNT (TODO). |
| FatalMenace | 🚩 | known gap:  has SC_ABYSS_DAGGER. Dagger-bonus div_ + warp on hit are TODO. |
| FatalShadowCrow | 🚩 | known gap:  is TODO. |
| FeintBomb | 🚩 | known gap:  Backslide / retarget are TODO. |
| FindStone | 🚩 | known gap:  Grants 1× Stone (ITEMID_STONE) via pc_additem. Item grant is TODO. |
| FrenzyShot | 🚩 | known gap:  at <c>5*lv</c>% is handled in ModifyDamageData (TODO). |
| FromTheAbyss | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| Grimtooth | ✓ | matched |
| HallucinationWalk | ✓ | matched |
| Hiding | ✓ | matched |
| ImpactCrater | 🚩 | known gap:  scales with SC_ROLLINGCUTTER val1 (TODO). |
| Invisibility | ✓ | matched |
| Maelstrom | ✓ | matched |
| ManHole | ✓ | matched |
| MasqueradeEnervation | ✓ | matched |
| MasqueradeGloomy | ✓ | matched |
| MasqueradeIgnorance | ✓ | matched |
| MasqueradeLaziness | ✓ | matched |
| MasqueradeUnlucky | ✓ | matched |
| MasqueradeWeakness | ✓ | matched |
| MeteorAssault | ✓ | matched |
| Mug | ✓ | matched |
| OmegaAbyssStrike | ✓ | matched |
| PhantomMenace | 🚩 | known gap:  targets. Splash + dispel are TODO — animation only. |
| Poison | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/thief/poison.cpp) |
| PoisonSmoke | 🚩 | known gap:  (gating is TODO). |
| PoisoningWeapon | 🚩 | known gap:  Opens the poison-list selection dialog. Dialog wiring is TODO. |
| Remover | 🚩 | known gap:  Removes graffiti units in splash radius. Graffiti removal is TODO. |
| Reproduce | ✓ | matched |
| RollingCutter | 🚩 | known gap:  stacks SC_ROLLINGCUTTER up to 10 (TODO). |
| SandAttack | ✓ | matched |
| SavageImpact | 🚩 | known gap:  SC_SHADOW_EXCEED bonus +20*lv +2*pow (TODO). |
| Scribble | ✓ | matched |
| ShadowForm | 🚩 | known gap:  granting 4 + skill_lv reflection charges. Linking-id wiring is TODO. |
| ShadowStab | ✓ | matched |
| SightlessMind | ✓ | matched |
| Snatch | 🚩 | known gap:  behaviour is TODO. |
| SonicBlow | 🚩 | known gap:  SL_ASSASIN linked stun bonus is TODO. |
| SoulDestroyer | ✓ | matched |
| Steal | ✓ | matched |
| Stealth | ✓ | matched |
| StoneFling | ✓ | matched |
| StripAccessory | 🚩 | known gap:  Strips the target's accessory. Strip service is TODO. |
| StripShadow | 🚩 | known gap:  Strips shadow gear from the target. Strip service is TODO. |
| ThrowVenomKnife | ✓ | matched |
| TriangleShot | ✓ | matched |
| UnluckyRush | 🚩 | known gap:  Slide + SC_HANDICAPSTATE_MISFORTUNE follow-up are TODO. |
| VenomDust | ✓ | matched |
| VenomPressure | ✓ | matched |
| VenomSplasher | 🚩 | known gap:  AS_POISONREACT partner bonus is TODO. |
| WeaponCrush | 🚩 | known gap:  Strips the target's weapon. Strip service is TODO. |

## Taekwon family

| Skill | Status | Note |
|---|---|---|
| AllInTheSky | ✓ | matched |
| BookofCreatingStar | ✓ | matched |
| CircleOfDirectionsAndElementals | 🚩 | known gap:  <summary>SOA_CIRCLE_OF_DIRECTIONS_AND_ELEMENTALS — Recursive splash; ratio +( |
| Counter | ✓ | matched |
| CurseExplosion | ✓ | matched |
| DawnBreak | 🚩 | known gap:  <summary>SKE_DAWN_BREAK — Recursive splash; ratio +(-100 + 600 + 700*lv) + 5* |
| DocumentofSunMoonAndStar | ✓ | matched |
| DownKick | ✓ | matched |
| Esha | ✓ | matched |
| Eska | ✓ | matched |
| Eske | ✓ | matched |
| Esma | 🚩 | known gap:  gating + SC_STUN punish are TODO. |
| Espa | 🚩 | known gap:  applies SC_USE_SKILL_SP_SPA on caster (TODO). |
| Estin | 🚩 | known gap:  <summary>SL_STIN — Estin. Manual port. Ratio +10*lv vs small targets, -99 oth |
| Estun | ✓ | matched |
| Eswhoo | ✓ | matched |
| Eswoo | ✓ | matched |
| ExorcismOfMaliciousSoul | 🚩 | known gap:  <summary>SOA_EXORCISM_OF_MALICIOUS_SOUL — Recursive splash; ratio +(-100 + 15 |
| FairysSoul | ✓ | matched |
| FalconsSoul | ✓ | matched |
| FallingStar | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| FeelingtheSunMoonandStars | 🚩 | known gap:  <summary>SG_FEEL — Feeling the Sun/Moon/Stars. Map memorisation per skill_lv  |
| FlashKick | 🚩 | known gap:  management (sd-&gt;stellar_mark[]) is TODO — animation + hit only. |
| FullMoonKick | ✓ | matched |
| GolemsSoul | ✓ | matched |
| GravityControl | 🚩 | known gap:  <summary>SJ_GRAVITYCONTROL — Gravity Control. Applies SC_GRAVITYCONTROL with  |
| HatredoftheSunMoonandStars | 🚩 | known gap:  <summary>SG_HATE — Hatred of the Sun, Moon, and Stars. Marks a mob race the S |
| HighJump | 🚩 | known gap:  (4/3 on diagonals). Map-flag gating + cell teleport are TODO. |
| JumpKick | 🚩 | known gap:  (+4%*baseLv, or 8% under SC_SPURT) are TODO. Caster teleport to |
| Kaahi | 🚩 | known gap:  <summary>SL_KAAHI — Kaahi (HP recovery on hit). StatusSkillImpl port; partner |
| Kaite | 🚩 | known gap:  <summary>SL_KAITE — Kaite (magic reflect). StatusSkillImpl port; target gatin |
| Kaizel | ✓ | matched |
| Kaupe | ✓ | matched |
| Kaute | 🚩 | known gap:  <summary>SP_KAUTE — Kaute (SP transfer). Soul-link sharing — partner-link g |
| MidnightKick | ✓ | matched |
| Mission | 🚩 | known gap:  Random-mob pick + script-variable persistence are TODO. |
| NewMoonKick | ✓ | matched |
| NoonBlast | ✓ | matched |
| NovaExplosion | ✓ | matched |
| ProminenceKick | ✓ | matched |
| RisingMoon | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| RisingSun | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| Run | 🚩 | known gap:  Toggles SC_RUN on the target. Walkok re-send is TODO. |
| SevenWind | ✓ | matched |
| ShadowsSoul | ✓ | matched |
| SolarBurst | ✓ | matched |
| SoulCollect | 🚩 | known gap:  <summary>SP_SOULCOLLECT — Soul Collect. Grants soulballs; ball allocation TOD |
| SoulCurse | ✓ | matched |
| SoulDivision | ✓ | matched |
| SoulExplosion | ✓ | matched |
| SoulGathering | 🚩 | known gap:  <summary>SOA_SOUL_GATHERING — Animation only; soulball generation TODO.</summ |
| SoulOfHeavenAndEarth | ✓ | matched |
| SoulRevolution | ✓ | matched |
| SoulUnity | ✓ | matched |
| SpiritofRebirth | ✓ | matched |
| SpiritoftheAlchemist | ✓ | matched |
| SpiritoftheArtist | ✓ | matched |
| SpiritoftheAssasin | ✓ | matched |
| SpiritoftheBlacksmith | ✓ | matched |
| SpiritoftheCrusader | ✓ | matched |
| SpiritoftheHunter | ✓ | matched |
| SpiritoftheKnight | ✓ | matched |
| SpiritoftheMonk | ✓ | matched |
| SpiritofthePriest | ✓ | matched |
| SpiritoftheRogue | ✓ | matched |
| SpiritoftheSage | ✓ | matched |
| SpiritoftheSoulLinker | ✓ | matched |
| SpiritoftheStarGladiator | ✓ | matched |
| SpiritoftheSupernovice | 🚩 | known gap:  Status-only buff; 1% chance to erase die counter on success (TODO). |
| SpiritoftheWizard | ✓ | matched |
| StarBurst | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| StarCannon | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| StarEmperorAdvent | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| StormKick | 🚩 | known gap:  +60 + 20*lv ratio; splash via map_foreachinshootrange (TODO). |
| SunsetBlast | ✓ | matched |
| TalismanOfBlackTortoise | ✓ | matched |
| TalismanOfBlueDragon | ✓ | matched |
| TalismanOfFiveElements | ✓ | matched |
| TalismanOfFourBearingGod | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| TalismanOfMagician | ✓ | matched |
| TalismanOfProtection | ✓ | matched |
| TalismanOfRedPhoenix | ✓ | matched |
| TalismanOfSoulStealing | ✓ | matched |
| TalismanOfWarrior | ✓ | matched |
| TalismanOfWhiteTiger | ✓ | matched |
| TotemOfTutelary | ✓ | matched |
| TurnKick | 🚩 | known gap:  is TODO. |
| TwinklingGalaxy | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| WarmthoftheMoon | ✓ | matched |
| WarmthoftheStars | ✓ | matched |
| WarmthoftheSun | ✓ | matched |

## Acolyte family

| Skill | Status | Note |
|---|---|---|
| AbsorbSpiritSphere | ✓ | matched |
| Adoramus | ✓ | matched |
| Ancilla | ✓ | matched |
| Angelus | ✓ | matched |
| Arbitrium | ✓ | matched |
| Aspersio | ✓ | matched |
| AssimilatePower | ✓ | matched |
| Assumptio | ✓ | matched |
| AsuraStrike | 🚩 | known gap:  ZC_HIGHJUMP — that's TODO for the unit-ops layer. |
| Basilica | ✓ | matched |
| BenedictioSanctissimiSacramenti | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/acolyte/benedictiosanctissimisacramenti.cpp) |
| Blessing | ✓ | matched |
| CantoCandidus | ✓ | matched |
| ChainCrushCombo | ✓ | matched |
| Clearance | ✓ | matched |
| ColuceoHeal | 🚩 | known gap:  <para>Party-iteration TODO — single-target / no-party fallback |
| Competentia | ✓ | matched |
| Convenio | ✓ | matched |
| Crementia | ✓ | matched |
| Crucis | ✓ | matched |
| Cure | ✓ | matched |
| CursedCircle | ✓ | matched |
| DecreaseAgi | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/acolyte/decreaseagi.cpp) |
| DilectioHeal | ✓ | matched |
| DragonCombo | ✓ | matched |
| DupleLightMagic | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/acolyte/duplelightmagic.cpp) |
| EarthShaker | ✓ | matched |
| Effligo | ✓ | matched |
| Epiclesis | ✓ | matched |
| ExplosionBlaster | ✓ | matched |
| FallenEmpire | ✓ | matched |
| FirstBrand | ✓ | matched |
| FlashCombo | ✓ | matched |
| Framen | ✓ | matched |
| GateOfHell | 🚩 | known gap:  when SC_COMBO is active (SC-aware ratio hook is TODO). |
| GentleTouchCure | ✓ | matched |
| GentleTouchQuiet | ✓ | matched |
| GlacierFist | ✓ | matched |
| Gloria | ✓ | matched |
| Heal | ✓ | matched |
| HighnessHeal | ✓ | matched |
| HolyLight | ✓ | matched |
| HolyWater | ✓ | matched |
| HowlingOfLion | ✓ | matched |
| ImpositioManus | ✓ | matched |
| IncreaseAgi | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/acolyte/increaseagi.cpp) |
| Judex | ✓ | matched |
| KiExplosion | 🚩 | known gap:  C# BattleDamage doesn't carry blew-count yet — TODO. |
| KiTranslation | ✓ | matched |
| KnuckleArrow | ✓ | matched |
| KyrieEleison | ✓ | matched |
| LaudaAgnus | ✓ | matched |
| LaudaRamus | ✓ | matched |
| LexDivina | ✓ | matched |
| Magnificat | ✓ | matched |
| MagnusExorcismus | ✓ | matched |
| MassiveFlameBlaster | ✓ | matched |
| MedialeVotum | ✓ | matched |
| OccultImpaction | 🚩 | known gap:  bonus is deferred (TODO). |
| OleumSanctum | ✓ | matched |
| Oratio | ✓ | matched |
| Petitio | ✓ | matched |
| Pneuma | ✓ | matched |
| PneumaticusProcella | ✓ | matched |
| PowerVelocity | ✓ | matched |
| Praefatio | ✓ | matched |
| RagingPalmStrike | ✓ | matched |
| RagingQuadrupleBlow | ✓ | matched |
| RagingThrust | 🚩 | known gap:  bonus (+50 %) is TODO (SC reader not in this hook).</para> |
| RagingTrifectaBlow | ✓ | matched |
| RaisingDragon | ✓ | matched |
| RampageBlaster | 🚩 | known gap:  SC_GT_CHANGE buff multiplies the result by +50 % (TODO — |
| Redemptio | 🚩 | known gap:  <para>Party iteration is TODO until the same-map party helper |
| Renovatio | 🚩 | known gap:  applies (TODO — needs party iteration helper).</para> |
| Reparatio | ✓ | matched |
| Resurrection | 🚩 | known gap: gate when IMapFlagService is |
| RideInLightening | ✓ | matched |
| Ruwach | ✓ | matched |
| Sanctuary | ✓ | matched |
| SecondFaith | ✓ | matched |
| SecondFlame | ✓ | matched |
| SecondJudgement | ✓ | matched |
| SignumCrucis | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/acolyte/signumcrucis.cpp) |
| Silentium | ✓ | matched |
| SkyNetBlow | ✓ | matched |
| Snap | ✓ | matched |
| StatusRecovery | ✓ | matched |
| Suffragium | ✓ | matched |
| SummoningSpiritSphere | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/acolyte/summoningspiritsphere.cpp) |
| Teleport | 🚩 | known gap:  are TODO until the map-flag service + save-point field are |
| ThirdConsecration | ✓ | matched |
| ThirdFlameBomb | ✓ | matched |
| ThirdPunish | ✓ | matched |
| ThrowSpiritSphere | ✓ | matched |
| TigerCannon | 🚩 | known gap: (hp + sp) / 4. Combo path would be / 2 (TODO). |
| TurnUndead | ✓ | matched |
| Vituperatum | ✓ | matched |
| WarpPortal | 🚩 | known gap: hydrate from |
| Windmill | ✓ | matched |
| Zen | ✓ | matched |

## Merchant family

| Skill | Status | Note |
|---|---|---|
| AbrBattleWarrior | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/merchant/abrbattlewarrior.cpp) |
| AbrDualCannon | 🚩 | known gap:  Spawns an ABR pet (TODO); applies SC_ABR_DUAL_CANNON. |
| AbrInfinity | ✓ | matched |
| AbrMotherNet | ✓ | matched |
| AcidDemonstration | 🚩 | known gap:  against players. Breaks weapon+armor on hit (TODO). |
| AcidTerror | ✓ | matched |
| AcidifiedZoneFire | 🚩 | known gap:  + Formless/Plant bonuses TODO). |
| AcidifiedZoneGround | ✓ | matched |
| AcidifiedZoneWater | ✓ | matched |
| AcidifiedZoneWind | ✓ | matched |
| AdrenalineRush | ✓ | matched |
| AdvanceProtection | 🚩 | known gap:  Requires target wearing Shadow Gear. Equip check TODO — we hand |
| AdvancedAdrenalineRush | ✓ | matched |
| AidBerserkPotion | 🚩 | known gap:  gate (TODO). |
| AidCondensedPotion | ✓ | matched |
| AidPotion | ✓ | matched |
| AlchemicalWeapon | 🚩 | known gap:  Player-only target; weapon-equip gate TODO. |
| Analyze | ✓ | matched |
| ArmCannon | 🚩 | known gap:  div_ (TODO). |
| AttackMachine | ✓ | matched |
| AxeBoomerang | 🚩 | known gap:  Ratio <c>+(150 + 50*lv)</c>; weapon-weight bonus TODO. |
| AxeStomp | ✓ | matched |
| AxeTornado | 🚩 | known gap:  +380 (caster SC TODO). |
| BackSideSlide | ✓ | matched |
| BiochemicalHelm | 🚩 | known gap:  Player-only target; head-equip gate TODO. |
| BionicPharmacy | ✓ | matched |
| Bomb | 🚩 | known gap:  break_equip(weapon) TODO. |
| BoostKnuckle | ✓ | matched |
| CallHomunculus | ✓ | matched |
| CartCannon | ✓ | matched |
| CartRevolution | 🚩 | known gap:  cart-weight read TODO; we use the non-player max of +150). |
| CartTermination | ✓ | matched |
| CartTornado | 🚩 | known gap:  scale TODO. |
| ChangeCart | ✓ | matched |
| ChangeMaterial | ✓ | matched |
| ColdSlower | ✓ | matched |
| CrazyUproar | 🚩 | known gap:  Party-wide STR buff; party splash via party_foreachsamemap TODO. |
| CrazyWeed | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| CreateBomb | ✓ | matched |
| Creeper | 🚩 | known gap:  + spawns the bionic creeper (mob spawn TODO). |
| DecorateCart | ✓ | matched |
| DemonicFire | 🚩 | known gap:  Expansion variants — TODO branch on skillLevel > 10). |
| DustExplosion | 🚩 | known gap:  bonus TODO). |
| EmergencyCool | ✓ | matched |
| EnergyCannonade | ✓ | matched |
| ExplosivePowder | 🚩 | known gap:  +100*lv bonus TODO). |
| FawMagicDecoy | ✓ | matched |
| FawRemoval | ✓ | matched |
| FawSilverSniper | ✓ | matched |
| FireExpansion | ✓ | matched |
| FlameLauncher | 🚩 | known gap:  at <c>20 + 10*lv %</c>. Eight-path AoE TODO — primary hit lands. |
| FrontSideSlide | 🚩 | known gap:  Slides caster forward. Direction read TODO; broadcast only. |
| FullProtection | 🚩 | known gap:  Equip-slot check TODO. |
| Greed | ✓ | matched |
| HammerFall | 🚩 | known gap:  SC_STUN. Splash dispatch TODO; named target gets the roll. |
| HellTree | ✓ | matched |
| HellsPlant | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| HomunculusResurrection | ✓ | matched |
| HowlingOfMandragora | ✓ | matched |
| IllusionDoping | ✓ | matched |
| InfraredScan | 🚩 | known gap:  SC_INFRAREDSCAN. Splash dispatch TODO; we land the dispel + SC on |
| ItemAppraisal | ✓ | matched |
| MagmaEruption | 🚩 | known gap:  follow-up unit TODO. |
| MagneticField | 🚩 | known gap:  Splash SC_MAGNETICFIELD application. Splash dispatch TODO. |
| Mammonite | ✓ | matched |
| ManufactureMachine | 🚩 | known gap:  Opens the Meister crafting panel. UI packet TODO. |
| MayhemicThorns | ✓ | matched |
| MightySmash | 🚩 | known gap:  +20 +5*POW TODO. |
| MixCooking | ✓ | matched |
| MysteryPowder | ✓ | matched |
| NeutralBarrier | ✓ | matched |
| PileBunker | ✓ | matched |
| PlantCultivation | ✓ | matched |
| PowerSwing | ✓ | matched |
| PowerThrust | 🚩 | known gap:  Party-wide ATK buff. Party splash + weapon-type gate TODO. |
| PowerfulSwing | ✓ | matched |
| PreparePotion | 🚩 | known gap:  Opens the produce-mix list. UI packet TODO. |
| Repair | 🚩 | known gap:  check TODO). |
| RushQuake | ✓ | matched |
| RushStrike | ✓ | matched |
| SelfDestruction | 🚩 | known gap:  SP zap TODO; we broadcast only. |
| SlingItem | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| SparkBlaster | ✓ | matched |
| SpecialPharmacy | 🚩 | known gap:  broadcast the no-damage animation and TODO the cook list packet. |
| SporeExplosion | ✓ | matched |
| StealthField | ✓ | matched |
| SummonFlora | ✓ | matched |
| SummonMarineSphere | ✓ | matched |
| SynthesizedShield | ✓ | matched |
| SyntheticArmor | ✓ | matched |
| TheWholeProtection | ✓ | matched |
| ThornTrap | ✓ | matched |
| TripleLaser | ✓ | matched |
| TwilightAlchemy1 | ✓ | matched |
| TwilightAlchemy2 | ✓ | matched |
| TwilightAlchemy3 | ✓ | matched |
| UpgradeWeapon | ✓ | matched |
| Vaporize | ✓ | matched |
| Vending | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/merchant/vending.cpp) |
| VulcanArm | 🚩 | known gap:  Sc availability isn't passed through ModifyDamageData yet — TODO once |
| WallOfThorns | ✓ | matched |
| WeaponPerfection | 🚩 | known gap:  equip check + party splash are TODO. |
| WeaponRepair | ✓ | matched |
| WoodenFairy | ✓ | matched |
| WoodenWarrior | ✓ | matched |

## Archer family

| Skill | Status | Note |
|---|---|---|
| AcousticRhythm | ✓ | matched |
| AimedBolt | 🚩 | known gap:  readback isn't surfaced to this hook — Fear Breeze branch TODO.</para> |
| AinRhapsody | 🚩 | known gap:  map_foreachinallrange is TODO; the named target gets the SC.</para> |
| Amp | ✓ | matched |
| AnkleSnare | ✓ | matched |
| ArrowShower | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| ArrowStorm | 🚩 | known gap:  readback TODO.</para> |
| BattleTheme | ✓ | matched |
| BeastStrafing | ✓ | matched |
| BlastMine | ✓ | matched |
| BlitzBeat | ✓ | matched |
| Camouflage | ✓ | matched |
| ChargeArrow | ✓ | matched |
| CircleOfNaturesSound | 🚩 | known gap:  level (lookup TODO). Party splash via party_foreachsamemap is TODO; |
| ClassicalPluck | ✓ | matched |
| ClaymoreTrap | ✓ | matched |
| ClusterBomb | ✓ | matched |
| CobaltTrap | ✓ | matched |
| Concentration | 🚩 | known gap:  Applies SC_CONCENTRATION; trap-reveal sub is TODO. |
| CresciveBolt | ✓ | matched |
| DanceWithAWarg | 🚩 | known gap:  Party-wide ASPD buff. Splash via party_foreachsamemap TODO; lands |
| Dazzler | 🚩 | known gap:  vs enemies, halved by 4 vs party members (party-check gate TODO). |
| DeepBlindTrap | 🚩 | known gap:  multiplier (TODO — passive read not surfaced). Drops a trap unit.</para> |
| DeepSleepLullaby | 🚩 | known gap:  SC_DEEPSLEEP with level/INT-scaled duration. Splash TODO. |
| Detect | ✓ | matched |
| Detonator | ✓ | matched |
| DominionImpulse | ✓ | matched |
| DoubleStrafe | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| DownTempo | ✓ | matched |
| EchoSong | 🚩 | known gap:  Party-wide WM_LESSON-scaled buff; WM_LESSON passive lookup TODO. |
| ElectricShocker | ✓ | matched |
| Encore | ✓ | matched |
| FalconAssault | ✓ | matched |
| FearBreeze | ✓ | matched |
| FiringTrap | ✓ | matched |
| FlameTrap | ✓ | matched |
| Flasher | ✓ | matched |
| FocusBallet | ✓ | matched |
| FocusedArrowStrike | 🚩 | known gap:  are TODO. Ends SC_CAMOUFLAGE on hit. |
| FreezingTrap | ✓ | matched |
| FriggsSong | 🚩 | known gap:  Party-wide MaxHP buff. Splash via party_foreachsamemap TODO. |
| GaleStorm | 🚩 | known gap:  Brute/Fish (caster SC TODO). |
| GeffeniaNocturn | ✓ | matched |
| GloomyDay | 🚩 | known gap:  classes — player skill-tree check TODO; we apply the standard SC). |
| GreatEcho | 🚩 | known gap:  partner doubles the ratio. Splash + partner check TODO.</para> |
| GypsysKiss | ✓ | matched |
| HarmonicLick | ✓ | matched |
| Harmonize | ✓ | matched |
| HawkBoomerang | 🚩 | known gap:  scale + Brute/Fish ×1.5 bonus (race scaled here, passive TODO). |
| HawkMastery | ✓ | matched |
| HawkRush | ✓ | matched |
| HipShaker | ✓ | matched |
| IceboundTrap | ✓ | matched |
| ImpressiveRiff | ✓ | matched |
| ImproveConcentration | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/archer/improveconcentration.cpp) |
| ImprovisedSong | ✓ | matched |
| JawaiiSerenade | 🚩 | known gap:  Party-wide chorus song. Splash + partner detection TODO; lands |
| LadyLuck | ✓ | matched |
| LandMine | ✓ | matched |
| LeradsDew | ✓ | matched |
| Lullaby | ✓ | matched |
| MagentaTrap | ✓ | matched |
| MagicStrings | ✓ | matched |
| MaizeTrap | ✓ | matched |
| MakingArrow | ✓ | matched |
| MarionetteControl | ✓ | matched |
| MelodyOfSink | 🚩 | known gap:  lookup TODO). |
| MelodyStrike | ✓ | matched |
| MentalSensing | ✓ | matched |
| MetallicFury | 🚩 | known gap:  <c>800*lv + 2*TR_STAGE_MANNER*SPL</c> (passive + target SC TODO). |
| MetallicSound | 🚩 | known gap:  <c>+(-100 + 120*lv) + 60*WM_LESSON</c> (passive lookup TODO); +100 |
| MoonlitSerenade | 🚩 | known gap:  Party-wide buff. Splash via party_foreachsamemap TODO; lands on |
| MusicalInterlude | 🚩 | known gap:  Party-wide buff. Splash via party_foreachsamemap TODO. |
| NipelheimRequiem | 🚩 | known gap:  chorus partner). Splash + partner check TODO.</para> |
| OwlsEye | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/archer/owlseye.cpp) |
| PangVoice | ✓ | matched |
| PerfectTablature | ✓ | matched |
| PhantasmicArrow | ✓ | matched |
| PoemOfTheNetherworld | ✓ | matched |
| PowerChord | ✓ | matched |
| PronMarch | 🚩 | known gap:  Party-wide chorus song. Splash via party_foreachsamemap TODO. |
| RemoveTrap | ✓ | matched |
| Retrospection | ✓ | matched |
| Reverberation | 🚩 | known gap:  Splash via map_foreachinallrange is TODO; the named target gets |
| RhythmShooting | 🚩 | known gap:  bonuses TODO. |
| RokiCapriccio | 🚩 | known gap:  Splash + partner doubling TODO. |
| RoseBlossom | ✓ | matched |
| Sandman | ✓ | matched |
| SaturdayNightFever | 🚩 | known gap:  passive bonuses TODO). |
| SensitiveKeen | 🚩 | known gap:  Hidden-target dispatch + trap-iteration TODOs. |
| SevereRainstorm | 🚩 | known gap:  Drops a damage-trap ground unit. Equip-lock during duration TODO. |
| ShelteringBliss | ✓ | matched |
| ShockwaveTrap | ✓ | matched |
| SkidTrap | ✓ | matched |
| SkilledSpecialSinger | ✓ | matched |
| SlingingArrow | ✓ | matched |
| SlowGrace | ✓ | matched |
| SolidTrap | ✓ | matched |
| SongOfMana | 🚩 | known gap:  Party-wide SP-regen buff. Splash via party_foreachsamemap TODO. |
| SongofLutie | ✓ | matched |
| SoundBlend | 🚩 | known gap:  SC_MYSTIC_SYMPHONY (caster SC readback TODO) with an additional |
| SoundOfDestruction | 🚩 | known gap:  Splash debuff. WM_LESSON duration bonus TODO. |
| SpringTrap | ✓ | matched |
| SwiftTrap | ✓ | matched |
| SwingDance | 🚩 | known gap:  Party-wide ASPD buff. Splash via party_foreachsamemap TODO. |
| SymphonyOfLovers | 🚩 | known gap:  Party-wide buff. Splash via party_foreachsamemap TODO. |
| TalkieBox | ✓ | matched |
| TarotCardOfFate | ✓ | matched |
| UnbarringOctave | ✓ | matched |
| UnchainedSerenade | 🚩 | known gap:  Renewal damage ratio <c>+(10 + 50*lv)</c>. Job-level scale TODO. |
| UnlimitedHummingVoice | 🚩 | known gap:  Party-wide buff. Splash via party_foreachsamemap TODO. |
| ValleyOfDeath | ✓ | matched |
| VerdureTrap | ✓ | matched |
| VoiceOfSiren | ✓ | matched |
| VulcanArrow | ✓ | matched |
| WandOfHermode | 🚩 | known gap:  dispatcher TODO). |
| WarcryOfBeyond | 🚩 | known gap:  scale TODO). |
| WargBite | ✓ | matched |
| WargDash | 🚩 | known gap:  Ratio +200. Toggle SC_WUGDASH on/off; warg-riding gate TODO. |
| WargMastery | ✓ | matched |
| WargRider | ✓ | matched |
| WargStrike | 🚩 | known gap:  Ratio <c>+(-100 + 200*lv)</c>. Mounted dash-then-hit is TODO. |
| WildWalk | 🚩 | known gap:  after damage. WH_NATUREFRIENDLY / HT_STEELCROW passive scales TODO. |
| WindWalker | 🚩 | known gap:  Party-wide ASPD/MOVE buff. Splash via party_foreachsamemap TODO. |
| WindmillRushAttack | 🚩 | known gap:  Party-wide buff. Splash via party_foreachsamemap TODO. |
| WinkofCharm | ✓ | matched |

## Mage family

| Skill | Status | Note |
|---|---|---|
| ActivityBurn | ✓ | matched |
| AllBloom | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| Arrullo | 🚩 | known gap:  Splash dispatch is TODO — the named target gets the roll. |
| AstralStrike | ✓ | matched |
| BeastlyHypnosis | ✓ | matched |
| BlindingMist | ✓ | matched |
| CastCancel | ✓ | matched |
| ChainLightning | 🚩 | known gap:  SkillIds catalog yet, so the chained sub-skill bounce is TODO — the primary c |
| ClassChange | ✓ | matched |
| CloudKill | 🚩 | known gap:  on caster TODO.</para> |
| ColdBolt | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| Coma | ✓ | matched |
| Comet | ✓ | matched |
| Conflagration | 🚩 | known gap:  active on the caster (SC read TODO — buff registry isn't wired here). |
| CreateElementalConverter | ✓ | matched |
| CrimsonArrow | 🚩 | known gap:  (AG_CRIMSON_ARROW_ATK) splash on the eight-path is TODO until the |
| CrimsonRock | ✓ | matched |
| CrystalImpact | 🚩 | known gap:  count, lv 5 enlarges the AOE to 15×15) are TODO — caster SC |
| DeadlyProjection | ✓ | matched |
| Deluge | 🚩 | known gap:  a slot) is TODO — needs an ISkillUnitService.LocateElementField helper. |
| DestructiveHurricane | 🚩 | known gap:  caster buff; val1=5 enlarges the splash to 19×19) are TODO until |
| DiamondDust | 🚩 | known gap:  passive lookup and SC_COOLER_OPTION job-level bonus are TODO until |
| DiamondStorm | 🚩 | known gap:  is active on the caster (SC readback TODO). Splash victims roll 5 % |
| Dispell | 🚩 | known gap:  and song-area special cases are TODO until StatusEffectRegistry |
| DrainLife | ✓ | matched |
| EarthGrave | 🚩 | known gap:  passive lookup and SC_CURSED_SOIL_OPTION job-level bonus are TODO. |
| EarthInsignia | ✓ | matched |
| EarthSpike | ✓ | matched |
| EarthStrain | 🚩 | known gap:  chain is TODO until the engine surfaces it from this hook).</para> |
| ElectricWalk | 🚩 | known gap:  bonus is TODO. Buff replaces any active SC_ELECTRICWALK on the |
| ElementalAction | ✓ | matched |
| ElementalBuster | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| ElementalChangeEarth | ✓ | matched |
| ElementalChangeFire | ✓ | matched |
| ElementalChangeWater | ✓ | matched |
| ElementalChangeWind | ✓ | matched |
| ElementalShield | 🚩 | known gap:  than one player is TODO until <c>party_foreachsamemap</c> lands.</para> |
| ElementalVeil | ✓ | matched |
| EndowBlaze | 🚩 | known gap: fail on unarmed (W_FIST) target — weapon-type check is TODO until equip surfac |
| EndowQuake | ✓ | matched |
| EndowTornado | ✓ | matched |
| EndowTsunami | ✓ | matched |
| EnergyCoat | ✓ | matched |
| FiberLock | ✓ | matched |
| FireBolt | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| FireInsignia | ✓ | matched |
| FirePillar | 🚩 | known gap: players split MATK across hits (dmg.div_ *= -1). Signed-div TODO. |
| FireWalk | 🚩 | known gap: skillratio += -100 + 60*lv; SC_HEATER_OPTION job_level/2 TODO. |
| FireWall | ✓ | matched |
| Fireball | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| FloralFlareRoad | ✓ | matched |
| FourSpiritAnalysis | ✓ | matched |
| FrostDiver | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| FrostNova | 🚩 | known gap:  Full splash dispatch (map_foreachinshootrange) is TODO — for now we |
| FrostyMisty | 🚩 | known gap:  wall_check is TODO until path_search is wired here.</para> |
| FrozenSlash | 🚩 | known gap:  (SC readback TODO). RecursiveDamageSplashSkillImpl handles the |
| Ganbantein | ✓ | matched |
| GoldDigger | ✓ | matched |
| GravitationField | ✓ | matched |
| Gravity | ✓ | matched |
| GrimReaper | ✓ | matched |
| HeavensDrive | ✓ | matched |
| HellInferno | 🚩 | known gap:  Primary hit (Fire). Dark follow-up's +200*lv bonus is TODO — same hook can't  |
| Hindsight | 🚩 | known gap:  UI yet, so the player-side branch is TODO. The mob-side branch |
| HocusPocus | ✓ | matched |
| IceWall | ✓ | matched |
| IncreasingActivity | ✓ | matched |
| Indulge | 🚩 | known gap:  HP-charge precondition is checked on the live entity HP — current-HP read on  |
| JackFrost | ✓ | matched |
| JupitelThunder | ✓ | matched |
| Leveling | ✓ | matched |
| LightningBolt | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| LightningLand | 🚩 | known gap:  is TODO (SC readback isn't wired in this hook). Splash victims |
| LordOfVermilion | ✓ | matched |
| MagicBoltHelper | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mage/magicbolthelper.cpp) |
| MagicRod | ✓ | matched |
| MagneticEarth | ✓ | matched |
| MeteorStorm | 🚩 | known gap:  for now and leave the stagger as TODO.</para> |
| MindBreaker | 🚩 | known gap:  the target's ongoing cast on success. Mob aggro retarget is TODO.</para> |
| Monocell | 🚩 | known gap:  transformation is left as TODO and we only land the broadcast + |
| MonsterChant | ✓ | matched |
| MysteryIllusion | ✓ | matched |
| NapalmBeat | ✓ | matched |
| NapalmVulcan | ✓ | matched |
| PoisonBuster | 🚩 | known gap:  SC reads are TODO (no SC readback in this hook yet).</para> |
| PsychicWave | 🚩 | known gap:  is active on the caster (SC readback TODO). Hit count doubles when |
| Quagmire | ✓ | matched |
| Questioning | ✓ | matched |
| RainOfCrystal | ✓ | matched |
| ReadingSpellbook | ✓ | matched |
| Rejuvenation | ✓ | matched |
| Release | ✓ | matched |
| RockDown | 🚩 | known gap:  when SC_CLIMAX is active on caster (SC readback TODO).</para> |
| SafetyWall | ✓ | matched |
| Sense | ✓ | matched |
| SiennaExecrate | 🚩 | known gap:  Splash chain to other enemies is TODO until <c>map_foreachinrange</c> |
| Sight | ✓ | matched |
| SightBlaster | ✓ | matched |
| SightRasher | 🚩 | known gap: <c>+20*lv</c>. Splash dispatch is TODO; the |
| SoulExhale | 🚩 | known gap:  Caster gains 3 % of own max SP. soul_change_flag bookkeeping TODO. |
| SoulExpansion | ✓ | matched |
| SoulSiphon | ✓ | matched |
| SoulStrike | ✓ | matched |
| SoulVulcanStrike | ✓ | matched |
| SpellBreaker | ✓ | matched |
| SpellFist | ✓ | matched |
| SpiritControl | ✓ | matched |
| SpiritRecovery | ✓ | matched |
| Stasis | ✓ | matched |
| StoneCurse | ✓ | matched |
| StormCannon | 🚩 | known gap:  +<c>300*lv</c> with SC_CLIMAX (caster SC TODO). Splash dispatch via |
| StormGust | ✓ | matched |
| StrantumTremor | ✓ | matched |
| Striking | ✓ | matched |
| Suicide | ✓ | matched |
| SummonEarthSpiritTera | ✓ | matched |
| SummonElementalArdor | ✓ | matched |
| SummonElementalDiluvio | ✓ | matched |
| SummonElementalProcella | ✓ | matched |
| SummonElementalSerpens | ✓ | matched |
| SummonElementalTerremotus | ✓ | matched |
| SummonFireBall | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| SummonFireSpiritAgni | ✓ | matched |
| SummonLightningBall | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| SummonStone | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| SummonWaterBall | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| SummonWaterSpiritAqua | ✓ | matched |
| SummonWindSpiritVentus | ✓ | matched |
| TerraDrive | 🚩 | known gap:  is active on caster (SC readback TODO). Splash victims roll 5 % |
| TetraVortex | 🚩 | known gap:  4 hits 200 ms apart. Per-element sub-skill dispatch TODO. |
| Thunderstorm | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| TornadoStorm | ✓ | matched |
| VacuumExtreme | ✓ | matched |
| VaretyrSpear | 🚩 | known gap:  term and SC_BLAST_OPTION job_level*5 bonus are TODO. Splash victims |
| VenomSwamp | 🚩 | known gap:  on the caster (SC readback TODO). Splash victims roll 3 % |
| ViolentQuake | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| Volcano | 🚩 | known gap:  <summary>SA_VOLCANO — Sage Volcano. Element field (Fire ATK boost zone). Elem |
| WarlockSpellbookHelpers | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mage/warlockspellbookhelpers.cpp) |
| WarlockSphereHelpers | ❓ | no matching rAthena .cpp (/Volumes/1TB/Projetos/rathena-fork/src/map/skills/mage/warlockspherehelpers.cpp) |
| Warmer | ✓ | matched |
| WaterBall | ✓ | matched |
| WaterInsignia | ✓ | matched |
| Whirlwind | ✓ | matched |
| WhiteImprison | ✓ | matched |
| WindInsignia | ✓ | matched |

## Npc family

| Skill | Status | Note |
|---|---|---|
| AcidBreath | ✓ | matched |
| AgilityUp | ✓ | matched |
| AntiMagic | ✓ | matched |
| AttributeChange | ✓ | matched |
| Bleeding | ✓ | matched |
| Bleeding2 | ✓ | matched |
| BlindAttack | ✓ | matched |
| BreakArmor | 🚩 | known gap:  <summary>NPC_ARMORBRAKE — Weapon hit; armor break TODO.</summary> |
| BreakHelm | 🚩 | known gap:  <summary>NPC_HELMBRAKE — Weapon hit; helm break TODO.</summary> |
| BreakShield | 🚩 | known gap:  <summary>NPC_SHIELDBRAKE — Weapon hit; shield break TODO.</summary> |
| CaneOfEvilEye | ✓ | matched |
| ChangeLocation | 🚩 | known gap:  <summary>NPC_MOVE_COORDINATE — Mob warp. Position service TODO.</summary> |
| Comet2 | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| CriticalWounds | ✓ | matched |
| CrossOfDarkness | ✓ | matched |
| CurseAttack | ✓ | matched |
| DancingBlade | 🚩 | known gap:  <summary>NPC_DANCINGBLADE — Schedules NPC_DANCINGBLADE_ATK via skill timer. T |
| DarkBlessing | 🚩 | known gap:  <summary>NPC_DARKBLESSING — (50 + 5*lv) % SC_COMA via SC start. Status start  |
| DarkBreath | ✓ | matched |
| DarkPiercing | ✓ | matched |
| DarknessBreath | 🚩 | known gap:  <summary>NPC_DARKNESSBREATH — Magic hit; ratio +100*(lv-1); directional AoE ( |
| DarknessJupitel | ✓ | matched |
| DeadlyCurse | ✓ | matched |
| DeadlyCurse2 | ✓ | matched |
| DeathSummon | 🚩 | known gap:  <summary>NPC_DEATHSUMMON — Summon a Death Servant. Mob spawn TODO.</summary> |
| DecreaseAllStats | ✓ | matched |
| DemonShockAttack | ✓ | matched |
| DragonFear | ✓ | matched |
| EarthAttributeAttack | ✓ | matched |
| EarthAttributeChange | ✓ | matched |
| Earthquake | 🚩 | known gap:  <summary>NPC_EARTHQUAKE — Splash misc-type damage. Splash iteration TODO.</su |
| Emotion | ✓ | matched |
| EmotionOn | ✓ | matched |
| EnergyDrain | 🚩 | known gap:  <summary>NPC_ENERGYDRAIN — Mob SP drain. Drain mechanics TODO.</summary> |
| EvilLand | ✓ | matched |
| Expulsion | ✓ | matched |
| FireAttributeAttack | ✓ | matched |
| FireAttributeChange | ✓ | matched |
| FireBreath | 🚩 | known gap:  <summary>NPC_FIREBREATH — Magic hit; ratio +100*(lv-1); directional AoE TODO. |
| FireStorm | ✓ | matched |
| FlameCross | ✓ | matched |
| FollowerSummons | 🚩 | known gap:  <summary>NPC_SUMMONSLAVE — Mob spawns slave mobs. Mob spawn TODO.</summary> |
| FullHeal | 🚩 | known gap:  <summary>NPC_ALLHEAL — Mob full HP heal. Heal apply TODO.</summary> |
| GhostAttributeAttack | ✓ | matched |
| GhostAttributeChange | ✓ | matched |
| GrandCrossOfDarkness | ✓ | matched |
| GroundDrive | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| Hallucination | ✓ | matched |
| HellBurning | ✓ | matched |
| HellDignity | ✓ | matched |
| HellPower | ✓ | matched |
| HellsJudgement | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| HellsJudgement2 | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| HolyAttributeAttack | ✓ | matched |
| HolyAttributeChange | ✓ | matched |
| IceBreath | ✓ | matched |
| IceBreath2 | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| IceMine | ✓ | matched |
| IncreasedGravity | ✓ | matched |
| InvincibleOff | ✓ | matched |
| Invisible | ✓ | matched |
| JackFrost2 | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| Leash | ✓ | matched |
| LexAeterna2 | ✓ | matched |
| Lick | ✓ | matched |
| Metamorphosis | 🚩 | known gap:  <summary>NPC_METAMORPHOSIS — Mob transformation. Transformation TODO.</summar |
| MilleniumShield2 | ✓ | matched |
| MonsterSummons | 🚩 | known gap:  <summary>NPC_SUMMONMONSTER — Mob spawns reinforcement mobs. Mob spawn TODO.</ |
| MultiStageAttack | ✓ | matched |
| NpcArrowStorm | ✓ | matched |
| NpcCloudKill | ✓ | matched |
| NpcColuceoHeal | 🚩 | known gap:  <summary>NPC_CHEAL — Splash heal for all friendly mobs. Splash iteration TODO |
| NpcCursedCircle | ✓ | matched |
| NpcDragonBreath | ✓ | matched |
| NpcElectricWalk | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| NpcFatalMenace | ✓ | matched |
| NpcFireWalk | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| NpcHowlingOfMandragora | ✓ | matched |
| NpcIgnitionBreak | ✓ | matched |
| NpcMagmaEruption | 🚩 | known gap:  stage 2 is a TODO awaiting the skill-timer service integration. |
| NpcPhantomThrust | ✓ | matched |
| NpcPoisonBuster | ✓ | matched |
| NpcPsychicWave | ✓ | matched |
| NpcRayOfGenesis | ✓ | matched |
| NpcRun | ✓ | matched |
| NpcSuicide | ✓ | matched |
| NpcVenomImpress | ✓ | matched |
| PetrifyAttack | ✓ | matched |
| PiercingAttack | ✓ | matched |
| PoisonAttack | ✓ | matched |
| PoisonAttributeAttack | ✓ | matched |
| PoisonAttributeChange | ✓ | matched |
| PowerUp | ✓ | matched |
| PropertyImmune | ✓ | matched |
| Provocation | ✓ | matched |
| PulseStrike | ✓ | matched |
| PulseStrike2 | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| RainOfMeteor | 🚩 | known gap:  <summary>NPC_RAINOFMETEOR — Cell-placed meteor rain. Splash unit placement TO |
| RandomAttack | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| RandomMove | ✓ | matched |
| Rebirth | ✓ | matched |
| RecallSlaves | ✓ | matched |
| Revenge | ✓ | matched |
| Reverberation2 | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| ShadowAttributeAttack | ✓ | matched |
| ShadowAttributeChange | ✓ | matched |
| SiegeMode | ✓ | matched |
| SilenceAttack | ✓ | matched |
| SleepAttack | ✓ | matched |
| SlowCast | ✓ | matched |
| Smoking | ✓ | matched |
| SoulStrikeOfDarkness | ✓ | matched |
| SpeedUp | ✓ | matched |
| SpiritDestruction | ✓ | matched |
| SplashAttack | ✓ | matched |
| StoneSkin | ✓ | matched |
| Stop | ✓ | matched |
| StormGust2 | ✓ | matched |
| StunAttack | ✓ | matched |
| SuckingBlood | 🚩 | known gap: thread the |
| SuicideBombing | ✓ | matched |
| Talk | ✓ | matched |
| ThunderBreath | ✓ | matched |
| Transformation | ✓ | matched |
| UndeadAttributeChange | ✓ | matched |
| UndeadElementAttack | ✓ | matched |
| VampireGift | ✓ | matched |
| VenomFog | ⚠️ | rAthena has skillratio but C# missing CalculateSkillRatio (often false-positive for multi-hit skills) |
| WaterAttributeAttack | ✓ | matched |
| WaterAttributeChange | ✓ | matched |
| WideBleeding | ✓ | matched |
| WideBleeding2 | ✓ | matched |
| WideConfusion | ✓ | matched |
| WideConfusion2 | ✓ | matched |
| WideCriticalWounds | ✓ | matched |
| WideCurse | ✓ | matched |
| WideCurse2 | ✓ | matched |
| WideFreeze | ✓ | matched |
| WideFreeze2 | ✓ | matched |
| WideLeash | 🚩 | known gap:  <summary>NPC_WIDELEASH — Splash leash; pull each enemy in splash to src. Spla |
| WidePetrify | ✓ | matched |
| WidePetrify2 | ✓ | matched |
| WideSight | ✓ | matched |
| WideSilence | ✓ | matched |
| WideSilence2 | ✓ | matched |
| WideSleep | ✓ | matched |
| WideSleep2 | ✓ | matched |
| WideSoulDrain | 🚩 | known gap:  <summary>NPC_WIDESOULDRAIN — Splash SP drain (10 * skillLevel %) on splash hi |
| WideStun | ✓ | matched |
| WideStun2 | ✓ | matched |
| WideSuck | ✓ | matched |
| WideWeb | ✓ | matched |
| WindAttributeAttack | ✓ | matched |
| WindAttributeChange | ✓ | matched |

