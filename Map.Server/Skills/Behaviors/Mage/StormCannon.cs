using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// AG_STORM_CANNON — Arch Mage Storm Cannon. Manual port of
/// <c>rathena-fork/src/map/skills/mage/stormcannon.cpp</c>.
///
/// <para>Eight-path Wind-magic line. Ratio: <c>+(-100 + 1550*lv) + 5*SPL</c>;
/// +<c>300*lv</c> when SC_CLIMAX is active on caster. INFRA-DEFERRED:
/// the <c>map_foreachindir</c> path-AoE splash isn't wired here; we
/// land the primary magic hit on the named target.</para>
/// </summary>
public sealed class StormCannon : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public StormCannon() : base(SkillIds.AG_STORM_CANNON) { }

    public StormCannon(ISkillAttackService? skillAttack = null) : base(SkillIds.AG_STORM_CANNON)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var ratio = baseRatio + (-100 + 1550 * skillLevel) + 5 * src.Stats.Spl;
        if (ctx.Sc?.Get(src, StatusType.Climax) != null)
            ratio += 300 * skillLevel;
        return ratio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
    }
}
