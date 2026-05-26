using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// AG_DESTRUCTIVE_HURRICANE — Arch Mage Destructive Hurricane. Manual port of
/// <c>rathena-fork/src/map/skills/mage/destructivehurricane.cpp</c>.
///
/// <para>Wind AOE splash. Ratio: <c>+(-100 + 600 + 2850*lv) + 5*SPL</c>.
/// Post-hit, SC_CLIMAX val1==1 fires a follow-up
/// AG_DESTRUCTIVE_HURRICANE_CLIMAX hit on each victim through the
/// skill-attack service.</para>
///
/// <para>INFRA-DEFERRED: SC_CLIMAX val1==2's <c>dmg.blewcount = 2</c>
/// needs <c>ctx</c> on <see cref="ModifyDamageData"/>, which doesn't
/// receive a <see cref="SkillBehaviorContext"/> today. val1==4
/// (caster buff) and val1==5 (19×19 splash) need branch hooks on the
/// pre-cast / splash-radius path that aren't ctx-aware either.</para>
/// </summary>
public sealed class DestructiveHurricane : RecursiveDamageSplashSkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public DestructiveHurricane() : base(SkillIds.AG_DESTRUCTIVE_HURRICANE) { }

    public DestructiveHurricane(ISkillAttackService? skillAttack = null) : base(SkillIds.AG_DESTRUCTIVE_HURRICANE)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: skillratio += -100 + 600 + 2850*lv + 5*SPL.
        // The Climax buff additive is applied through pc_skillatk_bonus, not here.
        return baseRatio + (-100 + 600 + 2850 * skillLevel) + 5 * src.Stats.Spl;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: SC_CLIMAX val1 == 1 fires AG_DESTRUCTIVE_HURRICANE_CLIMAX
        // on each splash victim (12500 % flat ratio, no level scaling).
        var climax = ctx.Sc?.Get(src, StatusType.Climax);
        if (climax != null && climax.Val1 == 1)
            _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target,
                SkillIds.AG_DESTRUCTIVE_HURRICANE_CLIMAX, skillLevel);
    }
}
