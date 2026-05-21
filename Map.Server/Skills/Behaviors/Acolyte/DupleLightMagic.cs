using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AB_DUPLELIGHT_MAGIC — Arch Bishop Duple Light (Magic component).
/// Manual port of the AB_DUPLELIGHT_MAGIC half of
/// <c>rathena-fork/src/map/skills/acolyte/duplelight.cpp</c>.
///
/// <para>Holy magic component of the Duple Light passive auto-cast.
/// Ratio: <c>+300 + 40 * lv</c>.</para>
/// </summary>
public sealed class DupleLightMagic : SkillImpl
{
    private readonly Map.Server.Skills.ISkillAttackService? _skillAttack;

    public DupleLightMagic() : base(SkillIds.AB_DUPLELIGHT_MAGIC) { }

    public DupleLightMagic(Map.Server.Skills.ISkillAttackService? skillAttack = null) : base(SkillIds.AB_DUPLELIGHT_MAGIC)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + 300 + 40 * skillLevel;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
    }
}
