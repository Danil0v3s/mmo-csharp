using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// SS_KAGENOMAI — Shadow Dance. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/shadowdance.cpp</c>.
/// Splash damage; ratio <c>+(-100 + 750 + 900*lv) + 5*pow</c>.
/// SS_KAGEGARI partner bonus + mirage cast + alt-flag scaling TODO.
/// </summary>
public sealed class ShadowDance : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public ShadowDance() : base(SkillIds.SS_KAGENOMAI) { }

    public ShadowDance(ISkillAttackService? skillAttack = null) : base(SkillIds.SS_KAGENOMAI)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 750 + 900 * skillLevel) + 5 * src.Stats.Pow;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);
    }
}
