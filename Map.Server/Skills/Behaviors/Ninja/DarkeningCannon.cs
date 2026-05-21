using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// SS_ANTENPOU — Darkening Cannon. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/darkeningcannon.cpp</c>.
/// Splash damage; ratio <c>+(-100 + 450 + 950*lv) + 5*spl</c>.
/// Mirage cast + alt-flag scaling are TODO.
/// </summary>
public sealed class DarkeningCannon : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public DarkeningCannon() : base(SkillIds.SS_ANTENPOU) { }

    public DarkeningCannon(ISkillAttackService? skillAttack = null) : base(SkillIds.SS_ANTENPOU)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 450 + 950 * skillLevel) + 5 * src.Stats.Spl;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // TODO: splash via skill_area_sub + mirage cast.
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
    }
}
