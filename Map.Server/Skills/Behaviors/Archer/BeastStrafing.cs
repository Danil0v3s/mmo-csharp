using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// HT_POWER — Hunter Beast Strafing. Manual port of
/// <c>rathena-fork/src/map/skills/archer/beaststrafing.cpp</c>.
///
/// <para>Only fires against Brute / Doram / Insect races. Ratio:
/// <c>+(-50 + 8*STR)</c>.</para>
/// </summary>
public sealed class BeastStrafing : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public BeastStrafing() : base(SkillIds.HT_POWER) { }

    public BeastStrafing(ISkillAttackService? skillAttack = null) : base(SkillIds.HT_POWER)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + (-50 + 8 * src.Stats.Str);
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target.Stats.Race != BattleRace.Brute
            && target.Stats.Race != BattleRace.PlayerDoram
            && target.Stats.Race != BattleRace.Insect)
            return;
        _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);
    }
}
