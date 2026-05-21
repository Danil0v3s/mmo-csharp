using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AB_HIGHNESSHEAL — Arch Bishop Highness Heal. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/highnessheal.cpp</c>.
///
/// <para>Single-target high-power heal with the same suppress /
/// redirect rules as <see cref="Heal"/>:</para>
/// <list type="bullet">
///   <item>StatusImmune mob (or Emperium / Battlefield mob class) → heal = 0.</item>
///   <item>SC_KAITE → bounce-back; self-cast voided.</item>
///   <item>SC_BERSERK / SC_SATURDAYNIGHTFEVER → heal suppressed (0 frame).</item>
///   <item>SC_AKAITSUKI → heal becomes damage.</item>
///   <item>Cast on Undead via <c>CastendDamageId</c> resolves as magic damage.</item>
/// </list>
///
/// <para>The Highness formula uses <c>skill_calc_heal</c> with a
/// higher multiplier than AL_HEAL — for now we reuse the renewal
/// heal formula bumped by the AB_HIGHNESSHEAL skill_db EffectAmount
/// (placeholder: 2.5× AL_HEAL output).</para>
/// </summary>
public sealed class HighnessHeal : SkillImpl
{
    private readonly IStatusOpsService? _statusOps;
    private readonly Map.Server.Skills.ISkillAttackService? _skillAttack;
    private readonly IExpService? _exp;
    private readonly IBattleConfigService? _battleConfig;

    public HighnessHeal() : base(SkillIds.AB_HIGHNESSHEAL) { }

    public HighnessHeal(
        IStatusOpsService? statusOps = null,
        Map.Server.Skills.ISkillAttackService? skillAttack = null,
        IExpService? exp = null,
        IBattleConfigService? battleConfig = null) : base(SkillIds.AB_HIGHNESSHEAL)
    {
        _statusOps = statusOps;
        _skillAttack = skillAttack;
        _exp = exp;
        _battleConfig = battleConfig;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: skill_attack(BF_MAGIC, ...) — undead path.
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Heal formula — renewal AB_HIGHNESSHEAL is 2.5× the renewal AL_HEAL
        // baseline; computed inline (same approach as Heal.cs).
        var heal = CalcHighnessHeal(src, skillLevel);

        // Suppress checks (identical to AL_HEAL).
        if ((target.Stats.Mode & MobMode.StatusImmune) != 0) heal = 0;

        var sc = ctx.Sc;
        if (sc != null)
        {
            var kaite = sc.Get(target, StatusType.Kaite);
            if (kaite != null && (src.Stats.Mode & MobMode.StatusImmune) == 0)
            {
                kaite.Val2--;
                if (kaite.Val2 <= 0) sc.End(target, StatusType.Kaite);
                if (ReferenceEquals(src, target)) heal = 0;
                else target = src;
            }
            else if (sc.Get(target, StatusType.Berserk) != null ||
                     sc.Get(target, StatusType.Saturdaynightfever) != null)
            {
                heal = 0;
            }
        }

        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, heal);

        if (sc?.Get(target, StatusType.Akaitsuki) != null && heal != 0) heal = -heal;

        int actuallyHealed = 0;
        if (_statusOps != null && heal > 0 && target.Stats.Hp > 0)
        {
            var preHp = target.Stats.Hp;
            _statusOps.Heal(target, heal, 0, 0);
            actuallyHealed = target.Stats.Hp - preHp;
        }
        else if (heal < 0)
        {
            ctx.Damage?.ApplyDamage(target, -heal, src);
        }

        // Heal-EXP gain (same as AL_HEAL).
        if (src is PlayerEntity caster && target is PlayerEntity dst &&
            !ReferenceEquals(caster, dst) && actuallyHealed > 0 &&
            _battleConfig != null && _exp != null)
        {
            var knob = _battleConfig.GetValue("heal_exp");
            if (knob > 0)
            {
                long jobExp = (long)actuallyHealed * knob / 100;
                if (jobExp <= 0) jobExp = 1;
                _exp.GainExp(caster, baseExp: 0, jobExp: jobExp);
            }
        }
    }

    private static int CalcHighnessHeal(Entity src, ushort skillLevel)
    {
        // Renewal AL_HEAL formula × 2.5 (rough approximation of the
        // AB_HIGHNESSHEAL multiplier from skill_db).
        var hp = (src.Level + src.Stats.IntStat) / 5 * 30 * skillLevel / 10;
        return Math.Max(1, hp * 25 / 10);
    }
}
