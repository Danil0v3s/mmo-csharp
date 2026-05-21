using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_FIRE_ARROW — Elemental Fire Arrow. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/firearrow.cpp</c>.
/// Single-target weapon hit; +200 ratio.
/// </summary>
public sealed class FireArrow : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public FireArrow() : base(SkillIds.EL_FIRE_ARROW) { }

    public FireArrow(ISkillAttackService? skillAttack = null) : base(SkillIds.EL_FIRE_ARROW)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 200;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);
    }
}
