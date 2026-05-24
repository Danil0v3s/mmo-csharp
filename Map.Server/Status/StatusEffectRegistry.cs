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
        // CombatMarkerHandler with explicit consumer citation
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
        // CombatMarkerHandler registrations in wave 4a/5a with their
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
        Register(StatusType.Spirit, new StatusEffectHandler(
            OnStart: (_, _, _) => { },
            OnEnd: (_, _) => { },
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
        // val2 = 100*val1 weapon-break chance, val3 = 70*val1 armor-break
        // chance. Both are combat-side procs read on hit; no direct
        // stat-mod. Override as combat-marker (defeat any future generator
        // synthesis).
        Register(StatusType.Meltdown, CombatMarkerHandler(
            ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_REFLECTSHIELD (CR_REFLECTSHIELD) — rAthena status.cpp:
        // 10587-10602: val2 = 10+val1*3 reflect %. Combat-side proc
        // reads val2 on damage hit. No stat-mod. Earlier line 393
        // registered NoOpHandler (shared _NoOp) — not in CalcFlagDefaults
        // so safe from upgrade, but make explicit override defensible.
        Register(StatusType.Reflectshield, CombatMarkerHandler(
            ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_PROVIDENCE (CR_PROVIDENCE) — rAthena status.cpp:10584-10586
        // (val2 = val1*5 race/ele resist) + 4788-4790 (status_calc_pc
        // adds val2 to subele[HOLY] and subrace[DEMON]). Generator
        // upgrades the earlier NoOp registration with +Val1 to all 6
        // base stats (totally wrong — Providence doesn't touch stats).
        // Override with combat-marker so the upgrade is defeated.
        Register(StatusType.Providence, CombatMarkerHandler(
            ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_HIDING (TF_HIDING) — visibility marker. Generator: +Val1
        // AspdRate (semi-OK proxy for the walk-speed change but
        // direction is opposite — hiding SLOWS you). Override with
        // combat-marker; the visibility hook handles the real semantics.
        Register(StatusType.Hiding, CombatMarkerHandler(
            ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_CLOAKING (AS_CLOAKING) — visibility marker. Generator
        // emits +Val1 Cri + AspdRate (Cri is wrong; cloaking has speed
        // adjustment driven by val3, NOT a flat Crit boost). Override.
        Register(StatusType.Cloaking, CombatMarkerHandler(
            ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_EDP (ASC_EDP — Enchant Deadly Poison) — rAthena status.cpp:
        // 10522-10535: val2 = (val1+1)/2 + 2 poison chance %; val3 =
        // 50*(val1+1) damage increase % (pre-renewal). Combat reads val3
        // for the damage boost on poison-element hits. Generator emits
        // +Val1 Batk (wrong field — it's a damage% mod, not a flat batk
        // bump). Override.
        Register(StatusType.Edp, CombatMarkerHandler(
            ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_STEELBODY (MO_STEELBODY) — 90% damage reduction (phys+magic),
        // applied combat-side via DamageService SC presence check.
        // Generator: +Val1 Def+Mdef+AspdRate (wrong — steel body's
        // semantics are a damage CAP, not stat-mod). Override.
        Register(StatusType.Steelbody, CombatMarkerHandler(
            ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_SATURDAYNIGHTFEVER (WM_SATURDAY_NIGHT_FEVER) — Sura SC.
        // Generator emits +Val1 Hit+Flee — but rAthena's spec is "heal
        // suppressed + always 0 cure animation". No direct stat-mod.
        Register(StatusType.Saturdaynightfever, CombatMarkerHandler(
            ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // ---- (c) Cast-time SCs: combat-marker overrides ----
        //
        // SkillCastTimingService.CastFixSc reads val1/val2/val3 directly.
        // Generator-synthesized stat-mod bodies would mutate unrelated
        // fields; defeat the upgrade so the SC stays presence-only.

        // SC_PARALYSIS (Guillotine Cross) — val3 = +cast rate %.
        // Generator: Def2 (wrong field).
        Register(StatusType.Paralysis, CombatMarkerHandler(
            ScfFlag.Debuff | ScfFlag.RemoveOnRefresh));

        // SC_IZAYOI (Kagerou/Oboro) — halves variable cast time.
        // Generator: +Val1 Batk (wrong field).
        Register(StatusType.Izayoi, CombatMarkerHandler(
            ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // ---- (d) Weapon endow family: combat-marker overrides ----
        //
        // SC_{Fire,Water,Wind,Earth}WEAPON / SC_ASPERSIO / SC_ENCPOISON —
        // weapon-element overrides read by damage pipeline. Generator
        // assigns +Val1 to all 6 base stats for the WEAPON variants
        // (status.yml's "All" CalcFlag); rAthena's actual semantics are
        // pure element-override markers (val1 = element, val2 = duration).

        var endowFlags = ScfFlag.Buff | ScfFlag.RemoveOnLogout;
        Register(StatusType.Fireweapon, CombatMarkerHandler(endowFlags));
        Register(StatusType.Waterweapon, CombatMarkerHandler(endowFlags));
        Register(StatusType.Windweapon, CombatMarkerHandler(endowFlags));
        Register(StatusType.Earthweapon, CombatMarkerHandler(endowFlags));

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
        Register(StatusType.Stripweapon, CombatMarkerHandler(stripFlags));
        Register(StatusType.Stripshield, CombatMarkerHandler(stripFlags));
        Register(StatusType.Striparmor, CombatMarkerHandler(stripFlags));
        Register(StatusType.Striphelm, CombatMarkerHandler(stripFlags));

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
        // Soulshadow had CalcFlags AspdRate+Cri in defaults.
        Register(StatusType.Soulshadow, CombatMarkerHandler(soulLink2));
        // Soulfalcon had Batk+Hit.
        Register(StatusType.Soulfalcon, CombatMarkerHandler(soulLink2));
        // Soulgolem had Def+Mdef.
        Register(StatusType.Soulgolem, CombatMarkerHandler(soulLink2));
        // Soulenergy had Batk.
        Register(StatusType.Soulenergy, CombatMarkerHandler(soulLink2));
        // Soulfairy had Batk.
        Register(StatusType.Soulfairy, CombatMarkerHandler(soulLink2));
        // Soulcold had Agi.
        Register(StatusType.Soulcold, CombatMarkerHandler(soulLink2));

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
        //   (b) a CombatMarkerHandler() with explicit reader-side
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
        // Each call uses CombatMarkerHandler with the ScfFlag classifying
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
        Register(StatusType.Richmankim, CombatMarkerHandler(
            ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_NIBELUNGEN (BD_RINGNIBELUNGEN) — status.cpp:10725-10727
        // val2 = rnd() % RINGNBL_MAX (random elemental ring effect type).
        // Combat-side read. Generator: +Val1 to all 6 base stats (wrong).
        Register(StatusType.Nibelungen, CombatMarkerHandler(
            ScfFlag.Buff | ScfFlag.RemoveOnLogout));

        // SC_SIEGFRIED (BD_SIEGFRIED) — status.cpp:10728-10731
        // val2 = val1*3 Elemental Resistance, val3 = val1*5 status ailment
        // resistance. Combat-side reads. Generator: +Val1 to all 6 base
        // stats (wrong).
        Register(StatusType.Siegfried, CombatMarkerHandler(
            ScfFlag.Buff | ScfFlag.RemoveOnLogout));

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
        Register(StatusType.Marionette, CombatMarkerHandler(
            ScfFlag.Buff | ScfFlag.RemoveOnLogout));
        Register(StatusType.Marionette2, CombatMarkerHandler(
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
    ///   <see cref="CombatMarkerHandler"/> with an inline citation
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
        Register(StatusType.Signumcrucis, new StatusEffectHandler(
            OnStart: (_, sc, _) => { sc.Val2 = 10 + 4 * sc.Val1; },
            OnEnd: (_, _) => { },
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
        Register(StatusType.Stone, CombatMarkerHandler(ccDebuff));
        Register(StatusType.Freeze, CombatMarkerHandler(ccDebuff));
        Register(StatusType.Stun, CombatMarkerHandler(ccDebuff));
        Register(StatusType.Sleep, CombatMarkerHandler(ccDebuff));
        Register(StatusType.Silence, CombatMarkerHandler(ccDebuff));
        Register(StatusType.Confusion, CombatMarkerHandler(ccDebuff));
        Register(StatusType.Stonewait, CombatMarkerHandler(ccDebuff));

        var combatBuff = ScfFlag.Buff | ScfFlag.RemoveOnLogout;
        // SC_MAGNIFICAT (AL_MAGNIFICAT) — +50% SP regen renewal.
        // Consumer: NaturalHealService reads SC_MAGNIFICAT for regen
        // overlay (Map.Server/Status/NaturalHealService.cs).
        Register(StatusType.Magnificat, CombatMarkerHandler(combatBuff));

        // SC_MAXIMIZEPOWER (BS_MAXIMIZE) — weapon max-roll.
        // Consumer: BattleCalculator reads SC presence to force
        // damage roll to max in weapon-attack path
        // (Map.Server/Combat/BattleCalculator.cs).
        Register(StatusType.Maximizepower, CombatMarkerHandler(combatBuff));

        // SC_TENSIONRELAX (LK_TENSIONRELAX) — HP regen overlay.
        // Consumer: NaturalHealService HP overlay reads SC presence.
        Register(StatusType.Tensionrelax, CombatMarkerHandler(combatBuff));

        // SC_AETERNA (PR_LEXAETERNA) — next-hit-doubled debuff.
        // Consumer: damage pipeline checks SC_AETERNA on hit;
        // doubles damage then ends the SC.
        Register(StatusType.Aeterna, CombatMarkerHandler(ccDebuff));

        // SC_ASPERSIO (PR_ASPERSIO) — holy weapon endow.
        // Consumer: weapon-element resolver reads SC presence to override
        // weapon element (Map.Server/Combat/IBattleEffectsService.cs).
        Register(StatusType.Aspersio, CombatMarkerHandler(combatBuff));

        // SC_ENCPOISON (AS_ENCHANTPOISON) — poison weapon endow.
        // Consumer: same as Aspersio — weapon-element resolver.
        Register(StatusType.Encpoison, CombatMarkerHandler(combatBuff));

        // SC_BITESCAR (4th-class Sura DoT marker) — ends on heal.
        // Consumer: heal pipeline + damage pipeline read SC_BITESCAR
        // for tick damage (per-skill plugin gap; presence carries the
        // duration flag until consumer ports).
        Register(StatusType.Bitescar, CombatMarkerHandler(ccDebuff));

        // SC_AKAITSUKI (Sura) — next heal flipped to damage of equal magnitude.
        // Consumer: heal pipeline reads SC_AKAITSUKI on AL_HEAL apply.
        Register(StatusType.Akaitsuki, CombatMarkerHandler(combatBuff));

        // SC_BASILICA_CELL — stepped-on-Basilica-cell marker.
        // Permanent classification — never auto-cleared, only removed
        // when the PC steps off the Basilica cell.
        // Consumer: PlayerPositionHelpers.IsBasilicaCell + Cure script
        // gates (Map.Server/Movement/PlayerPositionHelpers.cs).
        Register(StatusType.BasilicaCell, CombatMarkerHandler(ScfFlag.Permanent));
    }

    // ====================================================================
    // NS-3 wave 5b — Class A family-grouped consumer wiring.
    //
    // Each family method below explicitly registers every SC in the
    // family with a CombatMarkerHandler that:
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
        Register(StatusType.Soulcollect, CombatMarkerHandler(soulBuff));

        // SC_SOULREAPER (Soul Reaper class spirit) — base spirit marker.
        // Already overridden in wave 4a; explicit re-register here to
        // group with family and document consumer chain.
        // Consumer: SoulReaperSoulCollect + soul-drain skill plugins.
        Register(StatusType.Soulreaper, CombatMarkerHandler(soulBuff));

        // SC_SOULUNITY (Soul Linker SL_SOULUNITY) — multi-target HP
        // share. Val1 = level. Consumer: SoulLinkerSoulUnityImpl reads
        // val2 = linked party member ids.
        Register(StatusType.Soulunity, CombatMarkerHandler(soulBuff));

        // SC_SOULDIVISION (Soul Linker SL_SOULDIVISION) — caster's
        // after-cast delay doubled debuff on target. Consumer: combat
        // delay path checks SC presence.
        Register(StatusType.Souldivision, CombatMarkerHandler(soulDebuff));

        // SC_SOULATTACK (Soul Reaper SOA_SOUL_ATTACK) — soul-attack
        // marker. Val1 = stored soul count. Consumer:
        // SoaSoulAttackImpl + damage pipeline read SC for damage
        // amplification.
        Register(StatusType.Soulattack, CombatMarkerHandler(soulBuff));

        // SC_SOULCURSE (Soul Reaper-targeted curse) — already
        // registered with debuff flags in ctor line 536; explicit
        // re-register here for family grouping.
        // Consumer: combat damage path applies curse magnitude.
        Register(StatusType.Soulcurse, CombatMarkerHandler(soulDebuff));
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
        Register(StatusType.Sunstance, CombatMarkerHandler(seBuff));
        Register(StatusType.Starstance, CombatMarkerHandler(seBuff));

        // SC_LIGHTOFSUN / SC_LIGHTOFMOON / SC_LIGHTOFSTAR — Star Emperor
        // Light* damage markers. Val1 = stack count consumed per
        // attack. Consumer: damage pipeline checks SC + decrements.
        Register(StatusType.Lightofsun, CombatMarkerHandler(seBuff));
        Register(StatusType.Lightofmoon, CombatMarkerHandler(seBuff));
        Register(StatusType.Lightofstar, CombatMarkerHandler(seBuff));

        // SC_MOONSTAR — Star Emperor + Soul Linker moonstar marker.
        // Consumer: Moonstar combo skill plugin reads SC for proc.
        Register(StatusType.Moonstar, CombatMarkerHandler(seBuff));

        // SC_SUNSET_SUN / SC_STAR_BURST — Star Emperor 4th-class.
        // Consumer: Star Emperor 4th-class skill plugins.
        Register(StatusType.SunsetSun, CombatMarkerHandler(seBuff));
        Register(StatusType.StarBurst, CombatMarkerHandler(seBuff));
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
        Register(StatusType.Reflectdamage, CombatMarkerHandler(rgBuff));

        // SC_BANDING (LG_BANDING) — multi-RG party stat boost. Val2 =
        // band member count. Consumer: per-RG party-share aggregator.
        Register(StatusType.Banding, CombatMarkerHandler(rgBuff));

        // SC_BANDING_DEFENCE — banding-derived defense overlay.
        // Consumer: damage defense math (LG_BANDING plugin emits).
        Register(StatusType.BandingDefence, CombatMarkerHandler(rgBuff));

        // SC_EARTHDRIVE (LG_EARTHDRIVE) — earth-element damage
        // multiplier marker. Val1 = level. Consumer: LG_EARTHDRIVE
        // skill plugin reads SC on next cast.
        Register(StatusType.Earthdrive, CombatMarkerHandler(rgBuff));

        // SC_INSPIRATION (LG_INSPIRATION) — major stat buff +
        // immunity to lvl up regen wipe. Has CalcFlags in status.yml
        // (generator gives +Val1 to base stats); explicit RG marker
        // here documents the per-skill consumer.
        Register(StatusType.Inspiration, CombatMarkerHandler(rgBuff));

        // SC_SHIELDSPELL_HP / SP / ATK (LG_SHIELDSPELL variants).
        // Val2 = HP/SP/ATK boost magnitude proc'd by Shield Spell.
        // Consumer: LG_SHIELDSPELL plugin reads val2 on attach.
        Register(StatusType.ShieldspellHp, CombatMarkerHandler(rgBuff));
        Register(StatusType.ShieldspellSp, CombatMarkerHandler(rgBuff));
        Register(StatusType.ShieldspellAtk, CombatMarkerHandler(rgBuff));

        // SC_HOVERING (NC_HOVERING — Mechanic, RG dispels via FAW).
        // Val1 = hover state. Consumer: Movement service reads SC to
        // disable terrain damage gates.
        Register(StatusType.Hovering, CombatMarkerHandler(rgBuff));
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
        Register(StatusType.Gensou, CombatMarkerHandler(suraBuff));

        // SC_CRESCENTELBOW (SR_CRESCENTELBOW) — Sura combo proc.
        // Val1 = level. Consumer: SrCrescentElbow plugin reads SC.
        Register(StatusType.Crescentelbow, CombatMarkerHandler(suraBuff));

        // SC_FALLEN_ANGEL (SR_FALLENEMPIRE follow-up) — combo gate.
        // Val1 = combo depth. Consumer: SrFallenEmpire plugin.
        Register(StatusType.FallenAngel, CombatMarkerHandler(suraBuff));

        // SC_TINDER_BREAKER / TINDER_BREAKER2 (SR_TINDER_BREAKER chain).
        // Val1 = chain level. Consumer: SrTinderBreaker plugin reads
        // SC to dispatch combo damage.
        Register(StatusType.TinderBreaker, CombatMarkerHandler(suraBuff));
        Register(StatusType.TinderBreaker2, CombatMarkerHandler(suraBuff));

        // SC_LIGHT_OF_REGENE (AB_LIGHTOFREGENE — Sura/Arch Bishop revival).
        // Val1 = revival HP %. Consumer: PcDeathService checks SC on
        // death for auto-revive.
        Register(StatusType.LightOfRegene, CombatMarkerHandler(suraBuff));
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
        Register(StatusType.Utsusemi, CombatMarkerHandler(ninjaBuff));

        // SC_BUNSINJYUTSU (NJ_BUNSINJYUTSU) — clone-block N attacks.
        // Val2 = remaining hits. Consumer: damage pipeline same as
        // Utsusemi but for magic.
        Register(StatusType.Bunsinjyutsu, CombatMarkerHandler(ninjaBuff));

        // SC_SUITON (NJ_SUITON) — water-floor cell marker. Val1 =
        // level, applied per-cell while standing on suiton unit. Slows
        // + boosts agi/water dmg. Consumer: SkillUnitTickRegistry tick
        // applies SC on cell entry; movement path reads SC for slow.
        Register(StatusType.Suiton, CombatMarkerHandler(ninjaDebuff));

        // SC_NEN (NJ_NEN) — auto-revive on death (1× consume). Val1 =
        // level. Consumer: PcDeathService checks SC on death; consumes
        // for revive + ends.
        Register(StatusType.Nen, CombatMarkerHandler(ninjaBuff));

        // SC_CURSEDCIRCLE_ATKER / TARGET (SR_CURSEDCIRCLE — Sura
        // cross-family). ATKER on caster, TARGET on each affected
        // entity. Val2 = circle id linking caster ↔ targets. Consumer:
        // combat path checks SC to enforce "must stand still" gate.
        Register(StatusType.CursedcircleAtker, CombatMarkerHandler(ninjaBuff));
        Register(StatusType.CursedcircleTarget, CombatMarkerHandler(ninjaDebuff));
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
        Register(StatusType.Heater, CombatMarkerHandler(sorcBuff));
        Register(StatusType.HeaterOption, CombatMarkerHandler(sorcBuff));

        // Tropic family (Fire stronger).
        Register(StatusType.Tropic, CombatMarkerHandler(sorcBuff));
        Register(StatusType.TropicOption, CombatMarkerHandler(sorcBuff));

        // Aquaplay family (Water).
        Register(StatusType.Aquaplay, CombatMarkerHandler(sorcBuff));
        Register(StatusType.AquaplayOption, CombatMarkerHandler(sorcBuff));

        // Cooler family (Water stronger).
        Register(StatusType.Cooler, CombatMarkerHandler(sorcBuff));
        Register(StatusType.CoolerOption, CombatMarkerHandler(sorcBuff));

        // ChillyAir family (Water cold).
        Register(StatusType.ChillyAir, CombatMarkerHandler(sorcBuff));
        Register(StatusType.ChillyAirOption, CombatMarkerHandler(sorcBuff));

        // Blast family (Wind).
        Register(StatusType.Blast, CombatMarkerHandler(sorcBuff));
        Register(StatusType.BlastOption, CombatMarkerHandler(sorcBuff));

        // WildStorm family (Wind stronger).
        Register(StatusType.WildStorm, CombatMarkerHandler(sorcBuff));
        Register(StatusType.WildStormOption, CombatMarkerHandler(sorcBuff));

        // Petrology family (Earth).
        Register(StatusType.Petrology, CombatMarkerHandler(sorcBuff));
        Register(StatusType.PetrologyOption, CombatMarkerHandler(sorcBuff));

        // CursedSoil family (Earth dark).
        Register(StatusType.CursedSoil, CombatMarkerHandler(sorcBuff));
        Register(StatusType.CursedSoilOption, CombatMarkerHandler(sorcBuff));
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

        // SC_MADNESSCANCEL (GS_MADNESSCANCEL) — fixed-ASPD + +Watk
        // buff. Val2 = stored ASPD bonus. Consumer: combat ASPD reader
        // applies fixed ASPD while SC active.
        Register(StatusType.Madnesscancel, CombatMarkerHandler(gsBuff));

        // SC_ADJUSTMENT (GS_ADJUSTMENT) — has CalcFlags (Hit + Flee).
        // NOT overridden here: generator's +Val1 default is correct
        // (rAthena status_calc adds val1 to Hit and val1 to Flee).
        // Leaving the generator body in place keeps stat-mod behavior
        // exact. Family-group consumer reader docs covered by the
        // GS_ADJUSTMENT entry in skill plugin folder.

        // SC_HEAT_BARREL (RL_HEAT_BARREL) — Rebellion bullet boost.
        // Val2 = stacked bullet count consumed per attack. Consumer:
        // Rebellion damage path reads val2 + decrements.
        Register(StatusType.HeatBarrel, CombatMarkerHandler(gsBuff));
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
        Register(StatusType.Hallucination, CombatMarkerHandler(gcDebuff));

        // SC_VENOMIMPRESS (GC_VENOMIMPRESS) — venom-element vuln.
        // Val2 = elemental damage % boost. Consumer: damage element
        // resolver reads val2 to amplify poison-element hits.
        Register(StatusType.Venomimpress, CombatMarkerHandler(gcDebuff));

        // GC New Poison family — each is a DoT/proc with rAthena-spec
        // val2 = damage interval, val3 = damage amount. Consumer:
        // IPcRegenService DoT overlay + Combat damage path.
        Register(StatusType.Toxin, CombatMarkerHandler(gcDebuff));
        Register(StatusType.Venombleed, CombatMarkerHandler(gcDebuff));
        Register(StatusType.Magicmushroom, CombatMarkerHandler(gcDebuff));
        Register(StatusType.Deathhurt, CombatMarkerHandler(gcDebuff));
        Register(StatusType.Pyrexia, CombatMarkerHandler(gcDebuff));
        Register(StatusType.Oblivioncurse, CombatMarkerHandler(gcDebuff));

        // SC_HALLUCINATIONWALK_POSTDELAY — post-cast cooldown marker
        // for GC_HALLUCINATIONWALK. Consumer: SkillCastTimingService
        // checks SC presence before allowing re-cast.
        Register(StatusType.HallucinationwalkPostdelay, CombatMarkerHandler(gcDebuff));
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
        Register(StatusType.Manhole, CombatMarkerHandler(scDebuff));

        // SC__BLOODYLUST — Shadow Chaser caster's damage % boost.
        // Val2 = damage %. Consumer: Combat damage path reads val2.
        Register(StatusType.Bloodylust, CombatMarkerHandler(scBuff));

        // SC__REPRODUCE — Shadow Chaser skill copy. Val2 = copied skill
        // id, val3 = level. Consumer: SkillCastService reads on cast.
        Register(StatusType.Reproduce, CombatMarkerHandler(scBuff));

        // SC__STRIPACCESSORY — Shadow Chaser strip accessory slot.
        // Equip-disable enforced by IEquipService while SC active.
        Register(StatusType.Stripaccessory, CombatMarkerHandler(scDebuff));
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
        Register(StatusType.GraniticArmor, CombatMarkerHandler(mgBuff));

        // SC_MAGMA_FLOW (NC_MAGMA_FLOW) — Mechanic ground unit cell
        // damage proc. Val2 = damage interval. Consumer:
        // SkillUnitTickRegistry tick + Combat damage path.
        Register(StatusType.MagmaFlow, CombatMarkerHandler(mgBuff));

        // SC_PYROCLASTIC (NC_PYROCLASTIC) — Mechanic fire weapon
        // endow + Atk boost. Val2 = atk + element. Consumer: weapon
        // element resolver + damage path.
        Register(StatusType.Pyroclastic, CombatMarkerHandler(mgBuff));

        // SC_MADOGEAR (NC_MADO mode) — Mechanic Madogear mode marker.
        // Val1 = Madogear type. Consumer: PlayerOptionService reads
        // SC for sprite + skill gating.
        Register(StatusType.Madogear, CombatMarkerHandler(mgBuff));

        // SC_HELLS_PLANT — Genetic ground unit. Val2 = plant id.
        // Consumer: SkillUnitTickRegistry tick + damage path.
        Register(StatusType.HellsPlant, CombatMarkerHandler(mgBuff));
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
        Register(StatusType.VacuumExtreme, CombatMarkerHandler(wlDebuff));

        // SC_VACUUM_EXTREME_POSTDELAY — post-cast cooldown marker.
        // Consumer: SkillCastTimingService checks for re-cast gate.
        Register(StatusType.VacuumExtremePostdelay, CombatMarkerHandler(wlDebuff));

        // SC_TEARGAS (HT_BLITZBEAT? Actually GC_TEARGAS) — DoT marker.
        // Val2 = tick damage interval. Consumer: damage path tick.
        Register(StatusType.Teargas, CombatMarkerHandler(wlDebuff));

        // SC_TEARGAS_SOB — TearGas-triggered "sob" anim follow-up.
        // Consumer: visual broadcast on tick.
        Register(StatusType.TeargasSob, CombatMarkerHandler(wlDebuff));

        // SC_BURNT — Mage burnt debuff marker (post-Fire DoT).
        // Consumer: damage path applies fire weakness.
        Register(StatusType.Burnt, CombatMarkerHandler(wlDebuff));
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
        Register(StatusType.Rushwindmill, CombatMarkerHandler(abBuff));

        // SC_SEVENWIND (BS_SEVENWIND? actually weapon-element endow).
        // Val2 = element id. Consumer: weapon element resolver.
        Register(StatusType.Sevenwind, CombatMarkerHandler(abBuff));
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
        Register(StatusType.Moonlitserenade, CombatMarkerHandler(wmBuff));

        // SC_LERADSDEW (WM_LERADSDEW) — Wanderer MaxHp boost song.
        // Val2 = MaxHp % boost. Consumer: status_calc_pc Hp path.
        Register(StatusType.Leradsdew, CombatMarkerHandler(wmBuff));

        // SC_LIGHTNINGWALK (WM_LIGHTNINGWALK) — Wanderer self-buff
        // teleport-on-attack. Val2 = trigger %. Consumer: Combat
        // damage path on incoming hit.
        Register(StatusType.Lightningwalk, CombatMarkerHandler(wmBuff));

        // Elemental option / curtain buffs — paired with elemental
        // spheres. Consumer: ElementalNpc skill plugins.
        Register(StatusType.WindStep, CombatMarkerHandler(wmBuff));
        Register(StatusType.WindStepOption, CombatMarkerHandler(wmBuff));
        Register(StatusType.WindCurtain, CombatMarkerHandler(wmBuff));
        Register(StatusType.WindCurtainOption, CombatMarkerHandler(wmBuff));
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
        Register(StatusType.MidnightMoon, CombatMarkerHandler(f4Buff));
        Register(StatusType.SkyEnchant, CombatMarkerHandler(f4Buff));
        Register(StatusType.ShinkirouCall, CombatMarkerHandler(f4Buff));

        // SC_WINDSIGN (Wind Hawk 4th class) — wind-element wind sphere.
        // Val1 = stored sphere. Consumer: WindHawk*.cs plugin.
        Register(StatusType.Windsign, CombatMarkerHandler(f4Buff));

        // SC_NIGHTMARE / NIGHT family — Night Watch 4th class.
        // Val1 = stored marker. Consumer: NightWatch*.cs plugin.
        Register(StatusType.Nightmare, CombatMarkerHandler(f4Buff));

        // SC_EARTH_CARE — 4th-class earth elemental care marker.
        // Consumer: ElementalNpc earth-care plugin.
        Register(StatusType.EarthCare, CombatMarkerHandler(f4Buff));
    }

    /// <summary>
    /// Wave 4a helper — combat/regen/cast presence-only marker.
    /// Uses fresh non-`_NoOp` lambdas so the NoOp-upgrade check in
    /// <see cref="RegisterDefaultsForMissingTypes"/> (reference-equality
    /// against the shared `_NoOp` delegate) skips synthesis. ScfFlag
    /// classification is preserved so lifecycle sweeps still route.
    /// </summary>
    private static StatusEffectHandler CombatMarkerHandler(ScfFlag flags) =>
        new StatusEffectHandler(
            OnStart: (_, _, _) => { /* combat-side reader does work */ },
            OnEnd:   (_, _) => { },
            Flags: flags);

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
