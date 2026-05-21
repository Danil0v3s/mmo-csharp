using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_ERASER_CUTTER — Homunculus Eraser Cutter. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_erasercutter.cpp</c>.
/// Magic-type ratio <c>+(-100 + 450*lv*BaseLv/100) + INT</c>.
/// </summary>
public sealed class EraserCutter : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public EraserCutter() : base(SkillIds.MH_ERASER_CUTTER) { }

    public EraserCutter(ISkillAttackService? skillAttack = null) : base(SkillIds.MH_ERASER_CUTTER)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 450 * skillLevel * src.Level / 100) + src.Stats.IntStat;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
}
