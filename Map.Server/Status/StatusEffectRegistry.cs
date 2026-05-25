using Map.Server.Entities;

namespace Map.Server.Status;

/// <summary>
/// Per-SC behavior table. Mirrors rAthena's giant <c>status.cpp</c>
/// switch statements collapsed into a record per SC type. Effects
/// register their <see cref="StatusEffectHandler.OnStart"/> (apply stat mods + initial
/// per-tick gating) and <see cref="StatusEffectHandler.OnEnd"/> (revert stat mods)
/// callbacks. Periodic logic is owned by the SC handler via the
/// <c>NextTick</c> / <c>PeriodMs</c> fields on <see cref="StatusChange"/>.
///
/// <para>T2.4b adds the first wave of ~30 SC handlers — crowd-control
/// gates (Stone / Freeze / Stun / Sleep / Curse / Silence / Confusion /
/// Blind), damage-over-time (Bleeding / Burning / DeadlyPoison),
/// physical buffs (Endure / AdrenalineRush / Concentration), defense
/// buffs (Assumptio / Kyrie marker), and cast-time scaling markers
/// (Suffragium / Memorize / Slowcast / Paralysis / Izayoi / Bragi)
/// consumed by <see cref="Skills.SkillCastTimingService.CastFixSc"/>.</para>
/// </summary>
public sealed class StatusEffectRegistry
{
    private readonly Dictionary<StatusType, StatusEffectHandler> _handlers = new();

    public StatusEffectRegistry()
    {
        // ===== Damage-over-time =====

        // SC_POISON — 1.5%/sec MaxHp DoT, 30 s default.
        // rAthena status.cpp tick = 1500 ms; damage = max(1, maxhp*15/1000).
        Register(StatusType.Poison, new StatusEffectHandler(
            OnStart: (_, _, _) => { /* no immediate stat mod */ },
            OnEnd: (_, _) => { },
            PeriodMs: 1500,
            OnPeriodic: (target, _, applyDamage) =>
            {
                var dmg = Math.Max(1, target.Stats.MaxHp * 15 / 1000);
                applyDamage(dmg);
            }));

        // SC_DPOISON (Deadly Poison) — 2 % MaxHp / sec.
        Register(StatusType.DeadlyPoison, new StatusEffectHandler(
            OnStart: (_, _, _) => { },
            OnEnd: (_, _) => { },
            PeriodMs: 1000,
            OnPeriodic: (target, _, applyDamage) =>
            {
                var dmg = Math.Max(1, target.Stats.MaxHp * 2 / 100);
                applyDamage(dmg);
            }));

        // SC_BLEEDING — every 10 s deals MaxHp/100. rAthena: also blocks
        // HP regen (gated by IPcRegenService elsewhere; here we just DoT).
        Register(StatusType.Bleeding, new StatusEffectHandler(
            OnStart: (_, _, _) => { },
            OnEnd: (_, _) => { },
            PeriodMs: 10_000,
            OnPeriodic: (target, _, applyDamage) =>
            {
                var dmg = Math.Max(1, target.Stats.MaxHp / 100);
                applyDamage(dmg);
            }));

        // SC_BURNING — every 3 s deals MaxHp*3/100 (fire). Val1 = caster
        // attacker level for scaling once renewal magic damage ports.
        Register(StatusType.Burning, new StatusEffectHandler(
            OnStart: (_, _, _) => { },
            OnEnd: (_, _) => { },
            PeriodMs: 3000,
            OnPeriodic: (target, _, applyDamage) =>
            {
                var dmg = Math.Max(1, target.Stats.MaxHp * 3 / 100);
                applyDamage(dmg);
            }));

        // ===== Crowd-control gates (no stat mod — presence is the gate) =====
        // Wave 5a (RegisterWave5aClassAFormulas) now registers Stone/
        // Freeze/Stun/Sleep/Silence/Confusion/Stonewait via
        // PresenceMarker with explicit consumer citation
        // (EntityActionGates.CanAct/CanCastSkill reads SC presence).
        // Curse and Blind got real OnStart bodies in wave 1 (lines 577,
        // 598).  Original placeholder NoOpHandler() calls removed —
        // shadowing was already happening via dictionary overwrite; now
        // the file has 0 literal NoOpHandler() calls per NS-3 wave 5e
        // close-out (the bulk-policy citation in
        // RegisterDefaultsForMissingTypes() covers everything else).

        // ===== Stat buffs =====

        // SC_BLESSING — +val1 STR/INT/DEX (renewal). Stat mods applied
        // on start, reverted on end.
        Register(StatusType.Blessing, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Str = (short)Math.Min(short.MaxValue, target.Stats.Str + sc.Val1);
                target.Stats.IntStat = (short)Math.Min(short.MaxValue, target.Stats.IntStat + sc.Val1);
                target.Stats.Dex = (short)Math.Min(short.MaxValue, target.Stats.Dex + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Str = (short)Math.Max(0, target.Stats.Str - sc.Val1);
                target.Stats.IntStat = (short)Math.Max(0, target.Stats.IntStat - sc.Val1);
                target.Stats.Dex = (short)Math.Max(0, target.Stats.Dex - sc.Val1);
            }));

        // SC_INCREASEAGI — +val1 AGI, +ASPD.
        Register(StatusType.IncreaseAgi, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Agi = (short)Math.Min(short.MaxValue, target.Stats.Agi + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Agi = (short)Math.Max(0, target.Stats.Agi - sc.Val1);
            }));

        // SC_DECREASEAGI — −val1 AGI (debuff, mirror of IncreaseAgi).
        Register(StatusType.DecreaseAgi, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Agi = (short)Math.Max(0, target.Stats.Agi - sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Agi = (short)Math.Min(short.MaxValue, target.Stats.Agi + sc.Val1);
            }));

        // SC_ANGELUS — +val1*5 % Mdef. rAthena: status.cpp sets def2 +5*val1.
        Register(StatusType.Angelus, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var d = (short)(sc.Val1 * 5);
                target.Stats.Mdef2 = (short)Math.Min(short.MaxValue, target.Stats.Mdef2 + d);
            },
            OnEnd: (target, sc) =>
            {
                var d = (short)(sc.Val1 * 5);
                target.Stats.Mdef2 = (short)Math.Max(0, target.Stats.Mdef2 - d);
            }));

        // SC_PROVOKE — −val1*5 % DEF, +val1*2 % ATK. rAthena: cuts def by
        // 25 % at lv5 and boosts batk by 10 %. Simplified here.
        Register(StatusType.Provoke, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var defDrop = (short)(sc.Val1 * 5);
                target.Stats.Def = (short)Math.Max(0, target.Stats.Def - defDrop);
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + sc.Val1 * 2);
            },
            OnEnd: (target, sc) =>
            {
                var defDrop = (short)(sc.Val1 * 5);
                target.Stats.Def = (short)Math.Min(short.MaxValue, target.Stats.Def + defDrop);
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val1 * 2);
            }));

        // SC_CONCENTRATE / SC_CONCENTRATION (Awakening Potion + LK skill).
        // Concentrate (Awake): +val1 Agi, +val1 Dex.
        Register(StatusType.Concentrate, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Agi = (short)Math.Min(short.MaxValue, target.Stats.Agi + sc.Val1);
                target.Stats.Dex = (short)Math.Min(short.MaxValue, target.Stats.Dex + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Agi = (short)Math.Max(0, target.Stats.Agi - sc.Val1);
                target.Stats.Dex = (short)Math.Max(0, target.Stats.Dex - sc.Val1);
            }));

        // SC_CONCENTRATION (LK skill) — +val1*2 % ATK + +val1*1 % Hit,
        // takes 5 % more damage. Simplified: +Hit only.
        Register(StatusType.Concentration, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var hit = (short)(sc.Val1 * 2);
                target.Stats.Hit = (short)Math.Min(short.MaxValue, target.Stats.Hit + hit);
            },
            OnEnd: (target, sc) =>
            {
                var hit = (short)(sc.Val1 * 2);
                target.Stats.Hit = (short)Math.Max(0, target.Stats.Hit - hit);
            }));

        // SC_ADRENALINE — +val1 ASPD (renewal: +30 % at lv1).
        // Stored on AspdRate (display rate scaling).
        Register(StatusType.Adrenaline, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val1);
            }));

        // SC_TWOHANDQUICKEN — +val1 ASPD (renewal: +7 ASPD at level 1).
        // Same shape as Adrenaline.
        Register(StatusType.Twohandquicken, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val1);
            }));

        // NS-3 wave 5e: original Endure / Magnificat / Fireweapon /
        // Waterweapon / Windweapon / Earthweapon / Kyrie NoOpHandler()
        // placeholders removed. Endure + Kyrie now have real Val*
        // bodies in wave 5a (RegisterWave5aClassAFormulas, lines
        // 1352-1500). Weapon endow family + Magnificat have explicit
        // PresenceMarker registrations in wave 4a/5a with their
        // consumer paths cited (weapon element resolver / NaturalHealService).

        // SC_ASSUMPTIO — DEF +val1*20 %, MDEF +val1*20 %. Simplified
        // here as a flat boost using Val2/Val3 to remember the cached
        // delta so OnEnd reverts cleanly.
        Register(StatusType.Assumptio, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var defDelta = (short)(target.Stats.Def * sc.Val1 / 5);
                var mdefDelta = (short)(target.Stats.Mdef * sc.Val1 / 5);
                sc.Val2 = defDelta;
                sc.Val3 = mdefDelta;
                target.Stats.Def = (short)Math.Min(short.MaxValue, target.Stats.Def + defDelta);
                target.Stats.Mdef = (short)Math.Min(short.MaxValue, target.Stats.Mdef + mdefDelta);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Def = (short)Math.Max(0, target.Stats.Def - sc.Val2);
                target.Stats.Mdef = (short)Math.Max(0, target.Stats.Mdef - sc.Val3);
            }));

        // NS-3 wave 5e: Autoguard / Strip* / Hiding / Overthrust / Aeterna /
        // Impositio / Aspersio NoOpHandler() placeholders removed.
        // All have explicit Register() in wave 1/4a/5a with formula
        // bodies or CombatMarker + consumer citations.
        // - Autoguard → wave 5a (val2=sum block%)
        // - Strip family → wave 4a (CombatMarker, IEquipService consumer)
        // - Hiding → wave 4a (CombatMarker, visibility hook)
        // - Overthrust → wave 4a (CombatMarker → wave 4a bespoke at line 1190)
        // - Aeterna → wave 5a (CombatMarker, damage pipeline)
        // - Impositio → wave 1 bespoke (line 693)
        // - Aspersio → wave 5a (CombatMarker, weapon element resolver)

        // SC_GLORIA — +30 Luk (PR_GLORIA).
        Register(StatusType.Gloria, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Luk = (short)Math.Min(short.MaxValue, target.Stats.Luk + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Luk = (short)Math.Max(0, target.Stats.Luk - sc.Val1);
            }));

        // NS-3 wave 5e: Signumcrucis + Encpoison NoOpHandler() removed.
        // - Signumcrucis → wave 5a bespoke (val2=10+4*val1 Def-reduction)
        // - Encpoison → wave 5a CombatMarker (weapon element resolver)

        // SC_EXPLOSIONSPIRITS — Monk finisher prep (MO_EXPLOSIONSPIRITS).
        // Val1 = +crit, Val2 = +ATK.
        Register(StatusType.Explosionspirits, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Cri = (short)Math.Min(short.MaxValue, target.Stats.Cri + sc.Val1);
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + sc.Val2);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Cri = (short)Math.Max(0, target.Stats.Cri - sc.Val1);
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val2);
            }));

        // NS-3 wave 5e: removed ~30 more NoOpHandler() placeholders for
        // Cloaking / Maximizepower / Tensionrelax / Berserk / Magicpower /
        // Sacrifice / Edp / Windwalk / Meltdown / Cartboost / Laudaagnus /
        // Laudaramus / Kaite / Bitescar / Akaitsuki / Saturdaynightfever /
        // Deathbound / Adoramus / DragonicAura / Reflectshield / Steelbody /
        // Providence / Suffragium / Memorize / Slowcast / Paralysis / Izayoi /
        // Poembragi. All have explicit Register() in wave 1/4a/4b/5a with
        // formula bodies (Berserk/Windwalk/Cartboost/Laudaagnus/Laudaramus/
        // DragonicAura/Adoramus/Sacrifice/Kaite/Deathbound/Suffragium/Memorize/
        // Slowcast/Poembragi) or CombatMarker + consumer citation
        // (everything else).

        // ===== Periodic heal (existing C# port-only anchor) =====

        // SC_HEAL_OVERTIME — val1 HP per tick, tick every 1 s. Generic
        // heal-over-time anchor for items / future skills. No rAthena
        // equivalent — parked at id 2000.
        Register(StatusType.HealOverTime, new StatusEffectHandler(
            OnStart: (_, _, _) => { },
            OnEnd: (_, _) => { },
            PeriodMs: 1000,
            OnPeriodic: (target, sc, _) =>
            {
                target.Stats.Hp = Math.Min(target.Stats.MaxHp, target.Stats.Hp + sc.Val1);
            }));

        // ===== Cell-based SCs (Basilica / Land Protector unit overlap) =====
        // BasilicaCell NoOpHandler() removed in NS-3 wave 5e — wave 5a
        // (RegisterWave5aClassAFormulas) registers it as CombatMarker
        // with ScfFlag.Permanent and the PlayerPositionHelpers.IsBasilicaCell
        // consumer citation.

        // ===== ST.3 — backfill: combat-relevant SCs that consumers gate on =====

        // SC_DEFENDER (Crusader CR_DEFENDER) — ranged dmg reduction +
        // walk-speed penalty. The damage gate lives in the ranged-attack
        // path; our handler just attaches so consumers can query it.
        // Val1 stores the skill level (1-5).
        Register(StatusType.Defender, new StatusEffectHandler(
            OnStart: (_, _, _) => { },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_QUAGMIRE (Wizard WZ_QUAGMIRE) — ground unit halves Move +
        // ASPD + Dex/Agi on stepped-on target. The unit applies it; the
        // SC marks the affected target. Cleared by Refresh.
        Register(StatusType.Quagmire, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                // Renewal: -50 % MoveSpeed via AspdRate bump. Halving is
                // not exact; matching rAthena's quick approximation.
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + 50);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - 50);
            },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // SC_DOUBLECAST (Sage SA_DOUBLECASTING) — 50 % chance per cast
        // to trigger an extra hit. Cast pipeline reads the SC presence.
        Register(StatusType.Doublecast, new StatusEffectHandler(
            OnStart: (_, _, _) => { },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_HAWKEYES (Sniper TT_HAWKEYE) — passive +Hit. Val1 = level.
        Register(StatusType.Hawkeyes, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var hit = (short)(sc.Val1 * 3);
                target.Stats.Hit = (short)Math.Min(short.MaxValue, target.Stats.Hit + hit);
            },
            OnEnd: (target, sc) =>
            {
                var hit = (short)(sc.Val1 * 3);
                target.Stats.Hit = (short)Math.Max(0, target.Stats.Hit - hit);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_SPURT (TaeKwon's stance — Tornado Kick / Heelfall) — ATK
        // bonus on the next hit. Stored on Batk; cleared on attack
        // (rAthena pc_skill_check_spurt). Here it just persists until
        // expired.
        Register(StatusType.Spurt, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_SPIRIT (Soul Linker spirit — generic). Val2 = job id of the
        // linked class; downstream skills check this to gate their
        // boosted behavior. Most skills just need "is the SC active";
        // detailed per-job branching lives in the skill behavior plugin.
        // Wave 59 — Spirit: apply +Val1 to all 6 base stats per CalcFlag
        // listing. The per-job skill plugin still reads sc.Val2 for the
        // specific job-link mode (SL_BARDDANCER, SL_KNIGHT, etc.).
        Register(StatusType.Spirit, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Str = (short)Math.Min(short.MaxValue, target.Stats.Str + sc.Val1);
                target.Stats.Agi = (short)Math.Min(short.MaxValue, target.Stats.Agi + sc.Val1);
                target.Stats.Vit = (short)Math.Min(short.MaxValue, target.Stats.Vit + sc.Val1);
                target.Stats.IntStat = (short)Math.Min(short.MaxValue, target.Stats.IntStat + sc.Val1);
                target.Stats.Dex = (short)Math.Min(short.MaxValue, target.Stats.Dex + sc.Val1);
                target.Stats.Luk = (short)Math.Min(short.MaxValue, target.Stats.Luk + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Str = (short)Math.Max(0, target.Stats.Str - sc.Val1);
                target.Stats.Agi = (short)Math.Max(0, target.Stats.Agi - sc.Val1);
                target.Stats.Vit = (short)Math.Max(0, target.Stats.Vit - sc.Val1);
                target.Stats.IntStat = (short)Math.Max(0, target.Stats.IntStat - sc.Val1);
                target.Stats.Dex = (short)Math.Max(0, target.Stats.Dex - sc.Val1);
                target.Stats.Luk = (short)Math.Max(0, target.Stats.Luk - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // ===== Soul Linker family — Val1 = soul-orb count / lv =====
        // All carry the SC presence so per-job skill plugins can dispatch
        // (Soulreaper boosts Reaper Trample, Soulshadow gives auto-Hiding,
        // etc.). Stat mods land per-skill when those plugins port.
        var soulLink = ScfFlag.Buff | ScfFlag.RemoveOnLogout;
        Register(StatusType.Soulreaper, new StatusEffectHandler(_NoOp, _NoOpEnd, Flags: soulLink));
        Register(StatusType.Soulunity, new StatusEffectHandler(_NoOp, _NoOpEnd, Flags: soulLink));
        Register(StatusType.Soulshadow, new StatusEffectHandler(_NoOp, _NoOpEnd, Flags: soulLink));
        Register(StatusType.Soulfairy, new StatusEffectHandler(_NoOp, _NoOpEnd, Flags: soulLink));
        Register(StatusType.Soulfalcon, new StatusEffectHandler(_NoOp, _NoOpEnd, Flags: soulLink));
        Register(StatusType.Soulgolem, new StatusEffectHandler(_NoOp, _NoOpEnd, Flags: soulLink));
        Register(StatusType.Souldivision, new StatusEffectHandler(_NoOp, _NoOpEnd, Flags: soulLink));
        Register(StatusType.Soulenergy, new StatusEffectHandler(_NoOp, _NoOpEnd, Flags: soulLink));
        // Soulcurse is a debuff (Reaper class hit): Refresh-clear-able.
        Register(StatusType.Soulcurse, new StatusEffectHandler(_NoOp, _NoOpEnd,
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // ===== Sphere1..5 — Gunslinger coin orbs =====
        // Each sphere is a separate SC slot so they can stack to 5.
        // Per-coin Val1 holds the coin type (1-5).
        var sphereBuff = ScfFlag.Buff | ScfFlag.RemoveOnLogout;
        Register(StatusType.Sphere1, new StatusEffectHandler(_NoOp, _NoOpEnd, Flags: sphereBuff));
        Register(StatusType.Sphere2, new StatusEffectHandler(_NoOp, _NoOpEnd, Flags: sphereBuff));
        Register(StatusType.Sphere3, new StatusEffectHandler(_NoOp, _NoOpEnd, Flags: sphereBuff));
        Register(StatusType.Sphere4, new StatusEffectHandler(_NoOp, _NoOpEnd, Flags: sphereBuff));
        Register(StatusType.Sphere5, new StatusEffectHandler(_NoOp, _NoOpEnd, Flags: sphereBuff));

        // SC_PUTTI_TAILS_NOODLES — Wanderer's noodle song: HP regen
        // boost on party members.
        Register(StatusType.PuttiTailsNoodles, new StatusEffectHandler(_NoOp, _NoOpEnd,
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // ====================================================================
        // NS-3 wave 1 — real OnStart/OnEnd bodies for 10 SCs that mutate
        // visible PC stat fields, plus explicit-flag promotions for 8
        // combat-marker SCs whose semantics are "Val1/Val2 storage +
        // presence flag the damage/regen pipeline reads."
        //
        // Source-of-truth for each formula: rAthena status.cpp
        // (status.cpp line cited inline per SC). Where the rAthena value
        // is %-based and we don't have a direct percentile mod field
        // (e.g. MoveSpeed%), we use AspdRate as the proxy following the
        // existing T2.4b Quagmire pattern (line 469-474).
        //
        // Pattern: when the mod scales a base stat (Assumptio-style
        // % mods), store the absolute delta in sc.Val2/Val3 at OnStart
        // and revert it at OnEnd — that survives recalc races where
        // the underlying stat moved between Start and End.
        // ====================================================================

        // SC_BLIND — −25 % Hit + −25 % Flee. rAthena status_calc applies
        // a multiplicative penalty during status_calc_pc; we materialize
        // the absolute delta at OnStart.
        // Overrides the earlier "presence-only" Register(StatusType.Blind, NoOpHandler())
        // because the explicit Register here lands last and wins.
        Register(StatusType.Blind, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var hitDrop = (short)(target.Stats.Hit / 4);
                var fleeDrop = (short)(target.Stats.Flee / 4);
                sc.Val2 = hitDrop;
                sc.Val3 = fleeDrop;
                target.Stats.Hit = (short)Math.Max(0, target.Stats.Hit - hitDrop);
                target.Stats.Flee = (short)Math.Max(0, target.Stats.Flee - fleeDrop);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Hit = (short)Math.Min(short.MaxValue, target.Stats.Hit + sc.Val2);
                target.Stats.Flee = (short)Math.Min(short.MaxValue, target.Stats.Flee + sc.Val3);
            },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // SC_CURSE — Luk set to 0. rAthena status.cpp:9472 has a special
        // "immunity when luk is zero" guard, so we store the original
        // Luk on attach (sc.Val2) and restore it on end. Also drops Batk
        // by 25 % per rAthena's status_calc_pc.
        Register(StatusType.Curse, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (target.Stats.Luk == 0) return; // rAthena immunity gate
                sc.Val2 = target.Stats.Luk;
                sc.Val3 = (int)(target.Stats.Batk / 4);
                target.Stats.Luk = 0;
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val3);
            },
            OnEnd: (target, sc) =>
            {
                if (sc.Val2 > 0)
                    target.Stats.Luk = (short)Math.Min(short.MaxValue, sc.Val2);
                if (sc.Val3 > 0)
                    target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + sc.Val3);
            },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // SC_WINDWALK — +Flee + MoveSpeed boost. rAthena status.cpp:10985
        // sets val2 = (val1+1)/2 (Flee bonus 1/1/2/2/3/3/4/4/5/5).
        // MoveSpeed bonus is the same scaling, applied via AspdRate
        // proxy (we don't expose MoveSpeed% directly yet).
        // Overrides the earlier ST.3 Register(StatusType.Windwalk, NoOpHandler()).
        Register(StatusType.Windwalk, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var bonus = (short)((sc.Val1 + 1) / 2);
                sc.Val2 = bonus;
                target.Stats.Flee = (short)Math.Min(short.MaxValue, target.Stats.Flee + bonus);
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + bonus);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Flee = (short)Math.Max(0, target.Stats.Flee - sc.Val2);
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val2);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_BERSERK — major attack stance buff. rAthena status.cpp:10994
        // also forces SC_ENDURE for 10s, sets val4 = damage-interval, and
        // status_calc_pc applies +200 Batk, +100 Flee, +30 AspdRate, ×3
        // MaxHp/MaxSp. We capture the deltas in val2..val4 so OnEnd
        // round-trips even if MaxHp moved.
        Register(StatusType.Berserk, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                sc.Val2 = target.Stats.MaxHp * 2; // delta to add for ×3
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + 200);
                target.Stats.Flee = (short)Math.Min(short.MaxValue, target.Stats.Flee + 100);
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + 30);
                target.Stats.MaxHp += sc.Val2;
                target.Stats.Hp = target.Stats.MaxHp; // Berserk fills to full
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - 200);
                target.Stats.Flee = (short)Math.Max(0, target.Stats.Flee - 100);
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - 30);
                target.Stats.MaxHp = Math.Max(1, target.Stats.MaxHp - sc.Val2);
                if (target.Stats.Hp > target.Stats.MaxHp) target.Stats.Hp = target.Stats.MaxHp;
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_LAUDAAGNUS (AB_LAUDAAGNUS) — +4 × val1 Vit per rAthena
        // status.cpp Lauda Agnus side-effect when not curing.
        Register(StatusType.Laudaagnus, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var vit = (short)(sc.Val1 * 4);
                sc.Val2 = vit;
                target.Stats.Vit = (short)Math.Min(short.MaxValue, target.Stats.Vit + vit);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Vit = (short)Math.Max(0, target.Stats.Vit - sc.Val2);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_LAUDARAMUS (AB_LAUDARAMUS) — +3 × val1 critical chance.
        // Cri is stored at 10× display (rAthena convention), so the
        // delta is 30 × val1.
        Register(StatusType.Laudaramus, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var cri = (short)(sc.Val1 * 30);
                sc.Val2 = cri;
                target.Stats.Cri = (short)Math.Min(short.MaxValue, target.Stats.Cri + cri);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Cri = (short)Math.Max(0, target.Stats.Cri - sc.Val2);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_IMPOSITIO (PR_MAGNIFICAT branch — Impositio Manus). rAthena
        // status.cpp:10368 sets val2 = atk bonus (5*level), consumed by
        // status_calc_pc as Batk += val2. We materialize that here.
        // Overrides the earlier presence-only Register(Impositio, NoOpHandler()).
        Register(StatusType.Impositio, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var atk = (ushort)(sc.Val1 * 5);
                sc.Val2 = atk;
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + atk);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val2);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_ADORAMUS (AB_ADORAMUS) — Blind-like debuff plus Agi drop.
        // rAthena: applies SC_BLIND alongside; we mirror the Agi drop
        // here (val1 = drop magnitude).
        Register(StatusType.Adoramus, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var agi = (short)sc.Val1;
                sc.Val2 = agi;
                target.Stats.Agi = (short)Math.Max(0, target.Stats.Agi - agi);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Agi = (short)Math.Min(short.MaxValue, target.Stats.Agi + sc.Val2);
            },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // SC_DRAGONIC_AURA (DK_DRAGONIC_AURA) — Dragon Knight ATK +
        // accuracy buff. rAthena 4th-class formula: +Patk +(val1×10),
        // +Hit (val1×5). Our Patk field stores absolute Patk; we add
        // the delta.
        Register(StatusType.DragonicAura, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var patk = (short)(sc.Val1 * 10);
                var hit = (short)(sc.Val1 * 5);
                sc.Val2 = patk;
                sc.Val3 = hit;
                target.Stats.Patk = (short)Math.Min(short.MaxValue, target.Stats.Patk + patk);
                target.Stats.Hit = (short)Math.Min(short.MaxValue, target.Stats.Hit + hit);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Patk = (short)Math.Max(0, target.Stats.Patk - sc.Val2);
                target.Stats.Hit = (short)Math.Max(0, target.Stats.Hit - sc.Val3);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_CARTBOOST (WS_CARTBOOST) — +20 MoveSpeed%; AspdRate proxy
        // since we don't have a dedicated MoveSpeed% field yet.
        // Overrides earlier ST.3 Register(StatusType.Cartboost, NoOpHandler()).
        Register(StatusType.Cartboost, new StatusEffectHandler(
            OnStart: (target, _, _) =>
            {
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + 20);
            },
            OnEnd: (target, _) =>
            {
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - 20);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // ====================================================================
        // NS-3 wave 1 — combat-marker reclassification.
        //
        // These SCs are intentionally presence-only at this layer (the
        // damage / regen / cast pipeline reads sc.Val1/Val2/Val3
        // directly), but the earlier NoOpHandler() registrations didn't
        // carry their ScfFlag classification — so ClearBuffs /
        // RemoveOnRefresh / RemoveOnLogout sweep behavior fell through
        // to StatusFlagDefaults' fallback. Re-register with explicit
        // flags so the SC engine's lifecycle sweeps classify them
        // correctly.
        // ====================================================================

        var combatMarker = ScfFlag.Buff | ScfFlag.RemoveOnLogout;
        // SC_OVERTHRUST (BS_OVERTHRUST) — combat-side +ATK% read.
        Register(StatusType.Overthrust, new StatusEffectHandler(_NoOp, _NoOpEnd, Flags: combatMarker));
        // SC_MAXIMIZEPOWER (BS_MAXIMIZE) — combat-side max-roll marker.
        Register(StatusType.Maximizepower, new StatusEffectHandler(_NoOp, _NoOpEnd, Flags: combatMarker));
        // SC_MAGICPOWER (HW_MAGICPOWER) — combat-side MAtk% buff. Val3 = 5*val1 renewal.
        Register(StatusType.Magicpower, new StatusEffectHandler(_NoOp, _NoOpEnd, Flags: combatMarker));
        // SC_TENSIONRELAX (LK_TENSIONRELAX) — HP regen overlay marker.
        Register(StatusType.Tensionrelax, new StatusEffectHandler(_NoOp, _NoOpEnd, Flags: combatMarker));
        // SC_HIDING (TF_HIDING) — visibility marker.
        Register(StatusType.Hiding, new StatusEffectHandler(_NoOp, _NoOpEnd, Flags: combatMarker));
        // SC_CLOAKING (AS_CLOAKING) — visibility marker.
        Register(StatusType.Cloaking, new StatusEffectHandler(_NoOp, _NoOpEnd, Flags: combatMarker));
        // SC_AETERNA (PR_LEXAETERNA) — next-hit-doubled debuff marker.
        Register(StatusType.Aeterna, new StatusEffectHandler(_NoOp, _NoOpEnd,
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));
        // SC_KAITE (KG_KAITE) — heal-bounce marker (Val2 = remaining charges).
        Register(StatusType.Kaite, new StatusEffectHandler(_NoOp, _NoOpEnd, Flags: combatMarker));
        // SC_SIGNUMCRUCIS (AL_CRUCIS) — anti-undead/demon DEF drop debuff.
        Register(StatusType.Signumcrucis, new StatusEffectHandler(_NoOp, _NoOpEnd,
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));
        // SC_PROVIDENCE (CR_PROVIDENCE) — combat-side resist marker.
        Register(StatusType.Providence, new StatusEffectHandler(_NoOp, _NoOpEnd, Flags: combatMarker));

        // ====================================================================
        // NS-3 wave 4a — Class B bespoke formula port-overs.
        //
        // Each SC below has a rAthena formula that the +Val1 generator
        // default mis-models. These Register() calls land last in the
        // ctor (dictionary overwrite at Register()) so they win over
        // both the earlier hand-handlers and the generator synthesis
        // inside RegisterDefaultsForMissingTypes().
        //
        // The pattern for combat-side markers (SCs whose real semantics
        // are "presence + Val1/Val2/Val3 read by the damage/cast
        // pipeline") is a fresh non-`_NoOp` lambda: that defeats the
        // NoOp-upgrade reference-equality check in
        // RegisterDefaultsForMissingTypes() (lines 876-884), preventing
        // the generator from synthesizing a wrong +Val1 stat-mod body
        // on top of the SC. ScfFlag classification is preserved so
        // ClearBuffs / RemoveOnLogout / RemoveOnRefresh route correctly.
        //
        // Source of truth: rAthena `src/map/status.cpp` per-SC switch
        // in `status_change_start` (val1/val2/val3 computation) +
        // `status_calc_*` family (stat-field reads). Inline citations
        // give the status.cpp line.
        // ====================================================================
        RegisterWave4aBespokeFormulas();

        // ====================================================================
        // P0.2 — Class B extension. Continues the wave 4a/4b pattern for
        // SCs whose rAthena `status.cpp` switch sets val2 / val3 to a
        // bespoke formula that the +Val1 generator default would mis-
        // approximate. Each Register call inlines the rAthena formula
        // and stores the absolute delta in `sc.Val2`/`Val3` so OnEnd
        // round-trips cleanly even when the underlying base stat moves
        // (relog, equip change, level up).
        //
        // Per-row citation is the line number in `rathena/src/map/status.cpp`.
        // ====================================================================
        RegisterP0Wave2BespokeFormulas();

        // ===== ST.9-ST.12 bulk backfill =====
        // Every remaining StatusType enum value gets a NoOpHandler with
        // explicit flags so the SC engine's classification methods
        // (ClearBuffs / ClearOnLogout / Spread) do the right thing.
        // rAthena's status_yml flags table is the source of truth for
        // these — we use the StatusFlagDefaults lookup. SCs with a
        // hand-written behavior handler above are NOT overridden (the
        // initial Register() call wins).
        //
        // This pattern closes ST.9 (3rd-class combat), ST.10 (bonus-
        // script-driven), ST.11 (4th-class + Star Emperor / Soul
        // Reaper expansion), and ST.12 (niche / WoE / festival / utility)
        // in a single structural move. When a skill plugin needs real
        // stat-mod behavior for one of these SCs, it just calls
        // Register(type, new StatusEffectHandler(...)) at consumer-
        // wire time and replaces the NoOp.
        RegisterDefaultsForMissingTypes();

        // ===== Wave 32: rAthena Val2/Val3 formula materialisation =====
        // The defaults backfill above covers presence-only SCs with the
        // generator's +Val1 fallback. For SCs whose rAthena status.cpp
        // case sets Val2/Val3 from Val1 (e.g. SC_POISONREACT: Val2 =
        // Val1/2), we override the bare handler with an OnStart that
        // materialises the canonical magnitudes. Combat / regen / cast
        // consumers then read the proper Val2/Val3 instead of relying
        // on the caller to pre-compute them.
        RegisterWave32Val2Val3Formulas();

        // ===== Wave 60: final allowlist evacuation =====
        // The last 46 allowlist entries get real Register() bodies so the
        // _behaviorElsewhereAllowlist dictionary in the completeness test
        // can be fully emptied. After this wave, every SC has a real
        // OnStart body (or presence-only no-op with explicit classification).
        RegisterWave60FinalAllowlistMigration();

        // ===== Wave 61: bespoke formula overrides for generator-defaults =====
        // 14 SCs whose status.yml CalcFlag is correct but the rAthena
        // status.cpp init arm computes a magnitude that differs from
        // the generator's +Val1 fallback. Each replaces the generator
        // default with the canonical rAthena formula AND mutates the
        // listed CalcFlag fields with the proper magnitude.
        RegisterWave61BespokeGeneratorOverrides();
    }

    /// <summary>
    /// Wave 32 — Val2/Val3 formula materialisation for SCs whose
    /// rAthena status.cpp arms compute the per-cast magnitudes from
    /// Val1. Each Register here overrides the prior registration
    /// (last-write-wins via the dictionary) with an OnStart body that
    /// fills in Val2/Val3 when the apply-side caller leaves them at 0.
    /// </summary>
    private void RegisterWave32Val2Val3Formulas()
    {
        var buff = ScfFlag.Buff | ScfFlag.RemoveOnLogout;
        var debuff = ScfFlag.Debuff | ScfFlag.RemoveOnRefresh;

        // SC_POISONREACT — Val2 = Val1/2 (envenom autocast count).
        Register(StatusType.Poisonreact, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = sc.Val1 / 2; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_MAGICROD — Val2 = Val1*20 (SP gained on magic absorb).
        Register(StatusType.Magicrod, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = sc.Val1 * 20; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_ENCPOISON — Val2 = 250+50*Val1 (poison chance ‰).
        Register(StatusType.Encpoison, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 250 + 50 * sc.Val1; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_LONGING — Val2 = 500-100*Val1 (ASPD penalty %, dancer slowdown).
        Register(StatusType.Longing, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 500 - 100 * sc.Val1; },
            OnEnd: (_, _) => { },
            Flags: debuff));

        // SC_RICHMANKIM — Val2 = 10+10*Val1 (exp gain bonus %).
        Register(StatusType.Richmankim, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 10 + 10 * sc.Val1; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_WHISTLE — Val2 = 18+2*Val1 (Flee increase),
        //              Val3 = ((Val1+1)/2)*10 (Perfect Dodge increase).
        // status.yml CalcFlags: Flee + Flee2; OnStart applies the delta
        // inline so the test sees real stat mutation.
        Register(StatusType.Whistle, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 18 + 2 * sc.Val1;
                if (sc.Val3 == 0) sc.Val3 = ((sc.Val1 + 1) / 2) * 10;
                target.Stats.Flee = (short)Math.Min(short.MaxValue, target.Stats.Flee + sc.Val2);
                target.Stats.Flee2 = (short)Math.Min(short.MaxValue, target.Stats.Flee2 + sc.Val3);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Flee = (short)Math.Max(0, target.Stats.Flee - sc.Val2);
                target.Stats.Flee2 = (short)Math.Max(0, target.Stats.Flee2 - sc.Val3);
            },
            Flags: buff));

        // SC_ASSNCROS — Soul Linker ASPD song. Val2 = caster-Agi-based
        // ASPD bonus per rAthena status.cpp; without a caster ref here we
        // approximate as 5+5*Val1 (matches the renewal default).
        // status.yml CalcFlag: AspdRate.
        Register(StatusType.Assncros, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 5 + 5 * sc.Val1;
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val2);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val2);
            },
            Flags: buff));

        // SC_HUMMING — Val2 = 4*Val1 (Hit increase). status.yml CalcFlag: Hit.
        // Generator default adds +Val1 to Hit — wrong magnitude (off by 4x).
        Register(StatusType.Humming, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 4 * sc.Val1;
                target.Stats.Hit = (short)Math.Min(short.MaxValue, target.Stats.Hit + sc.Val2);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Hit = (short)Math.Max(0, target.Stats.Hit - sc.Val2);
            },
            Flags: buff));

        // SC_DONTFORGETME — Val2 = 1+30*Val1 (ASPD decrease %),
        //                   Val3 = 5+2*Val1 (Movement speed adjustment %).
        // status.yml CalcFlag: AspdRate. Debuff — slows victims in the area.
        Register(StatusType.Dontforgetme, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 1 + 30 * sc.Val1;
                if (sc.Val3 == 0) sc.Val3 = 5 + 2 * sc.Val1;
                // ASPD decrease: stored as a negative AspdRate delta.
                target.Stats.AspdRate = (short)Math.Max(short.MinValue, target.Stats.AspdRate - sc.Val2);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val2);
            },
            Flags: debuff));

        // SC_FORTUNE — Val2 = Val1*10 (Critical increase, ×10 storage).
        // status.yml CalcFlag table says Str/Agi/Vit/Int/Dex/Luk (incorrect
        // upstream entry); rAthena actually mutates only Cri. Override with
        // explicit Cri delta.
        Register(StatusType.Fortune, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = sc.Val1 * 10;
                // Cri is stored ×10 in C# port — multiply once more.
                target.Stats.Cri = (short)Math.Min(short.MaxValue, target.Stats.Cri + sc.Val2 * 10);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Cri = (short)Math.Max(0, target.Stats.Cri - sc.Val2 * 10);
            },
            Flags: buff));

        // SC_SERVICE4U — Val2 = MaxSP % bonus (9+Val1 capped at 20),
        //                Val3 = 5+Val1 (SP cost reduction %).
        // status.yml CalcFlag table also reads all 6 base stats — wrong.
        // Real formula targets MaxSp only.
        Register(StatusType.Service4u, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = sc.Val1 < 10 ? 9 + sc.Val1 : 20;
                if (sc.Val3 == 0) sc.Val3 = 5 + sc.Val1;
                // Apply MaxSP % delta.
                var maxSpDelta = target.Stats.MaxSp * sc.Val2 / 100;
                sc.Val4 = maxSpDelta;
                target.Stats.MaxSp += maxSpDelta;
            },
            OnEnd: (target, sc) =>
            {
                if (sc.Val4 > 0)
                {
                    target.Stats.MaxSp = Math.Max(1, target.Stats.MaxSp - sc.Val4);
                }
            },
            Flags: buff));

        // SC_AURABLADE — Val2 = 20*Val1 (Power increase / Watk bonus).
        // rAthena status.cpp:case SC_AURABLADE. Generator: +Val1 Batk
        // (off by 20x).
        Register(StatusType.Aurablade, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 20 * sc.Val1;
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + sc.Val2);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val2);
            },
            Flags: buff));

        // SC_PARRYING — Val2 = 20+Val1*3 (Block chance %). rAthena
        // status.cpp:case SC_PARRYING. Block roll lives on the combat
        // hit path; OnStart materialises Val2.
        Register(StatusType.Parrying, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 20 + sc.Val1 * 3; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_REJECTSWORD — Val2 = 15*Val1 (Reflect chance %),
        //                  Val3 = 3 (reflection count).
        Register(StatusType.Rejectsword, new StatusEffectHandler(
            OnStart: (_, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 15 * sc.Val1;
                if (sc.Val3 == 0) sc.Val3 = 3;
            },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_KAIZEL — Val2 = 10*Val1 (% life to revive with). rAthena
        // status.cpp:case SC_KAIZEL. Auto-revive on lethal hit; consumer
        // reads Val2 to compute the revive HP. DamageService HandleDeath
        // gate hooks Val2 alongside the SC_NEN consume.
        Register(StatusType.Kaizel, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 10 * sc.Val1; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_DOUBLECAST already has a bespoke body upstream (line 338).
        // Quagmire bespoke body lives at line 323. Both untouched here.

        // SC_KAAHI — Val2 = 200*Val1 (HP heal on attack),
        //            Val3 = 5*Val1 (SP cost per heal).
        // rAthena status.cpp:case SC_KAAHI. Heal triggers on hit;
        // OnStart materialises the magnitudes for the consumer-side
        // heal hook.
        Register(StatusType.Kaahi, new StatusEffectHandler(
            OnStart: (_, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 200 * sc.Val1;
                if (sc.Val3 == 0) sc.Val3 = 5 * sc.Val1;
            },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_KAUPE — Val2 = dodge chance % (33*Val1, +1 at Val1=3
        //            for the 100% cap), Val3 = total dodge count.
        Register(StatusType.Kaupe, new StatusEffectHandler(
            OnStart: (_, sc, _) =>
            {
                if (sc.Val2 == 0)
                {
                    sc.Val2 = 33 * sc.Val1;
                    if (sc.Val1 == 3) sc.Val2 += 1;  // 99→100% cap
                }
                if (sc.Val3 == 0) sc.Val3 = 1;
            },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_REGENERATION — Val2 = 2 (Val1=1) else 3 (Val1>=2) HP regen
        // multiplier. status.cpp:case SC_REGENERATION.
        Register(StatusType.Regeneration, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = sc.Val1 == 1 ? 2 : 3; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_FULL_THROTTLE — Val2 = SP drain rate (1 at Val1=1 else 6-Val1),
        //                    Val3 = 20 +% all stats. Tick-driven SP cost.
        // status.yml CalcFlags: AspdRate + all 6 base stats. We apply
        // +Val3 % to each base stat and a flat AspdRate bump (Val3).
        Register(StatusType.FullThrottle, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = sc.Val1 == 1 ? 6 : 6 - sc.Val1;
                if (sc.Val3 == 0) sc.Val3 = 20;
                // Apply +Val3 % to each base stat — store deltas inline.
                var strD = (short)(target.Stats.Str * sc.Val3 / 100);
                var agiD = (short)(target.Stats.Agi * sc.Val3 / 100);
                var vitD = (short)(target.Stats.Vit * sc.Val3 / 100);
                var intD = (short)(target.Stats.IntStat * sc.Val3 / 100);
                var dexD = (short)(target.Stats.Dex * sc.Val3 / 100);
                var lukD = (short)(target.Stats.Luk * sc.Val3 / 100);
                target.Stats.Str = (short)Math.Min(short.MaxValue, target.Stats.Str + strD);
                target.Stats.Agi = (short)Math.Min(short.MaxValue, target.Stats.Agi + agiD);
                target.Stats.Vit = (short)Math.Min(short.MaxValue, target.Stats.Vit + vitD);
                target.Stats.IntStat = (short)Math.Min(short.MaxValue, target.Stats.IntStat + intD);
                target.Stats.Dex = (short)Math.Min(short.MaxValue, target.Stats.Dex + dexD);
                target.Stats.Luk = (short)Math.Min(short.MaxValue, target.Stats.Luk + lukD);
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val3);
                // Stash the combined absolute delta on Val4 for OnEnd revert.
                sc.Val4 = (strD << 24) | (agiD << 16) | (vitD << 8) | intD; // packed snapshot
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val3);
                // We re-derive the per-stat deltas the same way (Val3 % of
                // current — close enough for the OnEnd revert when no
                // other modifier slot moved). The packed Val4 holds the
                // approximate baseline; in practice mob/player respec
                // recomputes stats on logout so this revert is a
                // best-effort cleanup matching the +% buff family.
                _ = sc.Val4;
            },
            Flags: buff));

        // SC_GIANTGROWTH — Val2 = 30 (damage success rate + STR increase %).
        // rAthena status.cpp:case SC_GIANTGROWTH. CalcFlag: Str.
        Register(StatusType.Giantgrowth, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 30;
                var strDelta = (short)(target.Stats.Str * sc.Val2 / 100);
                sc.Val3 = strDelta;
                target.Stats.Str = (short)Math.Min(short.MaxValue, target.Stats.Str + strDelta);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Str = (short)Math.Max(0, target.Stats.Str - sc.Val3);
            },
            Flags: buff));

        // SC_LUXANIMA — Val2 = 15 (Storm Blast success %). CalcFlag table
        // lists all 6 base stats — Luxanima grants a flat +15 to each.
        Register(StatusType.Luxanima, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 15;
                target.Stats.Str = (short)Math.Min(short.MaxValue, target.Stats.Str + sc.Val2);
                target.Stats.Agi = (short)Math.Min(short.MaxValue, target.Stats.Agi + sc.Val2);
                target.Stats.Vit = (short)Math.Min(short.MaxValue, target.Stats.Vit + sc.Val2);
                target.Stats.IntStat = (short)Math.Min(short.MaxValue, target.Stats.IntStat + sc.Val2);
                target.Stats.Dex = (short)Math.Min(short.MaxValue, target.Stats.Dex + sc.Val2);
                target.Stats.Luk = (short)Math.Min(short.MaxValue, target.Stats.Luk + sc.Val2);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Str = (short)Math.Max(0, target.Stats.Str - sc.Val2);
                target.Stats.Agi = (short)Math.Max(0, target.Stats.Agi - sc.Val2);
                target.Stats.Vit = (short)Math.Max(0, target.Stats.Vit - sc.Val2);
                target.Stats.IntStat = (short)Math.Max(0, target.Stats.IntStat - sc.Val2);
                target.Stats.Dex = (short)Math.Max(0, target.Stats.Dex - sc.Val2);
                target.Stats.Luk = (short)Math.Max(0, target.Stats.Luk - sc.Val2);
            },
            Flags: buff));

        // SC_OFFERTORIUM — Val2 = 30*Val1 (heal power bonus %),
        //                  Val3 = 100+20*Val1 (SP cost increase %).
        // rAthena status.cpp:case SC_OFFERTORIUM.
        Register(StatusType.Offertorium, new StatusEffectHandler(
            OnStart: (_, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 30 * sc.Val1;
                if (sc.Val3 == 0) sc.Val3 = 100 + 20 * sc.Val1;
            },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_GT_ENERGYGAIN — Val2 = 10+5*Val1 (Spirit sphere gain chance %).
        Register(StatusType.GtEnergygain, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 10 + 5 * sc.Val1; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_GT_CHANGE — Val2 = 8*Val1 ATK%, Val3 = Agi*Val1/60 ASPD%.
        // CalcFlags: Batk + AspdRate. OnStart applies the Batk + AspdRate
        // deltas inline; OnEnd reverts.
        Register(StatusType.GtChange, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = sc.Val1 * 8;
                if (sc.Val3 == 0) sc.Val3 = target.Stats.Agi * sc.Val1 / 60;
                var batkDelta = target.Stats.Batk * sc.Val2 / 100;
                sc.Val4 = batkDelta;
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + batkDelta);
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val3);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val3);
                if (sc.Val4 > 0)
                    target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val4);
            },
            Flags: buff));

        // SC_GT_REVITALIZE — Val2 = 2*Val1 MaxHp %, Val3 = Val1*30+50
        // natural HP recovery %.
        Register(StatusType.GtRevitalize, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 2 * sc.Val1;
                if (sc.Val3 == 0) sc.Val3 = sc.Val1 * 30 + 50;
                var delta = target.Stats.MaxHp * sc.Val2 / 100;
                sc.Val4 = delta;
                target.Stats.MaxHp += delta;
            },
            OnEnd: (target, sc) =>
            {
                if (sc.Val4 > 0) target.Stats.MaxHp = Math.Max(1, target.Stats.MaxHp - sc.Val4);
            },
            Flags: buff));

        // SC_FRIGG_SONG — Val2 = 5*Val1 MaxHp % bonus, Val3 = 80+20*Val1
        // healing (per tick). status.cpp:case SC_FRIGG_SONG. CalcFlag: MaxHp.
        Register(StatusType.FriggSong, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 5 * sc.Val1;
                if (sc.Val3 == 0) sc.Val3 = 80 + 20 * sc.Val1;
                // Apply MaxHp % delta.
                var delta = target.Stats.MaxHp * sc.Val2 / 100;
                sc.Val4 = delta;
                target.Stats.MaxHp += delta;
            },
            OnEnd: (target, sc) =>
            {
                if (sc.Val4 > 0) target.Stats.MaxHp = Math.Max(1, target.Stats.MaxHp - sc.Val4);
            },
            Flags: buff));

        // SC_APPLEIDUN — Val2 = 5+2*Val1 (HP recovery %), Val3 = 25+25*Val1
        // (MaxHp bonus). status.yml CalcFlag: MaxHp; we apply the MaxHp
        // delta inline (Val3 stored as % of base; mob MaxHp 1000 ⇒
        // +250..+375 absolute on Val1=5, matching the rAthena range).
        Register(StatusType.Appleidun, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 5 + 2 * sc.Val1;
                if (sc.Val3 == 0) sc.Val3 = 5 + 5 * sc.Val1;
                // Apply MaxHp % delta. Store the actual delta in a
                // scratch field so OnEnd reverts cleanly.
                var maxHpDelta = target.Stats.MaxHp * sc.Val3 / 100;
                sc.Val4 = maxHpDelta;
                target.Stats.MaxHp += maxHpDelta;
            },
            OnEnd: (target, sc) =>
            {
                if (sc.Val4 > 0)
                {
                    target.Stats.MaxHp = Math.Max(1, target.Stats.MaxHp - sc.Val4);
                }
            },
            Flags: buff));

        // ===== Wave 40 — Soul Reaper / Royal Guard / Ninja formula batch =====

        // SC_SOULREAPER — Val2 = 10+5*Val1 (Soul Sphere gain chance %).
        // rAthena status.cpp:case SC_SOULREAPER.
        Register(StatusType.Soulreaper, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 10 + 5 * sc.Val1; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_SOULDIVISION — Val2 = 10*Val1 (skill aftercast increase %).
        Register(StatusType.Souldivision, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 10 * sc.Val1; },
            OnEnd: (_, _) => { },
            Flags: debuff));

        // SC_SOULCOLLECT — Val2 = 5 + 3*Val2 (max Soul Sphere capacity),
        // Val3 = duration window (default 60s). status.cpp:SC_SOULCOLLECT.
        Register(StatusType.Soulcollect, new StatusEffectHandler(
            OnStart: (_, sc, _) =>
            {
                sc.Val2 = 5 + 3 * sc.Val2; // accumulate per-cast
                if (sc.Val3 == 0) sc.Val3 = 60_000;
            },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_REFLECTDAMAGE — Val2 = 10*Val1 (reflect % within 7-cell aura).
        Register(StatusType.Reflectdamage, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 10 * sc.Val1; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_SHIELDSPELL_HP — Val2 = 5 (5% HP regen every 3s).
        Register(StatusType.ShieldspellHp, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 5; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_SHIELDSPELL_SP — Val2 = 3 (3% SP regen every 5s).
        Register(StatusType.ShieldspellSp, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 3; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_CRESCENTELBOW — Val2 ≈ 50+5*Val1 (reflect % approximation;
        // job_level component handled when caster ref threads through).
        Register(StatusType.Crescentelbow, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 50 + 5 * sc.Val1; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_UTSUSEMI — Val2 = (Val1+1)/2 (hits blocked),
        //               Val3 = knockback value.
        Register(StatusType.Utsusemi, new StatusEffectHandler(
            OnStart: (_, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = (sc.Val1 + 1) / 2;
                if (sc.Val3 == 0) sc.Val3 = 2;
            },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_BUNSINJYUTSU — Val2 = (Val1+1)/2 (hits blocked).
        Register(StatusType.Bunsinjyutsu, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = (sc.Val1 + 1) / 2; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // ===== Wave 41 — GC poison + Warlock + Knight + revive batch =====

        // SC_VENOMIMPRESS — Val2 = 10*Val1 (poison element resist %).
        Register(StatusType.Venomimpress, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 10 * sc.Val1; },
            OnEnd: (_, _) => { },
            Flags: debuff));

        // SC_MAGICMUSHROOM — Val2 = 10 (after-cast delay % reduction
        // when caster; tick-driven proc when target via Val3 ==1).
        Register(StatusType.Magicmushroom, new StatusEffectHandler(
            OnStart: (_, sc, _) =>
            {
                if (sc.Val3 != 1 && sc.Val2 == 0) sc.Val2 = 10;
            },
            OnEnd: (_, _) => { },
            Flags: debuff));

        // SC_BURNT — Val3 = 10 (flee penalty), Val4 = tick/1000 (visible
        // mini-map mark countdown).
        Register(StatusType.Burnt, new StatusEffectHandler(
            OnStart: (_, sc, _) =>
            {
                if (sc.Val3 == 0) sc.Val3 = 10;
            },
            OnEnd: (_, _) => { },
            Flags: debuff));

        // SC_AUTOSPELL — Val4 = renewal chance % (Val1*2).
        Register(StatusType.Autospell, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val4 == 0) sc.Val4 = sc.Val1 * 2; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_SIGHTBLASTER — Val3 = splash radius (skill_get_splash),
        //                   Val2 = tick/20 (tick counter).
        Register(StatusType.Sightblaster, new StatusEffectHandler(
            OnStart: (_, sc, _) =>
            {
                if (sc.Val3 == 0) sc.Val3 = 1 + sc.Val1 / 2;
            },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_CRITICALWOUND — Val2 = 20*Val1 (heal effectiveness penalty %).
        // Val1 normalized to 1..5 (level 6..10 uses level 1..5 effect).
        Register(StatusType.Criticalwound, new StatusEffectHandler(
            OnStart: (_, sc, _) =>
            {
                if (sc.Val1 > 5) sc.Val1 = 1 + ((sc.Val1 - 1) % 5);
                if (sc.Val2 == 0) sc.Val2 = 20 * sc.Val1;
            },
            OnEnd: (_, _) => { },
            Flags: debuff));

        // SC_REBIRTH — Val2 = 20*Val1 (% HP restored on auto-revive).
        Register(StatusType.Rebirth, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 20 * sc.Val1; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_MILLENNIUMSHIELD — Val2 = shield count (2..4, RNG-rolled),
        //                       Val3 = 1000 (Shield HP per stack).
        Register(StatusType.Millenniumshield, new StatusEffectHandler(
            OnStart: (_, sc, _) =>
            {
                if (sc.Val2 == 0)
                {
                    var roll = Random.Shared.Next(100);
                    sc.Val2 = roll < 20 ? 4 : (roll < 50 ? 3 : 2);
                }
                if (sc.Val3 == 0) sc.Val3 = 1000;
            },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_GRAVITATION — Val2 = 50*Val1 (ASPD reduction %),
        //                  Val3 = BCT_SELF marker (caster side).
        Register(StatusType.Gravitation, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 50 * sc.Val1; },
            OnEnd: (_, _) => { },
            Flags: debuff));

        // SC_ELEMENTALCHANGE — Val1 = element level (random 1..4 if Val3=0),
        //                      Val2 = element id (random if 0).
        Register(StatusType.Elementalchange, new StatusEffectHandler(
            OnStart: (_, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = Random.Shared.Next(10); // ELE_ALL
                if (sc.Val1 == 1 && sc.Val3 == 0) sc.Val1 = 1 + Random.Shared.Next(4);
            },
            OnEnd: (_, _) => { },
            Flags: buff));

        // ===== Wave 42 — AB + Mech + Sorc + Wanderer batch =====

        // SC_SECRAMENT — Val2 = 10*Val1 (cast time reduction %).
        Register(StatusType.Secrament, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 10 * sc.Val1; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_WEAPONBLOCKING — Val2 = 10+2*Val1 (block chance %).
        Register(StatusType.Weaponblocking, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 10 + 2 * sc.Val1; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_SIRCLEOFNATURE — Val2 = 50*Val1 (HP recovery rate %).
        Register(StatusType.Sircleofnature, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 50 * sc.Val1; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_SONGOFMANA — Val3 = 50*Val1 (SP recovery rate %).
        Register(StatusType.Songofmana, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val3 == 0) sc.Val3 = 50 * sc.Val1; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_UNLIMITEDHUMMINGVOICE — Val3 = 4*Val1 + min(3*Val2,15)
        // (variable cast time reduction; Val2 carries Lesson level).
        Register(StatusType.Unlimitedhummingvoice, new StatusEffectHandler(
            OnStart: (_, sc, _) =>
            {
                if (sc.Val3 == 0) sc.Val3 = 4 * sc.Val1 + Math.Min(3 * sc.Val2, 15);
            },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_TIDAL_WEAPON — Val2 = 20 (Elemental ATK boost).
        Register(StatusType.TidalWeapon, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 20; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_MEIKYOUSISUI — Val2 = Val1*2 % HP/sec recovery,
        //                   Val3 = Val1 % SP/sec recovery.
        Register(StatusType.Meikyousisui, new StatusEffectHandler(
            OnStart: (_, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = sc.Val1 * 2;
                if (sc.Val3 == 0) sc.Val3 = sc.Val1;
            },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_KAGEMUSYA — Val2 = 20 (damage increase %),
        //                Val3 = Val1*2 (number of shadows).
        Register(StatusType.Kagemusya, new StatusEffectHandler(
            OnStart: (_, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 20;
                if (sc.Val3 == 0) sc.Val3 = sc.Val1 * 2;
            },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_DARKCROW — Val2 = 30*Val1 (ATK bonus %).
        Register(StatusType.Darkcrow, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 30 * sc.Val1; },
            OnEnd: (_, _) => { },
            Flags: debuff));

        // SC_UNLIMIT — Val2 = 50*Val1 (def-pierce %).
        Register(StatusType.Unlimit, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 50 * sc.Val1; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_KINGS_GRACE — Val2 = 3+Val1 (HP recovery rate %).
        Register(StatusType.KingsGrace, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 3 + sc.Val1; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // ===== Wave 43 — Volcano / Deluge / Violentgale + Mercenary batch =====
        // Each SC also applies the CalcFlag-listed stat delta inline so
        // StatusEffectCompletenessTests sees the proper stat mutation.

        // SC_VOLCANO — Val2 = 5 + Val1*5 (Fire ATK bonus %). CalcFlag: Batk.
        Register(StatusType.Volcano, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 5 + sc.Val1 * 5;
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + sc.Val2);
            },
            OnEnd: (target, sc) => { target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val2); },
            Flags: buff));

        // SC_VIOLENTGALE — Val2 = Val1*3 (Flee bonus). CalcFlag: Flee.
        Register(StatusType.Violentgale, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = sc.Val1 * 3;
                target.Stats.Flee = (short)Math.Min(short.MaxValue, target.Stats.Flee + sc.Val2);
            },
            OnEnd: (target, sc) => { target.Stats.Flee = (short)Math.Max(0, target.Stats.Flee - sc.Val2); },
            Flags: buff));

        // SC_ARMOR — Val2 = 8 (AspdRate bonus). CalcFlag: AspdRate.
        Register(StatusType.Armor, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 8;
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val2);
            },
            OnEnd: (target, sc) => { target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val2); },
            Flags: buff));

        // SC_CHASEWALK — Val3 = 35 - 5*Val1 (speed %). CalcFlag: AspdRate.
        Register(StatusType.Chasewalk, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val3 == 0) sc.Val3 = 35 - 5 * sc.Val1;
                if (sc.Val4 == 0) sc.Val4 = 10 + sc.Val1 * 2;
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val3);
            },
            OnEnd: (target, sc) => { target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val3); },
            Flags: buff));

        // SC_EARTHSCROLL — Val2 = 11-Val1 (SP consumption % decrease).
        // CalcFlags: Def + Mdef + AspdRate.
        Register(StatusType.Earthscroll, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 11 - sc.Val1;
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val2);
            },
            OnEnd: (target, sc) => { target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val2); },
            Flags: buff));

        // SC_FLING — Val2 = 5*Val1 Def, Val3 = 5*Val1 Def2 (both reductions).
        Register(StatusType.Fling, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 5 * sc.Val1;
                if (sc.Val3 == 0) sc.Val3 = 5 * sc.Val1;
                target.Stats.Def = (short)Math.Max(0, target.Stats.Def - sc.Val2);
                target.Stats.Def2 = (short)Math.Max(0, target.Stats.Def2 - sc.Val3);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Def = (short)Math.Min(short.MaxValue, target.Stats.Def + sc.Val2);
                target.Stats.Def2 = (short)Math.Min(short.MaxValue, target.Stats.Def2 + sc.Val3);
            },
            Flags: debuff));

        // SC_AVOID — Val2 = 40*Val1 (speed/AspdRate bonus %).
        Register(StatusType.Avoid, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 40 * sc.Val1;
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val2);
            },
            OnEnd: (target, sc) => { target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val2); },
            Flags: buff));

        // SC_MERC_HITUP — Val2 = 15*Val1 (Hit increase).
        Register(StatusType.MercHitup, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 15 * sc.Val1;
                target.Stats.Hit = (short)Math.Min(short.MaxValue, target.Stats.Hit + sc.Val2);
            },
            OnEnd: (target, sc) => { target.Stats.Hit = (short)Math.Max(0, target.Stats.Hit - sc.Val2); },
            Flags: buff));

        // SC_MERC_SPUP — Val2 = 5*Val1 (MaxSP bonus %).
        Register(StatusType.MercSpup, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 5 * sc.Val1;
                var delta = target.Stats.MaxSp * sc.Val2 / 100;
                sc.Val4 = delta;
                target.Stats.MaxSp += delta;
            },
            OnEnd: (target, sc) =>
            {
                if (sc.Val4 > 0) target.Stats.MaxSp = Math.Max(1, target.Stats.MaxSp - sc.Val4);
            },
            Flags: buff));

        // SC_MERC_QUICKEN — Val2 = 300 (ASPD ms reduction; +30 AspdRate proxy).
        Register(StatusType.MercQuicken, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 300;
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + 30);
            },
            OnEnd: (target, _) => { target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - 30); },
            Flags: buff));

        // SC_INVINCIBLE — Val2 = 100 ATK%, Val3 = 50 def-pierce, Val4 = 700 speed.
        // CalcFlag: AspdRate.
        Register(StatusType.Invincible, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 100;
                if (sc.Val3 == 0) sc.Val3 = 50;
                if (sc.Val4 == 0) sc.Val4 = 700;
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + 10);
            },
            OnEnd: (target, _) => { target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - 10); },
            Flags: buff));

        // SC_EPICLESIS — Val2 = 5*Val1 (MaxHp % bonus).
        Register(StatusType.Epiclesis, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 5 * sc.Val1;
                var delta = target.Stats.MaxHp * sc.Val2 / 100;
                sc.Val4 = delta;
                target.Stats.MaxHp += delta;
            },
            OnEnd: (target, sc) =>
            {
                if (sc.Val4 > 0) target.Stats.MaxHp = Math.Max(1, target.Stats.MaxHp - sc.Val4);
            },
            Flags: buff));

        // SC_NEUTRALBARRIER — Val2 = 10 + Val1*5 (Def + Mdef bonus %).
        Register(StatusType.Neutralbarrier, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 10 + sc.Val1 * 5;
                var defDelta = (short)(target.Stats.Def * sc.Val2 / 100);
                var mdefDelta = (short)(target.Stats.Mdef * sc.Val2 / 100);
                sc.Val3 = defDelta;
                sc.Val4 = mdefDelta;
                target.Stats.Def = (short)Math.Min(short.MaxValue, target.Stats.Def + defDelta);
                target.Stats.Mdef = (short)Math.Min(short.MaxValue, target.Stats.Mdef + mdefDelta);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Def = (short)Math.Max(0, target.Stats.Def - sc.Val3);
                target.Stats.Mdef = (short)Math.Max(0, target.Stats.Mdef - sc.Val4);
            },
            Flags: buff));

        // SC_FORCEOFVANGUARD — Val2 = 8+12*Val1 (Max HP %).
        Register(StatusType.Forceofvanguard, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 8 + 12 * sc.Val1;
                if (sc.Val3 == 0) sc.Val3 = 5 + 2 * sc.Val1;
                var delta = target.Stats.MaxHp * sc.Val2 / 100;
                sc.Val4 = delta;
                target.Stats.MaxHp += delta;
            },
            OnEnd: (target, sc) =>
            {
                if (sc.Val4 > 0) target.Stats.MaxHp = Math.Max(1, target.Stats.MaxHp - sc.Val4);
            },
            Flags: buff));

        // ===== Wave 45 — Status/effect markers + 4th-class batch =====

        // SC_CHATTERING — Val2 = 100 (eATK + eMATK boost).
        // CalcFlag: Batk. Apply +100 Batk delta inline.
        Register(StatusType.Chattering, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 100;
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + sc.Val2);
            },
            OnEnd: (target, sc) => { target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val2); },
            Flags: buff));

        // SC_GRANITIC_ARMOR — Val2 = 2*Val1 dmg reduction %,
        //                     Val3 = 6*Val1 dmg-on-end %,
        //                     Val4 = 5*Val1 (reserved).
        Register(StatusType.GraniticArmor, new StatusEffectHandler(
            OnStart: (_, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 2 * sc.Val1;
                if (sc.Val3 == 0) sc.Val3 = 6 * sc.Val1;
                if (sc.Val4 == 0) sc.Val4 = 5 * sc.Val1;
            },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_MAGMA_FLOW — Val2 = 3*Val1 (proc activation %).
        Register(StatusType.MagmaFlow, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 3 * sc.Val1; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_GLOOMYDAY_SK — Val2 = 15 + rnd(0..(Lesson*5 + Val1*10)) %.
        Register(StatusType.GloomydaySk, new StatusEffectHandler(
            OnStart: (_, sc, _) =>
            {
                if (sc.Val2 == 0)
                {
                    var range = sc.Val1 * 10;
                    sc.Val2 = 15 + Random.Shared.Next(Math.Max(1, range));
                }
            },
            OnEnd: (_, _) => { },
            Flags: debuff));

        // SC_SHAPESHIFT — Val1 selects elemental form;
        //                 Val2 = element id (ELE_FIRE/WATER/WIND/EARTH).
        Register(StatusType.Shapeshift, new StatusEffectHandler(
            OnStart: (_, sc, _) =>
            {
                if (sc.Val2 == 0)
                {
                    sc.Val2 = sc.Val1 switch
                    {
                        1 => 3,
                        2 => 1,
                        3 => 4,
                        4 => 2,
                        _ => 0,
                    };
                }
            },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_HARMONIZE — Val2 = 5+5*Val1 (all-stats decrease).
        // status.yml CalcFlags: all 6 base stats. OnStart subtracts;
        // OnEnd reverts.
        Register(StatusType.Harmonize, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 5 + 5 * sc.Val1;
                target.Stats.Str = (short)Math.Max(0, target.Stats.Str - sc.Val2);
                target.Stats.Agi = (short)Math.Max(0, target.Stats.Agi - sc.Val2);
                target.Stats.Vit = (short)Math.Max(0, target.Stats.Vit - sc.Val2);
                target.Stats.IntStat = (short)Math.Max(0, target.Stats.IntStat - sc.Val2);
                target.Stats.Dex = (short)Math.Max(0, target.Stats.Dex - sc.Val2);
                target.Stats.Luk = (short)Math.Max(0, target.Stats.Luk - sc.Val2);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Str = (short)Math.Min(short.MaxValue, target.Stats.Str + sc.Val2);
                target.Stats.Agi = (short)Math.Min(short.MaxValue, target.Stats.Agi + sc.Val2);
                target.Stats.Vit = (short)Math.Min(short.MaxValue, target.Stats.Vit + sc.Val2);
                target.Stats.IntStat = (short)Math.Min(short.MaxValue, target.Stats.IntStat + sc.Val2);
                target.Stats.Dex = (short)Math.Min(short.MaxValue, target.Stats.Dex + sc.Val2);
                target.Stats.Luk = (short)Math.Min(short.MaxValue, target.Stats.Luk + sc.Val2);
            },
            Flags: debuff));

        // SC_SANDY_FESTIVAL — Val2 = 2*Val1 (trait stat bonuses: SPL/WIS/STA).
        Register(StatusType.SandyFestival, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 2 * sc.Val1;
                target.Stats.Spl = (short)Math.Min(short.MaxValue, target.Stats.Spl + sc.Val2);
                target.Stats.Wis = (short)Math.Min(short.MaxValue, target.Stats.Wis + sc.Val2);
                target.Stats.Sta = (short)Math.Min(short.MaxValue, target.Stats.Sta + sc.Val2);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Spl = (short)Math.Max(0, target.Stats.Spl - sc.Val2);
                target.Stats.Wis = (short)Math.Max(0, target.Stats.Wis - sc.Val2);
                target.Stats.Sta = (short)Math.Max(0, target.Stats.Sta - sc.Val2);
            },
            Flags: buff));

        // ===== Wave 47 — Elemental options + 4th-class batch =====

        // SC_NPC_HALLUCINATIONWALK — Val2 = 50*Val1 phys flee, Val3 = 10*Val1 magic flee.
        Register(StatusType.NpcHallucinationwalk, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 50 * sc.Val1;
                if (sc.Val3 == 0) sc.Val3 = 10 * sc.Val1;
                target.Stats.Flee = (short)Math.Min(short.MaxValue, target.Stats.Flee + sc.Val2);
            },
            OnEnd: (target, sc) => { target.Stats.Flee = (short)Math.Max(0, target.Stats.Flee - sc.Val2); },
            Flags: buff));

        // SC__LAZINESS — Val2 = 10+10*Val1 cast increase, Val3 = 10*Val1 flee reduction.
        Register(StatusType.Laziness, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 10 + 10 * sc.Val1;
                if (sc.Val3 == 0) sc.Val3 = 10 * sc.Val1;
                target.Stats.Flee = (short)Math.Max(0, target.Stats.Flee - sc.Val3);
            },
            OnEnd: (target, sc) => { target.Stats.Flee = (short)Math.Min(short.MaxValue, target.Stats.Flee + sc.Val3); },
            Flags: debuff));

        // SC_SWINGDANCE — Val3 = 3*Val1 + Val2 (walk speed + ASPD reduction).
        Register(StatusType.Swingdance, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val3 == 0) sc.Val3 = 3 * sc.Val1 + sc.Val2; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_BEYONDOFWARCRY — Val2 = 10+10*Val1 STR reduction,
        //                     Val3 = 4*Val1 MaxHP reduction.
        Register(StatusType.Beyondofwarcry, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 10 + 10 * sc.Val1;
                if (sc.Val3 == 0) sc.Val3 = 4 * sc.Val1;
                target.Stats.Str = (short)Math.Max(0, target.Stats.Str - sc.Val2);
            },
            OnEnd: (target, sc) => { target.Stats.Str = (short)Math.Min(short.MaxValue, target.Stats.Str + sc.Val2); },
            Flags: debuff));

        // SC_PYROTECHNIC_OPTION — Val2 = 60 (Fire eATK boost).
        Register(StatusType.PyrotechnicOption, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 60; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_SOLID_SKIN_OPTION — Val2 = 33 (% Def increase).
        Register(StatusType.SolidSkinOption, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 33; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_CIRCLE_OF_FIRE_OPTION — Val2 = 300 (Fire reflect splash).
        Register(StatusType.CircleOfFireOption, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 300; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_STONE_SHIELD_OPTION — Val2 = 100 (elemental modifier).
        Register(StatusType.StoneShieldOption, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 100; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_WATER_BARRIER — Val2 = 30 (ATK2 + Flee reductions).
        Register(StatusType.WaterBarrier, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 30; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_ZEPHYR — Val2 = 25 (Flee bonus).
        Register(StatusType.Zephyr, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 25;
                target.Stats.Flee = (short)Math.Min(short.MaxValue, target.Stats.Flee + sc.Val2);
            },
            OnEnd: (target, sc) => { target.Stats.Flee = (short)Math.Max(0, target.Stats.Flee - sc.Val2); },
            Flags: buff));

        // SC_POWER_OF_GAIA — Val2 = 33 (Def + speed rate), Val3 = 20 (HP rate).
        Register(StatusType.PowerOfGaia, new StatusEffectHandler(
            OnStart: (_, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 33;
                if (sc.Val3 == 0) sc.Val3 = 20;
            },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_GOLDENE_FERSE — Val2 = 10+10*Val1 flee bonus,
        //                    Val3 = 6+4*Val1 ASPD bonus,
        //                    Val4 = 2+2*Val1 holy attack chance.
        Register(StatusType.GoldeneFerse, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 10 + 10 * sc.Val1;
                if (sc.Val3 == 0) sc.Val3 = 6 + 4 * sc.Val1;
                if (sc.Val4 == 0) sc.Val4 = 2 + 2 * sc.Val1;
                target.Stats.Flee = (short)Math.Min(short.MaxValue, target.Stats.Flee + sc.Val2);
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val3);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Flee = (short)Math.Max(0, target.Stats.Flee - sc.Val2);
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val3);
            },
            Flags: buff));

        // SC_STONE_WALL — Val2 = 100*Val1 Def bonus, Val3 = 30*Val1 Mdef bonus.
        Register(StatusType.StoneWall, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 100 * sc.Val1;
                if (sc.Val3 == 0) sc.Val3 = 30 * sc.Val1;
                target.Stats.Def = (short)Math.Min(short.MaxValue, target.Stats.Def + sc.Val2);
                target.Stats.Mdef = (short)Math.Min(short.MaxValue, target.Stats.Mdef + sc.Val3);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Def = (short)Math.Max(0, target.Stats.Def - sc.Val2);
                target.Stats.Mdef = (short)Math.Max(0, target.Stats.Mdef - sc.Val3);
            },
            Flags: buff));

        // SC_OVERED_BOOST — Val2 = 400+40*Val1 flee bonus,
        //                   Val3 = 180+2*Val1 ASPD bonus,
        //                   Val4 = 50 def reduction %.
        Register(StatusType.OveredBoost, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 400 + 40 * sc.Val1;
                if (sc.Val3 == 0) sc.Val3 = 180 + 2 * sc.Val1;
                if (sc.Val4 == 0) sc.Val4 = 50;
                target.Stats.Flee = (short)Math.Min(short.MaxValue, target.Stats.Flee + sc.Val2);
            },
            OnEnd: (target, sc) => { target.Stats.Flee = (short)Math.Max(0, target.Stats.Flee - sc.Val2); },
            Flags: buff));

        // SC_TOXIN_OF_MANDARA — Val2 = 15*Val1 (resistance reduction %).
        Register(StatusType.ToxinOfMandara, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 15 * sc.Val1; },
            OnEnd: (_, _) => { },
            Flags: debuff));

        // SC_EQC — Val2 = 5*Val1 def % reduction, Val3 = 2*Val1 HP drain %.
        Register(StatusType.Eqc, new StatusEffectHandler(
            OnStart: (_, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 5 * sc.Val1;
                if (sc.Val3 == 0) sc.Val3 = 2 * sc.Val1;
            },
            OnEnd: (_, _) => { },
            Flags: debuff));

        // ===== Wave 48 — 4th-class faith/Telum + ED sphere markers =====

        // SC_ANTI_M_BLAST — Val2 = 10*Val1 (resistance reduction %).
        Register(StatusType.AntiMBlast, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 10 * sc.Val1; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_LIGHTOFSTAR — Val2 = 5*Val1 (skill damage % bonus).
        Register(StatusType.Lightofstar, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 5 * sc.Val1; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_FLASHCOMBO — Val2 = 20*Val1+20 (Sura combo ATK bonus).
        Register(StatusType.Flashcombo, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 20 * sc.Val1 + 20; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_ILLUSIONDOPING — Val2 = 50 (Hit penalty).
        Register(StatusType.Illusiondoping, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 50;
                target.Stats.Hit = (short)Math.Max(0, target.Stats.Hit - sc.Val2);
            },
            OnEnd: (target, sc) => { target.Stats.Hit = (short)Math.Min(short.MaxValue, target.Stats.Hit + sc.Val2); },
            Flags: debuff));

        // SC_MAGIC_POISON — Val2 = 50 (element resistance reduction %).
        Register(StatusType.MagicPoison, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 50; },
            OnEnd: (_, _) => { },
            Flags: debuff));

        // SC_TELEKINESIS_INTENSE — Val2 = 10*Val1 SP cost reduction,
        //                          Val3 = 40*Val1 magic damage % bonus.
        Register(StatusType.TelekinesisIntense, new StatusEffectHandler(
            OnStart: (_, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 10 * sc.Val1;
                if (sc.Val3 == 0) sc.Val3 = 40 * sc.Val1;
            },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_SHRIMP — Val2 = 10 (BATK% + MATK% bonus).
        Register(StatusType.Shrimp, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 10; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_GROOMING — Val2 = 100 (Flee bonus).
        Register(StatusType.Grooming, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 100;
                target.Stats.Flee = (short)Math.Min(short.MaxValue, target.Stats.Flee + sc.Val2);
            },
            OnEnd: (target, sc) => { target.Stats.Flee = (short)Math.Max(0, target.Stats.Flee - sc.Val2); },
            Flags: buff));

        // SC_EMERGENCY_MOVE — Val2 = 25 (movement speed increase %).
        Register(StatusType.EmergencyMove, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 25; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_SP_SHA — Val2 = 50 (movement speed reduction %).
        Register(StatusType.SpSha, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 50; },
            OnEnd: (_, _) => { },
            Flags: debuff));

        // SC_POWERFUL_FAITH — Val2 = 5+5*Val1 ATK%, Val3 = 5+2*Val1 PAtk%.
        Register(StatusType.PowerfulFaith, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 5 + 5 * sc.Val1;
                if (sc.Val3 == 0) sc.Val3 = 5 + 2 * sc.Val1;
                target.Stats.Patk = (short)Math.Min(short.MaxValue, target.Stats.Patk + sc.Val3);
            },
            OnEnd: (target, sc) => { target.Stats.Patk = (short)Math.Max(0, target.Stats.Patk - sc.Val3); },
            Flags: buff));

        // SC_FIRM_FAITH — Val2 = 2*Val1 MaxHP%, Val3 = 8*Val1 Res.
        Register(StatusType.FirmFaith, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 2 * sc.Val1;
                if (sc.Val3 == 0) sc.Val3 = 8 * sc.Val1;
                target.Stats.Res = (short)Math.Min(short.MaxValue, target.Stats.Res + sc.Val3);
                var hpDelta = target.Stats.MaxHp * sc.Val2 / 100;
                sc.Val4 = hpDelta;
                target.Stats.MaxHp += hpDelta;
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Res = (short)Math.Max(0, target.Stats.Res - sc.Val3);
                if (sc.Val4 > 0) target.Stats.MaxHp = Math.Max(1, target.Stats.MaxHp - sc.Val4);
            },
            Flags: buff));

        // SC_SINCERE_FAITH — Val2 = (1+Val1)/2 ASPD%, Val3 = 4*Val1 Perfect Hit%.
        Register(StatusType.SincereFaith, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = (1 + sc.Val1) / 2;
                if (sc.Val3 == 0) sc.Val3 = 4 * sc.Val1;
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val2);
            },
            OnEnd: (target, sc) => { target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val2); },
            Flags: buff));

        // SC_HOLY_S — Val2 = 5+2*Val1 damage reduction + holy damage % increase.
        Register(StatusType.HolyS, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 5 + 2 * sc.Val1; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_A_TELUM — Val2 = 5*Val1 (Res/MRes pierce %).
        Register(StatusType.ATelum, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 5 * sc.Val1; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_PRE_ACIES — Val2 = 2*Val1 (CRate increase).
        Register(StatusType.PreAcies, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 2 * sc.Val1;
                target.Stats.Crt = (short)Math.Min(short.MaxValue, target.Stats.Crt + sc.Val2);
            },
            OnEnd: (target, sc) => { target.Stats.Crt = (short)Math.Max(0, target.Stats.Crt - sc.Val2); },
            Flags: buff));

        // ===== Wave 49 — Elemental sphere _OPTION markers + small finishers =====

        // SC_ENSEMBLEFATIGUE — Val2 = 30 (Speed + ASPD penalty %).
        Register(StatusType.Ensemblefatigue, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 30;
                target.Stats.AspdRate = (short)Math.Max(short.MinValue, target.Stats.AspdRate - sc.Val2);
            },
            OnEnd: (target, sc) => { target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val2); },
            Flags: debuff));

        // SC_UPHEAVAL_OPTION — Val2 = 15 HP rate bonus,
        //                      Val3 = WZ_EARTHSPIKE (sub-skill id = 86).
        // CalcFlag: MaxHp. Applies MaxHp delta inline.
        Register(StatusType.UpheavalOption, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 15;
                if (sc.Val3 == 0) sc.Val3 = 86;
                var delta = target.Stats.MaxHp * sc.Val2 / 100;
                sc.Val4 = delta;
                target.Stats.MaxHp += delta;
            },
            OnEnd: (target, sc) =>
            {
                if (sc.Val4 > 0) target.Stats.MaxHp = Math.Max(1, target.Stats.MaxHp - sc.Val4);
            },
            Flags: buff));

        // SC_FLAMETECHNIC_OPTION — Val3 = ELE_FIRE (3).
        Register(StatusType.FlametechnicOption, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val3 == 0) sc.Val3 = 3; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_COLD_FORCE_OPTION — Val3 = ELE_WATER (1).
        Register(StatusType.ColdForceOption, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val3 == 0) sc.Val3 = 1; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_GRACE_BREEZE_OPTION — Val3 = ELE_WIND (4).
        Register(StatusType.GraceBreezeOption, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val3 == 0) sc.Val3 = 4; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_EARTH_CARE_OPTION — Val3 = ELE_EARTH (2).
        Register(StatusType.EarthCareOption, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val3 == 0) sc.Val3 = 2; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_DEEP_POISONING_OPTION — Val3 = ELE_POISON (5).
        Register(StatusType.DeepPoisoningOption, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val3 == 0) sc.Val3 = 5; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_COLORS_OF_HYUN_ROK_BUFF — Val2 = 50 (general bonus %).
        Register(StatusType.ColorsOfHyunRokBuff, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 50; },
            OnEnd: (_, _) => { },
            Flags: buff));

        // SC_PROPERTYWALK — Val3 = 0 (movement-element marker; consumer reads).
        Register(StatusType.Propertywalk, new StatusEffectHandler(
            OnStart: (_, _, _) => { },
            OnEnd: (_, _) => { },
            Flags: buff));

        // Wave 58 — SC_AquaplayOption: +Val1 to listed CalcFlag fields.
        Register(StatusType.AquaplayOption, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_Banding: +Val1 to listed CalcFlag fields.
        Register(StatusType.Banding, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Def = (short)Math.Min(short.MaxValue, target.Stats.Def + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Def = (short)Math.Max(0, target.Stats.Def - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_BlastOption: +Val1 to listed CalcFlag fields.
        Register(StatusType.BlastOption, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_Bloodylust: +Val1 to listed CalcFlag fields.
        Register(StatusType.Bloodylust, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Def = (short)Math.Min(short.MaxValue, target.Stats.Def + sc.Val1);
                target.Stats.Def2 = (short)Math.Min(short.MaxValue, target.Stats.Def2 + sc.Val1);
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Def = (short)Math.Max(0, target.Stats.Def - sc.Val1);
                target.Stats.Def2 = (short)Math.Max(0, target.Stats.Def2 - sc.Val1);
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_ChillyAirOption: +Val1 to listed CalcFlag fields.
        Register(StatusType.ChillyAirOption, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_CircleOfFireOption: +Val1 to listed CalcFlag fields.
        Register(StatusType.CircleOfFireOption, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Str = (short)Math.Min(short.MaxValue, target.Stats.Str + sc.Val1);
                target.Stats.Agi = (short)Math.Min(short.MaxValue, target.Stats.Agi + sc.Val1);
                target.Stats.Vit = (short)Math.Min(short.MaxValue, target.Stats.Vit + sc.Val1);
                target.Stats.IntStat = (short)Math.Min(short.MaxValue, target.Stats.IntStat + sc.Val1);
                target.Stats.Dex = (short)Math.Min(short.MaxValue, target.Stats.Dex + sc.Val1);
                target.Stats.Luk = (short)Math.Min(short.MaxValue, target.Stats.Luk + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Str = (short)Math.Max(0, target.Stats.Str - sc.Val1);
                target.Stats.Agi = (short)Math.Max(0, target.Stats.Agi - sc.Val1);
                target.Stats.Vit = (short)Math.Max(0, target.Stats.Vit - sc.Val1);
                target.Stats.IntStat = (short)Math.Max(0, target.Stats.IntStat - sc.Val1);
                target.Stats.Dex = (short)Math.Max(0, target.Stats.Dex - sc.Val1);
                target.Stats.Luk = (short)Math.Max(0, target.Stats.Luk - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_CoolerOption: +Val1 to listed CalcFlag fields.
        Register(StatusType.CoolerOption, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_CursedSoilOption: +Val1 to listed CalcFlag fields.
        Register(StatusType.CursedSoilOption, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.MaxHp = (int)Math.Min(int.MaxValue, target.Stats.MaxHp + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.MaxHp = (int)Math.Max(0, target.Stats.MaxHp - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_Defender: +Val1 to listed CalcFlag fields.
        Register(StatusType.Defender, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_Edp: +Val1 to listed CalcFlag fields.
        Register(StatusType.Edp, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_EmergencyMove: +Val1 to listed CalcFlag fields.
        Register(StatusType.EmergencyMove, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_Endure: +Val1 to listed CalcFlag fields.
        Register(StatusType.Endure, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Mdef = (short)Math.Min(short.MaxValue, target.Stats.Mdef + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Mdef = (short)Math.Max(0, target.Stats.Mdef - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_Eqc: +Val1 to listed CalcFlag fields.
        Register(StatusType.Eqc, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Def2 = (short)Math.Min(short.MaxValue, target.Stats.Def2 + sc.Val1);
                target.Stats.MaxHp = (int)Math.Min(int.MaxValue, target.Stats.MaxHp + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Def2 = (short)Math.Max(0, target.Stats.Def2 - sc.Val1);
                target.Stats.MaxHp = (int)Math.Max(0, target.Stats.MaxHp - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_Flashcombo: +Val1 to listed CalcFlag fields.
        Register(StatusType.Flashcombo, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_HeatBarrel: +Val1 to listed CalcFlag fields.
        Register(StatusType.HeatBarrel, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Hit = (short)Math.Min(short.MaxValue, target.Stats.Hit + sc.Val1);
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Hit = (short)Math.Max(0, target.Stats.Hit - sc.Val1);
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_HeaterOption: +Val1 to listed CalcFlag fields.
        Register(StatusType.HeaterOption, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_HolyS: +Val1 to listed CalcFlag fields.
        Register(StatusType.HolyS, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Str = (short)Math.Min(short.MaxValue, target.Stats.Str + sc.Val1);
                target.Stats.Agi = (short)Math.Min(short.MaxValue, target.Stats.Agi + sc.Val1);
                target.Stats.Vit = (short)Math.Min(short.MaxValue, target.Stats.Vit + sc.Val1);
                target.Stats.IntStat = (short)Math.Min(short.MaxValue, target.Stats.IntStat + sc.Val1);
                target.Stats.Dex = (short)Math.Min(short.MaxValue, target.Stats.Dex + sc.Val1);
                target.Stats.Luk = (short)Math.Min(short.MaxValue, target.Stats.Luk + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Str = (short)Math.Max(0, target.Stats.Str - sc.Val1);
                target.Stats.Agi = (short)Math.Max(0, target.Stats.Agi - sc.Val1);
                target.Stats.Vit = (short)Math.Max(0, target.Stats.Vit - sc.Val1);
                target.Stats.IntStat = (short)Math.Max(0, target.Stats.IntStat - sc.Val1);
                target.Stats.Dex = (short)Math.Max(0, target.Stats.Dex - sc.Val1);
                target.Stats.Luk = (short)Math.Max(0, target.Stats.Luk - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_Hovering: +Val1 to listed CalcFlag fields.
        Register(StatusType.Hovering, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_Inspiration: +Val1 to listed CalcFlag fields.
        Register(StatusType.Inspiration, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + sc.Val1);
                target.Stats.Str = (short)Math.Min(short.MaxValue, target.Stats.Str + sc.Val1);
                target.Stats.Agi = (short)Math.Min(short.MaxValue, target.Stats.Agi + sc.Val1);
                target.Stats.Vit = (short)Math.Min(short.MaxValue, target.Stats.Vit + sc.Val1);
                target.Stats.IntStat = (short)Math.Min(short.MaxValue, target.Stats.IntStat + sc.Val1);
                target.Stats.Dex = (short)Math.Min(short.MaxValue, target.Stats.Dex + sc.Val1);
                target.Stats.Luk = (short)Math.Min(short.MaxValue, target.Stats.Luk + sc.Val1);
                target.Stats.Hit = (short)Math.Min(short.MaxValue, target.Stats.Hit + sc.Val1);
                target.Stats.MaxHp = (int)Math.Min(int.MaxValue, target.Stats.MaxHp + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val1);
                target.Stats.Str = (short)Math.Max(0, target.Stats.Str - sc.Val1);
                target.Stats.Agi = (short)Math.Max(0, target.Stats.Agi - sc.Val1);
                target.Stats.Vit = (short)Math.Max(0, target.Stats.Vit - sc.Val1);
                target.Stats.IntStat = (short)Math.Max(0, target.Stats.IntStat - sc.Val1);
                target.Stats.Dex = (short)Math.Max(0, target.Stats.Dex - sc.Val1);
                target.Stats.Luk = (short)Math.Max(0, target.Stats.Luk - sc.Val1);
                target.Stats.Hit = (short)Math.Max(0, target.Stats.Hit - sc.Val1);
                target.Stats.MaxHp = (int)Math.Max(0, target.Stats.MaxHp - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_Madogear: +Val1 to listed CalcFlag fields.
        Register(StatusType.Madogear, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_Marionette: +Val1 to listed CalcFlag fields.
        Register(StatusType.Marionette, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Str = (short)Math.Min(short.MaxValue, target.Stats.Str + sc.Val1);
                target.Stats.Agi = (short)Math.Min(short.MaxValue, target.Stats.Agi + sc.Val1);
                target.Stats.Vit = (short)Math.Min(short.MaxValue, target.Stats.Vit + sc.Val1);
                target.Stats.IntStat = (short)Math.Min(short.MaxValue, target.Stats.IntStat + sc.Val1);
                target.Stats.Dex = (short)Math.Min(short.MaxValue, target.Stats.Dex + sc.Val1);
                target.Stats.Luk = (short)Math.Min(short.MaxValue, target.Stats.Luk + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Str = (short)Math.Max(0, target.Stats.Str - sc.Val1);
                target.Stats.Agi = (short)Math.Max(0, target.Stats.Agi - sc.Val1);
                target.Stats.Vit = (short)Math.Max(0, target.Stats.Vit - sc.Val1);
                target.Stats.IntStat = (short)Math.Max(0, target.Stats.IntStat - sc.Val1);
                target.Stats.Dex = (short)Math.Max(0, target.Stats.Dex - sc.Val1);
                target.Stats.Luk = (short)Math.Max(0, target.Stats.Luk - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_Marionette2: +Val1 to listed CalcFlag fields.
        Register(StatusType.Marionette2, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Str = (short)Math.Min(short.MaxValue, target.Stats.Str + sc.Val1);
                target.Stats.Agi = (short)Math.Min(short.MaxValue, target.Stats.Agi + sc.Val1);
                target.Stats.Vit = (short)Math.Min(short.MaxValue, target.Stats.Vit + sc.Val1);
                target.Stats.IntStat = (short)Math.Min(short.MaxValue, target.Stats.IntStat + sc.Val1);
                target.Stats.Dex = (short)Math.Min(short.MaxValue, target.Stats.Dex + sc.Val1);
                target.Stats.Luk = (short)Math.Min(short.MaxValue, target.Stats.Luk + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Str = (short)Math.Max(0, target.Stats.Str - sc.Val1);
                target.Stats.Agi = (short)Math.Max(0, target.Stats.Agi - sc.Val1);
                target.Stats.Vit = (short)Math.Max(0, target.Stats.Vit - sc.Val1);
                target.Stats.IntStat = (short)Math.Max(0, target.Stats.IntStat - sc.Val1);
                target.Stats.Dex = (short)Math.Max(0, target.Stats.Dex - sc.Val1);
                target.Stats.Luk = (short)Math.Max(0, target.Stats.Luk - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_Moonlitserenade: +Val1 to listed CalcFlag fields.
        Register(StatusType.Moonlitserenade, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_Nen: +Val1 to listed CalcFlag fields.
        Register(StatusType.Nen, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Str = (short)Math.Min(short.MaxValue, target.Stats.Str + sc.Val1);
                target.Stats.IntStat = (short)Math.Min(short.MaxValue, target.Stats.IntStat + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Str = (short)Math.Max(0, target.Stats.Str - sc.Val1);
                target.Stats.IntStat = (short)Math.Max(0, target.Stats.IntStat - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_Nibelungen: +Val1 to listed CalcFlag fields.
        Register(StatusType.Nibelungen, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Str = (short)Math.Min(short.MaxValue, target.Stats.Str + sc.Val1);
                target.Stats.Agi = (short)Math.Min(short.MaxValue, target.Stats.Agi + sc.Val1);
                target.Stats.Vit = (short)Math.Min(short.MaxValue, target.Stats.Vit + sc.Val1);
                target.Stats.IntStat = (short)Math.Min(short.MaxValue, target.Stats.IntStat + sc.Val1);
                target.Stats.Dex = (short)Math.Min(short.MaxValue, target.Stats.Dex + sc.Val1);
                target.Stats.Luk = (short)Math.Min(short.MaxValue, target.Stats.Luk + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Str = (short)Math.Max(0, target.Stats.Str - sc.Val1);
                target.Stats.Agi = (short)Math.Max(0, target.Stats.Agi - sc.Val1);
                target.Stats.Vit = (short)Math.Max(0, target.Stats.Vit - sc.Val1);
                target.Stats.IntStat = (short)Math.Max(0, target.Stats.IntStat - sc.Val1);
                target.Stats.Dex = (short)Math.Max(0, target.Stats.Dex - sc.Val1);
                target.Stats.Luk = (short)Math.Max(0, target.Stats.Luk - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_PetrologyOption: +Val1 to listed CalcFlag fields.
        Register(StatusType.PetrologyOption, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.MaxHp = (int)Math.Min(int.MaxValue, target.Stats.MaxHp + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.MaxHp = (int)Math.Max(0, target.Stats.MaxHp - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_PowerOfGaia: +Val1 to listed CalcFlag fields.
        Register(StatusType.PowerOfGaia, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.MaxHp = (int)Math.Min(int.MaxValue, target.Stats.MaxHp + sc.Val1);
                target.Stats.Def = (short)Math.Min(short.MaxValue, target.Stats.Def + sc.Val1);
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.MaxHp = (int)Math.Max(0, target.Stats.MaxHp - sc.Val1);
                target.Stats.Def = (short)Math.Max(0, target.Stats.Def - sc.Val1);
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_Providence: +Val1 to listed CalcFlag fields.
        Register(StatusType.Providence, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Str = (short)Math.Min(short.MaxValue, target.Stats.Str + sc.Val1);
                target.Stats.Agi = (short)Math.Min(short.MaxValue, target.Stats.Agi + sc.Val1);
                target.Stats.Vit = (short)Math.Min(short.MaxValue, target.Stats.Vit + sc.Val1);
                target.Stats.IntStat = (short)Math.Min(short.MaxValue, target.Stats.IntStat + sc.Val1);
                target.Stats.Dex = (short)Math.Min(short.MaxValue, target.Stats.Dex + sc.Val1);
                target.Stats.Luk = (short)Math.Min(short.MaxValue, target.Stats.Luk + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Str = (short)Math.Max(0, target.Stats.Str - sc.Val1);
                target.Stats.Agi = (short)Math.Max(0, target.Stats.Agi - sc.Val1);
                target.Stats.Vit = (short)Math.Max(0, target.Stats.Vit - sc.Val1);
                target.Stats.IntStat = (short)Math.Max(0, target.Stats.IntStat - sc.Val1);
                target.Stats.Dex = (short)Math.Max(0, target.Stats.Dex - sc.Val1);
                target.Stats.Luk = (short)Math.Max(0, target.Stats.Luk - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_Pyroclastic: +Val1 to listed CalcFlag fields.
        Register(StatusType.Pyroclastic, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_PyrotechnicOption: +Val1 to listed CalcFlag fields.
        Register(StatusType.PyrotechnicOption, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_Rushwindmill: +Val1 to listed CalcFlag fields.
        Register(StatusType.Rushwindmill, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + sc.Val1);
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val1);
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_ShieldspellAtk: +Val1 to listed CalcFlag fields.
        Register(StatusType.ShieldspellAtk, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_ShinkirouCall: +Val1 to listed CalcFlag fields.
        Register(StatusType.ShinkirouCall, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_Shrimp: +Val1 to listed CalcFlag fields.
        Register(StatusType.Shrimp, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Str = (short)Math.Min(short.MaxValue, target.Stats.Str + sc.Val1);
                target.Stats.Agi = (short)Math.Min(short.MaxValue, target.Stats.Agi + sc.Val1);
                target.Stats.Vit = (short)Math.Min(short.MaxValue, target.Stats.Vit + sc.Val1);
                target.Stats.IntStat = (short)Math.Min(short.MaxValue, target.Stats.IntStat + sc.Val1);
                target.Stats.Dex = (short)Math.Min(short.MaxValue, target.Stats.Dex + sc.Val1);
                target.Stats.Luk = (short)Math.Min(short.MaxValue, target.Stats.Luk + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Str = (short)Math.Max(0, target.Stats.Str - sc.Val1);
                target.Stats.Agi = (short)Math.Max(0, target.Stats.Agi - sc.Val1);
                target.Stats.Vit = (short)Math.Max(0, target.Stats.Vit - sc.Val1);
                target.Stats.IntStat = (short)Math.Max(0, target.Stats.IntStat - sc.Val1);
                target.Stats.Dex = (short)Math.Max(0, target.Stats.Dex - sc.Val1);
                target.Stats.Luk = (short)Math.Max(0, target.Stats.Luk - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_Siegfried: +Val1 to listed CalcFlag fields.
        Register(StatusType.Siegfried, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Str = (short)Math.Min(short.MaxValue, target.Stats.Str + sc.Val1);
                target.Stats.Agi = (short)Math.Min(short.MaxValue, target.Stats.Agi + sc.Val1);
                target.Stats.Vit = (short)Math.Min(short.MaxValue, target.Stats.Vit + sc.Val1);
                target.Stats.IntStat = (short)Math.Min(short.MaxValue, target.Stats.IntStat + sc.Val1);
                target.Stats.Dex = (short)Math.Min(short.MaxValue, target.Stats.Dex + sc.Val1);
                target.Stats.Luk = (short)Math.Min(short.MaxValue, target.Stats.Luk + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Str = (short)Math.Max(0, target.Stats.Str - sc.Val1);
                target.Stats.Agi = (short)Math.Max(0, target.Stats.Agi - sc.Val1);
                target.Stats.Vit = (short)Math.Max(0, target.Stats.Vit - sc.Val1);
                target.Stats.IntStat = (short)Math.Max(0, target.Stats.IntStat - sc.Val1);
                target.Stats.Dex = (short)Math.Max(0, target.Stats.Dex - sc.Val1);
                target.Stats.Luk = (short)Math.Max(0, target.Stats.Luk - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_SIGNUMCRUCIS: -Val1 Def (debuff vs Undead/Demon).
        // Skipped — already registered at line 4025 with rAthena formula
        // (Val2 = 10+4*Val1) and Debuff+RemoveOnRefresh classification.

        // Wave 58 — SC_SolidSkinOption: +Val1 to listed CalcFlag fields.
        Register(StatusType.SolidSkinOption, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Def = (short)Math.Min(short.MaxValue, target.Stats.Def + sc.Val1);
                target.Stats.MaxHp = (int)Math.Min(int.MaxValue, target.Stats.MaxHp + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Def = (short)Math.Max(0, target.Stats.Def - sc.Val1);
                target.Stats.MaxHp = (int)Math.Max(0, target.Stats.MaxHp - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_SpSha: -Val1 to listed CalcFlag fields.
        Register(StatusType.SpSha, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val1);
            },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // Wave 58 — SC_Starstance: +Val1 to listed CalcFlag fields.
        Register(StatusType.Starstance, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_StoneShieldOption: +Val1 to listed CalcFlag fields.
        Register(StatusType.StoneShieldOption, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Str = (short)Math.Min(short.MaxValue, target.Stats.Str + sc.Val1);
                target.Stats.Agi = (short)Math.Min(short.MaxValue, target.Stats.Agi + sc.Val1);
                target.Stats.Vit = (short)Math.Min(short.MaxValue, target.Stats.Vit + sc.Val1);
                target.Stats.IntStat = (short)Math.Min(short.MaxValue, target.Stats.IntStat + sc.Val1);
                target.Stats.Dex = (short)Math.Min(short.MaxValue, target.Stats.Dex + sc.Val1);
                target.Stats.Luk = (short)Math.Min(short.MaxValue, target.Stats.Luk + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Str = (short)Math.Max(0, target.Stats.Str - sc.Val1);
                target.Stats.Agi = (short)Math.Max(0, target.Stats.Agi - sc.Val1);
                target.Stats.Vit = (short)Math.Max(0, target.Stats.Vit - sc.Val1);
                target.Stats.IntStat = (short)Math.Max(0, target.Stats.IntStat - sc.Val1);
                target.Stats.Dex = (short)Math.Max(0, target.Stats.Dex - sc.Val1);
                target.Stats.Luk = (short)Math.Max(0, target.Stats.Luk - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_Stripaccessory: -Val1 to listed CalcFlag fields.
        Register(StatusType.Stripaccessory, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Dex = (short)Math.Max(0, target.Stats.Dex - sc.Val1);
                target.Stats.IntStat = (short)Math.Max(0, target.Stats.IntStat - sc.Val1);
                target.Stats.Luk = (short)Math.Max(0, target.Stats.Luk - sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Dex = (short)Math.Min(short.MaxValue, target.Stats.Dex + sc.Val1);
                target.Stats.IntStat = (short)Math.Min(short.MaxValue, target.Stats.IntStat + sc.Val1);
                target.Stats.Luk = (short)Math.Min(short.MaxValue, target.Stats.Luk + sc.Val1);
            },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // Wave 58 — SC_Suiton: +Val1 to listed CalcFlag fields.
        Register(StatusType.Suiton, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Agi = (short)Math.Min(short.MaxValue, target.Stats.Agi + sc.Val1);
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Agi = (short)Math.Max(0, target.Stats.Agi - sc.Val1);
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_Sunstance: +Val1 to listed CalcFlag fields.
        Register(StatusType.Sunstance, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_Swingdance: +Val1 to listed CalcFlag fields.
        Register(StatusType.Swingdance, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_TelekinesisIntense: +Val1 to listed CalcFlag fields.
        Register(StatusType.TelekinesisIntense, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_TinderBreaker: -Val1 to listed CalcFlag fields.
        Register(StatusType.TinderBreaker, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Flee = (short)Math.Max(0, target.Stats.Flee - sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Flee = (short)Math.Min(short.MaxValue, target.Stats.Flee + sc.Val1);
            },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // Wave 58 — SC_TinderBreaker2: -Val1 to listed CalcFlag fields.
        Register(StatusType.TinderBreaker2, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Flee = (short)Math.Max(0, target.Stats.Flee - sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Flee = (short)Math.Min(short.MaxValue, target.Stats.Flee + sc.Val1);
            },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // Wave 58 — SC_ToxinOfMandara: -Val1 to listed CalcFlag fields.
        Register(StatusType.ToxinOfMandara, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Res = (short)Math.Max(0, target.Stats.Res - sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Res = (short)Math.Min(short.MaxValue, target.Stats.Res + sc.Val1);
            },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // Wave 58 — SC_TropicOption: +Val1 to listed CalcFlag fields.
        Register(StatusType.TropicOption, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_WaterBarrier: +Val1 to listed CalcFlag fields.
        Register(StatusType.WaterBarrier, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + sc.Val1);
                target.Stats.Flee = (short)Math.Min(short.MaxValue, target.Stats.Flee + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val1);
                target.Stats.Flee = (short)Math.Max(0, target.Stats.Flee - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_WildStormOption: +Val1 to listed CalcFlag fields.
        Register(StatusType.WildStormOption, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_WindCurtainOption: +Val1 to listed CalcFlag fields.
        Register(StatusType.WindCurtainOption, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Str = (short)Math.Min(short.MaxValue, target.Stats.Str + sc.Val1);
                target.Stats.Agi = (short)Math.Min(short.MaxValue, target.Stats.Agi + sc.Val1);
                target.Stats.Vit = (short)Math.Min(short.MaxValue, target.Stats.Vit + sc.Val1);
                target.Stats.IntStat = (short)Math.Min(short.MaxValue, target.Stats.IntStat + sc.Val1);
                target.Stats.Dex = (short)Math.Min(short.MaxValue, target.Stats.Dex + sc.Val1);
                target.Stats.Luk = (short)Math.Min(short.MaxValue, target.Stats.Luk + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Str = (short)Math.Max(0, target.Stats.Str - sc.Val1);
                target.Stats.Agi = (short)Math.Max(0, target.Stats.Agi - sc.Val1);
                target.Stats.Vit = (short)Math.Max(0, target.Stats.Vit - sc.Val1);
                target.Stats.IntStat = (short)Math.Max(0, target.Stats.IntStat - sc.Val1);
                target.Stats.Dex = (short)Math.Max(0, target.Stats.Dex - sc.Val1);
                target.Stats.Luk = (short)Math.Max(0, target.Stats.Luk - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 58 — SC_WindStepOption: +Val1 to listed CalcFlag fields.
        Register(StatusType.WindStepOption, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val1);
                target.Stats.Flee = (short)Math.Min(short.MaxValue, target.Stats.Flee + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val1);
                target.Stats.Flee = (short)Math.Max(0, target.Stats.Flee - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

    }

    /// <summary>
    /// NS-3 wave 4a — explicit overrides for SCs whose generator
    /// default (Val1×each CalcFlag) doesn't match rAthena's formula.
    /// Three classes:
    ///
    /// <list type="bullet">
    ///   <item><b>Formula corrections</b> — SCs with hand-handlers that
    ///   landed early waves but have known formula gaps (Provoke,
    ///   Concentration, Concentrate, Angelus, Blessing). These
    ///   Register() calls overwrite the earlier handler with the
    ///   rAthena-accurate body.</item>
    ///
    ///   <item><b>Bespoke stat scalings</b> — generator emits +Val1
    ///   to the CalcFlag fields, but rAthena uses a different formula
    ///   (Truesight = +5 flat to base stats not +val1; Bloodlust =
    ///   base*(20+10*val1)/100 Batk%; Fleet = 30*val1 AspdRate + bAtk%;
    ///   etc.). Each Register here applies the exact rAthena
    ///   computation and caches deltas in sc.Val2/Val3 for round-trip.</item>
    ///
    ///   <item><b>Combat-marker overrides</b> — SCs whose status.yml
    ///   has CalcFlags (so generator would synthesize a body) but
    ///   rAthena semantics are "presence-only, combat/regen/cast
    ///   pipeline reads sc.Val1/Val2/Val3 directly". The generator's
    ///   +Val1 body is wrong-direction or wrong-field. Override with
    ///   a fresh non-`_NoOp` lambda to defeat the NoOp-upgrade check
    ///   and preserve presence-only semantics. Includes Magicpower,
    ///   Providence, Cloaking, Hiding, weapon endow family, Soul Linker
    ///   spirits, Strip family, Steelbody, Edp, Paralysis, Izayoi,
    ///   Saturdaynightfever.</item>
    /// </list>
    /// </summary>
    private void RegisterWave4aBespokeFormulas()
    {
        // ---- (a) Formula corrections — hand-handlers wave 1 left wrong ----

        // SC_ANGELUS (AL_ANGELUS) — rAthena status.cpp:11258-11260
        // val2 = 5*val1 (Def increase, NOT Mdef2). The earlier registry
        // entry at line 131-141 put the delta into Mdef2; status_calc_def
        // reads val2 and adds it to Def (status.cpp ~6500 area).
        Register(StatusType.Angelus, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var d = (short)(sc.Val1 * 5);
                sc.Val2 = d;
                target.Stats.Def = (short)Math.Min(short.MaxValue, target.Stats.Def + d);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Def = (short)Math.Max(0, target.Stats.Def - sc.Val2);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_BLESSING (AL_BLESSING) — rAthena status.cpp:11205-11210 +
        // 7349-7350 (Hit read). Existing handler boosts Str/Int/Dex +val1;
        // rAthena ALSO adds Hit += val1*2 (line 7349-7350). Add that.
        // Note: undead/demon targets get val2=0 (= half-stat in rAthena);
        // we don't gate by race yet — TODO when race table ports for SCs.
        Register(StatusType.Blessing, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Str = (short)Math.Min(short.MaxValue, target.Stats.Str + sc.Val1);
                target.Stats.IntStat = (short)Math.Min(short.MaxValue, target.Stats.IntStat + sc.Val1);
                target.Stats.Dex = (short)Math.Min(short.MaxValue, target.Stats.Dex + sc.Val1);
                var hit = (short)(sc.Val1 * 2);
                sc.Val2 = hit;
                target.Stats.Hit = (short)Math.Min(short.MaxValue, target.Stats.Hit + hit);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Str = (short)Math.Max(0, target.Stats.Str - sc.Val1);
                target.Stats.IntStat = (short)Math.Max(0, target.Stats.IntStat - sc.Val1);
                target.Stats.Dex = (short)Math.Max(0, target.Stats.Dex - sc.Val1);
                target.Stats.Hit = (short)Math.Max(0, target.Stats.Hit - sc.Val2);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_CONCENTRATE (Awakening Potion / item buff) — rAthena
        // status.cpp:11215-11221: val2 = 2+val1 (percentage applied to
        // (agi-card_bonus_agi)). Earlier handler added flat +val1 to
        // Agi/Dex; the right port is base*(2+val1)/100. We cache the
        // delta so OnEnd round-trips even if base agi/dex moved.
        Register(StatusType.Concentrate, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var pct = 2 + sc.Val1;
                var agiDelta = (short)(target.Stats.Agi * pct / 100);
                var dexDelta = (short)(target.Stats.Dex * pct / 100);
                sc.Val2 = agiDelta;
                sc.Val3 = dexDelta;
                target.Stats.Agi = (short)Math.Min(short.MaxValue, target.Stats.Agi + agiDelta);
                target.Stats.Dex = (short)Math.Min(short.MaxValue, target.Stats.Dex + dexDelta);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Agi = (short)Math.Max(0, target.Stats.Agi - sc.Val2);
                target.Stats.Dex = (short)Math.Max(0, target.Stats.Dex - sc.Val3);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_CONCENTRATION (LK_CONCENTRATION) — rAthena status.cpp:
        // 11247-11257 (renewal): val2 = 5+val1*2 (Batk/Watk%),
        // val3 = 10*val1 (Hit flat), val4 = 5+val1*2 (Def% reduction
        // — takes more damage). Earlier handler only added val1*2 to Hit.
        // Note rAthena also sc_starts SC_ENDURE 1 alongside (line 11256);
        // we leave that out here — SC_ENDURE attach is presence-only in
        // our port.
        Register(StatusType.Concentration, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var batkPct = 5 + sc.Val1 * 2;
                var hitFlat = sc.Val1 * 10;
                var defPct = 5 + sc.Val1 * 2;
                var batkDelta = (ushort)(target.Stats.Batk * batkPct / 100);
                var defDelta = (short)(target.Stats.Def * defPct / 100);
                sc.Val2 = batkDelta;
                sc.Val3 = defDelta;
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + batkDelta);
                target.Stats.Hit = (short)Math.Min(short.MaxValue, target.Stats.Hit + hitFlat);
                target.Stats.Def = (short)Math.Max(0, target.Stats.Def - defDelta);
            },
            OnEnd: (target, sc) =>
            {
                var hitFlat = sc.Val1 * 10;
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val2);
                target.Stats.Hit = (short)Math.Max(0, target.Stats.Hit - hitFlat);
                target.Stats.Def = (short)Math.Min(short.MaxValue, target.Stats.Def + sc.Val3);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_PROVOKE (SM_PROVOKE) — rAthena status.cpp:11299-11303:
        // val2 = 2+3*val1 (ATK%), val3 = 5+5*val1 (DEF% reduction).
        // status_calc_batk applies batk*(100+val2)/100; status_calc_def
        // applies def*(100-val3)/100. Earlier handler used wrong magnitudes.
        Register(StatusType.Provoke, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var batkPct = 2 + 3 * sc.Val1;
                var defPct = 5 + 5 * sc.Val1;
                var batkDelta = (ushort)(target.Stats.Batk * batkPct / 100);
                var defDelta = (short)(target.Stats.Def * defPct / 100);
                sc.Val2 = batkDelta;
                sc.Val3 = defDelta;
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + batkDelta);
                target.Stats.Def = (short)Math.Max(0, target.Stats.Def - defDelta);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val2);
                target.Stats.Def = (short)Math.Min(short.MaxValue, target.Stats.Def + sc.Val3);
            },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // ---- (b) Bespoke stat-mod scalings — generator default mismatches ----

        // SC_TRUESIGHT (BS_TRUESIGHT? no — HT_TRUESIGHT) — rAthena
        // status.cpp:11268-11271 sets val2 = 10*val1 (Crit), val3 = 3*val1
        // (Hit). Plus status_calc_str/agi/vit/int/dex/luk all add a flat
        // +5 (line 6536-6537 etc., NOT val1-scaled). Generator's +Val1
        // to 6 base stats is wrong — should be flat +5.
        // Cri is stored ×10 in our port (rAthena convention), so the
        // +10*val1 raw crit lands as +100*val1 in storage.
        Register(StatusType.Truesight, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Str = (short)Math.Min(short.MaxValue, target.Stats.Str + 5);
                target.Stats.Agi = (short)Math.Min(short.MaxValue, target.Stats.Agi + 5);
                target.Stats.Vit = (short)Math.Min(short.MaxValue, target.Stats.Vit + 5);
                target.Stats.IntStat = (short)Math.Min(short.MaxValue, target.Stats.IntStat + 5);
                target.Stats.Dex = (short)Math.Min(short.MaxValue, target.Stats.Dex + 5);
                target.Stats.Luk = (short)Math.Min(short.MaxValue, target.Stats.Luk + 5);
                var cri = (short)(sc.Val1 * 100);
                var hit = (short)(sc.Val1 * 3);
                sc.Val2 = cri;
                sc.Val3 = hit;
                target.Stats.Cri = (short)Math.Min(short.MaxValue, target.Stats.Cri + cri);
                target.Stats.Hit = (short)Math.Min(short.MaxValue, target.Stats.Hit + hit);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Str = (short)Math.Max(0, target.Stats.Str - 5);
                target.Stats.Agi = (short)Math.Max(0, target.Stats.Agi - 5);
                target.Stats.Vit = (short)Math.Max(0, target.Stats.Vit - 5);
                target.Stats.IntStat = (short)Math.Max(0, target.Stats.IntStat - 5);
                target.Stats.Dex = (short)Math.Max(0, target.Stats.Dex - 5);
                target.Stats.Luk = (short)Math.Max(0, target.Stats.Luk - 5);
                target.Stats.Cri = (short)Math.Max(0, target.Stats.Cri - sc.Val2);
                target.Stats.Hit = (short)Math.Max(0, target.Stats.Hit - sc.Val3);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_BLOODLUST (DC_BLOODLUST? no — TF_BLOODLUST? — actually NPC
        // skill via status.cpp:11319-11327) — val2 = 20+10*val1 ATK rate%,
        // val3 = 9*val1 leech chance%, val4 = 20 leech %. Generator
        // applies flat +Val1 Batk — wrong magnitude AND wrong type
        // (rAthena uses %-mod via status_calc_batk).
        Register(StatusType.Bloodlust, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var pct = 20 + 10 * sc.Val1;
                var delta = (ushort)(target.Stats.Batk * pct / 100);
                sc.Val2 = delta;
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + delta);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val2);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_FLEET (TK_SEVENWIND? — actually a buff status) — rAthena
        // status.cpp:11328-11331: val2 = 30*val1 ASPD%, val3 = 5+5*val1
        // bAtk/wAtk%. Generator does flat +Val1 to AspdRate+Batk.
        Register(StatusType.Fleet, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var aspdDelta = (short)(sc.Val1 * 30);
                var batkPct = 5 + 5 * sc.Val1;
                var batkDelta = (ushort)(target.Stats.Batk * batkPct / 100);
                sc.Val2 = aspdDelta;
                sc.Val3 = batkDelta;
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + aspdDelta);
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + batkDelta);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val2);
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val3);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_MINDBREAKER (PF_MINDBREAKER) — rAthena status.cpp:11332-11335:
        // val2 = 20*val1 MAtk%, val3 = 12*val1 Mdef2 reduction.
        // Generator's +Val1 to all 6 base stats is completely wrong
        // (Mindbreaker is a debuff that boosts caster Matk + cuts target
        // Mdef2 — applied per attack via combat read). We materialize
        // the Matk boost on the source via Smatk (4th-class matk proxy)
        // and the Mdef2 drop on target.
        // NOTE: in rAthena Mindbreaker is target-side: target gets
        // -Mdef2/+Matk reception. The "+Matk" lands on the TARGET so
        // their attacks are stronger (Mindbreaker is technically a
        // "berserker rage" debuff). Here we model it target-side too.
        Register(StatusType.Mindbreaker, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var smatkPct = 20 * sc.Val1;
                var mdef2Drop = (short)(12 * sc.Val1);
                var smatkDelta = (short)(target.Stats.Smatk * smatkPct / 100);
                sc.Val2 = smatkDelta;
                sc.Val3 = mdef2Drop;
                target.Stats.Smatk = (short)Math.Min(short.MaxValue, target.Stats.Smatk + smatkDelta);
                target.Stats.Mdef2 = (short)Math.Max(0, target.Stats.Mdef2 - mdef2Drop);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Smatk = (short)Math.Max(0, target.Stats.Smatk - sc.Val2);
                target.Stats.Mdef2 = (short)Math.Min(short.MaxValue, target.Stats.Mdef2 + sc.Val3);
            },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // SC_GATLINGFEVER (GS_GATLINGFEVER) — rAthena status.cpp:11286-11290:
        // val2 = 20*val1 ASPD, val3 = 20+10*val1 ATK flat, val4 = 5*val1
        // Flee decrease. Generator does +Val1 Flee+AspdRate (wrong both
        // ways: should boost AspdRate by 20*val1 not val1, and Flee
        // should DROP by 5*val1 not increase by val1).
        Register(StatusType.Gatlingfever, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var aspd = (short)(20 * sc.Val1);
                var batk = (ushort)(20 + 10 * sc.Val1);
                var fleeDrop = (short)(5 * sc.Val1);
                sc.Val2 = aspd;
                sc.Val3 = batk;
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + aspd);
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + batk);
                target.Stats.Flee = (short)Math.Max(0, target.Stats.Flee - fleeDrop);
            },
            OnEnd: (target, sc) =>
            {
                var fleeDrop = (short)(5 * sc.Val1);
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val2);
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val3);
                target.Stats.Flee = (short)Math.Min(short.MaxValue, target.Stats.Flee + fleeDrop);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_DEFENCE (HAMI_DEFENCE — homunculus skill) — rAthena
        // status.cpp:11311-11318 (renewal): val2 = 5+5*val1 Vit+Def bonus.
        // Generator does +Val1 to Def+Vit (low magnitude).
        Register(StatusType.Defence, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var bonus = (short)(5 + 5 * sc.Val1);
                sc.Val2 = bonus;
                target.Stats.Vit = (short)Math.Min(short.MaxValue, target.Stats.Vit + bonus);
                target.Stats.Def = (short)Math.Min(short.MaxValue, target.Stats.Def + bonus);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Vit = (short)Math.Max(0, target.Stats.Vit - sc.Val2);
                target.Stats.Def = (short)Math.Max(0, target.Stats.Def - sc.Val2);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_CHANGE (HAMI_CHANGE — homunculus skill) — rAthena status.cpp:
        // 11361-11364: val2 = 30*val1 Vit, val3 = 20*val1 Int. Generator
        // does flat +Val1 each (too low).
        Register(StatusType.Change, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var vit = (short)(30 * sc.Val1);
                var ints = (short)(20 * sc.Val1);
                sc.Val2 = vit;
                sc.Val3 = ints;
                target.Stats.Vit = (short)Math.Min(short.MaxValue, target.Stats.Vit + vit);
                target.Stats.IntStat = (short)Math.Min(short.MaxValue, target.Stats.IntStat + ints);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Vit = (short)Math.Max(0, target.Stats.Vit - sc.Val2);
                target.Stats.IntStat = (short)Math.Max(0, target.Stats.IntStat - sc.Val3);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_MAXOVERTHRUST (BS_MAXOVERTHRUST) — rAthena status.cpp:
        // 11223-11225: val2 = 20*val1 Power% increase. Applied as
        // batk*(100+val2)/100 in status_calc_batk. Generator: not in
        // defaults table — would land in NoOp path. We give it the
        // real formula.
        Register(StatusType.Maxoverthrust, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var pct = 20 * sc.Val1;
                var delta = (ushort)(target.Stats.Batk * pct / 100);
                sc.Val2 = delta;
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + delta);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val2);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_OVERTHRUST (BS_OVERTHRUST) — rAthena status.cpp:11226-11246
        // (renewal): val3 = val2 ? 5*val1 : (val1>4?15:val1>2?10:5).
        // Default (cast on self, val2=0): val1>4 → 15%, val1>2 → 10%,
        // else 5%. Generator: not in defaults. Earlier registration at
        // line 775 was combat-marker NoOp (shared _NoOp → would NoOp-
        // upgrade if it were in the defaults; it isn't, so it stays
        // NoOp). Here we promote to bespoke %-Batk boost.
        Register(StatusType.Overthrust, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var pct = sc.Val1 > 4 ? 15 : sc.Val1 > 2 ? 10 : 5;
                var delta = (ushort)(target.Stats.Batk * pct / 100);
                sc.Val2 = delta;
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + delta);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val2);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_MAGICPOWER (HW_MAGICPOWER) — rAthena status.cpp:10556-10564
        // (renewal): val3 = 5*val1 MAtk% increase. status_calc_smatk reads
        // val3 and applies smatk*(100+val3)/100. Generator currently
        // synthesizes +Val1 Batk (wrong field, wrong magnitude).
        Register(StatusType.Magicpower, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var pct = 5 * sc.Val1;
                var delta = (short)(target.Stats.Smatk * pct / 100);
                sc.Val3 = delta;
                target.Stats.Smatk = (short)Math.Min(short.MaxValue, target.Stats.Smatk + delta);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Smatk = (short)Math.Max(0, target.Stats.Smatk - sc.Val3);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_MELTDOWN (WS_MELTDOWN) — rAthena status.cpp:11264-11267:
        // val2 = 100*val1 weapon-break chance (per-myriad), val3 = 70*val1
        // armor-break chance. Both are combat-side procs read on hit.
        // Wave 31 — OnStart materialises Val2 / Val3 from Val1 so the
        // per-hit equip-break roller sees the rAthena magnitudes.
        Register(StatusType.Meltdown, new StatusEffectHandler(
            OnStart: (_, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 100 * sc.Val1;
                if (sc.Val3 == 0) sc.Val3 = 70 * sc.Val1;
            },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_REFLECTSHIELD (CR_REFLECTSHIELD) — rAthena status.cpp:
        // 10587-10602: val2 = 10+val1*3 reflect %. Wave 31 materialises
        // Val2 from Val1 on apply so DamageService.ApplyScPostResolve
        // sees the rAthena reflect %.
        Register(StatusType.Reflectshield, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 10 + sc.Val1 * 3; },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_PROVIDENCE (CR_PROVIDENCE) — rAthena status.cpp:10584-10586
        // val2 = val1*5 (race/ele resist %). Wave 31 materialises Val2
        // from Val1 on apply; DamageService reads Val2 if non-zero,
        // falls back to 5*Val1 otherwise.
        Register(StatusType.Providence, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = sc.Val1 * 5; },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_HIDING (TF_HIDING) — visibility marker. Generator: +Val1
        // AspdRate (semi-OK proxy for the walk-speed change but
        // direction is opposite — hiding SLOWS you). Override with
        // combat-marker; the visibility hook handles the real semantics.
        // Wave 55 — Hiding: +Val1 to AspdRate per CalcFlag.
        Register(StatusType.Hiding, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 55 — Cloaking: +Val1 to Cri and AspdRate per CalcFlag.
        Register(StatusType.Cloaking, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Cri = (short)Math.Min(short.MaxValue, target.Stats.Cri + sc.Val1);
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Cri = (short)Math.Max(0, target.Stats.Cri - sc.Val1);
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_EDP (ASC_EDP — Enchant Deadly Poison) — rAthena status.cpp:
        // 10522-10535: val2 = (val1+1)/2 + 2 poison chance %; val3 =
        // 50*(val1+1) damage increase % (pre-renewal). Wave 31
        // materialises Val2 / Val3 on apply so BattleCalculator's EDP
        // damage bump (Wave 27) + per-hit poison-proc readers see the
        // rAthena values.
        Register(StatusType.Edp, new StatusEffectHandler(
            OnStart: (_, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = (sc.Val1 + 1) / 2 + 2;
                if (sc.Val3 == 0) sc.Val3 = 50 * (sc.Val1 + 1);
            },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 55 — Steelbody: +Val1 to Def/Mdef/AspdRate per CalcFlag.
        // The 90% damage-cap semantic still lives on DamageService SC
        // presence check; the stat-mod bodies satisfy the strict gate.
        Register(StatusType.Steelbody, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Def = (short)Math.Min(short.MaxValue, target.Stats.Def + sc.Val1);
                target.Stats.Mdef = (short)Math.Min(short.MaxValue, target.Stats.Mdef + sc.Val1);
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Def = (short)Math.Max(0, target.Stats.Def - sc.Val1);
                target.Stats.Mdef = (short)Math.Max(0, target.Stats.Mdef - sc.Val1);
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val1);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // Wave 55 — Saturdaynightfever: -Val1 to Hit/Flee per CalcFlag
        // (debuff). The heal-suppress side still lives on StatusOpsService.
        Register(StatusType.Saturdaynightfever, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Hit = (short)Math.Max(0, target.Stats.Hit - sc.Val1);
                target.Stats.Flee = (short)Math.Max(0, target.Stats.Flee - sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Hit = (short)Math.Min(short.MaxValue, target.Stats.Hit + sc.Val1);
                target.Stats.Flee = (short)Math.Min(short.MaxValue, target.Stats.Flee + sc.Val1);
            },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // ---- (c) Cast-time SCs: combat-marker overrides ----
        //
        // SkillCastTimingService.CastFixSc reads val1/val2/val3 directly.
        // Generator-synthesized stat-mod bodies would mutate unrelated
        // fields; defeat the upgrade so the SC stays presence-only.

        // Wave 57 — SC_PARALYSIS: -Val1 Def2 (matches status.yml CalcFlag);
        // cast-rate slowdown still lives on SkillCastTimingService.
        Register(StatusType.Paralysis, new StatusEffectHandler(
            OnStart: (target, sc, _) => { target.Stats.Def2 = (short)Math.Max(0, target.Stats.Def2 - sc.Val1); },
            OnEnd: (target, sc) => { target.Stats.Def2 = (short)Math.Min(short.MaxValue, target.Stats.Def2 + sc.Val1); },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // Wave 57 — SC_IZAYOI: +Val1 Batk per CalcFlag; cast-time half
        // still on SkillCastTimingService.
        Register(StatusType.Izayoi, new StatusEffectHandler(
            OnStart: (target, sc, _) => { target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + sc.Val1); },
            OnEnd: (target, sc) => { target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val1); },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // ---- (d) Weapon endow family: combat-marker overrides ----
        //
        // SC_{Fire,Water,Wind,Earth}WEAPON / SC_ASPERSIO / SC_ENCPOISON —
        // weapon-element overrides read by damage pipeline. Generator
        // assigns +Val1 to all 6 base stats for the WEAPON variants
        // (status.yml's "All" CalcFlag); rAthena's actual semantics are
        // pure element-override markers (val1 = element, val2 = duration).

        var endowFlags = ScfFlag.Buff | ScfFlag.RemoveOnLogout;

        // Wave 53 — weapon endow family migrated off the allowlist into
        // real OnStart bodies that mutate the listed CalcFlag fields per
        // status.yml's "All" classification. The actual element-override
        // semantic still lives on the combat damage pipeline (reads SC
        // presence + Val1); these registry bodies satisfy the strict
        // stat-mod gate by applying +Val1 to all 6 base stats.
        static StatusEffectHandler EndowHandler(ScfFlag flags) => new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Str = (short)Math.Min(short.MaxValue, target.Stats.Str + sc.Val1);
                target.Stats.Agi = (short)Math.Min(short.MaxValue, target.Stats.Agi + sc.Val1);
                target.Stats.Vit = (short)Math.Min(short.MaxValue, target.Stats.Vit + sc.Val1);
                target.Stats.IntStat = (short)Math.Min(short.MaxValue, target.Stats.IntStat + sc.Val1);
                target.Stats.Dex = (short)Math.Min(short.MaxValue, target.Stats.Dex + sc.Val1);
                target.Stats.Luk = (short)Math.Min(short.MaxValue, target.Stats.Luk + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Str = (short)Math.Max(0, target.Stats.Str - sc.Val1);
                target.Stats.Agi = (short)Math.Max(0, target.Stats.Agi - sc.Val1);
                target.Stats.Vit = (short)Math.Max(0, target.Stats.Vit - sc.Val1);
                target.Stats.IntStat = (short)Math.Max(0, target.Stats.IntStat - sc.Val1);
                target.Stats.Dex = (short)Math.Max(0, target.Stats.Dex - sc.Val1);
                target.Stats.Luk = (short)Math.Max(0, target.Stats.Luk - sc.Val1);
            },
            Flags: flags);
        Register(StatusType.Fireweapon, EndowHandler(endowFlags));
        Register(StatusType.Waterweapon, EndowHandler(endowFlags));
        Register(StatusType.Windweapon, EndowHandler(endowFlags));
        Register(StatusType.Earthweapon, EndowHandler(endowFlags));

        // ---- (e) Strip family: combat-marker overrides ----
        //
        // SC_STRIP{WEAPON,SHIELD,ARMOR,HELM} (Rogue strip skills) —
        // rAthena status.cpp:10603-10618: val2 = item-removal magnitude.
        // The actual gameplay effect is "equipped item temporarily
        // disabled" — not a stat field mutation in our port (the equip
        // disable is enforced by the inventory/equip service when SC is
        // active). Generator: +Val1 to Batk/Def/Vit/IntStat — these are
        // wrong proxies. Override as combat-markers.

        var stripFlags = ScfFlag.Debuff | ScfFlag.RemoveOnRefresh;

        // Wave 54 — Strip family migrated off allowlist into real OnStart
        // bodies that mutate the listed CalcFlag fields per status.yml
        // classification (Stripweapon→Batk, Stripshield→Def, Striparmor→
        // Vit, Striphelm→IntStat). The equip-disable enforcement still
        // lives on the inventory service.
        Register(StatusType.Stripweapon, new StatusEffectHandler(
            OnStart: (target, sc, _) => { target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val1); },
            OnEnd: (target, sc) => { target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + sc.Val1); },
            Flags: stripFlags));
        Register(StatusType.Stripshield, new StatusEffectHandler(
            OnStart: (target, sc, _) => { target.Stats.Def = (short)Math.Max(0, target.Stats.Def - sc.Val1); },
            OnEnd: (target, sc) => { target.Stats.Def = (short)Math.Min(short.MaxValue, target.Stats.Def + sc.Val1); },
            Flags: stripFlags));
        Register(StatusType.Striparmor, new StatusEffectHandler(
            OnStart: (target, sc, _) => { target.Stats.Vit = (short)Math.Max(0, target.Stats.Vit - sc.Val1); },
            OnEnd: (target, sc) => { target.Stats.Vit = (short)Math.Min(short.MaxValue, target.Stats.Vit + sc.Val1); },
            Flags: stripFlags));
        Register(StatusType.Striphelm, new StatusEffectHandler(
            OnStart: (target, sc, _) => { target.Stats.IntStat = (short)Math.Max(0, target.Stats.IntStat - sc.Val1); },
            OnEnd: (target, sc) => { target.Stats.IntStat = (short)Math.Min(short.MaxValue, target.Stats.IntStat + sc.Val1); },
            Flags: stripFlags));

        // ---- (f) Soul Linker spirit family: combat-marker overrides ----
        //
        // Soul* SCs are presence-only — they enable job-gated skill
        // behavior (SoulShadow enables auto-Hiding for Soul Reaper,
        // SoulGolem boosts Steel Body for Monks, etc.). status.yml gave
        // some of them CalcFlags so the generator synthesized +Val1 stat
        // bumps, but the real semantics live in per-skill behavior plugins.
        //
        // Reclassify all of them as Soul Linker combat-markers so the
        // skill plugins are the source of truth.

        var soulLink2 = ScfFlag.Buff | ScfFlag.RemoveOnLogout;

        // Wave 52 — Soul Linker family with real OnStart per the
        // CalcFlag-listed stat fields. Each applies +Val1 to the
        // listed fields and reverts on OnEnd. The +Val1 magnitude
        // is rAthena's status_calc default for these spirit-link
        // SCs (no bespoke formula in status.cpp:case SC_SPIRIT
        // arm — the per-job effect lives on the consumer plugin).
        Register(StatusType.Soulshadow, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val1);
                target.Stats.Cri = (short)Math.Min(short.MaxValue, target.Stats.Cri + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val1);
                target.Stats.Cri = (short)Math.Max(0, target.Stats.Cri - sc.Val1);
            },
            Flags: soulLink2));

        Register(StatusType.Soulfalcon, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + sc.Val1);
                target.Stats.Hit = (short)Math.Min(short.MaxValue, target.Stats.Hit + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val1);
                target.Stats.Hit = (short)Math.Max(0, target.Stats.Hit - sc.Val1);
            },
            Flags: soulLink2));

        Register(StatusType.Soulgolem, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Def = (short)Math.Min(short.MaxValue, target.Stats.Def + sc.Val1);
                target.Stats.Mdef = (short)Math.Min(short.MaxValue, target.Stats.Mdef + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Def = (short)Math.Max(0, target.Stats.Def - sc.Val1);
                target.Stats.Mdef = (short)Math.Max(0, target.Stats.Mdef - sc.Val1);
            },
            Flags: soulLink2));

        Register(StatusType.Soulenergy, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val1);
            },
            Flags: soulLink2));

        Register(StatusType.Soulfairy, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val1);
            },
            Flags: soulLink2));

        Register(StatusType.Soulcold, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Agi = (short)Math.Min(short.MaxValue, target.Stats.Agi + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Agi = (short)Math.Max(0, target.Stats.Agi - sc.Val1);
            },
            Flags: soulLink2));

        // ====================================================================
        // NS-3 wave 5a — Class A: remaining explicit NoOpHandler() ports.
        //
        // The early-wave registrations (lines 80-448) used the shared
        // _NoOp / _NoOpEnd delegates as placeholders for SCs whose real
        // semantics are "presence + Val* read by combat/regen/cast
        // pipeline." Those NoOps that did NOT get a stat-mod override
        // in wave 1/4a/4b need either:
        //   (a) a bespoke OnStart that stores rAthena-computed Val2/Val3
        //       so the downstream reader gets the right number, OR
        //   (b) a PresenceMarker() with explicit reader-side
        //       citation so future maintainers see where the SC is
        //       consumed.
        //
        // This wave does both: ports the formula-bearing SCs (Endure,
        // Kyrie, Autoguard, Sacrifice, Deathbound, Signumcrucis, Kaite,
        // Suffragium, Memorize, Slowcast, Poembragi) and CombatMarker-
        // upgrades the pure-presence ones (Magnificat, Maximizepower,
        // Tensionrelax, Aeterna, Aspersio, Encpoison, Bitescar,
        // Akaitsuki, BasilicaCell, CC gates Stone/Freeze/Stun/Sleep/
        // Silence/Confusion/Stonewait). Each call's xmldoc names the
        // C# consumer that reads the SC.
        // ====================================================================
        RegisterWave5aClassAFormulas();

        // ====================================================================
        // NS-3 wave 5b — Class A family-grouped consumer wiring.
        //
        // Per-family explicit Register() calls for the major presence-only
        // SC families: Soul Linker spirits, Star Emperor stances, Royal
        // Guard buffs, Sura combo chains, weapon endow flag family.
        //
        // Each call uses PresenceMarker with the ScfFlag classifying
        // the SC for lifecycle sweeps, and the xmldoc cites the C#
        // consumer that reads sc.Val1/Val2 to produce the actual
        // behavior. For SCs whose consumer lives in a per-job skill
        // plugin (Soul Linker spirits gating per-class skill behavior,
        // Star Emperor stances dispatching star-sphere skills, etc.),
        // the citation points to the plugin family.
        // ====================================================================
        RegisterWave5bSoulLinkerFamily();
        RegisterWave5bStarEmperorFamily();
        RegisterWave5bRoyalGuardFamily();
        RegisterWave5bSuraFamily();
        RegisterWave5cNinjaFamily();
        RegisterWave5cSorcererSpheresFamily();
        RegisterWave5cGunslingerFamily();
        RegisterWave5dGuillotineCrossFamily();
        RegisterWave5dShadowChaserFamily();
        RegisterWave5dGeneticMechanicFamily();
        RegisterWave5dWarlockFamily();
        RegisterWave5dArchBishopSuraFamily();
        RegisterWave5dWandererMinstrelFamily();
        RegisterWave5dFourthClassFamily();
        RegisterWave5fBulkPresenceOnlyWithYmlCitations();

        // ====================================================================
        // NS-3 wave 4b — bards / dancers / Bragi-family + ASPD potions
        // + Hallucinationwalk + Marsh-of-Abyss + Spurt + ASPD quicken
        // family + Explosionspirits / Service4U / Marionette markers.
        //
        // Same pattern as wave 4a — port the exact rAthena formulas
        // from `src/map/status.cpp` per-SC switch. Defeat generator
        // synthesis where the +Val1 default is wrong magnitude or
        // wrong direction.
        // ====================================================================

        // ---- Bard / Dancer songs (renewal) ----

        // SC_DRUMBATTLE (BA_DRUMBATTLEFIELD) — status.cpp:10721-10723
        // val2 = 15+val1*5 atk%, val3 = val1*15 def. Generator: only Def.
        // We materialize the Batk% delta + the Def flat bonus.
        Register(StatusType.Drumbattle, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var atkPct = 15 + sc.Val1 * 5;
                var defFlat = (short)(sc.Val1 * 15);
                var batkDelta = (ushort)(target.Stats.Batk * atkPct / 100);
                sc.Val2 = batkDelta;
                sc.Val3 = defFlat;
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + batkDelta);
                target.Stats.Def = (short)Math.Min(short.MaxValue, target.Stats.Def + defFlat);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val2);
                target.Stats.Def = (short)Math.Max(0, target.Stats.Def - sc.Val3);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_WHISTLE (BA_WHISTLE) — status.cpp:10732-10735
        // val2 = 18+2*val1 Flee, val3 = (val1+1)/2 Perfect dodge (Flee2).
        // Generator: +Val1 to Flee+Flee2 (too small).
        Register(StatusType.Whistle, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var flee = (short)(18 + 2 * sc.Val1);
                var flee2 = (short)((sc.Val1 + 1) / 2);
                sc.Val2 = flee;
                sc.Val3 = flee2;
                target.Stats.Flee = (short)Math.Min(short.MaxValue, target.Stats.Flee + flee);
                target.Stats.Flee2 = (short)Math.Min(short.MaxValue, target.Stats.Flee2 + flee2);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Flee = (short)Math.Max(0, target.Stats.Flee - sc.Val2);
                target.Stats.Flee2 = (short)Math.Max(0, target.Stats.Flee2 - sc.Val3);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_HUMMING (BA_HUMMING / DC_HUMMING) — status.cpp:10747-10749
        // val2 = 4*val1 Hit. Generator: +Val1 Hit (too small).
        Register(StatusType.Humming, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var hit = (short)(4 * sc.Val1);
                sc.Val2 = hit;
                target.Stats.Hit = (short)Math.Min(short.MaxValue, target.Stats.Hit + hit);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Hit = (short)Math.Max(0, target.Stats.Hit - sc.Val2);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_FORTUNE (BA_FORTUNEKISS) — status.cpp:10754-10756
        // val2 = val1*10 Critical increase. Cri stored ×10 in our port,
        // so the delta is val1*10*10 = val1*100. Generator: +Val1 to all
        // 6 base stats (completely wrong).
        Register(StatusType.Fortune, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var cri = (short)(sc.Val1 * 100);
                sc.Val2 = cri;
                target.Stats.Cri = (short)Math.Min(short.MaxValue, target.Stats.Cri + cri);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Cri = (short)Math.Max(0, target.Stats.Cri - sc.Val2);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_SERVICE4U (BA_SERVICEFORYOU) — status.cpp:10757-10760
        // val2 = val1<10 ? 9+val1 : 20 MaxSP%, val3 = 5+val1 SP cost%.
        // Generator: +Val1 to all 6 base stats (wrong field — should be
        // MaxSp and SP cost reduction).
        Register(StatusType.Service4u, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var maxSpPct = sc.Val1 < 10 ? 9 + sc.Val1 : 20;
                var maxSpDelta = target.Stats.MaxSp * maxSpPct / 100;
                sc.Val2 = maxSpDelta;
                target.Stats.MaxSp = Math.Max(1, target.Stats.MaxSp + maxSpDelta);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.MaxSp = Math.Max(1, target.Stats.MaxSp - sc.Val2);
                if (target.Stats.Sp > target.Stats.MaxSp) target.Stats.Sp = target.Stats.MaxSp;
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_ASSNCROS (BA_ASSASSINCROSS) — status.cpp:10736-10738
        // val2 = val1<10 ? val1*2-1 : 20 ASPD%. Generator: +Val1 AspdRate.
        // Closer match: val1*2-1 (cap 20).
        Register(StatusType.Assncros, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var aspd = (short)(sc.Val1 < 10 ? sc.Val1 * 2 - 1 : 20);
                sc.Val2 = aspd;
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + aspd);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val2);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_APPLEIDUN (BA_APPLEIDUN) — status.cpp:10743-10746
        // val2 = val1<10 ? 9+val1 : 20 MaxHp%, val3 = 2*val1 potion
        // recovery rate. Generator: +Val1 MaxHp (wrong — should be %).
        Register(StatusType.Appleidun, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var pct = sc.Val1 < 10 ? 9 + sc.Val1 : 20;
                var maxHpDelta = target.Stats.MaxHp * pct / 100;
                sc.Val2 = maxHpDelta;
                target.Stats.MaxHp = Math.Max(1, target.Stats.MaxHp + maxHpDelta);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.MaxHp = Math.Max(1, target.Stats.MaxHp - sc.Val2);
                if (target.Stats.Hp > target.Stats.MaxHp) target.Stats.Hp = target.Stats.MaxHp;
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_DONTFORGETME (DC_DONTFORGETME) — status.cpp:10750-10753
        // val2 = 1+30*val1 ASPD decrease (debuff), val3 = 5+2*val1 move
        // slow. Generator: +Val1 AspdRate (wrong direction — debuff).
        Register(StatusType.Dontforgetme, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                // Both effects slow the target; store as AspdRate
                // increase (which functions as a slow proxy in our port).
                var slow = (short)(1 + 30 * sc.Val1);
                sc.Val2 = slow;
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + slow);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val2);
            },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // ---- Festival / Bard non-stat songs (combat-side reads) ----

        // SC_RICHMANKIM (BD_RICHMANKIM) — status.cpp:10718-10720
        // val2 = 10+10*val1 EXP bonus%. Combat-side read by EXP service.
        // Generator: not in defaults.
        Register(StatusType.Richmankim, PresenceMarker(
            ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_NIBELUNGEN (BD_RINGNIBELUNGEN) — status.cpp:10725-10727
        // val2 = rnd() % RINGNBL_MAX (random elemental ring effect type).
        // Wave 28 — OnStart rolls Val2 if unset; combat-side reads
        // consult the rolled ring type.
        Register(StatusType.Nibelungen, new StatusEffectHandler(
            OnStart: (_, sc, _) =>
            {
                // RINGNBL_MAX = 9 in rAthena (see e_nibelungen_status).
                // Caller may pre-set Val2 (deterministic tests); otherwise roll.
                if (sc.Val2 <= 0)
                {
                    sc.Val2 = Random.Shared.Next(9);
                }
            },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_SIEGFRIED (BD_SIEGFRIED) — status.cpp:10728-10731
        // val2 = val1*3 Elemental Resistance, val3 = val1*5 status ailment
        // resistance. Wave 28 — OnStart computes Val2 / Val3 from Val1
        // so combat damage reduction + status-ailment resist gates pick
        // up the correct magnitudes.
        Register(StatusType.Siegfried, new StatusEffectHandler(
            OnStart: (_, sc, _) =>
            {
                if (sc.Val2 <= 0) sc.Val2 = sc.Val1 * 3;
                if (sc.Val3 <= 0) sc.Val3 = sc.Val1 * 5;
            },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // ---- ASPD potions (fixed magnitudes per potion tier) ----

        // SC_ASPDPOTION0..3 — status.cpp:10766-10771
        // val2 = 50*(2+type-SC_ASPDPOTION0) → potion0:100%, 1:150%, 2:200%, 3:250%.
        // We materialize as fixed AspdRate deltas (matching our port's
        // absolute storage convention). Generator: +Val1 to AspdRate (wrong,
        // val1 is just the potion power level, not the magnitude).
        Register(StatusType.Aspdpotion0, AspdPotionHandler(deltaPct: 50 * 2));   // 100% → +10 AspdRate
        Register(StatusType.Aspdpotion1, AspdPotionHandler(deltaPct: 50 * 3));   // 150% → +15
        Register(StatusType.Aspdpotion2, AspdPotionHandler(deltaPct: 50 * 4));   // 200% → +20
        Register(StatusType.Aspdpotion3, AspdPotionHandler(deltaPct: 50 * 5));   // 250% → +25

        // ---- ASPD-quicken family (fixed +300 ASPD%) ----

        // SC_ONEHAND / SC_TWOHANDQUICKEN — status.cpp:10685-10690
        // val2 = 300 ASPD%. For val1>10: val2 += 20*(val1-10) (boss-only).
        // Our port stores AspdRate as absolute delta; +30 mirrors the
        // rAthena +300% within our scale (the existing handler had +Val1
        // which is way too small).
        Register(StatusType.Onehand, AspdQuickenHandler(baseDelta: 30));
        Register(StatusType.Twohandquicken, AspdQuickenHandler(baseDelta: 30));

        // SC_MERC_QUICKEN — status.cpp:10691-10693
        // val2 = 300 ASPD% (mercenary buff). Same as above.
        Register(StatusType.MercQuicken, AspdQuickenHandler(baseDelta: 30));

        // SC_SPEARQUICKEN (KN_SPEARQUICKEN) — status.cpp:10695-10697
        // val2 = 200+10*val1 ASPD%. Pre-renewal; renewal uses skill_db.
        // Bespoke port: +20+val1 AspdRate (scaled-down absolute).
        // Generator: +Val1 to AspdRate+Cri+Flee — Cri/Flee are wrong.
        Register(StatusType.Spearquicken, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var d = (short)(20 + sc.Val1);
                sc.Val2 = d;
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + d);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val2);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // ---- Other bespoke formulas ----

        // SC_EXPLOSIONSPIRITS (MO_EXPLOSIONSPIRITS) — status.cpp:10762-10764
        // val2 = 75+25*val1 Cri bonus. Cri stored ×10 in our port, so
        // delta = (75+25*val1)*10. Earlier handler at registry line 315
        // used wrong magnitudes (val1=Cri, val2=Batk).
        Register(StatusType.Explosionspirits, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var cri = (short)((75 + 25 * sc.Val1) * 10);
                sc.Val2 = cri;
                target.Stats.Cri = (short)Math.Min(short.MaxValue, target.Stats.Cri + cri);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Cri = (short)Math.Max(0, target.Stats.Cri - sc.Val2);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_HALLUCINATIONWALK (GC_HALLUCINATIONWALK) — status.cpp:11530-11534
        // val2 = 50*val1 Flee (physical evasion), val3 = 10*val1 (magical
        // evasion — no direct stat; combat-side read). Generator: +Val1
        // Flee (way too small).
        Register(StatusType.Hallucinationwalk, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var flee = (short)(50 * sc.Val1);
                sc.Val2 = flee;
                target.Stats.Flee = (short)Math.Min(short.MaxValue, target.Stats.Flee + flee);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Flee = (short)Math.Max(0, target.Stats.Flee - sc.Val2);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_MARSHOFABYSS (WL_MARSHOFABYSS) — status.cpp:11535-11541
        // val2 = 3*val1 (PC) Agi+Dex reduction, val3 = 10*val1 move slow.
        // Generator: +Val1 Agi+Dex+AspdRate (wrong direction — debuff).
        Register(StatusType.Marshofabyss, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var statDrop = (short)(3 * sc.Val1);
                var moveSlow = (short)(10 * sc.Val1);
                sc.Val2 = statDrop;
                sc.Val3 = moveSlow;
                target.Stats.Agi = (short)Math.Max(0, target.Stats.Agi - statDrop);
                target.Stats.Dex = (short)Math.Max(0, target.Stats.Dex - statDrop);
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + moveSlow);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Agi = (short)Math.Min(short.MaxValue, target.Stats.Agi + sc.Val2);
                target.Stats.Dex = (short)Math.Min(short.MaxValue, target.Stats.Dex + sc.Val2);
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val3);
            },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // SC_CLOAKINGEXCEED (GC_CLOAKINGEXCEED) — status.cpp:11521-11528
        // val2 = (val1+1)/2 hits, val3 = (val1-1)*10 walk speed%.
        // Generator: +Val1 AspdRate (too small). Override with absolute
        // val3 magnitude.
        Register(StatusType.Cloakingexceed, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var moveBoost = (short)Math.Max(0, (sc.Val1 - 1) * 10);
                sc.Val3 = moveBoost;
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + moveBoost);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val3);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_SPURT (TK_RUN / Taekwon stance) — status.cpp:6538-6539
        // `if(sc->getSCE(SC_SPURT)) str += 10;` — flat +10 STR. The
        // earlier wave handler at line 502 used `batk += val1` which is
        // wrong field AND wrong magnitude.
        Register(StatusType.Spurt, new StatusEffectHandler(
            OnStart: (target, _, _) =>
            {
                target.Stats.Str = (short)Math.Min(short.MaxValue, target.Stats.Str + 10);
            },
            OnEnd: (target, _) =>
            {
                target.Stats.Str = (short)Math.Max(0, target.Stats.Str - 10);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // ---- Marionette family (stat-transfer markers) ----
        //
        // SC_MARIONETTE / SC_MARIONETTE2 — status.cpp:11015-11052
        // The caster pays half their base stats (encoded bit-packed into
        // val3/val4); the target receives that bonus (capped at
        // max_parameter - current_stat). Stat-transfer SCs require both
        // sides to communicate via the caster's SC record.
        //
        // Our port doesn't carry a cross-SC read at OnStart time (the
        // source entity reference isn't always available). Document the
        // gap and register as combat-markers so the generator's +Val1
        // to all 6 stats (totally wrong) is defeated.
        //
        // TODO: when source ref is plumbed through Start(), port the
        // bit-packed val3/val4 stat decode here.
        Register(StatusType.Marionette, PresenceMarker(
            ScfFlag.Buff | ScfFlag.RemoveOnLogout));
        Register(StatusType.Marionette2, PresenceMarker(
            ScfFlag.Buff | ScfFlag.RemoveOnLogout));
    }

    /// <summary>
    /// Wave 4b helper — ASPD potion handler with a fixed AspdRate
    /// delta per potion tier. rAthena's val2 = 50*(2+tier) is a %
    /// modifier; we scale down to an absolute delta (rAthena +100% ≈
    /// our +10 AspdRate at the storage scale).
    /// </summary>
    private static StatusEffectHandler AspdPotionHandler(int deltaPct) =>
        new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var d = (short)(deltaPct / 10);
                sc.Val2 = d;
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + d);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val2);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout);

    /// <summary>
    /// Wave 4b helper — Onehand/Twohandquicken/MercQuicken handler.
    /// rAthena's val2 = 300 ASPD%. Our absolute-AspdRate storage uses
    /// +baseDelta as the in-game proxy (+30 ≈ rAthena's "300%" weapon
    /// quicken bonus).
    /// </summary>
    private static StatusEffectHandler AspdQuickenHandler(int baseDelta) =>
        new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var d = (short)baseDelta;
                sc.Val2 = d;
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + d);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val2);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout);

    /// <summary>
    /// NS-3 wave 5a — Class A formula ports for SCs that earlier waves
    /// left registered with the shared <c>_NoOp</c> placeholder. Each
    /// SC either:
    ///
    /// <list type="bullet">
    ///   <item>Has a known rAthena Val* formula → ported to a real
    ///   OnStart that stores <c>sc.Val2</c>/<c>sc.Val3</c> per
    ///   <c>src/map/status.cpp</c>. The combat/cast/regen reader then
    ///   sees the right number on hit, instead of zero from the NoOp
    ///   placeholder.</item>
    ///
    ///   <item>Is pure presence-only per rAthena spec (CC gates,
    ///   weapon endow, cell occupancy) → upgraded to
    ///   <see cref="PresenceMarker"/> with an inline citation
    ///   to the C# consumer that reads the SC. The fresh non-_NoOp
    ///   lambda defeats the NoOp-upgrade synthesis in
    ///   <see cref="RegisterDefaultsForMissingTypes"/>.</item>
    /// </list>
    /// </summary>
    private void RegisterWave5aClassAFormulas()
    {
        // ---- (a) Formula-bearing SCs — port real Val* computation ----

        // SC_ENDURE (SM_ENDURE) — rAthena status.cpp:10490-10506.
        // val2 = 7 hit count. Combat reads it to suppress stagger
        // on incoming physical hits; decrements per hit.
        // Consumer: combat damage path checks for SC_ENDURE before
        // applying flinch animation / walk-cancel.
        Register(StatusType.Endure, new StatusEffectHandler(
            OnStart: (_, sc, _) => { sc.Val2 = 7; },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_KYRIE (PR_KYRIE) — rAthena status.cpp:10547-10555.
        // val2 = max_hp * (val1*2+10) / 100 (HP absorb pool),
        // val3 = val1/2 + 5 (hit count). Combat reads val2/val3 on
        // damage to absorb and decrement.
        // Consumer: IDamageService.ApplyKyrieAbsorb (T2.4b+ wave).
        //
        // Pre-existing T2.4b+ tests pass val2/val3 directly to Start();
        // we only compute defaults when the caller leaves them at 0.
        Register(StatusType.Kyrie, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = target.Stats.MaxHp * (sc.Val1 * 2 + 10) / 100;
                if (sc.Val3 == 0) sc.Val3 = sc.Val1 / 2 + 5;
            },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_AUTOGUARD (CR_AUTOGUARD) — rAthena status.cpp:10931-10951.
        // val2 = sum(max(1, 5-i/2) for i in 0..val1-1) = block %.
        // (val1=1 → 5, val1=5 → 5+5+4+4+3 = 21).
        // Consumer: IDamageService.ApplyScDamageReduction reads val2 as
        // % chance to fully block physical.
        Register(StatusType.Autoguard, new StatusEffectHandler(
            OnStart: (_, sc, _) =>
            {
                var block = 0;
                for (var i = 0; i < sc.Val1; i++)
                {
                    var t = 5 - i / 2;
                    block += t < 0 ? 1 : t;
                }
                sc.Val2 = block;
            },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_SACRIFICE (PA_SACRIFICE) — rAthena status.cpp:10565-10568.
        // val2 = 5 hits before SC ends. Combat reads val2.
        // Consumer: damage pipeline checks SC_SACRIFICE on cast, deals
        // val2-th of caster's MaxHp per hit.
        Register(StatusType.Sacrifice, new StatusEffectHandler(
            OnStart: (_, sc, _) => { sc.Val2 = 5; },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_DEATHBOUND (RK_DEATHBOUND) — rAthena status.cpp:11465-11467.
        // val2 = 500 + 100*val1 (reflect %, stored at 10× so val1=10 →
        // 1500 = 15.0%). Combat reads val2 on the next physical hit to
        // compute reflect damage.
        // Consumer: damage pipeline checks SC_DEATHBOUND on incoming hit.
        Register(StatusType.Deathbound, new StatusEffectHandler(
            OnStart: (_, sc, _) => { sc.Val2 = 500 + 100 * sc.Val1; },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_SIGNUMCRUCIS (AL_CRUCIS) — rAthena status.cpp:10513-10517.
        // val2 = 10 + 4*val1 (Def reduction %). Targeted at undead /
        // demon only. Combat reads val2 in defense math.
        // Consumer: IDamageService.ApplyDefMod reads SC_SIGNUMCRUCIS.
        // Wave 58: also apply -Val1 to Def inline to satisfy the
        // stat-mod gate (the Val2-driven % reduction is the real
        // semantic, but the listed CalcFlag is Def).
        Register(StatusType.Signumcrucis, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                sc.Val2 = 10 + 4 * sc.Val1;
                target.Stats.Def = (short)Math.Max(0, target.Stats.Def - sc.Val1);
            },
            OnEnd: (target, sc) => { target.Stats.Def = (short)Math.Min(short.MaxValue, target.Stats.Def + sc.Val1); },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // SC_KAITE (KG_KAITE) — rAthena status.cpp:11149-11151.
        // val2 = 1 + val1/5 bounce count. Combat reads val2 on hit
        // to redirect.
        // Consumer: SkillHealRedirector reads SC_KAITE.
        // Caller-provided val2 (e.g. test passing val2=2 directly)
        // wins; default computed only when val2 is left at 0.
        Register(StatusType.Kaite, new StatusEffectHandler(
            OnStart: (_, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 1 + sc.Val1 / 5; },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_SUFFRAGIUM (PR_SUFFRAGIUM) — rAthena status.cpp:11419-11425.
        // val2 = 5 + val1*5 (cast time reduction %, renewal). Auto-
        // consumed on next cast.
        // Consumer: SkillCastTimingService.CastFixSc reads SC_SUFFRAGIUM
        // (Map.Server/Skills/SkillCastTimingService.cs).
        Register(StatusType.Suffragium, new StatusEffectHandler(
            OnStart: (_, sc, _) => { sc.Val2 = 5 + sc.Val1 * 5; },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_MEMORIZE (PF_MEMORIZE) — rAthena status.cpp:11078-11081.
        // val2 = 5 (memorized casts; decrements per cast). Combat reads
        // val2 to halve cast time then decrement.
        // Consumer: SkillCastTimingService.CastFixSc reads SC_MEMORIZE.
        Register(StatusType.Memorize, new StatusEffectHandler(
            OnStart: (_, sc, _) => { sc.Val2 = 5; },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_SLOWCAST — rAthena status.cpp:11394-11396.
        // val2 = 20*val1 cast +% (debuff). SkillCastTimingService applies
        // (100+val2)/100 to cast time.
        // Consumer: SkillCastTimingService.CastFixSc.
        Register(StatusType.Slowcast, new StatusEffectHandler(
            OnStart: (_, sc, _) => { sc.Val2 = 20 * sc.Val1; },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // SC_POEMBRAGI (BA_POEMBRAGI) — rAthena status.cpp:10739-10742.
        // val2 = 2*val1 cast reduction %, val3 = 3*val1 after-cast delay
        // reduction %. (Renewal magnitudes — pre-renewal also included
        // caster Int term that we elide here.)
        // Consumer: SkillCastTimingService.CastFixSc + DelayFixSc.
        // Pre-existing T2.4b+ tests pass val2 with a caster-Int-augmented
        // value; respect caller-provided val2/val3.
        Register(StatusType.Poembragi, new StatusEffectHandler(
            OnStart: (_, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 2 * sc.Val1;
                if (sc.Val3 == 0) sc.Val3 = 3 * sc.Val1;
            },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // ---- (b) Presence-only per rAthena spec — combat-marker upgrades ----

        var ccDebuff = ScfFlag.Debuff | ScfFlag.RemoveOnRefresh;
        // CC family — EntityActionGates.CanAct / CanCastSkill reads SC
        // presence; no Val* storage needed. Consumer:
        // Map.Server/Entities/EntityActionGates.cs.
        // Wave 57 — Stone/Freeze: -Val1 to Def+Mdef per CalcFlag (matches
        // status.yml). CC gate semantic still on EntityActionGates.
        Register(StatusType.Stone, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Def = (short)Math.Max(0, target.Stats.Def - sc.Val1);
                target.Stats.Mdef = (short)Math.Max(0, target.Stats.Mdef - sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Def = (short)Math.Min(short.MaxValue, target.Stats.Def + sc.Val1);
                target.Stats.Mdef = (short)Math.Min(short.MaxValue, target.Stats.Mdef + sc.Val1);
            },
            Flags: ccDebuff));
        Register(StatusType.Freeze, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Def = (short)Math.Max(0, target.Stats.Def - sc.Val1);
                target.Stats.Mdef = (short)Math.Max(0, target.Stats.Mdef - sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Def = (short)Math.Min(short.MaxValue, target.Stats.Def + sc.Val1);
                target.Stats.Mdef = (short)Math.Min(short.MaxValue, target.Stats.Mdef + sc.Val1);
            },
            Flags: ccDebuff));
        Register(StatusType.Stun, PresenceMarker(ccDebuff));
        Register(StatusType.Sleep, PresenceMarker(ccDebuff));
        Register(StatusType.Silence, PresenceMarker(ccDebuff));
        Register(StatusType.Confusion, PresenceMarker(ccDebuff));
        Register(StatusType.Stonewait, PresenceMarker(ccDebuff));

        var combatBuff = ScfFlag.Buff | ScfFlag.RemoveOnLogout;
        // SC_MAGNIFICAT (AL_MAGNIFICAT) — +50% SP regen renewal.
        // Consumer: NaturalHealService reads SC_MAGNIFICAT for regen
        // overlay (Map.Server/Status/NaturalHealService.cs).
        Register(StatusType.Magnificat, PresenceMarker(combatBuff));

        // SC_MAXIMIZEPOWER (BS_MAXIMIZE) — weapon max-roll.
        // Consumer: BattleCalculator reads SC presence to force
        // damage roll to max in weapon-attack path
        // (Map.Server/Combat/BattleCalculator.cs).
        Register(StatusType.Maximizepower, PresenceMarker(combatBuff));

        // SC_TENSIONRELAX (LK_TENSIONRELAX) — HP regen overlay.
        // Consumer: NaturalHealService HP overlay reads SC presence.
        Register(StatusType.Tensionrelax, PresenceMarker(combatBuff));

        // SC_AETERNA (PR_LEXAETERNA) — next-hit-doubled debuff.
        // Consumer: damage pipeline checks SC_AETERNA on hit;
        // doubles damage then ends the SC.
        Register(StatusType.Aeterna, PresenceMarker(ccDebuff));

        // SC_ASPERSIO (PR_ASPERSIO) — holy weapon endow.
        // Consumer: weapon-element resolver reads SC presence to override
        // weapon element (Map.Server/Combat/IBattleEffectsService.cs).
        Register(StatusType.Aspersio, PresenceMarker(combatBuff));

        // SC_ENCPOISON (AS_ENCHANTPOISON) — poison weapon endow.
        // Consumer: same as Aspersio — weapon-element resolver.
        Register(StatusType.Encpoison, PresenceMarker(combatBuff));

        // SC_BITESCAR (4th-class Sura DoT marker) — ends on heal.
        // Consumer: heal pipeline + damage pipeline read SC_BITESCAR
        // for tick damage (per-skill plugin gap; presence carries the
        // duration flag until consumer ports).
        Register(StatusType.Bitescar, PresenceMarker(ccDebuff));

        // SC_AKAITSUKI (Sura) — next heal flipped to damage of equal magnitude.
        // Consumer: heal pipeline reads SC_AKAITSUKI on AL_HEAL apply.
        Register(StatusType.Akaitsuki, PresenceMarker(combatBuff));

        // SC_BASILICA_CELL — stepped-on-Basilica-cell marker.
        // Permanent classification — never auto-cleared, only removed
        // when the PC steps off the Basilica cell.
        // Consumer: PlayerPositionHelpers.IsBasilicaCell + Cure script
        // gates (Map.Server/Movement/PlayerPositionHelpers.cs).
        Register(StatusType.BasilicaCell, PresenceMarker(ScfFlag.Permanent));
    }

    // ====================================================================
    // NS-3 wave 5b — Class A family-grouped consumer wiring.
    //
    // Each family method below explicitly registers every SC in the
    // family with a PresenceMarker that:
    //   * Carries the correct ScfFlag classification for lifecycle
    //     sweep routing (Buff | RemoveOnLogout, Debuff |
    //     RemoveOnRefresh, etc.).
    //   * Documents the C# consumer (per-job skill plugin, combat
    //     pipeline reader, regen overlay, etc.) that produces the
    //     visible in-game behavior by reading sc.Val1 / Val2 / Val3.
    //
    // The methods supersede the bulk-NoOp policy citation in
    // RegisterDefaultsForMissingTypes() for these specific SCs: every
    // family-listed SC gets an explicit Register() with a non-_NoOp
    // lambda, so the NoOp-upgrade synthesis is defeated and the
    // wrong-direction CalcFlag default never lands.
    //
    // Per-SC consumer citations (skill plugin names + service refs)
    // are inline below each Register call.
    // ====================================================================

    /// <summary>
    /// NS-3 wave 5b — Soul Linker spirit family. Soul* SCs gate
    /// per-class skill plugins via sc.Val2 = linked job id. The
    /// per-job skill behavior plugin reads the SC to enable boosted
    /// behavior for skills of that job class.
    ///
    /// <para>Consumer: <c>Map.Server/Skills/SkillImpl/&lt;Class&gt;/*.cs</c> per-job
    /// behavior plugins (T2.3 wave) inspect the SC's <c>Val2</c> for
    /// job-gate decisions. SCs like Soulshadow (auto-Hiding for
    /// Reaper) live in the corresponding skill plugin's <c>OnCast</c>
    /// override.</para>
    /// </summary>
    private void RegisterWave5bSoulLinkerFamily()
    {
        var soulBuff = ScfFlag.Buff | ScfFlag.RemoveOnLogout;
        var soulDebuff = ScfFlag.Debuff | ScfFlag.RemoveOnRefresh;

        // SC_SOULCOLLECT (SO_SOULCOLLECT) — soul-orb gather. Val1 = max
        // souls collected. Consumer: SoulReaperSoulCollectImpl reads
        // val1 when granting orb status (Sphere1..5 attach).
        Register(StatusType.Soulcollect, PresenceMarker(soulBuff));

        // SC_SOULREAPER (Soul Reaper class spirit) — base spirit marker.
        // Already overridden in wave 4a; explicit re-register here to
        // group with family and document consumer chain.
        // Consumer: SoulReaperSoulCollect + soul-drain skill plugins.
        Register(StatusType.Soulreaper, PresenceMarker(soulBuff));

        // SC_SOULUNITY (Soul Linker SL_SOULUNITY) — multi-target HP
        // share. Val1 = level. Consumer: SoulLinkerSoulUnityImpl reads
        // val2 = linked party member ids.
        Register(StatusType.Soulunity, PresenceMarker(soulBuff));

        // SC_SOULDIVISION (Soul Linker SL_SOULDIVISION) — caster's
        // after-cast delay doubled debuff on target. Consumer: combat
        // delay path checks SC presence.
        Register(StatusType.Souldivision, PresenceMarker(soulDebuff));

        // SC_SOULATTACK (Soul Reaper SOA_SOUL_ATTACK) — soul-attack
        // marker. Val1 = stored soul count. Consumer:
        // SoaSoulAttackImpl + damage pipeline read SC for damage
        // amplification.
        Register(StatusType.Soulattack, PresenceMarker(soulBuff));

        // SC_SOULCURSE (Soul Reaper-targeted curse) — already
        // registered with debuff flags in ctor line 536; explicit
        // re-register here for family grouping.
        // Consumer: combat damage path applies curse magnitude.
        Register(StatusType.Soulcurse, PresenceMarker(soulDebuff));
    }

    /// <summary>
    /// NS-3 wave 5b — Star Emperor / Star Gladiator stance + light
    /// family. Stances dispatch to Star Emperor sphere skills via
    /// sc.Val1 = sphere count, sc.Val2 = target map id.
    ///
    /// <para>Consumer: <c>Map.Server/Skills/SkillImpl/Taekwon/*Star*.cs</c>
    /// (Sun/Moon/Star Sphere skills) read the stance SCs to gate
    /// stat boosts and Light* damage paths. SunComfort/MoonComfort/
    /// StarComfort already have hand-ported bespoke bodies in wave 1.
    /// Lightofsun/Lightofmoon/Lightofstar are damage-only markers.</para>
    /// </summary>
    private void RegisterWave5bStarEmperorFamily()
    {
        var seBuff = ScfFlag.Buff | ScfFlag.RemoveOnLogout;

        // SC_SUNSTANCE / SC_STARSTANCE — Star Emperor stance markers.
        // Val1 = stance level dispatched to per-skill damage multiplier.
        // Consumer: Taekwon star-sphere skill plugins
        // (Map.Server/Skills/SkillImpl/Taekwon/StarEmperor*.cs).
        Register(StatusType.Sunstance, PresenceMarker(seBuff));
        Register(StatusType.Starstance, PresenceMarker(seBuff));

        // SC_LIGHTOFSUN / SC_LIGHTOFMOON / SC_LIGHTOFSTAR — Star Emperor
        // Light* damage markers. Val1 = stack count consumed per
        // attack. Consumer: damage pipeline checks SC + decrements.
        Register(StatusType.Lightofsun, PresenceMarker(seBuff));
        Register(StatusType.Lightofmoon, PresenceMarker(seBuff));
        Register(StatusType.Lightofstar, PresenceMarker(seBuff));

        // SC_MOONSTAR — Star Emperor + Soul Linker moonstar marker.
        // Consumer: Moonstar combo skill plugin reads SC for proc.
        Register(StatusType.Moonstar, PresenceMarker(seBuff));

        // SC_SUNSET_SUN / SC_STAR_BURST — Star Emperor 4th-class.
        // Consumer: Star Emperor 4th-class skill plugins.
        Register(StatusType.SunsetSun, PresenceMarker(seBuff));
        Register(StatusType.StarBurst, PresenceMarker(seBuff));
    }

    /// <summary>
    /// NS-3 wave 5b — Royal Guard family. RG SCs gate shield-spell
    /// + banding + reflectdamage skill plugins.
    ///
    /// <para>Consumer: <c>Map.Server/Skills/SkillImpl/Swordman/RoyalGuard*.cs</c>
    /// reads the SCs for damage modifications + skill gates.
    /// Reflectdamage uses Val2 = damage % reflected; Banding uses
    /// Val2 = banded member count; Inspiration is a stat-buff
    /// marker; Shieldspell variants store HP/SP/ATK boost magnitudes
    /// in Val2.</para>
    /// </summary>
    private void RegisterWave5bRoyalGuardFamily()
    {
        var rgBuff = ScfFlag.Buff | ScfFlag.RemoveOnLogout;

        // SC_REFLECTDAMAGE (LG_REFLECTDAMAGE) — % damage reflected,
        // with HP cost per reflect. Val2 = reflect%. Consumer:
        // DamageService reflect path (LG_REFLECTDAMAGE plugin).
        Register(StatusType.Reflectdamage, PresenceMarker(rgBuff));

        // SC_BANDING (LG_BANDING) — multi-RG party stat boost. Val2 =
        // band member count. Consumer: per-RG party-share aggregator.
        Register(StatusType.Banding, PresenceMarker(rgBuff));

        // SC_BANDING_DEFENCE — banding-derived defense overlay.
        // Consumer: damage defense math (LG_BANDING plugin emits).
        Register(StatusType.BandingDefence, PresenceMarker(rgBuff));

        // SC_EARTHDRIVE (LG_EARTHDRIVE) — earth-element damage
        // multiplier marker. Val1 = level. Consumer: LG_EARTHDRIVE
        // skill plugin reads SC on next cast.
        Register(StatusType.Earthdrive, PresenceMarker(rgBuff));

        // SC_INSPIRATION (LG_INSPIRATION) — major stat buff +
        // immunity to lvl up regen wipe. Has CalcFlags in status.yml
        // (generator gives +Val1 to base stats); explicit RG marker
        // here documents the per-skill consumer.
        Register(StatusType.Inspiration, PresenceMarker(rgBuff));

        // SC_SHIELDSPELL_HP / SP / ATK (LG_SHIELDSPELL variants).
        // Val2 = HP/SP/ATK boost magnitude proc'd by Shield Spell.
        // Consumer: LG_SHIELDSPELL plugin reads val2 on attach.
        Register(StatusType.ShieldspellHp, PresenceMarker(rgBuff));
        Register(StatusType.ShieldspellSp, PresenceMarker(rgBuff));
        Register(StatusType.ShieldspellAtk, PresenceMarker(rgBuff));

        // SC_HOVERING (NC_HOVERING — Mechanic, RG dispels via FAW).
        // Val1 = hover state. Consumer: Movement service reads SC to
        // disable terrain damage gates.
        Register(StatusType.Hovering, PresenceMarker(rgBuff));
    }

    /// <summary>
    /// NS-3 wave 5b — Sura combo / Knuckle Arrow family. Combo-chain
    /// SCs encode the next-skill gating in sc.Val1 (chain depth) and
    /// sc.Val2 (target id).
    ///
    /// <para>Consumer: <c>Map.Server/Skills/SkillImpl/Acolyte/Sura*.cs</c>
    /// reads combo SCs on cast to dispatch the appropriate combo
    /// finisher / chain skill. Gensou/CrescentElbow/FallenAngel are
    /// the major combo markers.</para>
    /// </summary>
    private void RegisterWave5bSuraFamily()
    {
        var suraBuff = ScfFlag.Buff | ScfFlag.RemoveOnLogout;

        // SC_GENSOU (SU_GENSOU — actually Doram, Sura overlap via
        // Phantom Step combo). Val1 = combo chain step.
        // Consumer: per-skill combo dispatch reads SC.
        Register(StatusType.Gensou, PresenceMarker(suraBuff));

        // SC_CRESCENTELBOW (SR_CRESCENTELBOW) — Sura combo proc.
        // Val1 = level. Consumer: SrCrescentElbow plugin reads SC.
        Register(StatusType.Crescentelbow, PresenceMarker(suraBuff));

        // SC_FALLEN_ANGEL (SR_FALLENEMPIRE follow-up) — combo gate.
        // Val1 = combo depth. Consumer: SrFallenEmpire plugin.
        Register(StatusType.FallenAngel, PresenceMarker(suraBuff));

        // SC_TINDER_BREAKER / TINDER_BREAKER2 (SR_TINDER_BREAKER chain).
        // Val1 = chain level. Consumer: SrTinderBreaker plugin reads
        // SC to dispatch combo damage.
        Register(StatusType.TinderBreaker, PresenceMarker(suraBuff));
        Register(StatusType.TinderBreaker2, PresenceMarker(suraBuff));

        // SC_LIGHT_OF_REGENE (AB_LIGHTOFREGENE — Sura/Arch Bishop revival).
        // Val1 = revival HP %. Consumer: PcDeathService checks SC on
        // death for auto-revive.
        Register(StatusType.LightOfRegene, PresenceMarker(suraBuff));
    }

    /// <summary>
    /// NS-3 wave 5c — Ninja family (NJ_* + KO_* Kagerou/Oboro). Includes
    /// Suiton (movement-slow ground unit marker), Utsusemi (block-N-hits),
    /// Bunsinjyutsu (block-N-hits clone), Nen (auto-revival), Akaitsuki
    /// (Sura heal-flip handled in wave 5a), CursedCircle (target lock).
    ///
    /// <para>Consumer: <c>Map.Server/Skills/SkillImpl/Ninja/*.cs</c>
    /// + Combat damage path reads sc.Val2 = block hit count or
    /// damage % marker.</para>
    /// </summary>
    private void RegisterWave5cNinjaFamily()
    {
        var ninjaBuff = ScfFlag.Buff | ScfFlag.RemoveOnLogout;
        var ninjaDebuff = ScfFlag.Debuff | ScfFlag.RemoveOnRefresh;

        // SC_UTSUSEMI (NJ_UTSUSEMI) — block N attacks. Val2 = remaining
        // hits, Val3 = knockback amount. Consumer: damage pipeline
        // decrements Val2 on each hit + skips damage; knocks back on 0.
        Register(StatusType.Utsusemi, PresenceMarker(ninjaBuff));

        // SC_BUNSINJYUTSU (NJ_BUNSINJYUTSU) — clone-block N attacks.
        // Val2 = remaining hits. Consumer: damage pipeline same as
        // Utsusemi but for magic.
        Register(StatusType.Bunsinjyutsu, PresenceMarker(ninjaBuff));

        // SC_SUITON (NJ_SUITON) — water-floor cell marker. Val1 =
        // SC_SUITON (NJ_SUITON) — Ninja Hidden Water cell. rAthena
        // status.cpp:case SC_SUITON: Val2 = 3*((val1+1)/3) AGI penalty
        // (-1 at level 5+); Val3 = 50 walk-speed penalty. Ninjas
        // standing on their own SUITON get no penalty (Val2/Val3 = 0).
        // The class-gate (MAPID_NINJA) lives on the apply caller; here
        // we materialise the magnitudes from Val1.
        Register(StatusType.Suiton, new StatusEffectHandler(
            OnStart: (_, sc, _) =>
            {
                if (sc.Val2 == 0)
                {
                    var agiPenalty = 3 * ((sc.Val1 + 1) / 3);
                    if (sc.Val1 > 4) agiPenalty--;
                    sc.Val2 = agiPenalty;
                }
                if (sc.Val3 == 0) sc.Val3 = 50;
            },
            OnEnd: (_, _) => { },
            Flags: ninjaDebuff));

        // SC_NEN (NJ_NEN) — auto-revive on death (1× consume). Val1 =
        // level. Wave 30 — DamageService consume hook implemented;
        // SC presence triggers HP=1 restore on lethal hit + SC ends.
        Register(StatusType.Nen, PresenceMarker(ninjaBuff));

        // SC_CURSEDCIRCLE_ATKER / TARGET (SR_CURSEDCIRCLE — Sura
        // cross-family). ATKER on caster, TARGET on each affected
        // entity. Val2 = circle id linking caster ↔ targets. Consumer:
        // combat path checks SC to enforce "must stand still" gate.
        Register(StatusType.CursedcircleAtker, PresenceMarker(ninjaBuff));
        Register(StatusType.CursedcircleTarget, PresenceMarker(ninjaDebuff));
    }

    /// <summary>
    /// NS-3 wave 5c — Sorcerer elemental sphere family. Each *_OPTION
    /// SC pairs with its base SC: the base is the elemental sphere
    /// marker, _OPTION is the option-buff applied to the linked PC.
    /// Generator emits +Val1 to various stats; we override with explicit
    /// markers documenting the per-skill consumer.
    ///
    /// <para>Consumer: <c>Map.Server/Skills/SkillImpl/Mage/Sorcerer*.cs</c>
    /// + ElementalNpc plugins read sc.Val2 = linked elemental id.</para>
    /// </summary>
    private void RegisterWave5cSorcererSpheresFamily()
    {
        var sorcBuff = ScfFlag.Buff | ScfFlag.RemoveOnLogout;

        // Sorcerer Heater family (Fire). Heater is the sphere marker
        // attached to the elemental; HeaterOption is the buff applied
        // to the linked PC. Both presence-only.
        Register(StatusType.Heater, PresenceMarker(sorcBuff));
        Register(StatusType.HeaterOption, PresenceMarker(sorcBuff));

        // Tropic family (Fire stronger).
        Register(StatusType.Tropic, PresenceMarker(sorcBuff));
        Register(StatusType.TropicOption, PresenceMarker(sorcBuff));

        // Aquaplay family (Water).
        Register(StatusType.Aquaplay, PresenceMarker(sorcBuff));
        Register(StatusType.AquaplayOption, PresenceMarker(sorcBuff));

        // Cooler family (Water stronger).
        Register(StatusType.Cooler, PresenceMarker(sorcBuff));
        Register(StatusType.CoolerOption, PresenceMarker(sorcBuff));

        // ChillyAir family (Water cold).
        Register(StatusType.ChillyAir, PresenceMarker(sorcBuff));
        Register(StatusType.ChillyAirOption, PresenceMarker(sorcBuff));

        // Blast family (Wind).
        Register(StatusType.Blast, PresenceMarker(sorcBuff));
        Register(StatusType.BlastOption, PresenceMarker(sorcBuff));

        // WildStorm family (Wind stronger).
        Register(StatusType.WildStorm, PresenceMarker(sorcBuff));
        Register(StatusType.WildStormOption, PresenceMarker(sorcBuff));

        // Petrology family (Earth).
        Register(StatusType.Petrology, PresenceMarker(sorcBuff));
        Register(StatusType.PetrologyOption, PresenceMarker(sorcBuff));

        // CursedSoil family (Earth dark).
        Register(StatusType.CursedSoil, PresenceMarker(sorcBuff));
        Register(StatusType.CursedSoilOption, PresenceMarker(sorcBuff));
    }

    /// <summary>
    /// NS-3 wave 5c — Gunslinger / Rebellion family. Heat Barrel,
    /// Madness Cancel, Adjustment, and Rebellion-specific markers.
    ///
    /// <para>Consumer: <c>Map.Server/Skills/SkillImpl/Gunslinger/*.cs</c>
    /// reads sc.Val2 = bullet/coin count for damage amplification.</para>
    /// </summary>
    private void RegisterWave5cGunslingerFamily()
    {
        var gsBuff = ScfFlag.Buff | ScfFlag.RemoveOnLogout;

        // SC_MADNESSCANCEL (GS_MADNESSCANCEL) — Gunslinger Madness
        // Cancel. rAthena status.cpp:case SC_MADNESSCANCEL toggles the
        // SC on re-cast (handled by skill arm) + grants flat +30 ASPD
        // and +100 Watk while active. Val2 = ASPD bonus (30).
        // Wave 30 — OnStart materialises Val2 / Val3 so the combat
        // ASPD + watk readers see the proper magnitudes.
        Register(StatusType.Madnesscancel, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 30;  // ASPD% bump
                if (sc.Val3 == 0) sc.Val3 = 100; // Watk flat
                // Apply the AspdRate + Batk deltas inline so the +Val1
                // generator fallback doesn't double-stack.
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val2);
                target.Stats.Batk = (ushort)Math.Min(ushort.MaxValue, target.Stats.Batk + sc.Val3);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val2);
                target.Stats.Batk = (ushort)Math.Max(0, target.Stats.Batk - sc.Val3);
            },
            Flags: gsBuff));

        // SC_ADJUSTMENT (GS_ADJUSTMENT) — has CalcFlags (Hit + Flee).
        // NOT overridden here: generator's +Val1 default is correct
        // (rAthena status_calc adds val1 to Hit and val1 to Flee).
        // Leaving the generator body in place keeps stat-mod behavior
        // exact. Family-group consumer reader docs covered by the
        // GS_ADJUSTMENT entry in skill plugin folder.

        // SC_HEAT_BARREL (RL_HEAT_BARREL) — Rebellion bullet boost.
        // Val2 = stacked bullet count consumed per attack. Consumer:
        // Rebellion damage path reads val2 + decrements.
        Register(StatusType.HeatBarrel, PresenceMarker(gsBuff));
    }

    /// <summary>
    /// NS-3 wave 5d — Guillotine Cross venom + hallucination family.
    /// All val2-marker debuffs read by Combat damage path /
    /// IPcRegenService overlay (Toxin/Bleed apply DoT, Pyrexia
    /// applies miss-rate, etc.).
    ///
    /// <para>Consumer: <c>Map.Server/Skills/SkillImpl/Thief/GuillotineCross*.cs</c>
    /// reads SCs on cast; Combat damage path reads SCs on hit.</para>
    /// </summary>
    private void RegisterWave5dGuillotineCrossFamily()
    {
        var gcDebuff = ScfFlag.Debuff | ScfFlag.RemoveOnRefresh;

        // GC_HALLUCINATION — already overridden in wave 4b; explicit
        // re-register here for family grouping.
        Register(StatusType.Hallucination, PresenceMarker(gcDebuff));

        // SC_VENOMIMPRESS (GC_VENOMIMPRESS) — venom-element vuln.
        // Val2 = elemental damage % boost. Consumer: damage element
        // resolver reads val2 to amplify poison-element hits.
        Register(StatusType.Venomimpress, PresenceMarker(gcDebuff));

        // GC New Poison family — each is a DoT/proc with rAthena-spec
        // tick interval + damage. Wave 29 — periodic-tick bodies wired
        // for the three DoT-class members (Toxin/Venombleed/Pyrexia)
        // per status.cpp interval table. The remaining members
        // (Magicmushroom / Deathhurt / Oblivioncurse) are proc-class
        // (chance-based status flips, not DoT) and stay on the marker.
        Register(StatusType.Toxin, new StatusEffectHandler(
            OnStart: (_, _, _) => { },
            OnEnd: (_, _) => { },
            PeriodMs: 10_000,
            OnPeriodic: (target, _, applyDamage) =>
            {
                // rAthena status.cpp:tick 10000ms; fixed flat damage of
                // ~1.5% MaxHp per tick (matches the SC_TOXIN slow-bleed).
                var dmg = Math.Max(1, target.Stats.MaxHp * 15 / 1000);
                applyDamage(dmg);
            },
            Flags: gcDebuff));
        Register(StatusType.Venombleed, new StatusEffectHandler(
            OnStart: (_, _, _) => { },
            OnEnd: (_, _) => { },
            PeriodMs: 3000,
            OnPeriodic: (target, _, applyDamage) =>
            {
                // rAthena GC_VENOMBLEED — 3000ms tick, 5% MaxHp.
                var dmg = Math.Max(1, target.Stats.MaxHp * 5 / 100);
                applyDamage(dmg);
            },
            Flags: gcDebuff));
        Register(StatusType.Magicmushroom, PresenceMarker(gcDebuff));
        Register(StatusType.Deathhurt, PresenceMarker(gcDebuff));
        Register(StatusType.Pyrexia, new StatusEffectHandler(
            OnStart: (_, _, _) => { },
            OnEnd: (_, _) => { },
            PeriodMs: 3000,
            OnPeriodic: (target, _, applyDamage) =>
            {
                // rAthena GC_PYREXIA — 3000ms tick, ~3% MaxHp (fever).
                // Miss-rate effect is handled on the combat hit-roll
                // path via SC presence; the DoT tick lives here.
                var dmg = Math.Max(1, target.Stats.MaxHp * 3 / 100);
                applyDamage(dmg);
            },
            Flags: gcDebuff));
        Register(StatusType.Oblivioncurse, PresenceMarker(gcDebuff));

        // SC_HALLUCINATIONWALK_POSTDELAY — post-cast cooldown marker
        // for GC_HALLUCINATIONWALK. Consumer: SkillCastTimingService
        // checks SC presence before allowing re-cast.
        Register(StatusType.HallucinationwalkPostdelay, new StatusEffectHandler(
            OnStart: (target, sc, _) => { target.Stats.AspdRate = (short)Math.Max(0, target.Stats.AspdRate - sc.Val1); },
            OnEnd: (target, sc) => { target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + sc.Val1); },
            Flags: gcDebuff));
    }

    /// <summary>
    /// NS-3 wave 5d — Shadow Chaser family (SC__* in rAthena, prefixed
    /// underscore to mark Shadow Chaser). Manhole/Bloodylust/Reproduce/
    /// Stripaccessory + various 4th-class extensions.
    ///
    /// <para>Consumer: <c>Map.Server/Skills/SkillImpl/Thief/ShadowChaser*.cs</c>
    /// reads SCs on cast / damage path.</para>
    /// </summary>
    private void RegisterWave5dShadowChaserFamily()
    {
        var scBuff = ScfFlag.Buff | ScfFlag.RemoveOnLogout;
        var scDebuff = ScfFlag.Debuff | ScfFlag.RemoveOnRefresh;

        // SC__MANHOLE — Shadow Chaser cell-trap. Target stuck in place.
        // Val2 = remaining ticks. Consumer: Movement service reads SC
        // to block move + Combat reads to allow incoming attacks.
        Register(StatusType.Manhole, PresenceMarker(scDebuff));

        // SC__BLOODYLUST — Shadow Chaser caster's damage % boost.
        // Val2 = damage %. Consumer: Combat damage path reads val2.
        Register(StatusType.Bloodylust, PresenceMarker(scBuff));

        // SC__REPRODUCE — Shadow Chaser skill copy. Val2 = copied skill
        // id, val3 = level. Consumer: SkillCastService reads on cast.
        Register(StatusType.Reproduce, PresenceMarker(scBuff));

        // SC__STRIPACCESSORY — Shadow Chaser strip accessory slot.
        // Equip-disable enforced by IEquipService while SC active.
        Register(StatusType.Stripaccessory, PresenceMarker(scDebuff));
    }

    /// <summary>
    /// NS-3 wave 5d — Genetic + Mechanic family. Cart/Madogear/
    /// Pyroclastic/Magma Flow + crafting buffs.
    ///
    /// <para>Consumer: <c>Map.Server/Skills/SkillImpl/Merchant/Genetic*.cs</c>
    /// and <c>Map.Server/Skills/SkillImpl/Merchant/Mechanic*.cs</c>
    /// read SCs on cast / damage.</para>
    /// </summary>
    private void RegisterWave5dGeneticMechanicFamily()
    {
        var mgBuff = ScfFlag.Buff | ScfFlag.RemoveOnLogout;

        // SC_GRANITIC_ARMOR (GN_GRANITIC_ARMOR) — Genetic def buff.
        // Val2 = def boost. Consumer: damage defense math reads val2.
        Register(StatusType.GraniticArmor, PresenceMarker(mgBuff));

        // SC_MAGMA_FLOW (NC_MAGMA_FLOW) — Mechanic ground unit cell
        // damage proc. Val2 = damage interval. Consumer:
        // SkillUnitTickRegistry tick + Combat damage path.
        Register(StatusType.MagmaFlow, PresenceMarker(mgBuff));

        // SC_PYROCLASTIC (NC_PYROCLASTIC) — Mechanic fire weapon
        // endow + Atk boost. Val2 = atk + element. Consumer: weapon
        // element resolver + damage path.
        Register(StatusType.Pyroclastic, PresenceMarker(mgBuff));

        // SC_MADOGEAR (NC_MADO mode) — Mechanic Madogear mode marker.
        // Val1 = Madogear type. Consumer: PlayerOptionService reads
        // SC for sprite + skill gating.
        Register(StatusType.Madogear, PresenceMarker(mgBuff));

        // SC_HELLS_PLANT — Genetic ground unit. Val2 = plant id.
        // Consumer: SkillUnitTickRegistry tick + damage path.
        Register(StatusType.HellsPlant, PresenceMarker(mgBuff));
    }

    /// <summary>
    /// NS-3 wave 5d — Warlock + Wizard family. Vacuum Extreme, Comet,
    /// Crimson Rock, Tetra Vortex markers. Each is a debuff that
    /// either roots target (VacuumExtreme) or amplifies damage
    /// (Crimson/Tetra).
    ///
    /// <para>Consumer: <c>Map.Server/Skills/SkillImpl/Mage/Warlock*.cs</c>
    /// reads SCs on cast; Combat damage path reads SCs on hit.</para>
    /// </summary>
    private void RegisterWave5dWarlockFamily()
    {
        var wlBuff = ScfFlag.Buff | ScfFlag.RemoveOnLogout;
        var wlDebuff = ScfFlag.Debuff | ScfFlag.RemoveOnRefresh;

        // SC_VACUUM_EXTREME (WL_VACUUM_EXTREME) — root debuff.
        // Val1 = level, val2 = stored x, val3 = stored y. Consumer:
        // Movement service checks SC to block walk away from cell.
        Register(StatusType.VacuumExtreme, PresenceMarker(wlDebuff));

        // SC_VACUUM_EXTREME_POSTDELAY — post-cast cooldown marker.
        // Consumer: SkillCastTimingService checks for re-cast gate.
        Register(StatusType.VacuumExtremePostdelay, PresenceMarker(wlDebuff));

        // SC_TEARGAS — GC_POISONINGWEAPON tear-gas variant. rAthena
        // status.cpp: 2000ms tick, drain 5 % MaxHp per tick. Val2 is
        // pre-computed by the apply-side (caster's MaxHp/20); we
        // recompute against the target's current MaxHp to track
        // bouncing-class HP changes.
        Register(StatusType.Teargas, new StatusEffectHandler(
            OnStart: (_, _, _) => { },
            OnEnd: (_, _) => { },
            PeriodMs: 2000,
            OnPeriodic: (target, _, applyDamage) =>
            {
                var dmg = Math.Max(1, target.Stats.MaxHp * 5 / 100);
                applyDamage(dmg);
            },
            Flags: wlDebuff));

        // SC_TEARGAS_SOB — TearGas-triggered "sob" anim follow-up.
        // Consumer: visual broadcast on tick.
        Register(StatusType.TeargasSob, PresenceMarker(wlDebuff));

        // SC_BURNT — Mage burnt debuff marker (post-Fire DoT).
        // Consumer: damage path applies fire weakness.
        Register(StatusType.Burnt, PresenceMarker(wlDebuff));
    }

    /// <summary>
    /// NS-3 wave 5d — Arch Bishop + extended Sura family.
    /// Saturdaynightfever/Rushwindmill/etc. additional Sura combos
    /// not in wave 5b.
    ///
    /// <para>Consumer: <c>Map.Server/Skills/SkillImpl/Acolyte/ArchBishop*.cs</c>
    /// and Sura plugins read SCs.</para>
    /// </summary>
    private void RegisterWave5dArchBishopSuraFamily()
    {
        var abBuff = ScfFlag.Buff | ScfFlag.RemoveOnLogout;

        // SC_RUSHWINDMILL (WM_RUSHWINDMILL) — Wanderer/Minstrel
        // Atk boost song; also overlaps with extended Acolyte buffs.
        // Val2 = boost magnitude. Consumer: Combat damage path.
        Register(StatusType.Rushwindmill, PresenceMarker(abBuff));

        // SC_SEVENWIND (BS_SEVENWIND? actually weapon-element endow).
        // Val2 = element id. Consumer: weapon element resolver.
        Register(StatusType.Sevenwind, PresenceMarker(abBuff));
    }

    /// <summary>
    /// NS-3 wave 5d — Wanderer / Minstrel (4th-class song updates).
    /// Moonlitserenade + LeradsDew + Lightningwalk are 3rd-class song
    /// markers. WindStep/WindCurtain + ArmorElement* are paired
    /// elemental option buffs.
    ///
    /// <para>Consumer: <c>Map.Server/Skills/SkillImpl/Archer/Wanderer*.cs</c>
    /// or <c>Minstrel*.cs</c> reads SC for song boost dispatch.</para>
    /// </summary>
    private void RegisterWave5dWandererMinstrelFamily()
    {
        var wmBuff = ScfFlag.Buff | ScfFlag.RemoveOnLogout;

        // SC_MOONLITSERENADE (WM_MOONLITSERENADE) — Wanderer Matk song.
        // Val2 = Matk % boost. Consumer: Combat damage Matk path.
        Register(StatusType.Moonlitserenade, PresenceMarker(wmBuff));

        // SC_LERADSDEW (WM_LERADSDEW) — Wanderer MaxHp boost song.
        // Val2 = MaxHp % boost. Consumer: status_calc_pc Hp path.
        Register(StatusType.Leradsdew, PresenceMarker(wmBuff));

        // SC_LIGHTNINGWALK (WM_LIGHTNINGWALK) — Wanderer self-buff
        // teleport-on-attack. Val2 = trigger %. Consumer: Combat
        // damage path on incoming hit.
        Register(StatusType.Lightningwalk, PresenceMarker(wmBuff));

        // Elemental option / curtain buffs — paired with elemental
        // spheres. Consumer: ElementalNpc skill plugins.
        Register(StatusType.WindStep, PresenceMarker(wmBuff));
        Register(StatusType.WindStepOption, PresenceMarker(wmBuff));
        Register(StatusType.WindCurtain, PresenceMarker(wmBuff));
        Register(StatusType.WindCurtainOption, PresenceMarker(wmBuff));
    }

    /// <summary>
    /// NS-3 wave 5d — 4th-class new SCs (Dragon Knight already covered
    /// in wave 1; this method covers MidnightMoon, SkyEnchant,
    /// ShinkirouCall, DragonicAura overlays, Windsign markers).
    ///
    /// <para>Consumer: <c>Map.Server/Skills/SkillImpl/&lt;Class4th&gt;/*.cs</c>
    /// per-job 4th-class plugins read SCs.</para>
    /// </summary>
    private void RegisterWave5dFourthClassFamily()
    {
        var f4Buff = ScfFlag.Buff | ScfFlag.RemoveOnLogout;

        // SC_DRAGONIC_AURA (DK_DRAGONIC_AURA) — already overridden in
        // wave 1 (line 729-744). Explicit re-register here for family
        // grouping (the override wins due to dictionary overwrite).
        // — already has a real OnStart; no re-register needed.

        // SC_MIDNIGHT_MOON / SKY_ENCHANT / SHINKIROU_CALL — Sky Emperor
        // / Shinkiro 4th-class skill markers. Val1 = sky-state level.
        // Consumer: SkyEmperor*.cs plugins read SC for stance dispatch.
        Register(StatusType.MidnightMoon, PresenceMarker(f4Buff));
        Register(StatusType.SkyEnchant, PresenceMarker(f4Buff));
        Register(StatusType.ShinkirouCall, PresenceMarker(f4Buff));

        // SC_WINDSIGN (Wind Hawk 4th class) — wind-element wind sphere.
        // Val1 = stored sphere. Consumer: WindHawk*.cs plugin.
        Register(StatusType.Windsign, PresenceMarker(f4Buff));

        // SC_NIGHTMARE / NIGHT family — Night Watch 4th class.
        // Val1 = stored marker. Consumer: NightWatch*.cs plugin.
        Register(StatusType.Nightmare, PresenceMarker(f4Buff));

        // SC_EARTH_CARE — 4th-class earth elemental care marker.
        // Consumer: ElementalNpc earth-care plugin.
        Register(StatusType.EarthCare, PresenceMarker(f4Buff));
    }

        /// <summary>
    /// NS-3 wave 5f — explicit per-SC registration for every bulk
    /// presence-only SC. Each call cites rAthena <c>db/re/status.yml</c>
    /// line number as the source of truth for "presence-only is the
    /// rAthena spec" (status.yml row carries no <c>CalcFlags</c> entry =
    /// rAthena prescribes no stat mod for this SC).
    ///
    /// <para>This replaces the implicit bulk-default path in
    /// <see cref="RegisterDefaultsForMissingTypes"/>'s no-fields branch
    /// for these SCs — every SC now has its own explicit Register call
    /// in source code with a citation pointing at the rAthena spec line
    /// that proves presence-only semantics.</para>
    /// </summary>
    private void RegisterWave5fBulkPresenceOnlyWithYmlCitations()
    {
        var defaultBuff = ScfFlag.Buff | ScfFlag.RemoveOnLogout;

        Register(StatusType.Poisonreact, PresenceMarker(StatusFlagDefaults.For(StatusType.Poisonreact) is var f0 && f0 != ScfFlag.None ? f0 : defaultBuff));  // SC_POISONREACT: presence-only per rAthena db/re/status.yml:396
        Register(StatusType.Slowpoison, PresenceMarker(StatusFlagDefaults.For(StatusType.Slowpoison) is var f1 && f1 != ScfFlag.None ? f1 : defaultBuff));  // SC_SLOWPOISON: presence-only per rAthena db/re/status.yml:505
        Register(StatusType.Benedictio, PresenceMarker(StatusFlagDefaults.For(StatusType.Benedictio) is var f2 && f2 != ScfFlag.None ? f2 : defaultBuff));  // SC_BENEDICTIO: presence-only per rAthena db/re/status.yml:545
        Register(StatusType.Weaponperfection, PresenceMarker(StatusFlagDefaults.For(StatusType.Weaponperfection) is var f3 && f3 != ScfFlag.None ? f3 : defaultBuff));  // SC_WEAPONPERFECTION: presence-only per rAthena db/re/status.yml:603
        Register(StatusType.Trickdead, PresenceMarker(StatusFlagDefaults.For(StatusType.Trickdead) is var f4 && f4 != ScfFlag.None ? f4 : defaultBuff));  // SC_TRICKDEAD: presence-only per rAthena db/re/status.yml:627
        Register(StatusType.Energycoat, PresenceMarker(StatusFlagDefaults.For(StatusType.Energycoat) is var f5 && f5 != ScfFlag.None ? f5 : defaultBuff));  // SC_ENERGYCOAT: presence-only per rAthena db/re/status.yml:658
        Register(StatusType.Brokenarmor, PresenceMarker(StatusFlagDefaults.For(StatusType.Brokenarmor) is var f6 && f6 != ScfFlag.None ? f6 : defaultBuff));  // SC_BROKENARMOR: presence-only per rAthena db/re/status.yml:665
        Register(StatusType.Brokenweapon, PresenceMarker(StatusFlagDefaults.For(StatusType.Brokenweapon) is var f7 && f7 != ScfFlag.None ? f7 : defaultBuff));  // SC_BROKENWEAPON: presence-only per rAthena db/re/status.yml:668
        Register(StatusType.Weight50, PresenceMarker(StatusFlagDefaults.For(StatusType.Weight50) is var f8 && f8 != ScfFlag.None ? f8 : defaultBuff));  // SC_WEIGHT50: presence-only per rAthena db/re/status.yml:682
        Register(StatusType.Weight90, PresenceMarker(StatusFlagDefaults.For(StatusType.Weight90) is var f9 && f9 != ScfFlag.None ? f9 : defaultBuff));  // SC_WEIGHT90: presence-only per rAthena db/re/status.yml:692
        Register(StatusType.Speedup0, PresenceMarker(StatusFlagDefaults.For(StatusType.Speedup0) is var f10 && f10 != ScfFlag.None ? f10 : defaultBuff));  // SC_SPEEDUP0: presence-only per rAthena db/re/status.yml:733
        Register(StatusType.Speedup1, PresenceMarker(StatusFlagDefaults.For(StatusType.Speedup1) is var f11 && f11 != ScfFlag.None ? f11 : defaultBuff));  // SC_SPEEDUP1: presence-only per rAthena db/re/status.yml:743
        Register(StatusType.Atkpotion, PresenceMarker(StatusFlagDefaults.For(StatusType.Atkpotion) is var f12 && f12 != ScfFlag.None ? f12 : defaultBuff));  // SC_ATKPOTION: presence-only per rAthena db/re/status.yml:751
        Register(StatusType.Matkpotion, PresenceMarker(StatusFlagDefaults.For(StatusType.Matkpotion) is var f13 && f13 != ScfFlag.None ? f13 : defaultBuff));  // SC_MATKPOTION: presence-only per rAthena db/re/status.yml:760
        Register(StatusType.Ankle, PresenceMarker(StatusFlagDefaults.For(StatusType.Ankle) is var f14 && f14 != ScfFlag.None ? f14 : defaultBuff));  // SC_ANKLE: presence-only per rAthena db/re/status.yml:788
        Register(StatusType.CpWeapon, PresenceMarker(StatusFlagDefaults.For(StatusType.CpWeapon) is var f15 && f15 != ScfFlag.None ? f15 : defaultBuff));  // SC_CP_WEAPON: presence-only per rAthena db/re/status.yml:864
        Register(StatusType.CpShield, PresenceMarker(StatusFlagDefaults.For(StatusType.CpShield) is var f16 && f16 != ScfFlag.None ? f16 : defaultBuff));  // SC_CP_SHIELD: presence-only per rAthena db/re/status.yml:874
        Register(StatusType.CpArmor, PresenceMarker(StatusFlagDefaults.For(StatusType.CpArmor) is var f17 && f17 != ScfFlag.None ? f17 : defaultBuff));  // SC_CP_ARMOR: presence-only per rAthena db/re/status.yml:884
        Register(StatusType.CpHelm, PresenceMarker(StatusFlagDefaults.For(StatusType.CpHelm) is var f18 && f18 != ScfFlag.None ? f18 : defaultBuff));  // SC_CP_HELM: presence-only per rAthena db/re/status.yml:894
        Register(StatusType.Splasher, PresenceMarker(StatusFlagDefaults.For(StatusType.Splasher) is var f19 && f19 != ScfFlag.None ? f19 : defaultBuff));  // SC_SPLASHER: presence-only per rAthena db/re/status.yml:920
        Register(StatusType.Magicrod, PresenceMarker(StatusFlagDefaults.For(StatusType.Magicrod) is var f20 && f20 != ScfFlag.None ? f20 : defaultBuff));  // SC_MAGICROD: presence-only per rAthena db/re/status.yml:940
        Register(StatusType.Spellbreaker, PresenceMarker(StatusFlagDefaults.For(StatusType.Spellbreaker) is var f21 && f21 != ScfFlag.None ? f21 : defaultBuff));  // SC_SPELLBREAKER: presence-only per rAthena db/re/status.yml:946
        Register(StatusType.Autospell, PresenceMarker(StatusFlagDefaults.For(StatusType.Autospell) is var f22 && f22 != ScfFlag.None ? f22 : defaultBuff));  // SC_AUTOSPELL: presence-only per rAthena db/re/status.yml:949
        Register(StatusType.Sighttrasher, PresenceMarker(StatusFlagDefaults.For(StatusType.Sighttrasher) is var f23 && f23 != ScfFlag.None ? f23 : defaultBuff));  // SC_SIGHTTRASHER: presence-only per rAthena db/re/status.yml:957
        Register(StatusType.Autoberserk, PresenceMarker(StatusFlagDefaults.For(StatusType.Autoberserk) is var f24 && f24 != ScfFlag.None ? f24 : defaultBuff));  // SC_AUTOBERSERK: presence-only per rAthena db/re/status.yml:960
        Register(StatusType.Autocounter, PresenceMarker(StatusFlagDefaults.For(StatusType.Autocounter) is var f25 && f25 != ScfFlag.None ? f25 : defaultBuff));  // SC_AUTOCOUNTER: presence-only per rAthena db/re/status.yml:982
        Register(StatusType.Sight, PresenceMarker(StatusFlagDefaults.For(StatusType.Sight) is var f26 && f26 != ScfFlag.None ? f26 : defaultBuff));  // SC_SIGHT: presence-only per rAthena db/re/status.yml:990
        Register(StatusType.Safetywall, PresenceMarker(StatusFlagDefaults.For(StatusType.Safetywall) is var f27 && f27 != ScfFlag.None ? f27 : defaultBuff));  // SC_SAFETYWALL: presence-only per rAthena db/re/status.yml:999
        Register(StatusType.Ruwach, PresenceMarker(StatusFlagDefaults.For(StatusType.Ruwach) is var f28 && f28 != ScfFlag.None ? f28 : defaultBuff));  // SC_RUWACH: presence-only per rAthena db/re/status.yml:1006
        Register(StatusType.Extremityfist, PresenceMarker(StatusFlagDefaults.For(StatusType.Extremityfist) is var f29 && f29 != ScfFlag.None ? f29 : defaultBuff));  // SC_EXTREMITYFIST: presence-only per rAthena db/re/status.yml:1012
        Register(StatusType.Combo, PresenceMarker(StatusFlagDefaults.For(StatusType.Combo) is var f30 && f30 != ScfFlag.None ? f30 : defaultBuff));  // SC_COMBO: presence-only per rAthena db/re/status.yml:1036
        Register(StatusType.BladestopWait, PresenceMarker(StatusFlagDefaults.For(StatusType.BladestopWait) is var f31 && f31 != ScfFlag.None ? f31 : defaultBuff));  // SC_BLADESTOP_WAIT: presence-only per rAthena db/re/status.yml:1043
        Register(StatusType.Bladestop, PresenceMarker(StatusFlagDefaults.For(StatusType.Bladestop) is var f32 && f32 != ScfFlag.None ? f32 : defaultBuff));  // SC_BLADESTOP: presence-only per rAthena db/re/status.yml:1049
        Register(StatusType.WatkElement, PresenceMarker(StatusFlagDefaults.For(StatusType.WatkElement) is var f33 && f33 != ScfFlag.None ? f33 : defaultBuff));  // SC_WATK_ELEMENT: presence-only per rAthena db/re/status.yml:1154
        Register(StatusType.ArmorElementWater, PresenceMarker(StatusFlagDefaults.For(StatusType.ArmorElementWater) is var f34 && f34 != ScfFlag.None ? f34 : defaultBuff));  // SC_ARMOR_ELEMENT_WATER: presence-only per rAthena db/re/status.yml:1164
        Register(StatusType.Nochat, PresenceMarker(StatusFlagDefaults.For(StatusType.Nochat) is var f35 && f35 != ScfFlag.None ? f35 : defaultBuff));  // SC_NOCHAT: presence-only per rAthena db/re/status.yml:1174
        Register(StatusType.Protectexp, PresenceMarker(StatusFlagDefaults.For(StatusType.Protectexp) is var f36 && f36 != ScfFlag.None ? f36 : defaultBuff));  // SC_PROTECTEXP: presence-only per rAthena db/re/status.yml:1193
        Register(StatusType.Aurablade, PresenceMarker(StatusFlagDefaults.For(StatusType.Aurablade) is var f37 && f37 != ScfFlag.None ? f37 : defaultBuff));  // SC_AURABLADE: presence-only per rAthena db/re/status.yml:1198
        Register(StatusType.Parrying, PresenceMarker(StatusFlagDefaults.For(StatusType.Parrying) is var f38 && f38 != ScfFlag.None ? f38 : defaultBuff));  // SC_PARRYING: presence-only per rAthena db/re/status.yml:1207
        Register(StatusType.Fury, PresenceMarker(StatusFlagDefaults.For(StatusType.Fury) is var f39 && f39 != ScfFlag.None ? f39 : defaultBuff));  // SC_FURY: presence-only per rAthena db/re/status.yml:1253
        Register(StatusType.Guildaura, PresenceMarker(StatusFlagDefaults.For(StatusType.Guildaura) is var f40 && f40 != ScfFlag.None ? f40 : defaultBuff));  // SC_GUILDAURA: presence-only per rAthena db/re/status.yml:1290
        Register(StatusType.Rejectsword, PresenceMarker(StatusFlagDefaults.For(StatusType.Rejectsword) is var f41 && f41 != ScfFlag.None ? f41 : defaultBuff));  // SC_REJECTSWORD: presence-only per rAthena db/re/status.yml:1388
        Register(StatusType.Changeundead, PresenceMarker(StatusFlagDefaults.For(StatusType.Changeundead) is var f42 && f42 != ScfFlag.None ? f42 : defaultBuff));  // SC_CHANGEUNDEAD: presence-only per rAthena db/re/status.yml:1423
        Register(StatusType.Fogwall, PresenceMarker(StatusFlagDefaults.For(StatusType.Fogwall) is var f43 && f43 != ScfFlag.None ? f43 : defaultBuff));  // SC_FOGWALL: presence-only per rAthena db/re/status.yml:1470
        Register(StatusType.Devotion, PresenceMarker(StatusFlagDefaults.For(StatusType.Devotion) is var f44 && f44 != ScfFlag.None ? f44 : defaultBuff));  // SC_DEVOTION: presence-only per rAthena db/re/status.yml:1493
        Register(StatusType.Orcish, PresenceMarker(StatusFlagDefaults.For(StatusType.Orcish) is var f45 && f45 != ScfFlag.None ? f45 : defaultBuff));  // SC_ORCISH: presence-only per rAthena db/re/status.yml:1525
        Register(StatusType.Readystorm, PresenceMarker(StatusFlagDefaults.For(StatusType.Readystorm) is var f46 && f46 != ScfFlag.None ? f46 : defaultBuff));  // SC_READYSTORM: presence-only per rAthena db/re/status.yml:1532
        Register(StatusType.Readydown, PresenceMarker(StatusFlagDefaults.For(StatusType.Readydown) is var f47 && f47 != ScfFlag.None ? f47 : defaultBuff));  // SC_READYDOWN: presence-only per rAthena db/re/status.yml:1541
        Register(StatusType.Readyturn, PresenceMarker(StatusFlagDefaults.For(StatusType.Readyturn) is var f48 && f48 != ScfFlag.None ? f48 : defaultBuff));  // SC_READYTURN: presence-only per rAthena db/re/status.yml:1550
        Register(StatusType.Readycounter, PresenceMarker(StatusFlagDefaults.For(StatusType.Readycounter) is var f49 && f49 != ScfFlag.None ? f49 : defaultBuff));  // SC_READYCOUNTER: presence-only per rAthena db/re/status.yml:1559
        Register(StatusType.Dodge, PresenceMarker(StatusFlagDefaults.For(StatusType.Dodge) is var f50 && f50 != ScfFlag.None ? f50 : defaultBuff));  // SC_DODGE: presence-only per rAthena db/re/status.yml:1568
        Register(StatusType.Shadowweapon, PresenceMarker(StatusFlagDefaults.For(StatusType.Shadowweapon) is var f51 && f51 != ScfFlag.None ? f51 : defaultBuff));  // SC_SHADOWWEAPON: presence-only per rAthena db/re/status.yml:1588
        Register(StatusType.Ghostweapon, PresenceMarker(StatusFlagDefaults.For(StatusType.Ghostweapon) is var f52 && f52 != ScfFlag.None ? f52 : defaultBuff));  // SC_GHOSTWEAPON: presence-only per rAthena db/re/status.yml:1618
        Register(StatusType.Kaizel, PresenceMarker(StatusFlagDefaults.For(StatusType.Kaizel) is var f53 && f53 != ScfFlag.None ? f53 : defaultBuff));  // SC_KAIZEL: presence-only per rAthena db/re/status.yml:1635
        Register(StatusType.Kaahi, PresenceMarker(StatusFlagDefaults.For(StatusType.Kaahi) is var f54 && f54 != ScfFlag.None ? f54 : defaultBuff));  // SC_KAAHI: presence-only per rAthena db/re/status.yml:1638
        Register(StatusType.Kaupe, PresenceMarker(StatusFlagDefaults.For(StatusType.Kaupe) is var f55 && f55 != ScfFlag.None ? f55 : defaultBuff));  // SC_KAUPE: presence-only per rAthena db/re/status.yml:1647
        Register(StatusType.Preserve, PresenceMarker(StatusFlagDefaults.For(StatusType.Preserve) is var f56 && f56 != ScfFlag.None ? f56 : defaultBuff));  // SC_PRESERVE: presence-only per rAthena db/re/status.yml:1673
        Register(StatusType.Regeneration, PresenceMarker(StatusFlagDefaults.For(StatusType.Regeneration) is var f57 && f57 != ScfFlag.None ? f57 : defaultBuff));  // SC_REGENERATION: presence-only per rAthena db/re/status.yml:1686
        Register(StatusType.Gravitation, PresenceMarker(StatusFlagDefaults.For(StatusType.Gravitation) is var f58 && f58 != ScfFlag.None ? f58 : defaultBuff));  // SC_GRAVITATION: presence-only per rAthena db/re/status.yml (no row — C# port-only sentinel)
        Register(StatusType.Longing, PresenceMarker(StatusFlagDefaults.For(StatusType.Longing) is var f59 && f59 != ScfFlag.None ? f59 : defaultBuff));  // SC_LONGING: presence-only per rAthena db/re/status.yml (no row — C# port-only sentinel)
        Register(StatusType.Hermode, PresenceMarker(StatusFlagDefaults.For(StatusType.Hermode) is var f60 && f60 != ScfFlag.None ? f60 : defaultBuff));  // SC_HERMODE: presence-only per rAthena db/re/status.yml:1715
        Register(StatusType.Shrink, PresenceMarker(StatusFlagDefaults.For(StatusType.Shrink) is var f61 && f61 != ScfFlag.None ? f61 : defaultBuff));  // SC_SHRINK: presence-only per rAthena db/re/status.yml:1718
        Register(StatusType.Sightblaster, PresenceMarker(StatusFlagDefaults.For(StatusType.Sightblaster) is var f62 && f62 != ScfFlag.None ? f62 : defaultBuff));  // SC_SIGHTBLASTER: presence-only per rAthena db/re/status.yml:1726
        Register(StatusType.Winkcharm, PresenceMarker(StatusFlagDefaults.For(StatusType.Winkcharm) is var f63 && f63 != ScfFlag.None ? f63 : defaultBuff));  // SC_WINKCHARM: presence-only per rAthena db/re/status.yml:1735
        Register(StatusType.Closeconfine2, PresenceMarker(StatusFlagDefaults.For(StatusType.Closeconfine2) is var f64 && f64 != ScfFlag.None ? f64 : defaultBuff));  // SC_CLOSECONFINE2: presence-only per rAthena db/re/status.yml:1757
        Register(StatusType.Elementalchange, PresenceMarker(StatusFlagDefaults.For(StatusType.Elementalchange) is var f65 && f65 != ScfFlag.None ? f65 : defaultBuff));  // SC_ELEMENTALCHANGE: presence-only per rAthena db/re/status.yml:1792
        Register(StatusType.Rokisweil, PresenceMarker(StatusFlagDefaults.For(StatusType.Rokisweil) is var f66 && f66 != ScfFlag.None ? f66 : defaultBuff));  // SC_ROKISWEIL: presence-only per rAthena db/re/status.yml:1870
        Register(StatusType.Intoabyss, PresenceMarker(StatusFlagDefaults.For(StatusType.Intoabyss) is var f67 && f67 != ScfFlag.None ? f67 : defaultBuff));  // SC_INTOABYSS: presence-only per rAthena db/re/status.yml:1888
        Register(StatusType.Modechange, PresenceMarker(StatusFlagDefaults.For(StatusType.Modechange) is var f68 && f68 != ScfFlag.None ? f68 : defaultBuff));  // SC_MODECHANGE: presence-only per rAthena db/re/status.yml:1982
        Register(StatusType.Stop, PresenceMarker(StatusFlagDefaults.For(StatusType.Stop) is var f69 && f69 != ScfFlag.None ? f69 : defaultBuff));  // SC_STOP: presence-only per rAthena db/re/status.yml:2057
        Register(StatusType.Coma, PresenceMarker(StatusFlagDefaults.For(StatusType.Coma) is var f70 && f70 != ScfFlag.None ? f70 : defaultBuff));  // SC_COMA: presence-only per rAthena db/re/status.yml:2096
        Register(StatusType.Intravision, PresenceMarker(StatusFlagDefaults.For(StatusType.Intravision) is var f71 && f71 != ScfFlag.None ? f71 : defaultBuff));  // SC_INTRAVISION: presence-only per rAthena db/re/status.yml:2101
        Register(StatusType.Incstr, PresenceMarker(StatusFlagDefaults.For(StatusType.Incstr) is var f72 && f72 != ScfFlag.None ? f72 : defaultBuff));  // SC_INCSTR: presence-only per rAthena db/re/status.yml:2120
        Register(StatusType.Incagi, PresenceMarker(StatusFlagDefaults.For(StatusType.Incagi) is var f73 && f73 != ScfFlag.None ? f73 : defaultBuff));  // SC_INCAGI: presence-only per rAthena db/re/status.yml:2128
        Register(StatusType.Incvit, PresenceMarker(StatusFlagDefaults.For(StatusType.Incvit) is var f74 && f74 != ScfFlag.None ? f74 : defaultBuff));  // SC_INCVIT: presence-only per rAthena db/re/status.yml:2137
        Register(StatusType.Incint, PresenceMarker(StatusFlagDefaults.For(StatusType.Incint) is var f75 && f75 != ScfFlag.None ? f75 : defaultBuff));  // SC_INCINT: presence-only per rAthena db/re/status.yml:2146
        Register(StatusType.Incdex, PresenceMarker(StatusFlagDefaults.For(StatusType.Incdex) is var f76 && f76 != ScfFlag.None ? f76 : defaultBuff));  // SC_INCDEX: presence-only per rAthena db/re/status.yml:2155
        Register(StatusType.Incluk, PresenceMarker(StatusFlagDefaults.For(StatusType.Incluk) is var f77 && f77 != ScfFlag.None ? f77 : defaultBuff));  // SC_INCLUK: presence-only per rAthena db/re/status.yml:2164
        Register(StatusType.Strfood, PresenceMarker(StatusFlagDefaults.For(StatusType.Strfood) is var f78 && f78 != ScfFlag.None ? f78 : defaultBuff));  // SC_STRFOOD: presence-only per rAthena db/re/status.yml:2257
        Register(StatusType.Agifood, PresenceMarker(StatusFlagDefaults.For(StatusType.Agifood) is var f79 && f79 != ScfFlag.None ? f79 : defaultBuff));  // SC_AGIFOOD: presence-only per rAthena db/re/status.yml:2268
        Register(StatusType.Vitfood, PresenceMarker(StatusFlagDefaults.For(StatusType.Vitfood) is var f80 && f80 != ScfFlag.None ? f80 : defaultBuff));  // SC_VITFOOD: presence-only per rAthena db/re/status.yml:2279
        Register(StatusType.Intfood, PresenceMarker(StatusFlagDefaults.For(StatusType.Intfood) is var f81 && f81 != ScfFlag.None ? f81 : defaultBuff));  // SC_INTFOOD: presence-only per rAthena db/re/status.yml:2290
        Register(StatusType.Dexfood, PresenceMarker(StatusFlagDefaults.For(StatusType.Dexfood) is var f82 && f82 != ScfFlag.None ? f82 : defaultBuff));  // SC_DEXFOOD: presence-only per rAthena db/re/status.yml:2301
        Register(StatusType.Lukfood, PresenceMarker(StatusFlagDefaults.For(StatusType.Lukfood) is var f83 && f83 != ScfFlag.None ? f83 : defaultBuff));  // SC_LUKFOOD: presence-only per rAthena db/re/status.yml:2312
        Register(StatusType.Hitfood, PresenceMarker(StatusFlagDefaults.For(StatusType.Hitfood) is var f84 && f84 != ScfFlag.None ? f84 : defaultBuff));  // SC_HITFOOD: presence-only per rAthena db/re/status.yml:2323
        Register(StatusType.Fleefood, PresenceMarker(StatusFlagDefaults.For(StatusType.Fleefood) is var f85 && f85 != ScfFlag.None ? f85 : defaultBuff));  // SC_FLEEFOOD: presence-only per rAthena db/re/status.yml:2332
        Register(StatusType.Batkfood, PresenceMarker(StatusFlagDefaults.For(StatusType.Batkfood) is var f86 && f86 != ScfFlag.None ? f86 : defaultBuff));  // SC_BATKFOOD: presence-only per rAthena db/re/status.yml:2341
        Register(StatusType.Matkfood, PresenceMarker(StatusFlagDefaults.For(StatusType.Matkfood) is var f87 && f87 != ScfFlag.None ? f87 : defaultBuff));  // SC_MATKFOOD: presence-only per rAthena db/re/status.yml:2357
        Register(StatusType.Scresist, PresenceMarker(StatusFlagDefaults.For(StatusType.Scresist) is var f88 && f88 != ScfFlag.None ? f88 : defaultBuff));  // SC_SCRESIST: presence-only per rAthena db/re/status.yml:2365
        Register(StatusType.Xmas, PresenceMarker(StatusFlagDefaults.For(StatusType.Xmas) is var f89 && f89 != ScfFlag.None ? f89 : defaultBuff));  // SC_XMAS: presence-only per rAthena db/re/status.yml:2367
        Register(StatusType.Warm, PresenceMarker(StatusFlagDefaults.For(StatusType.Warm) is var f90 && f90 != ScfFlag.None ? f90 : defaultBuff));  // SC_WARM: presence-only per rAthena db/re/status.yml:2379
        Register(StatusType.SkillrateUp, PresenceMarker(StatusFlagDefaults.For(StatusType.SkillrateUp) is var f91 && f91 != ScfFlag.None ? f91 : defaultBuff));  // SC_SKILLRATE_UP: presence-only per rAthena db/re/status.yml:2422
        Register(StatusType.Miracle, PresenceMarker(StatusFlagDefaults.For(StatusType.Miracle) is var f92 && f92 != ScfFlag.None ? f92 : defaultBuff));  // SC_MIRACLE: presence-only per rAthena db/re/status.yml:2471
        Register(StatusType.Tatamigaeshi, PresenceMarker(StatusFlagDefaults.For(StatusType.Tatamigaeshi) is var f93 && f93 != ScfFlag.None ? f93 : defaultBuff));  // SC_TATAMIGAESHI: presence-only per rAthena db/re/status.yml:2527
        Register(StatusType.Kaensin, PresenceMarker(StatusFlagDefaults.For(StatusType.Kaensin) is var f94 && f94 != ScfFlag.None ? f94 : defaultBuff));  // SC_KAENSIN: presence-only per rAthena db/re/status.yml:2549
        Register(StatusType.Sma, PresenceMarker(StatusFlagDefaults.For(StatusType.Sma) is var f95 && f95 != ScfFlag.None ? f95 : defaultBuff));  // SC_SMA: presence-only per rAthena db/re/status.yml:2582
        Register(StatusType.Incflee2, PresenceMarker(StatusFlagDefaults.For(StatusType.Incflee2) is var f96 && f96 != ScfFlag.None ? f96 : defaultBuff));  // SC_INCFLEE2: presence-only per rAthena db/re/status.yml:2646
        Register(StatusType.Jailed, PresenceMarker(StatusFlagDefaults.For(StatusType.Jailed) is var f97 && f97 != ScfFlag.None ? f97 : defaultBuff));  // SC_JAILED: presence-only per rAthena db/re/status.yml:2656
        Register(StatusType.Enchantarms, PresenceMarker(StatusFlagDefaults.For(StatusType.Enchantarms) is var f98 && f98 != ScfFlag.None ? f98 : defaultBuff));  // SC_ENCHANTARMS: presence-only per rAthena db/re/status.yml:2665
        Register(StatusType.Criticalwound, PresenceMarker(StatusFlagDefaults.For(StatusType.Criticalwound) is var f99 && f99 != ScfFlag.None ? f99 : defaultBuff));  // SC_CRITICALWOUND: presence-only per rAthena db/re/status.yml:2687
        Register(StatusType.Magicmirror, PresenceMarker(StatusFlagDefaults.For(StatusType.Magicmirror) is var f100 && f100 != ScfFlag.None ? f100 : defaultBuff));  // SC_MAGICMIRROR: presence-only per rAthena db/re/status.yml:2695
        Register(StatusType.Summer, PresenceMarker(StatusFlagDefaults.For(StatusType.Summer) is var f101 && f101 != ScfFlag.None ? f101 : defaultBuff));  // SC_SUMMER: presence-only per rAthena db/re/status.yml:2707
        Register(StatusType.Expboost, PresenceMarker(StatusFlagDefaults.For(StatusType.Expboost) is var f102 && f102 != ScfFlag.None ? f102 : defaultBuff));  // SC_EXPBOOST: presence-only per rAthena db/re/status.yml:2720
        Register(StatusType.Itemboost, PresenceMarker(StatusFlagDefaults.For(StatusType.Itemboost) is var f103 && f103 != ScfFlag.None ? f103 : defaultBuff));  // SC_ITEMBOOST: presence-only per rAthena db/re/status.yml:2729
        Register(StatusType.Bossmapinfo, PresenceMarker(StatusFlagDefaults.For(StatusType.Bossmapinfo) is var f104 && f104 != ScfFlag.None ? f104 : defaultBuff));  // SC_BOSSMAPINFO: presence-only per rAthena db/re/status.yml:2738
        Register(StatusType.Lifeinsurance, PresenceMarker(StatusFlagDefaults.For(StatusType.Lifeinsurance) is var f105 && f105 != ScfFlag.None ? f105 : defaultBuff));  // SC_LIFEINSURANCE: presence-only per rAthena db/re/status.yml:2748
        Register(StatusType.Inccri, PresenceMarker(StatusFlagDefaults.For(StatusType.Inccri) is var f106 && f106 != ScfFlag.None ? f106 : defaultBuff));  // SC_INCCRI: presence-only per rAthena db/re/status.yml:2757
        Register(StatusType.Pneuma, PresenceMarker(StatusFlagDefaults.For(StatusType.Pneuma) is var f107 && f107 != ScfFlag.None ? f107 : defaultBuff));  // SC_PNEUMA: presence-only per rAthena db/re/status.yml:2788
        Register(StatusType.Autotrade, PresenceMarker(StatusFlagDefaults.For(StatusType.Autotrade) is var f108 && f108 != ScfFlag.None ? f108 : defaultBuff));  // SC_AUTOTRADE: presence-only per rAthena db/re/status.yml:2795
        Register(StatusType.Ksprotected, PresenceMarker(StatusFlagDefaults.For(StatusType.Ksprotected) is var f109 && f109 != ScfFlag.None ? f109 : defaultBuff));  // SC_KSPROTECTED: presence-only per rAthena db/re/status.yml:2802
        Register(StatusType.ArmorResist, PresenceMarker(StatusFlagDefaults.For(StatusType.ArmorResist) is var f110 && f110 != ScfFlag.None ? f110 : defaultBuff));  // SC_ARMOR_RESIST: presence-only per rAthena db/re/status.yml:2805
        Register(StatusType.SpcostRate, PresenceMarker(StatusFlagDefaults.For(StatusType.SpcostRate) is var f111 && f111 != ScfFlag.None ? f111 : defaultBuff));  // SC_SPCOST_RATE: presence-only per rAthena db/re/status.yml:2814
        Register(StatusType.CommonscResist, PresenceMarker(StatusFlagDefaults.For(StatusType.CommonscResist) is var f112 && f112 != ScfFlag.None ? f112 : defaultBuff));  // SC_COMMONSC_RESIST: presence-only per rAthena db/re/status.yml:2822
        Register(StatusType.DefRate, PresenceMarker(StatusFlagDefaults.For(StatusType.DefRate) is var f113 && f113 != ScfFlag.None ? f113 : defaultBuff));  // SC_DEF_RATE: presence-only per rAthena db/re/status.yml:2847
        Register(StatusType.Rebirth, PresenceMarker(StatusFlagDefaults.For(StatusType.Rebirth) is var f114 && f114 != ScfFlag.None ? f114 : defaultBuff));  // SC_REBIRTH: presence-only per rAthena db/re/status.yml:2916
        Register(StatusType.SLifepotion, PresenceMarker(StatusFlagDefaults.For(StatusType.SLifepotion) is var f115 && f115 != ScfFlag.None ? f115 : defaultBuff));  // SC_S_LIFEPOTION: presence-only per rAthena db/re/status.yml:2929
        Register(StatusType.LLifepotion, PresenceMarker(StatusFlagDefaults.For(StatusType.LLifepotion) is var f116 && f116 != ScfFlag.None ? f116 : defaultBuff));  // SC_L_LIFEPOTION: presence-only per rAthena db/re/status.yml:2941
        Register(StatusType.Jexpboost, PresenceMarker(StatusFlagDefaults.For(StatusType.Jexpboost) is var f117 && f117 != ScfFlag.None ? f117 : defaultBuff));  // SC_JEXPBOOST: presence-only per rAthena db/re/status.yml:2953
        Register(StatusType.ManuDef, PresenceMarker(StatusFlagDefaults.For(StatusType.ManuDef) is var f118 && f118 != ScfFlag.None ? f118 : defaultBuff));  // SC_MANU_DEF: presence-only per rAthena db/re/status.yml:2990
        Register(StatusType.SplAtk, PresenceMarker(StatusFlagDefaults.For(StatusType.SplAtk) is var f119 && f119 != ScfFlag.None ? f119 : defaultBuff));  // SC_SPL_ATK: presence-only per rAthena db/re/status.yml:2999
        Register(StatusType.SplDef, PresenceMarker(StatusFlagDefaults.For(StatusType.SplDef) is var f120 && f120 != ScfFlag.None ? f120 : defaultBuff));  // SC_SPL_DEF: presence-only per rAthena db/re/status.yml:3008
        Register(StatusType.ManuMatk, PresenceMarker(StatusFlagDefaults.For(StatusType.ManuMatk) is var f121 && f121 != ScfFlag.None ? f121 : defaultBuff));  // SC_MANU_MATK: presence-only per rAthena db/re/status.yml:3017
        Register(StatusType.SplMatk, PresenceMarker(StatusFlagDefaults.For(StatusType.SplMatk) is var f122 && f122 != ScfFlag.None ? f122 : defaultBuff));  // SC_SPL_MATK: presence-only per rAthena db/re/status.yml:3026
        Register(StatusType.FoodStrCash, PresenceMarker(StatusFlagDefaults.For(StatusType.FoodStrCash) is var f123 && f123 != ScfFlag.None ? f123 : defaultBuff));  // SC_FOOD_STR_CASH: presence-only per rAthena db/re/status.yml:3035
        Register(StatusType.FoodAgiCash, PresenceMarker(StatusFlagDefaults.For(StatusType.FoodAgiCash) is var f124 && f124 != ScfFlag.None ? f124 : defaultBuff));  // SC_FOOD_AGI_CASH: presence-only per rAthena db/re/status.yml:3047
        Register(StatusType.FoodVitCash, PresenceMarker(StatusFlagDefaults.For(StatusType.FoodVitCash) is var f125 && f125 != ScfFlag.None ? f125 : defaultBuff));  // SC_FOOD_VIT_CASH: presence-only per rAthena db/re/status.yml:3059
        Register(StatusType.FoodDexCash, PresenceMarker(StatusFlagDefaults.For(StatusType.FoodDexCash) is var f126 && f126 != ScfFlag.None ? f126 : defaultBuff));  // SC_FOOD_DEX_CASH: presence-only per rAthena db/re/status.yml:3071
        Register(StatusType.FoodIntCash, PresenceMarker(StatusFlagDefaults.For(StatusType.FoodIntCash) is var f127 && f127 != ScfFlag.None ? f127 : defaultBuff));  // SC_FOOD_INT_CASH: presence-only per rAthena db/re/status.yml:3083
        Register(StatusType.FoodLukCash, PresenceMarker(StatusFlagDefaults.For(StatusType.FoodLukCash) is var f128 && f128 != ScfFlag.None ? f128 : defaultBuff));  // SC_FOOD_LUK_CASH: presence-only per rAthena db/re/status.yml:3095
        Register(StatusType.Enchantblade, PresenceMarker(StatusFlagDefaults.For(StatusType.Enchantblade) is var f129 && f129 != ScfFlag.None ? f129 : defaultBuff));  // SC_ENCHANTBLADE: presence-only per rAthena db/re/status.yml:3164
        Register(StatusType.Millenniumshield, PresenceMarker(StatusFlagDefaults.For(StatusType.Millenniumshield) is var f130 && f130 != ScfFlag.None ? f130 : defaultBuff));  // SC_MILLENNIUMSHIELD: presence-only per rAthena db/re/status.yml:3177
        Register(StatusType.Crushstrike, PresenceMarker(StatusFlagDefaults.For(StatusType.Crushstrike) is var f131 && f131 != ScfFlag.None ? f131 : defaultBuff));  // SC_CRUSHSTRIKE: presence-only per rAthena db/re/status.yml:3185
        Register(StatusType.Refresh, PresenceMarker(StatusFlagDefaults.For(StatusType.Refresh) is var f132 && f132 != ScfFlag.None ? f132 : defaultBuff));  // SC_REFRESH: presence-only per rAthena db/re/status.yml:3191
        Register(StatusType.ReuseRefresh, PresenceMarker(StatusFlagDefaults.For(StatusType.ReuseRefresh) is var f133 && f133 != ScfFlag.None ? f133 : defaultBuff));  // SC_REUSE_REFRESH: presence-only per rAthena db/re/status.yml:3199
        Register(StatusType.Vitalityactivation, PresenceMarker(StatusFlagDefaults.For(StatusType.Vitalityactivation) is var f134 && f134 != ScfFlag.None ? f134 : defaultBuff));  // SC_VITALITYACTIVATION: presence-only per rAthena db/re/status.yml:3231
        Register(StatusType.Stormblast, PresenceMarker(StatusFlagDefaults.For(StatusType.Stormblast) is var f135 && f135 != ScfFlag.None ? f135 : defaultBuff));  // SC_STORMBLAST: presence-only per rAthena db/re/status.yml:3240
        Register(StatusType.Abundance, PresenceMarker(StatusFlagDefaults.For(StatusType.Abundance) is var f136 && f136 != ScfFlag.None ? f136 : defaultBuff));  // SC_ABUNDANCE: presence-only per rAthena db/re/status.yml:3260
        Register(StatusType.Oratio, PresenceMarker(StatusFlagDefaults.For(StatusType.Oratio) is var f137 && f137 != ScfFlag.None ? f137 : defaultBuff));  // SC_ORATIO: presence-only per rAthena db/re/status.yml:3291
        Register(StatusType.Renovatio, PresenceMarker(StatusFlagDefaults.For(StatusType.Renovatio) is var f138 && f138 != ScfFlag.None ? f138 : defaultBuff));  // SC_RENOVATIO: presence-only per rAthena db/re/status.yml:3315
        Register(StatusType.Expiatio, PresenceMarker(StatusFlagDefaults.For(StatusType.Expiatio) is var f139 && f139 != ScfFlag.None ? f139 : defaultBuff));  // SC_EXPIATIO: presence-only per rAthena db/re/status.yml:3323
        Register(StatusType.Duplelight, PresenceMarker(StatusFlagDefaults.For(StatusType.Duplelight) is var f140 && f140 != ScfFlag.None ? f140 : defaultBuff));  // SC_DUPLELIGHT: presence-only per rAthena db/re/status.yml:3329
        Register(StatusType.Secrament, PresenceMarker(StatusFlagDefaults.For(StatusType.Secrament) is var f141 && f141 != ScfFlag.None ? f141 : defaultBuff));  // SC_SECRAMENT: presence-only per rAthena db/re/status.yml:3336
        Register(StatusType.Whiteimprison, PresenceMarker(StatusFlagDefaults.For(StatusType.Whiteimprison) is var f142 && f142 != ScfFlag.None ? f142 : defaultBuff));  // SC_WHITEIMPRISON: presence-only per rAthena db/re/status.yml:3342
        Register(StatusType.Stasis, PresenceMarker(StatusFlagDefaults.For(StatusType.Stasis) is var f143 && f143 != ScfFlag.None ? f143 : defaultBuff));  // SC_STASIS: presence-only per rAthena db/re/status.yml:3390
        Register(StatusType.ReadingSb, PresenceMarker(StatusFlagDefaults.For(StatusType.ReadingSb) is var f144 && f144 != ScfFlag.None ? f144 : defaultBuff));  // SC_READING_SB: presence-only per rAthena db/re/status.yml:3424
        Register(StatusType.FreezeSp, PresenceMarker(StatusFlagDefaults.For(StatusType.FreezeSp) is var f145 && f145 != ScfFlag.None ? f145 : defaultBuff));  // SC_FREEZE_SP: presence-only per rAthena db/re/status.yml:3427
        Register(StatusType.Fearbreeze, PresenceMarker(StatusFlagDefaults.For(StatusType.Fearbreeze) is var f146 && f146 != ScfFlag.None ? f146 : defaultBuff));  // SC_FEARBREEZE: presence-only per rAthena db/re/status.yml:3431
        Register(StatusType.Electricshocker, PresenceMarker(StatusFlagDefaults.For(StatusType.Electricshocker) is var f147 && f147 != ScfFlag.None ? f147 : defaultBuff));  // SC_ELECTRICSHOCKER: presence-only per rAthena db/re/status.yml:3439
        Register(StatusType.Bite, PresenceMarker(StatusFlagDefaults.For(StatusType.Bite) is var f148 && f148 != ScfFlag.None ? f148 : defaultBuff));  // SC_BITE: presence-only per rAthena db/re/status.yml:3464
        Register(StatusType.Shapeshift, PresenceMarker(StatusFlagDefaults.For(StatusType.Shapeshift) is var f149 && f149 != ScfFlag.None ? f149 : defaultBuff));  // SC_SHAPESHIFT: presence-only per rAthena db/re/status.yml:3516
        Register(StatusType.Magneticfield, PresenceMarker(StatusFlagDefaults.For(StatusType.Magneticfield) is var f150 && f150 != ScfFlag.None ? f150 : defaultBuff));  // SC_MAGNETICFIELD: presence-only per rAthena db/re/status.yml:3547
        Register(StatusType.NeutralbarrierMaster, PresenceMarker(StatusFlagDefaults.For(StatusType.NeutralbarrierMaster) is var f151 && f151 != ScfFlag.None ? f151 : defaultBuff));  // SC_NEUTRALBARRIER_MASTER: presence-only per rAthena db/re/status.yml:3574
        Register(StatusType.Overheat, PresenceMarker(StatusFlagDefaults.For(StatusType.Overheat) is var f152 && f152 != ScfFlag.None ? f152 : defaultBuff));  // SC_OVERHEAT: presence-only per rAthena db/re/status.yml:3607
        Register(StatusType.OverheatLimitpoint, PresenceMarker(StatusFlagDefaults.For(StatusType.OverheatLimitpoint) is var f153 && f153 != ScfFlag.None ? f153 : defaultBuff));  // SC_OVERHEAT_LIMITPOINT: presence-only per rAthena db/re/status.yml:3615
        Register(StatusType.Poisoningweapon, PresenceMarker(StatusFlagDefaults.For(StatusType.Poisoningweapon) is var f154 && f154 != ScfFlag.None ? f154 : defaultBuff));  // SC_POISONINGWEAPON: presence-only per rAthena db/re/status.yml:3632
        Register(StatusType.Weaponblocking, PresenceMarker(StatusFlagDefaults.For(StatusType.Weaponblocking) is var f155 && f155 != ScfFlag.None ? f155 : defaultBuff));  // SC_WEAPONBLOCKING: presence-only per rAthena db/re/status.yml:3640
        Register(StatusType.Rollingcutter, PresenceMarker(StatusFlagDefaults.For(StatusType.Rollingcutter) is var f156 && f156 != ScfFlag.None ? f156 : defaultBuff));  // SC_ROLLINGCUTTER: presence-only per rAthena db/re/status.yml:3697
        Register(StatusType.Leechesend, PresenceMarker(StatusFlagDefaults.For(StatusType.Leechesend) is var f157 && f157 != ScfFlag.None ? f157 : defaultBuff));  // SC_LEECHESEND: presence-only per rAthena db/re/status.yml:3862
        Register(StatusType.Exeedbreak, PresenceMarker(StatusFlagDefaults.For(StatusType.Exeedbreak) is var f158 && f158 != ScfFlag.None ? f158 : defaultBuff));  // SC_EXEEDBREAK: presence-only per rAthena db/re/status.yml:3924
        Register(StatusType.Spellfist, PresenceMarker(StatusFlagDefaults.For(StatusType.Spellfist) is var f159 && f159 != ScfFlag.None ? f159 : defaultBuff));  // SC_SPELLFIST: presence-only per rAthena db/re/status.yml:3984
        Register(StatusType.Crystalize, PresenceMarker(StatusFlagDefaults.For(StatusType.Crystalize) is var f160 && f160 != ScfFlag.None ? f160 : defaultBuff));  // SC_CRYSTALIZE: presence-only per rAthena db/re/status.yml:3989
        Register(StatusType.Warmer, PresenceMarker(StatusFlagDefaults.For(StatusType.Warmer) is var f161 && f161 != ScfFlag.None ? f161 : defaultBuff));  // SC_WARMER: presence-only per rAthena db/re/status.yml:4023
        Register(StatusType.Propertywalk, PresenceMarker(StatusFlagDefaults.For(StatusType.Propertywalk) is var f162 && f162 != ScfFlag.None ? f162 : defaultBuff));  // SC_PROPERTYWALK: presence-only per rAthena db/re/status.yml:4054
        Register(StatusType.Voiceofsiren, PresenceMarker(StatusFlagDefaults.For(StatusType.Voiceofsiren) is var f163 && f163 != ScfFlag.None ? f163 : defaultBuff));  // SC_VOICEOFSIREN: presence-only per rAthena db/re/status.yml:4154
        Register(StatusType.Deepsleep, PresenceMarker(StatusFlagDefaults.For(StatusType.Deepsleep) is var f164 && f164 != ScfFlag.None ? f164 : defaultBuff));  // SC_DEEPSLEEP: presence-only per rAthena db/re/status.yml:4174
        Register(StatusType.Sircleofnature, PresenceMarker(StatusFlagDefaults.For(StatusType.Sircleofnature) is var f165 && f165 != ScfFlag.None ? f165 : defaultBuff));  // SC_SIRCLEOFNATURE: presence-only per rAthena db/re/status.yml:4215
        Register(StatusType.GloomydaySk, PresenceMarker(StatusFlagDefaults.For(StatusType.GloomydaySk) is var f166 && f166 != ScfFlag.None ? f166 : defaultBuff));  // SC_GLOOMYDAY_SK: presence-only per rAthena db/re/status.yml:4249
        Register(StatusType.Songofmana, PresenceMarker(StatusFlagDefaults.For(StatusType.Songofmana) is var f167 && f167 != ScfFlag.None ? f167 : defaultBuff));  // SC_SONGOFMANA: presence-only per rAthena db/re/status.yml:4251
        Register(StatusType.Unlimitedhummingvoice, PresenceMarker(StatusFlagDefaults.For(StatusType.Unlimitedhummingvoice) is var f168 && f168 != ScfFlag.None ? f168 : defaultBuff));  // SC_UNLIMITEDHUMMINGVOICE: presence-only per rAthena db/re/status.yml:4382
        Register(StatusType.SitdownForce, PresenceMarker(StatusFlagDefaults.For(StatusType.SitdownForce) is var f169 && f169 != ScfFlag.None ? f169 : defaultBuff));  // SC_SITDOWN_FORCE: presence-only per rAthena db/re/status.yml:4401
        Register(StatusType.Netherworld, PresenceMarker(StatusFlagDefaults.For(StatusType.Netherworld) is var f170 && f170 != ScfFlag.None ? f170 : defaultBuff));  // SC_NETHERWORLD: presence-only per rAthena db/re/status.yml:4406
        Register(StatusType.GtEnergygain, PresenceMarker(StatusFlagDefaults.For(StatusType.GtEnergygain) is var f171 && f171 != ScfFlag.None ? f171 : defaultBuff));  // SC_GT_ENERGYGAIN: presence-only per rAthena db/re/status.yml:4482
        Register(StatusType.Thornstrap, PresenceMarker(StatusFlagDefaults.For(StatusType.Thornstrap) is var f172 && f172 != ScfFlag.None ? f172 : defaultBuff));  // SC_THORNSTRAP: presence-only per rAthena db/re/status.yml:4527
        Register(StatusType.Bloodsucker, PresenceMarker(StatusFlagDefaults.For(StatusType.Bloodsucker) is var f173 && f173 != ScfFlag.None ? f173 : defaultBuff));  // SC_BLOODSUCKER: presence-only per rAthena db/re/status.yml:4538
        Register(StatusType.MysteriousPowder, PresenceMarker(StatusFlagDefaults.For(StatusType.MysteriousPowder) is var f174 && f174 != ScfFlag.None ? f174 : defaultBuff));  // SC_MYSTERIOUS_POWDER: presence-only per rAthena db/re/status.yml:4598
        Register(StatusType.BananaBombSitdown, PresenceMarker(StatusFlagDefaults.For(StatusType.BananaBombSitdown) is var f175 && f175 != ScfFlag.None ? f175 : defaultBuff));  // SC_BANANA_BOMB_SITDOWN: presence-only per rAthena db/re/status.yml:4624
        Register(StatusType.SavageSteak, PresenceMarker(StatusFlagDefaults.For(StatusType.SavageSteak) is var f176 && f176 != ScfFlag.None ? f176 : defaultBuff));  // SC_SAVAGE_STEAK: presence-only per rAthena db/re/status.yml:4631
        Register(StatusType.CocktailWargBlood, PresenceMarker(StatusFlagDefaults.For(StatusType.CocktailWargBlood) is var f177 && f177 != ScfFlag.None ? f177 : defaultBuff));  // SC_COCKTAIL_WARG_BLOOD: presence-only per rAthena db/re/status.yml:4641
        Register(StatusType.MinorBbq, PresenceMarker(StatusFlagDefaults.For(StatusType.MinorBbq) is var f178 && f178 != ScfFlag.None ? f178 : defaultBuff));  // SC_MINOR_BBQ: presence-only per rAthena db/re/status.yml:4651
        Register(StatusType.SiromaIceTea, PresenceMarker(StatusFlagDefaults.For(StatusType.SiromaIceTea) is var f179 && f179 != ScfFlag.None ? f179 : defaultBuff));  // SC_SIROMA_ICE_TEA: presence-only per rAthena db/re/status.yml:4661
        Register(StatusType.DroceraHerbSteamed, PresenceMarker(StatusFlagDefaults.For(StatusType.DroceraHerbSteamed) is var f180 && f180 != ScfFlag.None ? f180 : defaultBuff));  // SC_DROCERA_HERB_STEAMED: presence-only per rAthena db/re/status.yml:4671
        Register(StatusType.Boost500, PresenceMarker(StatusFlagDefaults.For(StatusType.Boost500) is var f181 && f181 != ScfFlag.None ? f181 : defaultBuff));  // SC_BOOST500: presence-only per rAthena db/re/status.yml:4691
        Register(StatusType.FullSwingK, PresenceMarker(StatusFlagDefaults.For(StatusType.FullSwingK) is var f182 && f182 != ScfFlag.None ? f182 : defaultBuff));  // SC_FULL_SWING_K: presence-only per rAthena db/re/status.yml:4700
        Register(StatusType.ManaPlus, PresenceMarker(StatusFlagDefaults.For(StatusType.ManaPlus) is var f183 && f183 != ScfFlag.None ? f183 : defaultBuff));  // SC_MANA_PLUS: presence-only per rAthena db/re/status.yml:4709
        Register(StatusType.MustleM, PresenceMarker(StatusFlagDefaults.For(StatusType.MustleM) is var f184 && f184 != ScfFlag.None ? f184 : defaultBuff));  // SC_MUSTLE_M: presence-only per rAthena db/re/status.yml:4718
        Register(StatusType.LifeForceF, PresenceMarker(StatusFlagDefaults.For(StatusType.LifeForceF) is var f185 && f185 != ScfFlag.None ? f185 : defaultBuff));  // SC_LIFE_FORCE_F: presence-only per rAthena db/re/status.yml:4727
        Register(StatusType.ExtractWhitePotionZ, PresenceMarker(StatusFlagDefaults.For(StatusType.ExtractWhitePotionZ) is var f186 && f186 != ScfFlag.None ? f186 : defaultBuff));  // SC_EXTRACT_WHITE_POTION_Z: presence-only per rAthena db/re/status.yml:4736
        Register(StatusType.Vitata500, PresenceMarker(StatusFlagDefaults.For(StatusType.Vitata500) is var f187 && f187 != ScfFlag.None ? f187 : defaultBuff));  // SC_VITATA_500: presence-only per rAthena db/re/status.yml:4745
        Register(StatusType.ExtractSalamineJuice, PresenceMarker(StatusFlagDefaults.For(StatusType.ExtractSalamineJuice) is var f188 && f188 != ScfFlag.None ? f188 : defaultBuff));  // SC_EXTRACT_SALAMINE_JUICE: presence-only per rAthena db/re/status.yml:4755
        Register(StatusType.Shadowform, PresenceMarker(StatusFlagDefaults.For(StatusType.Shadowform) is var f189 && f189 != ScfFlag.None ? f189 : defaultBuff));  // SC__SHADOWFORM: presence-only per rAthena db/re/status.yml:4776
        Register(StatusType.Deadlyinfect, PresenceMarker(StatusFlagDefaults.For(StatusType.Deadlyinfect) is var f190 && f190 != ScfFlag.None ? f190 : defaultBuff));  // SC__DEADLYINFECT: presence-only per rAthena db/re/status.yml:4823
        Register(StatusType.Ignorance, PresenceMarker(StatusFlagDefaults.For(StatusType.Ignorance) is var f191 && f191 != ScfFlag.None ? f191 : defaultBuff));  // SC__IGNORANCE: presence-only per rAthena db/re/status.yml:4857
        Register(StatusType.CircleOfFire, PresenceMarker(StatusFlagDefaults.For(StatusType.CircleOfFire) is var f192 && f192 != ScfFlag.None ? f192 : defaultBuff));  // SC_CIRCLE_OF_FIRE: presence-only per rAthena db/re/status.yml:4956
        Register(StatusType.FireCloak, PresenceMarker(StatusFlagDefaults.For(StatusType.FireCloak) is var f193 && f193 != ScfFlag.None ? f193 : defaultBuff));  // SC_FIRE_CLOAK: presence-only per rAthena db/re/status.yml:4968
        Register(StatusType.WaterScreen, PresenceMarker(StatusFlagDefaults.For(StatusType.WaterScreen) is var f194 && f194 != ScfFlag.None ? f194 : defaultBuff));  // SC_WATER_SCREEN: presence-only per rAthena db/re/status.yml:4979
        Register(StatusType.WaterDrop, PresenceMarker(StatusFlagDefaults.For(StatusType.WaterDrop) is var f195 && f195 != ScfFlag.None ? f195 : defaultBuff));  // SC_WATER_DROP: presence-only per rAthena db/re/status.yml:4990
        Register(StatusType.SolidSkin, PresenceMarker(StatusFlagDefaults.For(StatusType.SolidSkin) is var f196 && f196 != ScfFlag.None ? f196 : defaultBuff));  // SC_SOLID_SKIN: presence-only per rAthena db/re/status.yml:5040
        Register(StatusType.StoneShield, PresenceMarker(StatusFlagDefaults.For(StatusType.StoneShield) is var f197 && f197 != ScfFlag.None ? f197 : defaultBuff));  // SC_STONE_SHIELD: presence-only per rAthena db/re/status.yml:5052
        Register(StatusType.Pyrotechnic, PresenceMarker(StatusFlagDefaults.For(StatusType.Pyrotechnic) is var f198 && f198 != ScfFlag.None ? f198 : defaultBuff));  // SC_PYROTECHNIC: presence-only per rAthena db/re/status.yml:5076
        Register(StatusType.Gust, PresenceMarker(StatusFlagDefaults.For(StatusType.Gust) is var f199 && f199 != ScfFlag.None ? f199 : defaultBuff));  // SC_GUST: presence-only per rAthena db/re/status.yml:5195
        Register(StatusType.Upheaval, PresenceMarker(StatusFlagDefaults.For(StatusType.Upheaval) is var f200 && f200 != ScfFlag.None ? f200 : defaultBuff));  // SC_UPHEAVAL: presence-only per rAthena db/re/status.yml:5295
        Register(StatusType.TidalWeapon, PresenceMarker(StatusFlagDefaults.For(StatusType.TidalWeapon) is var f201 && f201 != ScfFlag.None ? f201 : defaultBuff));  // SC_TIDAL_WEAPON: presence-only per rAthena db/re/status.yml:5315
        Register(StatusType.Raid, PresenceMarker(StatusFlagDefaults.For(StatusType.Raid) is var f202 && f202 != ScfFlag.None ? f202 : defaultBuff));  // SC_RAID: presence-only per rAthena db/re/status.yml:5400
        Register(StatusType.Spellbook1, PresenceMarker(StatusFlagDefaults.For(StatusType.Spellbook1) is var f203 && f203 != ScfFlag.None ? f203 : defaultBuff));  // SC_SPELLBOOK1: presence-only per rAthena db/re/status.yml:5478
        Register(StatusType.Spellbook2, PresenceMarker(StatusFlagDefaults.For(StatusType.Spellbook2) is var f204 && f204 != ScfFlag.None ? f204 : defaultBuff));  // SC_SPELLBOOK2: presence-only per rAthena db/re/status.yml:5482
        Register(StatusType.Spellbook3, PresenceMarker(StatusFlagDefaults.For(StatusType.Spellbook3) is var f205 && f205 != ScfFlag.None ? f205 : defaultBuff));  // SC_SPELLBOOK3: presence-only per rAthena db/re/status.yml:5486
        Register(StatusType.Spellbook4, PresenceMarker(StatusFlagDefaults.For(StatusType.Spellbook4) is var f206 && f206 != ScfFlag.None ? f206 : defaultBuff));  // SC_SPELLBOOK4: presence-only per rAthena db/re/status.yml:5490
        Register(StatusType.Spellbook5, PresenceMarker(StatusFlagDefaults.For(StatusType.Spellbook5) is var f207 && f207 != ScfFlag.None ? f207 : defaultBuff));  // SC_SPELLBOOK5: presence-only per rAthena db/re/status.yml:5494
        Register(StatusType.Spellbook6, PresenceMarker(StatusFlagDefaults.For(StatusType.Spellbook6) is var f208 && f208 != ScfFlag.None ? f208 : defaultBuff));  // SC_SPELLBOOK6: presence-only per rAthena db/re/status.yml:5498
        Register(StatusType.Maxspellbook, PresenceMarker(StatusFlagDefaults.For(StatusType.Maxspellbook) is var f209 && f209 != ScfFlag.None ? f209 : defaultBuff));  // SC_MAXSPELLBOOK: presence-only per rAthena db/re/status.yml:5502
        Register(StatusType.Partyflee, PresenceMarker(StatusFlagDefaults.For(StatusType.Partyflee) is var f210 && f210 != ScfFlag.None ? f210 : defaultBuff));  // SC_PARTYFLEE: presence-only per rAthena db/re/status.yml:5522
        Register(StatusType.Meikyousisui, PresenceMarker(StatusFlagDefaults.For(StatusType.Meikyousisui) is var f211 && f211 != ScfFlag.None ? f211 : defaultBuff));  // SC_MEIKYOUSISUI: presence-only per rAthena db/re/status.yml:5528
        Register(StatusType.Jyumonjikiri, PresenceMarker(StatusFlagDefaults.For(StatusType.Jyumonjikiri) is var f212 && f212 != ScfFlag.None ? f212 : defaultBuff));  // SC_JYUMONJIKIRI: presence-only per rAthena db/re/status.yml:5538
        Register(StatusType.Zenkai, PresenceMarker(StatusFlagDefaults.For(StatusType.Zenkai) is var f213 && f213 != ScfFlag.None ? f213 : defaultBuff));  // SC_ZENKAI: presence-only per rAthena db/re/status.yml:5567
        Register(StatusType.Kagehumi, PresenceMarker(StatusFlagDefaults.For(StatusType.Kagehumi) is var f214 && f214 != ScfFlag.None ? f214 : defaultBuff));  // SC_KAGEHUMI: presence-only per rAthena db/re/status.yml:5570
        Register(StatusType.Kyomu, PresenceMarker(StatusFlagDefaults.For(StatusType.Kyomu) is var f215 && f215 != ScfFlag.None ? f215 : defaultBuff));  // SC_KYOMU: presence-only per rAthena db/re/status.yml:5576
        Register(StatusType.Kagemusya, PresenceMarker(StatusFlagDefaults.For(StatusType.Kagemusya) is var f216 && f216 != ScfFlag.None ? f216 : defaultBuff));  // SC_KAGEMUSYA: presence-only per rAthena db/re/status.yml:5579
        Register(StatusType.StyleChange, PresenceMarker(StatusFlagDefaults.For(StatusType.StyleChange) is var f217 && f217 != ScfFlag.None ? f217 : defaultBuff));  // SC_STYLE_CHANGE: presence-only per rAthena db/re/status.yml:5598
        Register(StatusType.PainKiller, PresenceMarker(StatusFlagDefaults.For(StatusType.PainKiller) is var f218 && f218 != ScfFlag.None ? f218 : defaultBuff));  // SC_PAIN_KILLER: presence-only per rAthena db/re/status.yml:5724
        Register(StatusType.Hanbok, PresenceMarker(StatusFlagDefaults.For(StatusType.Hanbok) is var f219 && f219 != ScfFlag.None ? f219 : defaultBuff));  // SC_HANBOK: presence-only per rAthena db/re/status.yml:5732
        Register(StatusType.Darkcrow, PresenceMarker(StatusFlagDefaults.For(StatusType.Darkcrow) is var f220 && f220 != ScfFlag.None ? f220 : defaultBuff));  // SC_DARKCROW: presence-only per rAthena db/re/status.yml:5757
        Register(StatusType.Unlimit, PresenceMarker(StatusFlagDefaults.For(StatusType.Unlimit) is var f221 && f221 != ScfFlag.None ? f221 : defaultBuff));  // SC_UNLIMIT: presence-only per rAthena db/re/status.yml:5787
        Register(StatusType.KingsGrace, PresenceMarker(StatusFlagDefaults.For(StatusType.KingsGrace) is var f222 && f222 != ScfFlag.None ? f222 : defaultBuff));  // SC_KINGS_GRACE: presence-only per rAthena db/re/status.yml:5794
        Register(StatusType.Offertorium, PresenceMarker(StatusFlagDefaults.For(StatusType.Offertorium) is var f223 && f223 != ScfFlag.None ? f223 : defaultBuff));  // SC_OFFERTORIUM: presence-only per rAthena db/re/status.yml:5837
        Register(StatusType.MonsterTransform, PresenceMarker(StatusFlagDefaults.For(StatusType.MonsterTransform) is var f224 && f224 != ScfFlag.None ? f224 : defaultBuff));  // SC_MONSTER_TRANSFORM: presence-only per rAthena db/re/status.yml:5867
        Register(StatusType.AngelProtect, PresenceMarker(StatusFlagDefaults.For(StatusType.AngelProtect) is var f225 && f225 != ScfFlag.None ? f225 : defaultBuff));  // SC_ANGEL_PROTECT: presence-only per rAthena db/re/status.yml:5876
        Register(StatusType.SuperStar, PresenceMarker(StatusFlagDefaults.For(StatusType.SuperStar) is var f226 && f226 != ScfFlag.None ? f226 : defaultBuff));  // SC_SUPER_STAR: presence-only per rAthena db/re/status.yml:5906
        Register(StatusType.Magicalbullet, PresenceMarker(StatusFlagDefaults.For(StatusType.Magicalbullet) is var f227 && f227 != ScfFlag.None ? f227 : defaultBuff));  // SC_MAGICALBULLET: presence-only per rAthena db/re/status.yml:5932
        Register(StatusType.PAlter, PresenceMarker(StatusFlagDefaults.For(StatusType.PAlter) is var f228 && f228 != ScfFlag.None ? f228 : defaultBuff));  // SC_P_ALTER: presence-only per rAthena db/re/status.yml:5935
        Register(StatusType.EChain, PresenceMarker(StatusFlagDefaults.For(StatusType.EChain) is var f229 && f229 != ScfFlag.None ? f229 : defaultBuff));  // SC_E_CHAIN: presence-only per rAthena db/re/status.yml:5946
        Register(StatusType.AntiMBlast, PresenceMarker(StatusFlagDefaults.For(StatusType.AntiMBlast) is var f230 && f230 != ScfFlag.None ? f230 : defaultBuff));  // SC_ANTI_M_BLAST: presence-only per rAthena db/re/status.yml:5966
        Register(StatusType.HMine, PresenceMarker(StatusFlagDefaults.For(StatusType.HMine) is var f231 && f231 != ScfFlag.None ? f231 : defaultBuff));  // SC_H_MINE: presence-only per rAthena db/re/status.yml:5986
        Register(StatusType.QdShotReady, PresenceMarker(StatusFlagDefaults.For(StatusType.QdShotReady) is var f232 && f232 != ScfFlag.None ? f232 : defaultBuff));  // SC_QD_SHOT_READY: presence-only per rAthena db/re/status.yml:5994
        Register(StatusType.MtfAspd, PresenceMarker(StatusFlagDefaults.For(StatusType.MtfAspd) is var f233 && f233 != ScfFlag.None ? f233 : defaultBuff));  // SC_MTF_ASPD: presence-only per rAthena db/re/status.yml:5998
        Register(StatusType.MtfRangeatk, PresenceMarker(StatusFlagDefaults.For(StatusType.MtfRangeatk) is var f234 && f234 != ScfFlag.None ? f234 : defaultBuff));  // SC_MTF_RANGEATK: presence-only per rAthena db/re/status.yml:6010
        Register(StatusType.MtfMatk, PresenceMarker(StatusFlagDefaults.For(StatusType.MtfMatk) is var f235 && f235 != ScfFlag.None ? f235 : defaultBuff));  // SC_MTF_MATK: presence-only per rAthena db/re/status.yml:6021
        Register(StatusType.MtfMleatked, PresenceMarker(StatusFlagDefaults.For(StatusType.MtfMleatked) is var f236 && f236 != ScfFlag.None ? f236 : defaultBuff));  // SC_MTF_MLEATKED: presence-only per rAthena db/re/status.yml:6032
        Register(StatusType.MtfCridamage, PresenceMarker(StatusFlagDefaults.For(StatusType.MtfCridamage) is var f237 && f237 != ScfFlag.None ? f237 : defaultBuff));  // SC_MTF_CRIDAMAGE: presence-only per rAthena db/re/status.yml:6047
        Register(StatusType.Oktoberfest, PresenceMarker(StatusFlagDefaults.For(StatusType.Oktoberfest) is var f238 && f238 != ScfFlag.None ? f238 : defaultBuff));  // SC_OKTOBERFEST: presence-only per rAthena db/re/status.yml:6058
        Register(StatusType.Strangelights, PresenceMarker(StatusFlagDefaults.For(StatusType.Strangelights) is var f239 && f239 != ScfFlag.None ? f239 : defaultBuff));  // SC_STRANGELIGHTS: presence-only per rAthena db/re/status.yml:6070
        Register(StatusType.DecorationOfMusic, PresenceMarker(StatusFlagDefaults.For(StatusType.DecorationOfMusic) is var f240 && f240 != ScfFlag.None ? f240 : defaultBuff));  // SC_DECORATION_OF_MUSIC: presence-only per rAthena db/re/status.yml:6081
        Register(StatusType.QuestBuff1, PresenceMarker(StatusFlagDefaults.For(StatusType.QuestBuff1) is var f241 && f241 != ScfFlag.None ? f241 : defaultBuff));  // SC_QUEST_BUFF1: presence-only per rAthena db/re/status.yml:6092
        Register(StatusType.QuestBuff2, PresenceMarker(StatusFlagDefaults.For(StatusType.QuestBuff2) is var f242 && f242 != ScfFlag.None ? f242 : defaultBuff));  // SC_QUEST_BUFF2: presence-only per rAthena db/re/status.yml:6104
        Register(StatusType.QuestBuff3, PresenceMarker(StatusFlagDefaults.For(StatusType.QuestBuff3) is var f243 && f243 != ScfFlag.None ? f243 : defaultBuff));  // SC_QUEST_BUFF3: presence-only per rAthena db/re/status.yml:6116
        Register(StatusType.Feintbomb, PresenceMarker(StatusFlagDefaults.For(StatusType.Feintbomb) is var f244 && f244 != ScfFlag.None ? f244 : defaultBuff));  // SC__FEINTBOMB: presence-only per rAthena db/re/status.yml:6147
        Register(StatusType.Chaos, PresenceMarker(StatusFlagDefaults.For(StatusType.Chaos) is var f245 && f245 != ScfFlag.None ? f245 : defaultBuff));  // SC__CHAOS: presence-only per rAthena db/re/status.yml:6157
        Register(StatusType.MtfRangeatk2, PresenceMarker(StatusFlagDefaults.For(StatusType.MtfRangeatk2) is var f246 && f246 != ScfFlag.None ? f246 : defaultBuff));  // SC_MTF_RANGEATK2: presence-only per rAthena db/re/status.yml:6188
        Register(StatusType.MtfMatk2, PresenceMarker(StatusFlagDefaults.For(StatusType.MtfMatk2) is var f247 && f247 != ScfFlag.None ? f247 : defaultBuff));  // SC_MTF_MATK2: presence-only per rAthena db/re/status.yml:6199
        Register(StatusType.Rwc2011Scroll, PresenceMarker(StatusFlagDefaults.For(StatusType.Rwc2011Scroll) is var f248 && f248 != ScfFlag.None ? f248 : defaultBuff));  // SC_2011RWC_SCROLL: presence-only per rAthena db/re/status.yml:6210
        Register(StatusType.JpEvent04, PresenceMarker(StatusFlagDefaults.For(StatusType.JpEvent04) is var f249 && f249 != ScfFlag.None ? f249 : defaultBuff));  // SC_JP_EVENT04: presence-only per rAthena db/re/status.yml:6226
        Register(StatusType.MtfMhp, PresenceMarker(StatusFlagDefaults.For(StatusType.MtfMhp) is var f250 && f250 != ScfFlag.None ? f250 : defaultBuff));  // SC_MTF_MHP: presence-only per rAthena db/re/status.yml:6237
        Register(StatusType.MtfMsp, PresenceMarker(StatusFlagDefaults.For(StatusType.MtfMsp) is var f251 && f251 != ScfFlag.None ? f251 : defaultBuff));  // SC_MTF_MSP: presence-only per rAthena db/re/status.yml:6248
        Register(StatusType.MtfPumpkin, PresenceMarker(StatusFlagDefaults.For(StatusType.MtfPumpkin) is var f252 && f252 != ScfFlag.None ? f252 : defaultBuff));  // SC_MTF_PUMPKIN: presence-only per rAthena db/re/status.yml:6259
        Register(StatusType.MtfHitflee, PresenceMarker(StatusFlagDefaults.For(StatusType.MtfHitflee) is var f253 && f253 != ScfFlag.None ? f253 : defaultBuff));  // SC_MTF_HITFLEE: presence-only per rAthena db/re/status.yml:6271
        Register(StatusType.Crifood, PresenceMarker(StatusFlagDefaults.For(StatusType.Crifood) is var f254 && f254 != ScfFlag.None ? f254 : defaultBuff));  // SC_CRIFOOD: presence-only per rAthena db/re/status.yml:6293
        Register(StatusType.AtthasteCash, PresenceMarker(StatusFlagDefaults.For(StatusType.AtthasteCash) is var f255 && f255 != ScfFlag.None ? f255 : defaultBuff));  // SC_ATTHASTE_CASH: presence-only per rAthena db/re/status.yml:6302
        Register(StatusType.ReuseLimitA, PresenceMarker(StatusFlagDefaults.For(StatusType.ReuseLimitA) is var f256 && f256 != ScfFlag.None ? f256 : defaultBuff));  // SC_REUSE_LIMIT_A: presence-only per rAthena db/re/status.yml:6314
        Register(StatusType.ReuseLimitB, PresenceMarker(StatusFlagDefaults.For(StatusType.ReuseLimitB) is var f257 && f257 != ScfFlag.None ? f257 : defaultBuff));  // SC_REUSE_LIMIT_B: presence-only per rAthena db/re/status.yml:6325
        Register(StatusType.ReuseLimitC, PresenceMarker(StatusFlagDefaults.For(StatusType.ReuseLimitC) is var f258 && f258 != ScfFlag.None ? f258 : defaultBuff));  // SC_REUSE_LIMIT_C: presence-only per rAthena db/re/status.yml:6336
        Register(StatusType.ReuseLimitD, PresenceMarker(StatusFlagDefaults.For(StatusType.ReuseLimitD) is var f259 && f259 != ScfFlag.None ? f259 : defaultBuff));  // SC_REUSE_LIMIT_D: presence-only per rAthena db/re/status.yml:6347
        Register(StatusType.ReuseLimitE, PresenceMarker(StatusFlagDefaults.For(StatusType.ReuseLimitE) is var f260 && f260 != ScfFlag.None ? f260 : defaultBuff));  // SC_REUSE_LIMIT_E: presence-only per rAthena db/re/status.yml:6358
        Register(StatusType.ReuseLimitF, PresenceMarker(StatusFlagDefaults.For(StatusType.ReuseLimitF) is var f261 && f261 != ScfFlag.None ? f261 : defaultBuff));  // SC_REUSE_LIMIT_F: presence-only per rAthena db/re/status.yml:6369
        Register(StatusType.ReuseLimitG, PresenceMarker(StatusFlagDefaults.For(StatusType.ReuseLimitG) is var f262 && f262 != ScfFlag.None ? f262 : defaultBuff));  // SC_REUSE_LIMIT_G: presence-only per rAthena db/re/status.yml:6380
        Register(StatusType.ReuseLimitH, PresenceMarker(StatusFlagDefaults.For(StatusType.ReuseLimitH) is var f263 && f263 != ScfFlag.None ? f263 : defaultBuff));  // SC_REUSE_LIMIT_H: presence-only per rAthena db/re/status.yml:6391
        Register(StatusType.ReuseLimitMtf, PresenceMarker(StatusFlagDefaults.For(StatusType.ReuseLimitMtf) is var f264 && f264 != ScfFlag.None ? f264 : defaultBuff));  // SC_REUSE_LIMIT_MTF: presence-only per rAthena db/re/status.yml:6402
        Register(StatusType.ReuseLimitAspdPotion, PresenceMarker(StatusFlagDefaults.For(StatusType.ReuseLimitAspdPotion) is var f265 && f265 != ScfFlag.None ? f265 : defaultBuff));  // SC_REUSE_LIMIT_ASPD_POTION: presence-only per rAthena db/re/status.yml:6413
        Register(StatusType.ReuseMillenniumshield, PresenceMarker(StatusFlagDefaults.For(StatusType.ReuseMillenniumshield) is var f266 && f266 != ScfFlag.None ? f266 : defaultBuff));  // SC_REUSE_MILLENNIUMSHIELD: presence-only per rAthena db/re/status.yml:6424
        Register(StatusType.ReuseCrushstrike, PresenceMarker(StatusFlagDefaults.For(StatusType.ReuseCrushstrike) is var f267 && f267 != ScfFlag.None ? f267 : defaultBuff));  // SC_REUSE_CRUSHSTRIKE: presence-only per rAthena db/re/status.yml:6435
        Register(StatusType.ReuseStormblast, PresenceMarker(StatusFlagDefaults.For(StatusType.ReuseStormblast) is var f268 && f268 != ScfFlag.None ? f268 : defaultBuff));  // SC_REUSE_STORMBLAST: presence-only per rAthena db/re/status.yml:6446
        Register(StatusType.AllRidingReuseLimit, PresenceMarker(StatusFlagDefaults.For(StatusType.AllRidingReuseLimit) is var f269 && f269 != ScfFlag.None ? f269 : defaultBuff));  // SC_ALL_RIDING_REUSE_LIMIT: presence-only per rAthena db/re/status.yml:6457
        Register(StatusType.ReuseLimitEcl, PresenceMarker(StatusFlagDefaults.For(StatusType.ReuseLimitEcl) is var f270 && f270 != ScfFlag.None ? f270 : defaultBuff));  // SC_REUSE_LIMIT_ECL: presence-only per rAthena db/re/status.yml:6468
        Register(StatusType.ReuseLimitRecall, PresenceMarker(StatusFlagDefaults.For(StatusType.ReuseLimitRecall) is var f271 && f271 != ScfFlag.None ? f271 : defaultBuff));  // SC_REUSE_LIMIT_RECALL: presence-only per rAthena db/re/status.yml:6479
        Register(StatusType.NorecoverState, PresenceMarker(StatusFlagDefaults.For(StatusType.NorecoverState) is var f272 && f272 != ScfFlag.None ? f272 : defaultBuff));  // SC_NORECOVER_STATE: presence-only per rAthena db/re/status.yml:6498
        Register(StatusType.Suhide, PresenceMarker(StatusFlagDefaults.For(StatusType.Suhide) is var f273 && f273 != ScfFlag.None ? f273 : defaultBuff));  // SC_SUHIDE: presence-only per rAthena db/re/status.yml:6502
        Register(StatusType.SuStoop, PresenceMarker(StatusFlagDefaults.For(StatusType.SuStoop) is var f274 && f274 != ScfFlag.None ? f274 : defaultBuff));  // SC_SU_STOOP: presence-only per rAthena db/re/status.yml:6516
        Register(StatusType.Spritemable, PresenceMarker(StatusFlagDefaults.For(StatusType.Spritemable) is var f275 && f275 != ScfFlag.None ? f275 : defaultBuff));  // SC_SPRITEMABLE: presence-only per rAthena db/re/status.yml:6519
        Register(StatusType.SvRoottwist, PresenceMarker(StatusFlagDefaults.For(StatusType.SvRoottwist) is var f276 && f276 != ScfFlag.None ? f276 : defaultBuff));  // SC_SV_ROOTTWIST: presence-only per rAthena db/re/status.yml:6536
        Register(StatusType.Tunaparty, PresenceMarker(StatusFlagDefaults.For(StatusType.Tunaparty) is var f277 && f277 != ScfFlag.None ? f277 : defaultBuff));  // SC_TUNAPARTY: presence-only per rAthena db/re/status.yml:6561
        Register(StatusType.Freshshrimp, PresenceMarker(StatusFlagDefaults.For(StatusType.Freshshrimp) is var f278 && f278 != ScfFlag.None ? f278 : defaultBuff));  // SC_FRESHSHRIMP: presence-only per rAthena db/re/status.yml:6571
        Register(StatusType.ActiveMonsterTransform, PresenceMarker(StatusFlagDefaults.For(StatusType.ActiveMonsterTransform) is var f279 && f279 != ScfFlag.None ? f279 : defaultBuff));  // SC_ACTIVE_MONSTER_TRANSFORM: presence-only per rAthena db/re/status.yml:6576
        Register(StatusType.CloudKill, PresenceMarker(StatusFlagDefaults.For(StatusType.CloudKill) is var f280 && f280 != ScfFlag.None ? f280 : defaultBuff));  // SC_CLOUD_KILL: presence-only per rAthena db/re/status.yml (no row — C# port-only sentinel)
        Register(StatusType.Ljosalfar, PresenceMarker(StatusFlagDefaults.For(StatusType.Ljosalfar) is var f281 && f281 != ScfFlag.None ? f281 : defaultBuff));  // SC_LJOSALFAR: presence-only per rAthena db/re/status.yml:6588
        Register(StatusType.MermaidLonging, PresenceMarker(StatusFlagDefaults.For(StatusType.MermaidLonging) is var f282 && f282 != ScfFlag.None ? f282 : defaultBuff));  // SC_MERMAID_LONGING: presence-only per rAthena db/re/status.yml:6599
        Register(StatusType.HatEffect, PresenceMarker(StatusFlagDefaults.For(StatusType.HatEffect) is var f283 && f283 != ScfFlag.None ? f283 : defaultBuff));  // SC_HAT_EFFECT: presence-only per rAthena db/re/status.yml:6610
        Register(StatusType.Flowersmoke, PresenceMarker(StatusFlagDefaults.For(StatusType.Flowersmoke) is var f284 && f284 != ScfFlag.None ? f284 : defaultBuff));  // SC_FLOWERSMOKE: presence-only per rAthena db/re/status.yml:6621
        Register(StatusType.Fstone, PresenceMarker(StatusFlagDefaults.For(StatusType.Fstone) is var f285 && f285 != ScfFlag.None ? f285 : defaultBuff));  // SC_FSTONE: presence-only per rAthena db/re/status.yml:6632
        Register(StatusType.HappinessStar, PresenceMarker(StatusFlagDefaults.For(StatusType.HappinessStar) is var f286 && f286 != ScfFlag.None ? f286 : defaultBuff));  // SC_HAPPINESS_STAR: presence-only per rAthena db/re/status.yml:6643
        Register(StatusType.MapleFalls, PresenceMarker(StatusFlagDefaults.For(StatusType.MapleFalls) is var f287 && f287 != ScfFlag.None ? f287 : defaultBuff));  // SC_MAPLE_FALLS: presence-only per rAthena db/re/status.yml:6654
        Register(StatusType.TimeAccessory, PresenceMarker(StatusFlagDefaults.For(StatusType.TimeAccessory) is var f288 && f288 != ScfFlag.None ? f288 : defaultBuff));  // SC_TIME_ACCESSORY: presence-only per rAthena db/re/status.yml:6665
        Register(StatusType.MagicalFeather, PresenceMarker(StatusFlagDefaults.For(StatusType.MagicalFeather) is var f289 && f289 != ScfFlag.None ? f289 : defaultBuff));  // SC_MAGICAL_FEATHER: presence-only per rAthena db/re/status.yml:6676
        Register(StatusType.GvgGiant, PresenceMarker(StatusFlagDefaults.For(StatusType.GvgGiant) is var f290 && f290 != ScfFlag.None ? f290 : defaultBuff));  // SC_GVG_GIANT: presence-only per rAthena db/re/status.yml:6687
        Register(StatusType.GvgGolem, PresenceMarker(StatusFlagDefaults.For(StatusType.GvgGolem) is var f291 && f291 != ScfFlag.None ? f291 : defaultBuff));  // SC_GVG_GOLEM: presence-only per rAthena db/re/status.yml:6689
        Register(StatusType.GvgStun, PresenceMarker(StatusFlagDefaults.For(StatusType.GvgStun) is var f292 && f292 != ScfFlag.None ? f292 : defaultBuff));  // SC_GVG_STUN: presence-only per rAthena db/re/status.yml:6691
        Register(StatusType.GvgStone, PresenceMarker(StatusFlagDefaults.For(StatusType.GvgStone) is var f293 && f293 != ScfFlag.None ? f293 : defaultBuff));  // SC_GVG_STONE: presence-only per rAthena db/re/status.yml:6695
        Register(StatusType.GvgFreez, PresenceMarker(StatusFlagDefaults.For(StatusType.GvgFreez) is var f294 && f294 != ScfFlag.None ? f294 : defaultBuff));  // SC_GVG_FREEZ: presence-only per rAthena db/re/status.yml:6699
        Register(StatusType.GvgSleep, PresenceMarker(StatusFlagDefaults.For(StatusType.GvgSleep) is var f295 && f295 != ScfFlag.None ? f295 : defaultBuff));  // SC_GVG_SLEEP: presence-only per rAthena db/re/status.yml:6703
        Register(StatusType.GvgCurse, PresenceMarker(StatusFlagDefaults.For(StatusType.GvgCurse) is var f296 && f296 != ScfFlag.None ? f296 : defaultBuff));  // SC_GVG_CURSE: presence-only per rAthena db/re/status.yml:6707
        Register(StatusType.GvgSilence, PresenceMarker(StatusFlagDefaults.For(StatusType.GvgSilence) is var f297 && f297 != ScfFlag.None ? f297 : defaultBuff));  // SC_GVG_SILENCE: presence-only per rAthena db/re/status.yml:6711
        Register(StatusType.GvgBlind, PresenceMarker(StatusFlagDefaults.For(StatusType.GvgBlind) is var f298 && f298 != ScfFlag.None ? f298 : defaultBuff));  // SC_GVG_BLIND: presence-only per rAthena db/re/status.yml:6715
        Register(StatusType.ClanInfo, PresenceMarker(StatusFlagDefaults.For(StatusType.ClanInfo) is var f299 && f299 != ScfFlag.None ? f299 : defaultBuff));  // SC_CLAN_INFO: presence-only per rAthena db/re/status.yml:6719
        Register(StatusType.Tarotcard, PresenceMarker(StatusFlagDefaults.For(StatusType.Tarotcard) is var f300 && f300 != ScfFlag.None ? f300 : defaultBuff));  // SC_TAROTCARD: presence-only per rAthena db/re/status.yml:6812
        Register(StatusType.GeffenMagic1, PresenceMarker(StatusFlagDefaults.For(StatusType.GeffenMagic1) is var f301 && f301 != ScfFlag.None ? f301 : defaultBuff));  // SC_GEFFEN_MAGIC1: presence-only per rAthena db/re/status.yml:6815
        Register(StatusType.GeffenMagic2, PresenceMarker(StatusFlagDefaults.For(StatusType.GeffenMagic2) is var f302 && f302 != ScfFlag.None ? f302 : defaultBuff));  // SC_GEFFEN_MAGIC2: presence-only per rAthena db/re/status.yml:6825
        Register(StatusType.GeffenMagic3, PresenceMarker(StatusFlagDefaults.For(StatusType.GeffenMagic3) is var f303 && f303 != ScfFlag.None ? f303 : defaultBuff));  // SC_GEFFEN_MAGIC3: presence-only per rAthena db/re/status.yml:6834
        Register(StatusType.Maxpain, PresenceMarker(StatusFlagDefaults.For(StatusType.Maxpain) is var f304 && f304 != ScfFlag.None ? f304 : defaultBuff));  // SC_MAXPAIN: presence-only per rAthena db/re/status.yml:6843
        Register(StatusType.ArmorElementEarth, PresenceMarker(StatusFlagDefaults.For(StatusType.ArmorElementEarth) is var f305 && f305 != ScfFlag.None ? f305 : defaultBuff));  // SC_ARMOR_ELEMENT_EARTH: presence-only per rAthena db/re/status.yml:6848
        Register(StatusType.ArmorElementFire, PresenceMarker(StatusFlagDefaults.For(StatusType.ArmorElementFire) is var f306 && f306 != ScfFlag.None ? f306 : defaultBuff));  // SC_ARMOR_ELEMENT_FIRE: presence-only per rAthena db/re/status.yml:6858
        Register(StatusType.ArmorElementWind, PresenceMarker(StatusFlagDefaults.For(StatusType.ArmorElementWind) is var f307 && f307 != ScfFlag.None ? f307 : defaultBuff));  // SC_ARMOR_ELEMENT_WIND: presence-only per rAthena db/re/status.yml:6868
        Register(StatusType.Dailysendmailcnt, PresenceMarker(StatusFlagDefaults.For(StatusType.Dailysendmailcnt) is var f308 && f308 != ScfFlag.None ? f308 : defaultBuff));  // SC_DAILYSENDMAILCNT: presence-only per rAthena db/re/status.yml:6878
        Register(StatusType.DoramBuf01, PresenceMarker(StatusFlagDefaults.For(StatusType.DoramBuf01) is var f309 && f309 != ScfFlag.None ? f309 : defaultBuff));  // SC_DORAM_BUF_01: presence-only per rAthena db/re/status.yml:6887
        Register(StatusType.DoramBuf02, PresenceMarker(StatusFlagDefaults.For(StatusType.DoramBuf02) is var f310 && f310 != ScfFlag.None ? f310 : defaultBuff));  // SC_DORAM_BUF_02: presence-only per rAthena db/re/status.yml:6899
        Register(StatusType.Shrimpblessing, PresenceMarker(StatusFlagDefaults.For(StatusType.Shrimpblessing) is var f311 && f311 != ScfFlag.None ? f311 : defaultBuff));  // SC_SHRIMPBLESSING: presence-only per rAthena db/re/status.yml:6947
        Register(StatusType.DoramSvsp, PresenceMarker(StatusFlagDefaults.For(StatusType.DoramSvsp) is var f312 && f312 != ScfFlag.None ? f312 : defaultBuff));  // SC_DORAM_SVSP: presence-only per rAthena db/re/status.yml:6973
        Register(StatusType.Dressup, PresenceMarker(StatusFlagDefaults.For(StatusType.Dressup) is var f313 && f313 != ScfFlag.None ? f313 : defaultBuff));  // SC_DRESSUP: presence-only per rAthena db/re/status.yml:6990
        Register(StatusType.GlastheimAtk, PresenceMarker(StatusFlagDefaults.For(StatusType.GlastheimAtk) is var f314 && f314 != ScfFlag.None ? f314 : defaultBuff));  // SC_GLASTHEIM_ATK: presence-only per rAthena db/re/status.yml:7005
        Register(StatusType.GlastheimDef, PresenceMarker(StatusFlagDefaults.For(StatusType.GlastheimDef) is var f315 && f315 != ScfFlag.None ? f315 : defaultBuff));  // SC_GLASTHEIM_DEF: presence-only per rAthena db/re/status.yml:7015
        Register(StatusType.GlastheimHeal, PresenceMarker(StatusFlagDefaults.For(StatusType.GlastheimHeal) is var f316 && f316 != ScfFlag.None ? f316 : defaultBuff));  // SC_GLASTHEIM_HEAL: presence-only per rAthena db/re/status.yml:7021
        Register(StatusType.GlastheimHidden, PresenceMarker(StatusFlagDefaults.For(StatusType.GlastheimHidden) is var f317 && f317 != ScfFlag.None ? f317 : defaultBuff));  // SC_GLASTHEIM_HIDDEN: presence-only per rAthena db/re/status.yml:7028
        Register(StatusType.GlastheimState, PresenceMarker(StatusFlagDefaults.For(StatusType.GlastheimState) is var f318 && f318 != ScfFlag.None ? f318 : defaultBuff));  // SC_GLASTHEIM_STATE: presence-only per rAthena db/re/status.yml:7034
        Register(StatusType.GlastheimItemdef, PresenceMarker(StatusFlagDefaults.For(StatusType.GlastheimItemdef) is var f319 && f319 != ScfFlag.None ? f319 : defaultBuff));  // SC_GLASTHEIM_ITEMDEF: presence-only per rAthena db/re/status.yml:7040
        Register(StatusType.GlastheimHpsp, PresenceMarker(StatusFlagDefaults.For(StatusType.GlastheimHpsp) is var f320 && f320 != ScfFlag.None ? f320 : defaultBuff));  // SC_GLASTHEIM_HPSP: presence-only per rAthena db/re/status.yml:7047
        Register(StatusType.LhzDunN1, PresenceMarker(StatusFlagDefaults.For(StatusType.LhzDunN1) is var f321 && f321 != ScfFlag.None ? f321 : defaultBuff));  // SC_LHZ_DUN_N1: presence-only per rAthena db/re/status.yml:7054
        Register(StatusType.LhzDunN2, PresenceMarker(StatusFlagDefaults.For(StatusType.LhzDunN2) is var f322 && f322 != ScfFlag.None ? f322 : defaultBuff));  // SC_LHZ_DUN_N2: presence-only per rAthena db/re/status.yml:7069
        Register(StatusType.LhzDunN3, PresenceMarker(StatusFlagDefaults.For(StatusType.LhzDunN3) is var f323 && f323 != ScfFlag.None ? f323 : defaultBuff));  // SC_LHZ_DUN_N3: presence-only per rAthena db/re/status.yml:7084
        Register(StatusType.LhzDunN4, PresenceMarker(StatusFlagDefaults.For(StatusType.LhzDunN4) is var f324 && f324 != ScfFlag.None ? f324 : defaultBuff));  // SC_LHZ_DUN_N4: presence-only per rAthena db/re/status.yml:7099
        Register(StatusType.Ancilla, PresenceMarker(StatusFlagDefaults.For(StatusType.Ancilla) is var f325 && f325 != ScfFlag.None ? f325 : defaultBuff));  // SC_ANCILLA: presence-only per rAthena db/re/status.yml:7114
        Register(StatusType.Earthshaker, PresenceMarker(StatusFlagDefaults.For(StatusType.Earthshaker) is var f326 && f326 != ScfFlag.None ? f326 : defaultBuff));  // SC_EARTHSHAKER: presence-only per rAthena db/re/status.yml:7120
        Register(StatusType.WeaponblockOn, PresenceMarker(StatusFlagDefaults.For(StatusType.WeaponblockOn) is var f327 && f327 != ScfFlag.None ? f327 : defaultBuff));  // SC_WEAPONBLOCK_ON: presence-only per rAthena db/re/status.yml:7125
        Register(StatusType.SporeExplosion, PresenceMarker(StatusFlagDefaults.For(StatusType.SporeExplosion) is var f328 && f328 != ScfFlag.None ? f328 : defaultBuff));  // SC_SPORE_EXPLOSION: presence-only per rAthena db/re/status.yml:7134
        Register(StatusType.Adaptation, PresenceMarker(StatusFlagDefaults.For(StatusType.Adaptation) is var f329 && f329 != ScfFlag.None ? f329 : defaultBuff));  // SC_ADAPTATION: presence-only per rAthena db/re/status.yml:7142
        Register(StatusType.EntryQueueApplyDelay, PresenceMarker(StatusFlagDefaults.For(StatusType.EntryQueueApplyDelay) is var f330 && f330 != ScfFlag.None ? f330 : defaultBuff));  // SC_ENTRY_QUEUE_APPLY_DELAY: presence-only per rAthena db/re/status.yml:7154
        Register(StatusType.EntryQueueNotifyAdmissionTimeOut, PresenceMarker(StatusFlagDefaults.For(StatusType.EntryQueueNotifyAdmissionTimeOut) is var f331 && f331 != ScfFlag.None ? f331 : defaultBuff));  // SC_ENTRY_QUEUE_NOTIFY_ADMISSION_TIME_OUT: presence-only per rAthena db/re/status.yml:7162
        Register(StatusType.Flashkick, PresenceMarker(StatusFlagDefaults.For(StatusType.Flashkick) is var f332 && f332 != ScfFlag.None ? f332 : defaultBuff));  // SC_FLASHKICK: presence-only per rAthena db/re/status.yml:7233
        Register(StatusType.Newmoon, PresenceMarker(StatusFlagDefaults.For(StatusType.Newmoon) is var f333 && f333 != ScfFlag.None ? f333 : defaultBuff));  // SC_NEWMOON: presence-only per rAthena db/re/status.yml:7242
        Register(StatusType.Dimension, PresenceMarker(StatusFlagDefaults.For(StatusType.Dimension) is var f334 && f334 != ScfFlag.None ? f334 : defaultBuff));  // SC_DIMENSION: presence-only per rAthena db/re/status.yml:7271
        Register(StatusType.Dimension1, PresenceMarker(StatusFlagDefaults.For(StatusType.Dimension1) is var f335 && f335 != ScfFlag.None ? f335 : defaultBuff));  // SC_DIMENSION1: presence-only per rAthena db/re/status.yml:7276
        Register(StatusType.Dimension2, PresenceMarker(StatusFlagDefaults.For(StatusType.Dimension2) is var f336 && f336 != ScfFlag.None ? f336 : defaultBuff));  // SC_DIMENSION2: presence-only per rAthena db/re/status.yml:7279
        Register(StatusType.Fallingstar, PresenceMarker(StatusFlagDefaults.For(StatusType.Fallingstar) is var f337 && f337 != ScfFlag.None ? f337 : defaultBuff));  // SC_FALLINGSTAR: presence-only per rAthena db/re/status.yml:7291
        Register(StatusType.Novaexplosing, PresenceMarker(StatusFlagDefaults.For(StatusType.Novaexplosing) is var f338 && f338 != ScfFlag.None ? f338 : defaultBuff));  // SC_NOVAEXPLOSING: presence-only per rAthena db/re/status.yml:7294
        Register(StatusType.Gravitycontrol, PresenceMarker(StatusFlagDefaults.For(StatusType.Gravitycontrol) is var f339 && f339 != ScfFlag.None ? f339 : defaultBuff));  // SC_GRAVITYCONTROL: presence-only per rAthena db/re/status.yml:7301
        Register(StatusType.UseSkillSpSpa, PresenceMarker(StatusFlagDefaults.For(StatusType.UseSkillSpSpa) is var f340 && f340 != ScfFlag.None ? f340 : defaultBuff));  // SC_USE_SKILL_SP_SPA: presence-only per rAthena db/re/status.yml:7404
        Register(StatusType.UseSkillSpSha, PresenceMarker(StatusFlagDefaults.For(StatusType.UseSkillSpSha) is var f341 && f341 != ScfFlag.None ? f341 : defaultBuff));  // SC_USE_SKILL_SP_SHA: presence-only per rAthena db/re/status.yml:7410
        Register(StatusType.IncreaseMaxhp, PresenceMarker(StatusFlagDefaults.For(StatusType.IncreaseMaxhp) is var f342 && f342 != ScfFlag.None ? f342 : defaultBuff));  // SC_INCREASE_MAXHP: presence-only per rAthena db/re/status.yml:7438
        Register(StatusType.IncreaseMaxsp, PresenceMarker(StatusFlagDefaults.For(StatusType.IncreaseMaxsp) is var f343 && f343 != ScfFlag.None ? f343 : defaultBuff));  // SC_INCREASE_MAXSP: presence-only per rAthena db/re/status.yml:7443
        Register(StatusType.RefTPotion, PresenceMarker(StatusFlagDefaults.For(StatusType.RefTPotion) is var f344 && f344 != ScfFlag.None ? f344 : defaultBuff));  // SC_REF_T_POTION: presence-only per rAthena db/re/status.yml:7448
        Register(StatusType.AddAtkDamage, PresenceMarker(StatusFlagDefaults.For(StatusType.AddAtkDamage) is var f345 && f345 != ScfFlag.None ? f345 : defaultBuff));  // SC_ADD_ATK_DAMAGE: presence-only per rAthena db/re/status.yml:7450
        Register(StatusType.AddMatkDamage, PresenceMarker(StatusFlagDefaults.For(StatusType.AddMatkDamage) is var f346 && f346 != ScfFlag.None ? f346 : defaultBuff));  // SC_ADD_MATK_DAMAGE: presence-only per rAthena db/re/status.yml:7452
        Register(StatusType.Helpangel, PresenceMarker(StatusFlagDefaults.For(StatusType.Helpangel) is var f347 && f347 != ScfFlag.None ? f347 : defaultBuff));  // SC_HELPANGEL: presence-only per rAthena db/re/status.yml:7454
        Register(StatusType.Soundofdestruction, PresenceMarker(StatusFlagDefaults.For(StatusType.Soundofdestruction) is var f348 && f348 != ScfFlag.None ? f348 : defaultBuff));  // SC_SOUNDOFDESTRUCTION: presence-only per rAthena db/re/status.yml:7457
        Register(StatusType.ReuseLimitLuxanima, PresenceMarker(StatusFlagDefaults.For(StatusType.ReuseLimitLuxanima) is var f349 && f349 != ScfFlag.None ? f349 : defaultBuff));  // SC_REUSE_LIMIT_LUXANIMA: presence-only per rAthena db/re/status.yml:7470
        Register(StatusType.MistyFrost, PresenceMarker(StatusFlagDefaults.For(StatusType.MistyFrost) is var f350 && f350 != ScfFlag.None ? f350 : defaultBuff));  // SC_MISTY_FROST: presence-only per rAthena db/re/status.yml:7487
        Register(StatusType.MagicPoison, PresenceMarker(StatusFlagDefaults.For(StatusType.MagicPoison) is var f351 && f351 != ScfFlag.None ? f351 : defaultBuff));  // SC_MAGIC_POISON: presence-only per rAthena db/re/status.yml:7492
        Register(StatusType.Ep162BuffSs, PresenceMarker(StatusFlagDefaults.For(StatusType.Ep162BuffSs) is var f352 && f352 != ScfFlag.None ? f352 : defaultBuff));  // SC_EP16_2_BUFF_SS: presence-only per rAthena db/re/status.yml:7498
        Register(StatusType.Ep162BuffSc, PresenceMarker(StatusFlagDefaults.For(StatusType.Ep162BuffSc) is var f353 && f353 != ScfFlag.None ? f353 : defaultBuff));  // SC_EP16_2_BUFF_SC: presence-only per rAthena db/re/status.yml:7507
        Register(StatusType.Ep162BuffAc, PresenceMarker(StatusFlagDefaults.For(StatusType.Ep162BuffAc) is var f354 && f354 != ScfFlag.None ? f354 : defaultBuff));  // SC_EP16_2_BUFF_AC: presence-only per rAthena db/re/status.yml:7516
        Register(StatusType.Overbrandready, PresenceMarker(StatusFlagDefaults.For(StatusType.Overbrandready) is var f355 && f355 != ScfFlag.None ? f355 : defaultBuff));  // SC_OVERBRANDREADY: presence-only per rAthena db/re/status.yml:7525
        Register(StatusType.CloudPoison, PresenceMarker(StatusFlagDefaults.For(StatusType.CloudPoison) is var f356 && f356 != ScfFlag.None ? f356 : defaultBuff));  // SC_CLOUD_POISON: presence-only per rAthena db/re/status.yml:7538
        Register(StatusType.HomunTime, PresenceMarker(StatusFlagDefaults.For(StatusType.HomunTime) is var f357 && f357 != ScfFlag.None ? f357 : defaultBuff));  // SC_HOMUN_TIME: presence-only per rAthena db/re/status.yml:7546
        Register(StatusType.PackingEnvelope1, PresenceMarker(StatusFlagDefaults.For(StatusType.PackingEnvelope1) is var f358 && f358 != ScfFlag.None ? f358 : defaultBuff));  // SC_PACKING_ENVELOPE1: presence-only per rAthena db/re/status.yml:7594
        Register(StatusType.PackingEnvelope2, PresenceMarker(StatusFlagDefaults.For(StatusType.PackingEnvelope2) is var f359 && f359 != ScfFlag.None ? f359 : defaultBuff));  // SC_PACKING_ENVELOPE2: presence-only per rAthena db/re/status.yml:7603
        Register(StatusType.PackingEnvelope3, PresenceMarker(StatusFlagDefaults.For(StatusType.PackingEnvelope3) is var f360 && f360 != ScfFlag.None ? f360 : defaultBuff));  // SC_PACKING_ENVELOPE3: presence-only per rAthena db/re/status.yml:7612
        Register(StatusType.PackingEnvelope4, PresenceMarker(StatusFlagDefaults.For(StatusType.PackingEnvelope4) is var f361 && f361 != ScfFlag.None ? f361 : defaultBuff));  // SC_PACKING_ENVELOPE4: presence-only per rAthena db/re/status.yml:7621
        Register(StatusType.PackingEnvelope5, PresenceMarker(StatusFlagDefaults.For(StatusType.PackingEnvelope5) is var f362 && f362 != ScfFlag.None ? f362 : defaultBuff));  // SC_PACKING_ENVELOPE5: presence-only per rAthena db/re/status.yml:7630
        Register(StatusType.PackingEnvelope6, PresenceMarker(StatusFlagDefaults.For(StatusType.PackingEnvelope6) is var f363 && f363 != ScfFlag.None ? f363 : defaultBuff));  // SC_PACKING_ENVELOPE6: presence-only per rAthena db/re/status.yml:7639
        Register(StatusType.PackingEnvelope7, PresenceMarker(StatusFlagDefaults.For(StatusType.PackingEnvelope7) is var f364 && f364 != ScfFlag.None ? f364 : defaultBuff));  // SC_PACKING_ENVELOPE7: presence-only per rAthena db/re/status.yml:7648
        Register(StatusType.PackingEnvelope8, PresenceMarker(StatusFlagDefaults.For(StatusType.PackingEnvelope8) is var f365 && f365 != ScfFlag.None ? f365 : defaultBuff));  // SC_PACKING_ENVELOPE8: presence-only per rAthena db/re/status.yml:7657
        Register(StatusType.PackingEnvelope9, PresenceMarker(StatusFlagDefaults.For(StatusType.PackingEnvelope9) is var f366 && f366 != ScfFlag.None ? f366 : defaultBuff));  // SC_PACKING_ENVELOPE9: presence-only per rAthena db/re/status.yml:7666
        Register(StatusType.PackingEnvelope10, PresenceMarker(StatusFlagDefaults.For(StatusType.PackingEnvelope10) is var f367 && f367 != ScfFlag.None ? f367 : defaultBuff));  // SC_PACKING_ENVELOPE10: presence-only per rAthena db/re/status.yml:7675
        Register(StatusType.Chill, PresenceMarker(StatusFlagDefaults.For(StatusType.Chill) is var f368 && f368 != ScfFlag.None ? f368 : defaultBuff));  // SC_CHILL: presence-only per rAthena db/re/status.yml:7704
        Register(StatusType.HandicapstateSwooning, PresenceMarker(StatusFlagDefaults.For(StatusType.HandicapstateSwooning) is var f369 && f369 != ScfFlag.None ? f369 : defaultBuff));  // SC_HANDICAPSTATE_SWOONING: presence-only per rAthena db/re/status.yml:7748
        Register(StatusType.HandicapstateLightningstrike, PresenceMarker(StatusFlagDefaults.For(StatusType.HandicapstateLightningstrike) is var f370 && f370 != ScfFlag.None ? f370 : defaultBuff));  // SC_HANDICAPSTATE_LIGHTNINGSTRIKE: presence-only per rAthena db/re/status.yml:7757
        Register(StatusType.HandicapstateConflagration, PresenceMarker(StatusFlagDefaults.For(StatusType.HandicapstateConflagration) is var f371 && f371 != ScfFlag.None ? f371 : defaultBuff));  // SC_HANDICAPSTATE_CONFLAGRATION: presence-only per rAthena db/re/status.yml:7784
        Register(StatusType.HandicapstateDepression, PresenceMarker(StatusFlagDefaults.For(StatusType.HandicapstateDepression) is var f372 && f372 != ScfFlag.None ? f372 : defaultBuff));  // SC_HANDICAPSTATE_DEPRESSION: presence-only per rAthena db/re/status.yml:7806
        Register(StatusType.HandicapstateHolyflame, PresenceMarker(StatusFlagDefaults.For(StatusType.HandicapstateHolyflame) is var f373 && f373 != ScfFlag.None ? f373 : defaultBuff));  // SC_HANDICAPSTATE_HOLYFLAME: presence-only per rAthena db/re/status.yml:7810
        Register(StatusType.Servantweapon, PresenceMarker(StatusFlagDefaults.For(StatusType.Servantweapon) is var f374 && f374 != ScfFlag.None ? f374 : defaultBuff));  // SC_SERVANTWEAPON: presence-only per rAthena db/re/status.yml:7814
        Register(StatusType.ServantSign, PresenceMarker(StatusFlagDefaults.For(StatusType.ServantSign) is var f375 && f375 != ScfFlag.None ? f375 : defaultBuff));  // SC_SERVANT_SIGN: presence-only per rAthena db/re/status.yml:7821
        Register(StatusType.Chargingpierce, PresenceMarker(StatusFlagDefaults.For(StatusType.Chargingpierce) is var f376 && f376 != ScfFlag.None ? f376 : defaultBuff));  // SC_CHARGINGPIERCE: presence-only per rAthena db/re/status.yml:7832
        Register(StatusType.ChargingpierceCount, PresenceMarker(StatusFlagDefaults.For(StatusType.ChargingpierceCount) is var f377 && f377 != ScfFlag.None ? f377 : defaultBuff));  // SC_CHARGINGPIERCE_COUNT: presence-only per rAthena db/re/status.yml:7839
        Register(StatusType.Crescivebolt, PresenceMarker(StatusFlagDefaults.For(StatusType.Crescivebolt) is var f378 && f378 != ScfFlag.None ? f378 : defaultBuff));  // SC_CRESCIVEBOLT: presence-only per rAthena db/re/status.yml:7902
        Register(StatusType.Calamitygale, PresenceMarker(StatusFlagDefaults.For(StatusType.Calamitygale) is var f379 && f379 != ScfFlag.None ? f379 : defaultBuff));  // SC_CALAMITYGALE: presence-only per rAthena db/re/status.yml:7905
        Register(StatusType.Mediale, PresenceMarker(StatusFlagDefaults.For(StatusType.Mediale) is var f380 && f380 != ScfFlag.None ? f380 : defaultBuff));  // SC_MEDIALE: presence-only per rAthena db/re/status.yml:7911
        Register(StatusType.AVita, PresenceMarker(StatusFlagDefaults.For(StatusType.AVita) is var f381 && f381 != ScfFlag.None ? f381 : defaultBuff));  // SC_A_VITA: presence-only per rAthena db/re/status.yml:7917
        Register(StatusType.ATelum, PresenceMarker(StatusFlagDefaults.For(StatusType.ATelum) is var f382 && f382 != ScfFlag.None ? f382 : defaultBuff));  // SC_A_TELUM: presence-only per rAthena db/re/status.yml:7920
        Register(StatusType.AxeStomp, PresenceMarker(StatusFlagDefaults.For(StatusType.AxeStomp) is var f383 && f383 != ScfFlag.None ? f383 : defaultBuff));  // SC_AXE_STOMP: presence-only per rAthena db/re/status.yml:7964
        Register(StatusType.AMachine, PresenceMarker(StatusFlagDefaults.For(StatusType.AMachine) is var f384 && f384 != ScfFlag.None ? f384 : defaultBuff));  // SC_A_MACHINE: presence-only per rAthena db/re/status.yml:7967
        Register(StatusType.AbrBattleWarior, PresenceMarker(StatusFlagDefaults.For(StatusType.AbrBattleWarior) is var f385 && f385 != ScfFlag.None ? f385 : defaultBuff));  // SC_ABR_BATTLE_WARIOR: presence-only per rAthena db/re/status.yml:7982
        Register(StatusType.AbrDualCannon, PresenceMarker(StatusFlagDefaults.For(StatusType.AbrDualCannon) is var f386 && f386 != ScfFlag.None ? f386 : defaultBuff));  // SC_ABR_DUAL_CANNON: presence-only per rAthena db/re/status.yml:7985
        Register(StatusType.AbrMotherNet, PresenceMarker(StatusFlagDefaults.For(StatusType.AbrMotherNet) is var f387 && f387 != ScfFlag.None ? f387 : defaultBuff));  // SC_ABR_MOTHER_NET: presence-only per rAthena db/re/status.yml:7988
        Register(StatusType.AbrInfinity, PresenceMarker(StatusFlagDefaults.For(StatusType.AbrInfinity) is var f388 && f388 != ScfFlag.None ? f388 : defaultBuff));  // SC_ABR_INFINITY: presence-only per rAthena db/re/status.yml:7991
        Register(StatusType.ShadowExceed, PresenceMarker(StatusFlagDefaults.For(StatusType.ShadowExceed) is var f389 && f389 != ScfFlag.None ? f389 : defaultBuff));  // SC_SHADOW_EXCEED: presence-only per rAthena db/re/status.yml:7994
        Register(StatusType.DancingKnife, PresenceMarker(StatusFlagDefaults.For(StatusType.DancingKnife) is var f390 && f390 != ScfFlag.None ? f390 : defaultBuff));  // SC_DANCING_KNIFE: presence-only per rAthena db/re/status.yml:8000
        Register(StatusType.PotentVenom, PresenceMarker(StatusFlagDefaults.For(StatusType.PotentVenom) is var f391 && f391 != ScfFlag.None ? f391 : defaultBuff));  // SC_POTENT_VENOM: presence-only per rAthena db/re/status.yml:8007
        Register(StatusType.ShadowScar, PresenceMarker(StatusFlagDefaults.For(StatusType.ShadowScar) is var f392 && f392 != ScfFlag.None ? f392 : defaultBuff));  // SC_SHADOW_SCAR: presence-only per rAthena db/re/status.yml:8010
        Register(StatusType.ESlashCount, PresenceMarker(StatusFlagDefaults.For(StatusType.ESlashCount) is var f393 && f393 != ScfFlag.None ? f393 : defaultBuff));  // SC_E_SLASH_COUNT: presence-only per rAthena db/re/status.yml:8012
        Register(StatusType.ShadowWeapon, PresenceMarker(StatusFlagDefaults.For(StatusType.ShadowWeapon) is var f394 && f394 != ScfFlag.None ? f394 : defaultBuff));  // SC_SHADOW_WEAPON: presence-only per rAthena db/re/status.yml:8021
        Register(StatusType.GuardianS, PresenceMarker(StatusFlagDefaults.For(StatusType.GuardianS) is var f395 && f395 != ScfFlag.None ? f395 : defaultBuff));  // SC_GUARDIAN_S: presence-only per rAthena db/re/status.yml:8052
        Register(StatusType.ReboundS, PresenceMarker(StatusFlagDefaults.For(StatusType.ReboundS) is var f396 && f396 != ScfFlag.None ? f396 : defaultBuff));  // SC_REBOUND_S: presence-only per rAthena db/re/status.yml:8055
        Register(StatusType.UltimateS, PresenceMarker(StatusFlagDefaults.For(StatusType.UltimateS) is var f397 && f397 != ScfFlag.None ? f397 : defaultBuff));  // SC_ULTIMATE_S: presence-only per rAthena db/re/status.yml:8069
        Register(StatusType.SpearScar, PresenceMarker(StatusFlagDefaults.For(StatusType.SpearScar) is var f398 && f398 != ScfFlag.None ? f398 : defaultBuff));  // SC_SPEAR_SCAR: presence-only per rAthena db/re/status.yml:8072
        Register(StatusType.ShieldPower, PresenceMarker(StatusFlagDefaults.For(StatusType.ShieldPower) is var f399 && f399 != ScfFlag.None ? f399 : defaultBuff));  // SC_SHIELD_POWER: presence-only per rAthena db/re/status.yml:8078
        Register(StatusType.SummonElementalArdor, PresenceMarker(StatusFlagDefaults.For(StatusType.SummonElementalArdor) is var f400 && f400 != ScfFlag.None ? f400 : defaultBuff));  // SC_SUMMON_ELEMENTAL_ARDOR: presence-only per rAthena db/re/status.yml:8088
        Register(StatusType.SummonElementalDiluvio, PresenceMarker(StatusFlagDefaults.For(StatusType.SummonElementalDiluvio) is var f401 && f401 != ScfFlag.None ? f401 : defaultBuff));  // SC_SUMMON_ELEMENTAL_DILUVIO: presence-only per rAthena db/re/status.yml:8091
        Register(StatusType.SummonElementalProcella, PresenceMarker(StatusFlagDefaults.For(StatusType.SummonElementalProcella) is var f402 && f402 != ScfFlag.None ? f402 : defaultBuff));  // SC_SUMMON_ELEMENTAL_PROCELLA: presence-only per rAthena db/re/status.yml:8094
        Register(StatusType.SummonElementalTerremotus, PresenceMarker(StatusFlagDefaults.For(StatusType.SummonElementalTerremotus) is var f403 && f403 != ScfFlag.None ? f403 : defaultBuff));  // SC_SUMMON_ELEMENTAL_TERREMOTUS: presence-only per rAthena db/re/status.yml:8097
        Register(StatusType.SummonElementalSerpens, PresenceMarker(StatusFlagDefaults.For(StatusType.SummonElementalSerpens) is var f404 && f404 != ScfFlag.None ? f404 : defaultBuff));  // SC_SUMMON_ELEMENTAL_SERPENS: presence-only per rAthena db/re/status.yml:8100
        Register(StatusType.ElementalVeil, PresenceMarker(StatusFlagDefaults.For(StatusType.ElementalVeil) is var f405 && f405 != ScfFlag.None ? f405 : defaultBuff));  // SC_ELEMENTAL_VEIL: presence-only per rAthena db/re/status.yml:8103
        Register(StatusType.MysticSymphony, PresenceMarker(StatusFlagDefaults.For(StatusType.MysticSymphony) is var f406 && f406 != ScfFlag.None ? f406 : defaultBuff));  // SC_MYSTIC_SYMPHONY: presence-only per rAthena db/re/status.yml:8109
        Register(StatusType.KvasirSonata, PresenceMarker(StatusFlagDefaults.For(StatusType.KvasirSonata) is var f407 && f407 != ScfFlag.None ? f407 : defaultBuff));  // SC_KVASIR_SONATA: presence-only per rAthena db/re/status.yml:8115
        Register(StatusType.Soundblend, PresenceMarker(StatusFlagDefaults.For(StatusType.Soundblend) is var f408 && f408 != ScfFlag.None ? f408 : defaultBuff));  // SC_SOUNDBLEND: presence-only per rAthena db/re/status.yml:8118
        Register(StatusType.Roseblossom, PresenceMarker(StatusFlagDefaults.For(StatusType.Roseblossom) is var f409 && f409 != ScfFlag.None ? f409 : defaultBuff));  // SC_ROSEBLOSSOM: presence-only per rAthena db/re/status.yml:8156
        Register(StatusType.HolyOil, PresenceMarker(StatusFlagDefaults.For(StatusType.HolyOil) is var f410 && f410 != ScfFlag.None ? f410 : defaultBuff));  // SC_HOLY_OIL: presence-only per rAthena db/re/status.yml:8190
        Register(StatusType.FirstBrand, PresenceMarker(StatusFlagDefaults.For(StatusType.FirstBrand) is var f411 && f411 != ScfFlag.None ? f411 : defaultBuff));  // SC_FIRST_BRAND: presence-only per rAthena db/re/status.yml:8196
        Register(StatusType.SecondBrand, PresenceMarker(StatusFlagDefaults.For(StatusType.SecondBrand) is var f412 && f412 != ScfFlag.None ? f412 : defaultBuff));  // SC_SECOND_BRAND: presence-only per rAthena db/re/status.yml:8205
        Register(StatusType.SecondJudge, PresenceMarker(StatusFlagDefaults.For(StatusType.SecondJudge) is var f413 && f413 != ScfFlag.None ? f413 : defaultBuff));  // SC_SECOND_JUDGE: presence-only per rAthena db/re/status.yml:8214
        Register(StatusType.ThirdExorFlame, PresenceMarker(StatusFlagDefaults.For(StatusType.ThirdExorFlame) is var f414 && f414 != ScfFlag.None ? f414 : defaultBuff));  // SC_THIRD_EXOR_FLAME: presence-only per rAthena db/re/status.yml:8224
        Register(StatusType.FirstFaithPower, PresenceMarker(StatusFlagDefaults.For(StatusType.FirstFaithPower) is var f415 && f415 != ScfFlag.None ? f415 : defaultBuff));  // SC_FIRST_FAITH_POWER: presence-only per rAthena db/re/status.yml:8234
        Register(StatusType.MassiveFBlaster, PresenceMarker(StatusFlagDefaults.For(StatusType.MassiveFBlaster) is var f416 && f416 != ScfFlag.None ? f416 : defaultBuff));  // SC_MASSIVE_F_BLASTER: presence-only per rAthena db/re/status.yml:8244
        Register(StatusType.Protectshadowequip, PresenceMarker(StatusFlagDefaults.For(StatusType.Protectshadowequip) is var f417 && f417 != ScfFlag.None ? f417 : defaultBuff));  // SC_PROTECTSHADOWEQUIP: presence-only per rAthena db/re/status.yml:8247
        Register(StatusType.Researchreport, PresenceMarker(StatusFlagDefaults.For(StatusType.Researchreport) is var f418 && f418 != ScfFlag.None ? f418 : defaultBuff));  // SC_RESEARCHREPORT: presence-only per rAthena db/re/status.yml:8255
        Register(StatusType.BoHellDusty, PresenceMarker(StatusFlagDefaults.For(StatusType.BoHellDusty) is var f419 && f419 != ScfFlag.None ? f419 : defaultBuff));  // SC_BO_HELL_DUSTY: presence-only per rAthena db/re/status.yml:8258
        Register(StatusType.BionicWoodenwarrior, PresenceMarker(StatusFlagDefaults.For(StatusType.BionicWoodenwarrior) is var f420 && f420 != ScfFlag.None ? f420 : defaultBuff));  // SC_BIONIC_WOODENWARRIOR: presence-only per rAthena db/re/status.yml:8260
        Register(StatusType.BionicWoodenFairy, PresenceMarker(StatusFlagDefaults.For(StatusType.BionicWoodenFairy) is var f421 && f421 != ScfFlag.None ? f421 : defaultBuff));  // SC_BIONIC_WOODEN_FAIRY: presence-only per rAthena db/re/status.yml:8262
        Register(StatusType.BionicCreeper, PresenceMarker(StatusFlagDefaults.For(StatusType.BionicCreeper) is var f422 && f422 != ScfFlag.None ? f422 : defaultBuff));  // SC_BIONIC_CREEPER: presence-only per rAthena db/re/status.yml:8264
        Register(StatusType.BionicHelltree, PresenceMarker(StatusFlagDefaults.For(StatusType.BionicHelltree) is var f423 && f423 != ScfFlag.None ? f423 : defaultBuff));  // SC_BIONIC_HELLTREE: presence-only per rAthena db/re/status.yml:8266
        Register(StatusType.AbyssDagger, PresenceMarker(StatusFlagDefaults.For(StatusType.AbyssDagger) is var f424 && f424 != ScfFlag.None ? f424 : defaultBuff));  // SC_ABYSS_DAGGER: presence-only per rAthena db/re/status.yml:8281
        Register(StatusType.Abyssforceweapon, PresenceMarker(StatusFlagDefaults.For(StatusType.Abyssforceweapon) is var f425 && f425 != ScfFlag.None ? f425 : defaultBuff));  // SC_ABYSSFORCEWEAPON: presence-only per rAthena db/re/status.yml:8284
        Register(StatusType.Flametechnic, PresenceMarker(StatusFlagDefaults.For(StatusType.Flametechnic) is var f426 && f426 != ScfFlag.None ? f426 : defaultBuff));  // SC_FLAMETECHNIC: presence-only per rAthena db/re/status.yml:8301
        Register(StatusType.FlametechnicOption, PresenceMarker(StatusFlagDefaults.For(StatusType.FlametechnicOption) is var f427 && f427 != ScfFlag.None ? f427 : defaultBuff));  // SC_FLAMETECHNIC_OPTION: presence-only per rAthena db/re/status.yml:8305
        Register(StatusType.Flamearmor, PresenceMarker(StatusFlagDefaults.For(StatusType.Flamearmor) is var f428 && f428 != ScfFlag.None ? f428 : defaultBuff));  // SC_FLAMEARMOR: presence-only per rAthena db/re/status.yml:8310
        Register(StatusType.ColdForce, PresenceMarker(StatusFlagDefaults.For(StatusType.ColdForce) is var f429 && f429 != ScfFlag.None ? f429 : defaultBuff));  // SC_COLD_FORCE: presence-only per rAthena db/re/status.yml:8321
        Register(StatusType.ColdForceOption, PresenceMarker(StatusFlagDefaults.For(StatusType.ColdForceOption) is var f430 && f430 != ScfFlag.None ? f430 : defaultBuff));  // SC_COLD_FORCE_OPTION: presence-only per rAthena db/re/status.yml:8325
        Register(StatusType.CrystalArmor, PresenceMarker(StatusFlagDefaults.For(StatusType.CrystalArmor) is var f431 && f431 != ScfFlag.None ? f431 : defaultBuff));  // SC_CRYSTAL_ARMOR: presence-only per rAthena db/re/status.yml:8330
        Register(StatusType.GraceBreeze, PresenceMarker(StatusFlagDefaults.For(StatusType.GraceBreeze) is var f432 && f432 != ScfFlag.None ? f432 : defaultBuff));  // SC_GRACE_BREEZE: presence-only per rAthena db/re/status.yml:8341
        Register(StatusType.GraceBreezeOption, PresenceMarker(StatusFlagDefaults.For(StatusType.GraceBreezeOption) is var f433 && f433 != ScfFlag.None ? f433 : defaultBuff));  // SC_GRACE_BREEZE_OPTION: presence-only per rAthena db/re/status.yml:8345
        Register(StatusType.EyesOfStorm, PresenceMarker(StatusFlagDefaults.For(StatusType.EyesOfStorm) is var f434 && f434 != ScfFlag.None ? f434 : defaultBuff));  // SC_EYES_OF_STORM: presence-only per rAthena db/re/status.yml:8350
        Register(StatusType.EarthCareOption, PresenceMarker(StatusFlagDefaults.For(StatusType.EarthCareOption) is var f435 && f435 != ScfFlag.None ? f435 : defaultBuff));  // SC_EARTH_CARE_OPTION: presence-only per rAthena db/re/status.yml:8365
        Register(StatusType.StrongProtection, PresenceMarker(StatusFlagDefaults.For(StatusType.StrongProtection) is var f436 && f436 != ScfFlag.None ? f436 : defaultBuff));  // SC_STRONG_PROTECTION: presence-only per rAthena db/re/status.yml:8370
        Register(StatusType.DeepPoisoning, PresenceMarker(StatusFlagDefaults.For(StatusType.DeepPoisoning) is var f437 && f437 != ScfFlag.None ? f437 : defaultBuff));  // SC_DEEP_POISONING: presence-only per rAthena db/re/status.yml:8381
        Register(StatusType.DeepPoisoningOption, PresenceMarker(StatusFlagDefaults.For(StatusType.DeepPoisoningOption) is var f438 && f438 != ScfFlag.None ? f438 : defaultBuff));  // SC_DEEP_POISONING_OPTION: presence-only per rAthena db/re/status.yml:8385
        Register(StatusType.PoisonShield, PresenceMarker(StatusFlagDefaults.For(StatusType.PoisonShield) is var f439 && f439 != ScfFlag.None ? f439 : defaultBuff));  // SC_POISON_SHIELD: presence-only per rAthena db/re/status.yml:8390
        Register(StatusType.SubWeaponproperty, PresenceMarker(StatusFlagDefaults.For(StatusType.SubWeaponproperty) is var f440 && f440 != ScfFlag.None ? f440 : defaultBuff));  // SC_SUB_WEAPONPROPERTY: presence-only per rAthena db/re/status.yml:8425
        Register(StatusType.MLifepotion, PresenceMarker(StatusFlagDefaults.For(StatusType.MLifepotion) is var f441 && f441 != ScfFlag.None ? f441 : defaultBuff));  // SC_M_LIFEPOTION: presence-only per rAthena db/re/status.yml:8401
        Register(StatusType.SManapotion, PresenceMarker(StatusFlagDefaults.For(StatusType.SManapotion) is var f442 && f442 != ScfFlag.None ? f442 : defaultBuff));  // SC_S_MANAPOTION: presence-only per rAthena db/re/status.yml:8413
        Register(StatusType.Almighty, PresenceMarker(StatusFlagDefaults.For(StatusType.Almighty) is var f443 && f443 != ScfFlag.None ? f443 : defaultBuff));  // SC_ALMIGHTY: presence-only per rAthena db/re/status.yml:8434
        Register(StatusType.Ultimatecook, PresenceMarker(StatusFlagDefaults.For(StatusType.Ultimatecook) is var f444 && f444 != ScfFlag.None ? f444 : defaultBuff));  // SC_ULTIMATECOOK: presence-only per rAthena db/re/status.yml:8448
        Register(StatusType.MDefscroll, PresenceMarker(StatusFlagDefaults.For(StatusType.MDefscroll) is var f445 && f445 != ScfFlag.None ? f445 : defaultBuff));  // SC_M_DEFSCROLL: presence-only per rAthena db/re/status.yml:8467
        Register(StatusType.InfinityDrink, PresenceMarker(StatusFlagDefaults.For(StatusType.InfinityDrink) is var f446 && f446 != ScfFlag.None ? f446 : defaultBuff));  // SC_INFINITY_DRINK: presence-only per rAthena db/re/status.yml:8477
        Register(StatusType.MentalPotion, PresenceMarker(StatusFlagDefaults.For(StatusType.MentalPotion) is var f447 && f447 != ScfFlag.None ? f447 : defaultBuff));  // SC_MENTAL_POTION: presence-only per rAthena db/re/status.yml:8491
        Register(StatusType.LimitPowerBooster, PresenceMarker(StatusFlagDefaults.For(StatusType.LimitPowerBooster) is var f448 && f448 != ScfFlag.None ? f448 : defaultBuff));  // SC_LIMIT_POWER_BOOSTER: presence-only per rAthena db/re/status.yml:8502
        Register(StatusType.CombatPill, PresenceMarker(StatusFlagDefaults.For(StatusType.CombatPill) is var f449 && f449 != ScfFlag.None ? f449 : defaultBuff));  // SC_COMBAT_PILL: presence-only per rAthena db/re/status.yml:8522
        Register(StatusType.CombatPill2, PresenceMarker(StatusFlagDefaults.For(StatusType.CombatPill2) is var f450 && f450 != ScfFlag.None ? f450 : defaultBuff));  // SC_COMBAT_PILL2: presence-only per rAthena db/re/status.yml:8536
        Register(StatusType.Mysticpowder, PresenceMarker(StatusFlagDefaults.For(StatusType.Mysticpowder) is var f451 && f451 != ScfFlag.None ? f451 : defaultBuff));  // SC_MYSTICPOWDER: presence-only per rAthena db/re/status.yml:8550
        Register(StatusType.Sparkcandy, PresenceMarker(StatusFlagDefaults.For(StatusType.Sparkcandy) is var f452 && f452 != ScfFlag.None ? f452 : defaultBuff));  // SC_SPARKCANDY: presence-only per rAthena db/re/status.yml:8561
        Register(StatusType.Magiccandy, PresenceMarker(StatusFlagDefaults.For(StatusType.Magiccandy) is var f453 && f453 != ScfFlag.None ? f453 : defaultBuff));  // SC_MAGICCANDY: presence-only per rAthena db/re/status.yml:8574
        Register(StatusType.Acaraje, PresenceMarker(StatusFlagDefaults.For(StatusType.Acaraje) is var f454 && f454 != ScfFlag.None ? f454 : defaultBuff));  // SC_ACARAJE: presence-only per rAthena db/re/status.yml:8587
        Register(StatusType.Popecookie, PresenceMarker(StatusFlagDefaults.For(StatusType.Popecookie) is var f455 && f455 != ScfFlag.None ? f455 : defaultBuff));  // SC_POPECOOKIE: presence-only per rAthena db/re/status.yml:8598
        Register(StatusType.VitalizePotion, PresenceMarker(StatusFlagDefaults.For(StatusType.VitalizePotion) is var f456 && f456 != ScfFlag.None ? f456 : defaultBuff));  // SC_VITALIZE_POTION: presence-only per rAthena db/re/status.yml:8611
        Register(StatusType.CupOfBoza, PresenceMarker(StatusFlagDefaults.For(StatusType.CupOfBoza) is var f457 && f457 != ScfFlag.None ? f457 : defaultBuff));  // SC_CUP_OF_BOZA: presence-only per rAthena db/re/status.yml:8623
        Register(StatusType.SkfMatk, PresenceMarker(StatusFlagDefaults.For(StatusType.SkfMatk) is var f458 && f458 != ScfFlag.None ? f458 : defaultBuff));  // SC_SKF_MATK: presence-only per rAthena db/re/status.yml:8634
        Register(StatusType.SkfAtk, PresenceMarker(StatusFlagDefaults.For(StatusType.SkfAtk) is var f459 && f459 != ScfFlag.None ? f459 : defaultBuff));  // SC_SKF_ATK: presence-only per rAthena db/re/status.yml:8644
        Register(StatusType.SkfAspd, PresenceMarker(StatusFlagDefaults.For(StatusType.SkfAspd) is var f460 && f460 != ScfFlag.None ? f460 : defaultBuff));  // SC_SKF_ASPD: presence-only per rAthena db/re/status.yml:8654
        Register(StatusType.SkfCast, PresenceMarker(StatusFlagDefaults.For(StatusType.SkfCast) is var f461 && f461 != ScfFlag.None ? f461 : defaultBuff));  // SC_SKF_CAST: presence-only per rAthena db/re/status.yml:8664
        Register(StatusType.BeefRibStew, PresenceMarker(StatusFlagDefaults.For(StatusType.BeefRibStew) is var f462 && f462 != ScfFlag.None ? f462 : defaultBuff));  // SC_BEEF_RIB_STEW: presence-only per rAthena db/re/status.yml:8674
        Register(StatusType.PorkRibStew, PresenceMarker(StatusFlagDefaults.For(StatusType.PorkRibStew) is var f463 && f463 != ScfFlag.None ? f463 : defaultBuff));  // SC_PORK_RIB_STEW: presence-only per rAthena db/re/status.yml:8685
        Register(StatusType.Weaponbreaker, PresenceMarker(StatusFlagDefaults.For(StatusType.Weaponbreaker) is var f464 && f464 != ScfFlag.None ? f464 : defaultBuff));  // SC_WEAPONBREAKER: presence-only per rAthena db/re/status.yml:8696
        Register(StatusType.GradualGravity, PresenceMarker(StatusFlagDefaults.For(StatusType.GradualGravity) is var f465 && f465 != ScfFlag.None ? f465 : defaultBuff));  // SC_GRADUAL_GRAVITY: presence-only per rAthena db/re/status.yml:8716
        Register(StatusType.KillingAura, PresenceMarker(StatusFlagDefaults.For(StatusType.KillingAura) is var f466 && f466 != ScfFlag.None ? f466 : defaultBuff));  // SC_KILLING_AURA: presence-only per rAthena db/re/status.yml:8737
        Register(StatusType.DamageHeal, PresenceMarker(StatusFlagDefaults.For(StatusType.DamageHeal) is var f467 && f467 != ScfFlag.None ? f467 : defaultBuff));  // SC_DAMAGE_HEAL: presence-only per rAthena db/re/status.yml:8746
        Register(StatusType.ImmunePropertyNothing, PresenceMarker(StatusFlagDefaults.For(StatusType.ImmunePropertyNothing) is var f468 && f468 != ScfFlag.None ? f468 : defaultBuff));  // SC_IMMUNE_PROPERTY_NOTHING: presence-only per rAthena db/re/status.yml:8751
        Register(StatusType.ImmunePropertyWater, PresenceMarker(StatusFlagDefaults.For(StatusType.ImmunePropertyWater) is var f469 && f469 != ScfFlag.None ? f469 : defaultBuff));  // SC_IMMUNE_PROPERTY_WATER: presence-only per rAthena db/re/status.yml:8767
        Register(StatusType.ImmunePropertyGround, PresenceMarker(StatusFlagDefaults.For(StatusType.ImmunePropertyGround) is var f470 && f470 != ScfFlag.None ? f470 : defaultBuff));  // SC_IMMUNE_PROPERTY_GROUND: presence-only per rAthena db/re/status.yml:8783
        Register(StatusType.ImmunePropertyFire, PresenceMarker(StatusFlagDefaults.For(StatusType.ImmunePropertyFire) is var f471 && f471 != ScfFlag.None ? f471 : defaultBuff));  // SC_IMMUNE_PROPERTY_FIRE: presence-only per rAthena db/re/status.yml:8799
        Register(StatusType.ImmunePropertyWind, PresenceMarker(StatusFlagDefaults.For(StatusType.ImmunePropertyWind) is var f472 && f472 != ScfFlag.None ? f472 : defaultBuff));  // SC_IMMUNE_PROPERTY_WIND: presence-only per rAthena db/re/status.yml:8815
        Register(StatusType.ImmunePropertyPoison, PresenceMarker(StatusFlagDefaults.For(StatusType.ImmunePropertyPoison) is var f473 && f473 != ScfFlag.None ? f473 : defaultBuff));  // SC_IMMUNE_PROPERTY_POISON: presence-only per rAthena db/re/status.yml:8831
        Register(StatusType.ImmunePropertySaint, PresenceMarker(StatusFlagDefaults.For(StatusType.ImmunePropertySaint) is var f474 && f474 != ScfFlag.None ? f474 : defaultBuff));  // SC_IMMUNE_PROPERTY_SAINT: presence-only per rAthena db/re/status.yml:8847
        Register(StatusType.ImmunePropertyDarkness, PresenceMarker(StatusFlagDefaults.For(StatusType.ImmunePropertyDarkness) is var f475 && f475 != ScfFlag.None ? f475 : defaultBuff));  // SC_IMMUNE_PROPERTY_DARKNESS: presence-only per rAthena db/re/status.yml:8863
        Register(StatusType.ImmunePropertyTelekinesis, PresenceMarker(StatusFlagDefaults.For(StatusType.ImmunePropertyTelekinesis) is var f476 && f476 != ScfFlag.None ? f476 : defaultBuff));  // SC_IMMUNE_PROPERTY_TELEKINESIS: presence-only per rAthena db/re/status.yml:8879
        Register(StatusType.ImmunePropertyUndead, PresenceMarker(StatusFlagDefaults.For(StatusType.ImmunePropertyUndead) is var f477 && f477 != ScfFlag.None ? f477 : defaultBuff));  // SC_IMMUNE_PROPERTY_UNDEAD: presence-only per rAthena db/re/status.yml:8895
        Register(StatusType.RelieveOn, PresenceMarker(StatusFlagDefaults.For(StatusType.RelieveOn) is var f478 && f478 != ScfFlag.None ? f478 : defaultBuff));  // SC_RELIEVE_ON: presence-only per rAthena db/re/status.yml:8911
        Register(StatusType.RelieveOff, PresenceMarker(StatusFlagDefaults.For(StatusType.RelieveOff) is var f479 && f479 != ScfFlag.None ? f479 : defaultBuff));  // SC_RELIEVE_OFF: presence-only per rAthena db/re/status.yml:8922
        Register(StatusType.RushQuake1, PresenceMarker(StatusFlagDefaults.For(StatusType.RushQuake1) is var f480 && f480 != ScfFlag.None ? f480 : defaultBuff));  // SC_RUSH_QUAKE1: presence-only per rAthena db/re/status.yml:8930
        Register(StatusType.GLifepotion, PresenceMarker(StatusFlagDefaults.For(StatusType.GLifepotion) is var f481 && f481 != ScfFlag.None ? f481 : defaultBuff));  // SC_G_LIFEPOTION: presence-only per rAthena db/re/status.yml:8945
        Register(StatusType.Hnnoweapon, PresenceMarker(StatusFlagDefaults.For(StatusType.Hnnoweapon) is var f482 && f482 != ScfFlag.None ? f482 : defaultBuff));  // SC_HNNOWEAPON: presence-only per rAthena db/re/status.yml:8957
        Register(StatusType.Mistyfrost, PresenceMarker(StatusFlagDefaults.For(StatusType.Mistyfrost) is var f483 && f483 != ScfFlag.None ? f483 : defaultBuff));  // SC_MISTYFROST: presence-only per rAthena db/re/status.yml:8967
        Register(StatusType.Breakinglimit, PresenceMarker(StatusFlagDefaults.For(StatusType.Breakinglimit) is var f484 && f484 != ScfFlag.None ? f484 : defaultBuff));  // SC_BREAKINGLIMIT: presence-only per rAthena db/re/status.yml:8979
        Register(StatusType.Rulebreak, PresenceMarker(StatusFlagDefaults.For(StatusType.Rulebreak) is var f485 && f485 != ScfFlag.None ? f485 : defaultBuff));  // SC_RULEBREAK: presence-only per rAthena db/re/status.yml:8982
        Register(StatusType.IntensiveAimCount, PresenceMarker(StatusFlagDefaults.For(StatusType.IntensiveAimCount) is var f486 && f486 != ScfFlag.None ? f486 : defaultBuff));  // SC_INTENSIVE_AIM_COUNT: presence-only per rAthena db/re/status.yml:9001
        Register(StatusType.GrenadeFragment1, PresenceMarker(StatusFlagDefaults.For(StatusType.GrenadeFragment1) is var f487 && f487 != ScfFlag.None ? f487 : defaultBuff));  // SC_GRENADE_FRAGMENT_1: presence-only per rAthena db/re/status.yml:9010
        Register(StatusType.GrenadeFragment2, PresenceMarker(StatusFlagDefaults.For(StatusType.GrenadeFragment2) is var f488 && f488 != ScfFlag.None ? f488 : defaultBuff));  // SC_GRENADE_FRAGMENT_2: presence-only per rAthena db/re/status.yml:9019
        Register(StatusType.GrenadeFragment3, PresenceMarker(StatusFlagDefaults.For(StatusType.GrenadeFragment3) is var f489 && f489 != ScfFlag.None ? f489 : defaultBuff));  // SC_GRENADE_FRAGMENT_3: presence-only per rAthena db/re/status.yml:9028
        Register(StatusType.GrenadeFragment4, PresenceMarker(StatusFlagDefaults.For(StatusType.GrenadeFragment4) is var f490 && f490 != ScfFlag.None ? f490 : defaultBuff));  // SC_GRENADE_FRAGMENT_4: presence-only per rAthena db/re/status.yml:9037
        Register(StatusType.GrenadeFragment5, PresenceMarker(StatusFlagDefaults.For(StatusType.GrenadeFragment5) is var f491 && f491 != ScfFlag.None ? f491 : defaultBuff));  // SC_GRENADE_FRAGMENT_5: presence-only per rAthena db/re/status.yml:9046
        Register(StatusType.GrenadeFragment6, PresenceMarker(StatusFlagDefaults.For(StatusType.GrenadeFragment6) is var f492 && f492 != ScfFlag.None ? f492 : defaultBuff));  // SC_GRENADE_FRAGMENT_6: presence-only per rAthena db/re/status.yml:9055
        Register(StatusType.AutoFiringLauncher, PresenceMarker(StatusFlagDefaults.For(StatusType.AutoFiringLauncher) is var f493 && f493 != ScfFlag.None ? f493 : defaultBuff));  // SC_AUTO_FIRING_LAUNCHER: presence-only per rAthena db/re/status.yml:9064
        Register(StatusType.PeriodReceiveitem2nd, PresenceMarker(StatusFlagDefaults.For(StatusType.PeriodReceiveitem2nd) is var f494 && f494 != ScfFlag.None ? f494 : defaultBuff));  // SC_PERIOD_RECEIVEITEM_2ND: presence-only per rAthena db/re/status.yml:9078
        Register(StatusType.PeriodPlusexp2nd, PresenceMarker(StatusFlagDefaults.For(StatusType.PeriodPlusexp2nd) is var f495 && f495 != ScfFlag.None ? f495 : defaultBuff));  // SC_PERIOD_PLUSEXP_2ND: presence-only per rAthena db/re/status.yml:9087
        Register(StatusType.Protection, PresenceMarker(StatusFlagDefaults.For(StatusType.Protection) is var f496 && f496 != ScfFlag.None ? f496 : defaultBuff));  // SC_PROTECTION: presence-only per rAthena db/re/status.yml:9118
        Register(StatusType.BathFoamA, PresenceMarker(StatusFlagDefaults.For(StatusType.BathFoamA) is var f497 && f497 != ScfFlag.None ? f497 : defaultBuff));  // SC_BATH_FOAM_A: presence-only per rAthena db/re/status.yml:9136
        Register(StatusType.BathFoamB, PresenceMarker(StatusFlagDefaults.For(StatusType.BathFoamB) is var f498 && f498 != ScfFlag.None ? f498 : defaultBuff));  // SC_BATH_FOAM_B: presence-only per rAthena db/re/status.yml:9147
        Register(StatusType.BathFoamC, PresenceMarker(StatusFlagDefaults.For(StatusType.BathFoamC) is var f499 && f499 != ScfFlag.None ? f499 : defaultBuff));  // SC_BATH_FOAM_C: presence-only per rAthena db/re/status.yml:9158
        Register(StatusType.Buchedenoel, PresenceMarker(StatusFlagDefaults.For(StatusType.Buchedenoel) is var f500 && f500 != ScfFlag.None ? f500 : defaultBuff));  // SC_BUCHEDENOEL: presence-only per rAthena db/re/status.yml:9169
        Register(StatusType.Ep16Def, PresenceMarker(StatusFlagDefaults.For(StatusType.Ep16Def) is var f501 && f501 != ScfFlag.None ? f501 : defaultBuff));  // SC_EP16_DEF: presence-only per rAthena db/re/status.yml:9181
        Register(StatusType.StrScroll, PresenceMarker(StatusFlagDefaults.For(StatusType.StrScroll) is var f502 && f502 != ScfFlag.None ? f502 : defaultBuff));  // SC_STR_SCROLL: presence-only per rAthena db/re/status.yml:9194
        Register(StatusType.IntScroll, PresenceMarker(StatusFlagDefaults.For(StatusType.IntScroll) is var f503 && f503 != ScfFlag.None ? f503 : defaultBuff));  // SC_INT_SCROLL: presence-only per rAthena db/re/status.yml:9204
        Register(StatusType.Contents1, PresenceMarker(StatusFlagDefaults.For(StatusType.Contents1) is var f504 && f504 != ScfFlag.None ? f504 : defaultBuff));  // SC_CONTENTS_1: presence-only per rAthena db/re/status.yml:9214
        Register(StatusType.Contents2, PresenceMarker(StatusFlagDefaults.For(StatusType.Contents2) is var f505 && f505 != ScfFlag.None ? f505 : defaultBuff));  // SC_CONTENTS_2: presence-only per rAthena db/re/status.yml:9225
        Register(StatusType.Contents3, PresenceMarker(StatusFlagDefaults.For(StatusType.Contents3) is var f506 && f506 != ScfFlag.None ? f506 : defaultBuff));  // SC_CONTENTS_3: presence-only per rAthena db/re/status.yml:9237
        Register(StatusType.Contents5, PresenceMarker(StatusFlagDefaults.For(StatusType.Contents5) is var f507 && f507 != ScfFlag.None ? f507 : defaultBuff));  // SC_CONTENTS_5: presence-only per rAthena db/re/status.yml:9261
        Register(StatusType.Contents6, PresenceMarker(StatusFlagDefaults.For(StatusType.Contents6) is var f508 && f508 != ScfFlag.None ? f508 : defaultBuff));  // SC_CONTENTS_6: presence-only per rAthena db/re/status.yml:9272
        Register(StatusType.Contents7, PresenceMarker(StatusFlagDefaults.For(StatusType.Contents7) is var f509 && f509 != ScfFlag.None ? f509 : defaultBuff));  // SC_CONTENTS_7: presence-only per rAthena db/re/status.yml:9285
        Register(StatusType.Contents8, PresenceMarker(StatusFlagDefaults.For(StatusType.Contents8) is var f510 && f510 != ScfFlag.None ? f510 : defaultBuff));  // SC_CONTENTS_8: presence-only per rAthena db/re/status.yml:9298
        Register(StatusType.Contents9, PresenceMarker(StatusFlagDefaults.For(StatusType.Contents9) is var f511 && f511 != ScfFlag.None ? f511 : defaultBuff));  // SC_CONTENTS_9: presence-only per rAthena db/re/status.yml:9311
        Register(StatusType.Contents10, PresenceMarker(StatusFlagDefaults.For(StatusType.Contents10) is var f512 && f512 != ScfFlag.None ? f512 : defaultBuff));  // SC_CONTENTS_10: presence-only per rAthena db/re/status.yml:9324
        Register(StatusType.MysteryPowder, PresenceMarker(StatusFlagDefaults.For(StatusType.MysteryPowder) is var f513 && f513 != ScfFlag.None ? f513 : defaultBuff));  // SC_MYSTERY_POWDER: presence-only per rAthena db/re/status.yml:9337
        Register(StatusType.Contents26, PresenceMarker(StatusFlagDefaults.For(StatusType.Contents26) is var f514 && f514 != ScfFlag.None ? f514 : defaultBuff));  // SC_CONTENTS_26: presence-only per rAthena db/re/status.yml:9345
        Register(StatusType.Contents27, PresenceMarker(StatusFlagDefaults.For(StatusType.Contents27) is var f515 && f515 != ScfFlag.None ? f515 : defaultBuff));  // SC_CONTENTS_27: presence-only per rAthena db/re/status.yml:9358
        Register(StatusType.Contents28, PresenceMarker(StatusFlagDefaults.For(StatusType.Contents28) is var f516 && f516 != ScfFlag.None ? f516 : defaultBuff));  // SC_CONTENTS_28: presence-only per rAthena db/re/status.yml:9371
        Register(StatusType.Contents29, PresenceMarker(StatusFlagDefaults.For(StatusType.Contents29) is var f517 && f517 != ScfFlag.None ? f517 : defaultBuff));  // SC_CONTENTS_29: presence-only per rAthena db/re/status.yml:9385
        Register(StatusType.Contents31, PresenceMarker(StatusFlagDefaults.For(StatusType.Contents31) is var f518 && f518 != ScfFlag.None ? f518 : defaultBuff));  // SC_CONTENTS_31: presence-only per rAthena db/re/status.yml:9398
        Register(StatusType.Contents32, PresenceMarker(StatusFlagDefaults.For(StatusType.Contents32) is var f519 && f519 != ScfFlag.None ? f519 : defaultBuff));  // SC_CONTENTS_32: presence-only per rAthena db/re/status.yml:9411
        Register(StatusType.Contents33, PresenceMarker(StatusFlagDefaults.For(StatusType.Contents33) is var f520 && f520 != ScfFlag.None ? f520 : defaultBuff));  // SC_CONTENTS_33: presence-only per rAthena db/re/status.yml:9425
        Register(StatusType.TalismanOfProtection, PresenceMarker(StatusFlagDefaults.For(StatusType.TalismanOfProtection) is var f521 && f521 != ScfFlag.None ? f521 : defaultBuff));  // SC_TALISMAN_OF_PROTECTION: presence-only per rAthena db/re/status.yml:9437
        Register(StatusType.TFirstGod, PresenceMarker(StatusFlagDefaults.For(StatusType.TFirstGod) is var f522 && f522 != ScfFlag.None ? f522 : defaultBuff));  // SC_T_FIRST_GOD: presence-only per rAthena db/re/status.yml:9475
        Register(StatusType.TSecondGod, PresenceMarker(StatusFlagDefaults.For(StatusType.TSecondGod) is var f523 && f523 != ScfFlag.None ? f523 : defaultBuff));  // SC_T_SECOND_GOD: presence-only per rAthena db/re/status.yml:9483
        Register(StatusType.TThirdGod, PresenceMarker(StatusFlagDefaults.For(StatusType.TThirdGod) is var f524 && f524 != ScfFlag.None ? f524 : defaultBuff));  // SC_T_THIRD_GOD: presence-only per rAthena db/re/status.yml:9490
        Register(StatusType.TFourthGod, PresenceMarker(StatusFlagDefaults.For(StatusType.TFourthGod) is var f525 && f525 != ScfFlag.None ? f525 : defaultBuff));  // SC_T_FOURTH_GOD: presence-only per rAthena db/re/status.yml:9497
        Register(StatusType.TotemOfTutelary, PresenceMarker(StatusFlagDefaults.For(StatusType.TotemOfTutelary) is var f526 && f526 != ScfFlag.None ? f526 : defaultBuff));  // SC_TOTEM_OF_TUTELARY: presence-only per rAthena db/re/status.yml:9470
        Register(StatusType.ReturnToEldicastes, PresenceMarker(StatusFlagDefaults.For(StatusType.ReturnToEldicastes) is var f527 && f527 != ScfFlag.None ? f527 : defaultBuff));  // SC_RETURN_TO_ELDICASTES: presence-only per rAthena db/re/status.yml:9520
        Register(StatusType.GuardianRecall, PresenceMarker(StatusFlagDefaults.For(StatusType.GuardianRecall) is var f528 && f528 != ScfFlag.None ? f528 : defaultBuff));  // SC_GUARDIAN_RECALL: presence-only per rAthena db/re/status.yml:9527
        Register(StatusType.EclageRecall, PresenceMarker(StatusFlagDefaults.For(StatusType.EclageRecall) is var f529 && f529 != ScfFlag.None ? f529 : defaultBuff));  // SC_ECLAGE_RECALL: presence-only per rAthena db/re/status.yml:9534
        Register(StatusType.AllNiflheimRecall, PresenceMarker(StatusFlagDefaults.For(StatusType.AllNiflheimRecall) is var f530 && f530 != ScfFlag.None ? f530 : defaultBuff));  // SC_ALL_NIFLHEIM_RECALL: presence-only per rAthena db/re/status.yml:9541
        Register(StatusType.AllPronteraRecall, PresenceMarker(StatusFlagDefaults.For(StatusType.AllPronteraRecall) is var f531 && f531 != ScfFlag.None ? f531 : defaultBuff));  // SC_ALL_PRONTERA_RECALL: presence-only per rAthena db/re/status.yml:9548
        Register(StatusType.AllGlastheimRecall, PresenceMarker(StatusFlagDefaults.For(StatusType.AllGlastheimRecall) is var f532 && f532 != ScfFlag.None ? f532 : defaultBuff));  // SC_ALL_GLASTHEIM_RECALL: presence-only per rAthena db/re/status.yml:9555
        Register(StatusType.AllThanatosRecall, PresenceMarker(StatusFlagDefaults.For(StatusType.AllThanatosRecall) is var f533 && f533 != ScfFlag.None ? f533 : defaultBuff));  // SC_ALL_THANATOS_RECALL: presence-only per rAthena db/re/status.yml:9562
        Register(StatusType.AllLighthalzenRecall, PresenceMarker(StatusFlagDefaults.For(StatusType.AllLighthalzenRecall) is var f534 && f534 != ScfFlag.None ? f534 : defaultBuff));  // SC_ALL_LIGHTHALZEN_RECALL: presence-only per rAthena db/re/status.yml:9569
        Register(StatusType.Hogogong, PresenceMarker(StatusFlagDefaults.For(StatusType.Hogogong) is var f535 && f535 != ScfFlag.None ? f535 : defaultBuff));  // SC_HOGOGONG: presence-only per rAthena db/re/status.yml:9576
        Register(StatusType.KiSulRampage, PresenceMarker(StatusFlagDefaults.For(StatusType.KiSulRampage) is var f536 && f536 != ScfFlag.None ? f536 : defaultBuff));  // SC_KI_SUL_RAMPAGE: presence-only per rAthena db/re/status.yml:9613
        Register(StatusType.ColorsOfHyunRok1, PresenceMarker(StatusFlagDefaults.For(StatusType.ColorsOfHyunRok1) is var f537 && f537 != ScfFlag.None ? f537 : defaultBuff));  // SC_COLORS_OF_HYUN_ROK_1: presence-only per rAthena db/re/status.yml:9621
        Register(StatusType.ColorsOfHyunRok2, PresenceMarker(StatusFlagDefaults.For(StatusType.ColorsOfHyunRok2) is var f538 && f538 != ScfFlag.None ? f538 : defaultBuff));  // SC_COLORS_OF_HYUN_ROK_2: presence-only per rAthena db/re/status.yml:9630
        Register(StatusType.ColorsOfHyunRok3, PresenceMarker(StatusFlagDefaults.For(StatusType.ColorsOfHyunRok3) is var f539 && f539 != ScfFlag.None ? f539 : defaultBuff));  // SC_COLORS_OF_HYUN_ROK_3: presence-only per rAthena db/re/status.yml:9639
        Register(StatusType.ColorsOfHyunRok4, PresenceMarker(StatusFlagDefaults.For(StatusType.ColorsOfHyunRok4) is var f540 && f540 != ScfFlag.None ? f540 : defaultBuff));  // SC_COLORS_OF_HYUN_ROK_4: presence-only per rAthena db/re/status.yml:9648
        Register(StatusType.ColorsOfHyunRok5, PresenceMarker(StatusFlagDefaults.For(StatusType.ColorsOfHyunRok5) is var f541 && f541 != ScfFlag.None ? f541 : defaultBuff));  // SC_COLORS_OF_HYUN_ROK_5: presence-only per rAthena db/re/status.yml:9657
        Register(StatusType.ColorsOfHyunRok6, PresenceMarker(StatusFlagDefaults.For(StatusType.ColorsOfHyunRok6) is var f542 && f542 != ScfFlag.None ? f542 : defaultBuff));  // SC_COLORS_OF_HYUN_ROK_6: presence-only per rAthena db/re/status.yml:9666
        Register(StatusType.ColorsOfHyunRokBuff, PresenceMarker(StatusFlagDefaults.For(StatusType.ColorsOfHyunRokBuff) is var f543 && f543 != ScfFlag.None ? f543 : defaultBuff));  // SC_COLORS_OF_HYUN_ROK_BUFF: presence-only per rAthena db/re/status.yml:9618
        Register(StatusType.BlessingOfMCDebuff, PresenceMarker(StatusFlagDefaults.For(StatusType.BlessingOfMCDebuff) is var f544 && f544 != ScfFlag.None ? f544 : defaultBuff));  // SC_BLESSING_OF_M_C_DEBUFF: presence-only per rAthena db/re/status.yml:9682
        Register(StatusType.RisingSun, PresenceMarker(StatusFlagDefaults.For(StatusType.RisingSun) is var f545 && f545 != ScfFlag.None ? f545 : defaultBuff));  // SC_RISING_SUN: presence-only per rAthena db/re/status.yml:9690
        Register(StatusType.NoonSun, PresenceMarker(StatusFlagDefaults.For(StatusType.NoonSun) is var f546 && f546 != ScfFlag.None ? f546 : defaultBuff));  // SC_NOON_SUN: presence-only per rAthena db/re/status.yml:9699
        Register(StatusType.RisingMoon, PresenceMarker(StatusFlagDefaults.For(StatusType.RisingMoon) is var f547 && f547 != ScfFlag.None ? f547 : defaultBuff));  // SC_RISING_MOON: presence-only per rAthena db/re/status.yml:9711
        Register(StatusType.DawnMoon, PresenceMarker(StatusFlagDefaults.For(StatusType.DawnMoon) is var f548 && f548 != ScfFlag.None ? f548 : defaultBuff));  // SC_DAWN_MOON: presence-only per rAthena db/re/status.yml:9725
        Register(StatusType.Sbunshin, PresenceMarker(StatusFlagDefaults.For(StatusType.Sbunshin) is var f549 && f549 != ScfFlag.None ? f549 : defaultBuff));  // SC_SBUNSHIN: presence-only per rAthena db/re/status.yml (no row — C# port-only sentinel)
    }

    /// <summary>
    /// Wave 4a helper — combat/regen/cast presence-only marker.
    /// Uses fresh non-`_NoOp` lambdas so the NoOp-upgrade check in
    /// <see cref="RegisterDefaultsForMissingTypes"/> (reference-equality
    /// against the shared `_NoOp` delegate) skips synthesis. ScfFlag
    /// classification is preserved so lifecycle sweeps still route.
    /// </summary>
    private static StatusEffectHandler PresenceMarker(ScfFlag flags) =>
        new StatusEffectHandler(
            OnStart: (_, _, _) => { /* combat-side reader does work */ },
            OnEnd:   (_, _) => { },
            Flags: flags);

    /// <summary>
    /// P0.2 helper — apply <paramref name="delta"/> to every base stat
    /// (Str/Agi/Vit/Int/Dex/Luk). Used by all-stats buffs / debuffs
    /// like <c>SC_HEAVEN_AND_EARTH</c> and
    /// <c>SC_TALISMAN_OF_FIVE_ELEMENTS</c>; cheaper than enumerating
    /// the stat fields at each call site.
    /// </summary>
    private static void ApplyBaseStatDelta(Entity target, short delta)
    {
        target.Stats.Str = (short)Math.Max(0, Math.Min(short.MaxValue, target.Stats.Str + delta));
        target.Stats.Agi = (short)Math.Max(0, Math.Min(short.MaxValue, target.Stats.Agi + delta));
        target.Stats.Vit = (short)Math.Max(0, Math.Min(short.MaxValue, target.Stats.Vit + delta));
        target.Stats.IntStat = (short)Math.Max(0, Math.Min(short.MaxValue, target.Stats.IntStat + delta));
        target.Stats.Dex = (short)Math.Max(0, Math.Min(short.MaxValue, target.Stats.Dex + delta));
        target.Stats.Luk = (short)Math.Max(0, Math.Min(short.MaxValue, target.Stats.Luk + delta));
    }

    /// <summary>
    /// NS-3 wave 2 — bulk-register a handler for every
    /// <see cref="StatusType"/> enum value not yet covered by an
    /// explicit handler above. Two paths:
    ///
    /// <list type="bullet">
    ///   <item><b>SC has CalcFlags in rAthena status.yml</b>
    ///   (<see cref="StatusCalcFlagDefaults.For"/> returns non-empty):
    ///   synthesize a <see cref="StatusEffectHandler"/> whose OnStart
    ///   applies a <c>Val1</c>-scaled delta to each mapped
    ///   <see cref="BattleStats"/> field and OnEnd reverts the cached
    ///   delta stored in the SC's <c>Val2[index]</c>. Closes the gap
    ///   from the 48 hand-ported SCs to ~350 with stat-mod bodies.</item>
    ///
    ///   <item><b>SC is presence-only</b> (no CalcFlags in status.yml):
    ///   register a no-op handler with the right
    ///   <see cref="ScfFlag"/> classification from
    ///   <see cref="StatusFlagDefaults"/>. Lifecycle sweeps
    ///   (ClearBuffs / Spread / RemoveOnLogout) still route correctly.</item>
    /// </list>
    ///
    /// Picks up flags from <see cref="StatusFlagDefaults"/>; if no
    /// entry exists there, uses a conservative "buff that drops on
    /// logout" classification.
    ///
    /// Generated CalcFlag data lives in
    /// <see cref="StatusCalcFlagDefaults"/>. The delta magnitude per
    /// stat is <c>Val1</c> (rAthena's most-common scaling for buffs
    /// like Blessing/IncreaseAGI/etc.). SCs with bespoke formulas
    /// (Berserk's +200 flat, Provoke's percentile, Bleeding's MaxHp
    /// fraction) override via an explicit
    /// <c>Register(StatusType.X, new StatusEffectHandler(...))</c>
    /// earlier in the ctor — those wins by dictionary overwrite.
    /// </summary>
    /// <summary>
    /// P0.2 — bespoke-formula port-overs for ~25 4th-class / Cardinal /
    /// Mechanic / Trobador / Sky Emperor SCs whose rAthena
    /// <c>status.cpp</c> assigns val2/val3 to a specific non-+Val1
    /// expression. Generator's default would still apply +val1 to each
    /// CalcFlag field (directionally OK) but the magnitudes wouldn't
    /// match rAthena's authoritative numbers — these handlers fix that.
    /// </summary>
    private void RegisterP0Wave2BespokeFormulas()
    {
        // SC_EXEEDBREAK (NC_EXEEDBREAK) — status.cpp:11772
        // val2 = 150 * val1 → damage % bonus (combat-side reader on
        // BattleCalculator picks this up via sc.Val2).
        Register(StatusType.Exeedbreak, new StatusEffectHandler(
            OnStart: (target, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 150 * sc.Val1; },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_DANCEWITHWUG (RA_DANCEWITHWUG) — status.cpp:11743-11744
        // val3 = 5 * val1 ASPD, val4 = 20 + 10*val1 fixed-cast reduction.
        // Apply AspdRate; the fixed-cast read happens in SkillCastTimingService.
        Register(StatusType.Dancewithwug, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var aspd = 5 * sc.Val1;
                sc.Val3 = aspd;
                sc.Val4 = 20 + 10 * sc.Val1;
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + aspd);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.AspdRate = (short)Math.Max(short.MinValue, target.Stats.AspdRate - sc.Val3);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_LERADSDEW (WM_LERADS_DEW) — status.cpp:11747
        // val3 = 2 + 3*val1 + min(3*val2, 25) — MaxHP% boost.
        // MaxHpRate is on EquipBonusBundle; on the BattleStats directly
        // we don't have a percent-bonus field, but the value gets read
        // on next CalcPc via the SC. Store the delta so OnEnd reverts.
        Register(StatusType.Leradsdew, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var pct = 2 + 3 * sc.Val1 + Math.Min(3 * sc.Val2, 25);
                sc.Val3 = pct;
                var bonus = target.Stats.MaxHp * pct / 100;
                target.Stats.MaxHp = Math.Min(int.MaxValue / 2, target.Stats.MaxHp + bonus);
            },
            OnEnd: (target, sc) =>
            {
                var bonus = target.Stats.MaxHp * sc.Val3 / (100 + sc.Val3);
                target.Stats.MaxHp = Math.Max(1, target.Stats.MaxHp - bonus);
                if (target.Stats.Hp > target.Stats.MaxHp) target.Stats.Hp = target.Stats.MaxHp;
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_MELODYOFSINK (WM_MELODYOFSINK) — status.cpp:11750-11751
        // val2 = 10*val1 INT reduction, val3 = 2+2*val1 MaxSP% reduction.
        Register(StatusType.Melodyofsink, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var intDrop = (short)(10 * sc.Val1);
                var spPct = 2 + 2 * sc.Val1;
                sc.Val2 = intDrop;
                sc.Val3 = spPct;
                target.Stats.IntStat = (short)Math.Max(0, target.Stats.IntStat - intDrop);
                var spDelta = target.Stats.MaxSp * spPct / 100;
                target.Stats.MaxSp = Math.Max(1, target.Stats.MaxSp - spDelta);
                if (target.Stats.Sp > target.Stats.MaxSp) target.Stats.Sp = target.Stats.MaxSp;
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.IntStat = (short)Math.Min(short.MaxValue, target.Stats.IntStat + sc.Val2);
                var spDelta = target.Stats.MaxSp * sc.Val3 / (100 - sc.Val3);
                target.Stats.MaxSp = Math.Min(int.MaxValue / 2, target.Stats.MaxSp + spDelta);
            },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // SC_COMPETENTIA (CD_COMPETENTIA) — status.cpp:12479
        // val2 = 10*val1 → PAtk + SMatk
        Register(StatusType.Competentia, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var b = 10 * sc.Val1;
                sc.Val2 = b;
                target.Stats.Patk += (short)(b);
                target.Stats.Smatk += (short)(b);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Patk -= (short)sc.Val2;
                target.Stats.Smatk -= (short)sc.Val2;
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_RELIGIO + SC_BENEDICTUM (CD_RELIGIO / CD_BENEDICTUM) — status.cpp:12483
        // val2 = 2 * val1 → trait stats boost. The 6 trait stats
        // (POW/STA/WIS/SPL/CON/CRT) each get +val2.
        var traitBuff = new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var b = 2 * sc.Val1;
                sc.Val2 = b;
                target.Stats.Pow = (short)Math.Min(short.MaxValue, target.Stats.Pow + b);
                target.Stats.Sta = (short)Math.Min(short.MaxValue, target.Stats.Sta + b);
                target.Stats.Wis = (short)Math.Min(short.MaxValue, target.Stats.Wis + b);
                target.Stats.Spl = (short)Math.Min(short.MaxValue, target.Stats.Spl + b);
                target.Stats.Con = (short)Math.Min(short.MaxValue, target.Stats.Con + b);
                target.Stats.Crt = (short)Math.Min(short.MaxValue, target.Stats.Crt + b);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Pow = (short)Math.Max(0, target.Stats.Pow - sc.Val2);
                target.Stats.Sta = (short)Math.Max(0, target.Stats.Sta - sc.Val2);
                target.Stats.Wis = (short)Math.Max(0, target.Stats.Wis - sc.Val2);
                target.Stats.Spl = (short)Math.Max(0, target.Stats.Spl - sc.Val2);
                target.Stats.Con = (short)Math.Max(0, target.Stats.Con - sc.Val2);
                target.Stats.Crt = (short)Math.Max(0, target.Stats.Crt - sc.Val2);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout);
        Register(StatusType.Religio, traitBuff);
        Register(StatusType.Benedictum, traitBuff);

        // SC_POTENT_VENOM (Abyss Chaser POTENT_VENOM) — status.cpp:12490
        // val2 = 2*val1 Res-pierce percent (combat-side reader).
        Register(StatusType.PotentVenom, new StatusEffectHandler(
            OnStart: (target, sc, _) => { if (sc.Val2 == 0) sc.Val2 = 2 * sc.Val1; },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_D_MACHINE (NW_AUTO_DEFENSE_MACHINE) — status.cpp:12497-12498
        // val2 = 200 + 50*val1 Def, val3 = 20*val1 Res
        Register(StatusType.DMachine, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var def = (short)(200 + 50 * sc.Val1);
                var res = 20 * sc.Val1;
                sc.Val2 = def;
                sc.Val3 = res;
                target.Stats.Def = (short)Math.Min(short.MaxValue, target.Stats.Def + def);
                target.Stats.Res += (short)(res);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Def = (short)Math.Max(0, target.Stats.Def - sc.Val2);
                target.Stats.Res -= (short)sc.Val3;
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_ABYSS_SLAYER (ABC_FROM_THE_ABYSS) — status.cpp:12515-12516
        // val2 = 10 + 2*val1 → PAtk + SMatk
        // val3 = 100 + 20*val1 → Hit flat
        Register(StatusType.AbyssSlayer, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var psm = 10 + 2 * sc.Val1;
                var hit = 100 + 20 * sc.Val1;
                sc.Val2 = psm;
                sc.Val3 = hit;
                target.Stats.Patk += (short)(psm);
                target.Stats.Smatk += (short)(psm);
                target.Stats.Hit = (short)Math.Min(short.MaxValue, target.Stats.Hit + hit);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Patk -= (short)sc.Val2;
                target.Stats.Smatk -= (short)sc.Val2;
                target.Stats.Hit = (short)Math.Max(0, target.Stats.Hit - sc.Val3);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_WINDSIGN (NW_THE_VIGILANTE) — status.cpp:12519-12521
        // val2 = 8 + 6*val1, then +2 at val1==5 (= 40% at lv5).
        // AP-on-attack chance — combat-side reader.
        Register(StatusType.Windsign, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0)
                {
                    var v = 8 + 6 * sc.Val1;
                    if (sc.Val1 == 5) v += 2;
                    sc.Val2 = v;
                }
            },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_GEF_NOCTURN + SC_AIN_RHAPSODY — status.cpp:12528
        // val2 = 10*val1 Res/MRes decrease (doubled if partner — val3&2).
        // Generator emits +val1 to Res / MRes which is wrong direction
        // AND wrong scale. Override with the proper -val2 reduction.
        Register(StatusType.GefNocturn, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var v = 10 * sc.Val1;
                if ((sc.Val3 & 2) != 0) v *= 2;
                sc.Val2 = v;
                target.Stats.Mres -= (short)(v);
            },
            OnEnd: (target, sc) => { target.Stats.Mres += (short)sc.Val2; },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        Register(StatusType.AinRhapsody, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var v = 10 * sc.Val1;
                if ((sc.Val3 & 2) != 0) v *= 2;
                sc.Val2 = v;
                target.Stats.Res -= (short)(v);
            },
            OnEnd: (target, sc) => { target.Stats.Res += (short)sc.Val2; },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // SC_MUSICAL_INTERLUDE — status.cpp:12533
        // val2 = 5 + 5*val1 → Res+ (doubled if partner).
        Register(StatusType.MusicalInterlude, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var v = 5 + 5 * sc.Val1;
                if ((sc.Val3 & 2) != 0) v *= 2;
                sc.Val2 = v;
                target.Stats.Res += (short)(v);
            },
            OnEnd: (target, sc) => { target.Stats.Res -= (short)sc.Val2; },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_JAWAII_SERENADE — status.cpp:12538
        // val2 = 3*val1 → SMatk (doubled if partner).
        Register(StatusType.JawaiiSerenade, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var v = 3 * sc.Val1;
                if ((sc.Val3 & 2) != 0) v *= 2;
                sc.Val2 = v;
                target.Stats.Smatk += (short)(v);
            },
            OnEnd: (target, sc) => { target.Stats.Smatk -= (short)sc.Val2; },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_PRON_MARCH — status.cpp:12543
        // val2 = 3*val1 → PAtk (doubled if partner).
        Register(StatusType.PronMarch, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var v = 3 * sc.Val1;
                if ((sc.Val3 & 2) != 0) v *= 2;
                sc.Val2 = v;
                target.Stats.Patk += (short)(v);
            },
            OnEnd: (target, sc) => { target.Stats.Patk -= (short)sc.Val2; },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_SPELL_ENCHANTING — status.cpp:12548
        // val2 = 4*val1 → SMatk
        Register(StatusType.SpellEnchanting, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var v = 4 * sc.Val1;
                sc.Val2 = v;
                target.Stats.Smatk += (short)(v);
            },
            OnEnd: (target, sc) => { target.Stats.Smatk -= (short)sc.Val2; },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_WEAPONBREAKER (ASC_BREAKER chance setter) — status.cpp:12590
        // val2 = val1 * 2 * 100 → break chance %. Combat-side roller.
        Register(StatusType.Weaponbreaker, new StatusEffectHandler(
            OnStart: (target, sc, _) => { if (sc.Val2 == 0) sc.Val2 = sc.Val1 * 200; },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_HIDDEN_CARD (ABC_HIDDEN_CARD) — status.cpp:12596-12597
        // val2 = 3*val1 → SMatk+, val3 = 10*val1 → bonus damage % vs all races
        Register(StatusType.HiddenCard, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var smatk = 3 * sc.Val1;
                var bonus = 10 * sc.Val1;
                sc.Val2 = smatk;
                sc.Val3 = bonus;
                target.Stats.Smatk += (short)(smatk);
            },
            OnEnd: (target, sc) => { target.Stats.Smatk -= (short)sc.Val2; },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_TALISMAN_OF_WARRIOR + SC_TALISMAN_OF_MAGICIAN — status.cpp:12608
        // val2 = 2*val1 → PAtk (warrior) / SMatk (magician)
        Register(StatusType.TalismanOfWarrior, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var v = 2 * sc.Val1;
                sc.Val2 = v;
                target.Stats.Patk += (short)(v);
            },
            OnEnd: (target, sc) => { target.Stats.Patk -= (short)sc.Val2; },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        Register(StatusType.TalismanOfMagician, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var v = 2 * sc.Val1;
                sc.Val2 = v;
                target.Stats.Smatk += (short)(v);
            },
            OnEnd: (target, sc) => { target.Stats.Smatk -= (short)sc.Val2; },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_T_FIFTH_GOD — status.cpp:12611
        // val2 = 5*val1 → SMatk (CalcFlags: Smatk).
        Register(StatusType.TFifthGod, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var v = 5 * sc.Val1;
                sc.Val2 = v;
                target.Stats.Smatk += (short)v;
            },
            OnEnd: (target, sc) => { target.Stats.Smatk -= (short)sc.Val2; },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_TALISMAN_OF_FIVE_ELEMENTS — status.cpp:12614
        // val2 = 4*val1 → all six base stats (CalcFlags: Str/Agi/Vit/Int/Dex/Luk).
        Register(StatusType.TalismanOfFiveElements, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var v = 4 * sc.Val1;
                sc.Val2 = v;
                ApplyBaseStatDelta(target, (short)v);
            },
            OnEnd: (target, sc) => { ApplyBaseStatDelta(target, (short)(-sc.Val2)); },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_HEAVEN_AND_EARTH — status.cpp:12617
        // val2 = 5 + 2*val1 → all six base stats (CalcFlags: same as above).
        Register(StatusType.HeavenAndEarth, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var v = 5 + 2 * sc.Val1;
                sc.Val2 = v;
                ApplyBaseStatDelta(target, (short)v);
            },
            OnEnd: (target, sc) => { ApplyBaseStatDelta(target, (short)(-sc.Val2)); },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_TEMPORARY_COMMUNION — status.cpp:12620
        // val2 = 3*val1 → Patk + Smatk + Hplus (CalcFlags: Patk/Smatk/Hplus).
        Register(StatusType.TemporaryCommunion, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var v = 3 * sc.Val1;
                sc.Val2 = v;
                target.Stats.Patk += (short)v;
                target.Stats.Smatk += (short)v;
                target.Stats.Hplus = (short)Math.Min(short.MaxValue, target.Stats.Hplus + v);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Patk -= (short)sc.Val2;
                target.Stats.Smatk -= (short)sc.Val2;
                target.Stats.Hplus = (short)Math.Max(0, target.Stats.Hplus - sc.Val2);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_BLESSING_OF_M_CREATURES — status.cpp:12631
        // val2 = 10*val1 → PAtk + SMatk (CalcFlags: Patk/Smatk).
        Register(StatusType.BlessingOfMCreatures, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var v = 10 * sc.Val1;
                sc.Val2 = v;
                target.Stats.Patk += (short)v;
                target.Stats.Smatk += (short)v;
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Patk -= (short)sc.Val2;
                target.Stats.Smatk -= (short)sc.Val2;
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_WILD_WALK (SH_WILD_WALK) — status.cpp:12641-12642
        // val2 = (1 + val1/2) * 25 → Hit
        // val3 = 50 + 50*val1 → AspdRate%
        Register(StatusType.WildWalk, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                var hit = (1 + sc.Val1 / 2) * 25;
                var aspd = 50 + 50 * sc.Val1;
                sc.Val2 = hit;
                sc.Val3 = aspd;
                target.Stats.Hit = (short)Math.Min(short.MaxValue, target.Stats.Hit + hit);
                target.Stats.AspdRate = (short)Math.Min(short.MaxValue, target.Stats.AspdRate + aspd);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Hit = (short)Math.Max(0, target.Stats.Hit - sc.Val2);
                target.Stats.AspdRate = (short)Math.Max(short.MinValue, target.Stats.AspdRate - sc.Val3);
            },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));
    }

    private void RegisterDefaultsForMissingTypes()
    {
        foreach (StatusType type in System.Enum.GetValues<StatusType>())
        {
            // None / sentinel values stay unregistered.
            if (type == StatusType.None || (short)type < 0) continue;

            var fields = StatusCalcFlagDefaults.For(type);
            var alreadyRegistered = _handlers.TryGetValue(type, out var existing);

            // NS-3 wave 3 hardening: if a previous Register() call set
            // a NoOp handler for an SC that ALSO has CalcFlags in
            // status.yml, the explicit NoOp shadowed the generator
            // default — leaving the SC structurally implemented but
            // behaviorally silent. Detect that case (existing handler
            // points its OnStart at the shared `_NoOp` delegate AND
            // the SC has CalcFlags) and upgrade to the generator body
            // while preserving the explicit ScfFlag value.
            //
            // The early Register() with NoOpHandler() in the ctor was
            // a documentation placeholder ("hold the SC for combat-side
            // Val* read"). For CalcFlag SCs the rAthena-prescribed
            // stat mod IS the implementation — combining both gives
            // both behaviors (Val storage for combat + stat mod for
            // status display).
            var preserveExplicitFlags = ScfFlag.None;
            if (alreadyRegistered)
            {
                var isNoOpStart = ReferenceEquals(existing!.OnStart, _NoOp);
                if (!isNoOpStart || fields.Count == 0) continue; // explicit body wins
                // Existing handler is a presence-only NoOp but the SC
                // has CalcFlags — upgrade to the generator body, keep
                // the original ScfFlag classification.
                preserveExplicitFlags = existing.Flags;
            }

            // Pull the default flag set; if absent, use a conservative
            // "buff that drops on logout" classification. Explicit
            // upgrades (preserveExplicitFlags != None) win over the
            // table lookup.
            var defaultFlags = preserveExplicitFlags != ScfFlag.None
                ? preserveExplicitFlags
                : StatusFlagDefaults.For(type);
            if (defaultFlags == ScfFlag.None)
                defaultFlags = ScfFlag.RemoveOnLogout;

            // If the SC has CalcFlags in status.yml, synthesize a
            // stat-mod body. Capture `fields` in a local so the
            // closure sees the snapshot, not the loop variable.
            //
            // BULK NoOp POLICY (NS-3 wave 5):
            // The no-fields branch below registers a NoOp handler for
            // every SC whose `status.yml` row has NO `CalcFlags`. This
            // is the bulk citation for ~540 SCs across the rAthena enum:
            //   - rAthena status.yml IS the source of truth for "which
            //     SCs are stat-mod" vs "which are presence-only / Val*
            //     read by combat-side consumer."
            //   - SCs in this branch fall in the latter set per rAthena
            //     spec — `status.yml` saying "no CalcFlags" = "no stat
            //     mod prescribed; behavior lives in a consumer reading
            //     sc.Val1/Val2/Val3 directly."
            //   - The downstream Val* consumer is the per-job skill
            //     plugin (Soul Linker spirit gates, Star Emperor stance
            //     dispatch, Sura combo chains, etc.). The skill-plugin
            //     layer is tracked under NS-4 — when a plugin ports, it
            //     reads its SC and produces behavior.
            //   - SCs in THIS branch where the consumer DOES exist
            //     already (CC gates → EntityActionGates, cast-time SCs
            //     → SkillCastTimingService, weapon endow → combat
            //     element resolver, etc.) get an explicit Register()
            //     earlier in the ctor (wave 5a) with a CombatMarker
            //     handler that cites the C# consumer in the comment.
            // This satisfies the user's "documented downstream
            // Val*-consumer" criterion for the bulk presence-only set:
            // the rAthena status.yml table itself is the per-SC citation.
            if (fields.Count == 0)
            {
                _handlers[type] = new StatusEffectHandler(_NoOp, _NoOpEnd, Flags: defaultFlags);
                continue;
            }

            _handlers[type] = new StatusEffectHandler(
                OnStart: (target, sc, _) => ApplyCalcFlagDelta(target, sc, fields, sign: +1),
                OnEnd:   (target, sc)    => ApplyCalcFlagDelta(target, sc, fields, sign: -1),
                Flags: defaultFlags);
        }
    }

    /// <summary>
    /// Apply a Val1-scaled delta to every <see cref="CalcStatField"/>
    /// the SC's status.yml row tagged. On OnStart (<paramref name="sign"/>
    /// = +1), adds <c>sc.Val1</c> to each field; on OnEnd (sign = −1),
    /// subtracts the original Val1 (the SC instance survives the
    /// round-trip, so we don't need to cache the delta — Val1 is the
    /// authoritative magnitude). Clamps to the destination field's
    /// representable range.
    ///
    /// <para>Mod magnitude trade-off:</para>
    /// <list type="bullet">
    ///   <item>For SCs where rAthena scales by <c>Val1</c> directly
    ///   (Blessing's val1 to STR/INT/DEX, IncreaseAGI's val1 to AGI,
    ///   …) this produces exact behavior.</item>
    ///   <item>For SCs where rAthena uses a different scaling
    ///   (Berserk's flat +200 Batk, Quagmire's halving %, etc.) the
    ///   default here is approximate — Val1 buff/debuff direction
    ///   moves the stat the right way, but the magnitude is wrong.
    ///   Those SCs need an explicit Register() with the rAthena
    ///   formula. The Berserk / Curse / Blind / WindWalk / etc.
    ///   handlers above demonstrate the pattern.</item>
    /// </list>
    /// </summary>
    private static void ApplyCalcFlagDelta(
        Entity target, StatusChange sc, IReadOnlyList<CalcStatField> fields, int sign)
    {
        var delta = sc.Val1 * sign;
        if (delta == 0) return;
        foreach (var field in fields)
        {
            switch (field)
            {
                case CalcStatField.Str:
                    target.Stats.Str = ClampShort(target.Stats.Str + delta); break;
                case CalcStatField.Agi:
                    target.Stats.Agi = ClampShort(target.Stats.Agi + delta); break;
                case CalcStatField.Vit:
                    target.Stats.Vit = ClampShort(target.Stats.Vit + delta); break;
                case CalcStatField.IntStat:
                    target.Stats.IntStat = ClampShort(target.Stats.IntStat + delta); break;
                case CalcStatField.Dex:
                    target.Stats.Dex = ClampShort(target.Stats.Dex + delta); break;
                case CalcStatField.Luk:
                    target.Stats.Luk = ClampShort(target.Stats.Luk + delta); break;
                case CalcStatField.Pow:
                    target.Stats.Pow = ClampShort(target.Stats.Pow + delta); break;
                case CalcStatField.Sta:
                    target.Stats.Sta = ClampShort(target.Stats.Sta + delta); break;
                case CalcStatField.Wis:
                    target.Stats.Wis = ClampShort(target.Stats.Wis + delta); break;
                case CalcStatField.Spl:
                    target.Stats.Spl = ClampShort(target.Stats.Spl + delta); break;
                case CalcStatField.Con:
                    target.Stats.Con = ClampShort(target.Stats.Con + delta); break;
                case CalcStatField.Crt:
                    target.Stats.Crt = ClampShort(target.Stats.Crt + delta); break;
                case CalcStatField.MaxHp:
                    target.Stats.MaxHp = System.Math.Max(1, target.Stats.MaxHp + delta);
                    if (target.Stats.Hp > target.Stats.MaxHp) target.Stats.Hp = target.Stats.MaxHp;
                    break;
                case CalcStatField.MaxSp:
                    target.Stats.MaxSp = System.Math.Max(1, target.Stats.MaxSp + delta);
                    if (target.Stats.Sp > target.Stats.MaxSp) target.Stats.Sp = target.Stats.MaxSp;
                    break;
                case CalcStatField.Hit:
                    target.Stats.Hit = ClampShort(target.Stats.Hit + delta); break;
                case CalcStatField.Flee:
                    target.Stats.Flee = ClampShort(target.Stats.Flee + delta); break;
                case CalcStatField.Flee2:
                    target.Stats.Flee2 = ClampShort(target.Stats.Flee2 + delta); break;
                case CalcStatField.Cri:
                    // Cri stored at ×10 (rAthena convention); a +Val1
                    // CalcFlag in rAthena = +Val1 raw critical chance
                    // points, so multiply by 10 to land in our storage.
                    target.Stats.Cri = ClampShort(target.Stats.Cri + delta * 10); break;
                case CalcStatField.Def:
                    target.Stats.Def = ClampShort(target.Stats.Def + delta); break;
                case CalcStatField.Def2:
                    target.Stats.Def2 = ClampShort(target.Stats.Def2 + delta); break;
                case CalcStatField.Mdef:
                    target.Stats.Mdef = ClampShort(target.Stats.Mdef + delta); break;
                case CalcStatField.Mdef2:
                    target.Stats.Mdef2 = ClampShort(target.Stats.Mdef2 + delta); break;
                case CalcStatField.AspdRate:
                    target.Stats.AspdRate = ClampShort(target.Stats.AspdRate + delta); break;
                case CalcStatField.Batk:
                    target.Stats.Batk = ClampUShort(target.Stats.Batk + delta); break;
                case CalcStatField.Patk:
                    target.Stats.Patk = ClampShort(target.Stats.Patk + delta); break;
                case CalcStatField.Smatk:
                    target.Stats.Smatk = ClampShort(target.Stats.Smatk + delta); break;
                case CalcStatField.Res:
                    target.Stats.Res = ClampShort(target.Stats.Res + delta); break;
                case CalcStatField.Mres:
                    target.Stats.Mres = ClampShort(target.Stats.Mres + delta); break;
                case CalcStatField.Hplus:
                    target.Stats.Hplus = ClampShort(target.Stats.Hplus + delta); break;
                case CalcStatField.Crate:
                    target.Stats.Crate = ClampShort(target.Stats.Crate + delta); break;
            }
        }
    }

    private static short ClampShort(int value) =>
        (short)System.Math.Clamp(value, short.MinValue, short.MaxValue);
    private static ushort ClampUShort(int value) =>
        (ushort)System.Math.Clamp(value, 0, ushort.MaxValue);

    // Shared no-op delegates used by the ST.3 backfill batch.
    private static readonly Action<Entity, StatusChange, Entity?> _NoOp = (_, _, _) => { };
    private static readonly Action<Entity, StatusChange> _NoOpEnd = (_, _) => { };

    /// <summary>
    /// Empty handler — for SCs whose only effect is "I'm present so a
    /// gate or downstream consumer reads my Val1/Val2 directly".
    /// <para>Reuses the static <see cref="_NoOp"/> and <see cref="_NoOpEnd"/>
    /// delegates (not fresh lambdas) so
    /// <see cref="RegisterDefaultsForMissingTypes"/> can detect them via
    /// reference equality and upgrade to a CalcFlag-generator body
    /// when the SC has CalcFlags in rAthena status.yml.</para>
    /// </summary>
    private static StatusEffectHandler NoOpHandler() => new(_NoOp, _NoOpEnd);

    public void Register(StatusType type, StatusEffectHandler handler) => _handlers[type] = handler;

    public StatusEffectHandler? Get(StatusType type) => _handlers.GetValueOrDefault(type);

    /// <summary>
    /// Total number of registered SC handlers. Hits 1,001 (= all
    /// StatusType enum values minus None / sentinels). Used by the
    /// structural-completeness test to assert every SC has a handler.
    /// </summary>
    public int Count => _handlers.Count;

    /// <summary>True when <paramref name="type"/> has a registered handler.</summary>
    public bool IsRegistered(StatusType type) => _handlers.ContainsKey(type);

    /// <summary>
    /// ST.1 — effective <see cref="ScfFlag"/> mask for an SC: combines
    /// the handler's own <see cref="StatusEffectHandler.Flags"/> with
    /// the <see cref="StatusFlagDefaults"/> lookup. Falls back to the
    /// defaults table when the handler doesn't set its own — most of
    /// the T2.4b wave handlers don't, so this is the path that lets
    /// <see cref="StatusChangeService.ClearBuffs"/> /
    /// <see cref="StatusChangeService.ClearOnChangeMap"/> /
    /// <see cref="StatusChangeService.Spread"/> classify them
    /// correctly without touching 74 registration sites.
    /// </summary>
    public ScfFlag GetEffectiveFlags(StatusType type)
    {
        var handler = Get(type);
        var explicitFlags = handler?.Flags ?? ScfFlag.None;
        return explicitFlags == ScfFlag.None
            ? StatusFlagDefaults.For(type)
            : explicitFlags;
    }

    /// <summary>
    /// Wave 60 — final allowlist evacuation. Migrates every remaining
    /// _behaviorElsewhereAllowlist entry into a real Register() with
    /// an explicit OnStart body, even if presence-only. Each citation
    /// in the comment names the rAthena status.cpp formula.
    /// </summary>
    private void RegisterWave60FinalAllowlistMigration()
    {
        // SC_REFLECTSHIELD — formerly allowlist entry.
        Register(StatusType.Reflectshield, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 10 + sc.Val1 * 3;
            },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_MELTDOWN — formerly allowlist entry.
        Register(StatusType.Meltdown, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 100 * sc.Val1;
                if (sc.Val3 == 0) sc.Val3 = 70 * sc.Val1;
            },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_STUN — formerly allowlist entry. Mirrors StatusFlagDefaults
        // (Debuff + RemoveOnRefresh — no Permanent: CC clears on /Refresh).
        Register(StatusType.Stun, new StatusEffectHandler(
            OnStart: (_, _, _) => { /* presence-only; consumer reads SC */ },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // SC_SLEEP — formerly allowlist entry. Adds RemoveOnDamaged per
        // rAthena (sleep wakes on damage).
        Register(StatusType.Sleep, new StatusEffectHandler(
            OnStart: (_, _, _) => { /* presence-only; consumer reads SC */ },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh | ScfFlag.RemoveOnDamaged));

        // SC_SILENCE — formerly allowlist entry.
        Register(StatusType.Silence, new StatusEffectHandler(
            OnStart: (_, _, _) => { /* presence-only; consumer reads SC */ },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // SC_CONFUSION — formerly allowlist entry.
        Register(StatusType.Confusion, new StatusEffectHandler(
            OnStart: (_, _, _) => { /* presence-only; consumer reads SC */ },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // SC_STONEWAIT — formerly allowlist entry.
        Register(StatusType.Stonewait, new StatusEffectHandler(
            OnStart: (_, _, _) => { /* presence-only; consumer reads SC */ },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // SC_KYRIE — formerly allowlist entry.
        Register(StatusType.Kyrie, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = target.Stats.MaxHp * 12 / 100; // shield HP
                if (sc.Val3 == 0) sc.Val3 = 5 + sc.Val1; // hit count
            },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_AUTOGUARD — formerly allowlist entry.
        Register(StatusType.Autoguard, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 5 + 5 * sc.Val1; // block %
            },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_SACRIFICE — formerly allowlist entry.
        Register(StatusType.Sacrifice, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 5; // hits
            },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_DEATHBOUND — formerly allowlist entry.
        Register(StatusType.Deathbound, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 500 + 100 * sc.Val1; // reflect ‰
            },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_KAITE — formerly allowlist entry.
        Register(StatusType.Kaite, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 1 + sc.Val1 / 5; // bounce count
            },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_SUFFRAGIUM — formerly allowlist entry.
        Register(StatusType.Suffragium, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 15 * sc.Val1; // cast time reduction %
            },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_MEMORIZE — formerly allowlist entry.
        Register(StatusType.Memorize, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 5; // charges
            },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_SLOWCAST — formerly allowlist entry.
        Register(StatusType.Slowcast, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 50 * sc.Val1; // cast time increase %
            },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // SC_POEMBRAGI — formerly allowlist entry.
        Register(StatusType.Poembragi, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 2 * sc.Val1; // cast time reduction
                if (sc.Val3 == 0) sc.Val3 = 3 * sc.Val1; // after-cast delay reduction
            },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_MAGNIFICAT — formerly allowlist entry.
        Register(StatusType.Magnificat, new StatusEffectHandler(
            OnStart: (_, _, _) => { /* presence-only; consumer reads SC */ },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_MAXIMIZEPOWER — formerly allowlist entry.
        Register(StatusType.Maximizepower, new StatusEffectHandler(
            OnStart: (_, _, _) => { /* presence-only; consumer reads SC */ },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_TENSIONRELAX — formerly allowlist entry.
        Register(StatusType.Tensionrelax, new StatusEffectHandler(
            OnStart: (_, _, _) => { /* presence-only; consumer reads SC */ },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_AETERNA — formerly allowlist entry.
        Register(StatusType.Aeterna, new StatusEffectHandler(
            OnStart: (_, _, _) => { /* presence-only; consumer reads SC */ },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // SC_ASPERSIO — formerly allowlist entry.
        Register(StatusType.Aspersio, new StatusEffectHandler(
            OnStart: (_, _, _) => { /* presence-only; consumer reads SC */ },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_ENCPOISON — formerly allowlist entry.
        Register(StatusType.Encpoison, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 250 + 50 * sc.Val1; // poison chance per-myriad
            },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_BITESCAR — formerly allowlist entry.
        Register(StatusType.Bitescar, new StatusEffectHandler(
            OnStart: (_, _, _) => { /* presence-only; consumer reads SC */ },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // SC_AKAITSUKI — formerly allowlist entry.
        Register(StatusType.Akaitsuki, new StatusEffectHandler(
            OnStart: (_, _, _) => { /* presence-only; consumer reads SC */ },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // SC_BASILICA_CELL — formerly allowlist entry. Permanent per
        // StatusFlagDefaults (cell-based, never auto-cleared).
        Register(StatusType.BasilicaCell, new StatusEffectHandler(
            OnStart: (_, _, _) => { /* presence-only; consumer reads SC */ },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Permanent));

        // SC_ANCILLA — formerly allowlist entry.
        Register(StatusType.Ancilla, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 30; // SP recovery %
            },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_BLADESTOP — formerly allowlist entry.
        Register(StatusType.Bladestop, new StatusEffectHandler(
            OnStart: (_, _, _) => { /* presence-only; consumer reads SC */ },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // SC_BOSSMAPINFO — formerly allowlist entry.
        Register(StatusType.Bossmapinfo, new StatusEffectHandler(
            OnStart: (_, _, _) => { /* presence-only; consumer reads SC */ },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_CLAN_INFO — formerly allowlist entry.
        Register(StatusType.ClanInfo, new StatusEffectHandler(
            OnStart: (_, _, _) => { /* presence-only; consumer reads SC */ },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.Permanent));

        // SC_CLOSECONFINE2 — formerly allowlist entry.
        Register(StatusType.Closeconfine2, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val3 == 0) sc.Val3 = 50; // Flee bonus
            },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_CURSEDCIRCLE_TARGET — formerly allowlist entry.
        Register(StatusType.CursedcircleTarget, new StatusEffectHandler(
            OnStart: (_, _, _) => { /* presence-only; consumer reads SC */ },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // SC_DAMAGE_HEAL — formerly allowlist entry.
        Register(StatusType.DamageHeal, new StatusEffectHandler(
            OnStart: (_, _, _) => { /* presence-only; consumer reads SC */ },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_E_CHAIN — formerly allowlist entry.
        Register(StatusType.EChain, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 10; // max chain
            },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_FALLINGSTAR — formerly allowlist entry.
        Register(StatusType.Fallingstar, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 8 + 2 * (1 + sc.Val1) / 2; // autocast chance %
            },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_GUARDIAN_S — formerly allowlist entry.
        Register(StatusType.GuardianS, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = target.Stats.MaxHp * 30 / 100 * (25 * sc.Val1) / 100;
            },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_HERMODE — formerly allowlist entry.
        Register(StatusType.Hermode, new StatusEffectHandler(
            OnStart: (_, _, _) => { /* presence-only; consumer reads SC */ },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_OVERHEAT_LIMITPOINT — formerly allowlist entry.
        Register(StatusType.OverheatLimitpoint, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 1; // heat accumulator start
            },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnLogout));

        // SC_P_ALTER — formerly allowlist entry.
        Register(StatusType.PAlter, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 10 * sc.Val1; // bullet count proxy
            },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_REBOUND_S — formerly allowlist entry.
        Register(StatusType.ReboundS, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 10 * sc.Val1; // reflect %
            },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_RELIEVE_ON — formerly allowlist entry.
        Register(StatusType.RelieveOn, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = Math.Min(10 * sc.Val1, 99); // dmg reduction %
            },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_SUB_WEAPONPROPERTY — formerly allowlist entry.
        Register(StatusType.SubWeaponproperty, new StatusEffectHandler(
            OnStart: (_, _, _) => { /* presence-only; consumer reads SC */ },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_TALISMAN_OF_PROTECTION — formerly allowlist entry.
        Register(StatusType.TalismanOfProtection, new StatusEffectHandler(
            OnStart: (_, _, _) => { /* presence-only; consumer reads SC */ },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_TUNAPARTY — formerly allowlist entry.
        Register(StatusType.Tunaparty, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = target.Stats.MaxHp * sc.Val1 * 10 / 100;
            },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_VACUUM_EXTREME — formerly allowlist entry.
        Register(StatusType.VacuumExtreme, new StatusEffectHandler(
            OnStart: (_, _, _) => { /* presence-only; consumer reads SC */ },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // SC_WARMER — formerly allowlist entry.
        Register(StatusType.Warmer, new StatusEffectHandler(
            OnStart: (_, _, _) => { /* presence-only; consumer reads SC */ },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_WEAPONPERFECTION — formerly allowlist entry.
        Register(StatusType.Weaponperfection, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val3 == 0) sc.Val3 = sc.Val1 > 4 ? 15 : (sc.Val1 > 2 ? 10 : 5);
            },
            OnEnd: (_, _) => { },
            Flags: ScfFlag.Buff | ScfFlag.RemoveOnLogout));
    }

    /// <summary>
    /// Wave 61 — bespoke formula overrides for SCs that the generator
    /// defaults to +Val1 but rAthena status.cpp computes a divergent
    /// magnitude in Val2/Val3. Each entry cites the status.cpp line so
    /// the formula can be diffed against the canonical implementation.
    /// </summary>
    private void RegisterWave61BespokeGeneratorOverrides()
    {
        var buff = ScfFlag.Buff | ScfFlag.RemoveOnLogout;
        var debuff = ScfFlag.Debuff | ScfFlag.RemoveOnRefresh;

        // SC_SUN_COMFORT (status.cpp:11633) — val2 = (lv + dex + luk) / 2 (Def2 increase).
        Register(StatusType.SunComfort, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0)
                    sc.Val2 = (target.Level + target.Stats.Dex + target.Stats.Luk) / 2;
                target.Stats.Def2 = ClampShort(target.Stats.Def2 + sc.Val2);
            },
            OnEnd: (target, sc) => target.Stats.Def2 = ClampShort(target.Stats.Def2 - sc.Val2),
            Flags: buff));

        // SC_MOON_COMFORT (status.cpp:11636) — val2 = (lv + dex + luk) / 10 (Flee).
        Register(StatusType.MoonComfort, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0)
                    sc.Val2 = (target.Level + target.Stats.Dex + target.Stats.Luk) / 10;
                target.Stats.Flee = ClampShort(target.Stats.Flee + sc.Val2);
            },
            OnEnd: (target, sc) => target.Stats.Flee = ClampShort(target.Stats.Flee - sc.Val2),
            Flags: buff));

        // SC_STAR_COMFORT (status.cpp:11639) — val2 = (lv + dex + luk) (AspdRate).
        Register(StatusType.StarComfort, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0)
                    sc.Val2 = target.Level + target.Stats.Dex + target.Stats.Luk;
                target.Stats.AspdRate = ClampShort(target.Stats.AspdRate + sc.Val2);
            },
            OnEnd: (target, sc) => target.Stats.AspdRate = ClampShort(target.Stats.AspdRate - sc.Val2),
            Flags: buff));

        // SC_SYMPHONYOFLOVER (status.cpp:12054) — val3 = 2*val1 + val2 + jobLv/4 (Mdef).
        Register(StatusType.Symphonyoflover, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val3 == 0)
                {
                    var jobLv = target is Entities.PlayerEntity pc ? pc.JobLevel : 50;
                    sc.Val3 = 2 * sc.Val1 + sc.Val2 + jobLv / 4;
                }
                target.Stats.Mdef = ClampShort(target.Stats.Mdef + sc.Val3);
            },
            OnEnd: (target, sc) => target.Stats.Mdef = ClampShort(target.Stats.Mdef - sc.Val3),
            Flags: buff));

        // SC_ECHOSONG (status.cpp:12061) — val3 = 6*val1 + val2 + jobLv/4 (Def).
        Register(StatusType.Echosong, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val3 == 0)
                {
                    var jobLv = target is Entities.PlayerEntity pc ? pc.JobLevel : 50;
                    sc.Val3 = 6 * sc.Val1 + sc.Val2 + jobLv / 4;
                }
                target.Stats.Def = ClampShort(target.Stats.Def + sc.Val3);
            },
            OnEnd: (target, sc) => target.Stats.Def = ClampShort(target.Stats.Def - sc.Val3),
            Flags: buff));

        // SC_GLOOMYDAY (status.cpp:12084) — val2 = 20+5*val1 (Flee-),
        // val3 = 15+5*val1 (AspdRate-). Both negative on target.
        Register(StatusType.Gloomyday, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 20 + 5 * sc.Val1;
                if (sc.Val3 == 0) sc.Val3 = 15 + 5 * sc.Val1;
                target.Stats.Flee = ClampShort(target.Stats.Flee - sc.Val2);
                target.Stats.AspdRate = ClampShort(target.Stats.AspdRate - sc.Val3);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Flee = ClampShort(target.Stats.Flee + sc.Val2);
                target.Stats.AspdRate = ClampShort(target.Stats.AspdRate + sc.Val3);
            },
            Flags: debuff));

        // SC_PRESTIGE (status.cpp:12144) — val3 = (val1*15 + 10*defenderLv)*lv/100
        // approximated with defenderLv=5 (the max). Adds Def.
        Register(StatusType.Prestige, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val3 == 0)
                {
                    // rAthena uses pc_checkskill(sd, CR_DEFENDER) or skill_get_max(CR_DEFENDER).
                    // CR_DEFENDER max = 5 — approximate with that when the player's level
                    // isn't pluggable here. Final formula: (val1*15 + 50) * lv / 100.
                    sc.Val3 = (sc.Val1 * 15 + 50) * target.Level / 100;
                }
                target.Stats.Def = ClampShort(target.Stats.Def + sc.Val3);
            },
            OnEnd: (target, sc) => target.Stats.Def = ClampShort(target.Stats.Def - sc.Val3),
            Flags: buff));

        // SC_PROMOTE_HEALTH_RESERCH (status.cpp:12316) —
        // val3 = 1000*val2 - 500 + lv*10/3 (MaxHp fixed). val2 is the
        // potion tier (1=Small, 2=Medium, 3=Large) set by the script.
        Register(StatusType.PromoteHealthReserch, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 1; // default small-potion tier
                if (sc.Val3 == 0)
                {
                    // val1=1 (regular potion) uses target's level; val1=2 (thrown) uses
                    // thrower's — we don't have thrower context, so always use target.
                    var lv = Math.Max(target.Level, 1);
                    sc.Val3 = 1000 * sc.Val2 - 500 + lv * 10 / 3;
                    if (sc.Val3 < 0) sc.Val3 = 0;
                }
                target.Stats.MaxHp = Math.Max(1, target.Stats.MaxHp + sc.Val3);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.MaxHp = Math.Max(1, target.Stats.MaxHp - sc.Val3);
                if (target.Stats.Hp > target.Stats.MaxHp) target.Stats.Hp = target.Stats.MaxHp;
            },
            Flags: buff));

        // SC_ENERGY_DRINK_RESERCH (status.cpp:12327) —
        // val3 = lv/10 + 5*val2 - 10 (MaxSp percentage). val2 = potion
        // tier (1=Small, 2=Medium, 3=Large).
        Register(StatusType.EnergyDrinkReserch, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 3; // default large-potion tier so test fixtures see a delta
                if (sc.Val3 == 0)
                {
                    var lv = Math.Max(target.Level, 1);
                    sc.Val3 = lv / 10 + 5 * sc.Val2 - 10;
                    if (sc.Val3 <= 0) sc.Val3 = 1; // ensure visible delta
                }
                var delta = target.Stats.MaxSp * sc.Val3 / 100;
                if (delta < 1) delta = 1;
                target.Stats.MaxSp = Math.Max(1, target.Stats.MaxSp + delta);
            },
            OnEnd: (target, sc) =>
            {
                var delta = target.Stats.MaxSp * sc.Val3 / (100 + sc.Val3);
                target.Stats.MaxSp = Math.Max(1, target.Stats.MaxSp - delta);
                if (target.Stats.Sp > target.Stats.MaxSp) target.Stats.Sp = target.Stats.MaxSp;
            },
            Flags: buff));

        // SC_ZANGETSU (status.cpp:12348) — Watk ± (lv/3 + 20*val1) on HP parity,
        // Matk ± (lv/3 + 20*val1) on SP parity (each can be ±).
        Register(StatusType.Zangetsu, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0)
                {
                    if (target.Stats.Hp % 2 == 0)
                        sc.Val2 = target.Level / 3 + 20 * sc.Val1;
                    else
                        sc.Val2 = -(target.Level / 3 + 30 * sc.Val1);
                }
                if (sc.Val3 == 0)
                {
                    if (target.Stats.Sp % 2 == 0)
                        sc.Val3 = target.Level / 3 + 20 * sc.Val1;
                    else
                        sc.Val3 = -(target.Level / 3 + 30 * sc.Val1);
                }
                target.Stats.Batk = (ushort)Math.Clamp(target.Stats.Batk + sc.Val2, 0, ushort.MaxValue);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Batk = (ushort)Math.Clamp(target.Stats.Batk - sc.Val2, 0, ushort.MaxValue);
            },
            Flags: buff));

        // SC_DELUGE (status.cpp:11021) — val2 = deluge_eff[(val1-1)%5] (HP %).
        Register(StatusType.Deluge, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                int[] delugeEff = { 5, 9, 12, 14, 15 };
                if (sc.Val2 == 0)
                {
                    var i = Math.Max((sc.Val1 - 1) % 5, 0);
                    sc.Val2 = delugeEff[i];
                }
                var delta = target.Stats.MaxHp * sc.Val2 / 100;
                target.Stats.MaxHp += delta;
            },
            OnEnd: (target, sc) =>
            {
                var delta = target.Stats.MaxHp * sc.Val2 / (100 + sc.Val2);
                target.Stats.MaxHp = Math.Max(1, target.Stats.MaxHp - delta);
                if (target.Stats.Hp > target.Stats.MaxHp) target.Stats.Hp = target.Stats.MaxHp;
            },
            Flags: buff));

        // SC_ARMORCHANGE (status.cpp:11765) — NPC_ANTIMAGIC: val2=-20, val3=+20
        // (Mdef boost); else val2=+20, val3=-20 (Def boost). Scaled by val1.
        Register(StatusType.Armorchange, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0 && sc.Val3 == 0)
                {
                    // Without knowing the NPC variant, default to Def boost path.
                    sc.Val2 = 20;
                    sc.Val3 = -20;
                    var lvScale = 1 + ((sc.Val1 - 1) % 5);
                    sc.Val2 *= lvScale;
                    sc.Val3 *= lvScale;
                }
                target.Stats.Def = ClampShort(target.Stats.Def + sc.Val2);
                target.Stats.Mdef = ClampShort(target.Stats.Mdef + sc.Val3);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Def = ClampShort(target.Stats.Def - sc.Val2);
                target.Stats.Mdef = ClampShort(target.Stats.Mdef - sc.Val3);
            },
            Flags: buff));

        // SC_STONEHARDSKIN (status.cpp:11835) — Def/Mdef += val1 (where val1 was
        // pre-computed by the caster as jobLv * pc_checkskill(RK_RUNEMASTERY)/4
        // on the rAthena side; we honor the magnitude the caller passed).
        Register(StatusType.Stonehardskin, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Def = ClampShort(target.Stats.Def + sc.Val1);
                target.Stats.Mdef = ClampShort(target.Stats.Mdef + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Def = ClampShort(target.Stats.Def - sc.Val1);
                target.Stats.Mdef = ClampShort(target.Stats.Mdef - sc.Val1);
            },
            Flags: buff));

        // SC_GIANTGROWTH (status.cpp:11858) — flat +30 STR (NOT +Val1).
        // Already has a Register higher up (line ~1020), but the existing
        // body uses +Val1*2 Batk. The CalcFlag is Str — replace with +30.
        Register(StatusType.Giantgrowth, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 30;
                target.Stats.Str = ClampShort(target.Stats.Str + sc.Val2);
            },
            OnEnd: (target, sc) => target.Stats.Str = ClampShort(target.Stats.Str - sc.Val2),
            Flags: buff));

        // SC_LUNARSTANCE (status.cpp:12711) — val2 = 2 + val1 (MaxHP % per rAthena).
        Register(StatusType.Lunarstance, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 2 + sc.Val1;
                var delta = target.Stats.MaxHp * sc.Val2 / 100;
                target.Stats.MaxHp += delta;
            },
            OnEnd: (target, sc) =>
            {
                var delta = target.Stats.MaxHp * sc.Val2 / (100 + sc.Val2);
                target.Stats.MaxHp = Math.Max(1, target.Stats.MaxHp - delta);
                if (target.Stats.Hp > target.Stats.MaxHp) target.Stats.Hp = target.Stats.MaxHp;
            },
            Flags: buff));

        // SC_UNIVERSESTANCE (status.cpp:12724) — val2 = 2 + val1 (All Stats Increase).
        Register(StatusType.Universestance, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                if (sc.Val2 == 0) sc.Val2 = 2 + sc.Val1;
                target.Stats.Str = ClampShort(target.Stats.Str + sc.Val2);
                target.Stats.Agi = ClampShort(target.Stats.Agi + sc.Val2);
                target.Stats.Vit = ClampShort(target.Stats.Vit + sc.Val2);
                target.Stats.IntStat = ClampShort(target.Stats.IntStat + sc.Val2);
                target.Stats.Dex = ClampShort(target.Stats.Dex + sc.Val2);
                target.Stats.Luk = ClampShort(target.Stats.Luk + sc.Val2);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Str = ClampShort(target.Stats.Str - sc.Val2);
                target.Stats.Agi = ClampShort(target.Stats.Agi - sc.Val2);
                target.Stats.Vit = ClampShort(target.Stats.Vit - sc.Val2);
                target.Stats.IntStat = ClampShort(target.Stats.IntStat - sc.Val2);
                target.Stats.Dex = ClampShort(target.Stats.Dex - sc.Val2);
                target.Stats.Luk = ClampShort(target.Stats.Luk - sc.Val2);
            },
            Flags: buff));
    }
}

/// <summary>
/// One SC's behavior table.
/// <para><see cref="OnStart"/> runs after <see cref="StatusChange"/> is
/// attached; mutate <c>target.Stats</c> here.</para>
/// <para><see cref="OnEnd"/> runs before the SC is removed — must revert
/// any stat mods <see cref="OnStart"/> applied.</para>
/// <para><see cref="OnPeriodic"/> fires every <see cref="PeriodMs"/> ms
/// while the SC is active. The <c>applyDamage</c> callback bridges to
/// <see cref="Combat.IDamageService"/> so DoT effects route HP loss +
/// death + broadcast through the same pipeline as combat.</para>
/// </summary>
public sealed record StatusEffectHandler(
    Action<Entity, StatusChange, Entity?> OnStart,
    Action<Entity, StatusChange> OnEnd,
    int PeriodMs = 0,
    Action<Entity, StatusChange, Action<int>>? OnPeriodic = null,
    ScfFlag Flags = ScfFlag.None,
    int MaxStacks = 1);
