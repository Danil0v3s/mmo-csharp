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
        // EntityActionGates.CanAct / CanCastSkill checks for these SCs;
        // the registry just needs an entry so Start() can attach the SC.

        Register(StatusType.Stone, NoOpHandler());
        Register(StatusType.Freeze, NoOpHandler());
        Register(StatusType.Stun, NoOpHandler());
        Register(StatusType.Sleep, NoOpHandler());
        Register(StatusType.Curse, NoOpHandler());      // -75% Luk, halved MoveSpeed in rAthena
        Register(StatusType.Silence, NoOpHandler());    // blocks magic
        Register(StatusType.Confusion, NoOpHandler());  // blocks targeting
        Register(StatusType.Blind, NoOpHandler());      // -25% Hit/Flee (cosmetic for now)
        Register(StatusType.Stonewait, NoOpHandler());  // 5s petrify warmup

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

        // SC_ENDURE — hit-counter buff. Val1 = remaining hits before
        // expire (rAthena: 7 hits then OnEnd). Refresh-on-hit infra
        // ports later; for now the SC is a presence flag that combat
        // can read to suppress stagger.
        Register(StatusType.Endure, NoOpHandler());

        // SC_MAGNIFICAT — +val1*100 % SP regen (renewal +50 % at lv1).
        // No direct stat; we hold the marker — IPcRegenService reads it.
        Register(StatusType.Magnificat, NoOpHandler());

        // ===== Weapon-element endow (presence flags) =====
        // SC_FIREWEAPON / WATERWEAPON / WINDWEAPON / EARTHWEAPON —
        // damage-calc reads target.Stats.WeaponElement directly today;
        // these registry entries just let the SC attach for duration.
        Register(StatusType.Fireweapon, NoOpHandler());
        Register(StatusType.Waterweapon, NoOpHandler());
        Register(StatusType.Windweapon, NoOpHandler());
        Register(StatusType.Earthweapon, NoOpHandler());

        // ===== Defense buffs (presence flags) =====
        // SC_KYRIE — barrier with HP-shield value. Val1 = remaining
        // HP shield, Val2 = remaining hit count. Consumed by the
        // damage pipeline (TBD); registry entry just holds the slots.
        Register(StatusType.Kyrie, NoOpHandler());

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

        // SC_AUTOGUARD — % chance to block physical. Val1 = chance %.
        // Consumed by DamageService.ApplyScDamageReduction (T2.4b+).
        Register(StatusType.Autoguard, NoOpHandler());

        // SC_STRIPWEAPON / SHIELD / ARMOR / HELM — Rogue's Strip family.
        // Val1 = equip mask of the stripped slot; while attached the
        // PC can't re-equip until the SC ends. Item-side enforcement
        // ports when the unequip pipeline reads the SC marker on
        // EquipService.CanEquip — registry entry holds the duration.
        Register(StatusType.Stripweapon, NoOpHandler());
        Register(StatusType.Stripshield, NoOpHandler());
        Register(StatusType.Striparmor, NoOpHandler());
        Register(StatusType.Striphelm, NoOpHandler());

        // SC_HIDING — Thief Hiding (TF_HIDING). Presence flag; the
        // visibility hook (other entities can't target hidden players)
        // ports separately. We only need the SC slot for duration.
        Register(StatusType.Hiding, NoOpHandler());

        // SC_OVERTHRUST — Blacksmith Over Thrust (BS_OVERTHRUST).
        // Val1 = ATK % boost. Future damage-side hook reads it from
        // PlayerEntity.EquipBonuses-equivalent path; for now SC is
        // a presence + duration marker.
        Register(StatusType.Overthrust, NoOpHandler());

        // SC_AETERNA — Priest Lex Aeterna (PR_LEXAETERNA). Marker
        // that the next damage hit on the target should be doubled
        // (and then the SC ends). Damage-side consumer ports next.
        Register(StatusType.Aeterna, NoOpHandler());

        // ---- T2.3 wave 2 — Priest support + Acolyte + Assassin SCs ----

        // SC_IMPOSITIO — Priest Impositio Manus. Val1 = flat ATK boost.
        // Damage-side consumer reads Val1 in the weapon-attack path.
        Register(StatusType.Impositio, NoOpHandler());

        // SC_ASPERSIO — Holy weapon endow (PR_ASPERSIO). Presence
        // flag; weapon-element override consumer ports later.
        Register(StatusType.Aspersio, NoOpHandler());

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

        // SC_SIGNUMCRUCIS — Holy/Dark debuff (AL_CRUCIS). Val1 = DEF
        // drop %. Damage-side consumer reads it during DEF math.
        Register(StatusType.Signumcrucis, NoOpHandler());

        // SC_ENCPOISON — Poison weapon endow (AS_ENCHANTPOISON).
        Register(StatusType.Encpoison, NoOpHandler());

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

        // ---- T2.3 wave 3 — Cloaking, Maximize Power markers ----

        // SC_CLOAKING — Assassin Cloaking (AS_CLOAKING). Presence flag
        // for the visibility hook (similar to Hiding but with different
        // SP-drain cadence).
        Register(StatusType.Cloaking, NoOpHandler());

        // SC_MAXIMIZEPOWER — Blacksmith Maximize Power (BS_MAXIMIZE).
        // Marker that all weapon-attack rolls go to max; damage-side
        // consumer reads it inside BattleCalculator.
        Register(StatusType.Maximizepower, NoOpHandler());

        // ---- T2.3 transcend-class SC markers ----

        // SC_TENSIONRELAX (LK_TENSIONRELAX) — sit + boosted HP regen.
        Register(StatusType.Tensionrelax, NoOpHandler());
        // SC_BERSERK (LK_BERSERK) — triple MaxHp + ASPD + can't cast.
        Register(StatusType.Berserk, NoOpHandler());
        // SC_MAGICPOWER (HW_MAGICPOWER) — next magic cast +50+5*lv %.
        Register(StatusType.Magicpower, NoOpHandler());
        // SC_SACRIFICE (PA_SACRIFICE) — devotion link.
        Register(StatusType.Sacrifice, NoOpHandler());
        // SC_EDP (ASC_EDP) — Enchant Deadly Poison.
        Register(StatusType.Edp, NoOpHandler());
        // SC_WINDWALK (SN_WINDWALK) — +Flee +MoveSpeed.
        Register(StatusType.Windwalk, NoOpHandler());
        // SC_MELTDOWN (WS_MELTDOWN) — break-enemy-gear proc.
        Register(StatusType.Meltdown, NoOpHandler());
        // SC_CARTBOOST (WS_CARTBOOST) — +MoveSpeed while pushing cart.
        Register(StatusType.Cartboost, NoOpHandler());

        // ---- T2.3-P2 (Acolyte wave) SC markers ----
        // SC_LAUDAAGNUS (AB_LAUDAAGNUS) — cures Freeze/Stone/Blind/
        // Burning/Freezing/Crystallize on cast; otherwise +VIT buff.
        Register(StatusType.Laudaagnus, NoOpHandler());
        // SC_LAUDARAMUS (AB_LAUDARAMUS) — cures Sleep/Stun/Mandragora/
        // Silence/DeepSleep on cast; otherwise +CRIT buff.
        Register(StatusType.Laudaramus, NoOpHandler());
        // SC_PROTECTEXP (AB_EXPIATIO etc.) — left as marker for later.
        // SC_BLIND already registered above; we end it from Cure / LaudaAgnus.

        // ---- T2.3-P1 (Heal port) SC markers ----
        // SC_KAITE — bounce-back-heal SC (KG_KAITE). Acolyte/Priest
        // Heal is redirected to the caster while active; consumed per
        // Heal use (Val2 = remaining charges).
        Register(StatusType.Kaite, NoOpHandler());
        // SC_BITESCAR — Sura/4th-class DoT marker; ends when target
        // is healed (AL_HEAL clears it).
        Register(StatusType.Bitescar, NoOpHandler());
        // SC_AKAITSUKI — Sura Yggdrasil-Leaf marker. Flips next heal
        // into damage of equal magnitude.
        Register(StatusType.Akaitsuki, NoOpHandler());
        // SC_SATURDAYNIGHTFEVER — Sura buff that suppresses heal
        // (rAthena: clif still shows the 0 frame, real apply is zero).
        Register(StatusType.Saturdaynightfever, NoOpHandler());

        // ---- T2.3 3rd/4th class SC markers ----
        // SC_DEATHBOUND (RK_DEATHBOUND) — reflect next physical hit.
        Register(StatusType.Deathbound, NoOpHandler());
        // SC_ADORAMUS (AB_ADORAMUS) — Blind-like debuff from Holy spell.
        Register(StatusType.Adoramus, NoOpHandler());
        // SC_DRAGONIC_AURA (DK_DRAGONIC_AURA) — Dragon Knight ATK boost.
        Register(StatusType.DragonicAura, NoOpHandler());

        // SC_REFLECTSHIELD — % chance to reflect damage. Val1 = chance,
        // Val2 = reflect rate. Same combat-hook situation as Autoguard.
        Register(StatusType.Reflectshield, NoOpHandler());

        // SC_STEELBODY — 90 % phys + magic damage reduction. Combat
        // reads the SC presence; here we hold the marker.
        Register(StatusType.Steelbody, NoOpHandler());

        // SC_PROVIDENCE — anti-undead / anti-demon DEF marker. Val1 =
        // demon/undead resist %. Combat-side hook ports later.
        Register(StatusType.Providence, NoOpHandler());

        // ===== Cast-time scaling (consumed by SkillCastTimingService.CastFixSc) =====
        // Each lives here so Start/End round-trip cleanly; the actual
        // cast-time math reads Val1/Val2 inside CastFixSc.

        // SC_SUFFRAGIUM — next cast time × (100 - 15*val1)% (Priest skill).
        // Auto-consumed on cast.
        Register(StatusType.Suffragium, NoOpHandler());

        // SC_MEMORIZE — next 5 casts at half cast time. Val1 starts at 5
        // and decrements per cast; ends on val1=0.
        Register(StatusType.Memorize, NoOpHandler());

        // SC_SLOWCAST (debuff) — next cast time × (100 + 10*val1)%.
        Register(StatusType.Slowcast, NoOpHandler());

        // SC_PARALYSIS (Guillotine Cross status). Val3 = additional cast
        // rate %. rAthena: cast time × (100 + val3)/100.
        Register(StatusType.Paralysis, NoOpHandler());

        // SC_IZAYOI (Kagerou / Oboro). Halves variable cast.
        Register(StatusType.Izayoi, NoOpHandler());

        // SC_POEMBRAGI (Minstrel song). Val2 = combined rate
        // (6*lv + 3*int_caster), reduces cast time and after-skill delay.
        Register(StatusType.Poembragi, NoOpHandler());

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

        // SC_BASILICA_CELL — applied while standing on a Basilica cell;
        // removed when the player steps off. Checked by
        // PlayerPositionHelpers.IsBasilicaCell.
        Register(StatusType.BasilicaCell, NoOpHandler());

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
    /// ST.9-ST.12 — bulk-register a NoOpHandler for every
    /// <see cref="StatusType"/> enum value not yet covered by an
    /// explicit handler above. Picks up flags from
    /// <see cref="StatusFlagDefaults"/>; if no entry exists there,
    /// uses a permissive default (Buff | RemoveOnLogout for positive-
    /// sounding names, Debuff | RemoveOnRefresh otherwise — but the
    /// safe path is RemoveOnLogout-only so logout cleanup still
    /// works).
    /// </summary>
    private void RegisterDefaultsForMissingTypes()
    {
        foreach (StatusType type in System.Enum.GetValues<StatusType>())
        {
            // None / sentinel values stay unregistered.
            if (type == StatusType.None || (short)type < 0) continue;
            if (_handlers.ContainsKey(type)) continue;

            // Pull the default flag set; if absent, use a conservative
            // "buff that drops on logout" classification. The actual
            // SC's stat mods land when a consumer skill plugin needs
            // them and overrides this with a real Register().
            var defaultFlags = StatusFlagDefaults.For(type);
            if (defaultFlags == ScfFlag.None)
                defaultFlags = ScfFlag.RemoveOnLogout;

            _handlers[type] = new StatusEffectHandler(
                _NoOp, _NoOpEnd, Flags: defaultFlags);
        }
    }

    // Shared no-op delegates used by the ST.3 backfill batch.
    private static readonly Action<Entity, StatusChange, Entity?> _NoOp = (_, _, _) => { };
    private static readonly Action<Entity, StatusChange> _NoOpEnd = (_, _) => { };

    /// <summary>
    /// Empty handler — for SCs whose only effect is "I'm present so a
    /// gate or downstream consumer reads my Val1/Val2 directly".
    /// </summary>
    private static StatusEffectHandler NoOpHandler() => new(
        OnStart: (_, _, _) => { },
        OnEnd: (_, _) => { });

    public void Register(StatusType type, StatusEffectHandler handler) => _handlers[type] = handler;

    public StatusEffectHandler? Get(StatusType type) => _handlers.GetValueOrDefault(type);

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
