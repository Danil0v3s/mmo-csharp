using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// RL_H_MINE — Rebellion Howling Mine. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/howlingmine.cpp</c>.
/// Direct hit: ratio <c>+(-100 + 200 + 200*lv)</c> and tags target with
/// SC_H_MINE. Flicker-detonate: ratio <c>+(-100 + 500 + 300*lv)</c> with
/// splash + SC_BURNING follow-up (TODO).
/// </summary>
public sealed class HowlingMine : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public HowlingMine() : base(SkillIds.RL_H_MINE) { }

    public HowlingMine(ISkillAttackService? skillAttack = null) : base(SkillIds.RL_H_MINE)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 200 + 200 * skillLevel);

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.HMine, val1: skillLevel, val2: (int)src.Id, 0, 0, durationMs: 30_000, src);
    }
}
