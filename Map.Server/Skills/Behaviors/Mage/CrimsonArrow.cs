using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// AG_CRIMSON_ARROW — Arch Mage Crimson Arrow. Manual port of
/// <c>rathena-fork/src/map/skills/mage/crimsonarrow.cpp</c>.
///
/// <para>Single-target Fire magic. Ratio: <c>+(-100 + 400*lv) + 3*SPL</c>
/// then RE_LVL_DMOD(100). The companion AOE skill
/// (AG_CRIMSON_ARROW_ATK) splash on the eight-path is TODO until the
/// map-foreach-in-path helper is wired.</para>
/// </summary>
public sealed class CrimsonArrow : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public CrimsonArrow() : base(SkillIds.AG_CRIMSON_ARROW) { }

    public CrimsonArrow(ISkillAttackService? skillAttack = null) : base(SkillIds.AG_CRIMSON_ARROW)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: skillratio += -100 + 400*lv + 3*SPL.
        return baseRatio + (-100 + 400 * skillLevel) + 3 * src.Stats.Spl;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena fires AG_CRIMSON_ARROW_ATK splash along the eight-path. Splash TODO;
        // primary hit lands on the named target.
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
    }
}
