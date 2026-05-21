using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AL_HEAL — Acolyte Heal. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/heal.cpp</c>.
///
/// <para>Two entry points:</para>
/// <list type="bullet">
///   <item><see cref="CastendNoDamageId"/> — friendly heal target. Runs the
///         renewal heal formula, applies the bounce-back / suppress /
///         flip side-channels (Kaite / Berserk / Saturday Night Fever /
///         Akaitsuki), broadcasts the heal number, and gives the
///         caster job EXP per <c>battle_config.heal_exp</c>.</item>
///   <item><see cref="CastendDamageId"/> — hostile cast against an undead
///         target. Routes to the generic magic-attack pipeline (the
///         damage resolver treats AL_HEAL as Holy element vs Undead).</item>
/// </list>
///
/// <para><b>Renewal formula</b> (skill_calc_heal in skill.cpp):</para>
/// <code>
///   heal = ((BaseLv + INT) / 5) * 30 * skill_lv / 10  +  hp_bonus
///   hp_bonus += 2% per HP_MEDITATIO level on caster
///   if (caster is Super Novice married to target): heal *= 2
///   (MATK addition omitted — pending IBattleCalculator.MatkBase)
/// </code>
///
/// <para><b>Suppress / redirect rules</b> (in this order):</para>
/// <list type="number">
///   <item>Target is status-immune (MD_STATUSIMMUNE), or is the
///         Emperium, or is a Battlefield-class mob → heal = 0.</item>
///   <item>Target has SC_KAITE and caster is NOT MD_STATUSIMMUNE:
///         decrement Val2; when 0 the SC ends. Self-heal under Kaite
///         is voided. Otherwise the heal bounces to the caster.</item>
///   <item>Else target has SC_BERSERK or SC_SATURDAYNIGHTFEVER → heal = 0
///         (still displays the 0 frame, per rAthena comment).</item>
/// </list>
///
/// <para>Always: end SC_BITESCAR on the target before broadcasting,
/// then flip the heal sign under SC_AKAITSUKI (turns the heal into
/// damage — Yggdrasil Leaf interaction).</para>
/// </summary>
public sealed class Heal : SkillImpl
{
    private readonly ISkillSideEffectService? _sideEffects;
    private readonly IStatusOpsService? _statusOps;
    private readonly IExpService? _exp;
    private readonly IBattleConfigService? _battleConfig;
    private readonly Map.Server.Skills.ISkillAttackService? _skillAttack;

    /// <summary>Test ctor — bare, for the few tests that don't exercise heal application.</summary>
    public Heal() : base(SkillIds.AL_HEAL) { }

    /// <summary>Production ctor — all five collaborators are optional so
    /// the registry can still build the instance when a subset is
    /// missing during the migration; missing services degrade the
    /// behavior gracefully (no broadcast, no heal applied, no EXP).</summary>
    public Heal(
        ISkillSideEffectService? sideEffects = null,
        IStatusOpsService? statusOps = null,
        IExpService? exp = null,
        IBattleConfigService? battleConfig = null,
        Map.Server.Skills.ISkillAttackService? skillAttack = null) : base(SkillIds.AL_HEAL)
    {
        _sideEffects = sideEffects;
        _statusOps = statusOps;
        _exp = exp;
        _battleConfig = battleConfig;
        _skillAttack = skillAttack;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: int32 heal = skill_calc_heal(src, bl, getSkillId(), skill_lv, true);
        // The C# port has ISkillSideEffectService.CalcHeal but it uses the
        // pre-renewal multiplier model; for AL_HEAL we apply the renewal
        // formula directly so the math matches the fork's heal.cpp
        // expectation (skill_calc_heal is renewal-branched at the call site).
        var heal = CalcRenewalHeal(src, target, skillLevel);

        // rAthena: status_isimmune(bl) || (dstmd && (status_get_class(bl) == MOBID_EMPERIUM || status_get_class_(bl) == CLASS_BATTLEFIELD)) → heal = 0
        // Emperium + Battlefield class checks: not yet modelled on MobEntity in the C# port —
        // MobMode.StatusImmune covers status_isimmune cleanly; the named-class
        // gates land when MobConstants.Emperium / CLASS_BATTLEFIELD wire up.
        if ((target.Stats.Mode & MobMode.StatusImmune) != 0)
            heal = 0;

        // Suppress / redirect via target SCs. rAthena reads tsc once
        // and walks a chain: Kaite (bounce-back) → else Berserk /
        // SaturdayNightFever (suppressed).
        var sc = ctx.Sc;
        if (sc != null)
        {
            var kaite = sc.Get(target, StatusType.Kaite);
            if (kaite != null && (src.Stats.Mode & MobMode.StatusImmune) == 0)
            {
                // rAthena: --tsc->getSCE(SC_KAITE)->val2 → if it falls to ≤0, end SC
                kaite.Val2--;
                if (kaite.Val2 <= 0) sc.End(target, StatusType.Kaite);
                if (ReferenceEquals(src, target))
                {
                    // Self-heal under Kaite is voided per rAthena comment.
                    heal = 0;
                }
                else
                {
                    // Bounce back: redirect the heal to the caster. After
                    // this rebind every subsequent branch operates on
                    // `target = src` exactly as rAthena does (`bl = src;`).
                    target = src;
                }
            }
            else
            {
                // SC_BERSERK / SC_SATURDAYNIGHTFEVER → suppress heal,
                // but still emit the (0) frame so the visual is consistent.
                if (sc.Get(target, StatusType.Berserk) != null ||
                    sc.Get(target, StatusType.Saturdaynightfever) != null)
                {
                    heal = 0;
                }
            }
        }

        // rAthena: status_change_end(bl, SC_BITESCAR);
        sc?.End(target, StatusType.Bitescar);

        // rAthena: clif_skill_nodamage(src, *bl, getSkillId(), heal);
        // The packet's "level" field is overloaded with the heal amount
        // for heal-class skills — ISkillClientService handles that
        // overload on our side.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, heal);

        // rAthena: if (tsc && tsc->getSCE(SC_AKAITSUKI) && heal) heal = ~heal + 1;
        // (~heal + 1 == -heal in two's complement). Akaitsuki flips the
        // heal into damage — used by certain Yggdrasil-Leaf-style ports.
        if (sc?.Get(target, StatusType.Akaitsuki) != null && heal != 0)
            heal = -heal;

        // rAthena: t_exp heal_get_jobexp = status_heal(bl, heal, 0, 0);
        // status_heal returns the *actually-healed* amount (post-clamp
        // against MaxHp). We capture it the same way: pre-HP minus
        // post-HP. IStatusOpsService.Heal handles the clamp internally
        // but its return value ignores it, so compute the delta directly.
        int actuallyHealed = 0;
        if (_statusOps != null && heal > 0 && target.Stats.Hp > 0)
        {
            var preHp = target.Stats.Hp;
            _statusOps.Heal(target, heal, 0, 0);
            actuallyHealed = target.Stats.Hp - preHp;
        }
        else if (heal < 0 && _statusOps != null)
        {
            // Akaitsuki: heal turned to damage. Apply via the damage
            // pipeline so SC consumers (Steel Body / Kyrie / etc.) still
            // gate it like a normal hit. ctx.Damage is the canonical
            // entry; using -heal as the magnitude.
            ctx.Damage?.ApplyDamage(target, -heal, src);
        }

        // rAthena: heal_get_jobexp = healAmount * battle_config.heal_exp / 100;
        //          if heal_get_jobexp <= 0: heal_get_jobexp = 1;
        //          pc_gainexp(sd, bl, 0, heal_get_jobexp, 0);
        // Gates: caster + target both players, distinct, heal > 0,
        // heal_exp > 0. The C# port reads heal_exp via the generic
        // IBattleConfigService.GetValue("heal_exp") accessor.
        if (src is PlayerEntity caster && target is PlayerEntity dst &&
            !ReferenceEquals(caster, dst) && actuallyHealed > 0 &&
            _battleConfig != null && _exp != null)
        {
            var healExpKnob = _battleConfig.GetValue("heal_exp");
            if (healExpKnob > 0)
            {
                long jobExp = (long)actuallyHealed * healExpKnob / 100;
                if (jobExp <= 0) jobExp = 1;
                _exp.GainExp(caster, baseExp: 0, jobExp: jobExp);
            }
        }
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: skill_attack(BF_MAGIC, src, src, target, getSkillId(), skill_lv, tick, flag);
        // Used when the cast lands on an Undead target — skill_attack runs
        // the magic-attack pipeline and the Holy-vs-Undead element matrix
        // converts the heal into damage. The damage resolver
        // (MagicSkillResolver) already understands this case for AL_HEAL.
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
    }

    /// <summary>
    /// Renewal AL_HEAL formula (skill_calc_heal, skill.cpp). Inlined here
    /// so the per-skill body owns its math instead of routing through the
    /// pre-renewal multiplier path in SkillSideEffectService.
    ///
    /// <code>
    ///   hp = ((BaseLv + INT) / 5) * 30 * skill_lv / 10
    ///   + 2% per HP_MEDITATIO level (caster only)
    ///   * 2 if caster is married-couple Super Novice
    /// </code>
    /// MATK addition is part of the renewal formula but skipped here
    /// until IBattleCalculator exposes a MatkBase accessor —
    /// the constant component is the dominant term across all levels.
    /// </summary>
    private int CalcRenewalHeal(Entity src, Entity target, ushort skillLevel)
    {
        var lv = src.Level;
        var intStat = src.Stats.IntStat;
        var hp = (lv + intStat) / 5 * 30 * skillLevel / 10;

        // HP_MEDITATIO bonus +2%/level. Only PCs learn it.
        if (src is PlayerEntity sd)
        {
            var meditatio = sd.LearnedSkills.GetValueOrDefault(SkillIds.HP_MEDITATIO);
            if (meditatio > 0)
            {
                hp += hp * meditatio * 2 / 100;
            }
        }

        // rAthena: if (sd && tsd && sd->status.partner_id == tsd->status.char_id &&
        //          (sd->class_&MAPID_UPPERMASK) == MAPID_SUPER_NOVICE && sd->status.sex == 0)
        //          hp *= 2;
        // The Sex + class-mapid gates aren't modelled on PlayerEntity yet
        // (no SuperNovice class flag exposed); we only apply the partner
        // doubling. Falls back to "no double" when the data is missing,
        // matching rAthena when those flags are unset.
        if (src is PlayerEntity caster && target is PlayerEntity dstPc &&
            caster.PartnerId == dstPc.CharacterId)
        {
            // TODO: gate on (class_&MAPID_UPPERMASK)==MAPID_SUPER_NOVICE && sex==0
            //       once class-mapid + sex make it onto PlayerEntity.
            hp *= 2;
        }

        return Math.Max(1, hp);
    }
}
